using DCL.Helpers;
using DCL.Interface;
using DCL.Models;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;
using Decentraland.Sdk.Ecs6;
using MainScripts.DCL.Components;

namespace DCL.Components
{
    public class UIScrollRect : UIShape<UIScrollRectRefContainer, UIScrollRect.Model>
    {
        [System.Serializable]
        public new class Model : UIShape.Model
        {
            public float valueX = 0;
            public float valueY = 0;
            public Color backgroundColor = Color.clear;
            public bool isHorizontal = false;
            public bool isVertical = true;
            public float paddingTop = 0f;
            public float paddingRight = 0f;
            public float paddingBottom = 0f;
            public float paddingLeft = 0f;
            public string OnChanged;

            public override BaseModel GetDataFromJSON(string json) =>
                Utils.SafeFromJson<Model>(json);

            public override BaseModel GetDataFromPb(ComponentBodyPayload pbModel) =>
                pbModel.PayloadCase == ComponentBodyPayload.PayloadOneofCase.UiScrollRect
                    ? new Model
                    {
                        valueX = pbModel.UiScrollRect.ValueX,
                        valueY = pbModel.UiScrollRect.ValueY,
                        backgroundColor = pbModel.UiScrollRect.BackgroundColor.AsUnityColor(),
                        isHorizontal = pbModel.UiScrollRect.IsHorizontal,
                        isVertical = pbModel.UiScrollRect.IsVertical,
                        paddingTop = pbModel.UiScrollRect.PaddingTop,
                        paddingRight = pbModel.UiScrollRect.PaddingRight,
                        paddingBottom = pbModel.UiScrollRect.PaddingBottom,
                        paddingLeft = pbModel.UiScrollRect.PaddingLeft,
                        OnChanged = pbModel.UiScrollRect.OnChanged,

                        name = pbModel.UiScrollRect.Name,
                        parentComponent = pbModel.UiScrollRect.ParentComponent,
                        visible = pbModel.UiScrollRect.Visible,
                        opacity = pbModel.UiScrollRect.Opacity,
                        hAlign = pbModel.UiScrollRect.HAlign,
                        vAlign = pbModel.UiScrollRect.VAlign,
                        width = pbModel.UiScrollRect.Width.AsUiValue(),
                        height = pbModel.UiScrollRect.Height.AsUiValue(),
                        positionX = pbModel.UiScrollRect.PositionX.AsUiValue(),
                        positionY = pbModel.UiScrollRect.PositionY.AsUiValue(),
                        isPointerBlocker = pbModel.UiScrollRect.IsPointerBlocker,
                    }
                    : Utils.SafeUnimplemented<UIScrollRect, Model>(expected: ComponentBodyPayload.PayloadOneofCase.UiScrollRect, actual: pbModel.PayloadCase);
        }

        public override string referencesContainerPrefabName => "UIScrollRect";

        public UIScrollRect() { model = new Model(); }

        public override void AttachTo(IDCLEntity entity, System.Type overridenAttachedType = null)
        {
            Debug.LogError(
                "Aborted UIScrollRectShape attachment to an entity. UIShapes shouldn't be attached to entities.");
        }

        public override void DetachFrom(IDCLEntity entity, System.Type overridenAttachedType = null) { }

        public override void OnChildAttached(UIShape parent, UIShape childComponent)
        {
            base.OnChildAttached(parent, childComponent);
            childComponent.OnAppliedChanges -= RefreshContainerForShape;
            childComponent.OnAppliedChanges += RefreshContainerForShape;
            RefreshContainerForShape(childComponent);
        }

        public override void OnChildDetached(UIShape parent, UIShape childComponent)
        {
            base.OnChildDetached(parent, childComponent);
            childComponent.OnAppliedChanges -= RefreshContainerForShape;
        }

        void RefreshContainerForShape(BaseDisposable updatedComponent)
        {
            MarkLayoutDirty( () =>
                {
                    referencesContainer.fitter.RefreshRecursively();
                    AdjustChildHook();
                    referencesContainer.scrollRect.Rebuild(CanvasUpdate.MaxUpdateValue);
                }
            );
        }

        void AdjustChildHook()
        {
            UIScrollRectRefContainer rc = referencesContainer;
            rc.childHookRectTransform.SetParent(rc.layoutElementRT, false);
            rc.childHookRectTransform.SetToMaxStretch();
            rc.childHookRectTransform.SetParent(rc.content, true);
            RefreshDCLLayoutRecursively(false, true);
        }

        public override void RefreshDCLLayoutRecursively(bool refreshSize = true,
            bool refreshAlignmentAndPosition = true)
        {
            base.RefreshDCLLayoutRecursively(refreshSize, refreshAlignmentAndPosition);
            referencesContainer.fitter.RefreshRecursively();
        }

        public override IEnumerator ApplyChanges(BaseModel newModel)
        {
            UIScrollRectRefContainer rc = referencesContainer;

            rc.contentBackground.color = model.backgroundColor;

            // Apply padding
            rc.paddingLayoutGroup.padding.bottom = Mathf.RoundToInt(model.paddingBottom);
            rc.paddingLayoutGroup.padding.top = Mathf.RoundToInt(model.paddingTop);
            rc.paddingLayoutGroup.padding.left = Mathf.RoundToInt(model.paddingLeft);
            rc.paddingLayoutGroup.padding.right = Mathf.RoundToInt(model.paddingRight);

            rc.scrollRect.horizontal = model.isHorizontal;
            rc.scrollRect.vertical = model.isVertical;

            rc.HScrollbar.value = model.valueX;
            rc.VScrollbar.value = model.valueY;

            rc.scrollRect.onValueChanged.AddListener(OnChanged);

            MarkLayoutDirty( () =>
                {
                    referencesContainer.fitter.RefreshRecursively();
                    AdjustChildHook();
                }
            );
            return null;
        }

        void OnChanged(Vector2 scrollingValues) { WebInterface.ReportOnScrollChange(scene.sceneData.sceneNumber, model.OnChanged, scrollingValues, 0); }

        public override void Dispose()
        {
            if (referencesContainer != null)
            {
                referencesContainer.scrollRect.onValueChanged.RemoveAllListeners();
                Utils.SafeDestroy(referencesContainer.gameObject);
            }

            base.Dispose();
        }
    }
}
