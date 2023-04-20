using System;
using System.Collections;
using DCL.Controllers;
using DCL.Helpers;
using DCL.Models;
using DCLPlugins.UUIDEventComponentsPlugin.UUIDComponent.Interfaces;
using Decentraland.Sdk.Ecs6;

namespace DCL.Components
{
    public class OnPointerHoverEvent : UUIDComponent, IPointerEvent
    {
        [Serializable]
        public new class Model : UUIDComponent.Model
        {
            public float distance = 10f;

            public override BaseModel GetDataFromJSON(string json)
            {
                return Utils.SafeFromJson<Model>(json);
            }

            public override BaseModel GetDataFromPb(ComponentBodyPayload pbModel)
            {

                return pbModel.PayloadCase == ComponentBodyPayload.PayloadOneofCase.UuidCallback
                    ? new Model
                    {
                        // uuid = ??
                        type = pbModel.UuidCallback.Type,
                        distance = pbModel.UuidCallback.Distance,
                    }
                    : Utils.SafeUnimplemented<OnPointerEvent, Model>(expected: ComponentBodyPayload.PayloadOneofCase.UuidCallback, actual: pbModel.PayloadCase);
            }
        }

        internal OnPointerEventColliders pointerEventColliders;

        protected override string uuidComponentName { get; }

        public override void Initialize(IParcelScene scene, IDCLEntity entity)
        {
            base.Initialize(scene, entity);

            if (model == null)
                model = new Model();

            pointerEventColliders = new OnPointerEventColliders();
            SetEventColliders(entity);

            entity.OnShapeUpdated -= SetEventColliders;
            entity.OnShapeUpdated += SetEventColliders;
        }

        public virtual void SetHoverState(bool hoverState)
        {
        }

        void SetEventColliders(IDCLEntity entity)
        {
            pointerEventColliders.Initialize(entity);
        }

        public bool IsVisible()
        {
            if (entity == null)
                return false;

            bool isVisible = false;

            if (entity.meshesInfo != null &&
                entity.meshesInfo.renderers != null &&
                entity.meshesInfo.renderers.Length > 0)
            {
                isVisible = entity.meshesInfo.renderers[0].enabled;
            }

            return isVisible;
        }

        public bool IsAtHoverDistance(float distance)
        {
            Model model = this.model as Model;
            return distance <= model.distance;
        }

        public override IEnumerator ApplyChanges(BaseModel newModel)
        {
            this.model = newModel ?? new Model();
            return null;
        }

        void OnDestroy()
        {
            if (entity != null)
                entity.OnShapeUpdated -= SetEventColliders;

            pointerEventColliders.Dispose();
        }
    }
}
