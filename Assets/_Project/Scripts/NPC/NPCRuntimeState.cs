using System;
using UnityEngine;

namespace TomorrowsCrime.NPC
{
    /// <summary>
    /// Tracks mutable, loop-scoped runtime state for an NPC.
    /// Resets cleanly to baseline upon loop reset.
    /// </summary>
    [Serializable]
    public class NPCRuntimeState
    {
        public string CharacterId { get; private set; }
        public NPCBehaviorState BehaviorState { get; private set; }
        public NPCNavigationStatus NavigationStatus { get; private set; }
        public ScheduleActivity CurrentActivity { get; private set; }
        public string CurrentDestinationKey { get; private set; }
        public Vector3 CurrentDestinationPosition { get; private set; }
        public bool HasNoticedMissingKey { get; private set; }
        public string LastDecisionReason { get; private set; }
        public int LoopIndex { get; private set; }

        public event Action<NPCRuntimeState> OnStateChanged;

        public NPCRuntimeState(string characterId, int loopIndex = 1)
        {
            CharacterId = characterId ?? throw new ArgumentNullException(nameof(characterId));
            LoopIndex = loopIndex;
            ResetForNewLoop(loopIndex);
        }

        public void TransitionBehavior(NPCBehaviorState newState, string reason)
        {
            BehaviorState = newState;
            LastDecisionReason = reason ?? string.Empty;
            OnStateChanged?.Invoke(this);
        }

        public void SetNavigationStatus(NPCNavigationStatus newNavStatus, string reason = null)
        {
            NavigationStatus = newNavStatus;
            if (!string.IsNullOrEmpty(reason))
            {
                LastDecisionReason = reason;
            }
            OnStateChanged?.Invoke(this);
        }

        public void SetCurrentActivity(ScheduleActivity activity, string destinationKey, Vector3 destinationPosition, string reason)
        {
            CurrentActivity = activity;
            CurrentDestinationKey = destinationKey ?? string.Empty;
            CurrentDestinationPosition = destinationPosition;
            LastDecisionReason = reason ?? string.Empty;
            OnStateChanged?.Invoke(this);
        }

        /// <summary>
        /// Reaction A: Triggered when NPC reaches/observes the table and finds the expected keycard missing.
        /// </summary>
        public void MarkMissingKeyNoticed(string reason)
        {
            HasNoticedMissingKey = true;
            BehaviorState = NPCBehaviorState.Investigating;
            LastDecisionReason = reason ?? "Investigating: expected key missing from lobby table.";
            OnStateChanged?.Invoke(this);
        }

        /// <summary>
        /// Reaction B: Triggered when the navigation route is blocked or destination cannot be reached.
        /// </summary>
        public void MarkRouteBlocked(string reason)
        {
            BehaviorState = NPCBehaviorState.Blocked;
            NavigationStatus = NPCNavigationStatus.Blocked;
            LastDecisionReason = reason ?? "Blocked: route to destination is obstructed or unreachable.";
            OnStateChanged?.Invoke(this);
        }

        /// <summary>
        /// Resets all mutable runtime state to baseline for the new simulation loop.
        /// </summary>
        public void ResetForNewLoop(int newLoopIndex)
        {
            LoopIndex = newLoopIndex;
            BehaviorState = NPCBehaviorState.Idle;
            NavigationStatus = NPCNavigationStatus.Idle;
            CurrentActivity = null;
            CurrentDestinationKey = string.Empty;
            CurrentDestinationPosition = Vector3.zero;
            HasNoticedMissingKey = false;
            LastDecisionReason = $"Reset to baseline for Loop {newLoopIndex}.";
            OnStateChanged?.Invoke(this);
        }
    }
}
