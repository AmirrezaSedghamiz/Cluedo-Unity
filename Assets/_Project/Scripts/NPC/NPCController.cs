using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TomorrowsCrime.Core;
using TomorrowsCrime.Events;
using TomorrowsCrime.Simulation;
using TomorrowsCrime.World;

namespace TomorrowsCrime.NPC
{
    /// <summary>
    /// Behavior controller coordinating NPC schedule evaluation, NavMesh navigation,
    /// world-state queries, causal event engine integration, and loop resets.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class NPCController : MonoBehaviour, IInteractable, ILoopResetReceiver
    {
        [Header("Configuration")]
        [SerializeField] private NPCDefinition definition;
        [SerializeField] private TomorrowsCrime.Procedural.NPCs.NPCProfile profile;
        [SerializeField] private LayerMask obstructionMask;

        [Header("Waypoints")]
        [SerializeField] private Transform waypointLobbyDesk;
        [SerializeField] private Transform waypointLobbyTable;
        [SerializeField] private Transform waypointRoom102Door;

        [Header("Perception")]
        [SerializeField] private Transform eyeTransform;

        private NavMeshAgent _agent;
        private NPCScheduleEvaluator _scheduleEvaluator;
        private NPCRuntimeState _runtimeState;
        private readonly Dictionary<string, Transform> _waypoints = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);

        private Vector3 _baselinePosition;
        private Quaternion _baselineRotation;
        private ScheduleActivity _lastEvaluatedActivity;
        private float _lastPathAttemptTime = -10f;
        private bool _isActionHandledForCurrentActivity;
        private bool _isInConversation = false;

        public NPCDefinition Definition => definition;
        public TomorrowsCrime.Procedural.NPCs.NPCProfile Profile
        {
            get => profile;
            set => profile = value;
        }
        public NPCRuntimeState RuntimeState => _runtimeState;
        public NavMeshAgent Agent => _agent;

        public string CharacterDisplayName => profile != null ? profile.DisplayName : (definition != null ? definition.DisplayName : "Concierge Pierre");
        public string CharacterRole => profile != null ? profile.Role.ToString() : "Concierge";
        public string CharacterId => profile != null ? profile.NpcId : (definition != null ? definition.CharacterId : "npc_concierge");
        public TomorrowsCrime.Procedural.NPCs.NPCRole? Role => profile != null ? profile.Role : (TomorrowsCrime.Procedural.NPCs.NPCRole?)null;

        // IInteractable Implementation
        public string InteractionPrompt => $"Question {CharacterDisplayName} [E]";
        public bool CanInteract => true;

        public void Interact(GameObject interactor)
        {
            if (interactor != null)
            {
                Vector3 lookDir = interactor.transform.position - transform.position;
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(lookDir);
                }
            }

            _isInConversation = true;
            if (_agent != null && _agent.isOnNavMesh && !_agent.isStopped)
            {
                _agent.isStopped = true;
            }

