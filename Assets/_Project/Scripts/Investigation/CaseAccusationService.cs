using System;
using System.Collections.Generic;
using UnityEngine;
using TomorrowsCrime.Audio;
using TomorrowsCrime.Procedural.Mystery;
using TomorrowsCrime.Simulation;
using TomorrowsCrime.UI.Core;

namespace TomorrowsCrime.Investigation
{
    [Serializable]
    public class AccusationResult
    {
        public bool IsCorrect;
        public bool IsCulpritCorrect;
        public bool IsWeaponCorrect;
        public bool IsRoomCorrect;
        public bool IsMotiveCorrect;
        public string Title;
        public string DetailedResolution;
        public string RebuttalReason;
        public int LoopsElapsed;
        public float SimSeconds;
        public int EvidenceDiscoveredCount;
        public string SleuthRank;
    }

    /// <summary>
    /// Master evaluation service for testing detective deductions against scenario ground truth.
    /// Handles the dramatic climax of the murder mystery, awarding case resolution on correct deductions
    /// or triggering alibi rebuttals and time-loop turnover on failures.
    /// Updated with full support for all 6 procedural scenario archetypes and 9 suspects.
    /// </summary>
    public class CaseAccusationService : MonoBehaviour
    {
        private static CaseAccusationService _instance;
        public static CaseAccusationService Instance => _instance;

        private MysteryGroundTruth _currentGroundTruth;
        public MysteryGroundTruth CurrentGroundTruth => _currentGroundTruth;

        public static int CasesSolvedCount { get; private set; } = 0;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // Initialize default baseline prototype ground truth if none set
            if (_currentGroundTruth == null)
            {
                SetBaselineGroundTruth();
            }
        }

        private void SetBaselineGroundTruth()
        {
            _currentGroundTruth = new MysteryGroundTruth
            {
                CaseTitle = "The Midnight Key Incident",
                VictimId = "npc_madame_valerie",
                VictimName = "Madame Valerie",
                CulpritId = "npc_concierge_pierre",
                CulpritName = "Concierge Pierre",
                Motive = "Blackmail Silence",
                Method = MurderMethod.SharpCleaver,
                MurderWeaponObjectId = "evidence_room102_keycard",
                CrimeSceneRoomId = "Room 102 Suite",
                MurderTimestamp = 210f
            };
        }

        public void SetGroundTruth(MysteryGroundTruth truth)
        {
            if (truth != null)
            {
                _currentGroundTruth = truth;
                Debug.Log($"[CaseAccusationService] Loaded Ground Truth: {truth}");
            }
        }

        public List<string> GetKnownSuspects()
        {
            return new List<string>
            {
                "Concierge Pierre",
                "Chef Marcel",
                "Dr. Sterling",
                "Lord Harrington",
                "Madame Valerie",
                "Julian Adler (Manager)",
                "Clara Vance (Housekeeping)",
                "Otto Krause (Engineer)",
                "Vivian Monroe (Journalist)"
            };
        }

        public List<string> GetKnownWeapons()
        {
            return new List<string>
            {
                "Heavy Meat Cleaver",
                "Heavy Brass Candlestick",
                "Amber Alkaloid Poison Vial",
                "Steel Pipe Wrench"
            };
        }

        public List<string> GetKnownCrimeScenes()
        {
            return new List<string>
            {
                "Room 102 Suite",
                "Room 102 VIP Suite",
                "Room 101 Guest Suite",
                "Executive Staff Office",
                "Maintenance Utility Room",
                "Grand Dining Hall",
                "Hotel Kitchen & Scullery"
            };
        }

        public List<string> GetKnownMotives()
        {
            return new List<string>
            {
                "Blackmail Silence",
                "Financial Embezzlement",
                "Family Inheritance",
                "Stolen Industrial Patents",
                "Contraband Jewels"
            };
        }

