using System.Collections.Generic;
using UnityEngine;
using TomorrowsCrime.Procedural.Core;

namespace TomorrowsCrime.Procedural.Mystery
{
    /// <summary>
    /// Procedural scenario generator that creates logically coherent murder mysteries.
    /// Selects victim, culprit, motive, method, crime scene, causal timeline, and evidence threads.
    /// </summary>
    public static class CrimeScenarioGenerator
    {
        private struct VictimCandidate
        {
            public string Id;
            public string Name;
            public string RoomId;
            public string KeyId;
        }

        private struct CulpritCandidate
        {
            public string Id;
            public string Name;
            public string BaseRoomId;
        }

        public static MysteryGroundTruth GenerateScenario(SeededRandom rng)
        {
            var truth = new MysteryGroundTruth();

            // 1. Select Victim
            var victims = new List<VictimCandidate>
            {
                new() { Id = "npc_madame_valerie", Name = "Madame Valerie", RoomId = "room_guest_102", KeyId = "obj_key_room_102" },
                new() { Id = "npc_lord_harrington", Name = "Lord Harrington", RoomId = "room_guest_101", KeyId = "obj_key_room_101" },
                new() { Id = "npc_manager_adler", Name = "Julian Adler", RoomId = "room_staff_office", KeyId = "obj_key_staff_office" }
            };

            var selectedVictim = rng.NextItem(victims);
            truth.VictimId = selectedVictim.Id;
            truth.VictimName = selectedVictim.Name;
            truth.CrimeSceneRoomId = selectedVictim.RoomId;
            truth.RequiredAccessKeyId = selectedVictim.KeyId;

            // 2. Select Culprit from remaining suspects
            var culprits = new List<CulpritCandidate>
            {
                new() { Id = "npc_chef_marcel", Name = "Chef Marcel", BaseRoomId = "room_kitchen" },
                new() { Id = "npc_maid_clara", Name = "Clara Vance", BaseRoomId = "room_housekeeping" },
                new() { Id = "npc_engineer_otto", Name = "Otto Krause", BaseRoomId = "room_utility" },
                new() { Id = "npc_journalist_monroe", Name = "Vivian Monroe", BaseRoomId = "room_lobby" },
                new() { Id = "npc_dr_sterling", Name = "Dr. Sterling", BaseRoomId = "room_guest_103" },
                new() { Id = "npc_concierge_pierre", Name = "Concierge Pierre", BaseRoomId = "room_reception" }
            };

            // Ensure culprit is not the victim
            culprits.RemoveAll(c => c.Id == truth.VictimId);
            var selectedCulprit = rng.NextItem(culprits);
            truth.CulpritId = selectedCulprit.Id;
            truth.CulpritName = selectedCulprit.Name;

            // 3. Select Motive
            var motives = new List<string>
            {
                "Blackmail: The victim discovered the culprit's embezzled debts and demanded silence.",
                "Inheritance: The culprit was written out of the victim's will and acted to secure the fortune.",
                "Contraband: The victim threatened to turn the culprit over to Scotland Yard for illicit smuggling.",
                "Vengeance: A decades-old family grudge triggered by an encounter in the dining hall."
            };
            truth.Motive = rng.NextItem(motives);

            // 4. Select Method & Murder Weapon
            var methods = new List<MurderMethod>
            {
                MurderMethod.PoisonTincture,
                MurderMethod.BluntForceCandlestick,
                MurderMethod.BluntForceWrench,
                MurderMethod.SharpCleaver
            };
            truth.Method = rng.NextItem(methods);

            switch (truth.Method)
            {
                case MurderMethod.PoisonTincture:
                    truth.MurderWeaponObjectId = "obj_poison_vial";
                    truth.CaseTitle = $"The Cyanide Draught of {truth.VictimName}";
                    break;
                case MurderMethod.BluntForceCandlestick:
                    truth.MurderWeaponObjectId = "obj_brass_candlestick";
                    truth.CaseTitle = $"The Golden Candlestick Tragedy";
                    break;
                case MurderMethod.BluntForceWrench:
                    truth.MurderWeaponObjectId = "obj_maintenance_wrench";
                    truth.CaseTitle = $"The Iron Pipe Wrench Homicide";
                    break;
                case MurderMethod.SharpCleaver:
                    truth.MurderWeaponObjectId = "obj_chef_cleaver";
                    truth.CaseTitle = $"The Kitchen Cleaver Affair";
                    break;
            }

            // Murder happens between 02:30 (150s) and 03:30 (210s)
            truth.MurderTimestamp = rng.NextFloat(150f, 210f);

            // 5. Construct Causal Crime Timeline
            float tAcquire = truth.MurderTimestamp - 70f; // Acquire key or weapon
            float tEnter = truth.MurderTimestamp - 30f;   // Enters crime scene
            float tMurder = truth.MurderTimestamp;        // Strike / Poisoning
            float tFlee = truth.MurderTimestamp + 25f;    // Leaves crime scene

            truth.CrimeTimeline.Add(new TimelineCausalEvent("evt_crime_acquire_means", tAcquire,
                truth.CulpritId, "AcquireMeans", selectedCulprit.BaseRoomId, truth.MurderWeaponObjectId,
                $"{truth.CulpritName} takes the {truth.MurderWeaponObjectId} from their station."));

            truth.CrimeTimeline.Add(new TimelineCausalEvent("evt_crime_enter_scene", tEnter,
                truth.CulpritId, "EnterCrimeScene", truth.CrimeSceneRoomId, truth.RequiredAccessKeyId,
                $"{truth.CulpritName} enters {truth.CrimeSceneRoomId} using {truth.RequiredAccessKeyId}."));

            truth.CrimeTimeline.Add(new TimelineCausalEvent("evt_crime_execute_murder", tMurder,
                truth.CulpritId, "ExecuteMurder", truth.CrimeSceneRoomId, truth.MurderWeaponObjectId,
                $"{truth.CulpritName} murders {truth.VictimName} using {truth.Method}."));

            truth.CrimeTimeline.Add(new TimelineCausalEvent("evt_crime_flee_scene", tFlee,
                truth.CulpritId, "FleeScene", "room_corridor_ground", null,
                $"{truth.CulpritName} flees the crime scene, discarding trace evidence."));

            // 6. Mandatory Object Placements (ensures key, weapon, and documents spawn)
            // Put the required key where it can be discovered/intercepted
            truth.MandatoryObjectPlacements[truth.RequiredAccessKeyId] = "room_lobby"; // On lobby table or reception
            truth.MandatoryObjectPlacements[truth.MurderWeaponObjectId] = selectedCulprit.BaseRoomId; // Weapon in culprit's domain
            truth.MandatoryObjectPlacements["obj_blackmail_letter"] = truth.CrimeSceneRoomId; // Letter at crime scene

            // 7. Evidence Threads
            truth.RequiredEvidenceLinks.Add(new EvidenceClueLink(
                "evidence_murder_weapon", "PhysicalWeapon", selectedCulprit.BaseRoomId, truth.MurderWeaponObjectId,
                $"The {truth.MurderWeaponObjectId} displays traces linking it to the crime in {truth.CrimeSceneRoomId}."));

            truth.RequiredEvidenceLinks.Add(new EvidenceClueLink(
                "evidence_access_key", "AccessKey", "room_lobby", truth.RequiredAccessKeyId,
                $"Key '{truth.RequiredAccessKeyId}' granted access into the locked {truth.CrimeSceneRoomId}."));

            truth.RequiredEvidenceLinks.Add(new EvidenceClueLink(
                "evidence_motive_letter", "MotiveDocument", truth.CrimeSceneRoomId, "obj_blackmail_letter",
                $"Letter found in {truth.CrimeSceneRoomId} proving motive: '{truth.Motive}'."));

            // 8. Innocent Suspect Alibis (Allows deductive elimination!)
            foreach (var c in culprits)
            {
                if (c.Id == truth.CulpritId) continue;
                truth.ExoneratedSuspectIds.Add(c.Id);
                truth.SuspectAlibis[c.Id] = $"{c.Name} was verified stationed at {c.BaseRoomId} during the critical murder window ({tEnter:F0}s - {tFlee:F0}s).";
            }

            return truth;
        }
    }
}
