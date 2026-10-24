using TomorrowsCrime.Core;
using TomorrowsCrime.Investigation;
using TomorrowsCrime.World;

namespace TomorrowsCrime.Events
{
    // ==================== PRECONDITIONS ====================

    /// <summary>
    /// Precondition that evaluates whether a portable item/key is in an expected location state.
    /// </summary>
    public class KeyLocationPrecondition : IEventPrecondition
    {
        private readonly string _keyId;
        private readonly PropLocationState _expectedState;

        public string Description => $"'{_keyId}' must be {_expectedState}";

        public KeyLocationPrecondition(string keyId, PropLocationState expectedState)
        {
            _keyId = keyId;
            _expectedState = expectedState;
        }

        public bool Evaluate(WorldStateRegistry worldState, CausalEventEngine engine, out string failureReason)
        {
            if (worldState == null || !worldState.HasEntry(_keyId))
            {
                failureReason = $"World state does not contain entry for key '{_keyId}'.";
                return false;
            }

            var actual = worldState.GetState<PropLocationState>(_keyId);
            if (actual == _expectedState)
            {
                failureReason = null;
                return true;
            }

            failureReason = $"Expected key '{_keyId}' at {_expectedState}, but actual location is {actual}.";
            return false;
        }
    }

    /// <summary>
    /// Precondition that evaluates the locked/unlocked state of a door.
    /// </summary>
    public class DoorLockPrecondition : IEventPrecondition
    {
        private readonly string _doorId;
        private readonly DoorLockState _expectedLockState;

        public string Description => $"'{_doorId}' must be {_expectedLockState}";

        public DoorLockPrecondition(string doorId, DoorLockState expectedLockState)
        {
            _doorId = doorId;
            _expectedLockState = expectedLockState;
        }

        public bool Evaluate(WorldStateRegistry worldState, CausalEventEngine engine, out string failureReason)
        {
            if (worldState == null || !worldState.HasEntry(_doorId))
            {
                failureReason = $"World state does not contain entry for door '{_doorId}'.";
                return false;
            }

            var actual = worldState.GetState<DoorLockState>(_doorId);
            if (actual == _expectedLockState)
            {
                failureReason = null;
                return true;
            }

            failureReason = $"Expected door '{_doorId}' to be {_expectedLockState}, but actual state is {actual}.";
            return false;
        }
    }

    /// <summary>
    /// Precondition evaluating whether another event has achieved a specific status.
    /// </summary>
    public class EventStatusPrecondition : IEventPrecondition
    {
        private readonly string _prerequisiteEventId;
        private readonly EventStatus _expectedStatus;

        public string Description => $"Event '{_prerequisiteEventId}' must be {_expectedStatus}";

        public EventStatusPrecondition(string prerequisiteEventId, EventStatus expectedStatus = EventStatus.Executed)
        {
            _prerequisiteEventId = prerequisiteEventId;
            _expectedStatus = expectedStatus;
        }

        public bool Evaluate(WorldStateRegistry worldState, CausalEventEngine engine, out string failureReason)
        {
            if (engine == null)
            {
                failureReason = "Event engine reference is missing.";
                return false;
            }

            var instance = engine.GetInstance(_prerequisiteEventId);
            if (instance == null)
            {
                failureReason = $"Prerequisite event '{_prerequisiteEventId}' is not registered in the engine.";
                return false;
            }

            if (instance.Status == _expectedStatus)
            {
                failureReason = null;
                return true;
            }

            failureReason = $"Expected prerequisite '{_prerequisiteEventId}' to be {_expectedStatus}, but actual status is {instance.Status}.";
            return false;
        }
    }

    // ==================== EFFECTS ====================

    /// <summary>
    /// Effect modifying the lock and/or open state of a door.
    /// </summary>
    public class DoorStateEffect : IEventEffect
    {
        private readonly string _doorId;
        private readonly DoorLockState _newLockState;
        private readonly DoorOpenState _newOpenState;

        public string Description => $"Set '{_doorId}' to {_newLockState} and {_newOpenState}";

        public DoorStateEffect(string doorId, DoorLockState newLockState, DoorOpenState newOpenState)
        {
            _doorId = doorId;
            _newLockState = newLockState;
            _newOpenState = newOpenState;
        }

        public bool Apply(WorldStateRegistry worldState, out string failureReason)
        {
            if (worldState == null || !worldState.HasEntry(_doorId))
            {
                failureReason = $"Cannot apply DoorStateEffect: '{_doorId}' not registered in world state.";
                return false;
            }

            worldState.SetState(_doorId, _newLockState);

            // If an actual scene InteractableDoor exists, update its visual hinge
            var sceneDoor = UnityEngine.Object.FindAnyObjectByType<InteractableDoor>();
            if (sceneDoor != null && sceneDoor.DoorId == _doorId)
            {
                if (_newLockState == DoorLockState.Unlocked && _newOpenState == DoorOpenState.Open)
                {
                    sceneDoor.UnlockAndOpen();
                }
            }

            failureReason = null;
            return true;
        }
    }

    /// <summary>
    /// Effect modifying the location of an item/prop in the world state.
    /// </summary>
    public class PropLocationEffect : IEventEffect
    {
        private readonly string _propId;
        private readonly PropLocationState _newLocation;

        public string Description => $"Move '{_propId}' to {_newLocation}";

        public PropLocationEffect(string propId, PropLocationState newLocation)
        {
            _propId = propId;
            _newLocation = newLocation;
        }

        public bool Apply(WorldStateRegistry worldState, out string failureReason)
        {
            if (worldState == null || !worldState.HasEntry(_propId))
            {
                failureReason = $"Cannot apply PropLocationEffect: '{_propId}' not registered in world state.";
                return false;
            }

            worldState.SetState(_propId, _newLocation);
            failureReason = null;
            return true;
        }
    }

    /// <summary>
    /// Effect that records a persistent investigation discovery.
    /// </summary>
    public class InvestigationDiscoveryEffect : IEventEffect
    {
        private readonly string _discoveryId;
        private readonly string _description;
        private readonly int _loopIndex;
        private readonly float _simTime;

        public string Description => $"Record discovery '{_discoveryId}'";

        public InvestigationDiscoveryEffect(string discoveryId, string description, int loopIndex, float simTime)
        {
            _discoveryId = discoveryId;
            _description = description;
            _loopIndex = loopIndex;
            _simTime = simTime;
        }

        public bool Apply(WorldStateRegistry worldState, out string failureReason)
        {
            InvestigationKnowledgeService.Instance.RecordDiscovery(_discoveryId, _description, _loopIndex, _simTime);
            failureReason = null;
            return true;
        }
    }
}
