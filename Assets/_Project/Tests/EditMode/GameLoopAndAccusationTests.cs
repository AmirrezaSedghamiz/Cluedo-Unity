using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using TomorrowsCrime.Audio;
using TomorrowsCrime.Core;
using TomorrowsCrime.Investigation;
using TomorrowsCrime.NPC;
using TomorrowsCrime.Procedural.Core;
using TomorrowsCrime.Procedural.Mystery;
using TomorrowsCrime.Procedural.NPCs;
using TomorrowsCrime.Simulation;
using TomorrowsCrime.UI.Core;
using TomorrowsCrime.World;

namespace TomorrowsCrime.Tests.EditMode
{
    [TestFixture]
    public class GameLoopAndAccusationTests
    {
        private List<GameObject> _spawnedObjects;
        private InvestigationKnowledgeService _knowledge;

        [SetUp]
        public void SetUp()
        {
            _spawnedObjects = new List<GameObject>();
            _knowledge = InvestigationKnowledgeService.Instance;
            _knowledge.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            _knowledge?.Clear();
            foreach (var go in _spawnedObjects)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _spawnedObjects.Clear();
        }

        [Test]
        public void CaseAccusationService_BaselineCorrectAccusation_ResolvesCaseWithVictory()
        {
            var go = new GameObject("Test_AccusationService");
            _spawnedObjects.Add(go);
            var service = go.AddComponent<CaseAccusationService>();

            // Baseline Ground Truth: Concierge Pierre, Meat Cleaver, Room 102 Suite, Blackmail Silence
            var result = service.FormulateAccusation(
                "Concierge Pierre",
                "Heavy Meat Cleaver",
                "Room 102 Suite",
                "Blackmail Silence"
            );

            Assert.IsTrue(result.IsCorrect, "Correct deduction must succeed.");
            Assert.IsTrue(result.IsCulpritCorrect, "Culprit should match Pierre.");
            Assert.IsTrue(result.IsWeaponCorrect, "Weapon should match SharpCleaver.");
            Assert.IsTrue(result.IsRoomCorrect, "Crime scene should match Room 102.");
            Assert.IsTrue(result.IsMotiveCorrect, "Motive should match Blackmail.");
            Assert.That(result.Title, Does.Contain("CASE SOLVED"));
            Assert.That(result.DetailedResolution, Does.Contain("Pierre"));
            Assert.Greater(CaseAccusationService.CasesSolvedCount, 0);
        }

        [Test]
        public void CaseAccusationService_WrongSuspect_ReturnsFlawAndRebuttal()
        {
            var go = new GameObject("Test_AccusationService");
            _spawnedObjects.Add(go);
            var service = go.AddComponent<CaseAccusationService>();

            var result = service.FormulateAccusation(
                "Lord Harrington",
                "Heavy Meat Cleaver",
                "Room 102 Suite",
                "Blackmail Silence"
            );

            Assert.IsFalse(result.IsCorrect, "Incorrect suspect must fail deduction.");
            Assert.IsFalse(result.IsCulpritCorrect, "Lord Harrington is not the culprit.");
            Assert.IsTrue(result.IsWeaponCorrect);
            Assert.IsTrue(result.IsRoomCorrect);
            Assert.That(result.Title, Does.Contain("ACCUSATION FAILED"));
            Assert.That(result.RebuttalReason, Does.Contain("alibi"));
        }

        [Test]
        public void CaseAccusationService_WrongWeapon_IdentifiesForensicMismatch()
        {
            var go = new GameObject("Test_AccusationService");
            _spawnedObjects.Add(go);
            var service = go.AddComponent<CaseAccusationService>();

            var result = service.FormulateAccusation(
                "Concierge Pierre",
                "Amber Alkaloid Poison Vial",
                "Room 102 Suite",
                "Blackmail Silence"
            );

            Assert.IsFalse(result.IsCorrect, "Wrong weapon must fail accusation.");
            Assert.IsTrue(result.IsCulpritCorrect);
            Assert.IsFalse(result.IsWeaponCorrect, "Poison does not match cleaver murder.");
            Assert.That(result.RebuttalReason, Does.Contain("forensic markings"));
        }

        [Test]
        public void CaseAccusationService_CustomGroundTruth_EvaluatesAccurately()
        {
            var go = new GameObject("Test_AccusationService");
            _spawnedObjects.Add(go);
            var service = go.AddComponent<CaseAccusationService>();

            var customTruth = new MysteryGroundTruth
            {
                CaseTitle = "The Arsenic Souffle",
                CulpritId = "npc_chef_marcel",
                CulpritName = "Chef Marcel Laurent",
                Method = MurderMethod.PoisonTincture,
                CrimeSceneRoomId = "Kitchen & Pantry",
                Motive = "Financial Embezzlement"
            };

            service.SetGroundTruth(customTruth);

            var result = service.FormulateAccusation(
                "Chef Marcel Laurent",
                "Amber Alkaloid Poison Vial",
                "Kitchen & Pantry",
                "Financial Embezzlement"
            );

            Assert.IsTrue(result.IsCorrect, "Custom ground truth accusation must evaluate correctly.");
            Assert.IsTrue(result.IsCulpritCorrect);
            Assert.IsTrue(result.IsWeaponCorrect);
            Assert.IsTrue(result.IsRoomCorrect);
            Assert.That(result.DetailedResolution, Does.Contain("Chef Marcel Laurent"));
        }

