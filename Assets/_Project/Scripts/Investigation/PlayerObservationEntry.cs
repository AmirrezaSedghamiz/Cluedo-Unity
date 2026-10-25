using System;

namespace TomorrowsCrime.Investigation
{
    /// <summary>
    /// Represents a factual observation that the player personally witnessed or experienced.
    /// Distinct from the internal causal simulation history.
    /// Persists across loop resets, enabling loop-over-loop comparison.
    /// </summary>
    [Serializable]
    public class PlayerObservationEntry
    {
        public string ObservationId { get; }
        public int LoopIndex { get; }
        public float SimTime { get; }
        public string Description { get; }
        public string RelatedEventId { get; }
        public string RelatedEvidenceId { get; }

        public PlayerObservationEntry(
            string observationId,
            string description,
            int loopIndex,
            float simTime,
            string relatedEventId = null,
            string relatedEvidenceId = null)
        {
            if (string.IsNullOrWhiteSpace(observationId)) throw new ArgumentException("ObservationId cannot be null or empty.", nameof(observationId));
            if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description cannot be null or empty.", nameof(description));

            ObservationId = observationId;
            Description = description;
            LoopIndex = loopIndex;
            SimTime = simTime;
            RelatedEventId = relatedEventId ?? string.Empty;
            RelatedEvidenceId = relatedEvidenceId ?? string.Empty;
        }

        public string GetFormattedTimestamp()
        {
            int minutes = (int)(SimTime / 60f);
            int seconds = (int)(SimTime % 60f);
            return $"{minutes:00}:{seconds:00}";
        }

        public override string ToString()
        {
            return $"[Loop {LoopIndex} @ {GetFormattedTimestamp()}] {Description}";
        }
    }
}
