using DCL.Controllers;
using DCL.Helpers;
using DCL.Models;
using UnityEngine;
using Decentraland.Sdk.Ecs6;
using System.Linq;

namespace DCL.Components
{
    public class PlaneShape : ParametrizedShape<PlaneShape.Model>
    {
        [System.Serializable]
        new public class Model : BaseShape.Model
        {
            public float[] uvs;
            public float width = 1f; // Plane
            public float height = 1f; // Plane

            public override BaseModel GetDataFromJSON(string json) =>
                Utils.SafeFromJson<Model>(json);

            public override BaseModel GetDataFromPb(ComponentBodyPayload pbModel) =>
                pbModel.PayloadCase == ComponentBodyPayload.PayloadOneofCase.PlaneShape
                    ? new Model
                    {
                        uvs = pbModel.PlaneShape.Uvs.ToArray(),
                        height = pbModel.PlaneShape.Height,
                        width = pbModel.PlaneShape.Width,
                        visible = pbModel.PlaneShape.Visible,
                        withCollisions = pbModel.PlaneShape.WithCollisions,
                        isPointerBlocker = pbModel.PlaneShape.IsPointerBlocker,
                    }
                    : Utils.SafeUnimplemented<PlaneShape, Model>(expected: ComponentBodyPayload.PayloadOneofCase.PlaneShape, actual: pbModel.PayloadCase);
        }

        public PlaneShape() { model = new Model(); }

        public override int GetClassId() { return (int) CLASS_ID.PLANE_SHAPE; }

        public override Mesh GenerateGeometry()
        {
            var model = (Model) this.model;

            Mesh mesh = PrimitiveMeshBuilder.BuildPlane(1f);
            if (model.uvs != null && model.uvs.Length > 0)
            {
                mesh.uv = Utils.FloatArrayToV2List(model.uvs);
            }

            return mesh;
        }

        protected override bool ShouldGenerateNewMesh(BaseShape.Model previousModel)
        {
            if (currentMesh == null)
                return true;

            PlaneShape.Model newPlaneModel = (PlaneShape.Model) this.model;
            PlaneShape.Model oldPlaneModel = (PlaneShape.Model) previousModel;

            if (newPlaneModel.uvs != null && oldPlaneModel.uvs != null)
            {
                if (newPlaneModel.uvs.Length != oldPlaneModel.uvs.Length)
                    return true;

                for (int i = 0; i < newPlaneModel.uvs.Length; i++)
                {
                    if (newPlaneModel.uvs[i] != oldPlaneModel.uvs[i])
                        return true;
                }
            }
            else
            {
                if (newPlaneModel.uvs != oldPlaneModel.uvs)
                    return true;
            }

            return newPlaneModel.width != oldPlaneModel.width || newPlaneModel.height != oldPlaneModel.height;
        }
    }
}
