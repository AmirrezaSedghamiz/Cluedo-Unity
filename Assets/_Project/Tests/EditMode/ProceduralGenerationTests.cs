using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.Investigation;
using TomorrowsCrime.NPC;
using TomorrowsCrime.World;
using TomorrowsCrime.Procedural.Core;
using TomorrowsCrime.Procedural.Hotel;
using TomorrowsCrime.Procedural.NPCs;
using TomorrowsCrime.Procedural.Objects;
using TomorrowsCrime.Procedural.Mystery;
using TomorrowsCrime.Procedural.Validation;

namespace TomorrowsCrime.Tests.EditMode
{
    [TestFixture]
    public class ProceduralGenerationTests
    {
        [SetUp]
        public void SetUp()
        {
            InvestigationKnowledgeService.Instance.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            InvestigationKnowledgeService.Instance.Clear();
        }

        [Test]
        public void Seed_ReproducesIdenticalGenerationOutput()
        {
            var seedA = new InvestigationSeed(424242);
            var seedB = new InvestigationSeed(424242);

            var invA = GenerationPipeline.GenerateInvestigation(seedA);
            var invB = GenerationPipeline.GenerateInvestigation(seedB);

            Assert.AreEqual(invA.GroundTruth.VictimId, invB.GroundTruth.VictimId);
            Assert.AreEqual(invA.GroundTruth.CulpritId, invB.GroundTruth.CulpritId);
            Assert.AreEqual(invA.GroundTruth.Motive, invB.GroundTruth.Motive);
            Assert.AreEqual(invA.GroundTruth.Method, invB.GroundTruth.Method);
            Assert.AreEqual(invA.GroundTruth.MurderWeaponObjectId, invB.GroundTruth.MurderWeaponObjectId);
            Assert.AreEqual(invA.GroundTruth.MurderTimestamp, invB.GroundTruth.MurderTimestamp, 0.001f);
        }

        [Test]
        public void Seed_DifferentSeedsProduceMeaningfulVariation()
        {
            var seedA = new InvestigationSeed(1001);
            var seedB = new InvestigationSeed(9999);

            var invA = GenerationPipeline.GenerateInvestigation(seedA);
            var invB = GenerationPipeline.GenerateInvestigation(seedB);

            // Assert that across different seeds, at least one fundamental mystery element varies
            bool differs = invA.GroundTruth.VictimId != invB.GroundTruth.VictimId ||
                           invA.GroundTruth.CulpritId != invB.GroundTruth.CulpritId ||
                           invA.GroundTruth.Method != invB.GroundTruth.Method ||
                           invA.GroundTruth.Motive != invB.GroundTruth.Motive;

            Assert.IsTrue(differs, "Different seeds must produce different mystery parameters.");
        }

        [Test]
        public void Layout_SatisfiesMinimumRoomAndFloorRequirements()
        {
            var rooms = HotelLayoutTemplate.CreateMeridianHouseLayout();

            Assert.GreaterOrEqual(rooms.Count, 10, "Hotel layout must contain at least 10 distinct rooms.");

            var floors = new HashSet<int>();
            foreach (var r in rooms)
            {
                floors.Add(r.FloorIndex);
            }

            Assert.GreaterOrEqual(floors.Count, 2, "Hotel layout must span at least 2 distinct floors.");
            Assert.IsTrue(floors.Contains(0), "Must contain Ground Floor (0).");
            Assert.IsTrue(floors.Contains(1), "Must contain Upper Floor (1).");
        }

        [Test]
        public void Layout_EveryRoomHasValidIdAndType()
        {
            var rooms = HotelLayoutTemplate.CreateMeridianHouseLayout();
            var roomIds = new HashSet<string>();

            foreach (var r in rooms)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(r.RoomId), "Room ID must not be empty.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(r.DisplayName), "Room Display Name must not be empty.");
                Assert.IsTrue(roomIds.Add(r.RoomId), $"Duplicate room ID detected: '{r.RoomId}'.");
                Assert.Greater(r.Bounds.size.x, 2f, "Room width must be realistic.");
                Assert.Greater(r.Bounds.size.z, 2f, "Room depth must be realistic.");
            }
        }

        [Test]
        public void Layout_DoorwayConnectionsAreValid()
        {
            var rooms = HotelLayoutTemplate.CreateMeridianHouseLayout();
            var roomMap = new Dictionary<string, RoomDefinition>();
            foreach (var r in rooms) roomMap[r.RoomId] = r;

            int totalDoors = 0;
            foreach (var r in rooms)
            {
                foreach (var conn in r.Connections)
                {
                    totalDoors++;
                    Assert.IsTrue(roomMap.ContainsKey(conn.FromRoomId), $"Connection references invalid FromRoom: '{conn.FromRoomId}'");
                    Assert.IsTrue(roomMap.ContainsKey(conn.ToRoomId), $"Connection references invalid ToRoom: '{conn.ToRoomId}'");
                }
            }

            Assert.Greater(totalDoors, 5, "Layout must have valid interconnecting doorways.");
        }

        [Test]
        public void Objects_CatalogIsPopulatedAndWellFormed()
        {
            var catalog = RoomObjectPlacer.GetCatalog();

            Assert.GreaterOrEqual(catalog.Count, 12, "Object catalog must contain at least 12 distinct items.");

            foreach (var obj in catalog)
            {
                Assert.IsFalse(string.IsNullOrEmpty(obj.ObjectId), "Object ID must not be empty.");
                Assert.IsFalse(string.IsNullOrEmpty(obj.DisplayName), "Object Display Name must not be empty.");
                Assert.Greater(obj.ColliderSize.magnitude, 0.1f, "Object must have non-zero collider dimensions.");
            }
        }

        [Test]
        public void NPCs_PopulationMeetsTargetSize()
        {
            var rng = new SeededRandom(12345);
            var rooms = HotelLayoutTemplate.CreateMeridianHouseLayout();
            var roster = NPCPopulationGenerator.GenerateRoster(rng, rooms);

            Assert.GreaterOrEqual(roster.Count, 8, "NPC ecosystem must contain at least 8 characters.");
            Assert.LessOrEqual(roster.Count, 12, "NPC ecosystem should be bounded to 8-12 characters.");
        }

        [Test]
        public void NPCs_ProfilesHaveStableIdentitiesAndRoles()
        {
            var rng = new SeededRandom(54321);
            var rooms = HotelLayoutTemplate.CreateMeridianHouseLayout();
            var roster = NPCPopulationGenerator.GenerateRoster(rng, rooms, "npc_madame_valerie", "npc_chef_marcel", "Embezzlement blackmail");

            var ids = new HashSet<string>();
            foreach (var npc in roster)
            {
                Assert.IsFalse(string.IsNullOrEmpty(npc.NpcId), "NPC ID must not be empty.");
                Assert.IsFalse(string.IsNullOrEmpty(npc.DisplayName), "NPC Name must not be empty.");
                Assert.IsTrue(ids.Add(npc.NpcId), $"Duplicate NPC ID detected: '{npc.NpcId}'.");
                Assert.Greater(npc.Age, 18, "NPC age must be reasonable.");
                Assert.Greater(npc.PermittedRoomIds.Count, 0, "NPC must have room access permissions.");
            }

            var victim = roster.Find(n => n.NpcId == "npc_madame_valerie");
            Assert.IsNotNull(victim);
            Assert.IsTrue(victim.IsVictim);

            var culprit = roster.Find(n => n.NpcId == "npc_chef_marcel");
            Assert.IsNotNull(culprit);
            Assert.IsTrue(culprit.IsCulprit);
        }

        [Test]
        public void NPCs_RoutinesCoverFullFiveMinuteLoop()
        {
            var rng = new SeededRandom(777);
            var rooms = HotelLayoutTemplate.CreateMeridianHouseLayout();
            var roster = NPCPopulationGenerator.GenerateRoster(rng, rooms);
            var routines = NPCPopulationGenerator.GenerateRoutines(roster, rng);

            foreach (var npc in roster)
            {
                Assert.IsTrue(routines.TryGetValue(npc.NpcId, out var plan), $"Missing routine plan for NPC '{npc.NpcId}'.");
                Assert.Greater(plan.Activities.Count, 0, $"Routine for '{npc.NpcId}' has no scheduled activities.");

                float firstStart = plan.Activities[0].StartSimTime;
                float lastEnd = plan.Activities[^1].EndSimTime;

                Assert.AreEqual(0f, firstStart, 0.01f, $"Routine for '{npc.NpcId}' must start at 0s.");
                Assert.AreEqual(300f, lastEnd, 0.01f, $"Routine for '{npc.NpcId}' must span until 300s (5-minute loop end).");
            }
        }

