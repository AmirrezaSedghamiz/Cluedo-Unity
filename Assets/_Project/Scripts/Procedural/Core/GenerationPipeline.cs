using System;
using System.Collections.Generic;
using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.Investigation;
using TomorrowsCrime.Procedural.Hotel;
using TomorrowsCrime.Procedural.NPCs;
using TomorrowsCrime.Procedural.Objects;
using TomorrowsCrime.Procedural.Mystery;
using TomorrowsCrime.Procedural.Validation;

namespace TomorrowsCrime.Procedural.Core
{
    /// <summary>
    /// Encapsulates the complete generated data payload of an investigation.
    /// </summary>
    public class GeneratedInvestigation
    {
        public InvestigationSeed Seed { get; set; }
        public List<RoomDefinition> Rooms { get; set; }
        public MysteryGroundTruth GroundTruth { get; set; }
        public List<NPCProfile> Roster { get; set; }
        public Dictionary<string, NPCRoutinePlan> Routines { get; set; }
        public ValidationResult Validation { get; set; }
        public int AttemptCount { get; set; }
    }

    /// <summary>
    /// Master generation pipeline coordinating layout, scenario truth, solvability validation,
    /// NPC population, routine schedules, and room object placements.
    /// </summary>
    public static class GenerationPipeline
    {
        private const int MaxRetries = 10;

        public static GeneratedInvestigation GenerateInvestigation(InvestigationSeed seed, bool validateSolvability = true)
        {
            if (seed == null) seed = InvestigationSeed.GenerateRandom();

            var result = new GeneratedInvestigation
            {
                Seed = seed,
                AttemptCount = 0
            };

            // Step 1: Select Layout with Seeded Jitter & Styling Variations
            result.Rooms = HotelLayoutTemplate.CreateMeridianHouseLayout(seed.LayoutStream);

            // Step 2 & 3: Generate and Validate Mystery Scenario with Retry Policy
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                result.AttemptCount = attempt;
                var attemptSeed = attempt == 1 ? seed.ScenarioStream : seed.ScenarioStream.CreateSubStream($"Retry_{attempt}");

                var truth = CrimeScenarioGenerator.GenerateScenario(attemptSeed);
                var validation = MysterySolvabilityValidator.ValidateScenario(truth, result.Rooms);

                if (!validateSolvability || validation.IsValid)
                {
                    result.GroundTruth = truth;
                    result.Validation = validation;
                    break;
                }

                if (attempt == MaxRetries)
                {
                    Debug.LogError($"[GenerationPipeline] Failed to generate a solvable mystery after {MaxRetries} attempts. Last failure: {validation}");
                    result.GroundTruth = truth;
                    result.Validation = validation;
                }
            }

            // Step 4: Generate NPC Population
            result.Roster = NPCPopulationGenerator.GenerateRoster(
                seed.NPCStream,
                result.Rooms,
                result.GroundTruth.VictimId,
                result.GroundTruth.CulpritId,
                result.GroundTruth.Motive
            );

            // Step 5: Generate NPC Routines
            result.Routines = NPCPopulationGenerator.GenerateRoutines(result.Roster, seed.NPCStream);

            return result;
        }

        /// <summary>
        /// Instantiates the generated investigation into a playable physical Unity environment.
        /// </summary>
        public static GameObject BuildPlayableWorld(GeneratedInvestigation investigation, Transform parent, Dictionary<string, Material> mats)
        {
            if (investigation == null) throw new ArgumentNullException(nameof(investigation));

            var worldRoot = new GameObject($"World_Investigation_{investigation.Seed.SeedCode}");
            if (parent != null) worldRoot.transform.SetParent(parent);

            // 1. Build Physical Hotel Architecture & Colliders
            var hotelArch = HotelBuilder.BuildHotelEnvironment(worldRoot.transform, investigation.Rooms, mats);

            // 2. Populate Rooms with Objects & Scenario Evidence
            RoomObjectPlacer.PopulateRooms(worldRoot.transform, investigation.Rooms, investigation.Seed.ObjectStream,
                mats, investigation.GroundTruth.MandatoryObjectPlacements);

            // 3. Clear Case-Specific Knowledge on New Investigation
            var knowledge = InvestigationKnowledgeService.Instance;
            if (knowledge != null)
            {
                knowledge.Clear();
                Debug.Log("[GenerationPipeline] Initialized fresh investigation knowledge service.");
            }

            return worldRoot;
        }
    }
}
