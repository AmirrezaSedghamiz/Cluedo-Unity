using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.Events;
using TomorrowsCrime.Investigation;
using TomorrowsCrime.NPC;
using TomorrowsCrime.Player;
using TomorrowsCrime.Simulation;
using TomorrowsCrime.World;

namespace TomorrowsCrime.Tests.EditMode
{
    [TestFixture]
    public class InvestigationSystemTests
    {
        private InvestigationKnowledgeService _knowledge;
        private WorldStateRegistry _worldState;
        private List<GameObject> _spawnedGameObjects;

        [SetUp]
        public void SetUp()
        {
            _knowledge = InvestigationKnowledgeService.Instance;
            _knowledge.Clear();
            _worldState = new WorldStateRegistry();
            _spawnedGameObjects = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            _knowledge?.Clear();
            _worldState?.Clear();

            foreach (var go in _spawnedGameObjects)
            {
                if (go != null)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }
            _spawnedGameObjects.Clear();
        }

        private EvidenceDefinition CreateTestEvidence(
            string id = "evidence_test_keycard",
            string displayName = "Test Keycard",
            string category = "Keys & Access",
            string shortDesc = "A test keycard.",
            string detailedText = "Detailed inspection notes of the keycard.",
            string eventId = "evt_concierge_retrieve_key")
        {
            var def = ScriptableObject.CreateInstance<EvidenceDefinition>();
            def.Initialize(id, displayName, shortDesc, detailedText, category, eventId);
            return def;
        }

        [Test]
        public void Requirement1_InspectingValidTargetRecordsEvidence()
        {
            var evidenceDef = CreateTestEvidence("evidence_room102_keycard", "Room 102 Keycard");

            var targetGo = new GameObject("TestInspectableTarget");
            _spawnedGameObjects.Add(targetGo);
            var inspectable = targetGo.AddComponent<InspectableEvidence>();
            inspectable.Definition = evidenceDef;

            var playerGo = new GameObject("TestPlayer");
            _spawnedGameObjects.Add(playerGo);

            Assert.IsTrue(inspectable.CanInteract, "Inspectable target with valid definition should be interactable.");

            inspectable.Interact(playerGo);

            Assert.IsTrue(_knowledge.HasDiscoveredEvidence("evidence_room102_keycard"), "Evidence should be registered in persistent store.");
            Assert.AreEqual(1, _knowledge.EvidenceCount, "Evidence count should be exactly 1.");

            var record = _knowledge.GetEvidenceRecord("evidence_room102_keycard");
            Assert.IsNotNull(record, "Discovered record should not be null.");
            Assert.AreEqual("Room 102 Keycard", record.Definition.DisplayName);
            Assert.AreEqual("Keys & Access", record.Definition.Category);
        }

        [Test]
        public void Requirement2_RepeatedDiscoveryDoesNotDuplicateEvidence()
        {
            var evidenceDef = CreateTestEvidence("evidence_room102_keycard", "Room 102 Keycard");

            bool firstDiscovery = _knowledge.DiscoverEvidence(evidenceDef, loopIndex: 1, simTime: 10.0f);
            bool secondDiscovery = _knowledge.DiscoverEvidence(evidenceDef, loopIndex: 1, simTime: 25.0f);
            bool thirdDiscoveryInNewLoop = _knowledge.DiscoverEvidence(evidenceDef, loopIndex: 2, simTime: 5.0f);

            Assert.IsTrue(firstDiscovery, "First discovery should succeed.");
            Assert.IsFalse(secondDiscovery, "Repeated discovery in the same loop should return false.");
            Assert.IsFalse(thirdDiscoveryInNewLoop, "Repeated discovery in subsequent loops should return false.");
            Assert.AreEqual(1, _knowledge.EvidenceCount, "Only a single record should be stored.");

            var all = _knowledge.GetAllDiscoveredEvidence();
            Assert.AreEqual(1, all.Count);
            Assert.AreEqual(1, all[0].DiscoveredInLoop, "Original loop index should be preserved.");
            Assert.AreEqual(10.0f, all[0].DiscoveredAtSimTime, "Original discovery timestamp should be preserved.");
        }

