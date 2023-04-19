﻿using System;
using System.Collections;
using DCL.Controllers;
using DCL.Helpers;
using DCL.Models;
using UnityEngine;
using Decentraland.Sdk.Ecs6;

namespace DCL.Components
{
    public class AvatarAttachComponent : IEntityComponent
    {
        [Serializable]
        public class Model : BaseModel
        {
            public string avatarId = null;
            public int anchorPointId = 0;

            public override BaseModel GetDataFromJSON(string json) =>
                Utils.SafeFromJson<Model>(json);

            public override BaseModel GetDataFromPb(ComponentBodyPayload pbModel) {
                return Utils.SafeUnimplemented<Model>();
            }

        }

        IParcelScene IComponent.scene => handler.scene;
        IDCLEntity IEntityComponent.entity => handler.entity;

        string IComponent.componentName => "AvatarAttach";

        private readonly AvatarAttachHandler handler = new AvatarAttachHandler();

        void IEntityComponent.Initialize(IParcelScene scene, IDCLEntity entity)
        {
            handler.Initialize(scene, entity, Environment.i.platform.updateEventHandler);
        }

        bool IComponent.IsValid() => true;

        BaseModel IComponent.GetModel() => handler.model;

        int IComponent.GetClassId() => (int)CLASS_ID_COMPONENT.AVATAR_ATTACH;

        void IComponent.UpdateFromPb(object payload)
        {
            handler.OnModelUpdated(handler.model.GetDataFromPb(payload as ComponentBodyPayload) as Model);
        }

        void IComponent.UpdateFromJSON(string json)
        {
            handler.OnModelUpdated(json);
        }

        void IComponent.UpdateFromModel(BaseModel newModel)
        {
            handler.OnModelUpdated(newModel as Model);
        }

        IEnumerator IComponent.ApplyChanges(BaseModel newModel)
        {
            yield break;
        }

        void IComponent.RaiseOnAppliedChanges() { }

        Transform IMonoBehaviour.GetTransform() => null;

        void ICleanable.Cleanup() => handler.Dispose();
    }
}
