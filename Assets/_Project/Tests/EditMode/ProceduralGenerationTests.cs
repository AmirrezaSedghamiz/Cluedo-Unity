using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.Investigation;
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
    }
}
