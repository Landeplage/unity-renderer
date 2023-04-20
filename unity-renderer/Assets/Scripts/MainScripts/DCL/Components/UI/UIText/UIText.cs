using System;
using DCL.Helpers;
using DCL.Models;
using System.Collections;
using TMPro;
using UnityEngine;
using Decentraland.Sdk.Ecs6;
using MainScripts.DCL.Components;

namespace DCL.Components
{
    public class UIText : UIShape<UITextReferencesContainer, UIText.Model>
    {
        [System.Serializable]
        new public class Model : UIShape.Model
        {
            public float outlineWidth = 0f;
            public Color outlineColor = Color.white;

            public Color color = Color.white;

            public bool adaptWidth = false;
            public bool adaptHeight = false;
            public float fontSize = 100f;
            public bool fontAutoSize = false;

            public string font;
            public string value = "";
            public float lineSpacing = 0f;
            public int lineCount = 0;
            public string hTextAlign = "bottom";
            public string vTextAlign = "left";
            public bool textWrapping = false;

            public float shadowBlur = 0f;
            public float shadowOffsetX = 0f;
            public float shadowOffsetY = 0f;
            public Color shadowColor = new Color(1, 1, 1);

            public float paddingTop = 0f;
            public float paddingRight = 0f;
            public float paddingBottom = 0f;
            public float paddingLeft = 0f;

            public override BaseModel GetDataFromJSON(string json) =>
                Utils.SafeFromJson<Model>(json);

            public override BaseModel GetDataFromPb(ComponentBodyPayload pbModel) =>
                pbModel.PayloadCase == ComponentBodyPayload.PayloadOneofCase.UiText
                    ? new Model
                    {
                        outlineWidth = pbModel.UiText.OutlineWidth,
                        outlineColor = pbModel.UiText.OutlineColor.AsUnityColor(),
                        color = pbModel.UiText.Color.AsUnityColor(),
                        adaptWidth = pbModel.UiText.AdaptWidth,
                        adaptHeight = pbModel.UiText.AdaptHeight,
                        fontSize = pbModel.UiText.FontSize,
                        fontAutoSize = pbModel.UiText.FontAutoSize,
                        font = pbModel.UiText.Font,
                        value = pbModel.UiText.Value,
                        lineSpacing = pbModel.UiText.LineSpacing,
                        lineCount = pbModel.UiText.LineCount,
                        hTextAlign = pbModel.UiText.HTextAlign,
                        vTextAlign = pbModel.UiText.VTextAlign,
                        textWrapping = pbModel.UiText.TextWrapping,
                        shadowBlur = pbModel.UiText.ShadowBlur,
                        shadowOffsetX = pbModel.UiText.ShadowOffsetX,
                        shadowOffsetY = pbModel.UiText.ShadowOffsetY,
                        shadowColor = pbModel.UiText.ShadowColor.AsUnityColor(),
                        paddingTop = pbModel.UiText.PaddingTop,
                        paddingRight = pbModel.UiText.PaddingRight,
                        paddingBottom = pbModel.UiText.PaddingBottom,
                        paddingLeft = pbModel.UiText.PaddingLeft
                    }
                    : Utils.SafeUnimplemented<UIText, Model>(expected: ComponentBodyPayload.PayloadOneofCase.UiText, actual: pbModel.PayloadCase);
        }

        public override string referencesContainerPrefabName => "UIText";

        public UIText()
        {
            model = new Model();
        }

        public override int GetClassId() =>
            (int) CLASS_ID.UI_TEXT_SHAPE;

        public override void AttachTo(IDCLEntity entity, System.Type overridenAttachedType = null) { Debug.LogError("Aborted UITextShape attachment to an entity. UIShapes shouldn't be attached to entities."); }

        public override void DetachFrom(IDCLEntity entity, System.Type overridenAttachedType = null) { }

