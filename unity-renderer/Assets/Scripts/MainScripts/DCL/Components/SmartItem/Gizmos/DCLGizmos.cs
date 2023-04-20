using System.Collections;
using DCL.Helpers;
using DCL.Models;
using Decentraland.Sdk.Ecs6;

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

            public override BaseModel GetDataFromJSON(string json) =>
                Utils.SafeFromJson<Model>(json);

            public override BaseModel GetDataFromPb(ComponentBodyPayload pbModel) =>
                pbModel.PayloadCase == ComponentBodyPayload.PayloadOneofCase.Gizmos
                    ? new Model
                    {
                        cycle = pbModel.Gizmos.Cycle,
                        position = pbModel.Gizmos.Position,
                        rotation = pbModel.Gizmos.Rotation,
                        scale = pbModel.Gizmos.Scale,
                        localReference = pbModel.Gizmos.LocalReference,
                        selectedGizmo = pbModel.Gizmos.SelectedGizmo,
                    }
                    : Utils.SafeUnimplemented<DCLGizmos, Model>(expected: ComponentBodyPayload.PayloadOneofCase.Gizmos, actual: pbModel.PayloadCase);
        }

        private void Awake()
        {
            model = new Model();
        }

        public override IEnumerator ApplyChanges(BaseModel baseModel) =>
            null;

        public override int GetClassId() =>
            (int) CLASS_ID_COMPONENT.GIZMOS;

        public override string componentName => "gizmos";
    }
}
