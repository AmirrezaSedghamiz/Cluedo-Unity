using System;
using NUnit.Framework;
using TomorrowsCrime.Core;
using TomorrowsCrime.Events;
using TomorrowsCrime.Investigation;
using TomorrowsCrime.World;

namespace TomorrowsCrime.Tests.EditMode
{
    [TestFixture]
    public class WorldStateAndScenarioTests
    {
        private WorldStateRegistry _registry;
        private InvestigationKnowledgeService _knowledge;

        [SetUp]
        public void Setup()
        {
            _registry = new WorldStateRegistry();
            _knowledge = InvestigationKnowledgeService.Instance;
            _knowledge.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            _registry?.Clear();
            _knowledge?.Clear();
        }

        [Test]
        public void Requirement1_RegisteredObjectStartsWithBaselineState()
        {
            _registry.RegisterEntry("door_room_102", DoorLockState.Locked);

            var state = _registry.GetState<DoorLockState>("door_room_102");
            var baseline = _registry.GetBaseline<DoorLockState>("door_room_102");

            Assert.AreEqual(DoorLockState.Locked, state);
            Assert.AreEqual(DoorLockState.Locked, baseline);
        }

        [Test]
        public void Requirement2_MutationChangesRuntimeState()
        {
            _registry.RegisterEntry("door_room_102", DoorLockState.Locked);

            _registry.SetState("door_room_102", DoorLockState.Unlocked);

            Assert.AreEqual(DoorLockState.Unlocked, _registry.GetState<DoorLockState>("door_room_102"));
            Assert.AreEqual(DoorLockState.Locked, _registry.GetBaseline<DoorLockState>("door_room_102"));
        }

        [Test]
        public void Requirement3_ResetRestoresBaselineState()
        {
            _registry.RegisterEntry("key_room_102", PropLocationState.AtBaseline);
            _registry.RegisterEntry("door_room_102", DoorLockState.Locked);

            // Mutate during loop
            _registry.SetState("key_room_102", PropLocationState.HeldByPlayer);
            _registry.SetState("door_room_102", DoorLockState.Unlocked);

            // Simulate loop reset
            _registry.ResetAllToBaseline();

            Assert.AreEqual(PropLocationState.AtBaseline, _registry.GetState<PropLocationState>("key_room_102"));
            Assert.AreEqual(DoorLockState.Locked, _registry.GetState<DoorLockState>("door_room_102"));
        }

        [Test]
        public void Requirement4_PersistentDiscoveriesSurviveLoopReset()
        {
            _knowledge.RecordDiscovery("key_missing_at_table", "Key was taken before 00:30.", 1, 30.0f);

            // Trigger world reset
            _registry.ResetAllToBaseline();

            Assert.IsTrue(_knowledge.HasDiscovery("key_missing_at_table"));
            Assert.AreEqual(1, _knowledge.DiscoveryCount);
        }

        [Test]
        public void Requirement5_RecordingDuplicateDiscoveryDoesNotDuplicate()
        {
            bool firstAdd = _knowledge.RecordDiscovery("key_missing_at_table", "Key was taken.", 1, 30.0f);
            bool secondAdd = _knowledge.RecordDiscovery("key_missing_at_table", "Key was taken again.", 2, 30.0f);

            Assert.IsTrue(firstAdd, "First recording should return true.");
            Assert.IsFalse(secondAdd, "Duplicate recording should return false.");
            Assert.AreEqual(1, _knowledge.DiscoveryCount);
        }

        [Test]
        public void Requirement6_DuplicateWorldStateIdsHandledExplicitly()
        {
            _registry.RegisterEntry("key_room_102", PropLocationState.AtBaseline);

            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                _registry.RegisterEntry("key_room_102", PropLocationState.AtBaseline);
            });

            StringAssert.Contains("Duplicate world state registration detected", ex.Message);
        }

        [Test]
        public void Requirement7_ScheduledActionSucceedsWhenPreconditionsSatisfied()
        {
            var status = KeyScenarioController.EvaluatePreconditions(PropLocationState.AtBaseline, out var reason);

            Assert.AreEqual(ScenarioExecutionStatus.Succeeded, status);
            StringAssert.Contains("Preconditions met", reason);
        }

        [Test]
        public void Requirement8_ScheduledActionFailsWhenRequiredKeyUnavailable()
        {
            // Test when key is held by player
            var statusHeld = KeyScenarioController.EvaluatePreconditions(PropLocationState.HeldByPlayer, out var reasonHeld);
            Assert.AreEqual(ScenarioExecutionStatus.Failed, statusHeld);
            Assert.AreEqual("Event failed: required key was not available at the expected location.", reasonHeld);

            // Test when key was dropped elsewhere
            var statusMoved = KeyScenarioController.EvaluatePreconditions(PropLocationState.DroppedElsewhere, out var reasonMoved);
            Assert.AreEqual(ScenarioExecutionStatus.Failed, statusMoved);
            Assert.AreEqual("Event failed: required key was not available at the expected location.", reasonMoved);
        }

        [Test]
        public void Requirement9_ResettingPermitsScheduledActionToExecuteAgainInSubsequentLoop()
        {
            _registry.RegisterEntry("key_room_102", PropLocationState.AtBaseline);

            // Loop 1: Player moves key, scenario fails
            _registry.SetState("key_room_102", PropLocationState.HeldByPlayer);
            var loop1Status = KeyScenarioController.EvaluatePreconditions(
                _registry.GetState<PropLocationState>("key_room_102"), out _);
            Assert.AreEqual(ScenarioExecutionStatus.Failed, loop1Status);

            // Turnover / Reset
            _registry.ResetAllToBaseline();

            // Loop 2: Key is back at baseline, scenario now succeeds!
            var loop2Status = KeyScenarioController.EvaluatePreconditions(
                _registry.GetState<PropLocationState>("key_room_102"), out _);
            Assert.AreEqual(ScenarioExecutionStatus.Succeeded, loop2Status);
        }

        [Test]
        public void Requirement10_RepeatedResetsDoNotCauseDuplicateCallbacks()
        {
            int resetCount = 0;
            void Handler() => resetCount++;

            _registry.OnResetCompleted += Handler;

            _registry.ResetAllToBaseline();
            _registry.ResetAllToBaseline();
            _registry.ResetAllToBaseline();

            Assert.AreEqual(3, resetCount, "Reset event should fire exactly once per ResetAllToBaseline() call.");

            _registry.OnResetCompleted -= Handler;
        }
    }
}
