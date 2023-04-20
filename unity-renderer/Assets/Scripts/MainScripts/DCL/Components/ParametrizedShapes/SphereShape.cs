using DCL.Helpers;
using DCL.Models;
using UnityEngine;
using Decentraland.Sdk.Ecs6;

namespace DCL.Components
{
    public class SphereShape : ParametrizedShape<SphereShape.Model>
    {
        [System.Serializable]
        public new class Model : BaseShape.Model
        {
            public override BaseModel GetDataFromJSON(string json) =>
                Utils.SafeFromJson<Model>(json);

            public override BaseModel GetDataFromPb(ComponentBodyPayload pbModel) =>
                pbModel.PayloadCase == ComponentBodyPayload.PayloadOneofCase.SphereShape
                    ? new Model
                    {
                        visible = pbModel.SphereShape.Visible,
                        withCollisions = pbModel.SphereShape.WithCollisions,
                        isPointerBlocker = pbModel.SphereShape.IsPointerBlocker,
                    }
                    : Utils.SafeUnimplemented<SphereShape, Model>(expected: ComponentBodyPayload.PayloadOneofCase.SphereShape, actual: pbModel.PayloadCase);
        }

        public static Mesh mesh;
        private static int meshUses;

        public SphereShape()
        {
            model = new Model();
        }

        public override int GetClassId() =>
            (int) CLASS_ID.SPHERE_SHAPE;

        public override Mesh GenerateGeometry()
        {
            if (mesh == null)
            {
                mesh = PrimitiveMeshBuilder.BuildSphere(1f);
            }

            meshUses++;

            return mesh;
        }

        protected override void DestroyGeometry()
        {
            meshUses--;
            if (meshUses == 0)
            {
                GameObject.Destroy(mesh);
            }
        }

        protected override bool ShouldGenerateNewMesh(BaseShape.Model newModel) =>
            currentMesh == null;
    }
}
