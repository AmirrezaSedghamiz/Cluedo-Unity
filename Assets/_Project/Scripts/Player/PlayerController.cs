using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.Simulation;

namespace TomorrowsCrime.Player
{
    /// <summary>
    /// Robust First-Person Player Controller using CharacterController.
    /// Handles WASD movement, mouse look, sprinting, and implements
    /// ILoopResetReceiver to restore initial spawn transform on loop turnover.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour, ILoopResetReceiver
    {
        [Header("Locomotion")]
        [SerializeField] private float walkSpeed = 4.0f;
        [SerializeField] private float sprintSpeed = 6.5f;
        [SerializeField] private float gravity = -15.0f;

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

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;

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

        private void Update()
        {
            HandleMouseLook();
            HandleMovement();
            HandleCursorToggle();
        }

        private void HandleMouseLook()
        {
            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

            transform.Rotate(Vector3.up * mouseX);

            _cameraPitch -= mouseY;
            _cameraPitch = Mathf.Clamp(_cameraPitch, minPitch, maxPitch);

            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
            }
        }

        private void HandleMovement()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            bool isSprinting = Input.GetKey(KeyCode.LeftShift);
            float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

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
        }

        private void HandleCursorToggle()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
            }
        }

        public void OnLoopReset()
        {
            // Temporarily disable CharacterController to avoid collision interference during teleportation
            _characterController.enabled = false;
            transform.position = _spawnPosition;
            transform.rotation = _spawnRotation;
            _cameraPitch = 0f;
            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.identity;
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
