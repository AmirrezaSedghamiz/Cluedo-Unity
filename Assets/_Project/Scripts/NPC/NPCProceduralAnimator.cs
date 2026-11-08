using UnityEngine;
using UnityEngine.AI;

namespace TomorrowsCrime.NPC
{
    /// <summary>
    /// Procedural humanoid locomotion animator for Tomorrow's Crime NPCs.
    /// Provides lifelike walking, running, idle breathing, footfall bobbing,
    /// and dynamic head look-at tracking towards the player.
    /// Eliminates rigid sliding cartoon movement.
    /// </summary>
    public class NPCProceduralAnimator : MonoBehaviour
    {
        [Header("Locomotion Tuning")]
        [SerializeField] private float walkStrideRate = 3.2f;
        [SerializeField] private float runStrideRate = 4.2f;
        [SerializeField] private float walkLegAngle = 28.0f;
        [SerializeField] private float runLegAngle = 44.0f;
        [SerializeField] private float walkArmAngle = 22.0f;
        [SerializeField] private float runArmAngle = 38.0f;
        [SerializeField] private float verticalBobAmount = 0.04f;

        [Header("Head Look-At Tracking")]
        [SerializeField] private bool enableHeadLookAt = true;
        [SerializeField] private float lookAtDistance = 6.0f;
        [SerializeField] private float maxLookAtAngle = 75.0f;

        // Limb Transforms
        private Transform _headTransform;
        private Transform _torsoTransform;
        private Transform _leftLegTransform;
        private Transform _rightLegTransform;
        private Transform _leftArmTransform;
        private Transform _rightArmTransform;

        // Pivot anchors for anatomical hip and shoulder rotation
        private Transform _leftHipPivot;
        private Transform _rightHipPivot;
        private Transform _leftShoulderPivot;
        private Transform _rightShoulderPivot;

        // Baseline rotations and positions
        private Quaternion _baseHeadRot;
        private Quaternion _baseTorsoRot;
        private Vector3 _baseTorsoPos;

        private NavMeshAgent _agent;
        private Transform _playerTransform;
        private Vector3 _lastWorldPos;
        private float _stridePhase = 0f;
        private float _currentSpeed = 0f;
        private float _smoothedSpeed = 0f;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _lastWorldPos = transform.position;

            LocateLimbsAndSetupPivots();
        }

