using DCL.Helpers;
using DCL.Models;
using UnityEngine;
using Decentraland.Sdk.Ecs6;

namespace DCL.Components
{
    public class ConeShape : ParametrizedShape<ConeShape.Model>
    {
        [System.Serializable]
        public new class Model : BaseShape.Model
        {
            public float radiusTop;
            public float radiusBottom = 1f;
            public float segmentsHeight = 1f;
            public float segmentsRadial = 36f;
            public bool openEnded;
            public float? radius;
            public float arc = 360f;

            public override BaseModel GetDataFromJSON(string json) =>
                Utils.SafeFromJson<Model>(json);

            public override BaseModel GetDataFromPb(ComponentBodyPayload pbModel) =>
                pbModel.PayloadCase == ComponentBodyPayload.PayloadOneofCase.ConeShape
                    ? new Model
                    {
                        arc = pbModel.ConeShape.Arc,
                        radius = pbModel.ConeShape.Radius,
                        openEnded = pbModel.ConeShape.OpenEnded,
                        radiusBottom = pbModel.ConeShape.RadiusBottom,
                        radiusTop = pbModel.ConeShape.RadiusTop,
                        segmentsHeight = pbModel.ConeShape.SegmentsHeight,
                        segmentsRadial = pbModel.ConeShape.SegmentsRadial,
                        visible = pbModel.ConeShape.Visible,
                        withCollisions = pbModel.ConeShape.WithCollisions,
                        isPointerBlocker = pbModel.ConeShape.IsPointerBlocker,
                    }
                    : Utils.SafeUnimplemented<ConeShape, Model>(expected: ComponentBodyPayload.PayloadOneofCase.ConeShape, actual: pbModel.PayloadCase);
        }

        public ConeShape()
        {
            model = new Model();
        }

        public override int GetClassId() =>
            (int) CLASS_ID.CONE_SHAPE;

        public override Mesh GenerateGeometry()
        {
            var model = (Model) this.model;
            return PrimitiveMeshBuilder.BuildCone(50, model.radiusTop, model.radiusBottom, 2f, 0f, true, false);
        }

        protected override bool ShouldGenerateNewMesh(BaseShape.Model newModel)
        {
            if (currentMesh == null)
                return true;

            Model newConeModel = newModel as Model;
            var model = (Model) this.model;
            return newConeModel.radius != model.radius
                   || newConeModel.radiusTop != model.radiusTop
                   || newConeModel.radiusBottom != model.radiusBottom
                   || newConeModel.segmentsHeight != model.segmentsHeight
                   || newConeModel.segmentsRadial != model.segmentsRadial
                   || newConeModel.openEnded != model.openEnded
                   || newConeModel.arc != model.arc;
        }
    }
}
