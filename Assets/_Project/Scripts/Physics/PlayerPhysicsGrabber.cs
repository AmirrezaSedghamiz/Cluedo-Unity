using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.Player;

namespace TomorrowsCrime.PhysicsEngine
{
    /// <summary>
    /// Player physical interaction handler that enables picking up, carrying with spring physics,
    /// and throwing Rigidbody props in 3D space.
    /// </summary>
    public class PlayerPhysicsGrabber : MonoBehaviour
    {
        [Header("Grab & Hold Settings")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float grabRange = 2.8f;
        [SerializeField] private float holdDistance = 1.6f;
        [SerializeField] private float springStrength = 18.0f;
        [SerializeField] private float maxHoldDistance = 3.8f;

        [Header("Throwing Settings")]
        [SerializeField] private float throwForce = 9.5f;
        [SerializeField] private float throwTorque = 4.0f;

        private PhysicsProp _heldProp;
        private PlayerController _playerController;

        public PhysicsProp HeldProp => _heldProp;
        public bool IsHoldingProp => _heldProp != null;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }
        }

        private void Update()
        {
            if (_playerController != null && _playerController.IsInputBlocked)
            {
                if (_heldProp != null) ReleaseHeldProp();
                return;
            }

            if (_heldProp != null)
            {
                HandleHeldPropPhysics();
                HandleThrowInput();
            }
            else if (Input.GetKeyDown(KeyCode.G))
            {
                TryGrabInFront();
            }
        }

        public void TryGrabInFront()
        {
            if (playerCamera == null) return;
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (UnityEngine.Physics.Raycast(ray, out RaycastHit hit, grabRange))
            {
                var prop = hit.collider.GetComponentInParent<PhysicsProp>();
                if (prop != null)
                {
                    GrabProp(prop);
                }
            }
        }

        public void GrabProp(PhysicsProp prop)
        {
            if (prop == null || _heldProp != null) return;

            _heldProp = prop;
            _heldProp.SetHeld(true);
        }

        public void ReleaseHeldProp()
        {
            if (_heldProp == null) return;

            _heldProp.SetHeld(false);
            _heldProp = null;
        }

        private void HandleHeldPropPhysics()
        {
            if (_heldProp == null || playerCamera == null) return;

            Transform camTransform = playerCamera.transform;
            float desiredDist = holdDistance;

            // Obstacle sweep: prevent pushing held object inside walls or obstacles
            if (UnityEngine.Physics.SphereCast(camTransform.position, 0.18f, camTransform.forward, out RaycastHit wallHit, holdDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (wallHit.collider.gameObject != _heldProp.gameObject && !wallHit.collider.transform.IsChildOf(_heldProp.transform))
                {
                    desiredDist = Mathf.Clamp(wallHit.distance - 0.22f, 0.55f, holdDistance);
                }
            }

            Vector3 targetPosition = camTransform.position + camTransform.forward * desiredDist;

            // Check if object is pulled too far (e.g. caught on wall)
            float distFromCam = Vector3.Distance(camTransform.position, _heldProp.transform.position);
            if (distFromCam > maxHoldDistance)
            {
                ReleaseHeldProp();
                return;
            }

            // Spring force towards target point in front of camera
            Vector3 direction = targetPosition - _heldProp.transform.position;
            _heldProp.Rigidbody.linearVelocity = direction * springStrength;

            // Orient smoothly to camera facing
            Quaternion targetRot = Quaternion.LookRotation(camTransform.forward, camTransform.up);
            _heldProp.transform.rotation = Quaternion.Slerp(_heldProp.transform.rotation, targetRot, Time.deltaTime * 10f);
        }

        private void HandleThrowInput()
        {
            // Left click throws with physical impulse
            if (Input.GetMouseButtonDown(0))
            {
                ThrowHeldProp();
            }
            // Right click or E drops gently
            else if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Q))
            {
                ReleaseHeldProp();
            }
        }

        private void ThrowHeldProp()
        {
            if (_heldProp == null || playerCamera == null) return;

            var prop = _heldProp;
            ReleaseHeldProp();

            Vector3 throwDirection = playerCamera.transform.forward;
            float force = throwForce * prop.ThrowForceMultiplier;

            prop.Rigidbody.AddForce(throwDirection * force, ForceMode.Impulse);
            prop.Rigidbody.AddTorque(Random.insideUnitSphere * throwTorque, ForceMode.Impulse);
        }
    }
}
