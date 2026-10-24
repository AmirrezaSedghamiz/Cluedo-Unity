using System;
using System.Collections.Generic;
using System.Linq;
using TomorrowsCrime.World;

namespace TomorrowsCrime.Events
{
    /// <summary>
    /// Pure, deterministic causal event simulation engine.
    /// Manages event definitions, runtime instances, dependency validation,
    /// precondition evaluations, and effect application across simulation loops.
    /// Completely decoupled from UnityEngine scene lifecycles for 100% testability.
    /// </summary>
    public class CausalEventEngine
    {
        private readonly Dictionary<string, EventDefinition> _definitions = new();
        private readonly Dictionary<string, EventRuntimeInstance> _instances = new();
        private readonly List<EventExecutionRecord> _history = new();

        public int CurrentLoopIndex { get; private set; } = 1;
        public float CurrentSimTime { get; private set; } = 0f;
        public int RegisteredEventCount => _definitions.Count;
        public IReadOnlyList<EventExecutionRecord> History => _history.AsReadOnly();

        public event Action<EventExecutionRecord> OnEventProcessed;

        public CausalEventEngine(int initialLoop = 1)
        {
            CurrentLoopIndex = initialLoop;
        }

        /// <summary>
        /// Registers an event definition with the engine.
        /// Throws if ID is missing or duplicate.
        /// </summary>
        public void RegisterEvent(EventDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (string.IsNullOrWhiteSpace(definition.Id))
            {
                throw new ArgumentException("Event definition ID cannot be null or whitespace.", nameof(definition));
            }

            if (_definitions.ContainsKey(definition.Id))
            {
                throw new InvalidOperationException(
                    $"Duplicate event registration detected for ID '{definition.Id}'. " +
                    "Every event in the causal engine must have a unique, stable identifier.");
            }

            _definitions[definition.Id] = definition;
            _instances[definition.Id] = new EventRuntimeInstance(definition, CurrentLoopIndex);
        }

        public EventRuntimeInstance GetInstance(string eventId)
        {
            _instances.TryGetValue(eventId, out var instance);
            return instance;
        }

        public EventDefinition GetDefinition(string eventId)
        {
            _definitions.TryGetValue(eventId, out var def);
            return def;
        }

        public IReadOnlyList<EventRuntimeInstance> GetAllInstances()
        {
            return _instances.Values.ToList().AsReadOnly();
        }

        /// <summary>
        /// Validates graph consistency:
        /// 1. Ensures every prerequisite ID exists.
        /// 2. Ensures no circular dependency cycles exist.
        /// Throws InvalidOperationException if validation fails.
        /// </summary>
        public void ValidateGraph()
        {
            // 1. Missing prerequisites check
            foreach (var def in _definitions.Values)
            {
                foreach (var prereqId in def.PrerequisiteEventIds)
                {
                    if (!_definitions.ContainsKey(prereqId))
                    {
                        throw new InvalidOperationException(
                            $"Event '{def.Id}' references non-existent prerequisite event '{prereqId}'.");
                    }
                }
            }

            // 2. Cycle detection using DFS
            var visited = new HashSet<string>();
            var inStack = new HashSet<string>();

            foreach (var eventId in _definitions.Keys)
            {
                if (HasCycleDFS(eventId, visited, inStack))
                {
                    throw new InvalidOperationException(
                        $"Dependency cycle detected in causal event graph involving event '{eventId}'.");
                }
            }
        }

        private bool HasCycleDFS(string currentId, HashSet<string> visited, HashSet<string> inStack)
        {
            if (inStack.Contains(currentId)) return true;
            if (visited.Contains(currentId)) return false;

            visited.Add(currentId);
            inStack.Add(currentId);

            if (_definitions.TryGetValue(currentId, out var def))
            {
                foreach (var prereqId in def.PrerequisiteEventIds)
                {
                    if (HasCycleDFS(prereqId, visited, inStack))
                    {
                        return true;
                    }
                }
            }

            inStack.Remove(currentId);
            return false;
        }