        public override IEnumerator ApplyChanges(BaseModel baseModel)
        {
            model = (Model) baseModel;

            // We avoid using even yield break; as this instruction skips a frame and we don't want that.
            if ( !DCLFont.IsFontLoaded(scene, model.font) )
                yield return DCLFont.WaitUntilFontIsReady(scene, model.font);

            DCLFont.SetFontFromComponent(scene, model.font, referencesContainer.text);
            bool shouldMarkDirty = ShouldMarkDirty();

            ApplyModelChanges(referencesContainer.text, model);
            if (shouldMarkDirty) MarkLayoutDirty();
        }
        private bool ShouldMarkDirty()
        {
            TextMeshProUGUI text = referencesContainer.text;

            return model.value != text.text
                   || Math.Abs(model.fontSize - text.fontSize) > float.Epsilon
                   || model.fontAutoSize != text.enableAutoSizing
                   || Math.Abs(model.lineSpacing - text.lineSpacing) > float.Epsilon
                   || new Vector4((int) model.paddingLeft, (int) model.paddingTop, (int) model.paddingRight, (int) model.paddingBottom) != text.margin;
        }

        protected override void RefreshDCLSize(RectTransform parentTransform = null)
        {
            if (parentTransform == null)
            {
                parentTransform = referencesContainer.GetComponentInParent<RectTransform>();
            }

            if (model.adaptWidth || model.adaptHeight)
                referencesContainer.text.ForceMeshUpdate(false);

            Bounds b = referencesContainer.text.textBounds;

            float width, height;

            if (model.adaptWidth)
            {
                width = b.size.x;
            }
            else
            {
                width = model.width.GetScaledValue(parentTransform.rect.width);
            }

            if (model.adaptHeight)
            {
                height = b.size.y;
            }
            else
            {
                height = model.height.GetScaledValue(parentTransform.rect.height);
            }

            referencesContainer.layoutElementRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            referencesContainer.layoutElementRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        public override void Dispose()
        {
            if (referencesContainer != null)
                Utils.SafeDestroy(referencesContainer.gameObject);

            base.Dispose();
        }

        private static void ApplyModelChanges(TMP_Text text, Model model)
        {
            text.text = model.value;

            text.color = new Color(model.color.r, model.color.g, model.color.b, model.visible ? model.opacity : 0);
            text.fontSize = (int) model.fontSize;
            text.richText = true;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = model.fontAutoSize;

            text.margin =
                new Vector4
                (
                    (int) model.paddingLeft,
                    (int) model.paddingTop,
                    (int) model.paddingRight,
                    (int) model.paddingBottom
                );

            text.alignment = TextShape.GetAlignment(model.vTextAlign, model.hTextAlign);
            text.lineSpacing = model.lineSpacing;

            if (model.lineCount != 0)
            {
                text.maxVisibleLines = Mathf.Max(model.lineCount, 1);
            }
            else
            {
                text.maxVisibleLines = int.MaxValue;
            }

            text.enableWordWrapping = model.textWrapping && !text.enableAutoSizing;

            if (model.shadowOffsetX != 0 || model.shadowOffsetY != 0)
            {
                text.fontSharedMaterial.EnableKeyword("UNDERLAY_ON");
                text.fontSharedMaterial.SetColor("_UnderlayColor", model.shadowColor);
                text.fontSharedMaterial.SetFloat("_UnderlaySoftness", model.shadowBlur);
            }
            else if (text.fontSharedMaterial.IsKeywordEnabled("UNDERLAY_ON"))
            {
                text.fontSharedMaterial.DisableKeyword("UNDERLAY_ON");
            }

            if (model.outlineWidth > 0f)
            {
                text.fontSharedMaterial.EnableKeyword("OUTLINE_ON");
                text.outlineWidth = model.outlineWidth;
                text.outlineColor = model.outlineColor;
            }
            else if (text.fontSharedMaterial.IsKeywordEnabled("OUTLINE_ON"))
            {
                text.fontSharedMaterial.DisableKeyword("OUTLINE_ON");
            }
        }
    }
}
