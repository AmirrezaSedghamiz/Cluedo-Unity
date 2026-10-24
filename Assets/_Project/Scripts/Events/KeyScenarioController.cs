using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.Investigation;
using TomorrowsCrime.Simulation;
using TomorrowsCrime.World;

namespace TomorrowsCrime.Events
{
    /// <summary>
    /// Coordinates the initial causal scenario:
    /// At a scheduled simulation time (default: 30s / 00:30), the concierge NPC attempts
    /// to take the Room 102 keycard and unlock Door 102.
    ///
    /// - If the key is at its baseline position on the table, the action succeeds.
    /// - If the player moved or picked up the key, the action fails with an explicit reason.
    /// - Failures observed by the player record a persistent discovery that survives loop reset.
    /// </summary>
    public class KeyScenarioController : MonoBehaviour, ILoopResetReceiver
    {
        [Header("Scenario Timing")]
        [Tooltip("Simulation timestamp in seconds when the NPC attempts the action.")]
        [SerializeField] private float scheduledTimeSeconds = 30.0f;

        [Header("Identifiers & References")]
        [SerializeField] private string requiredKeyId = "key_room_102";
        [SerializeField] private string targetDoorId = "door_room_102";
        [SerializeField] private InteractableDoor targetDoor;
        [SerializeField] private Transform npcTransform;

        [Header("Observation Settings")]
        [Tooltip("Max distance from NPC or key within which the player is considered a witness.")]
        [SerializeField] private float witnessDistance = 20.0f;

        [Header("Runtime Status")]
        [SerializeField] private ScenarioExecutionStatus status = ScenarioExecutionStatus.Pending;
        [SerializeField] private string lastResultReason = "Awaiting scheduled execution at 00:30.";

        private bool _hasExecuted;

        public ScenarioExecutionStatus Status => status;
        public string LastResultReason => lastResultReason;
        public float ScheduledTimeSeconds => scheduledTimeSeconds;

        private void Start()
        {
            if (targetDoor == null)
            {
                targetDoor = FindFirstObjectByType<InteractableDoor>();
            }

            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.RegisterResetReceiver(this);
            }
        }

        private void Update()
        {
            if (_hasExecuted) return;

            var sim = SimulationManager.Instance;
            if (sim == null || sim.Clock == null) return;

            if (sim.Clock.ElapsedSeconds >= scheduledTimeSeconds)
            {
                ExecuteScenario(sim.Clock);
            }
        }

        public void ExecuteScenario(SimulationClockModel clock)
        {
            _hasExecuted = true;

            PropLocationState keyState = PropLocationState.AtBaseline;
            if (WorldStateManager.Instance != null && WorldStateManager.Instance.Registry.HasEntry(requiredKeyId))
            {
                keyState = WorldStateManager.Instance.Registry.GetState<PropLocationState>(requiredKeyId);
            }

            status = EvaluatePreconditions(keyState, out lastResultReason);

            if (status == ScenarioExecutionStatus.Succeeded)
            {
                if (targetDoor != null)
                {
                    targetDoor.UnlockAndOpen();
                }
                Debug.Log($"[KeyScenarioController] SUCCESS at {clock.GetFormattedTime()}: {lastResultReason}");
            }
            else
            {
                Debug.LogWarning($"[KeyScenarioController] FAILURE at {clock.GetFormattedTime()}: {lastResultReason}");

                // Check if player witnessed the failure
                if (CheckPlayerWitnessed())
                {
                    bool isNew = InvestigationKnowledgeService.Instance.RecordDiscovery(
                        "key_missing_at_table",
                        "The concierge could not unlock Room 102 at 00:30 because the key was missing from the table.",
                        clock.LoopCount,
                        clock.ElapsedSeconds
                    );

                    if (isNew)
                    {
                        Debug.Log("[KeyScenarioController] Player acquired persistent discovery: 'key_missing_at_table'!");
                    }
                }
            }
        }

        /// <summary>
        /// Pure evaluation function. Decoupled from Unity components to enable fast,
        /// deterministic unit testing in EditMode.
        /// </summary>
        public static ScenarioExecutionStatus EvaluatePreconditions(PropLocationState keyState, out string reportReason)
        {
            if (keyState == PropLocationState.AtBaseline)
            {
                reportReason = "Preconditions met: Keycard was at expected location. Concierge retrieved key and unlocked Door 102.";
                return ScenarioExecutionStatus.Succeeded;
            }

            reportReason = "Event failed: required key was not available at the expected location.";
            return ScenarioExecutionStatus.Failed;
        }

        private bool CheckPlayerWitnessed()
        {
            var player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                var pc = FindFirstObjectByType<TomorrowsCrime.Player.PlayerController>();
                if (pc != null) player = pc.gameObject;
            }

            if (player == null) return true; // Default to true if player reference is absent

            // Player witnesses if carrying the key or within line-of-sight/hearing distance of NPC or table
            if (WorldStateManager.Instance != null && WorldStateManager.Instance.Registry.HasEntry(requiredKeyId))
            {
                var keyState = WorldStateManager.Instance.Registry.GetState<PropLocationState>(requiredKeyId);
                if (keyState == PropLocationState.HeldByPlayer)
                {
                    return true;
                }
            }

            if (npcTransform != null)
            {
                float dist = Vector3.Distance(player.transform.position, npcTransform.position);
                if (dist <= witnessDistance)
                {
                    return true;
                }
            }

            return false;
        }

        public void OnLoopReset()
        {
            _hasExecuted = false;
            status = ScenarioExecutionStatus.Pending;
            lastResultReason = "Awaiting scheduled execution at 00:30.";
            Debug.Log("[KeyScenarioController] Scenario reset to Pending for the new loop.");
        }

        private void OnDestroy()
        {
            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.UnregisterResetReceiver(this);
            }
        }
    }
}