        [Test]
        public void Mystery_GroundTruthIsInternallyConsistent()
        {
            var rng = new SeededRandom(888);
            var truth = CrimeScenarioGenerator.GenerateScenario(rng);

            Assert.IsFalse(string.IsNullOrEmpty(truth.VictimId));
            Assert.IsFalse(string.IsNullOrEmpty(truth.CulpritId));
            Assert.AreNotEqual(truth.VictimId, truth.CulpritId, "Culprit cannot be the victim.");
            Assert.IsFalse(string.IsNullOrEmpty(truth.MurderWeaponObjectId));
            Assert.IsFalse(string.IsNullOrEmpty(truth.CrimeSceneRoomId));
            Assert.GreaterOrEqual(truth.MurderTimestamp, 60f, "Murder must occur after initial loop setup.");
            Assert.LessOrEqual(truth.MurderTimestamp, 280f, "Murder must conclude before loop resets at 300s.");
        }

        [Test]
        public void Mystery_SolvabilityValidatorAcceptsValidMystery()
        {
            var rng = new SeededRandom(999);
            var rooms = HotelLayoutTemplate.CreateMeridianHouseLayout();
            var truth = CrimeScenarioGenerator.GenerateScenario(rng);

            var validation = MysterySolvabilityValidator.ValidateScenario(truth, rooms);

            Assert.IsTrue(validation.IsValid, $"Generated mystery must be valid: {validation}");
            Assert.AreEqual(0, validation.FailureReasons.Count);
        }

        [Test]
        public void Mystery_SolvabilityValidatorRejectsInvalidMystery()
        {
            var rooms = HotelLayoutTemplate.CreateMeridianHouseLayout();

            // Broken scenario: culprit equals victim
            var brokenTruth = new MysteryGroundTruth
            {
                VictimId = "npc_lord_harrington",
                CulpritId = "npc_lord_harrington",
                MurderWeaponObjectId = "obj_cleaver",
                CrimeSceneRoomId = "room_guest_101",
                MurderTimestamp = 150f
            };

            var validation = MysterySolvabilityValidator.ValidateScenario(brokenTruth, rooms);

            Assert.IsFalse(validation.IsValid, "Validator must reject scenario where culprit == victim.");
            Assert.Greater(validation.FailureReasons.Count, 0);
        }

        [Test]
        public void Events_ScenarioEventBridgeCompilesValidEventDefs()
        {
            var rng = new SeededRandom(555);
            var truth = CrimeScenarioGenerator.GenerateScenario(rng);

            var eventDefs = ScenarioEventBridge.CompileScenarioEvents(truth);

            Assert.GreaterOrEqual(eventDefs.Count, 3, "Scenario must compile into at least 3 causal events.");
            foreach (var def in eventDefs)
            {
                Assert.IsFalse(string.IsNullOrEmpty(def.Id));
                Assert.GreaterOrEqual(def.ScheduledTimeSeconds, 0f);
                Assert.LessOrEqual(def.ScheduledTimeSeconds, 300f);
            }
        }

        [Test]
        public void Mystery_BatchSeeds_AllGeneratedScenariosPassSolvabilityValidation()
        {
            var rooms = HotelLayoutTemplate.CreateMeridianHouseLayout();
            for (int seed = 1; seed <= 50; seed++)
            {
                var rng = new SeededRandom(seed * 777);
                var truth = CrimeScenarioGenerator.GenerateScenario(rng);
                var validation = MysterySolvabilityValidator.ValidateScenario(truth, rooms);
                Assert.IsTrue(validation.IsValid, $"Seed {seed} generated unsolvable mystery scenario: {validation}");
                Assert.AreEqual(0, validation.FailureReasons.Count);
                Assert.GreaterOrEqual(truth.CrimeTimeline.Count, 4);
                Assert.GreaterOrEqual(truth.RequiredEvidenceLinks.Count, 3);
                Assert.GreaterOrEqual(truth.ExoneratedSuspectIds.Count, 6);
            }
        }

        [Test]
        public void Collision_WorldAuditorDetectsSolidColliders()
        {
            var testRoot = new GameObject("TestRoot");
            var cubeA = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeA.transform.SetParent(testRoot.transform);

            var cubeB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeB.transform.SetParent(testRoot.transform);
            cubeB.GetComponent<BoxCollider>().isTrigger = true;

            var audit = WorldCollisionAuditor.AuditEnvironment(testRoot);

            Assert.AreEqual(2, audit.TotalCollidersChecked);
            Assert.AreEqual(1, audit.SolidCollidersCount);
            Assert.AreEqual(1, audit.TriggerCollidersCount);
            Assert.IsTrue(audit.IsValid);

            Object.DestroyImmediate(testRoot);
        }

        [Test]
        public void Investigation_NewInvestigationClearsKnowledge()
        {
            var knowledge = InvestigationKnowledgeService.Instance;
            knowledge.RecordDiscovery("old_clue", "Discovered in previous case", 1, 10f);
            Assert.AreEqual(1, knowledge.DiscoveryCount);

            var seed = new InvestigationSeed(12345);
            var inv = GenerationPipeline.GenerateInvestigation(seed);

            // Simulation of starting a new investigation
            knowledge.Clear();

            Assert.AreEqual(0, knowledge.DiscoveryCount, "New investigation must clear case-specific knowledge.");
        }

        [Test]
        public void Batch_OneHundredSeedsPassValidationSuite()
        {
            // Multi-seed batch test: 100 seeds tested through the pure generation and solvability solver!
            var report = MultiSeedBatchRunner.RunBatchTest(seedCount: 100, startingSeed: 2000);

            Debug.Log(report.ToString());

            Assert.AreEqual(100, report.TotalSeedsTested);
            Assert.AreEqual(100, report.PassedCount, $"All 100 seeds must pass solvability validation. Failures: {string.Join("; ", report.FailureLogs)}");
            Assert.AreEqual(0, report.FailedCount);
            Assert.Less(report.AverageMsPerSeed, 10.0, "Average generation time per seed must be under 10ms for fast execution.");
        }

        [Test]
        public void Collision_ReceptionDeskAndLockersHaveSolidCompoundCollision()
        {
            var deskPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/HotelLobby_ReceptionDesk.obj");
            Assert.IsNotNull(deskPrefab, "Desk prefab must be found in Assets.");

            var testRoot = new GameObject("TestRoot_Desk");
            var desk = Object.Instantiate(deskPrefab, testRoot.transform);
            desk.name = "Reception_Desk";
            desk.transform.position = new Vector3(0f, 0f, 4.5f);

            // Front counter collider
            var counterCol = desk.AddComponent<BoxCollider>();
            counterCol.center = new Vector3(0f, 0.55f, 0f);
            counterCol.size = new Vector3(5.4f, 1.1f, 1.4f);
            counterCol.isTrigger = false;

            // Rear lockers unit collider
            var lockersGo = new GameObject("Reception_Lockers_Unit");
            lockersGo.transform.SetParent(desk.transform);
            lockersGo.transform.localPosition = Vector3.zero;

            var lockersCol = lockersGo.AddComponent<BoxCollider>();
            lockersCol.center = new Vector3(0f, 1.15f, -1.77f);
            lockersCol.size = new Vector3(4.25f, 2.30f, 0.45f);
            lockersCol.isTrigger = false;

            // Verify both colliders are solid
            Assert.IsFalse(counterCol.isTrigger, "Counter collider must be solid non-trigger.");
            Assert.IsFalse(lockersCol.isTrigger, "Lockers collider must be solid non-trigger.");

            // Verify physical dimensions cover the lockers mesh
            Assert.GreaterOrEqual(lockersCol.size.x, 4.0f, "Lockers collider width must cover key cubbies.");
            Assert.GreaterOrEqual(lockersCol.size.y, 2.0f, "Lockers collider height must block character height.");

            // Verify audit passes
            var audit = WorldCollisionAuditor.AuditEnvironment(testRoot);
            Assert.IsTrue(audit.IsValid, "Audit must pass on compound reception desk.");
            Assert.AreEqual(2, audit.SolidCollidersCount, "Must have 2 solid colliders (counter + lockers).");

            Object.DestroyImmediate(testRoot);
        }