        /// <summary>
        /// Ticks the event engine up to current simulation time.
        /// Evaluates all eligible pending events in strict deterministic order.
        /// Sequential evaluation guarantees that events at the same timestamp observe
        /// world state modifications produced by earlier prioritized events.
        /// </summary>
        public void Tick(float simTime, WorldStateRegistry worldState)
        {
            CurrentSimTime = simTime;

            // Sequential deterministic evaluation with multi-pass dependency resolution:
            // If an event completes in this tick, dependent events scheduled at or before simTime
            // can proceed within the same tick.
            bool progressMade;
            do
            {
                progressMade = false;

                var eligible = _instances.Values
                    .Where(inst => inst.Status == EventStatus.Pending && inst.ScheduledTimeSeconds <= simTime)
                    .OrderBy(inst => inst.ScheduledTimeSeconds)
                    .ThenByDescending(inst => inst.Priority)
                    .ThenBy(inst => inst.Id, StringComparer.Ordinal)
                    .ToList();

                foreach (var instance in eligible)
                {
                    if (instance.Status != EventStatus.Pending) continue;

                    if (ProcessEvent(instance, simTime, worldState))
                    {
                        progressMade = true;
                    }
                }
            } while (progressMade);
        }

        private bool ProcessEvent(EventRuntimeInstance instance, float simTime, WorldStateRegistry worldState)
        {
            var def = instance.Definition;

            // 1. Evaluate Prerequisites
            foreach (var prereqId in def.PrerequisiteEventIds)
            {
                if (!_instances.TryGetValue(prereqId, out var prereqInstance))
                {
                    instance.MarkFailed(simTime, $"Missing prerequisite definition for '{prereqId}'.");
                    RecordHistory(instance);
                    return true;
                }

                // If prerequisite is still pending, this event must wait
                if (!prereqInstance.IsTerminal)
                {
                    return false; // Retain Pending status
                }

                // If prerequisite reached an unsuccessful terminal state, apply dependency failure policy
                if (prereqInstance.Status != EventStatus.Executed)
                {
                    string reason = $"Prerequisite '{prereqId}' was not executed successfully (Status: {prereqInstance.Status}).";
                    switch (def.FailurePolicy)
                    {
                        case PrerequisiteFailurePolicy.Fail:
                            instance.MarkFailed(simTime, reason);
                            break;
                        case PrerequisiteFailurePolicy.Cancel:
                            instance.MarkCancelled(simTime, reason);
                            break;
                        case PrerequisiteFailurePolicy.Skip:
                        default:
                            instance.MarkSkipped(simTime, reason);
                            break;
                    }

                    RecordHistory(instance);
                    return true;
                }
            }

            // 2. Evaluate Preconditions
            instance.MarkExecuting();

            foreach (var precondition in def.Preconditions)
            {
                if (!precondition.Evaluate(worldState, this, out string failureReason))
                {
                    instance.MarkFailed(simTime, failureReason ?? "Precondition returned false without explanation.");
                    RecordHistory(instance);
                    return true;
                }
            }

            // 3. Apply Effects (with failure safety)
            foreach (var effect in def.Effects)
            {
                if (!effect.Apply(worldState, out string effectFailure))
                {
                    instance.MarkFailed(simTime, $"Effect failed: {effectFailure}");
                    RecordHistory(instance);
                    return true;
                }
            }

            // 4. Successful Execution
            instance.MarkExecuted(simTime, "Preconditions satisfied and all effects applied successfully.");
            RecordHistory(instance);
            return true;
        }

        private void RecordHistory(EventRuntimeInstance instance)
        {
            var record = new EventExecutionRecord(
                instance.Id,
                instance.EventName,
                instance.ExecutionSimTime ?? CurrentSimTime,
                instance.LoopIndex,
                instance.Status,
                instance.ResultExplanation
            );

            _history.Add(record);
            OnEventProcessed?.Invoke(record);
        }

        /// <summary>
        /// Resets all runtime instances to Pending for the new simulation loop.
        /// Clears current loop history.
        /// </summary>
        public void ResetForNewLoop(int newLoopIndex)
        {
            CurrentLoopIndex = newLoopIndex;
            CurrentSimTime = 0f;

            foreach (var instance in _instances.Values)
            {
                instance.ResetState(newLoopIndex);
            }

            _history.Clear();
        }

        public void Clear()
        {
            _definitions.Clear();
            _instances.Clear();
            _history.Clear();
            CurrentSimTime = 0f;
            CurrentLoopIndex = 1;
        }
    }
}
