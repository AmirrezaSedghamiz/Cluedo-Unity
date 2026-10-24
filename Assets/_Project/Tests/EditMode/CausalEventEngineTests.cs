using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.Events;
using TomorrowsCrime.World;

namespace TomorrowsCrime.Tests.EditMode
{
    [TestFixture]
    public class CausalEventEngineTests
    {
        private CausalEventEngine _engine;
        private WorldStateRegistry _worldState;

        [SetUp]
        public void Setup()
        {
            _engine = new CausalEventEngine(1);
            _worldState = new WorldStateRegistry();
        }

        [TearDown]
        public void TearDown()
        {
            _engine?.Clear();
            _worldState?.Clear();
        }

        private class MockPrecondition : IEventPrecondition
        {
            public bool ShouldPass { get; set; }
            public string FailMessage { get; set; }
            public string Description => "Mock Precondition";

            public MockPrecondition(bool shouldPass, string failMessage = "Condition failed")
            {
                ShouldPass = shouldPass;
                FailMessage = failMessage;
            }

            public bool Evaluate(WorldStateRegistry worldState, CausalEventEngine engine, out string failureReason)
            {
                if (ShouldPass)
                {
                    failureReason = null;
                    return true;
                }

                failureReason = FailMessage;
                return false;
            }
        }

        private class MockEffect : IEventEffect
        {
            public bool Applied { get; private set; }
            public bool ShouldFail { get; set; }
            public string Description => "Mock Effect";

            public MockEffect(bool shouldFail = false)
            {
                ShouldFail = shouldFail;
            }

            public bool Apply(WorldStateRegistry worldState, out string failureReason)
            {
                if (ShouldFail)
                {
                    failureReason = "Effect application failed.";
                    return false;
                }

                Applied = true;
                failureReason = null;
                return true;
            }
        }

        [Test]
        public void Requirement1_EventExecutesWhenTimeReachedAndPreconditionsPass()
        {
            var def = ScriptableObject.CreateInstance<EventDefinition>();
            def.Initialize("test_event", "Test Event", 10.0f);
            var effect = new MockEffect();
            def.AddEffect(effect);
            _engine.RegisterEvent(def);

            _engine.Tick(10.0f, _worldState);

            var inst = _engine.GetInstance("test_event");
            Assert.AreEqual(EventStatus.Executed, inst.Status);
            Assert.IsTrue(effect.Applied);
            Assert.AreEqual(1, _engine.History.Count);
            Assert.AreEqual(EventStatus.Executed, _engine.History[0].Status);
        }

        [Test]
        public void Requirement2_EventRemainsPendingBeforeScheduledTime()
        {
            var def = ScriptableObject.CreateInstance<EventDefinition>();
            def.Initialize("test_event", "Test Event", 10.0f);
            var effect = new MockEffect();
            def.AddEffect(effect);
            _engine.RegisterEvent(def);

            _engine.Tick(5.0f, _worldState);

            var inst = _engine.GetInstance("test_event");
            Assert.AreEqual(EventStatus.Pending, inst.Status);
            Assert.IsFalse(effect.Applied);
            Assert.AreEqual(0, _engine.History.Count);
        }

        [Test]
        public void Requirement3_EventFailsWithExplanationWhenPreconditionFalse()
        {
            var def = ScriptableObject.CreateInstance<EventDefinition>();
            def.Initialize("test_event", "Test Event", 10.0f);
            def.AddPrecondition(new MockPrecondition(false, "Keycard is missing from table."));
            _engine.RegisterEvent(def);

            _engine.Tick(10.0f, _worldState);

            var inst = _engine.GetInstance("test_event");
            Assert.AreEqual(EventStatus.Failed, inst.Status);
            StringAssert.Contains("Keycard is missing from table", inst.ResultExplanation);
            Assert.AreEqual(1, _engine.History.Count);
            Assert.AreEqual(EventStatus.Failed, _engine.History[0].Status);
        }

