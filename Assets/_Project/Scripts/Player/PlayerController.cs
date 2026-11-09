using UnityEngine;
using TomorrowsCrime.Audio;
using TomorrowsCrime.Core;
using TomorrowsCrime.Settings;
using TomorrowsCrime.Simulation;
using TomorrowsCrime.UI.Core;

namespace TomorrowsCrime.Player
{
    /// <summary>
    /// Robust First-Person Player Controller using CharacterController with realistic physics,
    /// momentum-based locomotion, jumping, crouching, dynamic head bobbing, push physics,
    /// strafe camera tilt, distance footstep audio, and animated viewmodel integration.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour, ILoopResetReceiver
    {
        [Header("Locomotion & Momentum")]
        [SerializeField] private float walkSpeed = 3.8f;
        [SerializeField] private float sprintSpeed = 6.4f;
        [SerializeField] private float crouchSpeed = 2.0f;
        [SerializeField] private float acceleration = 12.0f;
        [SerializeField] private float brakingDeceleration = 14.0f;

        [Header("Jump & Vertical Physics")]
        [SerializeField] private float jumpForce = 5.6f;
        [SerializeField] private float gravity = -18.0f;
        [SerializeField] private float airControl = 0.5f;

        [Header("Push Physics")]
        [SerializeField] private float pushPower = 2.4f;

        [Header("Crouch Configuration")]
        [SerializeField] private float standingHeight = 2.0f;
        [SerializeField] private float crouchingHeight = 1.15f;
        [SerializeField] private float standingEyeHeight = 1.75f;
        [SerializeField] private float crouchingEyeHeight = 0.95f;
        [SerializeField] private float crouchTransitionSpeed = 8f;

        [Header("Head Bobbing & Camera Tilt")]
        [SerializeField] private bool enableHeadBob = true;
        [SerializeField] private float bobFrequency = 9.0f;
        [SerializeField] private float bobHorizontalAmplitude = 0.035f;
        [SerializeField] private float bobVerticalAmplitude = 0.045f;
        [SerializeField] private float strafeCameraTilt = 1.6f;

        [Header("Mouse Look")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float mouseSensitivity = 2.0f;
        [SerializeField] private float maxPitch = 85.0f;
        [SerializeField] private float minPitch = -85.0f;

        private CharacterController _characterController;
        private PlayerViewmodelAnimator _viewmodelAnimator;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private float _verticalVelocity;
        private float _cameraPitch;
        private float _cameraRoll;

        // Locomotion & Physics State
        private Vector3 _currentPlanarVelocity = Vector3.zero;
        private bool _isCrouching = false;
        private bool _wasGroundedLastFrame = true;
        private float _currentEyeHeight;
        private float _bobTimer = 0f;
        private float _footstepDistanceCounter = 0f;
        private Vector3 _lastPosition;
        private float _lastMouseX = 0f;
        private float _lastMouseY = 0f;

        // Current Room Detection
        public string CurrentRoomName { get; private set; } = "Meridian House • Grand Lobby";

        public bool IsCrouching => _isCrouching;
        public bool IsGrounded => _characterController != null && _characterController.isGrounded;
        public Vector3 Velocity => _characterController != null ? _characterController.velocity : Vector3.zero;
        public bool IsRidingElevator { get; set; }

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            // Ensure safe spawn elevation inside Grand Lobby
            if (transform.position.y < 0.1f || transform.position.z < -6.0f)
            {
                transform.position = new Vector3(0f, 0.15f, -2.0f);
            }

            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
            _lastPosition = transform.position;
            _currentEyeHeight = standingEyeHeight;

            if (cameraTransform == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) cameraTransform = cam.transform;
            }

            // Ensure PlayerViewmodelAnimator is attached for animated hands
            _viewmodelAnimator = GetComponent<PlayerViewmodelAnimator>();
            if (_viewmodelAnimator == null && cameraTransform != null)
            {
                _viewmodelAnimator = gameObject.AddComponent<PlayerViewmodelAnimator>();
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
            // Void Fall Safety Guard: Prevent player from ever falling through the floor into the void
            if (transform.position.y < -1.0f)
            {
                _characterController.enabled = false;
                transform.position = new Vector3(_spawnPosition.x, 0.15f, _spawnPosition.z);
                _verticalVelocity = 0f;
                _characterController.enabled = true;
            }

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
            _lastMouseX = mouseX;
            _lastMouseY = mouseY;

            transform.Rotate(Vector3.up * mouseX);

            _cameraPitch -= mouseY;
            _cameraPitch = Mathf.Clamp(_cameraPitch, minPitch, maxPitch);

            // Dynamic camera roll into strafing motion
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            float targetRoll = -horizontalInput * strafeCameraTilt;
            _cameraRoll = Mathf.Lerp(_cameraRoll, targetRoll, Time.deltaTime * 6f);

            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.Euler(_cameraPitch, 0f, _cameraRoll);
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
            bool isGrounded = _characterController.isGrounded;

            // Landing Impact Detection
            if (!_wasGroundedLastFrame && isGrounded)
            {
                if (_verticalVelocity < -2.0f)
                {
                    _viewmodelAnimator?.TriggerLandingImpact(_verticalVelocity);
                    bool isCarpet = CheckIsOnCarpet();
                    AudioManager.Instance?.PlayFootstep(false, isCarpet);
                }
            }
            _wasGroundedLastFrame = isGrounded;

            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            bool isSprinting = !_isCrouching && Input.GetKey(KeyCode.LeftShift);
            float targetSpeed = _isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);

            Vector3 inputDirection = (transform.forward * vertical + transform.right * horizontal);
            if (inputDirection.sqrMagnitude > 1.0f) inputDirection.Normalize();

            Vector3 targetPlanarVelocity = inputDirection * targetSpeed;

            // Smooth Kinematic Momentum (Acceleration & Deceleration Inertia)
            float accelRate = inputDirection.sqrMagnitude > 0.001f ? acceleration : brakingDeceleration;
            if (!isGrounded) accelRate *= airControl;

            _currentPlanarVelocity = Vector3.Lerp(_currentPlanarVelocity, targetPlanarVelocity, Time.deltaTime * accelRate);

            // Vertical Jump Physics & Gravity
            if (IsRidingElevator)
            {
                _verticalVelocity = 0f;
            }
            else if (isGrounded)
            {
                if (_verticalVelocity < 0f) _verticalVelocity = -1.5f;

                if (Input.GetKeyDown(KeyCode.Space) && !_isCrouching)
                {
                    _verticalVelocity = jumpForce;
                    bool isCarpet = CheckIsOnCarpet();
                    AudioManager.Instance?.PlayFootstep(true, isCarpet);
                }
            }
            else
            {
                _verticalVelocity += gravity * Time.deltaTime;
            }

            Vector3 fullVelocity = _currentPlanarVelocity;
            fullVelocity.y = IsRidingElevator ? 0f : _verticalVelocity;

            _characterController.Move(fullVelocity * Time.deltaTime);

            // Handle Footstep Audio
            Vector3 horizontalMove = transform.position - _lastPosition;
            horizontalMove.y = 0f;
            float distMoved = horizontalMove.magnitude;

            if (isGrounded && distMoved > 0.001f)
            {
                _footstepDistanceCounter += distMoved;
                float stepThreshold = _isCrouching ? 2.1f : (isSprinting ? 1.35f : 1.75f);

                if (_footstepDistanceCounter >= stepThreshold)
                {
                    _footstepDistanceCounter = 0f;
                    bool isCarpet = CheckIsOnCarpet();
                    AudioManager.Instance?.PlayFootstep(isSprinting, isCarpet);
                }
            }

            // Update Viewmodel Animated Hands
            bool hasMoveInput = inputDirection.sqrMagnitude > 0.01f;
            if (_viewmodelAnimator != null)
            {
                _viewmodelAnimator.UpdateViewmodel(isGrounded, hasMoveInput, isSprinting, _isCrouching, _lastMouseX, _lastMouseY);
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

        /// <summary>
        /// Push physical Rigidbody objects upon collision.
        /// </summary>
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var body = hit.collider.attachedRigidbody;
            if (body == null || body.isKinematic) return;

            // Do not push objects we are standing directly on top of
            if (hit.moveDirection.y < -0.3f) return;

            Vector3 pushDir = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
            float speed = _currentPlanarVelocity.magnitude;
            float force = pushPower * Mathf.Max(speed, 1.0f);

            body.AddForceAtPosition(pushDir * force, hit.point, ForceMode.Impulse);
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
                Cursor.lockState = (Cursor.lockState == CursorLockMode.Locked) ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
            }
        }

        public void OnLoopReset()
        {
            _characterController.enabled = false;
            transform.position = _spawnPosition.y < 0.1f ? new Vector3(_spawnPosition.x, 0.15f, _spawnPosition.z) : _spawnPosition;
            transform.rotation = _spawnRotation;
            _lastPosition = _spawnPosition;
            _cameraPitch = 0f;
            _cameraRoll = 0f;
            _isCrouching = false;
            _currentEyeHeight = standingEyeHeight;
            _currentPlanarVelocity = Vector3.zero;

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
