using UnityEngine;
using TomorrowsCrime.Audio;
using TomorrowsCrime.Core;
using TomorrowsCrime.Settings;
using TomorrowsCrime.Simulation;
using TomorrowsCrime.UI.Core;

namespace TomorrowsCrime.Player
{
    /// <summary>
    /// Robust First-Person Player Controller using CharacterController.
    /// Features:
    /// - Smooth WASD movement with walk, sprint, and crouch modes.
    /// - Dynamic procedural head-bobbing based on movement speed.
    /// - Distance-based footstep audio triggers that differentiate carpet vs hardwood.
    /// - Contextual room location detection for atmospheric HUD navigation.
    /// - Full loop turnover reset via ILoopResetReceiver.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour, ILoopResetReceiver
    {
        [Header("Locomotion")]
        [SerializeField] private float walkSpeed = 3.8f;
        [SerializeField] private float sprintSpeed = 6.2f;
        [SerializeField] private float crouchSpeed = 2.0f;
        [SerializeField] private float gravity = -15.0f;

        [Header("Crouch Configuration")]
        [SerializeField] private float standingHeight = 2.0f;
        [SerializeField] private float crouchingHeight = 1.15f;
        [SerializeField] private float standingEyeHeight = 1.75f;
        [SerializeField] private float crouchingEyeHeight = 0.95f;
        [SerializeField] private float crouchTransitionSpeed = 8f;

        [Header("Head Bobbing")]
        [SerializeField] private bool enableHeadBob = true;
        [SerializeField] private float bobFrequency = 9.0f;
        [SerializeField] private float bobHorizontalAmplitude = 0.035f;
        [SerializeField] private float bobVerticalAmplitude = 0.045f;

        [Header("Mouse Look")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float mouseSensitivity = 2.0f;
        [SerializeField] private float maxPitch = 85.0f;
        [SerializeField] private float minPitch = -85.0f;

        private CharacterController _characterController;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private float _verticalVelocity;
        private float _cameraPitch;

        // Locomotion & Head Bob State
        private bool _isCrouching = false;
        private float _currentEyeHeight;
        private float _bobTimer = 0f;
        private float _footstepDistanceCounter = 0f;
        private Vector3 _lastPosition;

        // Current Room Detection
        public string CurrentRoomName { get; private set; } = "Meridian House • Grand Lobby";

        public bool IsCrouching => _isCrouching;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
            _lastPosition = transform.position;
            _currentEyeHeight = standingEyeHeight;

            if (cameraTransform == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) cameraTransform = cam.transform;
            }
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.RegisterResetReceiver(this);
            }
        }

        public bool IsInputBlocked { get; set; }

        public void SetInputBlocked(bool blocked)
        {
            IsInputBlocked = blocked;
            if (blocked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void Update()
        {
            if (IsInputBlocked) return;

            HandleMouseLook();
            HandleCrouch();
            HandleMovement();
            HandleHeadBob();
            UpdateRoomDetection();
            HandleCursorToggle();
        }

        private void HandleMouseLook()
        {
            float activeSensitivity = GameSettingsManager.Instance != null
                ? GameSettingsManager.Instance.MouseSensitivity
                : mouseSensitivity;

            float mouseX = Input.GetAxisRaw("Mouse X") * activeSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * activeSensitivity;

            transform.Rotate(Vector3.up * mouseX);

            _cameraPitch -= mouseY;
            _cameraPitch = Mathf.Clamp(_cameraPitch, minPitch, maxPitch);

            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
            }
        }

        private void HandleCrouch()
        {
            if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl))
            {
                _isCrouching = !_isCrouching;
            }

            float targetHeight = _isCrouching ? crouchingHeight : standingHeight;
            float targetEyeHeight = _isCrouching ? crouchingEyeHeight : standingEyeHeight;

            _characterController.height = Mathf.Lerp(_characterController.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
            _characterController.center = new Vector3(0f, _characterController.height * 0.5f, 0f);
            _currentEyeHeight = Mathf.Lerp(_currentEyeHeight, targetEyeHeight, Time.deltaTime * crouchTransitionSpeed);
        }

        private void HandleMovement()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            bool isSprinting = !_isCrouching && Input.GetKey(KeyCode.LeftShift);
            float currentSpeed = _isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);

            Vector3 moveDirection = (transform.forward * vertical + transform.right * horizontal).normalized;

            if (_characterController.isGrounded)
            {
                _verticalVelocity = -0.5f;
            }
            else
            {
                _verticalVelocity += gravity * Time.deltaTime;
            }

            Vector3 velocity = moveDirection * currentSpeed;
            velocity.y = _verticalVelocity;

            _characterController.Move(velocity * Time.deltaTime);

            // Handle Procedural Footstep Sounds based on ground distance moved
            Vector3 horizontalMove = transform.position - _lastPosition;
            horizontalMove.y = 0f;
            float distMoved = horizontalMove.magnitude;

            if (_characterController.isGrounded && distMoved > 0.001f)
            {
                _footstepDistanceCounter += distMoved;
                float stepThreshold = _isCrouching ? 2.1f : (isSprinting ? 1.4f : 1.8f);

                if (_footstepDistanceCounter >= stepThreshold)
                {
                    _footstepDistanceCounter = 0f;
                    bool isCarpet = CheckIsOnCarpet();
                    AudioManager.Instance?.PlayFootstep(isSprinting, isCarpet);
                }
            }

            _lastPosition = transform.position;
        }

        private bool CheckIsOnCarpet()
        {
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 1.2f))
            {
                string hitName = hit.collider.gameObject.name.ToLower();
                if (hitName.Contains("carpet") || hitName.Contains("runner") || hitName.Contains("rug"))
                {
                    return true;
                }
            }
            return false;
        }

        private void HandleHeadBob()
        {
            if (!enableHeadBob || cameraTransform == null) return;

            Vector3 horizontalMove = transform.position - _lastPosition;
            horizontalMove.y = 0f;
            bool isMoving = _characterController.isGrounded && (Input.GetAxisRaw("Horizontal") != 0f || Input.GetAxisRaw("Vertical") != 0f);

            if (isMoving)
            {
                bool isSprinting = !_isCrouching && Input.GetKey(KeyCode.LeftShift);
                float speedMultiplier = isSprinting ? 1.4f : (_isCrouching ? 0.7f : 1.0f);
                _bobTimer += Time.deltaTime * bobFrequency * speedMultiplier;

                float bobX = Mathf.Cos(_bobTimer * 0.5f) * bobHorizontalAmplitude * (isSprinting ? 1.3f : 1.0f);
                float bobY = Mathf.Sin(_bobTimer) * bobVerticalAmplitude * (isSprinting ? 1.3f : 1.0f);

                cameraTransform.localPosition = new Vector3(bobX, _currentEyeHeight + bobY, 0f);
            }
            else
            {
                _bobTimer = 0f;
                cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, new Vector3(0f, _currentEyeHeight, 0f), Time.deltaTime * 6f);
            }
        }

        private void UpdateRoomDetection()
        {
            Vector3 pos = transform.position;

            // 1. Dynamic check against active procedural room bounds if present
            var currentInv = Procedural.Core.GameSessionManager.Instance?.CurrentInvestigation;
            if (currentInv != null && currentInv.Rooms != null)
            {
                foreach (var room in currentInv.Rooms)
                {
                    // Check horizontal bounds with generous vertical margin
                    var b = room.Bounds;
                    if (pos.x >= b.min.x && pos.x <= b.max.x &&
                        pos.z >= b.min.z && pos.z <= b.max.z &&
                        pos.y >= (room.FloorIndex * 5.0f - 0.5f) && pos.y < ((room.FloorIndex + 1) * 5.0f - 0.5f))
                    {
                        CurrentRoomName = $"Meridian House • {room.DisplayName}";
                        return;
                    }
                }
            }

            // 2. Spatial bounding classifier across 4 floors
            if (pos.y >= 14.5f)
            {
                // Floor 3: Penthouses, Observatory & Solarium
                if (pos.x < -10f && pos.z > 4f) CurrentRoomName = "Meridian House • Presidential Master Bedroom";
                else if (pos.x < -10f && pos.z < -4f) CurrentRoomName = "Meridian House • Presidential Private Study";
                else if (pos.x < -10f) CurrentRoomName = "Meridian House • Crown Penthouse Salon";
                else if (pos.z > 5f && Mathf.Abs(pos.x) < 7f) CurrentRoomName = "Meridian House • Clock Tower Observatory";
                else if (pos.z < -5f && pos.x < 0f) CurrentRoomName = "Meridian House • Rooftop Solarium & Terrace";
                else if (pos.z < -5f && pos.x >= 0f) CurrentRoomName = "Meridian House • Roof Garden Lookout";
                else if (pos.x > 10f && pos.z > 2f) CurrentRoomName = "Meridian House • Royal Penthouse Suite 301";
                else if (pos.x > 10f && pos.z < -2f) CurrentRoomName = "Meridian House • Royal Penthouse Suite 302";
                else CurrentRoomName = "Meridian House • Floor 3 Sky Foyer";
            }
            else if (pos.y >= 9.5f)
            {
                // Floor 2: Deluxe Suites, Music Salon & Billiards
                if (pos.x < -10f && pos.z > 4f) CurrentRoomName = "Meridian House • Deluxe Suite 201 - Ambassador";
                else if (pos.x < -10f && pos.z < -4f) CurrentRoomName = "Meridian House • Deluxe Suite 203 - Professor";
                else if (pos.x < -10f) CurrentRoomName = "Meridian House • Billiards & Game Hall";
                else if (pos.z > 5f && Mathf.Abs(pos.x) < 7f) CurrentRoomName = "Meridian House • Meridian Grand Music Salon";
                else if (pos.z < -5f && pos.x < 0f) CurrentRoomName = "Meridian House • Velvet Cigar & Cognac Den";
                else if (pos.z < -5f && pos.x >= 0f) CurrentRoomName = "Meridian House • Floor 2 Service Annex";
                else if (pos.x > 10f && pos.z > 2f) CurrentRoomName = "Meridian House • Deluxe Suite 202 - Countess";
                else if (pos.x > 10f && pos.z < -2f) CurrentRoomName = "Meridian House • Deluxe Suite 204 - Captain";
                else CurrentRoomName = "Meridian House • Floor 2 Promenade";
            }
            else if (pos.y >= 4.5f)
            {
                // Floor 1: Guest Suites & Grand Library
                if (pos.x < -10f && pos.z > 4f) CurrentRoomName = "Meridian House • Guest Suite 101 - Lord Harrington";
                else if (pos.x < -10f && pos.z < -4f) CurrentRoomName = "Meridian House • Guest Suite 103 - Dr. Sterling";
                else if (pos.x < -10f) CurrentRoomName = "Meridian House • Meridian Grand Library";
                else if (pos.x > 10f && pos.z > 2f) CurrentRoomName = "Meridian House • VIP Suite 102 - Crime Scene";
                else if (pos.x > 10f && pos.z < -2f) CurrentRoomName = "Meridian House • Housekeeping Central Storage";
                else if (pos.z < -5f && pos.x < 0f) CurrentRoomName = "Meridian House • Gentleman's Card Room";
                else if (pos.z < -5f && pos.x >= 0f) CurrentRoomName = "Meridian House • Floor 1 Linen Annex";
                else if (pos.z > 5f && Mathf.Abs(pos.x) < 7f) CurrentRoomName = "Meridian House • North Reading Parlor";
                else CurrentRoomName = "Meridian House • Floor 1 Mezzanine Gallery";
            }
            else
            {
                // Floor 0: Ground Floor & Lobby
                if (pos.z > 3.0f && pos.z < 8.0f && Mathf.Abs(pos.x) < 5.0f) CurrentRoomName = "Meridian House • Front Reception Desk";
                else if (pos.z >= 8.0f && Mathf.Abs(pos.x) < 5.0f) CurrentRoomName = "Meridian House • Room 102 Grand Entrance";
                else if (pos.x < -7f && pos.z > 5f) CurrentRoomName = "Meridian House • Hotel Kitchen & Scullery";
                else if (pos.x < -7f && pos.z < -5f) CurrentRoomName = "Meridian House • Winter Garden Conservatory";
                else if (pos.x < -7f) CurrentRoomName = "Meridian House • Grand Dining Hall";
                else if (pos.x > 7f && pos.z > 5f) CurrentRoomName = "Meridian House • Manager's Executive Office";
                else if (pos.x > 7f && pos.z < -5f) CurrentRoomName = "Meridian House • Maintenance & Boiler Utility";
                else if (pos.x > 7f) CurrentRoomName = "Meridian House • Central Grand Staircase Hub";
                else if (pos.z < -5f && pos.x < 0f) CurrentRoomName = "Meridian House • The Golden Quill Bar & Lounge";
                else if (pos.z < -5f && pos.x >= 0f) CurrentRoomName = "Meridian House • Luggage & Bellhop Storage";
                else CurrentRoomName = "Meridian House • Central Marble Lobby";
            }
        }

        private void HandleCursorToggle()
        {
            if (UIManager.Instance != null) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
            }
        }

        public void OnLoopReset()
        {
            _characterController.enabled = false;
            transform.position = _spawnPosition;
            transform.rotation = _spawnRotation;
            _lastPosition = _spawnPosition;
            _cameraPitch = 0f;
            _isCrouching = false;
            _currentEyeHeight = standingEyeHeight;

            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.identity;
                cameraTransform.localPosition = new Vector3(0f, standingEyeHeight, 0f);
            }
            _verticalVelocity = 0f;
            _characterController.enabled = true;

            Debug.Log("[PlayerController] Player returned to initial spawn position for loop turnover.");
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
