using System;
using System.Collections.Generic;
using System.Linq;
using DCL.Components;
using DCL.Controllers;
using DCL.Helpers;
using DCL.Models;
using DCL.Rendering;
using Unity.Profiling;
using UnityEngine;

namespace DCL
{
    public class ECSComponentsManagerLegacy : IECSComponentsManagerLegacy
    {

        private readonly Dictionary<string, ISharedComponent> disposableComponents = new Dictionary<string, ISharedComponent>();

        private readonly Dictionary<long, Dictionary<Type, ISharedComponent>> entitiesSharedComponents =
            new Dictionary<long, Dictionary<Type, ISharedComponent>>();

        private readonly Dictionary<long, Dictionary<CLASS_ID_COMPONENT, IEntityComponent>> entitiesComponents =
            new Dictionary<long, Dictionary<CLASS_ID_COMPONENT, IEntityComponent>>();

        private readonly IParcelScene scene;
        private readonly IRuntimeComponentFactory componentFactory;
        private readonly IParcelScenesCleaner parcelScenesCleaner;
        private readonly ISceneBoundsChecker sceneBoundsChecker;
        private readonly IPhysicsSyncController physicsSyncController;
        private readonly ICullingController cullingController;
        private readonly DataStore_FeatureFlag dataStoreFeatureFlag;
        private readonly bool isSBCNerfEnabled;

        public event Action<string, ISharedComponent> OnAddSharedComponent;

        public ECSComponentsManagerLegacy(IParcelScene scene)
            : this(scene,
                Environment.i.world.componentFactory,
                Environment.i.platform.parcelScenesCleaner,
                Environment.i.world.sceneBoundsChecker,
                Environment.i.platform.physicsSyncController,
                Environment.i.platform.cullingController,
                DataStore.i.featureFlags.flags.Get()) { }

        public ECSComponentsManagerLegacy(IParcelScene scene,
            IRuntimeComponentFactory componentFactory,
            IParcelScenesCleaner parcelScenesCleaner,
            ISceneBoundsChecker sceneBoundsChecker,
            IPhysicsSyncController physicsSyncController,
            ICullingController cullingController,
            FeatureFlag featureFlags)
        {
            this.scene = scene;
            this.componentFactory = componentFactory;
            this.parcelScenesCleaner = parcelScenesCleaner;
            this.sceneBoundsChecker = sceneBoundsChecker;
            this.physicsSyncController = physicsSyncController;
            this.cullingController = cullingController;

            isSBCNerfEnabled = featureFlags.IsFeatureEnabled("NERF_SBC");
        }

        static readonly ProfilerMarker k_AddSharedComponent = new ("ECSComponentsManagerLegacy_AddSharedComponent");

        public void AddSharedComponent(IDCLEntity entity, Type componentType, ISharedComponent component)
        {
            k_AddSharedComponent.Begin();
            if (component == null)
            {            k_AddSharedComponent.End();

                return;
            }

            RemoveSharedComponent(entity, componentType);

            if (!entitiesSharedComponents.TryGetValue(entity.entityId, out Dictionary<Type, ISharedComponent> entityComponents))
            {
                entityComponents = new Dictionary<Type, ISharedComponent>();
                entitiesSharedComponents.Add(entity.entityId, entityComponents);
            }

            entityComponents.Add(componentType, component);
            k_AddSharedComponent.End();
        }

        static readonly ProfilerMarker k_RemoveSharedComponent = new ("ECSComponentsManagerLegacy_RemoveSharedComponent");

