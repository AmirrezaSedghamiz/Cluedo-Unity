using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.Simulation;

namespace TomorrowsCrime.World
{
    /// <summary>
    /// Test interactive object to demonstrate mutable world state.
    /// Can be altered during a loop (e.g. moved or inspected), but implements
    /// ILoopResetReceiver to restore its exact initial state on loop reset.
    /// </summary>
    public class TestInteractableProp : MonoBehaviour, ILoopResetReceiver
    {
        [SerializeField] private string propName = "Hotel Room 102 Keycard";
        [SerializeField] private Vector3 interactionOffset = new Vector3(0f, 0.4f, 0f);

        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private bool _isManipulated;

        public string PropName => propName;
        public bool IsManipulated => _isManipulated;

        private void Awake()
        {
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
        }

        private void Start()
        {
            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.RegisterResetReceiver(this);
            }
        }

        /// <summary>
        /// Simulates player manipulation (e.g. picking up, inspecting, moving).
        /// </summary>
        public void Manipulate()
        {
            if (_isManipulated) return;

            _isManipulated = true;
            transform.position += interactionOffset;
            Debug.Log($"[TestInteractableProp] {propName} was manipulated. State is now altered for this loop.");
        }

        public void OnLoopReset()
        {
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;
            _isManipulated = false;
            Debug.Log($"[TestInteractableProp] {propName} restored to initial conditions for the new loop.");
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