            if (TomorrowsCrime.UI.Core.UIManager.Instance != null)
            {
                TomorrowsCrime.UI.Core.UIManager.Instance.ShowDialogueModal(this);
            }
        }

        public void EndConversation()
        {
            _isInConversation = false;
            if (_agent != null && _agent.isOnNavMesh && SimulationManager.Instance != null && !SimulationManager.Instance.IsPaused)
            {
                _agent.isStopped = false;
            }
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (eyeTransform == null) eyeTransform = transform;

            _baselinePosition = transform.position;
            _baselineRotation = transform.rotation;

            InitializeFromDefinition();
        }

        private void Start()
        {
            RegisterWaypoints();

            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.RegisterResetReceiver(this);
            }
        }

        private void Update()
        {
            if (SimulationManager.Instance == null || SimulationManager.Instance.Clock == null) return;

            var clock = SimulationManager.Instance.Clock;

            // Pause locomotion when in conversation or simulation is paused
            if (_isInConversation || clock.IsPaused)
            {
                if (_agent != null && _agent.isOnNavMesh && !_agent.isStopped)
                {
                    _agent.isStopped = true;
                }
                return;
            }

            if (_agent != null && _agent.isOnNavMesh && _agent.isStopped)
            {
                _agent.isStopped = false;
            }

            Tick(clock.ElapsedSeconds);
        }

        public void InitializeFromProfile(TomorrowsCrime.Procedural.NPCs.NPCProfile p, TomorrowsCrime.Procedural.NPCs.NPCRoutinePlan plan = null)
        {
            profile = p;
            _baselinePosition = transform.position;
            _baselineRotation = transform.rotation;

            definition = ScriptableObject.CreateInstance<NPCDefinition>();
            definition.Initialize(p.NpcId, p.DisplayName, _baselinePosition, _baselineRotation);

            // Populate procedural activities from 5-minute routine plan
            if (plan != null && plan.Activities != null && plan.Activities.Count > 0)
            {
                for (int i = 0; i < plan.Activities.Count; i++)
                {
                    var act = plan.Activities[i];
                    string wpKey = $"{act.ActivityName}_{i}";
                    ActivityType type = ActivityType.Patrol;
                    if (act.ActivityName.Contains("RetrieveKey")) type = ActivityType.RetrieveKey;
                    else if (act.ActivityName.Contains("Unlock")) type = ActivityType.UnlockDoor;
                    else if (act.ActivityName.Contains("Wait") || act.ActivityName.Contains("Duty") || act.ActivityName.Contains("Station")) type = ActivityType.WaitAtPost;
                    else if (act.ActivityName.Contains("Inspect") || act.ActivityName.Contains("Check")) type = ActivityType.Investigate;

                    definition.AddActivity(new ScheduleActivity(type, act.StartSimTime, act.EndSimTime, wpKey, null, act.Description));
                }
            }

            _scheduleEvaluator = definition.CreateEvaluator();
            _runtimeState = new NPCRuntimeState(p.NpcId);

            if (_agent != null)
            {
                _agent.speed = definition.MovementSpeed;
                _agent.stoppingDistance = definition.StoppingDistance;
            }
        }

        public void InitializeFromDefinition()
        {
            if (definition == null)
            {
                // Fallback default definition
                definition = ScriptableObject.CreateInstance<NPCDefinition>();
                definition.Initialize(
                    "npc_concierge",
                    "Concierge Pierre",
                    _baselinePosition,
                    _baselineRotation
                );

                // Add default 5-minute schedule
                definition.AddActivity(new ScheduleActivity(ActivityType.WaitAtPost, 0f, 15f, "LobbyDesk", null, "Waiting at reception desk"));
                definition.AddActivity(new ScheduleActivity(ActivityType.RetrieveKey, 15f, 30f, "LobbyTable", "concierge_retrieves_key", "Walking to lobby table to retrieve key"));
                definition.AddActivity(new ScheduleActivity(ActivityType.UnlockDoor, 30f, 45f, "Room102Door", "concierge_unlocks_room102", "Walking to Room 102 to unlock door"));
                definition.AddActivity(new ScheduleActivity(ActivityType.WaitAtPost, 45f, 300f, "Room102Door", null, "Stationed at Room 102"));
            }

            _scheduleEvaluator = definition.CreateEvaluator();
            _runtimeState = new NPCRuntimeState(definition.CharacterId);

            if (_agent != null)
            {
                _agent.speed = definition.MovementSpeed;
                _agent.stoppingDistance = definition.StoppingDistance;
            }
        }

        public void RegisterWaypoints()
        {
            // Preserve procedural waypoints; only assign serialized inspectables if unset
            if (waypointLobbyDesk != null && !_waypoints.ContainsKey("LobbyDesk")) _waypoints["LobbyDesk"] = waypointLobbyDesk;
            if (waypointLobbyTable != null && !_waypoints.ContainsKey("LobbyTable")) _waypoints["LobbyTable"] = waypointLobbyTable;
            if (waypointRoom102Door != null && !_waypoints.ContainsKey("Room102Door")) _waypoints["Room102Door"] = waypointRoom102Door;
        }

        public void SetWaypoint(string key, Transform target)
        {
            if (!string.IsNullOrEmpty(key) && target != null)
            {
                _waypoints[key] = target;
            }
        }

        /// <summary>
        /// Frame-independent tick driven by simulation clock time.
        /// </summary>
        public void Tick(float simTime)
        {
            if (_scheduleEvaluator == null || _runtimeState == null) return;

            // 1. Evaluate schedule activity
            var activity = _scheduleEvaluator.GetActivityForTime(simTime);

            // Handle transition to new activity block
            if (activity != _lastEvaluatedActivity)
            {
                _lastEvaluatedActivity = activity;
                _isActionHandledForCurrentActivity = false;
                OnActivityStarted(activity, simTime);
            }

            // 2. Process active behavior state
            ProcessCurrentState(activity, simTime);
        }

        private void OnActivityStarted(ScheduleActivity activity, float simTime)
        {
            // If already noticed missing key and activity requires it, handle reaction
            if (_runtimeState.HasNoticedMissingKey && activity.ActivityType == ActivityType.UnlockDoor)
            {
                _runtimeState.TransitionBehavior(
                    NPCBehaviorState.Waiting,
                    "Waiting: cannot unlock Room 102 without keycard."
                );
                StopAgent();
                return;
            }

            Transform destinationTransform = null;
            if (!string.IsNullOrEmpty(activity.DestinationKey))
            {
                _waypoints.TryGetValue(activity.DestinationKey, out destinationTransform);
            }

            Vector3 destPos = destinationTransform != null ? destinationTransform.position : transform.position;

            switch (activity.ActivityType)
            {
                case ActivityType.WaitAtPost:
                    if (destinationTransform != null && Vector3.Distance(transform.position, destinationTransform.position) > 0.8f)
                    {
                        RequestNavigation(destinationTransform.position, activity.DestinationKey, simTime);
                        _runtimeState.SetCurrentActivity(activity, activity.DestinationKey, destPos, $"Navigating to post at {activity.DestinationKey}.");
                    }
                    else
                    {
                        StopAgent();
                        _runtimeState.SetCurrentActivity(activity, activity.DestinationKey, destPos, "Waiting at station.");
                        _runtimeState.TransitionBehavior(NPCBehaviorState.Idle, "Idle at post.");
                    }
                    break;

                case ActivityType.RetrieveKey:
                case ActivityType.UnlockDoor:
                case ActivityType.Patrol:
                case ActivityType.Investigate:
                    if (destinationTransform != null)
                    {
                        RequestNavigation(destinationTransform.position, activity.DestinationKey, simTime);
                        _runtimeState.SetCurrentActivity(activity, activity.DestinationKey, destPos, $"Navigating to {activity.DestinationKey}.");
                    }
                    else
                    {
                        _runtimeState.TransitionBehavior(NPCBehaviorState.Idle, $"No waypoint registered for '{activity.DestinationKey}'.");
                    }
                    break;

                case ActivityType.Idle:
                default:
                    StopAgent();
                    _runtimeState.TransitionBehavior(NPCBehaviorState.Idle, "Idle.");
                    break;
            }
        }

        private void ProcessCurrentState(ScheduleActivity activity, float simTime)
        {
            if (_runtimeState.BehaviorState == NPCBehaviorState.Moving)
            {
                // 1. NavMesh locomotion if agent is active on a NavMesh
                if (_agent != null && _agent.isOnNavMesh)
                {
                    // Check for blocked/unreachable path
                    if (_agent.pathStatus == NavMeshPathStatus.PathInvalid ||
                        (_agent.pathStatus == NavMeshPathStatus.PathPartial && !_agent.pathPending))
                    {
                        // Reaction B: Blocked route
                        HandleBlockedRoute(activity.DestinationKey);
                        return;
                    }

                    // Check if arrived at destination
                    if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
                    {
                        _runtimeState.SetNavigationStatus(NPCNavigationStatus.Arrived, $"Arrived at {activity.DestinationKey}.");
                        _runtimeState.TransitionBehavior(NPCBehaviorState.PerformingAction, $"Executing action at {activity.DestinationKey}.");
                        PerformScheduledDestinationAction(activity, simTime);
                    }
                    else
                    {
                        _runtimeState.SetNavigationStatus(NPCNavigationStatus.Traversing);
                    }
                }
                else
                {
                    // 2. Smooth direct locomotion fallback when off-mesh
                    Transform destTrans = null;
                    if (!string.IsNullOrEmpty(activity.DestinationKey))
                    {
                        _waypoints.TryGetValue(activity.DestinationKey, out destTrans);
                    }

                    if (destTrans != null)
                    {
                        Vector3 destPos = destTrans.position;
                        Vector3 toDest = destPos - transform.position;
                        float dist = toDest.magnitude;

                        if (dist <= 0.8f)
                        {
                            _runtimeState.SetNavigationStatus(NPCNavigationStatus.Arrived, $"Arrived at {activity.DestinationKey}.");
                            _runtimeState.TransitionBehavior(NPCBehaviorState.PerformingAction, $"Executing action at {activity.DestinationKey}.");
                            PerformScheduledDestinationAction(activity, simTime);
                        }
                        else
                        {
                            float speed = definition != null ? definition.MovementSpeed : 3.0f;
                            transform.position = Vector3.MoveTowards(transform.position, destPos, speed * Time.deltaTime);

                            Vector3 flatDir = toDest;
                            flatDir.y = 0f;
                            if (flatDir.sqrMagnitude > 0.001f)
                            {
                                Quaternion targetRot = Quaternion.LookRotation(flatDir);
                                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 360f * Time.deltaTime);
                            }
                            _runtimeState.SetNavigationStatus(NPCNavigationStatus.Traversing);
                        }
                    }
                    else
                    {
                        _runtimeState.TransitionBehavior(NPCBehaviorState.Idle, "No target waypoint.");
                    }
                }
            }
        }

        private void PerformScheduledDestinationAction(ScheduleActivity activity, float simTime)
        {
            if (_isActionHandledForCurrentActivity) return;
            _isActionHandledForCurrentActivity = true;

            if (activity.ActivityType == ActivityType.WaitAtPost ||
                activity.ActivityType == ActivityType.Patrol ||
                activity.ActivityType == ActivityType.Investigate)
            {
                StopAgent();
                _runtimeState.TransitionBehavior(NPCBehaviorState.Idle, $"Stationed at {activity.DestinationKey}.");
                return;
            }

            var worldState = WorldStateManager.Instance != null ? WorldStateManager.Instance.Registry : null;

            if (activity.ActivityType == ActivityType.RetrieveKey)
            {
                // Check if key is available at table
                bool isKeyAtTable = worldState != null &&
                                   worldState.TryGetState<PropLocationState>("key_room_102", out var keyState) &&
                                   keyState == PropLocationState.AtBaseline;

                if (isKeyAtTable)
                {
                    // Success: concierge acquires key
                    _runtimeState.TransitionBehavior(
                        NPCBehaviorState.PerformingAction,
                        "Concierge retrieved Room 102 keycard from table."
                    );
                }
                else
                {
                    // Reaction A: Missing Key
                    _runtimeState.MarkMissingKeyNoticed(
                        "Investigating: expected keycard is missing from lobby table!"
                    );
                    Debug.LogWarning($"[NPCController] REACTION A: Concierge noticed key is missing at {simTime:F1}s.");
                }
            }
            else if (activity.ActivityType == ActivityType.UnlockDoor)
            {
                if (!_runtimeState.HasNoticedMissingKey)
                {
                    _runtimeState.TransitionBehavior(
                        NPCBehaviorState.PerformingAction,
                        "Concierge unlocking Room 102 door."
                    );
                }
                else
                {
                    _runtimeState.TransitionBehavior(
                        NPCBehaviorState.Waiting,
                        "Waiting: door cannot be unlocked without keycard."
                    );
                }
            }
        }

        private void HandleBlockedRoute(string destinationKey)
        {
            StopAgent();
            _runtimeState.MarkRouteBlocked($"Blocked: destination '{destinationKey}' is unreachable or obstructed.");
            Debug.LogWarning($"[NPCController] REACTION B: Route to '{destinationKey}' is blocked.");
        }

        private void RequestNavigation(Vector3 targetPosition, string destinationKey, float simTime)
        {
            if (_agent != null && _agent.isOnNavMesh)
            {
                // Prevent spamming path requests every frame
                if (simTime - _lastPathAttemptTime < 0.5f) return;
                _lastPathAttemptTime = simTime;

                _agent.isStopped = false;
                bool pathSet = _agent.SetDestination(targetPosition);

                if (!pathSet)
                {
                    HandleBlockedRoute(destinationKey);
                    return;
                }
            }

            _runtimeState.SetNavigationStatus(NPCNavigationStatus.Traversing);
            _runtimeState.TransitionBehavior(NPCBehaviorState.Moving, $"Moving: -> {destinationKey}");
        }

        private void StopAgent()
        {
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.ResetPath();
                _agent.velocity = Vector3.zero;
                _agent.isStopped = true;
            }
            _runtimeState?.SetNavigationStatus(NPCNavigationStatus.Idle);
        }

        /// <summary>
        /// Pure decision evaluator helper for testability without NavMesh/Unity scenes.
        /// </summary>
        public static NPCBehaviorState EvaluateHighLevelDecision(
            ScheduleActivity activity,
            PropLocationState keyState,
            bool isPathReachable,
            bool previouslyNoticedMissingKey,
            out string decisionReason)
        {
            if (!isPathReachable)
            {
                decisionReason = $"Blocked: route to '{activity.DestinationKey}' is unreachable.";
                return NPCBehaviorState.Blocked;
            }

            if (activity.ActivityType == ActivityType.WaitAtPost)
            {
                decisionReason = "Idle at designated station.";
                return NPCBehaviorState.Idle;
            }

            if (activity.ActivityType == ActivityType.RetrieveKey)
            {
                if (keyState != PropLocationState.AtBaseline)
                {
                    decisionReason = "Investigating: expected key missing from lobby table.";
                    return NPCBehaviorState.Investigating;
                }

                decisionReason = "Concierge retrieved keycard successfully.";
                return NPCBehaviorState.PerformingAction;
            }

            if (activity.ActivityType == ActivityType.UnlockDoor)
            {
                if (previouslyNoticedMissingKey || keyState != PropLocationState.AtBaseline)
                {
                    decisionReason = "Waiting: cannot unlock door without Room 102 keycard.";
                    return NPCBehaviorState.Waiting;
                }

                decisionReason = "Concierge unlocking Room 102 door.";
                return NPCBehaviorState.PerformingAction;
            }

            decisionReason = "Moving to destination.";
            return NPCBehaviorState.Moving;
        }

        public void OnLoopReset()
        {
            _isInConversation = false;
            StopAgent();

            // Restore baseline transform
            Vector3 targetPos = definition != null ? definition.DefaultPosition : _baselinePosition;
            Quaternion targetRot = definition != null ? definition.DefaultRotation : _baselineRotation;

            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.Warp(targetPos);
            }
            else
            {
                transform.position = targetPos;
            }
            transform.rotation = targetRot;

            int newLoopIndex = SimulationManager.Instance != null && SimulationManager.Instance.Clock != null
                ? SimulationManager.Instance.Clock.LoopCount
                : 1;

            _runtimeState.ResetForNewLoop(newLoopIndex);
            _lastEvaluatedActivity = null;
            _isActionHandledForCurrentActivity = false;
            _lastPathAttemptTime = -10f;

            Debug.Log($"[NPCController] Reset to baseline for Loop {newLoopIndex}.");
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