        public void RemoveSharedComponent(IDCLEntity entity, Type targetType, bool triggerDetaching = true)
        {
            k_RemoveSharedComponent.Begin();
            if (!entitiesSharedComponents.TryGetValue(entity.entityId, out Dictionary<Type, ISharedComponent> entityComponents))
            {
                k_RemoveSharedComponent.End();
                return;
            }
            if (!entityComponents.TryGetValue(targetType, out ISharedComponent component))
            {
                k_RemoveSharedComponent.End();
                return;
            }

            if (component == null)
            {
                k_RemoveSharedComponent.End();
                return;
            }

            entityComponents.Remove(targetType);

            if (entityComponents.Count == 0)
            {
                entitiesSharedComponents.Remove(entity.entityId);
            }

            if (triggerDetaching)
                component.DetachFrom(entity, targetType);

            k_RemoveSharedComponent.End();
        }

        static readonly ProfilerMarker k_TryGetComponent = new ("ECSComponentsManagerLegacy_TryGetComponent");

        public T TryGetComponent<T>(IDCLEntity entity) where T : class
        {
            k_TryGetComponent.Begin();
            T component = null;
            if (entitiesComponents.TryGetValue(entity.entityId, out Dictionary<CLASS_ID_COMPONENT, IEntityComponent> entityComponents))
            {
                component = entityComponents.Values.FirstOrDefault(x => x is T) as T;

                if (component != null)
                {
                    k_TryGetComponent.End();
                    return component;
                }
            }

            if (entitiesSharedComponents.TryGetValue(entity.entityId, out Dictionary<Type, ISharedComponent> entitySharedComponents))
            {
                component = entitySharedComponents.Values.FirstOrDefault(x => x is T) as T;

                if (component != null)
                {
                    k_TryGetComponent.End();
                    return component;
                }
            }

            k_TryGetComponent.End();
            return null;
        }

        static readonly ProfilerMarker k_TryGetBaseComponent = new ("ECSComponentsManagerLegacy_TryGetBaseComponent");

        public bool TryGetBaseComponent(IDCLEntity entity, CLASS_ID_COMPONENT componentId, out IEntityComponent component)
        {
            k_TryGetBaseComponent.Begin();
            if (entitiesComponents.TryGetValue(entity.entityId, out Dictionary<CLASS_ID_COMPONENT, IEntityComponent> entityComponents))
            {
                k_TryGetBaseComponent.End();
                return entityComponents.TryGetValue(componentId, out component);
            }
            component = null;

            k_TryGetBaseComponent.End();

            return false;
        }

        static readonly ProfilerMarker k_TryGetSharedComponent = new ("ECSComponentsManagerLegacy_TryGetSharedComponent");
        public bool TryGetSharedComponent(IDCLEntity entity, CLASS_ID componentId, out ISharedComponent component)
        {
            k_TryGetSharedComponent.Begin();
            component = null;
            if (!entitiesSharedComponents.TryGetValue(entity.entityId, out Dictionary<Type, ISharedComponent> entityComponents))
            {
                k_TryGetSharedComponent.End();

                return false;
            }

            using (var iterator = entityComponents.GetEnumerator())
            {
                while (iterator.MoveNext())
                {
                    if (iterator.Current.Value.GetClassId() != (int)componentId)
                        continue;

                    component = iterator.Current.Value;
                    break;
                }
            }

            k_TryGetSharedComponent.End();

            return component != null;
        }

        static readonly ProfilerMarker k_GetSharedComponent = new ("ECSComponentsManagerLegacy_GetSharedComponent");

        public ISharedComponent GetSharedComponent(IDCLEntity entity, Type targetType)
        {
            k_GetSharedComponent.Begin();
            if (!entitiesSharedComponents.TryGetValue(entity.entityId, out Dictionary<Type, ISharedComponent> entityComponents))
            {
                k_GetSharedComponent.End();

                return null;
            }

            if (entityComponents.TryGetValue(targetType, out ISharedComponent component))
            {
                k_GetSharedComponent.End();

                return component;
            }
            k_GetSharedComponent.End();

            return null;
        }

        static readonly ProfilerMarker k_HasComponent = new ("ECSComponentsManagerLegacy_HasComponent");