        public AccusationResult FormulateAccusation(string accusedSuspect, string chosenWeapon, string chosenRoom, string chosenMotive)
        {
            if (_currentGroundTruth == null) SetBaselineGroundTruth();

            int loops = SimulationManager.Instance != null && SimulationManager.Instance.Clock != null
                ? SimulationManager.Instance.Clock.LoopCount
                : 1;
            float elapsed = SimulationManager.Instance != null && SimulationManager.Instance.Clock != null
                ? SimulationManager.Instance.Clock.ElapsedSeconds
                : 0f;
            int clues = InvestigationKnowledgeService.Instance != null
                ? InvestigationKnowledgeService.Instance.EvidenceCount
                : 0;

            var result = EvaluateAccusation(_currentGroundTruth, accusedSuspect, chosenWeapon, chosenRoom, chosenMotive, loops, elapsed, clues);

            if (result.IsCorrect)
            {
                CasesSolvedCount++;
                AudioManager.Instance?.PlayVictory();
            }
            else
            {
                AudioManager.Instance?.PlayDefeat();
            }

            return result;
        }

        /// <summary>
        /// Pure evaluation function testing deductions against ground truth.
        /// Fully decoupled from MonoBehaviour for 100% testability.
        /// </summary>
        public static AccusationResult EvaluateAccusation(
            MysteryGroundTruth groundTruth,
            string accusedSuspect,
            string chosenWeapon,
            string chosenRoom,
            string chosenMotive,
            int loops = 1,
            float elapsed = 0f,
            int clues = 0)
        {
            if (groundTruth == null) throw new ArgumentNullException(nameof(groundTruth));

            var result = new AccusationResult
            {
                LoopsElapsed = loops,
                SimSeconds = elapsed,
                EvidenceDiscoveredCount = clues
            };

            string accusedLower = accusedSuspect != null ? accusedSuspect.ToLower() : "";
            string weaponLower = chosenWeapon != null ? chosenWeapon.ToLower() : "";
            string roomLower = chosenRoom != null ? chosenRoom.ToLower() : "";
            string motiveLower = chosenMotive != null ? chosenMotive.ToLower() : "";

            // 1. Evaluate Culprit
            bool culpritMatch = false;
            if (!string.IsNullOrEmpty(accusedLower))
            {
                string targetNameLower = (groundTruth.CulpritName ?? "").ToLower();
                string targetIdLower = (groundTruth.CulpritId ?? "").ToLower();

                culpritMatch = accusedLower.Contains(targetNameLower) || targetNameLower.Contains(accusedLower) ||
                    (accusedLower.Contains("pierre") && targetIdLower.Contains("pierre")) ||
                    (accusedLower.Contains("marcel") && targetIdLower.Contains("marcel")) ||
                    (accusedLower.Contains("sterling") && targetIdLower.Contains("sterling")) ||
                    (accusedLower.Contains("harrington") && targetIdLower.Contains("harrington")) ||
                    (accusedLower.Contains("adler") && targetIdLower.Contains("adler")) ||
                    (accusedLower.Contains("clara") && targetIdLower.Contains("clara")) ||
                    (accusedLower.Contains("otto") && targetIdLower.Contains("otto")) ||
                    (accusedLower.Contains("monroe") && targetIdLower.Contains("monroe")) ||
                    (accusedLower.Contains("valerie") && targetIdLower.Contains("valerie"));
            }
            result.IsCulpritCorrect = culpritMatch;

            // 2. Evaluate Weapon
            bool weaponMatch = false;
            if (!string.IsNullOrEmpty(weaponLower))
            {
                weaponMatch =
                    (weaponLower.Contains("cleaver") && groundTruth.Method == MurderMethod.SharpCleaver) ||
                    (weaponLower.Contains("candlestick") && groundTruth.Method == MurderMethod.BluntForceCandlestick) ||
                    (weaponLower.Contains("poison") && groundTruth.Method == MurderMethod.PoisonTincture) ||
                    (weaponLower.Contains("wrench") && groundTruth.Method == MurderMethod.BluntForceWrench);
            }
            result.IsWeaponCorrect = weaponMatch;

            // 3. Evaluate Crime Scene Room
            bool roomMatch = false;
            if (!string.IsNullOrEmpty(roomLower))
            {
                string sceneTarget = (groundTruth.CrimeSceneRoomId ?? "").ToLower();
                roomMatch =
                    (roomLower.Contains("102") && sceneTarget.Contains("102")) ||
                    (roomLower.Contains("101") && sceneTarget.Contains("101")) ||
                    (roomLower.Contains("office") && sceneTarget.Contains("office")) ||
                    (roomLower.Contains("utility") && sceneTarget.Contains("utility")) ||
                    (roomLower.Contains("kitchen") && sceneTarget.Contains("kitchen")) ||
                    (roomLower.Contains("dining") && sceneTarget.Contains("dining"));
            }
            result.IsRoomCorrect = roomMatch;

            // 4. Evaluate Motive
            bool motiveMatch = false;
            if (!string.IsNullOrEmpty(motiveLower))
            {
                string trueMotiveLower = (groundTruth.Motive ?? "").ToLower();
                motiveMatch =
                    (motiveLower.Contains("blackmail") && trueMotiveLower.Contains("blackmail")) ||
                    (motiveLower.Contains("embezzle") && trueMotiveLower.Contains("embezzle")) ||
                    (motiveLower.Contains("inherit") && trueMotiveLower.Contains("inherit")) ||
                    (motiveLower.Contains("patent") && trueMotiveLower.Contains("patent")) ||
                    (motiveLower.Contains("jewel") && (trueMotiveLower.Contains("jewel") || trueMotiveLower.Contains("contraband"))) ||
                    (motiveLower.Contains("contraband") && (trueMotiveLower.Contains("jewel") || trueMotiveLower.Contains("contraband")));
            }
            result.IsMotiveCorrect = motiveMatch;

            // Overall Victory Check (Culprit + Weapon + Room strictly required; Motive grants S-Rank)
            result.IsCorrect = result.IsCulpritCorrect && result.IsWeaponCorrect && result.IsRoomCorrect;

            if (result.IsCorrect)
            {
                result.Title = "CASE SOLVED — THE MYSTERY OF MERIDIAN HOUSE RESOLVED";
                result.DetailedResolution =
                    $"Brilliant deduction, Detective!\n\n" +
                    $"You correctly unmasked {groundTruth.CulpritName} as the perpetrator! " +
                    $"Using the {groundTruth.Method} inside {groundTruth.CrimeSceneRoomId}, " +
                    $"{groundTruth.CulpritName} acted driven by {groundTruth.Motive}.\n\n" +
                    $"Through methodical clue collection and cross-loop timelines, you solved the crime before midnight concluded.\n\n" +
                    $"The time loop shatters — dawn finally breaks over London!";

                result.SleuthRank = (loops <= 2 && result.IsMotiveCorrect) ? "Master Detective (S-Rank)" : (loops <= 4 ? "Astute Sleuth (A-Rank)" : "Persistent Inquirer (B-Rank)");
            }
            else
            {
                result.Title = "ACCUSATION FAILED — FLAW IN THE DEDUCTION";
                List<string> flaws = new();

                if (!result.IsCulpritCorrect)
                {
                    if (groundTruth.SuspectAlibis != null && groundTruth.SuspectAlibis.TryGetValue(accusedSuspect, out string alibi))
                    {
                        flaws.Add(alibi);
                    }
                    else
                    {
                        flaws.Add($"{accusedSuspect} presents an airtight alibi verified by other witnesses");
                    }
                }

                if (!result.IsWeaponCorrect)
                {
                    flaws.Add($"the forensic markings contradict the use of {chosenWeapon}");
                }

                if (!result.IsRoomCorrect)
                {
                    flaws.Add($"the physical evidence indicates the crime occurred elsewhere, not in {chosenRoom}");
                }

                result.DetailedResolution =
                    $"Your accusation collapses under scrutiny!\n\n" +
                    $"{string.Join(", and ", flaws)}.\n\n" +
                    $"The real killer remains at large. The heavy grandfather clock in the foyer tolls midnight...\n\n" +
                    $"THE TIMELINE COLLAPSES. THE FIVE-MINUTE LOOP RESETS NOW!";

                result.RebuttalReason = string.Join("; ", flaws);
            }

            return result;
        }
    }
}
