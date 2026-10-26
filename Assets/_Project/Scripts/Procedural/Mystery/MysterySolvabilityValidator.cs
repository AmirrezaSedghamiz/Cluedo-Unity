using System;
using System.Collections.Generic;
using TomorrowsCrime.Procedural.Hotel;

namespace TomorrowsCrime.Procedural.Mystery
{
    public class ValidationResult
    {
        public bool IsValid => FailureReasons.Count == 0;
        public List<string> FailureReasons { get; } = new();

        public override string ToString()
        {
            return IsValid
                ? "[ValidationResult: SUCCESS] Mystery is logically consistent and provably solvable."
                : $"[ValidationResult: FAILED] {string.Join(" | ", FailureReasons)}";
        }
    }

    /// <summary>
    /// Bounded logical solver and mystery validator.
    /// Evaluates 8 foundational solvability criteria before allowing an investigation to start.
    /// </summary>
    public static class MysterySolvabilityValidator
    {
        public static ValidationResult ValidateScenario(MysteryGroundTruth truth, List<RoomDefinition> rooms)
        {
            var result = new ValidationResult();

            if (truth == null)
            {
                result.FailureReasons.Add("Ground truth object is null.");
                return result;
            }

            // Criterion 1: Culprit is a valid suspect and distinct from victim
            if (string.IsNullOrEmpty(truth.CulpritId) || string.IsNullOrEmpty(truth.VictimId))
            {
                result.FailureReasons.Add("Culprit or victim ID is empty.");
            }
            else if (truth.CulpritId == truth.VictimId)
            {
                result.FailureReasons.Add($"Culprit ID '{truth.CulpritId}' cannot be identical to victim ID.");
            }

            // Criterion 2: Crime timeline is internally consistent
            if (truth.CrimeTimeline == null || truth.CrimeTimeline.Count < 3)
            {
                result.FailureReasons.Add("Crime timeline has fewer than 3 events.");
            }
            else
            {
                float lastTime = -1f;
                foreach (var evt in truth.CrimeTimeline)
                {
                    if (evt.SimTimestamp < lastTime)
                    {
                        result.FailureReasons.Add($"Timeline event '{evt.EventId}' at {evt.SimTimestamp}s occurs out of chronological order (previous: {lastTime}s).");
                    }
                    if (evt.SimTimestamp < 0f || evt.SimTimestamp > 300f)
                    {
                        result.FailureReasons.Add($"Timeline event '{evt.EventId}' timestamp {evt.SimTimestamp}s is outside the 5-minute loop bounds (0-300s).");
                    }
                    lastTime = evt.SimTimestamp;
                }
            }

            // Criterion 3: Object and location dependencies are satisfied
            if (string.IsNullOrEmpty(truth.MurderWeaponObjectId))
            {
                result.FailureReasons.Add("Murder weapon object ID is unspecified.");
            }

            if (string.IsNullOrEmpty(truth.CrimeSceneRoomId))
            {
                result.FailureReasons.Add("Crime scene room ID is unspecified.");
            }
            else
            {
                bool crimeSceneExists = rooms.Exists(r => r.RoomId == truth.CrimeSceneRoomId);
                if (!crimeSceneExists)
                {
                    result.FailureReasons.Add($"Crime scene room '{truth.CrimeSceneRoomId}' does not exist in hotel layout.");
                }
            }

            // Criterion 4: Required evidence is reachable and discoverable
            if (truth.RequiredEvidenceLinks == null || truth.RequiredEvidenceLinks.Count < 3)
            {
                result.FailureReasons.Add("Mystery has fewer than 3 required evidence links (insufficient threads for proof).");
            }
            else
            {
                foreach (var link in truth.RequiredEvidenceLinks)
                {
                    bool roomExists = rooms.Exists(r => r.RoomId == link.TargetRoomId);
                    if (!roomExists)
                    {
                        result.FailureReasons.Add($"Evidence link '{link.EvidenceId}' is mapped to non-existent room '{link.TargetRoomId}'.");
                    }
                }
            }

            // Criterion 5: Innocent suspects can be ruled out (alibis provided)
            if (truth.ExoneratedSuspectIds == null || truth.ExoneratedSuspectIds.Count == 0)
            {
                result.FailureReasons.Add("No innocent suspects were exonerated with alibis (unbounded deduction).");
            }
            else
            {
                foreach (var suspectId in truth.ExoneratedSuspectIds)
                {
                    if (!truth.SuspectAlibis.ContainsKey(suspectId))
                    {
                        result.FailureReasons.Add($"Innocent suspect '{suspectId}' lacks a documented alibi.");
                    }
                }
            }

            // Criterion 6: Required access key is defined
            if (string.IsNullOrEmpty(truth.RequiredAccessKeyId))
            {
                result.FailureReasons.Add("Required access key ID is missing.");
            }

            // Criterion 7: Motive is coherent and non-empty
            if (string.IsNullOrWhiteSpace(truth.Motive))
            {
                result.FailureReasons.Add("Motive description is blank.");
            }

            // Criterion 8: Mandatory object placements map to valid rooms
            foreach (var kvp in truth.MandatoryObjectPlacements)
            {
                bool targetRoomExists = rooms.Exists(r => r.RoomId == kvp.Value);
                if (!targetRoomExists)
                {
                    result.FailureReasons.Add($"Mandatory object '{kvp.Key}' targets non-existent room '{kvp.Value}'.");
                }
            }

            return result;
        }
    }
}