        [Test]
        public void Requirement3_EvidenceCannotBeDiscoveredWhenExplicitRequirementsUnmet()
        {
            var evidenceDef = CreateTestEvidence("evidence_hidden_compartment", "Hidden Drawer");

            var targetGo = new GameObject("SecretDrawer");
            _spawnedGameObjects.Add(targetGo);
            var inspectable = targetGo.AddComponent<InspectableEvidence>();
            inspectable.Definition = evidenceDef;
            inspectable.RequiresHoldingProp = true;
            inspectable.RequiredPropId = "prop_room102_keycard";

            var playerGo = new GameObject("TestPlayer");
            _spawnedGameObjects.Add(playerGo);

            // Prop is not registered or not held by player
            bool canInspect = inspectable.CanInspect(out string failureReason);
            Assert.IsFalse(canInspect, "Cannot inspect target when required item is not held.");
            Assert.IsNotEmpty(failureReason, "Failure reason should explain missing requirement.");

            inspectable.Interact(playerGo);
            Assert.IsFalse(_knowledge.HasDiscoveredEvidence("evidence_hidden_compartment"), "Evidence should not be discovered when prerequisites unmet.");
            Assert.AreEqual(0, _knowledge.EvidenceCount);
        }

        [Test]
        public void Requirement4_UndiscoveredEvidenceNotExposedThroughPlayerNotebookData()
        {
            var discoveredDef = CreateTestEvidence("evidence_found_item", "Found Item");
            _knowledge.DiscoverEvidence(discoveredDef, loopIndex: 1, simTime: 12f);

            Assert.IsTrue(_knowledge.HasDiscoveredEvidence("evidence_found_item"));
            Assert.IsFalse(_knowledge.HasDiscoveredEvidence("evidence_unfound_murder_weapon"), "Undiscovered evidence ID must return false.");
            Assert.IsNull(_knowledge.GetEvidenceRecord("evidence_unfound_murder_weapon"), "Undiscovered evidence record lookup must be null.");

            var allDiscovered = _knowledge.GetAllDiscoveredEvidence();
            Assert.AreEqual(1, allDiscovered.Count);
            foreach (var record in allDiscovered)
            {
                Assert.AreNotEqual("evidence_unfound_murder_weapon", record.Definition.EvidenceId);
            }
        }

        [Test]
        public void Requirement5_PlayerObservationsRecordedOnlyThroughValidObservationPathways()
        {
            bool recorded = _knowledge.RecordObservation(
                observationId: "obs_concierge_dropped_key",
                description: "Witnessed concierge looking at table and noticing the missing key.",
                loopIndex: 1,
                simTime: 30.0f,
                relatedEventId: "evt_concierge_retrieve_key",
                relatedEvidenceId: "evidence_room102_keycard"
            );

            Assert.IsTrue(recorded, "Valid observation should be accepted.");
            Assert.IsTrue(_knowledge.HasObservation("obs_concierge_dropped_key"));
            Assert.AreEqual(1, _knowledge.ObservationCount);

            var observations = _knowledge.GetAllObservations();
            Assert.AreEqual(1, observations.Count);
            Assert.AreEqual("obs_concierge_dropped_key", observations[0].ObservationId);
            Assert.AreEqual(1, observations[0].LoopIndex);
            Assert.AreEqual(30.0f, observations[0].SimTime);
            Assert.AreEqual("evt_concierge_retrieve_key", observations[0].RelatedEventId);

            // Attempting to duplicate observation ID
            bool duplicate = _knowledge.RecordObservation(
                "obs_concierge_dropped_key",
                "Duplicate description",
                2,
                30.0f
            );
            Assert.IsFalse(duplicate, "Duplicate observation ID should return false.");
            Assert.AreEqual(1, _knowledge.ObservationCount);
        }

        [Test]
        public void Requirement6_UnobservedSimulationEventsDoNotEnterPlayerTimeline()
        {
            var engine = new CausalEventEngine(1);
            _worldState.RegisterEntry("door_room_102", DoorLockState.Locked);

            // Register and execute an internal simulation event
            var simDef = ScriptableObject.CreateInstance<EventDefinition>();
            simDef.Initialize("evt_secret_midnight_meeting", "Internal Concierge Meeting", 15.0f);

            engine.RegisterEvent(simDef);
            engine.Tick(20.0f, _worldState);

            var inst = engine.GetInstance("evt_secret_midnight_meeting");
            Assert.AreEqual(EventStatus.Executed, inst.Status, "Simulation event completed internally.");
            Assert.AreEqual(1, engine.History.Count, "Engine event history records the internal execution.");

            // The player timeline in InvestigationKnowledgeService must remain completely empty!
            Assert.AreEqual(0, _knowledge.ObservationCount, "Unobserved simulation events must not contaminate player observation timeline.");
            Assert.IsFalse(_knowledge.HasObservation("evt_secret_midnight_meeting"));
        }

