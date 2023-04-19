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
    }
}