        public bool HasComponent(IDCLEntity entity, CLASS_ID_COMPONENT componentId)
        {
            k_HasComponent.Begin();
            if (entitiesComponents.TryGetValue(entity.entityId, out Dictionary<CLASS_ID_COMPONENT, IEntityComponent> entityComponents))
            {
                k_HasComponent.End();

                return entityComponents.ContainsKey(componentId);
            }
            k_HasComponent.End();

            return false;
        }

        static readonly ProfilerMarker k_HasSharedComponent = new ("ECSComponentsManagerLegacy_HasSharedComponent");

        public bool HasSharedComponent(IDCLEntity entity, CLASS_ID componentId)
        {
            k_HasSharedComponent.Begin();
            if (TryGetSharedComponent(entity, componentId, out ISharedComponent component))
            {            k_HasSharedComponent.End();

                return component != null;
            }
            k_HasSharedComponent.End();

            return false;
        }

        static readonly ProfilerMarker k_RemoveComponent = new ("ECSComponentsManagerLegacy_RemoveComponent");

        public void RemoveComponent(IDCLEntity entity, CLASS_ID_COMPONENT componentId)
        {
            k_RemoveComponent.Begin();
            if (entitiesComponents.TryGetValue(entity.entityId, out Dictionary<CLASS_ID_COMPONENT, IEntityComponent> entityComponents))
            {
                entityComponents.Remove(componentId);

                if (entityComponents.Count == 0)
                {
                    entitiesComponents.Remove(entity.entityId);
                }
            }
            k_RemoveComponent.End();
        }

        static readonly ProfilerMarker k_AddComponent = new ("ECSComponentsManagerLegacy_AddComponent");

        public void AddComponent(IDCLEntity entity, CLASS_ID_COMPONENT componentId, IEntityComponent component)
        {
            k_AddComponent.Begin();
            if (!entitiesComponents.TryGetValue(entity.entityId, out Dictionary<CLASS_ID_COMPONENT, IEntityComponent> entityComponents))
            {
                entityComponents = new Dictionary<CLASS_ID_COMPONENT, IEntityComponent>();
                entitiesComponents.Add(entity.entityId, entityComponents);

                entity.OnBaseComponentAdded?.Invoke(componentId, entity);
            }
            entityComponents.Add(componentId, component);
            k_AddComponent.End();
        }

        static readonly ProfilerMarker k_GetComponentsById = new ("ECSComponentsManagerLegacy_GetComponentsById");

        public IEntityComponent GetComponent(IDCLEntity entity, CLASS_ID_COMPONENT componentId)
        {
            k_GetComponentsById.Begin();
            if (!entitiesComponents.TryGetValue(entity.entityId, out Dictionary<CLASS_ID_COMPONENT, IEntityComponent> entityComponents))
            {            k_GetComponentsById.End();

                return null;
            }
            if (entityComponents.TryGetValue(componentId, out IEntityComponent component))
            {            k_GetComponentsById.End();

                return component;
            }

            k_GetComponentsById.End();

            return null;
        }

        static readonly ProfilerMarker k_GetComponents = new ("ECSComponentsManagerLegacy_GetComponents");

        public IEnumerator<IEntityComponent> GetComponents(IDCLEntity entity)
        {
            k_GetComponents.Begin();
            if (!entitiesComponents.TryGetValue(entity.entityId, out Dictionary<CLASS_ID_COMPONENT, IEntityComponent> entityComponents))
            {
                yield break;
            }

            using (var iterator = entityComponents.GetEnumerator())
            {
                while (iterator.MoveNext())
                {
                    yield return iterator.Current.Value;
                }
            }

            k_GetComponents.End();
        }

        static readonly ProfilerMarker k_GetSharedComponents = new ("ECSComponentsManagerLegacy_GetSharedComponents");

        public IEnumerator<ISharedComponent> GetSharedComponents(IDCLEntity entity)
        {
            k_GetSharedComponents.Begin();
            if (!entitiesSharedComponents.TryGetValue(entity.entityId, out Dictionary<Type, ISharedComponent> entityComponents))
            {
                yield break;
            }

            using (var iterator = entityComponents.GetEnumerator())
            {
                while (iterator.MoveNext())
                {
                    yield return iterator.Current.Value;
                }
            }

            k_GetSharedComponents.End();
        }