        [Test]
        public void Collision_HotelBuilderGeneratesSolidFurnitureAndLockers()
        {
            var testRoot = new GameObject("TestRoot_Hotel");
            var rooms = HotelLayoutTemplate.CreateMeridianHouseLayout();
            var mats = new Dictionary<string, Material>();

            var hotel = HotelBuilder.BuildHotelEnvironment(testRoot.transform, rooms, mats);
            Assert.IsNotNull(hotel, "Hotel environment root must not be null.");

            var audit = WorldCollisionAuditor.AuditEnvironment(testRoot);
            Assert.IsTrue(audit.IsValid, $"Hotel environment collision must be valid: {audit}");
            Assert.Greater(audit.SolidCollidersCount, 30, "Hotel environment must contain solid walls, floors, ceilings, doors, and furniture.");
            Assert.AreEqual(0, audit.Errors.Count, "Hotel environment must have 0 collision errors.");

            Object.DestroyImmediate(testRoot);
        }

        [Test]
        public void Layout_GeneratesFourFloorsAndFortyDistinctRooms()
        {
            var rooms = HotelLayoutTemplate.CreateMeridianHouseLayout();

            Assert.AreEqual(40, rooms.Count, "Hotel layout must generate exactly 40 distinct rooms (10 per floor across 4 floors).");

            var floorCounts = new Dictionary<int, int>();
            var roomIds = new HashSet<string>();

            foreach (var room in rooms)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(room.RoomId), "RoomId must not be empty.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(room.DisplayName), "DisplayName must not be empty.");
                Assert.IsTrue(roomIds.Add(room.RoomId), $"Duplicate room ID detected: '{room.RoomId}'");

                if (!floorCounts.ContainsKey(room.FloorIndex)) floorCounts[room.FloorIndex] = 0;
                floorCounts[room.FloorIndex]++;

                Assert.Greater(room.Bounds.size.x, 3.0f, $"Room '{room.RoomId}' width must be realistic.");
                Assert.Greater(room.Bounds.size.z, 3.0f, $"Room '{room.RoomId}' depth must be realistic.");
                Assert.AreEqual(4.5f, room.Bounds.size.y, 0.01f, $"Room '{room.RoomId}' height must be 4.5m.");
            }

