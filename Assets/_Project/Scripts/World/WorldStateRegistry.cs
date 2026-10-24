using System;
using System.Collections.Generic;

namespace TomorrowsCrime.World
{
    /// <summary>
    /// Pure, deterministic registry managing mutable world state entries.
    /// Fully decoupled from UnityEngine to guarantee fast, reliable unit testing.
    /// Captures baseline states on registration and restores them on loop turnover.
    /// </summary>
    public class WorldStateRegistry
    {
        private readonly Dictionary<string, object> _currentState = new();
        private readonly Dictionary<string, object> _baselineState = new();
        private readonly Dictionary<string, Type> _registeredTypes = new();

        public event Action<string, object> OnStateChanged;
        public event Action OnResetCompleted;

        public int EntryCount => _registeredTypes.Count;

        /// <summary>
        /// Registers a world-state entry with a unique identifier and its initial baseline state.
        /// Throws an exception if the ID is already registered to prevent silent overwrites.
        /// </summary>
        public void RegisterEntry<T>(string id, T baseline)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("World state ID cannot be null or whitespace.", nameof(id));
            }

            if (_registeredTypes.ContainsKey(id))
            {
                throw new InvalidOperationException(
                    $"Duplicate world state registration detected for ID '{id}'. " +
                    "Every registered object in the simulation must have a unique, stable identifier.");
            }

            _registeredTypes[id] = typeof(T);
            _baselineState[id] = baseline;
            _currentState[id] = baseline;
        }

        /// <summary>
        /// Updates the runtime state of a registered object.
        /// Throws KeyNotFoundException if the ID has not been registered.
        /// </summary>
        public void SetState<T>(string id, T newState)
        {
            if (!_registeredTypes.TryGetValue(id, out var expectedType))
            {
                throw new KeyNotFoundException($"Cannot set state: No world state entry registered with ID '{id}'.");
            }

            if (typeof(T) != expectedType)
            {
                throw new ArgumentException(
                    $"Type mismatch for entry '{id}'. Expected type {expectedType.Name}, but received {typeof(T).Name}.");
            }

            _currentState[id] = newState;
            OnStateChanged?.Invoke(id, newState);
        }

        /// <summary>
        /// Retrieves the current runtime state of a registered object.
        /// </summary>
        public T GetState<T>(string id)
        {
            if (!_registeredTypes.TryGetValue(id, out var expectedType))
            {
                throw new KeyNotFoundException($"Cannot get state: No world state entry registered with ID '{id}'.");
            }

            if (typeof(T) != expectedType)
            {
                throw new ArgumentException(
                    $"Type mismatch for entry '{id}'. Expected type {expectedType.Name}, but requested {typeof(T).Name}.");
            }

            return (T)_currentState[id];
        }

        /// <summary>
        /// Safely attempts to retrieve the current runtime state without throwing.
        /// </summary>
        public bool TryGetState<T>(string id, out T state)
        {
            if (_registeredTypes.TryGetValue(id, out var expectedType) && typeof(T) == expectedType)
            {
                state = (T)_currentState[id];
                return true;
            }

            state = default;
            return false;
        }

        /// <summary>
        /// Retrieves the original baseline state captured when the object was registered.
        /// </summary>
        public T GetBaseline<T>(string id)
        {
            if (!_registeredTypes.TryGetValue(id, out var expectedType))
            {
                throw new KeyNotFoundException($"Cannot get baseline: No world state entry registered with ID '{id}'.");
            }

            if (typeof(T) != expectedType)
            {
                throw new ArgumentException(
                    $"Type mismatch for entry '{id}'. Expected type {expectedType.Name}, but requested {typeof(T).Name}.");
            }

            return (T)_baselineState[id];
        }

        public bool HasEntry(string id) => _registeredTypes.ContainsKey(id);

        /// <summary>
        /// Restores every registered loop-scoped object back to its captured baseline state.
        /// </summary>
        public void ResetAllToBaseline()
        {
            foreach (var kvp in _baselineState)
            {
                _currentState[kvp.Key] = kvp.Value;
                OnStateChanged?.Invoke(kvp.Key, kvp.Value);
            }

            OnResetCompleted?.Invoke();
        }

        /// <summary>
        /// Clears all entries. Primarily used to reset state between independent automated tests.
        /// </summary>
        public void Clear()
        {
            _currentState.Clear();
            _baselineState.Clear();
            _registeredTypes.Clear();
        }
    }
}
