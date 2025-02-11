using DCL.CRDT;
using DCL.ECS7;
using DCL.ECS7.InternalComponents;
using DCL.ECSComponents;
using DCL.ECSRuntime;
using DCL.Models;
using ECSSystems.InputSenderSystem;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Tests
{
    public class ECSInputSenderSystemShould
    {
        private ECS7TestUtilsScenesAndEntities testUtils;
        private ECS7TestScene scene;

        private IInternalECSComponent<InternalInputEventResults> inputResultComponent;
        private IECSComponentWriter componentWriter;
        private InternalECSComponents internalComponents;

        private Action updateSystems;

        [SetUp]
        public void SetUp()
        {
            var componentsFactory = new ECSComponentsFactory();
            var componentsManager = new ECSComponentsManager(componentsFactory.componentBuilders);
            var executors = new Dictionary<int, ICRDTExecutor>();
            internalComponents = new InternalECSComponents(componentsManager, componentsFactory, executors);

            inputResultComponent = internalComponents.inputEventResultsComponent;
            componentWriter = Substitute.For<IECSComponentWriter>();

            testUtils = new ECS7TestUtilsScenesAndEntities(componentsManager, executors);
            scene = testUtils.CreateScene(666);

            var systemUpdate = ECSInputSenderSystem.CreateSystem(inputResultComponent, componentWriter);

            updateSystems = () =>
            {
                internalComponents.MarkDirtyComponentsUpdate();
                systemUpdate();
                internalComponents.ResetDirtyComponentsUpdate();
            };
        }

        [TearDown]
        public void TearDown()
        {
            testUtils.Dispose();
        }

        [Test]
        public void SendResultsWhenDirty()
        {
            IList<InternalInputEventResults.EventData> events = new List<InternalInputEventResults.EventData>()
            {
                new InternalInputEventResults.EventData() { button = InputAction.IaPrimary, hit = new RaycastHit() { } },
                new InternalInputEventResults.EventData() { button = InputAction.IaSecondary, hit = new RaycastHit() { } },
                new InternalInputEventResults.EventData() { button = InputAction.IaAction4, hit = new RaycastHit() { } },
                new InternalInputEventResults.EventData() { button = InputAction.IaAction3, hit = new RaycastHit() { } },
                new InternalInputEventResults.EventData() { button = InputAction.IaAction5, hit = new RaycastHit() { } },
            };

            foreach (var eventData in events)
            {
                inputResultComponent.AddEvent(scene, eventData);
            }

            updateSystems();

            componentWriter.Received(5)
                           .AppendComponent(
                                scene.sceneData.sceneNumber,
                                SpecialEntityId.SCENE_ROOT_ENTITY,
                                ComponentID.POINTER_EVENTS_RESULT,
                                Arg.Any<PBPointerEventsResult>(),
                                ECSComponentWriteType.SEND_TO_SCENE | ECSComponentWriteType.WRITE_STATE_LOCALLY);
        }

        [Test]
        public void NotSendResultsWhenIsNotDirty()
        {
            IList<InternalInputEventResults.EventData> events = new List<InternalInputEventResults.EventData>()
            {
                new InternalInputEventResults.EventData() { button = InputAction.IaPrimary, hit = new RaycastHit() { } },
                new InternalInputEventResults.EventData() { button = InputAction.IaSecondary, hit = new RaycastHit() { } },
                new InternalInputEventResults.EventData() { button = InputAction.IaAction4, hit = new RaycastHit() { } },
                new InternalInputEventResults.EventData() { button = InputAction.IaAction3, hit = new RaycastHit() { } },
                new InternalInputEventResults.EventData() { button = InputAction.IaAction5, hit = new RaycastHit() { } },
            };

            foreach (var eventData in events)
            {
                inputResultComponent.AddEvent(scene, eventData);
            }

            var compData = inputResultComponent.GetFor(scene, SpecialEntityId.SCENE_ROOT_ENTITY);
            var model = compData.model;

            inputResultComponent.PutFor(scene, SpecialEntityId.SCENE_ROOT_ENTITY, model);

            //clean dirty
            internalComponents.MarkDirtyComponentsUpdate();
            internalComponents.ResetDirtyComponentsUpdate();

            updateSystems();

            componentWriter.DidNotReceive()
                           .PutComponent(
                                scene.sceneData.sceneNumber,
                                SpecialEntityId.SCENE_ROOT_ENTITY,
                                ComponentID.POINTER_EVENTS_RESULT,
                                Arg.Any<PBPointerEventsResult>(),
                                ECSComponentWriteType.SEND_TO_SCENE | ECSComponentWriteType.WRITE_STATE_LOCALLY);
        }
    }
}
