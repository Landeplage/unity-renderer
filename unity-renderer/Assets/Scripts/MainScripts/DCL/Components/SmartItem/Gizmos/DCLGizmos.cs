using System.Collections;
using System.Collections.Generic;
using DCL.Helpers;
using DCL.Models;
using Decentraland.Sdk.Ecs6;
using UnityEngine;

namespace DCL.Components
{
    public class DCLGizmos : BaseComponent
    {
        public static class Gizmo
        {
            public const string MOVE = "MOVE";
            public const string ROTATE = "ROTATE";
            public const string SCALE = "SCALE";
            public const string NONE = "NONE";
        }

        [System.Serializable]
        public class Model : BaseModel
        {
            public bool position = true;
            public bool rotation = true;
            public bool scale = true;
            public bool cycle = true;
            public string selectedGizmo = Gizmo.NONE;
            public bool localReference = false;

            public override BaseModel GetDataFromJSON(string json) { return Utils.SafeFromJson<Model>(json); }

            public override BaseModel GetDataFromPb(ComponentBodyPayload pbModel)
            {
                if (pbModel.PayloadCase == ComponentBodyPayload.PayloadOneofCase.Gizmos)
                    return new Model
                    {
                        cycle = pbModel.Gizmos.Cycle,
                        position = pbModel.Gizmos.Position,
                        rotation = pbModel.Gizmos.Rotation,
                        scale = pbModel.Gizmos.Scale,
                        localReference = pbModel.Gizmos.LocalReference,
                        selectedGizmo = pbModel.Gizmos.SelectedGizmo,
                    };

                Debug.LogError($"Payload provided for SDK6 {nameof(DCLGizmos)} component is not a {nameof(ComponentBodyPayload.PayloadOneofCase.Gizmos)}!");
                return null;
            }

        }

        private void Awake() { model = new Model(); }

        public override IEnumerator ApplyChanges(BaseModel baseModel) { return null; }

        public override int GetClassId() { return (int) CLASS_ID_COMPONENT.GIZMOS; }

        public override string componentName => "gizmos";
    }
}
