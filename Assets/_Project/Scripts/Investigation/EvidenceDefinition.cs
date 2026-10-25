using System;
using System.Collections.Generic;
using UnityEngine;

namespace TomorrowsCrime.Investigation
{
    /// <summary>
    /// Immutable definition of an investigative evidence item / clue.
    /// Authored as a ScriptableObject.
    /// Does NOT store mutable runtime state.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEvidenceDefinition", menuName = "Tomorrow's Crime/Evidence Definition")]
    public class EvidenceDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string evidenceId;
        [SerializeField] private string displayName;
        [SerializeField] private string category = "Physical Clue";

        [Header("Descriptions")]
        [TextArea(2, 4)]
        [SerializeField] private string shortDescription;

        [TextArea(4, 8)]
        [SerializeField] private string detailedInspectionText;

        [Header("Relationships")]
        [SerializeField] private string relatedEventId;
        [SerializeField] private List<string> relatedEvidenceIds = new List<string>();

        public string EvidenceId => evidenceId;
        public string DisplayName => displayName;
        public string Category => category;
        public string ShortDescription => shortDescription;
        public string DetailedInspectionText => detailedInspectionText;
        public string RelatedEventId => relatedEventId;
        public IReadOnlyList<string> RelatedEvidenceIds => relatedEvidenceIds;

        public void Initialize(
            string id,
            string name,
            string shortDesc,
            string detailedText,
            string cat = "Physical Clue",
            string eventId = null,
            IEnumerable<string> relatedIds = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("EvidenceId cannot be null or empty.", nameof(id));

            evidenceId = id;
            displayName = name ?? id;
            shortDescription = shortDesc ?? string.Empty;
            detailedInspectionText = detailedText ?? string.Empty;
            category = cat ?? "Physical Clue";
            relatedEventId = eventId ?? string.Empty;

            relatedEvidenceIds = relatedIds != null ? new List<string>(relatedIds) : new List<string>();
        }
    }
}
