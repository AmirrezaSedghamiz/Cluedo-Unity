using System;
using System.Collections.Generic;

namespace TomorrowsCrime.Investigation
{
    [Serializable]
    public class DiscoveryEntry
    {
        public string Id { get; }
        public string Description { get; }
        public int DiscoveredInLoop { get; }
        public float DiscoveredAtSimTime { get; }

        public DiscoveryEntry(string id, string description, int loopIndex, float simTime)
        {
            Id = id;
            Description = description;
            DiscoveredInLoop = loopIndex;
            DiscoveredAtSimTime = simTime;
        }
    }

    /// <summary>
    /// Persistent investigation knowledge store.
    /// Crucially, this service SURVIVES loop turnovers, modeling the detective's
    /// accumulated memory across time loops.
    /// </summary>
    public class InvestigationKnowledgeService
    {
        private static InvestigationKnowledgeService _instance;
        public static InvestigationKnowledgeService Instance => _instance ??= new InvestigationKnowledgeService();

        private readonly Dictionary<string, DiscoveryEntry> _discoveries = new();
        private readonly List<DiscoveryEntry> _orderedDiscoveries = new();

        public event Action<DiscoveryEntry> OnDiscoveryRecorded;

        public int DiscoveryCount => _discoveries.Count;

        /// <summary>
        /// Records a persistent investigative discovery.
        /// Returns true if newly added, or false if already known (preventing duplicates).
        /// </summary>
        public bool RecordDiscovery(string id, string description, int loopIndex, float simTime)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Discovery ID cannot be null or whitespace.", nameof(id));
            }

            if (_discoveries.ContainsKey(id))
            {
                return false; // Already discovered, ignore duplicate
            }

            var entry = new DiscoveryEntry(id, description, loopIndex, simTime);
            _discoveries[id] = entry;
            _orderedDiscoveries.Add(entry);
            OnDiscoveryRecorded?.Invoke(entry);
            return true;
        }

        public bool HasDiscovery(string id) => _discoveries.ContainsKey(id);

        public IReadOnlyList<DiscoveryEntry> GetAllDiscoveries() => _orderedDiscoveries.AsReadOnly();

        /// <summary>
        /// Resets the persistent memory. Used primarily between automated unit tests.
        /// </summary>
        public void Clear()
        {
            _discoveries.Clear();
            _orderedDiscoveries.Clear();
        }
    }
}
