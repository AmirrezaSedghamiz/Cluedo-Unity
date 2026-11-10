using System;
using UnityEngine;
using TomorrowsCrime.Audio;
using TomorrowsCrime.Core;
using TomorrowsCrime.Player;
using TomorrowsCrime.UI.Core;

namespace TomorrowsCrime.World
{
    /// <summary>
    /// Interactive 4-floor luxury Art Deco hotel elevator.
    /// Operates between Floor 0 (Lobby), Floor 1 (Guest Suites & Library),
    /// Floor 2 (Deluxe Suites & Salon), and Floor 3 (Sky Penthouse & Observatory).
    /// Provides smooth physical movement, synchronous CharacterController transport,
    /// floor call buttons, in-cabin floor selectors, and arrival notifications.
    /// </summary>
    public class HotelElevator : MonoBehaviour
    {
        [Header("Elevator Configuration")]
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private int currentFloor = 0;
        [SerializeField] private int targetFloor = 0;
        [SerializeField] private bool isMoving = false;

        [Header("Floor Elevations")]
        private readonly float[] _floorElevations = { 0.0f, 5.0f, 10.0f, 15.0f };
        private readonly string[] _floorNames = {
            "Floor 0 • Grand Marble Lobby",
            "Floor 1 • Guest Suites & Library",
            "Floor 2 • Deluxe Suites & Grand Salon",
            "Floor 3 • Sky Penthouse & Observatory"
        };

        private Transform _cabinTransform;
        private CharacterController _playerCharacterController;
        private PlayerController _playerController;
        private bool _isPlayerInside = false;
        private bool _isPlayerRiding = false;
        private readonly GameObject[] _landingGateBarriers = new GameObject[4];

        public int CurrentFloor => currentFloor;
        public int TargetFloor => targetFloor;
        public bool IsMoving => isMoving;
        public string CurrentFloorName => _floorNames[Mathf.Clamp(currentFloor, 0, 3)];

        public event Action<int> OnFloorChanged;
        public event Action<int> OnElevatorArrived;

        public void Initialize(Transform cabin)
        {
            _cabinTransform = cabin;
            transform.position = new Vector3(transform.position.x, _floorElevations[currentFloor], transform.position.z);
        }

        private void Start()
        {
            if (_cabinTransform == null)
            {
                _cabinTransform = transform;
            }
        }

        private void Update()
        {
            if (!isMoving)
            {
                CheckPlayerInsideCabin();
                return;
            }

            float targetY = _floorElevations[targetFloor];
            float currentY = _cabinTransform.position.y;
            float newY = Mathf.MoveTowards(currentY, targetY, moveSpeed * Time.deltaTime);
            float deltaY = newY - currentY;

            // Move elevator cabin
            _cabinTransform.position = new Vector3(_cabinTransform.position.x, newY, _cabinTransform.position.z);

            // Synchronously carry the player CharacterController up or down with the cabin
            if (_playerCharacterController == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    _playerCharacterController = player.GetComponent<CharacterController>();
                    _playerController = player.GetComponent<PlayerController>();
                }
            }

            if (_playerController == null && _playerCharacterController != null)
            {
                _playerController = _playerCharacterController.GetComponent<PlayerController>();
            }

            if (_isPlayerRiding && _playerCharacterController != null)
            {
                if (_playerController != null)
                {
                    _playerController.IsRidingElevator = true;
                }

                // Directly displace player transform to avoid character controller sweep jamming against rising floor
                bool wasCCEnabled = _playerCharacterController.enabled;
                _playerCharacterController.enabled = false;
                Vector3 pPos = _playerCharacterController.transform.position;
                pPos.y += deltaY;

                // Clamp to top of floor slab so player never sinks below the cabin floor
                float minFloorSurfaceY = newY;
                if (pPos.y < minFloorSurfaceY)
                {
                    pPos.y = minFloorSurfaceY;
                }

                _playerCharacterController.transform.position = pPos;
                _playerCharacterController.enabled = wasCCEnabled;
            }

            // Check if reached destination
            if (Mathf.Abs(newY - targetY) < 0.01f)
            {
                _cabinTransform.position = new Vector3(_cabinTransform.position.x, targetY, _cabinTransform.position.z);
                currentFloor = targetFloor;
                isMoving = false;

                if (_playerController != null)
                {
                    _playerController.IsRidingElevator = false;
                }
                _isPlayerRiding = false;

                HandleArrival();
            }
        }

