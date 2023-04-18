﻿using System.Collections;
using DCL.Controllers;
using DCL.Helpers;
using DCL.Models;
using UnityEngine;
using Decentraland.Sdk.Ecs6;

namespace DCL.Components
{
    public class DCLTransform : IEntityComponent, IOutOfSceneBoundariesHandler
    {
        [System.Serializable]
        public class Model : BaseModel
        {
            public Vector3 position = Vector3.zero;
            public Quaternion rotation = Quaternion.identity;
            public Vector3 scale = Vector3.one;

            public override BaseModel GetDataFromJSON(string json)
            {
                DCLTransformUtils.DecodeTransform(json, ref DCLTransform.model);
                return DCLTransform.model;
            }

            
            public override BaseModel GetDataFromPb(ComponentBodyPayload pbModel) {
                if (pbModel.PayloadCase == ComponentBodyPayload.PayloadOneofCase.Transform) {
                    DCLTransform.model.position.x = pbModel.Transform.Position.X;
                    DCLTransform.model.position.y = pbModel.Transform.Position.Y;
                    DCLTransform.model.position.z = pbModel.Transform.Position.Z;
                    DCLTransform.model.scale.x = pbModel.Transform.Scale.X;
                    DCLTransform.model.scale.y = pbModel.Transform.Scale.Y;
                    DCLTransform.model.scale.z = pbModel.Transform.Scale.Z;
                    DCLTransform.model.rotation.x = pbModel.Transform.Rotation.X;
                    DCLTransform.model.rotation.y = pbModel.Transform.Rotation.Y;
                    DCLTransform.model.rotation.z = pbModel.Transform.Rotation.Z;
                    DCLTransform.model.rotation.w = pbModel.Transform.Rotation.W;
                }
                return DCLTransform.model;
            }

        }
        
        public static Model model = new Model();

        public void Cleanup() { }

        public string componentName { get; } = "Transform";
        public IParcelScene scene { get; private set; }
        public IDCLEntity entity { get; private set; }
        public Transform GetTransform() => null;

        public void Initialize(IParcelScene scene, IDCLEntity entity)
        {
            this.scene = scene;
            this.entity = entity;
        }   

        public void UpdateFromJSON(string json)
        {
            model.GetDataFromJSON(json);
            UpdateFromModel(model);
        }

        
        public void UpdateFromPb(object payload)
        {
            model.GetDataFromPb(payload as ComponentBodyPayload);
            UpdateFromModel(model);
        }

        public void UpdateFromModel(BaseModel model)
        {
            DCLTransform.model = model as Model;

            // AvatarShape interpolation hack: we don't apply avatars position and rotation directly to the transform
            // and those values are used for the interpolation.
            if (entity.OnTransformChange != null)
            {
                entity.OnTransformChange.Invoke(DCLTransform.model);
            }
            else
            {
                entity.gameObject.transform.localPosition = DCLTransform.model.position;
                entity.gameObject.transform.localRotation = DCLTransform.model.rotation;
            }
            
            entity.gameObject.transform.localScale = DCLTransform.model.scale;
            entity.gameObject.transform.CapGlobalValuesToMax();
        }

        public IEnumerator ApplyChanges(BaseModel model) { return null; }

        public void RaiseOnAppliedChanges() { }

        public bool IsValid() => true;
        public BaseModel GetModel() => DCLTransform.model;
        public int GetClassId() => (int) CLASS_ID_COMPONENT.TRANSFORM;
        public void UpdateOutOfBoundariesState(bool enable) { }
        
        
    }
}