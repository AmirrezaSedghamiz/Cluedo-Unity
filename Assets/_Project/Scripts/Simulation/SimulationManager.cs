using System.Collections.Generic;
using UnityEngine;
using TomorrowsCrime.Core;

namespace TomorrowsCrime.Simulation
{
    /// <summary>
    /// Frame-rate independent simulation controller.
    /// Drives the 5-minute SimulationClockModel and coordinates loop turnover
    /// events with all registered ILoopResetReceiver instances.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class SimulationManager : MonoBehaviour
    {
        public static SimulationManager Instance { get; private set; }

        [Header("Clock Settings")]
        [Tooltip("Duration of a single time loop in seconds. Default is 300 (5 minutes).")]
        [SerializeField] private float loopDurationSeconds = 300f;

        [Header("Runtime State")]
        [SerializeField] private float initialTimeScale = 1.0f;

        public SimulationClockModel Clock { get; private set; }

        private readonly List<ILoopResetReceiver> _resetReceivers = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Clock = new SimulationClockModel(loopDurationSeconds);
            Clock.SetTimeScale(initialTimeScale);
            Clock.OnLoopCompleted += HandleLoopCompleted;
        }

        private void Start()
        {
            // Auto-discover existing reset receivers present in the scene
            var allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude);
            foreach (var mb in allMonoBehaviours)
            {
                if (mb is ILoopResetReceiver receiver && !_resetReceivers.Contains(receiver))
                {
                    _resetReceivers.Add(receiver);
                }
            }
        }

        private void Update()
        {
            Clock?.Tick(Time.deltaTime);

            // Developer speed & debug hotkeys
            if (Input.GetKeyDown(KeyCode.Alpha1)) Clock?.SetTimeScale(1.0f);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Clock?.SetTimeScale(2.0f);
            if (Input.GetKeyDown(KeyCode.Alpha5)) Clock?.SetTimeScale(5.0f);
            if (Input.GetKeyDown(KeyCode.P)) Clock?.TogglePause();
            if (Input.GetKeyDown(KeyCode.R)) ForceResetLoop();
        }

        public void RegisterResetReceiver(ILoopResetReceiver receiver)
        {
            if (receiver != null && !_resetReceivers.Contains(receiver))
            {
                _resetReceivers.Add(receiver);
            }
        }

        public void UnregisterResetReceiver(ILoopResetReceiver receiver)
        {
            _resetReceivers.Remove(receiver);
        }

        public void ForceResetLoop()
        {
            Debug.Log("[SimulationManager] Manual loop reset initiated.");
            Clock?.TriggerLoopTurnover();
        }

        private void HandleLoopCompleted(int completedLoopIndex)
        {
            Debug.Log($"[SimulationManager] Loop {completedLoopIndex} completed. Resetting world state for Loop {Clock.LoopCount}...");

            var snapshot = new List<ILoopResetReceiver>(_resetReceivers);
            foreach (var receiver in snapshot)
            {
                if (receiver != null)
                {
                    try
                    {
                        receiver.OnLoopReset();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[SimulationManager] Exception resetting receiver {receiver}: {ex}");
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
