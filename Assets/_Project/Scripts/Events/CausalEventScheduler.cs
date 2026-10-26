using System.Collections.Generic;
using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.Simulation;
using TomorrowsCrime.World;

namespace TomorrowsCrime.Events
{
    /// <summary>
    /// Scene-level scheduler coordinating the CausalEventEngine with
    /// the SimulationManager clock and WorldStateManager.
    /// Resets all event runtime states on loop turnover.
    /// </summary>
    [DefaultExecutionOrder(-70)]
    public class CausalEventScheduler : MonoBehaviour, ILoopResetReceiver
    {
        public static CausalEventScheduler Instance { get; private set; }

        [Header("Event Definitions")]
        [Tooltip("Serialized event definitions to register automatically at startup.")]
        [SerializeField] private List<EventDefinition> serializedDefinitions = new();

        public CausalEventEngine Engine { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Engine = new CausalEventEngine();

            foreach (var def in serializedDefinitions)
            {
                if (def != null && !string.IsNullOrWhiteSpace(def.Id))
                {
                    Engine.RegisterEvent(def);
                }
            }

            try
            {
                Engine.ValidateGraph();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CausalEventScheduler] Event graph validation failed: {ex.Message}");
            }
        }

        private void Start()
        {
            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.RegisterResetReceiver(this);
            }
        }

        private void Update()
        {
            var sim = SimulationManager.Instance;
            var world = WorldStateManager.Instance;

            if (sim == null || sim.Clock == null || world == null) return;

            Engine.Tick(sim.Clock.ElapsedSeconds, world.Registry);
        }

        public void ClearScheduledEvents()
        {
            Engine = new CausalEventEngine();
        }

        public void ScheduleEvent(EventDefinition def)
        {
            RegisterDefinition(def);
        }

        public void RegisterDefinition(EventDefinition def)
        {
            if (def != null && Engine != null)
            {
                Engine.RegisterEvent(def);
                Engine.ValidateGraph();
            }
        }

        public void OnLoopReset()
        {
            int loopIndex = SimulationManager.Instance != null && SimulationManager.Instance.Clock != null
                ? SimulationManager.Instance.Clock.LoopCount
                : 1;

            Debug.Log($"[CausalEventScheduler] Resetting event engine runtime state for Loop #{loopIndex}...");
            Engine.ResetForNewLoop(loopIndex);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                if (SimulationManager.Instance != null)
                {
                    SimulationManager.Instance.UnregisterResetReceiver(this);
                }
                Instance = null;
            }
        }
    }
}
