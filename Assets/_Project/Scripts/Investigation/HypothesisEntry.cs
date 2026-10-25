using System;
using System.Collections.Generic;

namespace TomorrowsCrime.Investigation
{
    /// <summary>
    /// Represents a theory or deduction recorded by the player.
    /// A hypothesis is player-authored and is NOT automatically verified as true by the simulation.
    /// Persists across time loops.
    /// </summary>
    [Serializable]
    public class HypothesisEntry
    {
        public string HypothesisId { get; }
        public string Title { get; set; }
        public string Explanation { get; set; }
        public List<string> SupportingEvidenceIds { get; }
        public int CreatedInLoop { get; }
        public float CreatedAtSimTime { get; }

        public HypothesisEntry(
            string id,
            string title,
            string explanation,
            int loopIndex,
            float simTime,
            IEnumerable<string> supportingEvidenceIds = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("HypothesisId cannot be null or empty.", nameof(id));

            HypothesisId = id;
            Title = title ?? "Untitled Hypothesis";
            Explanation = explanation ?? string.Empty;
            CreatedInLoop = loopIndex;
            CreatedAtSimTime = simTime;
            SupportingEvidenceIds = supportingEvidenceIds != null ? new List<string>(supportingEvidenceIds) : new List<string>();
        }

        public void Update(string newTitle, string newExplanation, IEnumerable<string> newSupportingEvidence = null)
        {
            if (!string.IsNullOrWhiteSpace(newTitle)) Title = newTitle;
            if (newExplanation != null) Explanation = newExplanation;

            if (newSupportingEvidence != null)
            {
                SupportingEvidenceIds.Clear();
                SupportingEvidenceIds.AddRange(newSupportingEvidence);
            }
        }
    }
}