        [Test]
        public void Requirement4_EventExecutesAtMostOncePerLoop()
        {
            var def = ScriptableObject.CreateInstance<EventDefinition>();
            def.Initialize("test_event", "Test Event", 10.0f);
            int applyCount = 0;
            def.AddEffect(new MockEffect());
            _engine.OnEventProcessed += _ => applyCount++;
            _engine.RegisterEvent(def);

            _engine.Tick(10.0f, _worldState);
            _engine.Tick(20.0f, _worldState);
            _engine.Tick(30.0f, _worldState);

            Assert.AreEqual(1, applyCount, "Event should execute at most once in a single simulation loop.");
        }

        [Test]
        public void Requirement5_EventsWithSameTimestampFollowDeterministicOrdering()
        {
            // Event B has priority 10
            var defB = ScriptableObject.CreateInstance<EventDefinition>();
            defB.Initialize("event_b", "Event B", 10.0f, priority: 10);

            // Event A has priority 5, ID "event_a"
            var defA = ScriptableObject.CreateInstance<EventDefinition>();
            defA.Initialize("event_a", "Event A", 10.0f, priority: 5);

            // Event C has priority 5, ID "event_c"
            var defC = ScriptableObject.CreateInstance<EventDefinition>();
            defC.Initialize("event_c", "Event C", 10.0f, priority: 5);

            // Register in random order
            _engine.RegisterEvent(defC);
            _engine.RegisterEvent(defA);
            _engine.RegisterEvent(defB);

            _engine.Tick(10.0f, _worldState);

            // Expected order: B (priority 10), then A (priority 5, id "event_a"), then C (priority 5, id "event_c")
            Assert.AreEqual(3, _engine.History.Count);
            Assert.AreEqual("event_b", _engine.History[0].EventId);
            Assert.AreEqual("event_a", _engine.History[1].EventId);
            Assert.AreEqual("event_c", _engine.History[2].EventId);
        }

        [Test]
        public void Requirement6_EventDoesNotExecuteBeforePendingPrerequisiteSucceeds()
        {
            var def1 = ScriptableObject.CreateInstance<EventDefinition>();
            def1.Initialize("prereq_event", "Prereq Event", 20.0f);

            var def2 = ScriptableObject.CreateInstance<EventDefinition>();
            def2.Initialize("dependent_event", "Dependent Event", 10.0f);
            def2.AddPrerequisite("prereq_event");

            _engine.RegisterEvent(def1);
            _engine.RegisterEvent(def2);
            _engine.ValidateGraph();

            // At 15s, def2 has reached its time, but def1 is still Pending (scheduled at 20s)
            _engine.Tick(15.0f, _worldState);

            Assert.AreEqual(EventStatus.Pending, _engine.GetInstance("dependent_event").Status);
            Assert.AreEqual(EventStatus.Pending, _engine.GetInstance("prereq_event").Status);

            // Advance to 20s: prereq executes first, then dependent executes!
            _engine.Tick(20.0f, _worldState);

            Assert.AreEqual(EventStatus.Executed, _engine.GetInstance("prereq_event").Status);
            Assert.AreEqual(EventStatus.Executed, _engine.GetInstance("dependent_event").Status);
        }

        [Test]
        public void Requirement7_TerminalUnsuccessfulPrerequisiteTriggersConfiguredDependencyPolicy()
        {
            var def1 = ScriptableObject.CreateInstance<EventDefinition>();
            def1.Initialize("failing_prereq", "Failing Prereq", 10.0f);
            def1.AddPrecondition(new MockPrecondition(false, "Simulated failure"));

            var defSkip = ScriptableObject.CreateInstance<EventDefinition>();
            defSkip.Initialize("dependent_skip", "Dependent Skip", 20.0f, failurePolicy: PrerequisiteFailurePolicy.Skip);
            defSkip.AddPrerequisite("failing_prereq");

            var defFail = ScriptableObject.CreateInstance<EventDefinition>();
            defFail.Initialize("dependent_fail", "Dependent Fail", 20.0f, failurePolicy: PrerequisiteFailurePolicy.Fail);
            defFail.AddPrerequisite("failing_prereq");

            _engine.RegisterEvent(def1);
            _engine.RegisterEvent(defSkip);
            _engine.RegisterEvent(defFail);
            _engine.ValidateGraph();

            _engine.Tick(20.0f, _worldState);

            Assert.AreEqual(EventStatus.Failed, _engine.GetInstance("failing_prereq").Status);
            Assert.AreEqual(EventStatus.Skipped, _engine.GetInstance("dependent_skip").Status);
            Assert.AreEqual(EventStatus.Failed, _engine.GetInstance("dependent_fail").Status);
        }

