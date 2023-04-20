using DCL.Helpers;
using DCL.Models;
using UnityEngine;
using Decentraland.Sdk.Ecs6;

namespace DCL.Components
{
    public class CylinderShape : ParametrizedShape<CylinderShape.Model>
    {
        [System.Serializable]
        public new class Model : BaseShape.Model
        {
            public float radiusTop = 1f;
            public float radiusBottom = 1f;
            public float segmentsHeight = 1f;
            public float segmentsRadial = 36f;
            public bool openEnded;
            public float? radius;
            public float arc = 360f;

            public override BaseModel GetDataFromJSON(string json) =>
                Utils.SafeFromJson<Model>(json);

            public override BaseModel GetDataFromPb(ComponentBodyPayload pbModel)
            {
                if (pbModel.PayloadCase == ComponentBodyPayload.PayloadOneofCase.CylinderShape)
                    return new Model
                    {
                        arc = pbModel.CylinderShape.Arc,
                        radius = pbModel.CylinderShape.Radius,
                        openEnded = pbModel.CylinderShape.OpenEnded,
                        radiusBottom = pbModel.CylinderShape.RadiusBottom,
                        radiusTop = pbModel.CylinderShape.RadiusTop,
                        segmentsHeight = pbModel.CylinderShape.SegmentsHeight,
                        segmentsRadial = pbModel.CylinderShape.SegmentsRadial,
                        visible = pbModel.CylinderShape.Visible,
                        withCollisions = pbModel.CylinderShape.WithCollisions,
                        isPointerBlocker = pbModel.CylinderShape.IsPointerBlocker,
                    };

                return Utils.SafeUnimplemented<CylinderShape, Model>(expected: ComponentBodyPayload.PayloadOneofCase.CylinderShape, actual: pbModel.PayloadCase);
            }
        }

        public CylinderShape()
        {
            model = new Model();
        }

        public override int GetClassId() =>
            (int) CLASS_ID.CYLINDER_SHAPE;

        public override Mesh GenerateGeometry()
        {
            var model = (Model) this.model;
            return PrimitiveMeshBuilder.BuildCylinder(50, model.radiusTop, model.radiusBottom, 2f, 0f, true, false);
        }

        protected override bool ShouldGenerateNewMesh(BaseShape.Model newModel)
        {
            if (currentMesh == null)
                return true;

            Model newCylinderModel = newModel as Model;
            var model = (Model) this.model;
            return newCylinderModel.radius != model.radius
                   || newCylinderModel.radiusTop != model.radiusTop
                   || newCylinderModel.radiusBottom != model.radiusBottom
                   || newCylinderModel.segmentsHeight != model.segmentsHeight
                   || newCylinderModel.segmentsRadial != model.segmentsRadial
                   || newCylinderModel.openEnded != model.openEnded
                   || newCylinderModel.arc != model.arc;
        }
    }
}