        static readonly ProfilerMarker k_GetComponentsDictionary = new ("ECSComponentsManagerLegacy_GetComponentsDictionary");

        public IReadOnlyDictionary<CLASS_ID_COMPONENT, IEntityComponent> GetComponentsDictionary(IDCLEntity entity)
        {
            k_GetComponentsDictionary.Begin();
            entitiesComponents.TryGetValue(entity.entityId, out Dictionary<CLASS_ID_COMPONENT, IEntityComponent> entityComponents);
            k_GetComponentsDictionary.End();

            return entityComponents;
        }

        static readonly ProfilerMarker k_GetSharedComponentsDictionary = new ("ECSComponentsManagerLegacy_GetSharedComponentsDictionary");

        public IReadOnlyDictionary<Type, ISharedComponent> GetSharedComponentsDictionary(IDCLEntity entity)
        {
            k_GetSharedComponentsDictionary.Begin();
            entitiesSharedComponents.TryGetValue(entity.entityId, out Dictionary<Type, ISharedComponent> entityComponents);
            k_GetSharedComponentsDictionary.End();

            return entityComponents;
        }

        public int GetComponentsCount()
        {
            int count = 0;
            using (var entityIterator = entitiesComponents.GetEnumerator())
            {
                while (entityIterator.MoveNext())
                {
                    count += entityIterator.Current.Value.Count;
                }
            }
            return count;
        }

        static readonly ProfilerMarker k_CleanComponents = new ("ECSComponentsManagerLegacy_CleanComponents");
        public void CleanComponents(IDCLEntity entity)
        {
            k_CleanComponents.Begin();
            if (!entitiesComponents.TryGetValue(entity.entityId, out Dictionary<CLASS_ID_COMPONENT, IEntityComponent> entityComponents))
            {
                return;
            }

            using (var iterator = entityComponents.GetEnumerator())
            {
                while (iterator.MoveNext())
                {
                    if (iterator.Current.Value == null)
                        continue;

                    if (iterator.Current.Value is ICleanable cleanableComponent)
                        cleanableComponent.Cleanup();

                    if (!(iterator.Current.Value is IPoolableObjectContainer poolableContainer))
                        continue;

                    if (poolableContainer.poolableObject == null)
                        continue;

                    poolableContainer.poolableObject.Release();
                }
            }
            entitiesComponents.Remove(entity.entityId);

            k_CleanComponents.End();
        }

        public bool HasSceneSharedComponent(string component)
        {
            return disposableComponents.ContainsKey(component);
        }

        static readonly ProfilerMarker k_GetSceneSharedComponent = new ("ECSComponentsManagerLegacy_GetSceneSharedComponent");

        public ISharedComponent GetSceneSharedComponent(string component)
        {
            k_GetSceneSharedComponent.Begin();
            disposableComponents.TryGetValue(component, out ISharedComponent sharedComponent);
            k_GetSceneSharedComponent.End();
            return sharedComponent;
        }


        public bool TryGetSceneSharedComponent(string component, out ISharedComponent sharedComponent)
        {
            return disposableComponents.TryGetValue(component, out sharedComponent);
        }

        public IReadOnlyDictionary<string, ISharedComponent> GetSceneSharedComponentsDictionary()
        {
            return disposableComponents;
        }

        public int GetSceneSharedComponentsCount()
        {
            return disposableComponents.Count;
        }

        static readonly ProfilerMarker k_AddSceneSharedComponent = new ("ECSComponentsManagerLegacy_AddSceneSharedComponent");
        public void AddSceneSharedComponent(string component, ISharedComponent sharedComponent)
        {
            k_AddSceneSharedComponent.Begin();
            disposableComponents.Add(component, sharedComponent);
            OnAddSharedComponent?.Invoke(component, sharedComponent);
            k_AddSceneSharedComponent.End();
        }

