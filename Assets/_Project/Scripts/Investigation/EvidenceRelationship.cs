using System;

namespace TomorrowsCrime.Investigation
{
    public enum EvidenceRelationshipType
    {
        Supports,
        Contradicts,
        RelatedTo,
        OccurredBefore,
        OccurredAfter
    }

    /// <summary>
    /// Represents a directed semantic relationship between two discovered evidence items.
    /// Distinguishes between player-authored hypotheses/connections and immutable authored relationships.
    /// Persists across loop resets on the deduction board.
    /// </summary>
    [Serializable]
    public class EvidenceRelationship
    {
        public string RelationshipId { get; }
        public string SourceEvidenceId { get; }
        public string TargetEvidenceId { get; }
        public EvidenceRelationshipType RelationshipType { get; set; }
        public string Explanation { get; set; }
        public bool IsPlayerCreated { get; }
        public int CreatedInLoop { get; }
        public float CreatedAtSimTime { get; }

        public EvidenceRelationship(
            string relationshipId,
            string sourceEvidenceId,
            string targetEvidenceId,
            EvidenceRelationshipType relationshipType,
            string explanation = "",
            bool isPlayerCreated = true,
            int createdInLoop = 1,
            float createdAtSimTime = 0f)
        {
            if (string.IsNullOrWhiteSpace(relationshipId)) throw new ArgumentException("RelationshipId cannot be null or empty.", nameof(relationshipId));
            if (string.IsNullOrWhiteSpace(sourceEvidenceId)) throw new ArgumentException("SourceEvidenceId cannot be null or empty.", nameof(sourceEvidenceId));
            if (string.IsNullOrWhiteSpace(targetEvidenceId)) throw new ArgumentException("TargetEvidenceId cannot be null or empty.", nameof(targetEvidenceId));

            RelationshipId = relationshipId;
            SourceEvidenceId = sourceEvidenceId;
            TargetEvidenceId = targetEvidenceId;
            RelationshipType = relationshipType;
            Explanation = explanation ?? string.Empty;
            IsPlayerCreated = isPlayerCreated;
            CreatedInLoop = createdInLoop;
            CreatedAtSimTime = createdAtSimTime;
        }

        public void Update(EvidenceRelationshipType newType, string newExplanation)
        {
            RelationshipType = newType;
            Explanation = newExplanation ?? string.Empty;
        }

        public override string ToString()
        {
            return $"[{RelationshipType}] {SourceEvidenceId} -> {TargetEvidenceId}: {Explanation}";
        }
    }
}
