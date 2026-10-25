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
    /// Unified persistent investigation knowledge store.
    /// Crucially, this service SURVIVES loop turnovers, modeling the detective's
    /// accumulated memory across time loops.
    /// 
    /// Manages:
    /// - Discovered Evidence items (physical and documented clues)
    /// - Player Observation Timeline (events personally witnessed by the player)
    /// - Player Hypotheses (theories formulated by the player)
    /// - Player Notes (freeform investigation notes)
    /// </summary>
    public class InvestigationKnowledgeService
    {
        private static InvestigationKnowledgeService _instance;
        public static InvestigationKnowledgeService Instance => _instance ??= new InvestigationKnowledgeService();

        // 1. Generic Discoveries (Backward compatible with Step 2 & 3)
        private readonly Dictionary<string, DiscoveryEntry> _discoveries = new();
        private readonly List<DiscoveryEntry> _orderedDiscoveries = new();
        public event Action<DiscoveryEntry> OnDiscoveryRecorded;
        public int DiscoveryCount => _discoveries.Count;

        // 2. Discovered Evidence Items
        private readonly Dictionary<string, DiscoveredEvidenceRecord> _evidenceRecords = new();
        private readonly List<DiscoveredEvidenceRecord> _orderedEvidence = new();
        public event Action<DiscoveredEvidenceRecord> OnEvidenceDiscovered;
        public int EvidenceCount => _evidenceRecords.Count;

        // 3. Player Observation Timeline
        private readonly Dictionary<string, PlayerObservationEntry> _observations = new();
        private readonly List<PlayerObservationEntry> _orderedObservations = new();
        public event Action<PlayerObservationEntry> OnObservationRecorded;
        public int ObservationCount => _observations.Count;

        // 4. Hypotheses & General Notes
        private readonly Dictionary<string, HypothesisEntry> _hypotheses = new();
        private readonly List<HypothesisEntry> _orderedHypotheses = new();
        public event Action OnHypothesesChanged;
        public int HypothesisCount => _hypotheses.Count;

        public string GeneralNotes { get; set; } = string.Empty;

        // --- Generic Discoveries API ---
        public bool RecordDiscovery(string id, string description, int loopIndex, float simTime)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Discovery ID cannot be null or whitespace.", nameof(id));
            if (_discoveries.ContainsKey(id)) return false;

            var entry = new DiscoveryEntry(id, description, loopIndex, simTime);
            _discoveries[id] = entry;
            _orderedDiscoveries.Add(entry);
            OnDiscoveryRecorded?.Invoke(entry);
            return true;
        }

        public bool HasDiscovery(string id) => _discoveries.ContainsKey(id);
        public IReadOnlyList<DiscoveryEntry> GetAllDiscoveries() => _orderedDiscoveries.AsReadOnly();

        // --- Evidence API ---
        public bool DiscoverEvidence(EvidenceDefinition definition, int loopIndex, float simTime)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (string.IsNullOrWhiteSpace(definition.EvidenceId)) throw new ArgumentException("EvidenceId cannot be null or empty.", nameof(definition));

            if (_evidenceRecords.ContainsKey(definition.EvidenceId))
            {
                return false; // Already discovered
            }

            var record = new DiscoveredEvidenceRecord(definition, loopIndex, simTime);
            _evidenceRecords[definition.EvidenceId] = record;
            _orderedEvidence.Add(record);
            OnEvidenceDiscovered?.Invoke(record);

            // Also mirror as a generic discovery for backward compatibility
            RecordDiscovery(definition.EvidenceId, definition.ShortDescription, loopIndex, simTime);

            return true;
        }

        public bool HasDiscoveredEvidence(string evidenceId)
        {
            return !string.IsNullOrEmpty(evidenceId) && _evidenceRecords.ContainsKey(evidenceId);
        }

        public DiscoveredEvidenceRecord GetEvidenceRecord(string evidenceId)
        {
            if (string.IsNullOrEmpty(evidenceId)) return null;
            _evidenceRecords.TryGetValue(evidenceId, out var record);
            return record;
        }

        public IReadOnlyList<DiscoveredEvidenceRecord> GetAllDiscoveredEvidence() => _orderedEvidence.AsReadOnly();

        // --- Player Observation Timeline API ---
        public bool RecordObservation(
            string observationId,
            string description,
            int loopIndex,
            float simTime,
            string relatedEventId = null,
            string relatedEvidenceId = null)
        {
            if (string.IsNullOrWhiteSpace(observationId)) throw new ArgumentException("ObservationId cannot be null or empty.", nameof(observationId));
            if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description cannot be null or empty.", nameof(description));

            if (_observations.ContainsKey(observationId))
            {
                return false; // Observation already recorded
            }

            var obs = new PlayerObservationEntry(observationId, description, loopIndex, simTime, relatedEventId, relatedEvidenceId);
            _observations[observationId] = obs;
            _orderedObservations.Add(obs);
            OnObservationRecorded?.Invoke(obs);
            return true;
        }

        public bool HasObservation(string observationId)
        {
            return !string.IsNullOrEmpty(observationId) && _observations.ContainsKey(observationId);
        }

        public IReadOnlyList<PlayerObservationEntry> GetAllObservations() => _orderedObservations.AsReadOnly();

        // --- Hypotheses API ---
        public HypothesisEntry CreateHypothesis(
            string title,
            string explanation,
            int loopIndex,
            float simTime,
            IEnumerable<string> supportingEvidenceIds = null)
        {
            string id = "hyp_" + Guid.NewGuid().ToString("N")[..8];
            var hypothesis = new HypothesisEntry(id, title, explanation, loopIndex, simTime, supportingEvidenceIds);
            _hypotheses[id] = hypothesis;
            _orderedHypotheses.Add(hypothesis);
            OnHypothesesChanged?.Invoke();
            return hypothesis;
        }

        public bool UpdateHypothesis(
            string hypothesisId,
            string newTitle,
            string newExplanation,
            IEnumerable<string> supportingEvidenceIds = null)
        {
            if (string.IsNullOrEmpty(hypothesisId) || !_hypotheses.TryGetValue(hypothesisId, out var hyp))
            {
                return false;
            }

            hyp.Update(newTitle, newExplanation, supportingEvidenceIds);
            OnHypothesesChanged?.Invoke();
            return true;
        }

        public bool DeleteHypothesis(string hypothesisId)
        {
            if (string.IsNullOrEmpty(hypothesisId) || !_hypotheses.TryGetValue(hypothesisId, out var hyp))
            {
                return false;
            }

            _hypotheses.Remove(hypothesisId);
            _orderedHypotheses.Remove(hyp);
            OnHypothesesChanged?.Invoke();
            return true;
        }

        public HypothesisEntry GetHypothesis(string hypothesisId)
        {
            if (string.IsNullOrEmpty(hypothesisId)) return null;
            _hypotheses.TryGetValue(hypothesisId, out var hyp);
            return hyp;
        }

        public IReadOnlyList<HypothesisEntry> GetAllHypotheses() => _orderedHypotheses.AsReadOnly();

        /// <summary>
        /// Clears all stored investigation data.
        /// Primarily used by automated unit tests to guarantee clean isolated fixtures.
        /// </summary>
        public void Clear()
        {
            _discoveries.Clear();
            _orderedDiscoveries.Clear();
            _evidenceRecords.Clear();
            _orderedEvidence.Clear();
            _observations.Clear();
            _orderedObservations.Clear();
            _hypotheses.Clear();
            _orderedHypotheses.Clear();
            GeneralNotes = string.Empty;
        }
    }
}