        [Test]
        public void ProceduralAudioEngine_AllWaveformGenerators_CreateValidAudioClips()
        {
            // Verify Footsteps
            var footstepHardwood = ProceduralAudioEngine.CreateFootstepClip(false, false);
            var footstepSprintCarpet = ProceduralAudioEngine.CreateFootstepClip(true, true);
            Assert.IsNotNull(footstepHardwood);
            Assert.IsNotNull(footstepSprintCarpet);
            Assert.Greater(footstepHardwood.samples, 0);

            // Verify Clock
            var clockTick = ProceduralAudioEngine.CreateClockTickClip(false);
            var clockTock = ProceduralAudioEngine.CreateClockTickClip(true);
            Assert.IsNotNull(clockTick);
            Assert.IsNotNull(clockTock);
            Assert.AreEqual(44100, clockTick.frequency);

            // Verify Midnight Bell
            var bell = ProceduralAudioEngine.CreateMidnightBellChime();
            Assert.IsNotNull(bell);
            Assert.Greater(bell.length, 1.0f);

            // Verify Clue & UI stingers
            var rustle = ProceduralAudioEngine.CreatePaperRustleClip();
            var chime = ProceduralAudioEngine.CreateEvidenceDiscoveryChime();
            var doorCreak = ProceduralAudioEngine.CreateDoorCreakClip();
            var doorLatch = ProceduralAudioEngine.CreateLockClickClip();
            var victory = ProceduralAudioEngine.CreateVictoryStinger();
            var defeat = ProceduralAudioEngine.CreateDefeatStinger();
            var rewind = ProceduralAudioEngine.CreateLoopRewindSweep();

            Assert.IsNotNull(rustle);
            Assert.IsNotNull(chime);
            Assert.IsNotNull(doorCreak);
            Assert.IsNotNull(doorLatch);
            Assert.IsNotNull(victory);
            Assert.IsNotNull(defeat);
            Assert.IsNotNull(rewind);
            Assert.Greater(victory.length, 0.5f);
        }

        [Test]
        public void NPCController_DialogueInteraction_UpdatesStateProperly()
        {
            var go = new GameObject("Test_NPC");
            _spawnedObjects.Add(go);

            var npc = go.AddComponent<NPCController>();
            var profile = new NPCProfile("npc_dr_sterling", "Dr. Alistair Sterling", NPCRole.DoctorGuest, 48, "Room 101");
            npc.Profile = profile;

            Assert.AreEqual("Dr. Alistair Sterling", npc.CharacterDisplayName);
            Assert.AreEqual("DoctorGuest", npc.CharacterRole);
            Assert.That(npc.InteractionPrompt, Does.Contain("Question Dr. Alistair Sterling"));
            Assert.IsTrue(npc.CanInteract);

            var playerGo = new GameObject("Test_Player");
            _spawnedObjects.Add(playerGo);
            playerGo.transform.position = new Vector3(0f, 0f, 3f);

            npc.Interact(playerGo);
            // End conversation
            npc.EndConversation();
        }

        [Test]
        public void CaseAccusationService_OptionLists_ContainStandardCluedoElements()
        {
            var go = new GameObject("Test_AccusationService");
            _spawnedObjects.Add(go);
            var service = go.AddComponent<CaseAccusationService>();

            var suspects = service.GetKnownSuspects();
            var weapons = service.GetKnownWeapons();
            var rooms = service.GetKnownCrimeScenes();
            var motives = service.GetKnownMotives();

            Assert.IsNotEmpty(suspects);
            Assert.IsNotEmpty(weapons);
            Assert.IsNotEmpty(rooms);
            Assert.IsNotEmpty(motives);

            Assert.That(suspects, Does.Contain("Concierge Pierre"));
            Assert.That(rooms, Does.Contain("Room 102 Suite"));
        }

        [Test]
        public void InteractableDoor_WithKeycardHeld_UnlocksAndOpens()
        {
            var worldMgrGo = new GameObject("Test_WorldStateMgr");
            _spawnedObjects.Add(worldMgrGo);
            var worldMgr = worldMgrGo.AddComponent<WorldStateManager>();

            var doorGo = new GameObject("Test_Door");
            _spawnedObjects.Add(doorGo);
            var door = doorGo.AddComponent<InteractableDoor>();
            door.RequiredKeyId = "key_room_102";

            var registry = new WorldStateRegistry();
            door.WorldStateRegistryOverride = registry;

            var playerGo = new GameObject("Test_Player");
            _spawnedObjects.Add(playerGo);

            // Initially locked without keycard
            Assert.AreEqual(DoorLockState.Locked, door.LockState);
            Assert.AreEqual(DoorOpenState.Closed, door.OpenState);
            Assert.That(door.InteractionPrompt, Does.Contain("Locked"));

            door.Interact(playerGo);
            Assert.AreEqual(DoorLockState.Locked, door.LockState, "Door must remain locked when player lacks key.");

            // Player acquires keycard
            registry.RegisterEntry("key_room_102", PropLocationState.HeldByPlayer);

            Assert.That(door.InteractionPrompt, Does.Contain("Unlock"));

            // Interacting now unlocks and opens
            door.Interact(playerGo);
            Assert.AreEqual(DoorLockState.Unlocked, door.LockState);
            Assert.AreEqual(DoorOpenState.Open, door.OpenState);

            // Loop reset relocks and closes
            door.OnLoopReset();
            Assert.AreEqual(DoorLockState.Locked, door.LockState);
            Assert.AreEqual(DoorOpenState.Closed, door.OpenState);
        }
    }
}