        public bool RemoveSceneSharedComponent(string component)
        {
            return disposableComponents.Remove(component);
        }

        static readonly ProfilerMarker k_SceneSharedComponentCreate = new ("ECSComponentsManagerLegacy_SceneSharedComponentCreate");

        public ISharedComponent SceneSharedComponentCreate(string id, int classId)
        {
            k_SceneSharedComponentCreate.Begin();

            if (disposableComponents.TryGetValue(id, out ISharedComponent component))
            {
                k_SceneSharedComponentCreate.End();

                return component;
            }

            IRuntimeComponentFactory factory = componentFactory;

            if (factory.createConditions.ContainsKey(classId))
            {
                if (!factory.createConditions[(int)classId].Invoke(scene.sceneData.sceneNumber, classId))
                {
                    k_SceneSharedComponentCreate.End();

                    return null;
                }
            }

            ISharedComponent newComponent = componentFactory.CreateComponent(classId) as ISharedComponent;

            if (newComponent == null)
            {
                k_SceneSharedComponentCreate.End();

                return null;
            }

            AddSceneSharedComponent(id, newComponent);

            newComponent.Initialize(scene, id);

            k_SceneSharedComponentCreate.End();
            return newComponent;
        }

        public T GetSceneSharedComponent<T>() where T : class
        {
            return disposableComponents.Values.FirstOrDefault(x => x is T) as T;
        }

        /**
          * This method is called when we need to attach a disposable component to the entity
          */

        static readonly ProfilerMarker k_SceneSharedComponentAttach = new ("ECSComponentsManagerLegacy_SceneSharedComponentAttach");
        public void SceneSharedComponentAttach(long entityId, string componentId)
        {
            k_SceneSharedComponentAttach.Begin();
            IDCLEntity entity = scene.GetEntityById(entityId);

            if (entity == null)
            {            k_SceneSharedComponentAttach.End();

                return;
            }

            if (disposableComponents.TryGetValue(componentId, out ISharedComponent sharedComponent))
            {
                sharedComponent.AttachTo(entity);
            }
            k_SceneSharedComponentAttach.End();
        }
        static readonly ProfilerMarker k_EntityComponentCreateOrUpdate = new ("ECSComponentsManagerLegacy_EntityComponentCreateOrUpdate");

        public IEntityComponent EntityComponentCreateOrUpdate(long entityId, CLASS_ID_COMPONENT classId, object data)
        {
            k_EntityComponentCreateOrUpdate.Begin();
            IDCLEntity entity = scene.GetEntityById(entityId);

            if (entity == null)
            {
                k_EntityComponentCreateOrUpdate.End();

                Debug.LogError($"scene '{scene.sceneData.sceneNumber}': Can't create entity component if the entity {entityId} doesn't exist!");
                return null;
            }

            IEntityComponent targetComponent = null;

            var overrideCreate = Environment.i.world.componentFactory.createOverrides;

            if (overrideCreate.ContainsKey((int)classId))
            {
                int classIdAsInt = (int)classId;
                overrideCreate[(int)classId].Invoke(scene.sceneData.sceneNumber, entityId, ref classIdAsInt, data);
                classId = (CLASS_ID_COMPONENT)classIdAsInt;
            }

            bool wasCreated = false;
            if (!HasComponent(entity, classId))
            {
                targetComponent = componentFactory.CreateComponent((int) classId) as IEntityComponent;

                if (targetComponent != null)
                {
                    AddComponent(entity, classId, targetComponent);

                    targetComponent.Initialize(scene, entity);

                    if (data is string json)
                        targetComponent.UpdateFromJSON(json);
                    else
                        targetComponent.UpdateFromModel(data as BaseModel);

                    wasCreated = true;
                }
            }
            else
            {
                targetComponent = EntityComponentUpdate(entity, classId, data as string);
            }

            var isTransform = classId == CLASS_ID_COMPONENT.TRANSFORM;
            var avoidThrottling = isSBCNerfEnabled ? isTransform && wasCreated : isTransform;

            if (targetComponent != null && targetComponent is IOutOfSceneBoundariesHandler)
                sceneBoundsChecker?.AddEntityToBeChecked(entity, runPreliminaryEvaluation: avoidThrottling);

            physicsSyncController.MarkDirty();
            cullingController.MarkDirty();
            k_EntityComponentCreateOrUpdate.End();

            return targetComponent;
        }

