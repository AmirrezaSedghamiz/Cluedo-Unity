using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.Simulation;

namespace TomorrowsCrime.World
{
    /// <summary>
    /// Interactive hotel door with explicit lock and open states.
    /// Can be opened/closed when unlocked, or unlocked via scenario events.
    /// Resets deterministically to locked and closed on loop turnover.
    /// </summary>
    public class InteractableDoor : MonoBehaviour, IInteractable, ILoopResetReceiver
    {
        [Header("Configuration")]
        [SerializeField] private string doorId = "door_room_102";
        [SerializeField] private string doorDisplayName = "Room 102";
        [SerializeField] private Transform doorHingeTransform;

        [Header("State")]
        [SerializeField] private DoorLockState lockState = DoorLockState.Locked;
        [SerializeField] private DoorOpenState openState = DoorOpenState.Closed;

        private Quaternion _closedRotation;
        private Quaternion _openRotation;

        public string DoorId => doorId;
        public DoorLockState LockState => lockState;
        public DoorOpenState OpenState => openState;

        public string InteractionPrompt
        {
            get
            {
                if (lockState == DoorLockState.Locked)
                {
                    return $"{doorDisplayName} is Locked";
                }
                return openState == DoorOpenState.Open
                    ? $"Press E to close {doorDisplayName}"
                    : $"Press E to open {doorDisplayName}";
            }
        }

        public bool CanInteract => true;

        private void Awake()
        {
            if (doorHingeTransform == null)
            {
                doorHingeTransform = transform;
            }
            _closedRotation = doorHingeTransform.localRotation;
            _openRotation = _closedRotation * Quaternion.Euler(0f, 90f, 0f);
        }

        private void Start()
        {
            RegisterWithManagers();
        }

        private void RegisterWithManagers()
        {
            if (WorldStateManager.Instance != null && !WorldStateManager.Instance.Registry.HasEntry(doorId))
            {
                WorldStateManager.Instance.Registry.RegisterEntry(doorId, DoorLockState.Locked);
            }

            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.RegisterResetReceiver(this);
            }
        }

        public void Interact(GameObject interactor)
        {
            if (lockState == DoorLockState.Locked)
            {
                Debug.Log($"[InteractableDoor] {doorDisplayName} is locked. Interaction cannot open it without key/unlock.");
                TomorrowsCrime.Audio.AudioManager.Instance?.PlayLockClick();
                return;
            }

            ToggleOpen();
        }

        public void UnlockAndOpen()
        {
            lockState = DoorLockState.Unlocked;
            openState = DoorOpenState.Open;

            if (doorHingeTransform != null)
            {
                doorHingeTransform.localRotation = _openRotation;
            }

            if (WorldStateManager.Instance != null)
            {
                WorldStateManager.Instance.Registry.SetState(doorId, DoorLockState.Unlocked);
            }

            TomorrowsCrime.Audio.AudioManager.Instance?.PlayLockClick();
            TomorrowsCrime.Audio.AudioManager.Instance?.PlayDoorCreak();
            Debug.Log($"[InteractableDoor] {doorDisplayName} unlocked and opened by scenario event.");
        }

        public void ToggleOpen()
        {
            openState = openState == DoorOpenState.Closed ? DoorOpenState.Open : DoorOpenState.Closed;
            if (doorHingeTransform != null)
            {
                doorHingeTransform.localRotation = openState == DoorOpenState.Open ? _openRotation : _closedRotation;
            }
            TomorrowsCrime.Audio.AudioManager.Instance?.PlayDoorCreak();
        }

        public void OnLoopReset()
        {
            lockState = DoorLockState.Locked;
            openState = DoorOpenState.Closed;

            if (doorHingeTransform != null)
            {
                doorHingeTransform.localRotation = _closedRotation;
            }

            if (WorldStateManager.Instance != null && WorldStateManager.Instance.Registry.HasEntry(doorId))
            {
                WorldStateManager.Instance.Registry.SetState(doorId, DoorLockState.Locked);
            }

            Debug.Log($"[InteractableDoor] {doorDisplayName} restored to locked/closed baseline on loop reset.");
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
