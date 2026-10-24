using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.Events;
using TomorrowsCrime.NPC;
using TomorrowsCrime.World;

namespace TomorrowsCrime.Tests.EditMode
{
    [TestFixture]
    public class NPCSimulationTests
    {
        private NPCDefinition _definition;
        private List<ScheduleActivity> _activities;

        [SetUp]
        public void SetUp()
        {
            _definition = ScriptableObject.CreateInstance<NPCDefinition>();
            _definition.Initialize(
                "npc_concierge",
                "Concierge Pierre",
                new Vector3(0f, 1.0f, 5.5f),
                Quaternion.Euler(0f, 180f, 0f),
                speed: 3.0f,
                stopDist: 0.8f,
                viewRange: 8.0f,
                fov: 90.0f
            );

            _activities = new List<ScheduleActivity>
            {
                new ScheduleActivity(ActivityType.WaitAtPost, 0f, 15f, "LobbyDesk", null, "Waiting at desk"),
                new ScheduleActivity(ActivityType.RetrieveKey, 15f, 30f, "LobbyTable", "concierge_retrieves_key", "Retrieving key"),
                new ScheduleActivity(ActivityType.UnlockDoor, 30f, 45f, "Room102Door", "concierge_unlocks_room102", "Unlocking door"),
                new ScheduleActivity(ActivityType.WaitAtPost, 45f, 300f, "Room102Door", null, "Stationed at room")
            };

            foreach (var act in _activities)
            {
                _definition.AddActivity(act);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (_definition != null)
            {
                UnityEngine.Object.DestroyImmediate(_definition);
            }
        }

        [Test]
        public void Requirement1_CharacterStartsInConfiguredInitialState()
        {
            var state = new NPCRuntimeState(_definition.CharacterId, 1);

            Assert.AreEqual("npc_concierge", state.CharacterId);
            Assert.AreEqual(NPCBehaviorState.Idle, state.BehaviorState);
            Assert.AreEqual(NPCNavigationStatus.Idle, state.NavigationStatus);
            Assert.IsFalse(state.HasNoticedMissingKey);
            Assert.AreEqual(1, state.LoopIndex);
            Assert.IsNull(state.CurrentActivity);
            StringAssert.Contains("baseline", state.LastDecisionReason.ToLower());
        }

        [Test]
        public void Requirement2_ScheduleEvaluationReturnsExpectedActivityForGivenSimulationTime()
        {
            var evaluator = _definition.CreateEvaluator();

            var act1 = evaluator.GetActivityForTime(5.0f);
            Assert.AreEqual(ActivityType.WaitAtPost, act1.ActivityType);
            Assert.AreEqual("LobbyDesk", act1.DestinationKey);

            var act2 = evaluator.GetActivityForTime(20.0f);
            Assert.AreEqual(ActivityType.RetrieveKey, act2.ActivityType);
            Assert.AreEqual("LobbyTable", act2.DestinationKey);

            var act3 = evaluator.GetActivityForTime(35.0f);
            Assert.AreEqual(ActivityType.UnlockDoor, act3.ActivityType);
            Assert.AreEqual("Room102Door", act3.DestinationKey);

            var act4 = evaluator.GetActivityForTime(120.0f);
            Assert.AreEqual(ActivityType.WaitAtPost, act4.ActivityType);
            Assert.AreEqual("Room102Door", act4.DestinationKey);
        }

        [Test]
        public void Requirement3_ScheduleBoundariesFollowDocumentedPolicy()
        {
            var evaluator = _definition.CreateEvaluator();

            // Interval: [0, 15)
            Assert.AreEqual(ActivityType.WaitAtPost, evaluator.GetActivityForTime(0.0f).ActivityType, "At exact 0.0s, should be WaitAtPost");
            Assert.AreEqual(ActivityType.WaitAtPost, evaluator.GetActivityForTime(14.99f).ActivityType, "At 14.99s, should be WaitAtPost");

            // Interval: [15, 30)
            Assert.AreEqual(ActivityType.RetrieveKey, evaluator.GetActivityForTime(15.0f).ActivityType, "At exact 15.0s, should transition to RetrieveKey");
            Assert.AreEqual(ActivityType.RetrieveKey, evaluator.GetActivityForTime(29.99f).ActivityType, "At 29.99s, should be RetrieveKey");

            // Interval: [30, 45)
            Assert.AreEqual(ActivityType.UnlockDoor, evaluator.GetActivityForTime(30.0f).ActivityType, "At exact 30.0s, should transition to UnlockDoor");

            // Loop Boundary: 300.0s
            Assert.AreEqual(ActivityType.WaitAtPost, evaluator.GetActivityForTime(300.0f).ActivityType, "At 300.0s (loop boundary), returns final activity");
        }

        [Test]
        public void Requirement4_ScheduleUsesSimulationTimeRatherThanWallClockTime()
        {
            var evaluator = _definition.CreateEvaluator();

            // Advancing purely through simulated float seconds
            float simulatedClock = 0.0f;
            Assert.AreEqual(ActivityType.WaitAtPost, evaluator.GetActivityForTime(simulatedClock).ActivityType);

            simulatedClock += 15.0f;
            Assert.AreEqual(ActivityType.RetrieveKey, evaluator.GetActivityForTime(simulatedClock).ActivityType);

            simulatedClock += 15.0f;
            Assert.AreEqual(ActivityType.UnlockDoor, evaluator.GetActivityForTime(simulatedClock).ActivityType);

            // Fast forward 200 simulation seconds
            simulatedClock += 200.0f;
            Assert.AreEqual(ActivityType.WaitAtPost, evaluator.GetActivityForTime(simulatedClock).ActivityType);
        }

        [Test]
        public void Requirement5_MissingKeyConditionProducesIntendedResponse()
        {
            var activity = new ScheduleActivity(ActivityType.RetrieveKey, 15f, 30f, "LobbyTable");

            // Key is missing (HeldByPlayer)
            var decision = NPCController.EvaluateHighLevelDecision(
                activity,
                PropLocationState.HeldByPlayer,
                isPathReachable: true,
                previouslyNoticedMissingKey: false,
                out string reason
            );

            Assert.AreEqual(NPCBehaviorState.Investigating, decision);
            StringAssert.Contains("missing", reason.ToLower());

            // Subsequent UnlockDoor activity should now transition to Waiting
            var unlockActivity = new ScheduleActivity(ActivityType.UnlockDoor, 30f, 45f, "Room102Door");
            var followUpDecision = NPCController.EvaluateHighLevelDecision(
                unlockActivity,
                PropLocationState.HeldByPlayer,
                isPathReachable: true,
                previouslyNoticedMissingKey: true,
                out string followUpReason
            );

            Assert.AreEqual(NPCBehaviorState.Waiting, followUpDecision);
            StringAssert.Contains("cannot unlock door without", followUpReason.ToLower());
        }

        [Test]
        public void Requirement6_NPCDoesNotGainKnowledgeOfPlayerActionsItCouldNotPerceive()
        {
            Vector3 conciergePos = new Vector3(0f, 1.0f, 5.5f);
            Vector3 conciergeForward = new Vector3(0f, 0f, 1f); // Facing North toward wall

            // Player takes key at table behind concierge (distance 3.5m < 8m viewRange)
            Vector3 tablePos = new Vector3(0f, 1.0f, 2.0f); // South of concierge (180 degrees behind forward)

            bool canSeeBehind = NPCPerception.EvaluateSensoryCone(
                conciergePos,
                conciergeForward,
                tablePos,
                viewRange: 8.0f,
                fieldOfView: 90.0f,
                out string failReason
            );

            Assert.IsFalse(canSeeBehind, "NPC should not perceive player actions occurring behind its back.");
            StringAssert.Contains("field of view", failReason.ToLower());

            // Also test out of range
            Vector3 farPos = new Vector3(0f, 1.0f, 25.0f);
            bool canSeeFar = NPCPerception.EvaluateSensoryCone(
                conciergePos,
                conciergeForward,
                farPos,
                viewRange: 8.0f,
                fieldOfView: 90.0f,
                out string outOfRangeReason
            );

            Assert.IsFalse(canSeeFar, "NPC should not perceive actions beyond sensory view range.");
            StringAssert.Contains("range", outOfRangeReason.ToLower());
        }

        [Test]
        public void Requirement7_UnreachableDestinationProducesDefinedBlockedOrRecoveryState()
        {
            var activity = new ScheduleActivity(ActivityType.Patrol, 10f, 30f, "ServiceDoor");

            var decision = NPCController.EvaluateHighLevelDecision(
                activity,
                PropLocationState.AtBaseline,
                isPathReachable: false,
                previouslyNoticedMissingKey: false,
                out string reason
            );

            Assert.AreEqual(NPCBehaviorState.Blocked, decision);
            StringAssert.Contains("unreachable", reason.ToLower());
        }

        [Test]
        public void Requirement8_ResetRestoresInitialNPCStateAndScheduleProgress()
        {
            var state = new NPCRuntimeState(_definition.CharacterId, 1);

            // Mutate runtime state during loop 1
            state.TransitionBehavior(NPCBehaviorState.Moving, "Moving to table");
            state.SetNavigationStatus(NPCNavigationStatus.Traversing);
            state.MarkMissingKeyNoticed("Key was missing!");

            Assert.AreEqual(NPCBehaviorState.Investigating, state.BehaviorState);
            Assert.IsTrue(state.HasNoticedMissingKey);

            // Trigger reset for Loop 2
            state.ResetForNewLoop(2);

            Assert.AreEqual(NPCBehaviorState.Idle, state.BehaviorState);
            Assert.AreEqual(NPCNavigationStatus.Idle, state.NavigationStatus);
            Assert.IsFalse(state.HasNoticedMissingKey);
            Assert.AreEqual(2, state.LoopIndex);
            StringAssert.Contains("Loop 2", state.LastDecisionReason);
        }

        [Test]
        public void Requirement9_ResetClearsStaleNavigationIntentions()
        {
            var state = new NPCRuntimeState(_definition.CharacterId, 1);
            var activity = new ScheduleActivity(ActivityType.RetrieveKey, 15f, 30f, "LobbyTable");

            state.SetCurrentActivity(activity, "LobbyTable", new Vector3(0f, 0f, -1.8f), "Traversing");
            state.SetNavigationStatus(NPCNavigationStatus.Traversing);

            Assert.AreEqual("LobbyTable", state.CurrentDestinationKey);
            Assert.AreEqual(NPCNavigationStatus.Traversing, state.NavigationStatus);

            state.ResetForNewLoop(2);

            Assert.AreEqual(string.Empty, state.CurrentDestinationKey);
            Assert.AreEqual(Vector3.zero, state.CurrentDestinationPosition);
            Assert.AreEqual(NPCNavigationStatus.Idle, state.NavigationStatus);
            Assert.IsNull(state.CurrentActivity);
        }

        [Test]
        public void Requirement10_RepeatedResetsDoNotDuplicateEventCallbacks()
        {
            var state = new NPCRuntimeState(_definition.CharacterId, 1);
            int callbackCount = 0;
            state.OnStateChanged += _ => callbackCount++;

            state.ResetForNewLoop(2);
            state.ResetForNewLoop(3);
            state.ResetForNewLoop(4);

            // Each reset should invoke exactly 1 state change notification, total 3
            Assert.AreEqual(3, callbackCount);
        }

        [Test]
        public void Requirement11_NPCActionsIntegrateWithCausalEventEngineWithoutDuplicateExecution()
        {
            var engine = new CausalEventEngine();
            var worldState = new WorldStateRegistry();
            worldState.RegisterEntry("key_room_102", PropLocationState.AtBaseline);

            var def = ScriptableObject.CreateInstance<EventDefinition>();
            def.Initialize("concierge_retrieves_key", "Retrieve Key", 30.0f);
            def.AddPrecondition(new KeyLocationPrecondition("key_room_102", PropLocationState.AtBaseline));
            engine.RegisterEvent(def);

            int eventExecutionCount = 0;
            engine.OnEventProcessed += rec =>
            {
                if (rec.EventId == "concierge_retrieves_key" && rec.Status == EventStatus.Executed)
                {
                    eventExecutionCount++;
                }
            };

            // Simulating NPC reaching table at 30.0s
            engine.Tick(30.0f, worldState);
            // Simulating follow-up ticks while NPC remains at destination
            engine.Tick(31.0f, worldState);
            engine.Tick(32.0f, worldState);

            Assert.AreEqual(1, eventExecutionCount, "Causal event must execute exactly once per loop.");
            Assert.AreEqual(EventStatus.Executed, engine.GetInstance("concierge_retrieves_key").Status);
        }

        [Test]
        public void Requirement12_IdenticalInitialConditionsAndActionSequencesProduceIdenticalDecisions()
        {
            var activity = new ScheduleActivity(ActivityType.RetrieveKey, 15f, 30f, "LobbyTable");

            // Run 1: Key at baseline
            var decision1 = NPCController.EvaluateHighLevelDecision(
                activity,
                PropLocationState.AtBaseline,
                isPathReachable: true,
                previouslyNoticedMissingKey: false,
                out string reason1
            );

            // Run 2: Exact same initial conditions
            var decision2 = NPCController.EvaluateHighLevelDecision(
                activity,
                PropLocationState.AtBaseline,
                isPathReachable: true,
                previouslyNoticedMissingKey: false,
                out string reason2
            );

            Assert.AreEqual(decision1, decision2);
            Assert.AreEqual(reason1, reason2);

            // Run 3: Key missing
            var decision3 = NPCController.EvaluateHighLevelDecision(
                activity,
                PropLocationState.HeldByPlayer,
                isPathReachable: true,
                previouslyNoticedMissingKey: false,
                out string reason3
            );

            // Run 4: Key missing with exact same condition
            var decision4 = NPCController.EvaluateHighLevelDecision(
                activity,
                PropLocationState.HeldByPlayer,
                isPathReachable: true,
                previouslyNoticedMissingKey: false,
                out string reason4
            );

            Assert.AreEqual(decision3, decision4);
            Assert.AreEqual(reason3, reason4);
            Assert.AreEqual(NPCBehaviorState.Investigating, decision3);
        }
    }
}
