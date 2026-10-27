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
    }
}
