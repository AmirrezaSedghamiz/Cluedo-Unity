using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.Simulation;

namespace TomorrowsCrime.World
{
    /// <summary>
    /// Scene-level manager for the mutable world state.
    /// Provides access to the central WorldStateRegistry and coordinates with
    /// SimulationManager to ensure all loop-scoped objects reset deterministically.
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public class WorldStateManager : MonoBehaviour, ILoopResetReceiver
    {
        public static WorldStateManager Instance { get; private set; }

        public WorldStateRegistry Registry { get; private set; } = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.RegisterResetReceiver(this);
            }
        }

        public void OnLoopReset()
        {
            Debug.Log("[WorldStateManager] Restoring all registered world states to baseline for loop turnover...");
            Registry.ResetAllToBaseline();
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