        public void RegisterLandingGate(int floor, GameObject gateBarrier)
        {
            if (floor >= 0 && floor < _landingGateBarriers.Length)
            {
                _landingGateBarriers[floor] = gateBarrier;
                SetGateOpen(floor, floor == currentFloor);
            }
        }

        private void SetGateOpen(int floor, bool open)
        {
            if (floor < 0 || floor >= _landingGateBarriers.Length) return;
            var barrier = _landingGateBarriers[floor];
            if (barrier == null) return;

            var col = barrier.GetComponent<BoxCollider>();
            if (col != null)
            {
                col.isTrigger = open;
            }

            var gateL = barrier.transform.Find("Gate_Leaf_North");
            var gateR = barrier.transform.Find("Gate_Leaf_South");
            if (gateL != null) gateL.localPosition = new Vector3(0f, 0f, open ? 1.05f : 0.44f);
            if (gateR != null) gateR.localPosition = new Vector3(0f, 0f, open ? -1.05f : -0.44f);
        }

        public void MoveToFloor(int floorIndex)
        {
            if (floorIndex < 0 || floorIndex >= _floorElevations.Length) return;
            if (isMoving && targetFloor == floorIndex) return;

            // Close current floor shaft gate barrier when elevator departs
            SetGateOpen(currentFloor, false);

            CheckPlayerInsideCabin();
            if (_isPlayerInside)
            {
                _isPlayerRiding = true;
            }

            targetFloor = floorIndex;
            isMoving = true;

            string action = targetFloor > currentFloor ? "ASCENDING" : (targetFloor < currentFloor ? "DESCENDING" : "DOORS CLOSING");
            Debug.Log($"[HotelElevator] {action} to {_floorNames[targetFloor]} ({_floorElevations[targetFloor]}m)...");

            AudioManager.Instance?.PlayFootstep(false, false);
            UIManager.Instance?.ShowToast(
                $"ELEVATOR {action}",
                $"Destination: {_floorNames[targetFloor]}",
                UI.Styling.UITheme.BrassHighlight);

            OnFloorChanged?.Invoke(targetFloor);
        }

        private void HandleArrival()
        {
            Debug.Log($"[HotelElevator] Arrived at {_floorNames[currentFloor]}!");

            // Open arrival floor shaft gate barrier
            SetGateOpen(currentFloor, true);

            // Chime sound on arrival
            AudioManager.Instance?.PlayEvidenceDiscovery();

            UIManager.Instance?.ShowToast(
                "ELEVATOR ARRIVED",
                $"Now at: {_floorNames[currentFloor]}",
                UI.Styling.UITheme.StatusSuccess);

            OnElevatorArrived?.Invoke(currentFloor);
        }

        private void CheckPlayerInsideCabin()
        {
            if (_playerCharacterController == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    _playerCharacterController = player.GetComponent<CharacterController>();
                    _playerController = player.GetComponent<PlayerController>();
                }
            }

            if (_playerCharacterController == null || _cabinTransform == null)
            {
                _isPlayerInside = false;
                return;
            }

            Vector3 cabinPos = _cabinTransform.position;
            Vector3 playerPos = _playerCharacterController.transform.position;

            // Check if player is within the elevator cabin bounds
            bool inX = Mathf.Abs(playerPos.x - cabinPos.x) < 1.4f;
            bool inZ = Mathf.Abs(playerPos.z - cabinPos.z) < 1.4f;
            bool inY = playerPos.y >= (cabinPos.y - 0.5f) && playerPos.y <= (cabinPos.y + 3.5f);

            _isPlayerInside = inX && inZ && inY;
            if (_isPlayerInside && isMoving)
            {
                _isPlayerRiding = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _isPlayerInside = true;
                _playerCharacterController = other.GetComponent<CharacterController>();
                _playerController = other.GetComponent<PlayerController>();
                if (isMoving)
                {
                    _isPlayerRiding = true;
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && !isMoving)
            {
                _isPlayerInside = false;
                _isPlayerRiding = false;
                if (_playerController != null)
                {
                    _playerController.IsRidingElevator = false;
                }
            }
        }
    }
}