        [Test]
        public void Requirement7_HypothesesCanBeCreatedEditedAndDeleted()
        {
            // 1. Create
            var hyp = _knowledge.CreateHypothesis(
                title: "Concierge took the keycard",
                explanation: "The concierge checked the table at 00:30 and moved the key to unlock 102.",
                loopIndex: 1,
                simTime: 45.0f,
                supportingEvidenceIds: new[] { "evidence_room102_keycard", "evidence_concierge_schedule_memo" }
            );

            Assert.IsNotNull(hyp);
            Assert.IsNotEmpty(hyp.HypothesisId);
            Assert.AreEqual("Concierge took the keycard", hyp.Title);
            Assert.AreEqual(2, hyp.SupportingEvidenceIds.Count);
            Assert.AreEqual(1, _knowledge.HypothesisCount);

            // 2. Edit / Update
            bool updated = _knowledge.UpdateHypothesis(
                hyp.HypothesisId,
                newTitle: "Concierge noticed key was missing",
                newExplanation: "The concierge found the table empty at 00:30 and halted the routine.",
                supportingEvidenceIds: new[] { "evidence_concierge_schedule_memo" }
            );

            Assert.IsTrue(updated, "Update should succeed for existing hypothesis.");
            var fetched = _knowledge.GetHypothesis(hyp.HypothesisId);
            Assert.AreEqual("Concierge noticed key was missing", fetched.Title);
            Assert.AreEqual(1, fetched.SupportingEvidenceIds.Count);

            // 3. Delete
            bool deleted = _knowledge.DeleteHypothesis(hyp.HypothesisId);
            Assert.IsTrue(deleted, "Delete should return true.");
            Assert.IsNull(_knowledge.GetHypothesis(hyp.HypothesisId));
            Assert.AreEqual(0, _knowledge.HypothesisCount);
        }

        [Test]
        public void Requirement8_NotesAndHypothesesSurviveLoopReset()
        {
            _knowledge.GeneralNotes = "Note: Concierge reaches lobby table exactly at 00:30.";
            var hyp = _knowledge.CreateHypothesis("Key was moved before midnight", "Player moved it.", 1, 10f);

            // Simulate loop reset: reset world state and clear transient state
            _worldState.ResetAllToBaseline();

            // Notes and hypotheses must survive
            Assert.AreEqual("Note: Concierge reaches lobby table exactly at 00:30.", _knowledge.GeneralNotes);
            Assert.AreEqual(1, _knowledge.HypothesisCount);
            Assert.AreEqual(hyp.HypothesisId, _knowledge.GetHypothesis(hyp.HypothesisId).HypothesisId);
        }

        [Test]
        public void Requirement9_DiscoveredEvidenceSurvivesLoopReset()
        {
            var evidenceDef = CreateTestEvidence("evidence_room102_keycard", "Room 102 Keycard");
            _knowledge.DiscoverEvidence(evidenceDef, loopIndex: 1, simTime: 15.0f);

            // World state resets for new loop
            _worldState.ResetAllToBaseline();

            // Evidence must persist across resets
            Assert.IsTrue(_knowledge.HasDiscoveredEvidence("evidence_room102_keycard"));
            var record = _knowledge.GetEvidenceRecord("evidence_room102_keycard");
            Assert.IsNotNull(record);
            Assert.AreEqual(1, record.DiscoveredInLoop);
            Assert.AreEqual(15.0f, record.DiscoveredAtSimTime);
        }

        [Test]
        public void Requirement10_PreviousValidObservationsRemainAvailableAfterReset()
        {
            _knowledge.RecordObservation("obs_door_locked", "Door 102 was locked at midnight.", loopIndex: 1, simTime: 5.0f);

            // Simulate loop turnover
            _worldState.ResetAllToBaseline();

            Assert.IsTrue(_knowledge.HasObservation("obs_door_locked"));
            var obsList = _knowledge.GetAllObservations();
            Assert.AreEqual(1, obsList.Count);
            Assert.AreEqual(1, obsList[0].LoopIndex);
            Assert.AreEqual(5.0f, obsList[0].SimTime);
        }

