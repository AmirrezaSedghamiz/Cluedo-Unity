using System;

namespace TomorrowsCrime.Investigation
{
    /// <summary>
    /// Represents the placement and annotation of an evidence card on the detective deduction board.
    /// Preserved across time-loop resets.
    /// </summary>
    [Serializable]
    public class DeductionBoardNode
    {
        public string EvidenceId { get; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public string CustomNote { get; set; }

        public DeductionBoardNode(string evidenceId, float positionX, float positionY, string customNote = "")
        {
            if (string.IsNullOrWhiteSpace(evidenceId)) throw new ArgumentException("EvidenceId cannot be null or empty.", nameof(evidenceId));

            EvidenceId = evidenceId;
            PositionX = positionX;
            PositionY = positionY;
            CustomNote = customNote ?? string.Empty;
        }
    }
}
