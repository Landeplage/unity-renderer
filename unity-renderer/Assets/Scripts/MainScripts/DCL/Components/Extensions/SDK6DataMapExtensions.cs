using DCL.Components;
using Decentraland.Common;
using UnityEngine;

namespace MainScripts.DCL.Components
{
    public static class SDK6DataMapExtensions
    {
        public static Color AsUnityColor(this Color4 color4) =>
            new (color4.R, color4.G, color4.B, color4.A);

        public static Color AsUnityColor(this Color3 color3) =>
            new (color3.R, color3.G, color3.B);

        public static UnityEngine.Vector3 AsUnityVector3(this Decentraland.Common.Vector3 vector3) =>
            new (vector3.X, vector3.Y, vector3.Z);

        public static UnityEngine.Quaternion AsUnityQuaternion(this Decentraland.Common.Quaternion quaternion) =>
            new (quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
        
        public static UIValue FromProtobuf(UIValue defaultValue, Decentraland.Sdk.Ecs6.UiValue uiValue) =>
            new()
            {
                value = uiValue.HasValue ? uiValue.Value : defaultValue.value,
                type = uiValue.HasType ? (UIValue.Unit)uiValue.Type : defaultValue.type
            };
    }
}