        [Test]
        public void Requirement8_FailedEventDoesNotPartiallyApplyRequiredEffects()
        {
            _worldState.RegisterEntry("door_102", DoorLockState.Locked);

            var def = ScriptableObject.CreateInstance<EventDefinition>();
            def.Initialize("event_with_failed_precondition", "Door Unlocker", 10.0f);
            def.AddPrecondition(new MockPrecondition(false, "Precondition failed"));
            def.AddEffect(new DoorStateEffect("door_102", DoorLockState.Unlocked, DoorOpenState.Open));

            _engine.RegisterEvent(def);
            _engine.Tick(10.0f, _worldState);

            Assert.AreEqual(EventStatus.Failed, _engine.GetInstance("event_with_failed_precondition").Status);
            Assert.AreEqual(DoorLockState.Locked, _worldState.GetState<DoorLockState>("door_102"),
                "Door state should remain locked when event preconditions fail.");
        }

        [Test]
        public void Requirement9_InvalidEventReferencesAreReported()
        {
            var def = ScriptableObject.CreateInstance<EventDefinition>();
            def.Initialize("bad_event", "Bad Event", 10.0f);
            def.AddPrerequisite("non_existent_prerequisite_id");

            _engine.RegisterEvent(def);

            var ex = Assert.Throws<InvalidOperationException>(() => _engine.ValidateGraph());
            StringAssert.Contains("non-existent prerequisite event", ex.Message);
        }

        [Test]
        public void Requirement10_DependencyCyclesAreDetected()
        {
            var defA = ScriptableObject.CreateInstance<EventDefinition>();
            defA.Initialize("event_a", "Event A", 10.0f);
            defA.AddPrerequisite("event_b");

            var defB = ScriptableObject.CreateInstance<EventDefinition>();
            defB.Initialize("event_b", "Event B", 10.0f);
            defB.AddPrerequisite("event_a");

            _engine.RegisterEvent(defA);
            _engine.RegisterEvent(defB);

            var ex = Assert.Throws<InvalidOperationException>(() => _engine.ValidateGraph());
            StringAssert.Contains("cycle detected", ex.Message.ToLower());
        }

        [Test]
        public void Requirement11_LoopResetRestoresEventRuntimeState()
        {
            var def = ScriptableObject.CreateInstance<EventDefinition>();
            def.Initialize("test_event", "Test Event", 10.0f);
            _engine.RegisterEvent(def);

            _engine.Tick(10.0f, _worldState);
            Assert.AreEqual(EventStatus.Executed, _engine.GetInstance("test_event").Status);

            // Reset for Loop 2
            _engine.ResetForNewLoop(2);

            var inst = _engine.GetInstance("test_event");
            Assert.AreEqual(EventStatus.Pending, inst.Status);
            Assert.IsNull(inst.ExecutionSimTime);
            Assert.AreEqual(2, inst.LoopIndex);
            Assert.AreEqual(0, _engine.History.Count);
        }

        [Test]
        public void Requirement12_EventsExecuteCorrectlyAgainInNewLoop()
        {
            var def = ScriptableObject.CreateInstance<EventDefinition>();
            def.Initialize("test_event", "Test Event", 10.0f);
            _engine.RegisterEvent(def);

            // Loop 1
            _engine.Tick(10.0f, _worldState);
            Assert.AreEqual(EventStatus.Executed, _engine.GetInstance("test_event").Status);

            // Loop 2
            _engine.ResetForNewLoop(2);
            _engine.Tick(10.0f, _worldState);

            Assert.AreEqual(EventStatus.Executed, _engine.GetInstance("test_event").Status);
            Assert.AreEqual(1, _engine.History.Count);
            Assert.AreEqual(2, _engine.History[0].LoopIndex);
        }

