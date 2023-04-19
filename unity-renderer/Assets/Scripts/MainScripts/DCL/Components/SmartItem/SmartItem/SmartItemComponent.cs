using System.Collections;
using System.Collections.Generic;
using DCL.Controllers;
using DCL.Helpers;
using DCL.Models;
using Newtonsoft.Json;
using Decentraland.Sdk.Ecs6;

namespace DCL.Components
{
    public class SmartItemComponent : BaseComponent
    {
        public class Model : BaseModel
        {
            public string assetId;
            public string src;
            public Dictionary<object, object> values = new Dictionary<object, object>();

            public override BaseModel GetDataFromJSON(string json) { return JsonConvert.DeserializeObject<Model>(json); }

            public override BaseModel GetDataFromPb(ComponentBodyPayload pbModel) {
                return null; //Utils.SafeUnimplemented<Model>();
            }

        }

        public override void Initialize(IParcelScene scene, IDCLEntity entity)
        {
            base.Initialize(scene, entity);

            DataStore.i.sceneWorldObjects.AddExcludedOwner(scene.sceneData.sceneNumber, entity.entityId);
        }

        public override void Cleanup()
        {
            DataStore.i.sceneWorldObjects.RemoveExcludedOwner(scene.sceneData.sceneNumber, entity.entityId);
            base.Cleanup();
        }

        private void Awake() { model = new Model(); }

        public override IEnumerator ApplyChanges(BaseModel newModel) { yield break; }

        public override int GetClassId() { return (int) CLASS_ID_COMPONENT.SMART_ITEM; }

        public Dictionary<object, object> GetValues() { return ((Model)model).values; }

        public override string componentName => "smartItem";
    }
}
