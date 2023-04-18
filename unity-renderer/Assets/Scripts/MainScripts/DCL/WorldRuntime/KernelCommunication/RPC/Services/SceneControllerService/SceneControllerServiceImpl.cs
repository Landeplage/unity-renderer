using Cysharp.Threading.Tasks;
using DCL;
using DCL.Controllers;
using DCL.CRDT;
using DCL.Helpers;
using DCL.Models;
using Decentraland.Renderer.RendererServices;
using Google.Protobuf;
using rpc_csharp;
using RPC.Context;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using BinaryWriter = KernelCommunication.BinaryWriter;
using Decentraland.Sdk.Ecs6;

namespace RPC.Services
{
    public class SceneControllerServiceImpl : IRpcSceneControllerService<RPCContext>
    {
        // HACK: Until we fix the code generator, we must replace all 'Decentraland.Common.Entity' for 'DCL.ECSComponents.Entity' in RpcSceneController.gen.cs
        // to be able to access request.Entity properties.
        private static readonly UnloadSceneResult defaultUnloadSceneResult = new UnloadSceneResult();

        private static readonly SendBatchResponse defaultSendBatchResult = new SendBatchResponse();

        private const string REQUIRED_PORT_ID_START = "scene-";

        private int sceneNumber = -1;
        private RPCContext context;
        private RpcServerPort<RPCContext> port;

        private readonly MemoryStream sendCrdtMemoryStream;
        private readonly BinaryWriter sendCrdtBinaryWriter;
        private readonly MemoryStream getStateMemoryStream;
        private readonly BinaryWriter getStateBinaryWriter;

        private readonly CRDTSceneMessage reusableCrdtMessageResult = new CRDTSceneMessage();

        public static void RegisterService(RpcServerPort<RPCContext> port)
        {
            if (!port.portName.StartsWith(REQUIRED_PORT_ID_START)) return;

            RpcSceneControllerServiceCodeGen.RegisterService(port, new SceneControllerServiceImpl(port));
        }

        public SceneControllerServiceImpl(RpcServerPort<RPCContext> port)
        {
            port.OnClose += OnPortClose;
            this.port = port;

            sendCrdtMemoryStream = new MemoryStream();
            sendCrdtBinaryWriter = new BinaryWriter(sendCrdtMemoryStream);

            getStateMemoryStream = new MemoryStream();
            getStateBinaryWriter = new BinaryWriter(getStateMemoryStream);
        }

        private void OnPortClose()
        {
            port.OnClose -= OnPortClose;

            if (context != null && context.crdt.WorldState.ContainsScene(sceneNumber))
                UnloadScene(null, context, new CancellationToken()).Forget();
        }

        public async UniTask<LoadSceneResult> LoadScene(LoadSceneMessage request, RPCContext context, CancellationToken ct)
        {
            sceneNumber = request.SceneNumber;
            this.context = context;

            List<ContentServerUtils.MappingPair> parsedContent = new List<ContentServerUtils.MappingPair>();

            for (var i = 0; i < request.Entity.Content.Count; i++)
            {
                parsedContent.Add(new ContentServerUtils.MappingPair()
                {
                    file = request.Entity.Content[i].File,
                    hash = request.Entity.Content[i].Hash
                });
            }

            CatalystSceneEntityMetadata parsedMetadata = Utils.SafeFromJson<CatalystSceneEntityMetadata>(request.Entity.Metadata);
            Vector2Int[] parsedParcels = new Vector2Int[parsedMetadata.scene.parcels.Length];

            for (int i = 0; i < parsedMetadata.scene.parcels.Length; i++)
            {
                parsedParcels[i] = Utils.StringToVector2Int(parsedMetadata.scene.parcels[i]);
            }

            await UniTask.SwitchToMainThread(ct);

            if (request.IsGlobalScene)
            {
                CreateGlobalSceneMessage globalScene = new CreateGlobalSceneMessage()
                {
                    contents = parsedContent,
                    id = request.Entity.Id,
                    sdk7 = request.Sdk7,
                    name = request.SceneName,
                    baseUrl = request.BaseUrl,
                    sceneNumber = sceneNumber,
                    isPortableExperience = request.IsPortableExperience,
                    requiredPermissions = parsedMetadata.requiredPermissions,
                    allowedMediaHostnames = parsedMetadata.allowedMediaHostnames,
                    icon = string.Empty // TODO: add icon url!
                };

                context.crdt.SceneController.CreateGlobalScene(globalScene);
            }
            else
            {
                LoadParcelScenesMessage.UnityParcelScene unityParcelScene = new LoadParcelScenesMessage.UnityParcelScene()
                {
                    sceneNumber = sceneNumber,
                    id = request.Entity.Id,
                    sdk7 = request.Sdk7,
                    baseUrl = request.BaseUrl,
                    baseUrlBundles = request.BaseUrlAssetBundles,
                    basePosition = Utils.StringToVector2Int(parsedMetadata.scene.@base),
                    parcels = parsedParcels,
                    contents = parsedContent,
                    requiredPermissions = parsedMetadata.requiredPermissions,
                    allowedMediaHostnames = parsedMetadata.allowedMediaHostnames
                };

                context.crdt.SceneController.LoadUnityParcelScene(unityParcelScene);
            }

            LoadSceneResult result = new LoadSceneResult() { Success = true };
            return result;
        }