            Assert.AreEqual(4, floorCounts.Count, "Hotel layout must span exactly 4 floors (Floors 0, 1, 2, 3).");
            for (int f = 0; f < 4; f++)
            {
                Assert.IsTrue(floorCounts.ContainsKey(f), $"Floor {f} must exist.");
                Assert.AreEqual(10, floorCounts[f], $"Floor {f} must have exactly 10 distinct rooms.");
            }
        }

        [Test]
        public void Layout_SeededGenerationProducesMeaningfulSpatialAndThematicVariation()
        {
            var rngA = new SeededRandom(11111);
            var rngB = new SeededRandom(99999);
            var rngSame = new SeededRandom(11111);

            var roomsA = HotelLayoutTemplate.CreateMeridianHouseLayout(rngA);
            var roomsB = HotelLayoutTemplate.CreateMeridianHouseLayout(rngB);
            var roomsSame = HotelLayoutTemplate.CreateMeridianHouseLayout(rngSame);

            // Repeatability: Identical seed produces identical room dimensions and styling
            Assert.AreEqual(roomsA.Count, roomsSame.Count);
            for (int i = 0; i < roomsA.Count; i++)
            {
                Assert.AreEqual(roomsA[i].Bounds.center.x, roomsSame[i].Bounds.center.x, 0.001f);
                Assert.AreEqual(roomsA[i].Bounds.size.x, roomsSame[i].Bounds.size.x, 0.001f);
                Assert.AreEqual(roomsA[i].Styling.LightIntensity, roomsSame[i].Styling.LightIntensity, 0.001f);
            }

            // Meaningful Variation: Different seeds produce jittered dimensions and lighting
            bool dimensionsDiffer = false;
            bool lightingDiffers = false;

            for (int i = 0; i < roomsA.Count; i++)
            {
                if (Mathf.Abs(roomsA[i].Bounds.size.x - roomsB[i].Bounds.size.x) > 0.05f ||
                    Mathf.Abs(roomsA[i].Bounds.size.z - roomsB[i].Bounds.size.z) > 0.05f)
                {
                    dimensionsDiffer = true;
                }

                if (Mathf.Abs(roomsA[i].Styling.LightIntensity - roomsB[i].Styling.LightIntensity) > 0.05f)
                {
                    lightingDiffers = true;
                }
            }

            Assert.IsTrue(dimensionsDiffer, "Different seeds must produce jittered spatial room dimensions.");
            Assert.IsTrue(lightingDiffers, "Different seeds must produce varied lighting ambiance.");
        }

        [Test]
        public void Layout_MultiFloorStaircaseConnectsAllFourFloors()
        {
            var testRoot = new GameObject("TestRoot_Stairs");
            var rooms = HotelLayoutTemplate.CreateMeridianHouseLayout();
            var mats = new Dictionary<string, Material>();

            var hotel = HotelBuilder.BuildHotelEnvironment(testRoot.transform, rooms, mats);
            var stairsRoot = testRoot.transform.Find("MeridianHouse_Architecture/Staircase_CentralHub_4Floors");
            Assert.IsNotNull(stairsRoot, "4-floor staircase central hub must exist.");

            // Verify all 3 flights exist
            var flight0 = stairsRoot.Find("Flight_Floor0_to_1");
            var flight1 = stairsRoot.Find("Flight_Floor1_to_2");
            var flight2 = stairsRoot.Find("Flight_Floor2_to_3");

            Assert.IsNotNull(flight0, "Flight from Floor 0 to 1 must exist.");
            Assert.IsNotNull(flight1, "Flight from Floor 1 to 2 must exist.");
            Assert.IsNotNull(flight2, "Flight from Floor 2 to 3 must exist.");

            // Verify all 3 landings exist
            Assert.IsNotNull(stairsRoot.Find("Landing_Floor1"), "Landing at Floor 1 must exist.");
            Assert.IsNotNull(stairsRoot.Find("Landing_Floor2"), "Landing at Floor 2 must exist.");
            Assert.IsNotNull(stairsRoot.Find("Landing_Floor3"), "Landing at Floor 3 must exist.");

            // Verify each flight has walk ramp collider
            Assert.IsNotNull(flight0.Find("Flight_Floor0_to_1_WalkRamp_Collider"));
            Assert.IsNotNull(flight1.Find("Flight_Floor1_to_2_WalkRamp_Collider"));
            Assert.IsNotNull(flight2.Find("Flight_Floor2_to_3_WalkRamp_Collider"));

            Object.DestroyImmediate(testRoot);
        }

        [Test]
        public void Collision_HotelBuilderFourFloorsHasMassiveSolidCollisionIntegrity()
        {
            var testRoot = new GameObject("TestRoot_Hotel4Floors");
            var rooms = HotelLayoutTemplate.CreateMeridianHouseLayout();
            var mats = new Dictionary<string, Material>();

            var hotel = HotelBuilder.BuildHotelEnvironment(testRoot.transform, rooms, mats);
            var audit = WorldCollisionAuditor.AuditEnvironment(testRoot);

            Assert.IsTrue(audit.IsValid, $"4-Floor hotel environment collision must be valid: {audit}");
            Assert.Greater(audit.SolidCollidersCount, 150, "4-Floor hotel must have over 150 solid non-trigger colliders across all 40 rooms, stairs, and doors.");
            Assert.AreEqual(0, audit.Errors.Count, "4-Floor hotel must have 0 collision errors.");

            Object.DestroyImmediate(testRoot);
        }

        [Test]
        public void Elevator_ShaftAndCabinExistWithFourFloorButtons()
        {
            var testRoot = new GameObject("TestRoot_Elevator");
            var rooms = HotelLayoutTemplate.CreateMeridianHouseLayout();
            var mats = new Dictionary<string, Material>();

            var hotel = HotelBuilder.BuildHotelEnvironment(testRoot.transform, rooms, mats);
            var elevatorRoot = testRoot.transform.Find("MeridianHouse_Architecture/Hotel_ArtDecoElevator");

            Assert.IsNotNull(elevatorRoot, "Art Deco Elevator root must exist in hotel environment.");

            // Check Elevator Cabin & Component
            var cabin = elevatorRoot.Find("Elevator_Cabin");
            Assert.IsNotNull(cabin, "Elevator moving cabin must exist.");

            var elevatorComp = cabin.GetComponent<HotelElevator>();
            Assert.IsNotNull(elevatorComp, "HotelElevator component must be attached to cabin.");
            Assert.AreEqual(0, elevatorComp.CurrentFloor, "Initial floor must be Floor 0.");
            Assert.IsFalse(elevatorComp.IsMoving, "Elevator should not be moving initially.");

            // Check In-Cabin Control Panel & 4 Buttons
            var panel = cabin.Find("Elevator_ControlPanel");
            Assert.IsNotNull(panel, "Control panel must exist inside cabin.");
            for (int f = 0; f < 4; f++)
            {
                var btn = panel.Find($"Button_Floor_{f}");
                Assert.IsNotNull(btn, $"Floor {f} button must exist in control panel.");
                var btnInteract = btn.GetComponent<InteractableElevatorButton>();
                Assert.IsNotNull(btnInteract, $"Floor {f} button must have InteractableElevatorButton component.");
                Assert.AreEqual(f, btnInteract.TargetFloor);
                Assert.IsFalse(btnInteract.IsCallButton);
            }

            // Check Outside Floor Call Stations (Floors 0, 1, 2, 3)
            for (int f = 0; f < 4; f++)
            {
                var callStation = elevatorRoot.Find($"Elevator_CallStation_Floor_{f}");
                Assert.IsNotNull(callStation, $"Floor {f} outside call station must exist.");
                var callInteract = callStation.GetComponent<InteractableElevatorButton>();
                Assert.IsNotNull(callInteract, $"Floor {f} call station must have InteractableElevatorButton component.");
                Assert.AreEqual(f, callInteract.TargetFloor);
                Assert.IsTrue(callInteract.IsCallButton);
            }

            Object.DestroyImmediate(testRoot);
        }

        [Test]
        public void Collision_FoundationPlinthsSpanEntireBuildingFootprint()
        {
            var testRoot = new GameObject("TestRoot_Plinths");
            var rooms = HotelLayoutTemplate.CreateMeridianHouseLayout();
            var mats = new Dictionary<string, Material>();

            var hotel = HotelBuilder.BuildHotelEnvironment(testRoot.transform, rooms, mats);
            var plinthsRoot = testRoot.transform.Find("MeridianHouse_Architecture/Hotel_ContinuousFloorPlinths");

            Assert.IsNotNull(plinthsRoot, "Continuous floor plinths root must exist.");

            for (int f = 0; f < 4; f++)
            {
                var plinth = plinthsRoot.Find($"FoundationPlinth_Floor_{f}");
                Assert.IsNotNull(plinth, $"Foundation plinth for Floor {f} must exist.");

                var col = plinth.GetComponent<BoxCollider>();
                Assert.IsNotNull(col, $"Foundation plinth for Floor {f} must have BoxCollider.");
                Assert.IsFalse(col.isTrigger, $"Foundation plinth collider must be solid non-trigger.");

                // Verify plinth covers the entire hotel footprint
                Assert.GreaterOrEqual(plinth.localScale.x, 50.0f, "Foundation plinth must span at least 50m in width.");
                Assert.GreaterOrEqual(plinth.localScale.z, 36.0f, "Foundation plinth must span at least 36m in depth.");
            }

            Object.DestroyImmediate(testRoot);
        }

        [Test]
        public void Visibility_RoomWallsContainDoorwayCutoutsAndLintels()
        {
            var testRoot = new GameObject("TestRoot_Cutouts");
            var rooms = HotelLayoutTemplate.CreateMeridianHouseLayout();
            var mats = new Dictionary<string, Material>();

            var hotel = HotelBuilder.BuildHotelEnvironment(testRoot.transform, rooms, mats);
            var lobbyRoom = testRoot.transform.Find("MeridianHouse_Architecture/Room_room_lobby_GrandMarbleLobby");

            Assert.IsNotNull(lobbyRoom, "Lobby room must exist in architecture.");

            // Lobby has doorway to reception on Wall_North. Check lintel and flank cutouts exist
            var northWallLintel = lobbyRoom.Find("Wall_North_DoorLintel");
            Assert.IsNotNull(northWallLintel, "Lobby North wall must have doorway lintel cutout rather than solid wall.");

            // Lobby has doorway to dining on Wall_West. Check West lintel exists
            var westWallLintel = lobbyRoom.Find("Wall_West_DoorLintel");
            Assert.IsNotNull(westWallLintel, "Lobby West wall must have doorway lintel cutout rather than solid wall.");

            Object.DestroyImmediate(testRoot);
        }

        [Test]
        public void Archways_DoNotContainSolidDoorLeafBlockers()
        {
            var testRoot = new GameObject("TestRoot_Archways");
            var rooms = HotelLayoutTemplate.CreateMeridianHouseLayout();
            var mats = new Dictionary<string, Material>();

            var hotel = HotelBuilder.BuildHotelEnvironment(testRoot.transform, rooms, mats);
            var doorsRoot = testRoot.transform.Find("MeridianHouse_Architecture/Hotel_Doors");

            Assert.IsNotNull(doorsRoot, "Hotel_Doors root must exist.");

            // Find Front Reception Archway
            var archDoor = doorsRoot.Find("Door_conn_lobby_reception");
            Assert.IsNotNull(archDoor, "Front reception archway must exist.");

            // Archway should contain decorative arch casing, but NOT a blocking door leaf
            Assert.IsNotNull(archDoor.Find("Archway_Casing"), "Archway should contain Archway_Casing.");
            Assert.IsNull(archDoor.Find("Hinge/Door_Leaf"), "Archway must NOT contain a closed door leaf blocking navigation.");

            Object.DestroyImmediate(testRoot);
        }

        [Test]
        public void NPCWorldSpawner_PopulatesLivingRosterAcrossAllFloors()
        {
            var testRoot = new GameObject("TestRoot_NPCWorld");
            var seed = InvestigationSeed.FromCode("TC-TEST-SEEDA");
            var investigation = GenerationPipeline.GenerateInvestigation(seed);
            var mats = new Dictionary<string, Material>();

            var rosterGo = NPCWorldSpawner.SpawnRoster(testRoot.transform, investigation.Roster, mats);

            Assert.IsNotNull(rosterGo, "Spawned roster root must exist.");
            Assert.AreEqual(investigation.Roster.Count, rosterGo.transform.childCount, "Every NPC in the roster must be spawned.");

            // Verify each spawned NPC has physical collider and NPCController
            foreach (Transform child in rosterGo.transform)
            {
                var col = child.GetComponent<CapsuleCollider>();
                Assert.IsNotNull(col, $"{child.name} must have a CapsuleCollider.");
                Assert.IsFalse(col.isTrigger, $"{child.name} collider must be solid non-trigger.");

                var controller = child.GetComponent<NPCController>();
                Assert.IsNotNull(controller, $"{child.name} must have an NPCController.");
                Assert.IsNotNull(controller.Profile, $"{child.name} NPCController must have Profile set.");
                Assert.IsTrue(controller.CanInteract, $"{child.name} must be interactable.");
                Assert.That(controller.InteractionPrompt, Does.Contain("Question"));
            }

            Object.DestroyImmediate(testRoot);
        }

        [Test]
        public void RoomObjectPlacer_PopulatesAllObjectsWithSolidCollidersWithoutException()
        {
            var testRoot = new GameObject("TestRoot_Objects");
            var seed = InvestigationSeed.FromCode("TC-TEST-SEEDB");
            var investigation = GenerationPipeline.GenerateInvestigation(seed);
            var mats = new Dictionary<string, Material>();

            Assert.DoesNotThrow(() =>
            {
                RoomObjectPlacer.PopulateRooms(
                    testRoot.transform,
                    investigation.Rooms,
                    seed.ObjectStream,
                    mats,
                    investigation.GroundTruth.MandatoryObjectPlacements);
            }, "PopulateRooms must execute without MissingComponentException or null reference.");

            // Verify key objects and clues have BoxColliders
            var colliders = testRoot.GetComponentsInChildren<BoxCollider>();
            Assert.Greater(colliders.Length, 10, "Populated rooms must contain solid colliders on props.");

            Object.DestroyImmediate(testRoot);
        }

        [Test]
        public void DistinctNPCVisuals_EachRoleHasUniqueHeadwearAndHandheldSignatureProps()
        {
            var testRoot = new GameObject("TestRoot_DistinctNPCs");
            var mats = new Dictionary<string, Material>();
            var seed = InvestigationSeed.FromCode("TC-VISUAL-TEST");
            var investigation = GenerationPipeline.GenerateInvestigation(seed);

            foreach (var profile in investigation.Roster)
            {
                var charGo = NPCWorldSpawner.CreateDistinctStylizedCharacter(testRoot.transform, profile, mats);
                Assert.IsNotNull(charGo, $"Stylized model must be generated for {profile.DisplayName}.");

                // Check role-specific signature headwear / props
                switch (profile.Role)
                {
                    case NPCRole.HeadChef:
                        Assert.IsNotNull(charGo.transform.Find("Head/Toque_Band"), "Chef must wear a pleated Toque Blanche.");
                        Assert.IsNotNull(charGo.transform.Find("Arm_R/Hand_R/Cleaver_Blade"), "Chef must hold a kitchen cleaver.");
                        break;

                    case NPCRole.HousekeepingMaid:
                        Assert.IsNotNull(charGo.transform.Find("Head/Maid_LaceCap"), "Maid must wear a lace cap.");
                        Assert.IsNotNull(charGo.transform.Find("Arm_R/Hand_R/Duster_Handle"), "Maid must hold a feather duster.");
                        break;

                    case NPCRole.MaintenanceEngineer:
                        Assert.IsNotNull(charGo.transform.Find("Head/FlatCap_Crown"), "Engineer must wear a flat cap.");
                        Assert.IsNotNull(charGo.transform.Find("Arm_R/Hand_R/PipeWrench_Shaft"), "Engineer must hold a pipe wrench.");
                        break;

                    case NPCRole.HotelManager:
                        Assert.IsNotNull(charGo.transform.Find("Head/Glasses_Rim_L"), "Manager must wear spectacles.");
                        Assert.IsNotNull(charGo.transform.Find("Arm_R/Hand_R/Executive_Folio"), "Manager must hold an executive folio.");
                        break;

                    case NPCRole.WealthyGuest:
                        Assert.IsNotNull(charGo.transform.Find("Head/Gold_Monocle"), "Harrington must wear a gold monocle.");
                        Assert.IsNotNull(charGo.transform.Find("Arm_R/Hand_R/WalkingCane_Shaft"), "Harrington must hold a walking cane.");
                        break;

                    case NPCRole.SocietyLady:
                        Assert.IsNotNull(charGo.transform.Find("Feather_Boa"), "Madame Valerie must wear a fur stole / feather boa.");
                        Assert.IsNotNull(charGo.transform.Find("Arm_R/Hand_R/Coupe_Stem"), "Madame Valerie must hold a champagne coupe glass.");
                        break;

                    case NPCRole.DoctorGuest:
                        Assert.IsNotNull(charGo.transform.Find("Arm_R/Hand_R/Doctor_Bag"), "Dr. Sterling must hold a physician's doctor bag.");
                        break;

                    case NPCRole.JournalistGuest:
                        Assert.IsNotNull(charGo.transform.Find("Head/Fedora_Brim"), "Vivian Monroe must wear a 1930s fedora.");
                        Assert.IsNotNull(charGo.transform.Find("Arm_R/Hand_R/Reporter_Notebook"), "Vivian Monroe must hold a reporter's notebook.");
                        break;

                    case NPCRole.Receptionist:
                        Assert.IsNotNull(charGo.transform.Find("Arm_R/Hand_R/Reservation_Clipboard"), "Receptionist must hold a reservation clipboard.");
                        break;
                }
            }

            Object.DestroyImmediate(testRoot);
        }

        [Test]
        public void GuestSuites_ContainLuxuryFourPosterBedsAndEnSuiteShowers()
        {
            var testRoot = new GameObject("TestRoot_Suites");
            var rooms = HotelLayoutTemplate.CreateMeridianHouseLayout();
            var mats = new Dictionary<string, Material>();

            var hotel = HotelBuilder.BuildHotelEnvironment(testRoot.transform, rooms, mats);
            var furnitureRoot = testRoot.transform.Find("MeridianHouse_Architecture/Hotel_SolidFurniture");
            Assert.IsNotNull(furnitureRoot, "Solid furniture root must exist.");

            // Verify Suite 101 has a bed and an en-suite bathroom with walk-in shower
            var suite101Bed = furnitureRoot.Find("Suite_room_guest_101_Furnishings/room_guest_101_Bed");
            Assert.IsNotNull(suite101Bed, "Suite 101 must contain a luxury bed.");
            Assert.IsNotNull(suite101Bed.Find("Headboard"), "Bed must have a headboard.");
            Assert.IsNotNull(suite101Bed.Find("Nightstand_L"), "Bed must have a left nightstand.");
            Assert.IsNotNull(suite101Bed.Find("Nightstand_R"), "Bed must have a right nightstand.");

            var suite101Bath = furnitureRoot.Find("Suite_room_guest_101_Furnishings/room_guest_101_EnSuite_Bathroom");
            Assert.IsNotNull(suite101Bath, "Suite 101 must contain an en-suite bathroom.");

            var shower = suite101Bath.Find("room_guest_101_ShowerStall");
            Assert.IsNotNull(shower, "En-suite bathroom must contain a shower stall.");
            Assert.IsNotNull(shower.Find("Shower_TrayBase"), "Shower must contain a tiled shower tray base.");
            Assert.IsNotNull(shower.Find("Shower_Drain"), "Shower must contain a metallic drain.");
            Assert.IsNotNull(shower.Find("Rain_Showerhead"), "Shower must contain a rain showerhead.");

            var vanity = suite101Bath.Find("room_guest_101_VanityUnit");
            Assert.IsNotNull(vanity, "En-suite bathroom must contain a vanity unit.");
            Assert.IsNotNull(vanity.Find("Vanity_Basin"), "Vanity must contain a basin.");
            Assert.IsNotNull(vanity.Find("Vanity_FramedMirror"), "Vanity must contain a framed mirror.");

            var toilet = suite101Bath.Find("room_guest_101_PorcelainToilet");
            Assert.IsNotNull(toilet, "En-suite bathroom must contain a porcelain toilet.");

            // Also verify VIP Suite 102 (compact suite) contains shower
            var suite102Bath = furnitureRoot.Find("Suite_room_guest_102_Furnishings/room_guest_102_EnSuite_Bathroom");
            Assert.IsNotNull(suite102Bath, "VIP Suite 102 must also contain an en-suite bathroom.");
            Assert.IsNotNull(suite102Bath.Find("room_guest_102_ShowerStall"), "VIP Suite 102 must have a shower stall.");

            Object.DestroyImmediate(testRoot);
        }

        [Test]
        public void HotelKitchen_ContainsCommercialStoveWithBurnersAndPrepIsland()
        {
            var testRoot = new GameObject("TestRoot_Kitchen");
            var rooms = HotelLayoutTemplate.CreateMeridianHouseLayout();
            var mats = new Dictionary<string, Material>();

            var hotel = HotelBuilder.BuildHotelEnvironment(testRoot.transform, rooms, mats);
            var furnitureRoot = testRoot.transform.Find("MeridianHouse_Architecture/Hotel_SolidFurniture");

            var stove = furnitureRoot.Find("Kitchen_Commercial_Compound/Kitchen_CommercialStove");
            Assert.IsNotNull(stove, "Kitchen must contain a commercial cooking range.");
            Assert.IsNotNull(stove.Find("Burner_0_0"), "Commercial range must feature circular burner grates.");
            Assert.IsNotNull(stove.Find("Extractor_Hood"), "Commercial range must feature an overhead extraction hood.");

            var island = furnitureRoot.Find("Kitchen_Commercial_Compound/Kitchen_ButcherBlock_Island");
            Assert.IsNotNull(island, "Kitchen must contain a central butcher-block prep island.");
            Assert.IsNotNull(island.Find("ButcherBlock_Top"), "Prep island must feature a butcher-block wood top.");

            var sink = furnitureRoot.Find("Kitchen_Commercial_Compound/Kitchen_SculleryDoubleSink");
            Assert.IsNotNull(sink, "Kitchen must contain a double-basin scullery wash sink.");

            Object.DestroyImmediate(testRoot);
        }

        [Test]
        public void NPCRoutines_PopulateControllerScheduleActivitiesAndWaypoints()
        {
            var testRoot = new GameObject("TestRoot_Routines");
            var seed = InvestigationSeed.FromCode("TC-ROUTINE-TEST");
            var investigation = GenerationPipeline.GenerateInvestigation(seed);
            var mats = new Dictionary<string, Material>();

            var rosterGo = NPCWorldSpawner.SpawnRoster(testRoot.transform, investigation.Roster, investigation.Routines, mats);

            // Verify each NPC has scheduled activities and waypoints
            foreach (Transform npcChild in rosterGo.transform)
            {
                var controller = npcChild.GetComponent<NPCController>();
                Assert.IsNotNull(controller, $"{npcChild.name} must have NPCController.");

                Assert.IsNotNull(controller.Definition, $"{npcChild.name} must have NPCDefinition initialized.");
                Assert.Greater(controller.Definition.ScheduleActivities.Count, 0, $"{npcChild.name} must have at least one scheduled activity.");

                var waypointsChild = npcChild.Find("Waypoints");
                Assert.IsNotNull(waypointsChild, $"{npcChild.name} must have a Waypoints container.");
                Assert.Greater(waypointsChild.childCount, 0, $"{npcChild.name} must have instantiated waypoint transforms.");
            }

            Object.DestroyImmediate(testRoot);
        }

        [Test]
        public void NPCBaselines_AreCollisionFreeAndDoNotClipIntoFurniture()
        {
            var seed = InvestigationSeed.FromCode("TC-COLLISION-TEST");
            var investigation = GenerationPipeline.GenerateInvestigation(seed);

            // 1. Julian Adler (Manager) must be BEHIND desk (Z > 5.5m), not inside desk at Z=4.0m
            var manager = investigation.Roster.Find(p => p.Role == NPCRole.HotelManager);
            Assert.IsNotNull(manager);
            Assert.Greater(manager.BaselinePosition.z, 5.5f, "Manager must be stationed behind his executive desk, not inside it.");

            // 2. Vivian Monroe (Journalist) must be clear of center table
            var journalist = investigation.Roster.Find(p => p.Role == NPCRole.JournalistGuest);
            Assert.IsNotNull(journalist);
            float distFromLobbyCenter = new Vector2(journalist.BaselinePosition.x, journalist.BaselinePosition.z).magnitude;
            Assert.Greater(distFromLobbyCenter, 2.5f, "Journalist must not be inside the central lobby table.");

            // 3. Pierre & Celeste must be in the staff workstation behind front counter (Z in [9.4, 10.5]), in front of key lockers
            var pierre = investigation.Roster.Find(p => p.Role == NPCRole.Concierge);
            var celeste = investigation.Roster.Find(p => p.Role == NPCRole.Receptionist);
            Assert.Greater(pierre.BaselinePosition.z, 9.4f, "Concierge must be behind front counter.");
            Assert.Less(pierre.BaselinePosition.z, 10.5f, "Concierge must be in front of locker unit.");
            Assert.Greater(celeste.BaselinePosition.z, 9.4f, "Receptionist must be behind front counter.");
            Assert.Less(celeste.BaselinePosition.z, 10.5f, "Receptionist must be in front of locker unit.");
        }

        [Test]
        public void NPCWorldSpawner_ShoesAreParentedToLegs_EliminatingFrozenFeet()
        {
            var seed = InvestigationSeed.FromCode("TC-SHOES-TEST");
            var investigation = GenerationPipeline.GenerateInvestigation(seed);
            var testRoot = new GameObject("TestRoot_Shoes");

            NPCWorldSpawner.SpawnRoster(testRoot.transform, investigation.Roster, investigation.Routines, null);

            var rosterRoot = testRoot.transform.Find("Hotel_NPC_Roster");
            Assert.IsNotNull(rosterRoot);

            for (int i = 0; i < rosterRoot.childCount; i++)
            {
                var profile = investigation.Roster[i];
                if (profile.Role == NPCRole.Concierge) continue; // Concierge Pierre uses static 3D mesh

                var npcGo = rosterRoot.GetChild(i);
                var leftLeg = npcGo.Find($"Visual_{profile.DisplayName.Replace(" ", "")}/LeftLeg");
                var rightLeg = npcGo.Find($"Visual_{profile.DisplayName.Replace(" ", "")}/RightLeg");

                Assert.IsNotNull(leftLeg, $"{npcGo.name} must have LeftLeg.");
                Assert.IsNotNull(rightLeg, $"{npcGo.name} must have RightLeg.");

                var shoeL = leftLeg.Find("Shoe_L");
                var shoeR = rightLeg.Find("Shoe_R");

                Assert.IsNotNull(shoeL, $"{npcGo.name} Shoe_L must be parented directly to LeftLeg.");
                Assert.IsNotNull(shoeR, $"{npcGo.name} Shoe_R must be parented directly to RightLeg.");
            }

            Object.DestroyImmediate(testRoot);
        }

        [Test]
        public void HotelLayout_GrandStaircaseHasArchwayCutout_EliminatingWallInStairs()
        {
            var rooms = HotelLayoutTemplate.CreateMeridianHouseLayout();
            var lobby = rooms.Find(r => r.RoomId == "room_lobby");
            Assert.IsNotNull(lobby);

            var stairConn = lobby.Connections.Find(c => c.ToRoomId == "room_corridor_ground" && Mathf.Abs(c.WorldPosition.z - (-2.0f)) < 0.5f);
            Assert.IsNotNull(stairConn, "Lobby must have an open archway connection to transport hub at Z=-2.0m to prevent wall across stairs.");
        }

        [Test]
        public void InteractableDoor_CarvesNavMeshObstacleWhenClosed()
        {
            var doorGo = new GameObject("TestDoor");
            var doorComp = doorGo.AddComponent<InteractableDoor>();
            doorComp.EnsureNavObstacle();

            var obstacle = doorGo.GetComponent<UnityEngine.AI.NavMeshObstacle>();
            Assert.IsNotNull(obstacle, "InteractableDoor must have NavMeshObstacle component.");
            Assert.IsTrue(obstacle.carving, "NavMeshObstacle must have carving enabled to block NPC navigation.");
            Assert.IsTrue(obstacle.enabled, "NavMeshObstacle must be enabled when door is closed.");

            doorComp.UnlockAndOpen();
            Assert.IsFalse(obstacle.enabled, "NavMeshObstacle must be disabled when door is unlocked and opened.");

            Object.DestroyImmediate(doorGo);
        }

        [Test]
        public void RoomObjectPlacer_BespokePropsHavePhysicsAndRealisticParts()
        {
            var testRoot = new GameObject("TestRoot_Props");
            var catalog = RoomObjectPlacer.GetCatalog();

            var cleaverDef = catalog.Find(o => o.ObjectId == "obj_chef_cleaver");
            Assert.IsNotNull(cleaverDef);

            var cleaverGo = RoomObjectPlacer.SpawnObjectAtAnchor(testRoot.transform, cleaverDef,
                new RoomAnchor("anchor_test", AnchorCategory.Counter, Vector3.zero, Vector3.zero),
                new Dictionary<string, Material>());

            Assert.IsNotNull(cleaverGo);
            var blade = cleaverGo.transform.Find("VisualModel/Cleaver_Blade");
            Assert.IsNotNull(blade, "Cleaver must have bespoke blade model, not a raw generic cube.");

            var physProp = cleaverGo.GetComponent<TomorrowsCrime.PhysicsEngine.PhysicsProp>();
            Assert.IsNotNull(physProp, "Prop must have PhysicsProp component for interactive pickup and throwing.");

            Object.DestroyImmediate(testRoot);
        }

        [Test]
        public void Viewmodel_ArmsAnchoredBelowCameraAndHaveNoMidAirStubs()
        {
            var playerGo = new GameObject("TestPlayer_Viewmodel");
            var camGo = new GameObject("Main Camera");
            camGo.transform.SetParent(playerGo.transform);
            var cam = camGo.AddComponent<Camera>();

            var vmAnim = playerGo.AddComponent<TomorrowsCrime.Player.PlayerViewmodelAnimator>();
            var so = new UnityEditor.SerializedObject(vmAnim);
            so.FindProperty("cameraTransform").objectReferenceValue = camGo.transform;
            so.ApplyModifiedProperties();

            // Trigger viewmodel generation
            var method = typeof(TomorrowsCrime.Player.PlayerViewmodelAnimator).GetMethod("BuildDetectiveHandsViewmodel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(method, "BuildDetectiveHandsViewmodel method must exist.");
            method.Invoke(vmAnim, null);

            var vmRoot = camGo.transform.Find("Detective_Hands_Viewmodel");
            Assert.IsNotNull(vmRoot, "Detective_Hands_Viewmodel must be instantiated under camera.");

            var armL = vmRoot.Find("Arm_Left");
            var armR = vmRoot.Find("Arm_Right");
            Assert.IsNotNull(armL, "Left arm must exist.");
            Assert.IsNotNull(armR, "Right arm must exist.");

            // Verify UpperArm_Shoulder starts from behind or deep below the camera near clip plane
            var shoulderL = armL.Find("UpperArm_Shoulder");
            Assert.IsNotNull(shoulderL, "Upper arm shoulder must exist.");
            Assert.Less(shoulderL.localPosition.z, 0f, "Shoulder must be positioned behind arm root (negative Z).");
            Assert.Less(shoulderL.localPosition.y, -0.2f, "Shoulder must be positioned below arm root (negative Y).");

            Object.DestroyImmediate(playerGo);
        }

        [Test]
        public void Staircase_AllStepsHaveSufficientHeadroomClearance()
        {
            var testRoot = new GameObject("TestRoot_StairHeadroom");
            var rooms = HotelLayoutTemplate.CreateMeridianHouseLayout();
            var mats = new Dictionary<string, Material>();

            var hotel = HotelBuilder.BuildHotelEnvironment(testRoot.transform, rooms, mats);
            var stairsRoot = testRoot.transform.Find("MeridianHouse_Architecture/Staircase_CentralHub_4Floors");
            Assert.IsNotNull(stairsRoot, "Staircase_CentralHub_4Floors must exist.");

            string[] flights = { "Flight_Floor0_to_1", "Flight_Floor1_to_2", "Flight_Floor2_to_3" };
            for (int f = 0; f < flights.Length; f++)
            {
                var flight = stairsRoot.Find(flights[f]);
                Assert.IsNotNull(flight, $"Flight '{flights[f]}' must exist.");

                var ramp = flight.Find($"{flights[f]}_WalkRamp_Collider");
                Assert.IsNotNull(ramp, $"Flight '{flights[f]}' must have a smooth WalkRamp_Collider.");
                var rampCol = ramp.GetComponent<BoxCollider>();
                Assert.IsNotNull(rampCol, "WalkRamp must have a BoxCollider.");
                Assert.IsFalse(rampCol.isTrigger, "WalkRamp collider must be solid for smooth CharacterController climbing.");
            }

            Object.DestroyImmediate(testRoot);
        }

        [Test]
        public void Elevator_LandingGatesAndRoofCapAreSealedAndOperational()
        {
            var testRoot = new GameObject("TestRoot_ElevatorSealed");
            var rooms = HotelLayoutTemplate.CreateMeridianHouseLayout();
            var mats = new Dictionary<string, Material>();

            var hotel = HotelBuilder.BuildHotelEnvironment(testRoot.transform, rooms, mats);
            var elevatorRoot = testRoot.transform.Find("MeridianHouse_Architecture/Hotel_ArtDecoElevator");
            Assert.IsNotNull(elevatorRoot, "Hotel_ArtDecoElevator root must exist.");

            // Check all 4 landing gates exist
            for (int f = 0; f < 4; f++)
            {
                var gate = elevatorRoot.Find($"Elevator_LandingPortal_Floor_{f}/Elevator_ShaftGateBarrier_Floor_{f}");
                Assert.IsNotNull(gate, $"Landing gate barrier on Floor {f} must exist.");

                var col = gate.GetComponent<BoxCollider>();
                Assert.IsNotNull(col, $"Landing gate on Floor {f} must have a BoxCollider.");
                // Floor 0 starts open (isTrigger = true); Floors 1, 2, 3 start closed (isTrigger = false)
                Assert.AreEqual(f == 0, col.isTrigger, $"Floor {f} gate trigger status mismatch.");
            }

            // Check Floor 3 roof caps exist
            var roomFoyer = testRoot.transform.Find("MeridianHouse_Architecture/Room_room_floor3_sky_foyer_Floor3SkyFoyer&GrandLanding");
            Assert.IsNotNull(roomFoyer, "Sky Penthouse Foyer room must exist.");

            var elevRoof = roomFoyer.Find("Ceiling_Slab_ElevatorRoofCap");
            Assert.IsNotNull(elevRoof, "Ceiling_Slab_ElevatorRoofCap must seal the elevator shaft on Floor 3.");

            var stairRoof = roomFoyer.Find("Ceiling_Slab_StaircaseRoofCap");
            Assert.IsNotNull(stairRoof, "Ceiling_Slab_StaircaseRoofCap must seal the central staircase atrium on Floor 3.");

            Object.DestroyImmediate(testRoot);
        }

        [Test]
        public void HotelPhysics_FullSimulation_PlayerMovementStairsAndElevator()
        {
            var testRoot = new GameObject("TestRoot_PhysicsSim");
            var rooms = HotelLayoutTemplate.CreateMeridianHouseLayout();
            var mats = new Dictionary<string, Material>();

            var hotel = HotelBuilder.BuildHotelEnvironment(testRoot.transform, rooms, mats);
            Assert.IsNotNull(hotel, "Hotel environment must be successfully built.");

            // Verify all 31 doors exist and their casing jambs are properly positioned
            var doorsRoot = testRoot.transform.Find("MeridianHouse_Architecture/Hotel_Doors");
            Assert.IsNotNull(doorsRoot, "Hotel_Doors must exist.");
            Assert.GreaterOrEqual(doorsRoot.childCount, 30, "All hotel doorway connections must have spawned doors.");

            for (int i = 0; i < doorsRoot.childCount; i++)
            {
                var door = doorsRoot.GetChild(i);
                var casing = door.Find("Door_Casing") ?? door.Find("Archway_Casing");
                if (casing != null)
                {
                    Transform left = casing.Find("Casing_Jamb_Left") ?? casing.Find("Arch_Pillar_Left");
                    Transform right = casing.Find("Casing_Jamb_Right") ?? casing.Find("Arch_Pillar_Right");
                    Debug.Log($"DOOR_AUDIT: {door.name} | Rot={door.eulerAngles.y:F1} | LeftWorld={left?.position} | RightWorld={right?.position}");
                }
            }

            for (int i = 0; i < doorsRoot.childCount; i++)
            {
                var door = doorsRoot.GetChild(i);
                var casing = door.Find("Door_Casing") ?? door.Find("Archway_Casing");
                if (casing != null)
                {
                    var jambL = casing.Find("Casing_Jamb_Left") ?? casing.Find("Arch_Pillar_Left");
                    var jambR = casing.Find("Casing_Jamb_Right") ?? casing.Find("Arch_Pillar_Right");
                    Assert.IsNotNull(jambL, $"Door '{door.name}' must have left jamb or pillar.");
                    Assert.IsNotNull(jambR, $"Door '{door.name}' must have right jamb or pillar.");

                    // Verify casing rotation matches door root
                    Assert.AreEqual(Quaternion.identity, casing.localRotation, $"Door '{door.name}' casing localRotation must be identity so it matches wall orientation.");

                    // If door is oriented East-West (Euler 90 or 270 deg), jambs must be aligned along World Z (same World X)
                    float yAngle = door.eulerAngles.y;
                    if (Mathf.Abs(yAngle - 90f) < 5f || Mathf.Abs(yAngle - 270f) < 5f)
                    {
                        Assert.AreEqual(jambL.position.x, jambR.position.x, 0.05f, $"East/West partition door '{door.name}' jambs must have identical World X.");
                        Assert.Greater(Mathf.Abs(jambR.position.z - jambL.position.z), 1.2f, $"East/West partition door '{door.name}' jambs must be separated along World Z.");
                    }
                    else if (Mathf.Abs(yAngle - 0f) < 5f || Mathf.Abs(yAngle - 180f) < 5f)
                    {
                        Assert.AreEqual(jambL.position.z, jambR.position.z, 0.05f, $"North/South partition door '{door.name}' jambs must have identical World Z.");
                        Assert.Greater(Mathf.Abs(jambR.position.x - jambL.position.x), 1.2f, $"North/South partition door '{door.name}' jambs must be separated along World X.");
                    }

                    // Verify clear opening is unobstructed
                    float clearWidth = Mathf.Abs(jambR.localPosition.x - jambL.localPosition.x) - jambL.localScale.x;
                    Assert.GreaterOrEqual(clearWidth, 1.4f, $"Door '{door.name}' clear passage width ({clearWidth}m) must be at least 1.4m.");

                    // Verify threshold sill has no solid collider to snag player feet
                    var threshold = casing.Find("Casing_Threshold");
                    if (threshold != null)
                    {
                        var thCol = threshold.GetComponent<Collider>();
                        Assert.IsNull(thCol, $"Door '{door.name}' threshold must not have a blocking collider.");
                    }

                    // Verify doors on East/West walls are oriented along the wall (90 or 270 degrees, not 0)
                    if (door.name.Contains("corridor_office") || door.name.Contains("corridor_utility") || door.name.Contains("sky_penthouse"))
                    {
                        float yEuler = door.transform.eulerAngles.y;
                        bool isAlignedEW = Mathf.Abs(yEuler - 90f) < 5f || Mathf.Abs(yEuler - 270f) < 5f;
                        Assert.IsTrue(isAlignedEW, $"East/West partition door '{door.name}' must be oriented flush with wall (90 or 270 deg), but was {yEuler} deg.");
                    }

                    // Verify doors on South walls are oriented facing approach (180 degrees)
                    if (door.name.Contains("upper_housekeeping") || door.name.Contains("floor2_cigar") || door.name.Contains("dining_conservatory"))
                    {
                        float yEuler = door.transform.eulerAngles.y;
                        bool isAlignedSouth = Mathf.Abs(yEuler - 180f) < 5f;
                        Assert.IsTrue(isAlignedSouth, $"South wall door '{door.name}' must be oriented at 180 deg, but was {yEuler} deg.");
                    }
                }
            }

            // Verify elevator controller state & landing gates
            var elevatorCabin = testRoot.transform.Find("MeridianHouse_Architecture/Hotel_ArtDecoElevator/Elevator_Cabin");
            Assert.IsNotNull(elevatorCabin, "Elevator cabin must exist.");
            var elevatorComp = elevatorCabin.GetComponent<TomorrowsCrime.World.HotelElevator>();
            Assert.IsNotNull(elevatorComp, "HotelElevator component must be attached to cabin.");
            Assert.IsFalse(elevatorComp.IsMoving, "Elevator must start stationary.");
            Assert.AreEqual(0, elevatorComp.CurrentFloor, "Elevator must start on Floor 0.");

            Object.DestroyImmediate(testRoot);
        }

        [Test]
        public void CentralStaircase_Floor1Walls_DoNotIntersectStaircaseBounds()
        {
            var testRoot = new GameObject("TestRoot_StairsWallClearance");
            var seed = InvestigationSeed.FromCode("TC-1A2B3");
            var investigation = GenerationPipeline.GenerateInvestigation(seed);
            var mats = new Dictionary<string, Material>();
            var world = GenerationPipeline.BuildPlayableWorld(investigation, testRoot.transform, mats);

            // Audit Floor 1 walls against Staircase Flight 0 & Landing 1:
            // Flight 0 Track A spans X in [4.8, 11.2], Z in [-2.55, -0.95].
            // Landing 1 spans X in [11.2, 13.6], Z in [-2.60, 0.70].
            // South partition wall at Z = -2.90m must have ZERO intersection with staircase geometry!
            var renderers = testRoot.GetComponentsInChildren<Renderer>(true);
            int overlapCount = 0;
            foreach (var r in renderers)
            {
                if (r.name.Contains("Wall") || (r.transform.parent != null && r.transform.parent.name.Contains("Wall")))
                {
                    var b = r.bounds;
                    // Check Floor 1 walls (Y in [4.5, 9.5])
                    if (b.min.y >= 4.0f && b.max.y <= 10.5f)
                    {
                        // Check if wall penetrates Flight 0 (X in [4.8, 11.2], Z in [-2.55, -0.95])
                        bool penetratesFlight0 = b.max.x > 4.85f && b.min.x < 11.15f && b.max.z > -2.55f && b.min.z < -0.95f;
                        // Check if wall penetrates Landing 1 (X in [11.2, 13.6], Z in [-2.60, 0.70])
                        bool penetratesLanding1 = b.max.x > 11.25f && b.min.x < 13.55f && b.max.z > -2.60f && b.min.z < 0.70f;

                        if (penetratesFlight0 || penetratesLanding1)
                        {
                            overlapCount++;
                            Debug.LogError($"WALL_OVERLAPS_STAIRS: '{r.gameObject.name}' Bounds={b}");
                        }
                    }
                }
            }

            Assert.AreEqual(0, overlapCount, "No walls on Floor 1 should intersect central staircase Flight 0 or Landing 1.");
            Object.DestroyImmediate(testRoot);
        }

        [Test]
        public void Reception_StaffNPCsAndAnchors_PositionedInFrontOfLockers()
        {
            var testRoot = new GameObject("TestRoot_ReceptionNPCsAndAnchors");
            var seed = InvestigationSeed.FromCode("TC-1A2B3");
            var investigation = GenerationPipeline.GenerateInvestigation(seed);
            var mats = new Dictionary<string, Material>();
            var world = GenerationPipeline.BuildPlayableWorld(investigation, testRoot.transform, mats);

            // Pierre and Celeste must be stationed in front of the locker cabinet (Z in [9.5, 10.5]), facing the lobby (rotation Y = 180)
            var npcs = testRoot.GetComponentsInChildren<NPCController>(true);
            NPCController pierre = null;
            NPCController celeste = null;
            foreach (var npc in npcs)
            {
                if (npc.name.Contains("pierre") || npc.name.Contains("Pierre")) pierre = npc;
                if (npc.name.Contains("celeste") || npc.name.Contains("Celeste")) celeste = npc;
            }

            Assert.IsNotNull(pierre, "Concierge Pierre must be spawned.");
            Assert.IsNotNull(celeste, "Receptionist Celeste must be spawned.");

            Assert.GreaterOrEqual(pierre.transform.position.z, 9.6f, "Pierre must stand in front of key cubby cabinet.");
            Assert.LessOrEqual(pierre.transform.position.z, 10.4f, "Pierre must stand behind reception counter.");

            Assert.GreaterOrEqual(celeste.transform.position.z, 9.6f, "Celeste must stand in front of key cubby cabinet.");
            Assert.LessOrEqual(celeste.transform.position.z, 10.4f, "Celeste must stand behind reception counter.");

            // Verify Reception Anchors sit on valid furniture surfaces
            var receptionRoom = investigation.Rooms.Find(r => r.RoomId == "room_reception");
            Assert.IsNotNull(receptionRoom, "Reception room definition must exist.");

            var counterAnchor = receptionRoom.Anchors.Find(a => a.AnchorId == "anchor_reception_counter");
            var deskAnchor = receptionRoom.Anchors.Find(a => a.AnchorId == "anchor_reception_desk");
            var rackAnchor = receptionRoom.Anchors.Find(a => a.AnchorId == "anchor_reception_key_rack");
            var safeAnchor = receptionRoom.Anchors.Find(a => a.AnchorId == "anchor_reception_safe");

            Assert.IsNotNull(counterAnchor, "anchor_reception_counter must exist.");
            Assert.AreEqual(8.8f, counterAnchor.LocalPosition.z, 0.1f, "Counter anchor must be on front counter surface (Z ~ 8.8m).");

            Assert.IsNotNull(deskAnchor, "anchor_reception_desk must exist.");
            Assert.AreEqual(9.4f, deskAnchor.LocalPosition.z, 0.1f, "Desk anchor must be on desk writing ledge (Z ~ 9.4m).");

            Assert.IsNotNull(rackAnchor, "anchor_reception_key_rack must exist.");
            Assert.AreEqual(10.9f, rackAnchor.LocalPosition.z, 0.1f, "Key rack anchor must be inside key cubby unit (Z ~ 10.9m).");

            Assert.IsNotNull(safeAnchor, "anchor_reception_safe must exist.");
            Assert.AreEqual(13.5f, safeAnchor.LocalPosition.z, 0.1f, "Safe anchor must be atop executive safe (Z ~ 13.5m).");

            Object.DestroyImmediate(testRoot);
        }

        [Test]
        public void DoorLeaves_DecorativePanels_FitStrictlyInsideDoorSlab()
        {
            var testRoot = new GameObject("TestRoot_DoorPanelsFit");
            var seed = InvestigationSeed.FromCode("TC-1A2B3");
            var investigation = GenerationPipeline.GenerateInvestigation(seed);
            var mats = new Dictionary<string, Material>();
            var world = GenerationPipeline.BuildPlayableWorld(investigation, testRoot.transform, mats);

            var doorsRoot = testRoot.transform.Find($"World_Investigation_{seed.SeedCode}/MeridianHouse_Architecture/Hotel_Doors");
            Assert.IsNotNull(doorsRoot, "Hotel_Doors root must exist.");

            int panelsAudited = 0;
            for (int i = 0; i < doorsRoot.childCount; i++)
            {
                var door = doorsRoot.GetChild(i);
                var doorLeaves = door.GetComponentsInChildren<Transform>(true);
                foreach (var t in doorLeaves)
                {
                    if (t.name == "Door_Leaf")
                    {
                        var leafCol = t.GetComponent<BoxCollider>();
                        Assert.IsNotNull(leafCol, $"Door leaf '{t.name}' must have BoxCollider.");
                        float leafMinY = leafCol.bounds.min.y;
                        float leafMaxY = leafCol.bounds.max.y;

                        for (int c = 0; c < t.childCount; c++)
                        {
                            var child = t.GetChild(c);
                            var rend = child.GetComponent<Renderer>();
                            if (rend != null)
                            {
                                panelsAudited++;
                                Assert.GreaterOrEqual(rend.bounds.min.y, leafMinY - 0.05f,
                                    $"Detail '{child.name}' on '{door.name}' must not protrude below bottom of door leaf.");
                                Assert.LessOrEqual(rend.bounds.max.y, leafMaxY + 0.05f,
                                    $"Detail '{child.name}' on '{door.name}' must not protrude above top of door leaf.");
                            }
                        }
                    }
                }
            }

            Assert.Greater(panelsAudited, 20, "Should have audited decorative panels on all door leaves.");
            Object.DestroyImmediate(testRoot);
        }
    }
}
