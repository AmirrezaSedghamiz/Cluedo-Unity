using System;

namespace TomorrowsCrime.Investigation
{
    /// <summary>
    /// Runtime record of an evidence item discovered by the player.
    /// Persists across time loops.
    /// </summary>
    [Serializable]
    public class DiscoveredEvidenceRecord
    {
        public EvidenceDefinition Definition { get; }
        public string EvidenceId => Definition.EvidenceId;
        public string DisplayName => Definition.DisplayName;
        public string Category => Definition.Category;
        public string ShortDescription => Definition.ShortDescription;
        public string DetailedInspectionText => Definition.DetailedInspectionText;
        public int DiscoveredInLoop { get; }
        public float DiscoveredAtSimTime { get; }

        public DiscoveredEvidenceRecord(EvidenceDefinition definition, int loopIndex, float simTime)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            DiscoveredInLoop = loopIndex;
            DiscoveredAtSimTime = simTime;
        }

        public string GetFormattedTimestamp()
        {
            int minutes = (int)(DiscoveredAtSimTime / 60f);
            int seconds = (int)(DiscoveredAtSimTime % 60f);
            return $"{minutes:00}:{seconds:00}";
        }
    }
}
