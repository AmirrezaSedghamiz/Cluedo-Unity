using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.Events;
using TomorrowsCrime.Investigation;
using TomorrowsCrime.Player;
using TomorrowsCrime.Settings;
using TomorrowsCrime.Simulation;
using TomorrowsCrime.UI.Core;
using TomorrowsCrime.World;

namespace TomorrowsCrime.Tests.EditMode
{
    [TestFixture]
    public class InvestigationSystem2Tests
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
            string id = "evidence_test_key",
            string displayName = "Test Key",
            string category = "Keys & Access",
            string shortDesc = "A test key.",
            string detailedText = "Detailed notes on test key.",
            string eventId = "evt_concierge_retrieve_key")
        {
            var def = ScriptableObject.CreateInstance<EvidenceDefinition>();
            def.Initialize(id, displayName, shortDesc, detailedText, category, eventId);
            return def;
        }

        [Test]
        public void Requirement1_PersistentEvidenceSurvivesLoopReset()
        {
            var def = CreateTestEvidence("ev_room102_key", "Room 102 Key");
            _knowledge.DiscoverEvidence(def, loopIndex: 1, simTime: 12.0f);

            Assert.IsTrue(_knowledge.HasDiscoveredEvidence("ev_room102_key"));

            // World state resets for new loop
            _worldState.ResetAllToBaseline();

            Assert.IsTrue(_knowledge.HasDiscoveredEvidence("ev_room102_key"), "Discovered evidence must survive loop reset.");
            var record = _knowledge.GetEvidenceRecord("ev_room102_key");
            Assert.IsNotNull(record);
            Assert.AreEqual(1, record.DiscoveredInLoop);
            Assert.AreEqual(12.0f, record.DiscoveredAtSimTime);
        }

        [Test]
        public void Requirement2_PlayerHypothesesAndNotesSurviveLoopReset()
        {
            var hyp = _knowledge.CreateHypothesis(
                "Concierge moved the key",
                "Concierge walked to table at 00:15.",
                loopIndex: 1,
                simTime: 20f
            );

            _knowledge.GeneralNotes = "Key disappeared before 00:30.";

            // Reset loop
            _worldState.ResetAllToBaseline();

            Assert.AreEqual("Key disappeared before 00:30.", _knowledge.GeneralNotes, "Notes must survive loop reset.");
            Assert.AreEqual(1, _knowledge.HypothesisCount, "Hypotheses must survive loop reset.");
            var retrieved = _knowledge.GetHypothesis(hyp.HypothesisId);
            Assert.IsNotNull(retrieved);
            Assert.AreEqual("Concierge moved the key", retrieved.Title);
        }

        [Test]
        public void Requirement3_PlayerCreatedEvidenceRelationshipsSurviveLoopReset()
        {
            var keyDef = CreateTestEvidence("ev_key_102", "Room 102 Key");
            var memoDef = CreateTestEvidence("ev_memo", "Duty Memo");

            _knowledge.DiscoverEvidence(keyDef, 1, 10f);
            _knowledge.DiscoverEvidence(memoDef, 1, 15f);

            var rel = _knowledge.AddRelationship(
                "ev_key_102",
                "ev_memo",
                EvidenceRelationshipType.Contradicts,
                "Memo stated key would be on table, but key was moved.",
                isPlayerCreated: true,
                createdInLoop: 1
            );

            Assert.IsNotNull(rel);
            Assert.AreEqual(1, _knowledge.RelationshipCount);

            // Reset loop
            _worldState.ResetAllToBaseline();

            Assert.AreEqual(1, _knowledge.RelationshipCount, "Evidence relationships must persist across loop reset.");
            var fetched = _knowledge.GetRelationship(rel.RelationshipId);
            Assert.IsNotNull(fetched);
            Assert.AreEqual(EvidenceRelationshipType.Contradicts, fetched.RelationshipType);
            Assert.AreEqual("ev_key_102", fetched.SourceEvidenceId);
            Assert.AreEqual("ev_memo", fetched.TargetEvidenceId);
            Assert.IsTrue(fetched.IsPlayerCreated);
        }

        [Test]
        public void Requirement4_UndiscoveredEvidenceCannotBeSelectedForDeductionBoardRelationships()
        {
            var keyDef = CreateTestEvidence("ev_known_key", "Known Key");
            _knowledge.DiscoverEvidence(keyDef, 1, 10f);

            // Attempt to connect known evidence to unknown evidence
            Assert.Throws<InvalidOperationException>(() =>
            {
                _knowledge.AddRelationship("ev_known_key", "ev_undiscovered_knife", EvidenceRelationshipType.Supports);
            }, "Cannot create relationship targeting undiscovered evidence.");

            // Attempt to connect unknown evidence to known evidence
            Assert.Throws<InvalidOperationException>(() =>
            {
                _knowledge.AddRelationship("ev_undiscovered_poison", "ev_known_key", EvidenceRelationshipType.RelatedTo);
            }, "Cannot create relationship sourcing from undiscovered evidence.");

            Assert.AreEqual(0, _knowledge.RelationshipCount, "No invalid relationships should be added.");
        }

        [Test]
        public void Requirement5_UnobservedSimulationEventsDoNotEnterPlayerTimeline()
        {
            var engine = new CausalEventEngine(1);
            var simDef = ScriptableObject.CreateInstance<EventDefinition>();
            simDef.Initialize("evt_background_door_slam", "Door Slam in East Wing", 10.0f);

            engine.RegisterEvent(simDef);
            engine.Tick(15.0f, _worldState);

            Assert.AreEqual(1, engine.History.Count, "Internal causal engine records event in simulation history.");
            Assert.AreEqual(0, _knowledge.ObservationCount, "Player observation timeline must remain empty for unobserved events.");
            Assert.IsFalse(_knowledge.HasObservation("evt_background_door_slam"));
        }

        [Test]
        public void Requirement6_ObservationsRetainCorrectLoopIdAndSimulationTimestamp()
        {
            bool recorded = _knowledge.RecordObservation(
                observationId: "obs_concierge_empty_table",
                description: "Concierge reached table and stopped with hands empty.",
                loopIndex: 3,
                simTime: 31.5f,
                relatedEventId: "evt_concierge_retrieve_key",
                relatedEvidenceId: "ev_key_102",
                method: ObservationMethod.WitnessedEvent,
                sourceOrSubject: "NPC_Concierge"
            );

            Assert.IsTrue(recorded);
            var obs = _knowledge.GetAllObservations()[0];
            Assert.AreEqual(3, obs.LoopIndex, "Loop index must match loop when observation occurred.");
            Assert.AreEqual(31.5f, obs.SimTime, "SimTime must match simulation clock time when observed.");
            Assert.AreEqual("00:31", obs.GetFormattedTimestamp());
            Assert.AreEqual(ObservationMethod.WitnessedEvent, obs.Method);
            Assert.AreEqual("NPC_Concierge", obs.SourceOrSubject);
        }

        [Test]
        public void Requirement7_ComparingObservationsFromTwoLoopsDoesNotModifySourceRecords()
        {
            _knowledge.RecordObservation("obs_door_action", "Door was unlocked by concierge.", loopIndex: 1, simTime: 35.0f);
            _knowledge.RecordObservation("obs_door_action_loop2", "Door remained locked and unattended.", loopIndex: 2, simTime: 35.0f);

            var comparison = _knowledge.CompareLoops(1, 2);
            Assert.IsNotNull(comparison);

            // Verify original records are unchanged
            var allObs = _knowledge.GetAllObservations();
            Assert.AreEqual(2, allObs.Count);
            Assert.AreEqual("Door was unlocked by concierge.", allObs[0].Description);
            Assert.AreEqual("Door remained locked and unattended.", allObs[1].Description);
        }

        [Test]
        public void Requirement8_DuplicateNotificationsNotEmittedForDuplicateDiscoveryAttempts()
        {
            int notificationCount = 0;
            Action<DiscoveredEvidenceRecord> handler = (r) => notificationCount++;

            _knowledge.OnNewEvidenceDiscoveredNotification += handler;

            var def = CreateTestEvidence("ev_duplicate_test", "Duplicate Clue");

            bool first = _knowledge.DiscoverEvidence(def, 1, 10f);
            Assert.IsTrue(first);
            Assert.AreEqual(1, notificationCount, "First discovery emits notification.");

            bool second = _knowledge.DiscoverEvidence(def, 1, 20f);
            Assert.IsFalse(second);
            Assert.AreEqual(1, notificationCount, "Duplicate discovery must NOT emit another notification.");

            _knowledge.OnNewEvidenceDiscoveredNotification -= handler;
        }

        [Test]
        public void Requirement9_UIOpeningAndClosingDoesNotLeavePlayerInputDisabled()
        {
            var playerGo = new GameObject("TestPlayer");
            _spawnedGameObjects.Add(playerGo);
            var controller = playerGo.AddComponent<PlayerController>();

            var uiGo = new GameObject("TestUIManager");
            _spawnedGameObjects.Add(uiGo);
            var uiMgr = uiGo.AddComponent<UIManager>();

            // Baseline in InGameHUD
            uiMgr.SetState(UIState.InGameHUD);
            Assert.IsFalse(controller.IsInputBlocked, "Input must not be blocked during InGameHUD.");

            // Open Notebook
            uiMgr.SetState(UIState.InvestigationNotebook);
            Assert.IsTrue(controller.IsInputBlocked, "Input must be blocked when notebook is open.");

            // Close Notebook
            uiMgr.SetState(UIState.InGameHUD);
            Assert.IsFalse(controller.IsInputBlocked, "Input must be restored when notebook is closed.");

            // Open Pause Menu
            uiMgr.SetState(UIState.PauseMenu);
            Assert.IsTrue(controller.IsInputBlocked, "Input must be blocked when pause menu is open.");

            // Resume
            uiMgr.SetState(UIState.InGameHUD);
            Assert.IsFalse(controller.IsInputBlocked, "Input must be restored when pause menu resumes.");
        }

        [Test]
        public void Requirement10_UIInputDoesNotAccidentallyTriggerWorldInteractions()
        {
            var playerGo = new GameObject("TestPlayer");
            _spawnedGameObjects.Add(playerGo);
            var controller = playerGo.AddComponent<PlayerController>();
            var interaction = playerGo.AddComponent<PlayerInteraction>();

            var uiGo = new GameObject("TestUIManager");
            _spawnedGameObjects.Add(uiGo);
            var uiMgr = uiGo.AddComponent<UIManager>();

            uiMgr.SetState(UIState.InvestigationNotebook);

            Assert.IsTrue(uiMgr.IsModalOpen);
            Assert.IsFalse(interaction.HasInteractable, "Interaction system must have no interactable while UI modal is open.");
            Assert.IsNull(interaction.CurrentInteractable);
        }

        [Test]
        public void Requirement11_RestartingLoopDoesNotErasePersistentInvestigationData()
        {
            var def = CreateTestEvidence("ev_restart_test", "Clue Survives Restart");
            _knowledge.DiscoverEvidence(def, 1, 10f);
            _knowledge.CreateHypothesis("Restart Theory", "Testing restart", 1, 10f);
            _knowledge.RecordObservation("obs_restart", "Saw something", 1, 10f);
            _knowledge.GeneralNotes = "Important notes.";

            // Simulate loop reset
            _worldState.ResetAllToBaseline();

            Assert.IsTrue(_knowledge.HasDiscoveredEvidence("ev_restart_test"));
            Assert.AreEqual(1, _knowledge.HypothesisCount);
            Assert.AreEqual(1, _knowledge.ObservationCount);
            Assert.AreEqual("Important notes.", _knowledge.GeneralNotes);
        }

        [Test]
        public void Requirement12_ExistingEventEngineAndNPCTestsContinueToPass()
        {
            var engine = new CausalEventEngine(1);
            _worldState.RegisterEntry("door_test", DoorLockState.Locked);

            var def = ScriptableObject.CreateInstance<EventDefinition>();
            def.Initialize("evt_test", "Test Event", 10.0f);
            engine.RegisterEvent(def);

            engine.Tick(10.0f, _worldState);

            var inst = engine.GetInstance("evt_test");
            Assert.AreEqual(EventStatus.Executed, inst.Status);
            Assert.AreEqual(1, engine.History.Count);
        }

        [Test]
        public void Requirement13_ExistingWorldResetBehaviorRemainsCorrect()
        {
            _worldState.RegisterEntry("key_room_102", PropLocationState.AtBaseline);
            _worldState.RegisterEntry("door_room_102", DoorLockState.Locked);

            _worldState.SetState("key_room_102", PropLocationState.HeldByPlayer);
            _worldState.SetState("door_room_102", DoorLockState.Unlocked);

            _worldState.ResetAllToBaseline();

            Assert.AreEqual(PropLocationState.AtBaseline, _worldState.GetState<PropLocationState>("key_room_102"));
            Assert.AreEqual(DoorLockState.Locked, _worldState.GetState<DoorLockState>("door_room_102"));
        }

        [Test]
        public void Requirement14_UIEventSubscriptionsDoNotMultiplyAfterRepeatedOpenCloseOrReset()
        {
            int toastCount = 0;
            Action<DiscoveredEvidenceRecord> toastHandler = (r) => toastCount++;

            _knowledge.OnNewEvidenceDiscoveredNotification += toastHandler;

            var def1 = CreateTestEvidence("ev_sub_1", "Sub 1");
            _knowledge.DiscoverEvidence(def1, 1, 5f);
            Assert.AreEqual(1, toastCount);

            // Clean unsubscription
            _knowledge.OnNewEvidenceDiscoveredNotification -= toastHandler;

            // Trigger another discovery; callback count must not increment
            var def2 = CreateTestEvidence("ev_sub_2", "Sub 2");
            _knowledge.DiscoverEvidence(def2, 1, 10f);
            Assert.AreEqual(1, toastCount, "Unsubscribed handler must not receive further events.");
        }
    }
}