        [Test]
        public void Requirement13_MovingKeyChangesExpectedEventOutcome()
        {
            _worldState.RegisterEntry("key_room_102", PropLocationState.AtBaseline);

            var def = ScriptableObject.CreateInstance<EventDefinition>();
            def.Initialize("retrieve_key", "Retrieve Key", 30.0f);
            def.AddPrecondition(new KeyLocationPrecondition("key_room_102", PropLocationState.AtBaseline));
            _engine.RegisterEvent(def);

            // Loop 1: Key is at baseline -> Event succeeds
            _engine.Tick(30.0f, _worldState);
            Assert.AreEqual(EventStatus.Executed, _engine.GetInstance("retrieve_key").Status);

            // Loop 2: Reset & Player picks up key -> Event fails
            _engine.ResetForNewLoop(2);
            _worldState.SetState("key_room_102", PropLocationState.HeldByPlayer);

            _engine.Tick(30.0f, _worldState);

            var inst = _engine.GetInstance("retrieve_key");
            Assert.AreEqual(EventStatus.Failed, inst.Status);
            StringAssert.Contains("HeldByPlayer", inst.ResultExplanation);
        }

        [Test]
        public void Requirement14_LargeClockAdvanceDoesNotMissEligibleEvents()
        {
            var def1 = ScriptableObject.CreateInstance<EventDefinition>();
            def1.Initialize("ev_10", "Event 10", 10.0f);

            var def2 = ScriptableObject.CreateInstance<EventDefinition>();
            def2.Initialize("ev_30", "Event 30", 30.0f);

            var def3 = ScriptableObject.CreateInstance<EventDefinition>();
            def3.Initialize("ev_60", "Event 60", 60.0f);

            _engine.RegisterEvent(def1);
            _engine.RegisterEvent(def2);
            _engine.RegisterEvent(def3);

            // Single jump from 0s to 120s
            _engine.Tick(120.0f, _worldState);

            Assert.AreEqual(3, _engine.History.Count);
            Assert.AreEqual("ev_10", _engine.History[0].EventId);
            Assert.AreEqual("ev_30", _engine.History[1].EventId);
            Assert.AreEqual("ev_60", _engine.History[2].EventId);
        }

        [Test]
        public void Requirement15_ExactLoopBoundaryBehaviorMatchesDocumentedPolicy()
        {
            var defZero = ScriptableObject.CreateInstance<EventDefinition>();
            defZero.Initialize("ev_zero", "Event Zero", 0.0f);

            var defBoundary = ScriptableObject.CreateInstance<EventDefinition>();
            defBoundary.Initialize("ev_boundary", "Event Boundary", 300.0f);

            var defPast = ScriptableObject.CreateInstance<EventDefinition>();
            defPast.Initialize("ev_past", "Event Past Boundary", 300.1f);

            _engine.RegisterEvent(defZero);
            _engine.RegisterEvent(defBoundary);
            _engine.RegisterEvent(defPast);

            // Tick at 0s
            _engine.Tick(0.0f, _worldState);
            Assert.AreEqual(EventStatus.Executed, _engine.GetInstance("ev_zero").Status);
            Assert.AreEqual(EventStatus.Pending, _engine.GetInstance("ev_boundary").Status);

            // Advance to exact loop boundary (300.0s)
            _engine.Tick(300.0f, _worldState);
            Assert.AreEqual(EventStatus.Executed, _engine.GetInstance("ev_boundary").Status);
            Assert.AreEqual(EventStatus.Pending, _engine.GetInstance("ev_past").Status,
                "Events scheduled past 300.0s should remain Pending before loop reset.");
        }
    }
}