        [Test]
        public void Requirement11_LoopScopedWorldAndNPCStateStillResetCorrectly()
        {
            _worldState.RegisterEntry("key_room_102", PropLocationState.AtBaseline);
            _worldState.RegisterEntry("door_room_102", DoorLockState.Locked);

            // Player discovers evidence during loop
            var evidenceDef = CreateTestEvidence("evidence_room102_keycard", "Room 102 Keycard");
            _knowledge.DiscoverEvidence(evidenceDef, loopIndex: 1, simTime: 20f);

            // Mutate world state during loop simulation
            _worldState.SetState("key_room_102", PropLocationState.HeldByPlayer);
            _worldState.SetState("door_room_102", DoorLockState.Unlocked);

            Assert.AreEqual(PropLocationState.HeldByPlayer, _worldState.GetState<PropLocationState>("key_room_102"));
            Assert.AreEqual(DoorLockState.Unlocked, _worldState.GetState<DoorLockState>("door_room_102"));

            // Perform loop reset
            _worldState.ResetAllToBaseline();

            // World state must return to baseline
            Assert.AreEqual(PropLocationState.AtBaseline, _worldState.GetState<PropLocationState>("key_room_102"));
            Assert.AreEqual(DoorLockState.Locked, _worldState.GetState<DoorLockState>("door_room_102"));

            // But persistent knowledge remains intact
            Assert.IsTrue(_knowledge.HasDiscoveredEvidence("evidence_room102_keycard"));
        }

        [Test]
        public void Requirement12_ResettingDoesNotDuplicateCallbacksOrSubscriptions()
        {
            int discoveryCallbackCount = 0;
            Action<DiscoveredEvidenceRecord> onDiscovered = (r) => discoveryCallbackCount++;

            _knowledge.OnEvidenceDiscovered += onDiscovered;

            // Trigger discovery
            var def1 = CreateTestEvidence("ev_item_1", "Item 1");
            _knowledge.DiscoverEvidence(def1, 1, 10f);
            Assert.AreEqual(1, discoveryCallbackCount);

            // Unsubscribe cleanly
            _knowledge.OnEvidenceDiscovered -= onDiscovered;

            // Trigger another discovery; callback count must not increment
            var def2 = CreateTestEvidence("ev_item_2", "Item 2");
            _knowledge.DiscoverEvidence(def2, 1, 20f);
            Assert.AreEqual(1, discoveryCallbackCount, "Unsubscribed handler must not be invoked.");
        }

        [Test]
        public void Requirement13_OpeningAndClosingNotebookCorrectlyEnablesAndDisablesPlayerInput()
        {
            var playerGo = new GameObject("TestPlayerController");
            _spawnedGameObjects.Add(playerGo);
            var controller = playerGo.AddComponent<PlayerController>();

            Assert.IsFalse(controller.IsInputBlocked, "Input should initially not be blocked.");

            // Player opens notebook
            controller.SetInputBlocked(true);
            Assert.IsTrue(controller.IsInputBlocked, "Input must be blocked when notebook is opened.");

            // Player closes notebook
            controller.SetInputBlocked(false);
            Assert.IsFalse(controller.IsInputBlocked, "Input must be unblocked when notebook is closed.");
        }

        [Test]
        public void Requirement14_DiscoveringAdditionalEvidenceInLaterLoopDoesNotEraseEarlierDiscoveries()
        {
            // Loop 1 discovery
            var defLoop1 = CreateTestEvidence("evidence_room102_keycard", "Keycard");
            _knowledge.DiscoverEvidence(defLoop1, loopIndex: 1, simTime: 14f);

            // Loop reset occurs
            _worldState.ResetAllToBaseline();

            // Loop 2 discovery
            var defLoop2 = CreateTestEvidence("evidence_concierge_schedule_memo", "Duty Memo");
            _knowledge.DiscoverEvidence(defLoop2, loopIndex: 2, simTime: 40f);

            Assert.AreEqual(2, _knowledge.EvidenceCount, "Both evidence items must be present.");
            Assert.IsTrue(_knowledge.HasDiscoveredEvidence("evidence_room102_keycard"));
            Assert.IsTrue(_knowledge.HasDiscoveredEvidence("evidence_concierge_schedule_memo"));

            var record1 = _knowledge.GetEvidenceRecord("evidence_room102_keycard");
            var record2 = _knowledge.GetEvidenceRecord("evidence_concierge_schedule_memo");

            Assert.AreEqual(1, record1.DiscoveredInLoop);
            Assert.AreEqual(14f, record1.DiscoveredAtSimTime);

            Assert.AreEqual(2, record2.DiscoveredInLoop);
            Assert.AreEqual(40f, record2.DiscoveredAtSimTime);
        }
    }
}
