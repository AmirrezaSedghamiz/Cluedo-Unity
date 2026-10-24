using System.Collections.Generic;
using UnityEngine;

namespace TomorrowsCrime.Events
{
    /// <summary>
    /// Immutable definition of a causal simulation event.
    /// Authored as a ScriptableObject or instantiated programmatically in tests.
    /// Contains NO mutable runtime state.
    /// </summary>
    [CreateAssetMenu(fileName = "Event_", menuName = "Tomorrow's Crime/Events/Event Definition")]
    public class EventDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string eventName;
        [TextArea(2, 4)]
        [SerializeField] private string description;

        [Header("Scheduling")]
        [Tooltip("Simulation timestamp in seconds when this event becomes eligible to execute.")]
        [SerializeField] private float scheduledTimeSeconds;

        [Tooltip("Priority for events scheduled at the same timestamp (higher priority executes first).")]
        [SerializeField] private int priority;

        [Header("Prerequisites & Dependencies")]
        [Tooltip("IDs of prerequisite events that must successfully execute before this event.")]
        [SerializeField] private List<string> prerequisiteEventIds = new();

        [Tooltip("Behavior if a required prerequisite reaches a terminal unsuccessful state.")]
        [SerializeField] private PrerequisiteFailurePolicy failurePolicy = PrerequisiteFailurePolicy.Skip;

        private readonly List<IEventPrecondition> _preconditions = new();
        private readonly List<IEventEffect> _effects = new();

        public string Id => id;
        public string EventName => eventName;
        public string Description => description;
        public float ScheduledTimeSeconds => scheduledTimeSeconds;
        public int Priority => priority;
        public IReadOnlyList<string> PrerequisiteEventIds => prerequisiteEventIds;
        public PrerequisiteFailurePolicy FailurePolicy => failurePolicy;
        public IReadOnlyList<IEventPrecondition> Preconditions => _preconditions;
        public IReadOnlyList<IEventEffect> Effects => _effects;

        public void Initialize(
            string id,
            string eventName,
            float scheduledTimeSeconds,
            int priority = 0,
            PrerequisiteFailurePolicy failurePolicy = PrerequisiteFailurePolicy.Skip,
            string description = "")
        {
            this.id = id;
            this.eventName = eventName;
            this.scheduledTimeSeconds = scheduledTimeSeconds;
            this.priority = priority;
            this.failurePolicy = failurePolicy;
            this.description = description;
        }

        public void AddPrerequisite(string prerequisiteId)
        {
            if (!string.IsNullOrWhiteSpace(prerequisiteId) && !prerequisiteEventIds.Contains(prerequisiteId))
            {
                prerequisiteEventIds.Add(prerequisiteId);
            }
        }

        public void AddPrecondition(IEventPrecondition precondition)
        {
            if (precondition != null && !_preconditions.Contains(precondition))
            {
                _preconditions.Add(precondition);
            }
        }

        public void AddEffect(IEventEffect effect)
        {
            if (effect != null && !_effects.Contains(effect))
            {
                _effects.Add(effect);
            }
        }
    }
}