        private void Start()
        {
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null)
            {
                var cam = playerGo.GetComponentInChildren<Camera>();
                _playerTransform = cam != null ? cam.transform : playerGo.transform;
            }
        }

        private void LocateLimbsAndSetupPivots()
        {
            var allTransforms = GetComponentsInChildren<Transform>(true);
            Transform shoeL = null;
            Transform shoeR = null;

            foreach (var t in allTransforms)
            {
                string lower = t.name.ToLower();
                if (_headTransform == null && lower.Contains("head")) _headTransform = t;
                else if (_torsoTransform == null && lower.Contains("torso")) _torsoTransform = t;
                else if (_leftLegTransform == null && (lower.Contains("leftleg") || lower.Contains("leg_l"))) _leftLegTransform = t;
                else if (_rightLegTransform == null && (lower.Contains("rightleg") || lower.Contains("leg_r"))) _rightLegTransform = t;
                else if (_leftArmTransform == null && (lower.Contains("arm_l") || lower.Contains("leftarm"))) _leftArmTransform = t;
                else if (_rightArmTransform == null && (lower.Contains("arm_r") || lower.Contains("rightarm"))) _rightArmTransform = t;

                if (shoeL == null && (lower.Contains("shoe_l") || lower.Contains("foot_l") || lower.Contains("shoe.l"))) shoeL = t;
                else if (shoeR == null && (lower.Contains("shoe_r") || lower.Contains("foot_r") || lower.Contains("shoe.r"))) shoeR = t;
            }

            // Ensure shoes are solidly parented to their respective legs before pivots are built
            if (shoeL != null && _leftLegTransform != null && shoeL.parent != _leftLegTransform)
            {
                shoeL.SetParent(_leftLegTransform, true);
            }
            if (shoeR != null && _rightLegTransform != null && shoeR.parent != _rightLegTransform)
            {
                shoeR.SetParent(_rightLegTransform, true);
            }

            // Create anatomical pivot hierarchy so limbs rotate from hip and shoulder joints
            if (_leftLegTransform != null && _leftHipPivot == null)
            {
                _leftHipPivot = CreatePivotAnchor(_leftLegTransform, "Pivot_Hip_L", new Vector3(0f, 0.45f, 0f));
            }
            if (_rightLegTransform != null && _rightHipPivot == null)
            {
                _rightHipPivot = CreatePivotAnchor(_rightLegTransform, "Pivot_Hip_R", new Vector3(0f, 0.45f, 0f));
            }
            if (_leftArmTransform != null && _leftShoulderPivot == null)
            {
                _leftShoulderPivot = CreatePivotAnchor(_leftArmTransform, "Pivot_Shoulder_L", new Vector3(0f, 0.32f, 0f));
            }
            if (_rightArmTransform != null && _rightShoulderPivot == null)
            {
                _rightShoulderPivot = CreatePivotAnchor(_rightArmTransform, "Pivot_Shoulder_R", new Vector3(0f, 0.32f, 0f));
            }

            if (_headTransform != null) _baseHeadRot = _headTransform.localRotation;
            if (_torsoTransform != null)
            {
                _baseTorsoRot = _torsoTransform.localRotation;
                _baseTorsoPos = _torsoTransform.localPosition;
            }
        }

        private Transform CreatePivotAnchor(Transform limb, string pivotName, Vector3 offsetFromCenterToJoint)
        {
            Transform parent = limb.parent;
            var pivot = new GameObject(pivotName).transform;
            pivot.SetParent(parent, false);
            pivot.position = limb.position + offsetFromCenterToJoint;
            pivot.rotation = limb.rotation;

            limb.SetParent(pivot, true);
            return pivot;
        }

        private void Update()
        {
            CalculateSpeed();
            UpdateLocomotionAnimation();
            UpdateHeadLookAt();
        }

        private void CalculateSpeed()
        {
            if (_agent != null && _agent.isOnNavMesh)
            {
                _currentSpeed = _agent.velocity.magnitude;
            }
            else
            {
                float dist = Vector3.Distance(transform.position, _lastWorldPos);
                _currentSpeed = dist / Mathf.Max(Time.deltaTime, 0.0001f);
            }
            _lastWorldPos = transform.position;

            _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, _currentSpeed, Time.deltaTime * 8.0f);
        }

        private void UpdateLocomotionAnimation()
        {
            bool isMoving = _smoothedSpeed > 0.12f;
            bool isRunning = _smoothedSpeed > 3.4f;

            if (isMoving)
            {
                float rate = isRunning ? runStrideRate : walkStrideRate;
                float strideFreq = rate * (_smoothedSpeed / 3.0f);
                _stridePhase += Time.deltaTime * strideFreq * Mathf.PI * 2.0f;

                float legAmp = isRunning ? runLegAngle : walkLegAngle;
                float armAmp = isRunning ? runArmAngle : walkArmAngle;

                // Legs counter-oscillation
                float leftLegAngle = Mathf.Sin(_stridePhase) * legAmp;
                float rightLegAngle = -Mathf.Sin(_stridePhase) * legAmp;

                if (_leftHipPivot != null)
                {
                    _leftHipPivot.localRotation = Quaternion.Euler(leftLegAngle, 0f, 0f);
                }
                if (_rightHipPivot != null)
                {
                    _rightHipPivot.localRotation = Quaternion.Euler(rightLegAngle, 0f, 0f);
                }

                // Arms counter-oscillation (opposite phase to legs)
                float leftArmAngle = -Mathf.Sin(_stridePhase) * armAmp;
                float rightArmAngle = Mathf.Sin(_stridePhase) * armAmp;

                if (_leftShoulderPivot != null)
                {
                    _leftShoulderPivot.localRotation = Quaternion.Euler(leftArmAngle, 0f, 0f);
                }
                if (_rightShoulderPivot != null)
                {
                    _rightShoulderPivot.localRotation = Quaternion.Euler(rightArmAngle, 0f, 0f);
                }

                // Torso vertical bounce and subtle forward lean
                if (_torsoTransform != null)
                {
                    float bob = Mathf.Abs(Mathf.Sin(_stridePhase)) * verticalBobAmount * (isRunning ? 1.4f : 1.0f);
                    float forwardLean = isRunning ? 10.0f : 2.5f;
                    float hipRoll = Mathf.Cos(_stridePhase) * 2.2f;

                    _torsoTransform.localPosition = _baseTorsoPos + new Vector3(0f, -bob, 0f);
                    _torsoTransform.localRotation = Quaternion.Euler(forwardLean, hipRoll, 0f);
                }
            }
            else
            {
                // Idle Breathing & Relaxation
                float breath = Mathf.Sin(Time.time * 2.2f);
                float breathBob = breath * 0.008f;

                if (_torsoTransform != null)
                {
                    _torsoTransform.localPosition = Vector3.Lerp(_torsoTransform.localPosition, _baseTorsoPos + new Vector3(0f, breathBob, 0f), Time.deltaTime * 5.0f);
                    _torsoTransform.localRotation = Quaternion.Slerp(_torsoTransform.localRotation, _baseTorsoRot, Time.deltaTime * 5.0f);
                }

                if (_leftHipPivot != null)
                {
                    _leftHipPivot.localRotation = Quaternion.Slerp(_leftHipPivot.localRotation, Quaternion.identity, Time.deltaTime * 6.0f);
                }
                if (_rightHipPivot != null)
                {
                    _rightHipPivot.localRotation = Quaternion.Slerp(_rightHipPivot.localRotation, Quaternion.identity, Time.deltaTime * 6.0f);
                }
                if (_leftShoulderPivot != null)
                {
                    _leftShoulderPivot.localRotation = Quaternion.Slerp(_leftShoulderPivot.localRotation, Quaternion.identity, Time.deltaTime * 6.0f);
                }
                if (_rightShoulderPivot != null)
                {
                    _rightShoulderPivot.localRotation = Quaternion.Slerp(_rightShoulderPivot.localRotation, Quaternion.identity, Time.deltaTime * 6.0f);
                }
            }
        }

        private void UpdateHeadLookAt()
        {
            if (!enableHeadLookAt || _headTransform == null) return;

            if (_playerTransform == null)
            {
                var playerGo = GameObject.FindGameObjectWithTag("Player");
                if (playerGo != null) _playerTransform = playerGo.transform;
                if (_playerTransform == null) return;
            }

            Vector3 toPlayer = _playerTransform.position - _headTransform.position;
            float distToPlayer = toPlayer.magnitude;

            if (distToPlayer <= lookAtDistance)
            {
                Vector3 localDir = transform.InverseTransformDirection(toPlayer);
                float angle = Vector3.Angle(Vector3.forward, localDir);

                if (angle <= maxLookAtAngle)
                {
                    Quaternion targetRot = Quaternion.LookRotation(localDir, Vector3.up);
                    _headTransform.localRotation = Quaternion.Slerp(_headTransform.localRotation, targetRot, Time.deltaTime * 4.0f);
                    return;
                }
            }

            // Return head to baseline forward orientation
            _headTransform.localRotation = Quaternion.Slerp(_headTransform.localRotation, _baseHeadRot, Time.deltaTime * 3.5f);
        }
    }
}