        static readonly ProfilerMarker k_EntityComponentUpdate = new ("ECSComponentsManagerLegacy_EntityComponentUpdate");

        public IEntityComponent EntityComponentUpdate(IDCLEntity entity, CLASS_ID_COMPONENT classId,
            string componentJson)
        {
            k_EntityComponentUpdate.Begin();
            if (entity == null)
            {
                k_EntityComponentUpdate.End();

                Debug.LogError($"Can't update the {classId} component of a nonexistent entity!", scene.GetSceneTransform());
                return null;
            }

            if (!HasComponent(entity, classId))
            {
                k_EntityComponentUpdate.End();

                Debug.LogError($"Entity {entity.entityId} doesn't have a {classId} component to update!", scene.GetSceneTransform());
                return null;
            }

            var targetComponent = GetComponent(entity, classId);
            targetComponent.UpdateFromJSON(componentJson);

            k_EntityComponentUpdate.End();
            return targetComponent;
        }

        static readonly ProfilerMarker k_SceneSharedComponentDispose = new ("ECSComponentsManagerLegacy_SceneSharedComponentDispose");

        public void SceneSharedComponentDispose(string id)
        {
            k_SceneSharedComponentDispose.Begin();
            if (disposableComponents.TryGetValue(id, out ISharedComponent sharedComponent))
            {
                sharedComponent?.Dispose();
                disposableComponents.Remove(id);
            }
            k_SceneSharedComponentDispose.End();
        }

        static readonly ProfilerMarker k_SceneSharedComponentUpdate = new ("ECSComponentsManagerLegacy_SceneSharedComponentUpdate");

        public ISharedComponent SceneSharedComponentUpdate(string id, BaseModel model)
        {
            k_SceneSharedComponentUpdate.Begin();
            if (disposableComponents.TryGetValue(id, out ISharedComponent sharedComponent))
            {
                k_SceneSharedComponentUpdate.End();

                sharedComponent.UpdateFromModel(model);
                return sharedComponent;
            }

            k_SceneSharedComponentUpdate.End();
            return null;
        }


        public ISharedComponent SceneSharedComponentUpdate(string id, string json)
        {
            k_SceneSharedComponentUpdate.Begin();
            if (disposableComponents.TryGetValue(id, out ISharedComponent disposableComponent))
            {
                disposableComponent.UpdateFromJSON(json);
                k_SceneSharedComponentUpdate.End();

                return disposableComponent;
            }

            k_SceneSharedComponentUpdate.End();
            return null;
        }

        static readonly ProfilerMarker k_EntityComponentRemove = new ("ECSComponentsManagerLegacy_EntityComponentRemove");

