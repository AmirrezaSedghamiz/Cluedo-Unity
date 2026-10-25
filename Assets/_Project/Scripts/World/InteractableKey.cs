using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.Investigation;
using TomorrowsCrime.Simulation;

namespace TomorrowsCrime.World
{
    /// <summary>
    /// Portable keycard prop that the player can pick up and relocate.
    /// Updates the WorldStateManager with its current location status and restores
    /// its exact baseline position and transform when the loop resets.
    /// </summary>
    public class InteractableKey : MonoBehaviour, IInteractable, ILoopResetReceiver
    {
        [Header("Configuration")]
        [SerializeField] private string keyId = "key_room_102";
        [SerializeField] private string keyDisplayName = "Hotel Room 102 Keycard";
        [SerializeField] private EvidenceDefinition keyEvidence;

        [Header("State")]
        [SerializeField] private PropLocationState locationState = PropLocationState.AtBaseline;

        private Vector3 _baselinePosition;
        private Quaternion _baselineRotation;
        private Transform _baselineParent;
        private Collider _collider;
        private Renderer _renderer;
        private bool _isHeld;

        public string KeyId => keyId;
        public PropLocationState LocationState => locationState;
        public EvidenceDefinition KeyEvidence
        {
            get => keyEvidence;
            set => keyEvidence = value;
        }

        public string InteractionPrompt => _isHeld
            ? $"Press E to place {keyDisplayName}"
            : $"Press E to pick up {keyDisplayName}";

        public bool CanInteract => true;

        private void Awake()
        {
            _baselinePosition = transform.position;
            _baselineRotation = transform.rotation;
            _baselineParent = transform.parent;
            _collider = GetComponent<Collider>();
            _renderer = GetComponent<Renderer>();
        }

        private void Start()
        {
            RegisterWithManagers();
        }

        private void RegisterWithManagers()
        {
            if (WorldStateManager.Instance != null && !WorldStateManager.Instance.Registry.HasEntry(keyId))
            {
                WorldStateManager.Instance.Registry.RegisterEntry(keyId, PropLocationState.AtBaseline);
            }

            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.RegisterResetReceiver(this);
            }
        }

        public void Interact(GameObject interactor)
        {
            if (!_isHeld)
            {
                PickUp(interactor);
            }
            else
            {
                Drop(interactor);
            }
        }

        public void PickUp(GameObject interactor)
        {
            _isHeld = true;
            locationState = PropLocationState.HeldByPlayer;

            // Parent to interactor camera or transform
            var cam = interactor.GetComponentInChildren<Camera>();
            Transform holdTarget = cam != null ? cam.transform : interactor.transform;

            transform.SetParent(holdTarget);
            transform.localPosition = new Vector3(0.35f, -0.25f, 0.6f);
            transform.localRotation = Quaternion.Euler(15f, -30f, 10f);

            if (_collider != null) _collider.enabled = false;

            if (WorldStateManager.Instance != null)
            {
                WorldStateManager.Instance.Registry.SetState(keyId, PropLocationState.HeldByPlayer);
            }

            if (keyEvidence != null && InvestigationKnowledgeService.Instance != null)
            {
                int loop = SimulationManager.Instance != null && SimulationManager.Instance.Clock != null
                    ? SimulationManager.Instance.Clock.LoopCount
                    : 1;
                float simTime = SimulationManager.Instance != null && SimulationManager.Instance.Clock != null
                    ? SimulationManager.Instance.Clock.ElapsedSeconds
                    : 0f;
                InvestigationKnowledgeService.Instance.DiscoverEvidence(keyEvidence, loop, simTime);
            }

            Debug.Log($"[InteractableKey] Player picked up {keyDisplayName}. World state '{keyId}' set to HeldByPlayer.");
        }

        public void Drop(GameObject interactor)
        {
            _isHeld = false;
            locationState = PropLocationState.DroppedElsewhere;

            transform.SetParent(null);
            transform.position = interactor.transform.position + interactor.transform.forward * 1.0f + Vector3.up * 0.1f;
            transform.rotation = Quaternion.identity;

            if (_collider != null) _collider.enabled = true;

            if (WorldStateManager.Instance != null)
            {
                WorldStateManager.Instance.Registry.SetState(keyId, PropLocationState.DroppedElsewhere);
            }

            Debug.Log($"[InteractableKey] Player dropped {keyDisplayName}. World state '{keyId}' set to DroppedElsewhere.");
        }

        public void OnLoopReset()
        {
            _isHeld = false;
            locationState = PropLocationState.AtBaseline;

            transform.SetParent(_baselineParent);
            transform.position = _baselinePosition;
            transform.rotation = _baselineRotation;

            if (_collider != null) _collider.enabled = true;
            if (_renderer != null) _renderer.enabled = true;

            if (WorldStateManager.Instance != null && WorldStateManager.Instance.Registry.HasEntry(keyId))
            {
                WorldStateManager.Instance.Registry.SetState(keyId, PropLocationState.AtBaseline);
            }

            Debug.Log($"[InteractableKey] {keyDisplayName} returned to baseline table position on loop turnover.");
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
