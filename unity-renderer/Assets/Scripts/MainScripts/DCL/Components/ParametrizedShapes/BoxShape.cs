using DCL.Controllers;
using DCL.Helpers;
using DCL.Models;
using UnityEngine;
using Decentraland.Sdk.Ecs6;
using System.Linq;

namespace DCL.Components
{
    public class BoxShape : ParametrizedShape<BoxShape.Model>
    {
        [System.Serializable]
        public new class Model : BaseShape.Model
        {
            public float[] uvs;

            public override BaseModel GetDataFromJSON(string json) =>
                Utils.SafeFromJson<Model>(json);

            public override BaseModel GetDataFromPb(ComponentBodyPayload pbModel)
            {
                if (pbModel.PayloadCase == ComponentBodyPayload.PayloadOneofCase.BoxShape)
                    return new Model
                    {
                        visible = pbModel.BoxShape.Visible,
                        withCollisions = pbModel.BoxShape.WithCollisions,
                        isPointerBlocker = pbModel.BoxShape.IsPointerBlocker,
                        uvs = pbModel.BoxShape.Uvs.ToArray(),
                    };

                Debug.LogError($"Payload provided for SDK6 {nameof(BoxShape)} component is not a {nameof(ComponentBodyPayload.PayloadOneofCase.BoxShape)}!");
                return null;
            }

        }

        public BoxShape() { model = new Model(); }

        public static Mesh cubeMesh = null;
        private static int cubeMeshRefCount = 0;

        public override int GetClassId() { return (int) CLASS_ID.BOX_SHAPE; }

        public override Mesh GenerateGeometry()
        {
            var model = (Model) this.model;

            if (cubeMesh == null)
                cubeMesh = PrimitiveMeshBuilder.BuildCube(1f);

            if (model.uvs != null && model.uvs.Length > 0)
            {
                cubeMesh.uv = Utils.FloatArrayToV2List(model.uvs);
            }

            cubeMeshRefCount++;
            return cubeMesh;
        }

        protected override void DestroyGeometry()
        {
            cubeMeshRefCount--;

            if (cubeMeshRefCount == 0)
            {
                GameObject.Destroy(cubeMesh);
                cubeMesh = null;
            }
        }

        protected override bool ShouldGenerateNewMesh(BaseShape.Model previousModel)
        {
            if (currentMesh == null)
                return true;

            BoxShape.Model newBoxModel = (BoxShape.Model) this.model;
            BoxShape.Model oldBoxModel = (BoxShape.Model) previousModel;

            if (newBoxModel.uvs != null && oldBoxModel.uvs != null)
            {
                if (newBoxModel.uvs.Length != oldBoxModel.uvs.Length)
                    return true;

                for (int i = 0; i < newBoxModel.uvs.Length; i++)
                {
                    if (newBoxModel.uvs[i] != oldBoxModel.uvs[i])
                        return true;
                }
            }
            else
            {
                if (newBoxModel.uvs != oldBoxModel.uvs)
                    return true;
            }

            return false;
        }
    }
}