        public void EntityComponentRemove(long entityId, string componentName)
        {
            k_EntityComponentRemove.Begin();
            IDCLEntity entity = scene.GetEntityById(entityId);

            if (entity == null)
            {            k_EntityComponentRemove.End();

                return;
            }

            switch (componentName)
            {
                case "shape":
                    if (entity.meshesInfo.currentShape is BaseShape baseShape)
                    {
                        baseShape.DetachFrom(entity);
                    }
                    k_EntityComponentRemove.End();

                    return;

                case ComponentNameLiterals.OnClick:
                    {
                        if (TryGetBaseComponent(entity, CLASS_ID_COMPONENT.UUID_ON_CLICK, out IEntityComponent component))
                        {
                            Utils.SafeDestroy(component.GetTransform().gameObject);
                            RemoveComponent(entity, CLASS_ID_COMPONENT.UUID_ON_CLICK);
                        }
                        k_EntityComponentRemove.End();

                        return;
                    }
                case ComponentNameLiterals.OnPointerDown:
                    {
                        if (TryGetBaseComponent(entity, CLASS_ID_COMPONENT.UUID_ON_DOWN, out IEntityComponent component))
                        {
                            Utils.SafeDestroy(component.GetTransform().gameObject);
                            RemoveComponent(entity, CLASS_ID_COMPONENT.UUID_ON_DOWN);
                        }
                    }            k_EntityComponentRemove.End();

                    return;
                case ComponentNameLiterals.OnPointerUp:
                    {
                        if (TryGetBaseComponent(entity, CLASS_ID_COMPONENT.UUID_ON_UP, out IEntityComponent component))
                        {
                            Utils.SafeDestroy(component.GetTransform().gameObject);
                            RemoveComponent(entity, CLASS_ID_COMPONENT.UUID_ON_UP);
                        }
                    }            k_EntityComponentRemove.End();

                    return;
                case ComponentNameLiterals.OnPointerHoverEnter:
                    {
                        if (TryGetBaseComponent(entity, CLASS_ID_COMPONENT.UUID_ON_HOVER_ENTER, out IEntityComponent component))
                        {
                            Utils.SafeDestroy(component.GetTransform().gameObject);
                            RemoveComponent(entity, CLASS_ID_COMPONENT.UUID_ON_HOVER_ENTER);
                        }
                    }            k_EntityComponentRemove.End();

                    return;
                case ComponentNameLiterals.OnPointerHoverExit:
                    {
                        if (TryGetBaseComponent(entity, CLASS_ID_COMPONENT.UUID_ON_HOVER_EXIT, out IEntityComponent component))
                        {
                            Utils.SafeDestroy(component.GetTransform().gameObject);
                            RemoveComponent(entity, CLASS_ID_COMPONENT.UUID_ON_HOVER_EXIT);
                        }
                    }            k_EntityComponentRemove.End();

                    return;
                case "transform":
                    {
                        if (TryGetBaseComponent(entity, CLASS_ID_COMPONENT.AVATAR_ATTACH, out IEntityComponent component))
                        {
                            component.Cleanup();
                            RemoveComponent(entity, CLASS_ID_COMPONENT.AVATAR_ATTACH);
                        }
                    }            k_EntityComponentRemove.End();

                    return;

                default:
                    {
                        IEntityComponent component = GetComponentsDictionary(entity).FirstOrDefault(kp => kp.Value.componentName == componentName).Value;
                        if (component == null)
                            break;

                        RemoveComponent(entity, (CLASS_ID_COMPONENT)component.GetClassId());

                        if (component is ICleanable cleanableComponent)
                            cleanableComponent.Cleanup();

                        bool released = false;
                        if (component is IPoolableObjectContainer poolableContainer)
                        {
                            if (poolableContainer.poolableObject != null)
                            {
                                poolableContainer.poolableObject.Release();
                                released = true;
                            }
                        }
                        if (!released)
                        {
                            Utils.SafeDestroy(component.GetTransform()?.gameObject);
                        }
                        break;
                    }
            }
            k_EntityComponentRemove.End();
        }

        static readonly ProfilerMarker k_DisposeAllSceneComponents = new ("ECSComponentsManagerLegacy_DisposeAllSceneComponents");

        public void DisposeAllSceneComponents()
        {
            k_DisposeAllSceneComponents.Begin();
            List<string> allDisposableComponents = disposableComponents.Select(x => x.Key).ToList();
            foreach (string id in allDisposableComponents)
            {
                parcelScenesCleaner.MarkDisposableComponentForCleanup(scene, id);
            }
            k_DisposeAllSceneComponents.End();
        }
    }
}
