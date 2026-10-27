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
        [SerializeField] private string requiredKeyId = "key_room_102";
        [SerializeField] private Transform doorHingeTransform;

        [Header("State")]
        [SerializeField] private DoorLockState lockState = DoorLockState.Locked;
        [SerializeField] private DoorOpenState openState = DoorOpenState.Closed;

        private Quaternion _closedRotation;
        private Quaternion _openRotation;

        public string DoorId => doorId;
        public string RequiredKeyId
        {
            get => requiredKeyId;
            set => requiredKeyId = value;
        }
        public DoorLockState LockState => lockState;
        public DoorOpenState OpenState => openState;
        public WorldStateRegistry WorldStateRegistryOverride { get; set; }

        public string InteractionPrompt
        {
            get
            {
                if (lockState == DoorLockState.Locked)
                {
                    bool hasKey = PlayerHasRequiredKey();
                    return hasKey
                        ? $"Unlock {doorDisplayName} [E] (Using Keycard)"
                        : $"{doorDisplayName} (Locked - Requires Keycard)";
                }
                return openState == DoorOpenState.Open
                    ? $"Press E to close {doorDisplayName}"
                    : $"Press E to open {doorDisplayName}";
            }
        }

        public bool CanInteract => true;

        private bool PlayerHasRequiredKey()
        {
            if (string.IsNullOrEmpty(requiredKeyId)) return true;
            var registry = WorldStateRegistryOverride ?? (WorldStateManager.Instance != null ? WorldStateManager.Instance.Registry : null);
            if (registry != null && registry.TryGetState<PropLocationState>(requiredKeyId, out var state))
            {
                return state == PropLocationState.HeldByPlayer;
            }
            return false;
        }

        private void Awake()
        {
            if (doorHingeTransform == null)
            {
                doorHingeTransform = transform;
            }
            _closedRotation = doorHingeTransform.localRotation;
            _openRotation = _closedRotation * Quaternion.Euler(0f, 90f, 0f);

            if (openState == DoorOpenState.Open)
            {
                doorHingeTransform.localRotation = _openRotation;
            }
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
                if (PlayerHasRequiredKey())
                {
                    UnlockAndOpen();
                    TomorrowsCrime.Audio.AudioManager.Instance?.PlayLockClick();
                    TomorrowsCrime.Audio.AudioManager.Instance?.PlayDoorCreak();
                    TomorrowsCrime.UI.Core.UIManager.Instance?.ShowToast("DOOR UNLOCKED", $"{doorDisplayName} unlocked using keycard.", TomorrowsCrime.UI.Styling.UITheme.StatusSuccess);
                    return;
                }
                else
                {
                    Debug.Log($"[InteractableDoor] {doorDisplayName} is locked. Keycard '{requiredKeyId}' required.");
                    TomorrowsCrime.Audio.AudioManager.Instance?.PlayLockClick();
                    TomorrowsCrime.UI.Core.UIManager.Instance?.ShowToast("DOOR LOCKED", $"{doorDisplayName} is secured with an electronic deadbolt. Find the room keycard.", TomorrowsCrime.UI.Styling.UITheme.StatusAmber);
                    return;
                }
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
            if (string.IsNullOrEmpty(requiredKeyId))
            {
                // Naturally unlocked door restores to open
                lockState = DoorLockState.Unlocked;
                openState = DoorOpenState.Open;
                if (doorHingeTransform != null) doorHingeTransform.localRotation = _openRotation;
                if (WorldStateManager.Instance != null && WorldStateManager.Instance.Registry.HasEntry(doorId))
                {
                    WorldStateManager.Instance.Registry.SetState(doorId, DoorLockState.Unlocked);
                }
            }
            else
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
            }

            Debug.Log($"[InteractableDoor] {doorDisplayName} restored to baseline on loop reset.");
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
