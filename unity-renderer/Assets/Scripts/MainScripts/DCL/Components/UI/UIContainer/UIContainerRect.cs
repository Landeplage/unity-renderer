using DCL.Helpers;
using DCL.Models;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Decentraland.Sdk.Ecs6;
using MainScripts.DCL.Components;

namespace DCL.Components
{
    public class UIContainerRect : UIShape<UIContainerRectReferencesContainer, UIContainerRect.Model>
    {
        [System.Serializable]
        public new class Model : UIShape.Model
        {
            public float thickness;
            public Color color = Color.clear;

            public override BaseModel GetDataFromJSON(string json) =>
                Utils.SafeFromJson<Model>(json);

            public override BaseModel GetDataFromPb(ComponentBodyPayload pbModel) =>
                pbModel.PayloadCase == ComponentBodyPayload.PayloadOneofCase.UiContainerRect
                    ? new Model
                    {
                        color = pbModel.UiContainerRect.Color.AsUnityColor(),
                        thickness = pbModel.UiContainerRect.Thickness,

                        name = pbModel.UiContainerRect.Name,
                        parentComponent = pbModel.UiContainerRect.ParentComponent,
                        visible = pbModel.UiContainerRect.Visible,
                        opacity = pbModel.UiContainerRect.Opacity,
                        hAlign = pbModel.UiContainerRect.HAlign,
                        vAlign = pbModel.UiContainerRect.VAlign,
                        width = pbModel.UiContainerRect.Width.AsUiValue(),
                        height = pbModel.UiContainerRect.Height.AsUiValue(),
                        positionX = pbModel.UiContainerRect.PositionX.AsUiValue(),
                        positionY = pbModel.UiContainerRect.PositionY.AsUiValue(),
                        isPointerBlocker = pbModel.UiContainerRect.IsPointerBlocker,
                    }
                    : Utils.SafeUnimplemented<UIContainerRect, Model>(expected: ComponentBodyPayload.PayloadOneofCase.UiContainerRect, actual: pbModel.PayloadCase);
        }

        public override string referencesContainerPrefabName => "UIContainerRect";

        public UIContainerRect() { model = new Model(); }

        public override int GetClassId() { return (int) CLASS_ID.UI_CONTAINER_RECT; }

        public override void AttachTo(IDCLEntity entity, System.Type overridenAttachedType = null)
        {
            Debug.LogError(
                "Aborted UIContainerRectShape attachment to an entity. UIShapes shouldn't be attached to entities.");
        }

        public override void DetachFrom(IDCLEntity entity, System.Type overridenAttachedType = null) { }

        public override IEnumerator ApplyChanges(BaseModel newModel)
        {
            referencesContainer.image.color = new Color(model.color.r, model.color.g, model.color.b, model.color.a);
            referencesContainer.image.raycastTarget = model.color.a >= RAYCAST_ALPHA_THRESHOLD;

            Outline outline = referencesContainer.image.GetComponent<Outline>();

            if (model.thickness > 0f)
            {
                if (outline == null)
                {
                    outline = referencesContainer.image.gameObject.AddComponent<Outline>();
                }

                outline.effectDistance = new Vector2(model.thickness, model.thickness);
            }
            else if (outline != null)
            {
                Object.DestroyImmediate(outline, false);
            }

            return null;
        }

        public override void Dispose()
        {
            if (referencesContainer != null)
                Utils.SafeDestroy(referencesContainer.gameObject);

            base.Dispose();
        }
    }
}
