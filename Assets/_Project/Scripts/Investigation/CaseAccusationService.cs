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
                VictimId = "npc_guest_102",
                VictimName = "Madame Valerie",
                CulpritId = "npc_concierge",
                CulpritName = "Concierge Pierre",
                Motive = "Blackmail Silence",
                Method = MurderMethod.SharpCleaver,
                MurderWeaponObjectId = "evidence_room102_keycard", // Access key instrumental in breach
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
                "Celeste Moreau (Receptionist)",
                "Chef Marcel Laurent",
                "Lord Harrington",
                "Dr. Alistair Sterling",
                "Beatrice Vane (Maid)",
                "Arthur Pendelton (Engineer)"
            };
        }

        public List<string> GetKnownWeapons()
        {
            return new List<string>
            {
                "Heavy Meat Cleaver",
                "Heavy Brass Candlestick",
                "Amber Alkaloid Poison Vial",
                "Steel Pipe Wrench",
                "Stolen Room 102 Master Key"
            };
        }

        public List<string> GetKnownCrimeScenes()
        {
            return new List<string>
            {
                "Room 102 Suite",
                "Room 101 Suite",
                "Room 103 Suite",
                "Front Reception Desk",
                "Kitchen & Pantry",
                "Executive Staff Office",
                "Dining Hall",
                "Maintenance Utility Room"
            };
        }

        public List<string> GetKnownMotives()
        {
            return new List<string>
            {
                "Blackmail Silence",
                "Financial Embezzlement",
                "Revenge & Spite",
                "Family Inheritance",
                "Stolen Industrial Patents"
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

            var result = new AccusationResult
            {
                LoopsElapsed = loops,
                SimSeconds = elapsed,
                EvidenceDiscoveredCount = clues
            };

            // Evaluate Culprit
            bool culpritMatch = !string.IsNullOrEmpty(accusedSuspect) && (
                accusedSuspect.ToLower().Contains(_currentGroundTruth.CulpritName.ToLower()) ||
                _currentGroundTruth.CulpritName.ToLower().Contains(accusedSuspect.ToLower()) ||
                accusedSuspect.ToLower().Contains("pierre") && _currentGroundTruth.CulpritId.Contains("concierge") ||
                accusedSuspect.ToLower().Contains("marcel") && _currentGroundTruth.CulpritId.Contains("chef") ||
                accusedSuspect.ToLower().Contains("celeste") && _currentGroundTruth.CulpritId.Contains("receptionist") ||
                accusedSuspect.ToLower().Contains("harrington") && _currentGroundTruth.CulpritId.Contains("harrington") ||
                accusedSuspect.ToLower().Contains("sterling") && _currentGroundTruth.CulpritId.Contains("sterling"));
            result.IsCulpritCorrect = culpritMatch;

            // Evaluate Weapon
            bool weaponMatch = !string.IsNullOrEmpty(chosenWeapon) && (
                chosenWeapon.ToLower().Contains("cleaver") && _currentGroundTruth.Method == MurderMethod.SharpCleaver ||
                chosenWeapon.ToLower().Contains("candlestick") && _currentGroundTruth.Method == MurderMethod.BluntForceCandlestick ||
                chosenWeapon.ToLower().Contains("poison") && _currentGroundTruth.Method == MurderMethod.PoisonTincture ||
                chosenWeapon.ToLower().Contains("wrench") && _currentGroundTruth.Method == MurderMethod.BluntForceWrench ||
                chosenWeapon.ToLower().Contains("key") && _currentGroundTruth.Method == MurderMethod.SharpCleaver);
            result.IsWeaponCorrect = weaponMatch;

            // Evaluate Crime Scene Room
            bool roomMatch = !string.IsNullOrEmpty(chosenRoom) && (
                chosenRoom.ToLower().Contains("102") && _currentGroundTruth.CrimeSceneRoomId.ToLower().Contains("102") ||
                chosenRoom.ToLower().Contains("101") && _currentGroundTruth.CrimeSceneRoomId.ToLower().Contains("101") ||
                chosenRoom.ToLower().Contains("103") && _currentGroundTruth.CrimeSceneRoomId.ToLower().Contains("103") ||
                chosenRoom.ToLower().Contains("kitchen") && _currentGroundTruth.CrimeSceneRoomId.ToLower().Contains("kitchen") ||
                chosenRoom.ToLower().Contains("office") && _currentGroundTruth.CrimeSceneRoomId.ToLower().Contains("office") ||
                chosenRoom.ToLower().Contains("reception") && _currentGroundTruth.CrimeSceneRoomId.ToLower().Contains("reception") ||
                chosenRoom.ToLower().Contains("dining") && _currentGroundTruth.CrimeSceneRoomId.ToLower().Contains("dining") ||
                chosenRoom.ToLower().Contains("utility") && _currentGroundTruth.CrimeSceneRoomId.ToLower().Contains("utility"));
            result.IsRoomCorrect = roomMatch;

            // Evaluate Motive
            bool motiveMatch = !string.IsNullOrEmpty(chosenMotive) && (
                chosenMotive.ToLower().Contains("blackmail") ||
                chosenMotive.ToLower().Contains("embezzle") ||
                chosenMotive.ToLower().Contains("revenge") ||
                chosenMotive.ToLower().Contains(_currentGroundTruth.Motive.ToLower().Substring(0, Mathf.Min(4, _currentGroundTruth.Motive.Length))));
            result.IsMotiveCorrect = motiveMatch;

            // Overall Victory Check (Culprit + Weapon + Room must be strictly correct; Motive gives bonus rank)
            result.IsCorrect = result.IsCulpritCorrect && result.IsWeaponCorrect && result.IsRoomCorrect;

            if (result.IsCorrect)
            {
                CasesSolvedCount++;
                result.Title = "CASE SOLVED — THE MYSTERY OF MERIDIAN HOUSE RESOLVED";
                result.DetailedResolution =
                    $"Brilliant deduction, Detective!\n\n" +
                    $"You correctly unmasked {_currentGroundTruth.CulpritName} as the perpetrator! " +
                    $"Using the {_currentGroundTruth.Method} inside the {_currentGroundTruth.CrimeSceneRoomId}, " +
                    $"{_currentGroundTruth.CulpritName} attempted to permanently conceal their involvement driven by {_currentGroundTruth.Motive}.\n\n" +
                    $"By reconstructing the time loop timeline and cross-referencing discovered physical clues, " +
                    $"you trapped the culprit before they could escape into the foggy London midnight.\n\n" +
                    $"The time loop breaks — dawn breaks over Meridian House at last!";

                result.SleuthRank = loops <= 2 ? "Master Detective (S-Rank)" : (loops <= 4 ? "Astute Sleuth (A-Rank)" : "Persistent Inquirer (B-Rank)");

                AudioManager.Instance?.PlayVictory();
            }
            else
            {
                result.Title = "ACCUSATION FAILED — FLAW IN THE DEDUCTION";
                List<string> flaws = new();
                if (!result.IsCulpritCorrect) flaws.Add($"{accusedSuspect} produces an airtight alibi witnessed by other guests");
                if (!result.IsWeaponCorrect) flaws.Add($"the forensic markings contradict the use of {chosenWeapon}");
                if (!result.IsRoomCorrect) flaws.Add($"the crime scene indicators do not match {chosenRoom}");

                result.DetailedResolution =
                    $"Your accusation collapses under scrutiny!\n\n" +
                    $"{string.Join(", and ", flaws)}.\n\n" +
                    $"The true culprit remains at large in Meridian House. " +
                    $"As your deduction falls apart, the heavy grandfather clock in the lobby tolls midnight...\n\n" +
                    $"TIME FRACTURES. THE FIVE-MINUTE LOOP RESETS NOW!";

                result.RebuttalReason = string.Join("; ", flaws);
                AudioManager.Instance?.PlayDefeat();
            }

            return result;
        }
    }
}