        public async UniTask<UnloadSceneResult> UnloadScene(UnloadSceneMessage request, RPCContext context, CancellationToken ct)
        {
            await UniTask.SwitchToMainThread(ct);
            context.crdt.SceneController.UnloadParcelSceneExecute(sceneNumber);

            return defaultUnloadSceneResult;
        }

        public async UniTask<CRDTSceneMessage> SendCrdt(CRDTSceneMessage request, RPCContext context, CancellationToken ct)
        {
            IParcelScene scene = null;
            CRDTServiceContext crdtContext = context.crdt;

            await UniTask.SwitchToMainThread(ct);

            // This line is to avoid a race condition because a CRDT message could be sent before the scene was loaded
            // more info: https://github.com/decentraland/sdk/issues/480#issuecomment-1331309908
            await UniTask.WaitUntil(() => crdtContext.WorldState.TryGetScene(sceneNumber, out scene),
                cancellationToken: ct);

            await UniTask.WaitWhile(() => crdtContext.MessagingControllersManager.HasScenePendingMessages(sceneNumber),
                cancellationToken: ct);

            try
            {
                int incomingCrdtCount = 0;
                reusableCrdtMessageResult.Payload = ByteString.Empty;

                using (var iterator = CRDTDeserializer.DeserializeBatch(request.Payload.Memory))
                {
                    while (iterator.MoveNext())
                    {
                        if (!(iterator.Current is CRDTMessage crdtMessage))
                            continue;

                        crdtContext.CrdtMessageReceived?.Invoke(sceneNumber, crdtMessage);
                        incomingCrdtCount++;
                    }
                }

                if (incomingCrdtCount > 0)
                {
                    // When sdk7 scene receive it first crdt we set `InitMessagesDone` since
                    // kernel won't be sending that message for those scenes
                    if (scene.sceneData.sdk7 && !scene.IsInitMessageDone())
                    {
                        crdtContext.SceneController.EnqueueSceneMessage(new QueuedSceneMessage_Scene()
                        {
                            sceneNumber = sceneNumber,
                            tag = "scene",
                            payload = new Protocol.SceneReady(),
                            method = MessagingTypes.INIT_DONE,
                            type = QueuedSceneMessage.Type.SCENE_MESSAGE
                        });
                    }
                }

                if (crdtContext.scenesOutgoingCrdts.TryGetValue(sceneNumber, out DualKeyValueSet<int, long, CRDTMessage> sceneCrdtOutgoing))
                {
                    sendCrdtMemoryStream.SetLength(0);
                    crdtContext.scenesOutgoingCrdts.Remove(sceneNumber);

                    for (int i = 0; i < sceneCrdtOutgoing.Count; i++)
                    {
                        CRDTSerializer.Serialize(sendCrdtBinaryWriter, sceneCrdtOutgoing.Pairs[i].value);
                    }


                    sceneCrdtOutgoing.Clear();
                    reusableCrdtMessageResult.Payload = ByteString.CopyFrom(sendCrdtMemoryStream.ToArray());
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return reusableCrdtMessageResult;
        }

        public async UniTask<CRDTSceneCurrentState> GetCurrentState(GetCurrentStateMessage request, RPCContext context, CancellationToken ct)
        {
            DualKeyValueSet<int, long, CRDTMessage> outgoingMessages = null;
            CRDTProtocol sceneState = null;
            CRDTServiceContext crdtContext = context.crdt;

            // we wait until messages for scene are set
            await UniTask.WaitUntil(() => crdtContext.scenesOutgoingCrdts.TryGetValue(sceneNumber, out outgoingMessages),
                cancellationToken: ct);

            await UniTask.SwitchToMainThread(ct);

            if (crdtContext.CrdtExecutors != null && crdtContext.CrdtExecutors.TryGetValue(sceneNumber, out ICRDTExecutor executor))
            {
                sceneState = executor.crdtProtocol;
            }

            CRDTSceneCurrentState result = new CRDTSceneCurrentState
            {
                HasOwnEntities = false
            };

            try
            {
                getStateMemoryStream.SetLength(0);

                // serialize outgoing messages
                crdtContext.scenesOutgoingCrdts.Remove(sceneNumber);
                foreach (var msg in outgoingMessages)
                {
                    CRDTSerializer.Serialize(getStateBinaryWriter, msg.value);
                }
                outgoingMessages.Clear();

                // serialize scene state
                if (sceneState != null)
                {
                    var crdtMessages = sceneState.GetStateAsMessages();

                    for (int i = 0; i < crdtMessages.Count; i++)
                    {
                        if (crdtMessages[i].data != null){
                            result.HasOwnEntities = true;
                            break;
                        }
                    }

                    for (int i = 0; i < crdtMessages.Count; i++)
                    {
                        CRDTSerializer.Serialize(getStateBinaryWriter, crdtMessages[i]);
                    }
                }

                result.Payload = ByteString.CopyFrom(getStateMemoryStream.ToArray());
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return result;
        }

        public async UniTask<SendBatchResponse> SendBatch(SendBatchRequest request, RPCContext context, CancellationToken ct)
        {
            CRDTServiceContext crdtContext = context.crdt;
            await UniTask.SwitchToMainThread(ct);

            try
            {
                foreach(var action in request.Actions)
                {
                    QueuedSceneMessage_Scene queuedMessage = new QueuedSceneMessage_Scene
                        {
                            type = QueuedSceneMessage.Type.SCENE_MESSAGE,
                            sceneNumber = sceneNumber,
                            tag = action.Tag,
                        };

                    switch (action.Payload.PayloadCase)
                    {

                        case EntityActionPayload.PayloadOneofCase.InitMessagesFinished:
                            queuedMessage.method = MessagingTypes.INIT_DONE;
                            queuedMessage.payload = new Protocol.SceneReady();
                            crdtContext.SceneController.EnqueueSceneMessage(queuedMessage);
                            break;


                        case EntityActionPayload.PayloadOneofCase.OpenExternalUrl:
                            queuedMessage.method = MessagingTypes.OPEN_EXTERNAL_URL;
                            queuedMessage.payload = new Protocol.OpenExternalUrl() {url = action.Payload.OpenExternalUrl.Url};
                            crdtContext.SceneController.EnqueueSceneMessage(queuedMessage);
                            break;

                        case EntityActionPayload.PayloadOneofCase.OpenNftDialog:
                            queuedMessage.method = MessagingTypes.OPEN_NFT_DIALOG;
                            queuedMessage.payload = new Protocol.OpenNftDialog()
                                {
                                    contactAddress = action.Payload.OpenNftDialog.AssetContractAddress,
                                    comment = action.Payload.OpenNftDialog.Comment,
                                    tokenId = action.Payload.OpenNftDialog.TokenId
                                };
                            crdtContext.SceneController.EnqueueSceneMessage(queuedMessage);
                            break;

                        case EntityActionPayload.PayloadOneofCase.CreateEntity:
                            queuedMessage.method = MessagingTypes.ENTITY_CREATE;
                            queuedMessage.payload = new Protocol.CreateEntity() {entityId = action.Payload.CreateEntity.Id};
                            crdtContext.SceneController.EnqueueSceneMessage(queuedMessage);
                            break;

                        case EntityActionPayload.PayloadOneofCase.RemoveEntity:
                            queuedMessage.method = MessagingTypes.ENTITY_DESTROY;
                            queuedMessage.payload = new Protocol.RemoveEntity() {entityId = action.Payload.RemoveEntity.Id};
                            crdtContext.SceneController.EnqueueSceneMessage(queuedMessage);
                            break;

                        case EntityActionPayload.PayloadOneofCase.AttachEntityComponent:
                            queuedMessage.method = MessagingTypes.SHARED_COMPONENT_ATTACH;
                            queuedMessage.payload = new Protocol.SharedComponentAttach()
                                {
                                    entityId = action.Payload.AttachEntityComponent.EntityId,
                                    id = action.Payload.AttachEntityComponent.Id,
                                    name = action.Payload.AttachEntityComponent.Name
                                };
                            crdtContext.SceneController.EnqueueSceneMessage(queuedMessage);
                            break;

                        case EntityActionPayload.PayloadOneofCase.ComponentRemoved:
                            queuedMessage.method = MessagingTypes.ENTITY_COMPONENT_DESTROY;
                            queuedMessage.payload = new Protocol.EntityComponentDestroy() {entityId = action.Payload.ComponentRemoved.EntityId, name = action.Payload.ComponentRemoved.Name};
                            crdtContext.SceneController.EnqueueSceneMessage(queuedMessage);
                            break;

                        case EntityActionPayload.PayloadOneofCase.SetEntityParent:
                            queuedMessage.method = MessagingTypes.ENTITY_REPARENT;
                            queuedMessage.payload = new Protocol.SetEntityParent() {entityId = action.Payload.SetEntityParent.EntityId, parentId = action.Payload.SetEntityParent.ParentId};
                            crdtContext.SceneController.EnqueueSceneMessage(queuedMessage);
                            break;

                        case EntityActionPayload.PayloadOneofCase.Query:
                            queuedMessage.method = MessagingTypes.QUERY;

                            string queryId = action.Payload.Query.QueryId;
                            RaycastType raycastType = RaycastType.NONE;
                            switch (action.Payload.Query.Payload.QueryType) {
                                case "HitFirst":
                                    raycastType = RaycastType.HIT_FIRST;
                                    break; 
                                case "HitAll":
                                    raycastType = RaycastType.HIT_ALL;
                                    break; 
                                case "HitFirstAvatar":
                                    raycastType = RaycastType.HIT_FIRST_AVATAR;
                                    break; 
                                case "HitAllAvatars":
                                    raycastType = RaycastType.HIT_ALL_AVATARS;
                                    break; 
                            }

                            DCL.Models.Ray ray = new DCL.Models.Ray()
                            {
                                origin = new Vector3(){ x = action.Payload.Query.Payload.Ray.Origin.X, y = action.Payload.Query.Payload.Ray.Origin.Y, z = action.Payload.Query.Payload.Ray.Origin.Z },
                                direction = new Vector3(){ x = action.Payload.Query.Payload.Ray.Direction.X, y = action.Payload.Query.Payload.Ray.Direction.Y, z = action.Payload.Query.Payload.Ray.Direction.Z },
                                distance = action.Payload.Query.Payload.Ray.Distance
                            };

                            queuedMessage.method = MessagingTypes.QUERY;
                            queuedMessage.payload = new QueryMessage()
                            {
                                payload = new RaycastQuery()
                                {
                                    id = queryId,
                                    raycastType = raycastType,
                                    ray = ray,
                                    sceneNumber = sceneNumber
                                }
                            };
                            crdtContext.SceneController.EnqueueSceneMessage(queuedMessage);
                            break;

                        case EntityActionPayload.PayloadOneofCase.ComponentCreated:
                            queuedMessage.method = MessagingTypes.SHARED_COMPONENT_CREATE;
                            queuedMessage.payload =  new Protocol.SharedComponentCreate() {
                                id = action.Payload.ComponentCreated.Id,
                                classId = action.Payload.ComponentCreated.ClassId,
                                name = action.Payload.ComponentCreated.Name
                            };
                            crdtContext.SceneController.EnqueueSceneMessage(queuedMessage);
                            break;

                        case EntityActionPayload.PayloadOneofCase.ComponentDisposed:
                            queuedMessage.method = MessagingTypes.SHARED_COMPONENT_DISPOSE;
                            queuedMessage.payload = new Protocol.SharedComponentDispose() {id = action.Payload.ComponentDisposed.Id};
                            crdtContext.SceneController.EnqueueSceneMessage(queuedMessage);
                            break;

                 // This has changed!
                        case EntityActionPayload.PayloadOneofCase.UpdateEntityComponent:
                            object updateData = ComponentModelFromPayload(action.Payload.UpdateEntityComponent.ComponentData);

                            if (updateData != null)
                            {
                                queuedMessage.method = MessagingTypes.PB_ENTITY_COMPONENT_CREATE_OR_UPDATE;
                                queuedMessage.payload =
                                    action.Payload.UpdateEntityComponent.ComponentData.PayloadCase
                                        is ComponentBodyPayload.PayloadOneofCase.Animator
                                        or ComponentBodyPayload.PayloadOneofCase.Billboard
                                        or ComponentBodyPayload.PayloadOneofCase.Font
                                        // or ComponentBodyPayload.PayloadOneofCase.Gizmos
                                        // or ComponentBodyPayload.PayloadOneofCase.Material

                                        or ComponentBodyPayload.PayloadOneofCase.Texture
                                        or ComponentBodyPayload.PayloadOneofCase.Transform
                                        ? action.Payload.UpdateEntityComponent
                                    : new Protocol.EntityComponentCreateOrUpdate
                                    {
                                        entityId = action.Payload.UpdateEntityComponent.EntityId,
                                        classId = action.Payload.UpdateEntityComponent.ClassId,
                                        json = updateData.ToString(),
                                    };
                                crdtContext.SceneController.EnqueueSceneMessage(queuedMessage);
                            }
                            break;

                        case EntityActionPayload.PayloadOneofCase.ComponentUpdated:
                            var componentData = ComponentModelFromPayload(action.Payload.ComponentUpdated.ComponentData);
                            if (componentData != null) {
                                queuedMessage.method = MessagingTypes.SHARED_COMPONENT_UPDATE;
                                queuedMessage.payload =
                                    new Protocol.SharedComponentUpdate
                                    {
                                        componentId = action.Payload.ComponentUpdated.Id,
                                        json = componentData.ToString()
                                    };
                                crdtContext.SceneController.EnqueueSceneMessage(queuedMessage);
                            }
                            break;

                        // case EntityActionPayload.PayloadOneofCase.UpdateEntityComponent:
                        //     queuedMessage.method = MessagingTypes.PB_ENTITY_COMPONENT_CREATE_OR_UPDATE;
                        //     queuedMessage.payload = action.Payload.UpdateEntityComponent;
                        //     crdtContext.SceneController.EnqueueSceneMessage(queuedMessage);
                        //     break;

                        // case EntityActionPayload.PayloadOneofCase.ComponentUpdated:
                        //     queuedMessage.method = MessagingTypes.PB_SHARED_COMPONENT_UPDATE;
                        //     queuedMessage.payload = action.Payload.ComponentUpdated;
                        //     crdtContext.SceneController.EnqueueSceneMessage(queuedMessage);
                        //     break;
                    }

                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return defaultSendBatchResult;
        }

        private static object ComponentModelFromPayload(ComponentBodyPayload payload)
        {
            if (payload == null) return null;

            return payload.PayloadCase switch
                   {
                       ComponentBodyPayload.PayloadOneofCase.AvatarModifierArea => payload.AvatarModifierArea,
                       ComponentBodyPayload.PayloadOneofCase.Transform => payload.Transform,
                       ComponentBodyPayload.PayloadOneofCase.AttachToAvatar => payload.AttachToAvatar,
                       ComponentBodyPayload.PayloadOneofCase.Billboard => payload.Billboard,
                       ComponentBodyPayload.PayloadOneofCase.BoxShape => payload.BoxShape,
                       ComponentBodyPayload.PayloadOneofCase.SphereShape => payload.SphereShape,
                       ComponentBodyPayload.PayloadOneofCase.CircleShape => payload.CircleShape,
                       ComponentBodyPayload.PayloadOneofCase.PlaneShape => payload.PlaneShape,
                       ComponentBodyPayload.PayloadOneofCase.ConeShape => payload.ConeShape,
                       ComponentBodyPayload.PayloadOneofCase.CylinderShape => payload.CylinderShape,
                       ComponentBodyPayload.PayloadOneofCase.GltfShape => payload.GltfShape,
                       ComponentBodyPayload.PayloadOneofCase.NftShape => payload.NftShape,
                       ComponentBodyPayload.PayloadOneofCase.Texture => payload.Texture,
                       ComponentBodyPayload.PayloadOneofCase.Animator => payload.Animator,
                       ComponentBodyPayload.PayloadOneofCase.ObjShape => payload.ObjShape,
                       ComponentBodyPayload.PayloadOneofCase.Font => payload.Font,
                       ComponentBodyPayload.PayloadOneofCase.TextShape => payload.TextShape,
                       ComponentBodyPayload.PayloadOneofCase.Material => payload.Material,
                       ComponentBodyPayload.PayloadOneofCase.BasicMaterial => payload.BasicMaterial,
                       ComponentBodyPayload.PayloadOneofCase.UuidCallback => payload.UuidCallback,
                       ComponentBodyPayload.PayloadOneofCase.SmartItem => payload.SmartItem,
                       ComponentBodyPayload.PayloadOneofCase.VideoClip => payload.VideoClip,
                       ComponentBodyPayload.PayloadOneofCase.VideoTexture => payload.VideoTexture,
                       ComponentBodyPayload.PayloadOneofCase.CameraModeArea => payload.CameraModeArea,
                       ComponentBodyPayload.PayloadOneofCase.AvatarTexture => payload.AvatarTexture,
                       ComponentBodyPayload.PayloadOneofCase.AudioClip => payload.AudioClip,
                       ComponentBodyPayload.PayloadOneofCase.AudioSource => payload.AudioSource,
                       ComponentBodyPayload.PayloadOneofCase.AudioStream => payload.AudioStream,
                       ComponentBodyPayload.PayloadOneofCase.AvatarShape => payload.AvatarShape,
                       ComponentBodyPayload.PayloadOneofCase.Gizmos => payload.Gizmos,
                       ComponentBodyPayload.PayloadOneofCase.UiShape => payload.UiShape,
                       ComponentBodyPayload.PayloadOneofCase.UiContainerRect => payload.UiContainerRect,
                       ComponentBodyPayload.PayloadOneofCase.UiContainerStack => payload.UiContainerStack,
                       ComponentBodyPayload.PayloadOneofCase.UiButton => payload.UiButton,
                       ComponentBodyPayload.PayloadOneofCase.UiText => payload.UiText,
                       ComponentBodyPayload.PayloadOneofCase.UiInputText => payload.UiInputText,
                       ComponentBodyPayload.PayloadOneofCase.UiImage => payload.UiImage,
                       ComponentBodyPayload.PayloadOneofCase.UiScrollRect => payload.UiScrollRect,
                       _ => null,
                   };
        }
    }
}
