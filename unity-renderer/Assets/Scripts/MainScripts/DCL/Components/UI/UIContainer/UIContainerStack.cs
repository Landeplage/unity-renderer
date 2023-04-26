using DCL.Controllers;
using DCL.Helpers;
using DCL.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Decentraland.Sdk.Ecs6;
using MainScripts.DCL.Components;

namespace DCL.Components
{
    public class UIContainerStack : UIShape<UIContainerRectReferencesContainer, UIContainerStack.Model>
    {
        [System.Serializable]
        public new class Model : UIShape.Model
        {
            public Color color = Color.clear;
            public StackOrientation stackOrientation = StackOrientation.VERTICAL;
            public bool adaptWidth = true;
            public bool adaptHeight = true;
            public float spacing = 0;

            public override BaseModel GetDataFromJSON(string json) =>
                Utils.SafeFromJson<Model>(json);

            public override BaseModel GetDataFromPb(ComponentBodyPayload pbModel)
            {
                if (pbModel.PayloadCase != ComponentBodyPayload.PayloadOneofCase.UiContainerStack)
                    return Utils.SafeUnimplemented<UIContainerStack, Model>(expected: ComponentBodyPayload.PayloadOneofCase.UiContainerStack, actual: pbModel.PayloadCase);

                var model = new Model
                {
                    color = pbModel.UiContainerStack.Color.AsUnityColor(),
                    stackOrientation = (StackOrientation) pbModel.UiContainerStack.StackOrientation,
                    adaptWidth = pbModel.UiContainerStack.AdaptWidth,
                    adaptHeight = pbModel.UiContainerStack.AdaptHeight,
                    spacing = pbModel.UiContainerStack.Spacing,

                    name = pbModel.UiContainerStack.Name,
                    // parentComponent = ??
                    visible = pbModel.UiContainerStack.Visible,
                    opacity = pbModel.UiContainerStack.Opacity,
                    hAlign = pbModel.UiContainerStack.HAlign,
                    vAlign = pbModel.UiContainerStack.VAlign,
                    // width = new UIValue(pbModel.UiShape.Width.Value, (UIValue.Unit) pbModel.UiShape.Width.Type),
                    // height = new UIValue(pbModel.UiShape.Height.Value, (UIValue.Unit) pbModel.UiShape.Height.Type),
                    // positionX = new UIValue(pbModel.UiShape.PositionX.Value, (UIValue.Unit) pbModel.UiShape.PositionX.Type),
                    // positionY = new UIValue(pbModel.UiShape.PositionY.Value, (UIValue.Unit) pbModel.UiShape.PositionY.Type),
                    isPointerBlocker = pbModel.UiContainerStack.IsPointerBlocker,
                    // onClick = ??
                };

                return model;
            }

        }

        public enum StackOrientation
        {
            VERTICAL,
            HORIZONTAL
        }

        public override string referencesContainerPrefabName => "UIContainerRect";

        public Dictionary<string, GameObject> stackContainers = new Dictionary<string, GameObject>();

        HorizontalOrVerticalLayoutGroup layoutGroup;

        public UIContainerStack() { model = new Model(); }

        public override int GetClassId() { return (int) CLASS_ID.UI_CONTAINER_STACK; }

        public override void AttachTo(IDCLEntity entity, System.Type overridenAttachedType = null)
        {
            Debug.LogError(
                "Aborted UIContainerStack attachment to an entity. UIShapes shouldn't be attached to entities.");
        }

        public override void DetachFrom(IDCLEntity entity, System.Type overridenAttachedType = null) { }

        public override IEnumerator ApplyChanges(BaseModel newModel)
        {
            referencesContainer.image.color = new Color(model.color.r, model.color.g, model.color.b, model.color.a);
            referencesContainer.image.raycastTarget = model.color.a >= RAYCAST_ALPHA_THRESHOLD;

            if (model.stackOrientation == StackOrientation.VERTICAL && !(layoutGroup is VerticalLayoutGroup))
            {
                Object.DestroyImmediate(layoutGroup, false);
                layoutGroup = childHookRectTransform.gameObject.AddComponent<VerticalLayoutGroup>();
            }
            else if (model.stackOrientation == StackOrientation.HORIZONTAL && !(layoutGroup is HorizontalLayoutGroup))
            {
                Object.DestroyImmediate(layoutGroup, false);
                layoutGroup = childHookRectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = model.spacing;

            referencesContainer.sizeFitter.adjustHeight = model.adaptHeight;
            referencesContainer.sizeFitter.adjustWidth = model.adaptWidth;

            MarkLayoutDirty();
            return null;
        }

        void RefreshContainerForShape(BaseDisposable updatedComponent)
        {
            UIShape childComponent = updatedComponent as UIShape;
            Assert.IsTrue(childComponent != null, "This should never happen!!!!");

            if (((UIShape.Model)childComponent.GetModel()).parentComponent != this.id)
            {
                MarkLayoutDirty();
                return;
            }

            GameObject stackContainer = null;

            if (!stackContainers.ContainsKey(childComponent.id))
            {
                stackContainer = Object.Instantiate(Resources.Load("UIContainerStackChild")) as GameObject;
#if UNITY_EDITOR
                stackContainer.name = "UIContainerStackChild - " + childComponent.id;
#endif
                stackContainers.Add(childComponent.id, stackContainer);

                int oldSiblingIndex = childComponent.referencesContainer.transform.GetSiblingIndex();
                childComponent.referencesContainer.transform.SetParent(stackContainer.transform, false);
                stackContainer.transform.SetParent(referencesContainer.childHookRectTransform, false);
                stackContainer.transform.SetSiblingIndex(oldSiblingIndex);
            }
            else
            {
                stackContainer = stackContainers[childComponent.id];
            }

            MarkLayoutDirty();
        }

        public override void OnChildAttached(UIShape parentComponent, UIShape childComponent)
        {
            RefreshContainerForShape(childComponent);
            childComponent.OnAppliedChanges -= RefreshContainerForShape;
            childComponent.OnAppliedChanges += RefreshContainerForShape;
        }

        public override void RefreshDCLLayoutRecursively(bool refreshSize = true,
            bool refreshAlignmentAndPosition = true)
        {
            base.RefreshDCLLayoutRecursively(refreshSize, refreshAlignmentAndPosition);
            referencesContainer.sizeFitter.RefreshRecursively();
        }

        public override void OnChildDetached(UIShape parentComponent, UIShape childComponent)
        {
            if (parentComponent != this)
            {
                return;
            }

            if (stackContainers.ContainsKey(childComponent.id))
            {
                Object.Destroy(stackContainers[childComponent.id]);
                stackContainers[childComponent.id].transform.SetParent(null);
                stackContainers[childComponent.id].name += "- Detached";
                stackContainers.Remove(childComponent.id);
            }

            childComponent.OnAppliedChanges -= RefreshContainerForShape;
            RefreshDCLLayout();
        }

        public override void Dispose()
        {
            if (referencesContainer != null)
                Utils.SafeDestroy(referencesContainer.gameObject);

            base.Dispose();
        }
    }
}
