using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.Investigation;
using TomorrowsCrime.Simulation;
using TomorrowsCrime.World;

namespace TomorrowsCrime.Events
{
    /// <summary>
    /// Coordinates the key-based causal scenario using the CausalEventEngine.
    /// Replaces previous hardcoded logic with data-driven event definitions:
    ///
    /// 1. Event 'concierge_retrieves_key' (at 00:30):
    ///    - Precondition: 'key_room_102' is at PropLocationState.AtBaseline.
    ///    - If failed: reports missing key and registers persistent player discovery if witnessed.
    ///
    /// 2. Dependent Event 'concierge_unlocks_room102' (at 00:35):
    ///    - Prerequisite: 'concierge_retrieves_key'.
    ///    - Effect: Unlocks and opens 'door_room_102'.
    ///    - If prerequisite failed: Skipped according to dependency policy.
    /// </summary>
    public class KeyScenarioController : MonoBehaviour, ILoopResetReceiver
    {
        [Header("Timing")]
        [SerializeField] private float retrieveKeyTime = 30.0f;
        [SerializeField] private float unlockDoorTime = 35.0f;

        [Header("Identifiers")]
        [SerializeField] private string keyId = "key_room_102";
        [SerializeField] private string doorId = "door_room_102";

        [Header("References")]
        [SerializeField] private Transform npcTransform;
        [SerializeField] private float witnessDistance = 20.0f;

        public const string EventRetrieveKey = "concierge_retrieves_key";
        public const string EventUnlockDoor = "concierge_unlocks_room102";

        public ScenarioExecutionStatus Status
        {
            get
            {
                var scheduler = CausalEventScheduler.Instance;
                if (scheduler == null || scheduler.Engine == null) return ScenarioExecutionStatus.Pending;

                var ev1 = scheduler.Engine.GetInstance(EventRetrieveKey);
                var ev2 = scheduler.Engine.GetInstance(EventUnlockDoor);

                if (ev2 != null && ev2.Status == EventStatus.Executed)
                {
                    return ScenarioExecutionStatus.Succeeded;
                }

                if (ev1 != null && (ev1.Status == EventStatus.Failed || ev1.Status == EventStatus.Cancelled))
                {
                    return ScenarioExecutionStatus.Failed;
                }

                return ScenarioExecutionStatus.Pending;
            }
        }

        public string LastResultReason
        {
            get
            {
                var scheduler = CausalEventScheduler.Instance;
                if (scheduler == null || scheduler.Engine == null) return "Engine initializing...";

                var ev1 = scheduler.Engine.GetInstance(EventRetrieveKey);
                var ev2 = scheduler.Engine.GetInstance(EventUnlockDoor);

                if (ev2 != null && ev2.Status == EventStatus.Executed)
                {
                    return "Success: Concierge retrieved key and unlocked Room 102.";
                }

                if (ev1 != null && ev1.Status == EventStatus.Failed)
                {
                    return ev1.ResultExplanation;
                }

                if (ev2 != null && ev2.Status == EventStatus.Skipped)
                {
                    return ev2.ResultExplanation;
                }

                return "Awaiting scheduled execution at 00:30.";
            }
        }

        private void Start()
        {
            BuildAndRegisterScenarioEvents();

            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.RegisterResetReceiver(this);
            }
        }

        private void BuildAndRegisterScenarioEvents()
        {
            var scheduler = CausalEventScheduler.Instance;
            if (scheduler == null || scheduler.Engine == null) return;

            // Only register if not already present
            if (scheduler.Engine.GetDefinition(EventRetrieveKey) == null)
            {
                // Event 1: Concierge retrieves key
                var def1 = ScriptableObject.CreateInstance<EventDefinition>();
                def1.Initialize(
                    EventRetrieveKey,
                    "Concierge retrieves key from lobby table",
                    retrieveKeyTime,
                    priority: 10,
                    PrerequisiteFailurePolicy.Skip,
                    "Attendant checks the lobby table for the Room 102 keycard."
                );
                def1.AddPrecondition(new KeyLocationPrecondition(keyId, PropLocationState.AtBaseline));
                scheduler.Engine.RegisterEvent(def1);
            }

            if (scheduler.Engine.GetDefinition(EventUnlockDoor) == null)
            {
                // Event 2: Concierge unlocks door
                var def2 = ScriptableObject.CreateInstance<EventDefinition>();
                def2.Initialize(
                    EventUnlockDoor,
                    "Concierge unlocks Room 102 door",
                    unlockDoorTime,
                    priority: 5,
                    PrerequisiteFailurePolicy.Skip,
                    "Attendant walks to Room 102 and unlocks the door using the acquired key."
                );
                def2.AddPrerequisite(EventRetrieveKey);
                def2.AddEffect(new DoorStateEffect(doorId, DoorLockState.Unlocked, DoorOpenState.Open));
                scheduler.Engine.RegisterEvent(def2);
            }

            scheduler.Engine.OnEventProcessed += HandleEventProcessed;
            scheduler.Engine.ValidateGraph();
        }

        private void HandleEventProcessed(EventExecutionRecord record)
        {
            // If the key retrieval event failed, check if player witnessed the failure
            if (record.EventId == EventRetrieveKey && record.Status == EventStatus.Failed)
            {
                if (CheckPlayerWitnessed())
                {
                    int loop = SimulationManager.Instance != null && SimulationManager.Instance.Clock != null
                        ? SimulationManager.Instance.Clock.LoopCount
                        : 1;

                    InvestigationKnowledgeService.Instance.RecordDiscovery(
                        "key_missing_at_table",
                        "The concierge could not unlock Room 102 because the key was missing from the table.",
                        loop,
                        record.SimTime
                    );

                    InvestigationKnowledgeService.Instance.RecordObservation(
                        $"obs_concierge_key_missing_loop_{loop}",
                        "Observed Concierge Pierre discover that the Room 102 keycard was missing from the lobby table at 00:30.",
                        loop,
                        record.SimTime,
                        relatedEventId: EventRetrieveKey,
                        relatedEvidenceId: "evidence_room102_keycard"
                    );

                    Debug.Log("[KeyScenarioController] Discovery and player observation recorded for key missing event!");
                }
            }
        }

        /// <summary>
        /// Backward-compatible evaluation function.
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
                var pc = FindAnyObjectByType<TomorrowsCrime.Player.PlayerController>();
                if (pc != null) player = pc.gameObject;
            }

            if (player == null) return true;

            if (WorldStateManager.Instance != null && WorldStateManager.Instance.Registry.HasEntry(keyId))
            {
                if (WorldStateManager.Instance.Registry.GetState<PropLocationState>(keyId) == PropLocationState.HeldByPlayer)
                {
                    return true;
                }
            }

            if (npcTransform != null)
            {
                float dist = Vector3.Distance(player.transform.position, npcTransform.position);
                if (dist <= witnessDistance) return true;
            }

            return false;
        }

        public void OnLoopReset()
        {
            // Scenario runtime states are cleanly reset by CausalEventScheduler
            Debug.Log("[KeyScenarioController] Scenario reset for new loop.");
        }

        private void OnDestroy()
        {
            var scheduler = CausalEventScheduler.Instance;
            if (scheduler != null && scheduler.Engine != null)
            {
                scheduler.Engine.OnEventProcessed -= HandleEventProcessed;
            }

            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.UnregisterResetReceiver(this);
            }
        }
    }
}
