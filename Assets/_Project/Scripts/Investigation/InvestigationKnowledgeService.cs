using System;
using System.Collections.Generic;
using System.Linq;

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

    [Serializable]
    public class LoopComparisonEntry
    {
        public string ObservationId { get; set; }
        public PlayerObservationEntry RecordA { get; set; }
        public PlayerObservationEntry RecordB { get; set; }
        public bool HasDivergence => RecordA != null && RecordB != null && RecordA.Description != RecordB.Description;
        public string ComparisonSummary { get; set; }
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
    /// - Evidence Relationships (deductive links between clues)
    /// - Deduction Board layout nodes
    /// - Player Notes (freeform investigation scratchpad)
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
        public event Action<DiscoveredEvidenceRecord> OnNewEvidenceDiscoveredNotification;
        public int EvidenceCount => _evidenceRecords.Count;

        // 3. Player Observation Timeline
        private readonly Dictionary<string, PlayerObservationEntry> _observations = new();
        private readonly List<PlayerObservationEntry> _orderedObservations = new();
        public event Action<PlayerObservationEntry> OnObservationRecorded;
        public event Action<string> OnObservationNotification;
        public int ObservationCount => _observations.Count;

        // 4. Hypotheses
        private readonly Dictionary<string, HypothesisEntry> _hypotheses = new();
        private readonly List<HypothesisEntry> _orderedHypotheses = new();
        public event Action OnHypothesesChanged;
        public int HypothesisCount => _hypotheses.Count;

        // 5. Evidence Relationships (Deduction Board connections)
        private readonly Dictionary<string, EvidenceRelationship> _relationships = new();
        private readonly List<EvidenceRelationship> _orderedRelationships = new();
        public event Action OnRelationshipsChanged;
        public int RelationshipCount => _relationships.Count;

        // 6. Deduction Board Layout Nodes
        private readonly Dictionary<string, DeductionBoardNode> _boardNodes = new();
        private readonly List<DeductionBoardNode> _orderedBoardNodes = new();
        public event Action OnBoardChanged;
        public int BoardNodeCount => _boardNodes.Count;

        // 7. General Notes
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
            OnNewEvidenceDiscoveredNotification?.Invoke(record);

            // Automatically place on deduction board layout
            if (!_boardNodes.ContainsKey(definition.EvidenceId))
            {
                float defaultX = 60f + (_orderedEvidence.Count - 1) * 220f;
                float defaultY = 80f + ((_orderedEvidence.Count - 1) % 2) * 140f;
                AddOrUpdateBoardNode(definition.EvidenceId, defaultX, defaultY);
            }

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
            string relatedEvidenceId = null,
            ObservationMethod method = ObservationMethod.WitnessedEvent,
            string sourceOrSubject = null,
            string contextSummary = null)
        {
            if (string.IsNullOrWhiteSpace(observationId)) throw new ArgumentException("ObservationId cannot be null or empty.", nameof(observationId));
            if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description cannot be null or empty.", nameof(description));

            if (_observations.ContainsKey(observationId))
            {
                return false; // Observation already recorded
            }

            var obs = new PlayerObservationEntry(
                observationId,
                description,
                loopIndex,
                simTime,
                relatedEventId,
                relatedEvidenceId,
                method,
                sourceOrSubject,
                contextSummary
            );

            _observations[observationId] = obs;
            _orderedObservations.Add(obs);
            OnObservationRecorded?.Invoke(obs);
            OnObservationNotification?.Invoke(description);
            return true;
        }

        public bool HasObservation(string observationId)
        {
            return !string.IsNullOrEmpty(observationId) && _observations.ContainsKey(observationId);
        }

        public IReadOnlyList<PlayerObservationEntry> GetAllObservations() => _orderedObservations.AsReadOnly();

        public IReadOnlyList<PlayerObservationEntry> GetObservationsForLoop(int loopIndex)
        {
            return _orderedObservations.Where(o => o.LoopIndex == loopIndex).ToList().AsReadOnly();
        }

        /// <summary>
        /// Compares observations between two loops without mutating any original observation records.
        /// Identifies differences in witnessed event outcomes.
        /// </summary>
        public IReadOnlyList<LoopComparisonEntry> CompareLoops(int loopA, int loopB)
        {
            var obsA = _orderedObservations.Where(o => o.LoopIndex == loopA).ToDictionary(o => o.ObservationId);
            var obsB = _orderedObservations.Where(o => o.LoopIndex == loopB).ToDictionary(o => o.ObservationId);

            var allKeys = new HashSet<string>(obsA.Keys.Concat(obsB.Keys));
            var results = new List<LoopComparisonEntry>();

            foreach (var key in allKeys)
            {
                obsA.TryGetValue(key, out var recA);
                obsB.TryGetValue(key, out var recB);

                string summary;
                if (recA != null && recB != null)
                {
                    summary = recA.Description == recB.Description
                        ? "Identical outcome in both loops."
                        : $"Divergence observed: Loop {loopA} ('{recA.Description}') vs Loop {loopB} ('{recB.Description}').";
                }
                else if (recA != null)
                {
                    summary = $"Observed only in Loop {loopA}.";
                }
                else
                {
                    summary = $"Observed only in Loop {loopB}.";
                }

                results.Add(new LoopComparisonEntry
                {
                    ObservationId = key,
                    RecordA = recA,
                    RecordB = recB,
                    ComparisonSummary = summary
                });
            }

            return results.AsReadOnly();
        }

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

        // --- Evidence Relationships (Deduction Board Links) API ---
        public EvidenceRelationship AddRelationship(
            string sourceEvidenceId,
            string targetEvidenceId,
            EvidenceRelationshipType type,
            string explanation = "",
            bool isPlayerCreated = true,
            int createdInLoop = 1,
            float createdAtSimTime = 0f)
        {
            if (string.IsNullOrWhiteSpace(sourceEvidenceId)) throw new ArgumentException("SourceEvidenceId cannot be null or empty.", nameof(sourceEvidenceId));
            if (string.IsNullOrWhiteSpace(targetEvidenceId)) throw new ArgumentException("TargetEvidenceId cannot be null or empty.", nameof(targetEvidenceId));

            if (!HasDiscoveredEvidence(sourceEvidenceId))
            {
                throw new InvalidOperationException($"Cannot create relationship: source evidence '{sourceEvidenceId}' has not been discovered.");
            }
            if (!HasDiscoveredEvidence(targetEvidenceId))
            {
                throw new InvalidOperationException($"Cannot create relationship: target evidence '{targetEvidenceId}' has not been discovered.");
            }

            string relId = $"rel_{sourceEvidenceId}_{targetEvidenceId}_{type}";
            if (_relationships.TryGetValue(relId, out var existing))
            {
                existing.Update(type, explanation);
                OnRelationshipsChanged?.Invoke();
                return existing;
            }

            var rel = new EvidenceRelationship(
                relId,
                sourceEvidenceId,
                targetEvidenceId,
                type,
                explanation,
                isPlayerCreated,
                createdInLoop,
                createdAtSimTime
            );

            _relationships[relId] = rel;
            _orderedRelationships.Add(rel);
            OnRelationshipsChanged?.Invoke();
            return rel;
        }

        public bool RemoveRelationship(string relationshipId)
        {
            if (string.IsNullOrEmpty(relationshipId) || !_relationships.TryGetValue(relationshipId, out var rel))
            {
                return false;
            }

            _relationships.Remove(relationshipId);
            _orderedRelationships.Remove(rel);
            OnRelationshipsChanged?.Invoke();
            return true;
        }

        public EvidenceRelationship GetRelationship(string relationshipId)
        {
            if (string.IsNullOrEmpty(relationshipId)) return null;
            _relationships.TryGetValue(relationshipId, out var rel);
            return rel;
        }

        public IReadOnlyList<EvidenceRelationship> GetAllRelationships() => _orderedRelationships.AsReadOnly();

        public IReadOnlyList<EvidenceRelationship> GetRelationshipsForEvidence(string evidenceId)
        {
            if (string.IsNullOrEmpty(evidenceId)) return Array.Empty<EvidenceRelationship>();
            return _orderedRelationships
                .Where(r => r.SourceEvidenceId == evidenceId || r.TargetEvidenceId == evidenceId)
                .ToList()
                .AsReadOnly();
        }

        // --- Deduction Board Nodes API ---
        public DeductionBoardNode AddOrUpdateBoardNode(string evidenceId, float positionX, float positionY, string customNote = "")
        {
            if (!HasDiscoveredEvidence(evidenceId))
            {
                throw new InvalidOperationException($"Cannot place node on deduction board: evidence '{evidenceId}' is not yet discovered.");
            }

            if (_boardNodes.TryGetValue(evidenceId, out var existing))
            {
                existing.PositionX = positionX;
                existing.PositionY = positionY;
                if (!string.IsNullOrEmpty(customNote)) existing.CustomNote = customNote;
                OnBoardChanged?.Invoke();
                return existing;
            }

            var node = new DeductionBoardNode(evidenceId, positionX, positionY, customNote);
            _boardNodes[evidenceId] = node;
            _orderedBoardNodes.Add(node);
            OnBoardChanged?.Invoke();
            return node;
        }

        public bool RemoveBoardNode(string evidenceId)
        {
            if (string.IsNullOrEmpty(evidenceId) || !_boardNodes.TryGetValue(evidenceId, out var node))
            {
                return false;
            }

            _boardNodes.Remove(evidenceId);
            _orderedBoardNodes.Remove(node);
            OnBoardChanged?.Invoke();
            return true;
        }

        public DeductionBoardNode GetBoardNode(string evidenceId)
        {
            if (string.IsNullOrEmpty(evidenceId)) return null;
            _boardNodes.TryGetValue(evidenceId, out var node);
            return node;
        }

        public IReadOnlyList<DeductionBoardNode> GetAllBoardNodes() => _orderedBoardNodes.AsReadOnly();

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
            _relationships.Clear();
            _orderedRelationships.Clear();
            _boardNodes.Clear();
            _orderedBoardNodes.Clear();
            GeneralNotes = string.Empty;
        }
    }
}
