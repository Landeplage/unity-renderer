using System;
using DCL.Helpers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Decentraland.Sdk.Ecs6;
using MainScripts.DCL.Components;

[Serializable]
public class AvatarModel : BaseModel
{
    [Serializable]
    public class AvatarEmoteEntry
    {
        public int slot;
        public string urn;
    }

    public string id;
    public string name;
    public string bodyShape;
    public Color skinColor;
    public Color hairColor;
    public Color eyeColor;
    public List<string> wearables = new List<string>();

    public List<AvatarEmoteEntry> emotes = new List<AvatarEmoteEntry>();

    public string expressionTriggerId = null;
    public long expressionTriggerTimestamp = -1;
    public string stickerTriggerId = null;
    public long stickerTriggerTimestamp = -1;
    public bool talking = false;

    public bool HaveSameWearablesAndColors(AvatarModel other)
    {
        if (other == null)
            return false;

        //wearables are the same
        if (!(wearables.Count == other.wearables.Count
              && wearables.All(other.wearables.Contains)
              && other.wearables.All(wearables.Contains)))
            return false;

        //emotes are the same
        if (emotes == null && other.emotes != null)
            return false;
        if (emotes != null && other.emotes == null)
            return false;
        if (emotes != null && other.emotes != null)
        {
            if (emotes.Count != other.emotes.Count)
                return false;

            for (var i = 0; i < emotes.Count; i++)
            {
                AvatarEmoteEntry emote = emotes[i];
                if (other.emotes.FirstOrDefault(x => x.urn == emote.urn) == null)
                    return false;
            }
        }

        return bodyShape == other.bodyShape &&
               skinColor == other.skinColor &&
               hairColor == other.hairColor &&
               eyeColor == other.eyeColor;
    }

    public bool HaveSameExpressions(AvatarModel other)
    {
        return expressionTriggerId == other.expressionTriggerId &&
               expressionTriggerTimestamp == other.expressionTriggerTimestamp &&
               stickerTriggerTimestamp == other.stickerTriggerTimestamp;
    }

    public bool Equals(AvatarModel other)
    {
        if (other == null) return false;

        bool wearablesAreEqual = wearables.All(other.wearables.Contains)
                                 && other.wearables.All(wearables.Contains)
                                 && wearables.Count == other.wearables.Count;

        return id == other.id &&
               name == other.name &&
               bodyShape == other.bodyShape &&
               skinColor == other.skinColor &&
               hairColor == other.hairColor &&
               eyeColor == other.eyeColor &&
               expressionTriggerId == other.expressionTriggerId &&
               expressionTriggerTimestamp == other.expressionTriggerTimestamp &&
               stickerTriggerTimestamp == other.stickerTriggerTimestamp &&
               wearablesAreEqual;
    }

    public void CopyFrom(AvatarModel other)
    {
        if (other == null)
            return;

        id = other.id;
        name = other.name;
        bodyShape = other.bodyShape;
        skinColor = other.skinColor;
        hairColor = other.hairColor;
        eyeColor = other.eyeColor;
        expressionTriggerId = other.expressionTriggerId;
        expressionTriggerTimestamp = other.expressionTriggerTimestamp;
        stickerTriggerId = other.stickerTriggerId;
        stickerTriggerTimestamp = other.stickerTriggerTimestamp;
        wearables = new List<string>(other.wearables);
        emotes = other.emotes.Select(x => new AvatarEmoteEntry() { slot = x.slot, urn = x.urn }).ToList();
    }

    public override BaseModel GetDataFromJSON(string json) =>
        Utils.SafeFromJson<AvatarModel>(json);

    public override BaseModel GetDataFromPb(ComponentBodyPayload pbModel)
    {
        if (pbModel.PayloadCase == ComponentBodyPayload.PayloadOneofCase.AvatarShape)
        {
            var model = new AvatarModel
                {
                    id = pbModel.AvatarShape.Id,
                    name = pbModel.AvatarShape.Name,
                    talking = pbModel.AvatarShape.Talking,
                    bodyShape = pbModel.AvatarShape.BodyShape,
                    eyeColor = pbModel.AvatarShape.EyeColor.AsUnityColor(),
                    hairColor = pbModel.AvatarShape.HairColor.AsUnityColor(),
                    skinColor = pbModel.AvatarShape.SkinColor.AsUnityColor(),
                    expressionTriggerId = pbModel.AvatarShape.ExpressionTriggerId,
                    expressionTriggerTimestamp = pbModel.AvatarShape.ExpressionTriggerTimestamp,
                    // model.stickerTriggerTimestamp = ??
                    // model.stickerTriggerId = ??

                    wearables = new List<string>(pbModel.AvatarShape.Wearables.Count),
                    emotes = new List<AvatarEmoteEntry>(pbModel.AvatarShape.Emotes.Count),
                };

            for (var i = 0; i < pbModel.AvatarShape.Wearables.Count; i++)
                model.wearables[i] = pbModel.AvatarShape.Wearables[i];

            for (var i = 0; i < pbModel.AvatarShape.Emotes.Count; i++)
            {
                model.emotes[i].slot = pbModel.AvatarShape.Emotes[i].Slot;
                model.emotes[i].urn = pbModel.AvatarShape.Emotes[i].Urn;
            }

            return model;
        }

        Debug.LogError($"Payload provided for SDK6 {nameof(AvatarModel)} component is not a {nameof(ComponentBodyPayload.PayloadOneofCase.AvatarShape)}!");
        return null;
    }

}
