using UnityEngine;

namespace TomorrowsCrime.Player
{
    /// <summary>
    /// Procedural first-person detective viewmodel animator for Tomorrow's Crime.
    /// Provides lifelike first-person detective hands with walking, running, idle breathing,
    /// jump anticipation, landing impact compression, mouse look inertia sway, and sprint FOV scaling.
    /// </summary>
    public class PlayerViewmodelAnimator : MonoBehaviour
    {
        [Header("Viewmodel Rig")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Material coatMaterial;
        [SerializeField] private Material cuffMaterial;
        [SerializeField] private Material skinMaterial;
        [SerializeField] private Material brassMaterial;

        [Header("Breathing & Idle Sway")]
        [SerializeField] private float breatheFrequency = 1.8f;
        [SerializeField] private float breatheVerticalAmount = 0.008f;
        [SerializeField] private float breatheRotationalAmount = 0.4f;

        [Header("Walking Animation")]
        [SerializeField] private float walkBobFrequency = 8.5f;
        [SerializeField] private float walkBobHorizontal = 0.024f;
        [SerializeField] private float walkBobVertical = 0.020f;
        [SerializeField] private float walkArmPitch = 4.5f;

        [Header("Running / Sprinting Animation")]
        [SerializeField] private float runBobFrequency = 12.5f;
        [SerializeField] private float runBobHorizontal = 0.045f;
        [SerializeField] private float runBobVertical = 0.038f;
        [SerializeField] private float runArmPitch = 14.0f;
        [SerializeField] private float sprintFov = 74.0f;
        [SerializeField] private float normalFov = 60.0f;

        [Header("Mouse Look Inertia Sway")]
        [SerializeField] private float swayAmount = 0.025f;
        [SerializeField] private float maxSway = 0.06f;
        [SerializeField] private float swaySmooth = 8.0f;

        private GameObject _viewmodelRoot;
        private Transform _leftArm;
        private Transform _rightArm;
        private Vector3 _baseViewmodelPos = new Vector3(0f, -0.32f, 0.48f);
        private Vector3 _currentViewmodelPos;
        private Quaternion _currentViewmodelRot = Quaternion.identity;

        private Camera _cam;
        private float _animTimer = 0f;
        private float _landingDip = 0f;
        private float _landingDipVelocity = 0f;

        private void Awake()
        {
            if (cameraTransform == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) cameraTransform = cam.transform;
            }

            if (cameraTransform != null)
            {
                _cam = cameraTransform.GetComponent<Camera>();
                BuildDetectiveHandsViewmodel();
            }

            _currentViewmodelPos = _baseViewmodelPos;
        }

        private void BuildDetectiveHandsViewmodel()
        {
            if (cameraTransform == null) return;

            // Load materials if not assigned
            if (coatMaterial == null) coatMaterial = Resources.Load<Material>("M_UniformNavy");
            if (cuffMaterial == null) cuffMaterial = Resources.Load<Material>("M_ShirtWhite");
            if (skinMaterial == null) skinMaterial = Resources.Load<Material>("M_SkinStylized");
            if (brassMaterial == null) brassMaterial = Resources.Load<Material>("M_BrassGold");

#if UNITY_EDITOR
            if (coatMaterial == null) coatMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_UniformNavy.mat");
            if (cuffMaterial == null) cuffMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_ShirtWhite.mat");
            if (skinMaterial == null) skinMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_SkinStylized.mat");
            if (brassMaterial == null) brassMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_BrassGold.mat");
#endif

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (coatMaterial == null) coatMaterial = new Material(shader) { color = new Color(0.10f, 0.15f, 0.25f) };
            if (cuffMaterial == null) cuffMaterial = new Material(shader) { color = new Color(0.97f, 0.97f, 0.96f) };
            if (skinMaterial == null) skinMaterial = new Material(shader) { color = new Color(0.92f, 0.78f, 0.70f) };
            if (brassMaterial == null) brassMaterial = new Material(shader) { color = new Color(0.88f, 0.74f, 0.28f) };

            _viewmodelRoot = new GameObject("Detective_Hands_Viewmodel");
            _viewmodelRoot.transform.SetParent(cameraTransform, false);
            _viewmodelRoot.transform.localPosition = _baseViewmodelPos;
            _viewmodelRoot.transform.localRotation = Quaternion.identity;

            // Left Detective Arm
            _leftArm = BuildArm(true, _viewmodelRoot.transform);
            // Right Detective Arm
            _rightArm = BuildArm(false, _viewmodelRoot.transform);
        }

        private Transform BuildArm(bool isLeft, Transform parent)
        {
            float side = isLeft ? -1f : 1f;
            var armRoot = new GameObject(isLeft ? "Arm_Left" : "Arm_Right");
            armRoot.transform.SetParent(parent, false);
            // Arm root positioned naturally at detective's shoulder joint
            armRoot.transform.localPosition = new Vector3(side * 0.28f, -0.12f, 0.02f);
            armRoot.transform.localRotation = Quaternion.Euler(12f, side * 10f, side * -8f);

            // 0. Upper Arm & Shoulder Sleeve (Extends deep off-screen so arm never floats or shows open ends)
            var upperArm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            upperArm.name = "UpperArm_Shoulder";
            upperArm.transform.SetParent(armRoot.transform, false);
            upperArm.transform.localPosition = new Vector3(side * 0.06f, -0.20f, -0.16f);
            upperArm.transform.localRotation = Quaternion.Euler(42f, side * -12f, side * 18f);
            upperArm.transform.localScale = new Vector3(0.115f, 0.28f, 0.115f);
            DestroyCollider(upperArm);
            if (coatMaterial != null) upperArm.GetComponent<Renderer>().sharedMaterial = coatMaterial;

            // 0b. Elbow Joint Cover (Seamless transition between upper arm and forearm)
            var elbow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            elbow.name = "Elbow_Joint";
            elbow.transform.SetParent(armRoot.transform, false);
            elbow.transform.localPosition = new Vector3(side * 0.02f, -0.06f, -0.02f);
            elbow.transform.localScale = new Vector3(0.125f, 0.125f, 0.125f);
            DestroyCollider(elbow);
            if (coatMaterial != null) elbow.GetComponent<Renderer>().sharedMaterial = coatMaterial;

            // 1. Forearm Sleeve (Royal Wool Overcoat)
            var sleeve = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            sleeve.name = "Forearm_Sleeve";
            sleeve.transform.SetParent(armRoot.transform, false);
            sleeve.transform.localPosition = new Vector3(0f, 0.04f, 0.14f);
            sleeve.transform.localRotation = Quaternion.Euler(72f, 0f, 0f);
            sleeve.transform.localScale = new Vector3(0.10f, 0.22f, 0.10f);
            DestroyCollider(sleeve);
            if (coatMaterial != null) sleeve.GetComponent<Renderer>().sharedMaterial = coatMaterial;

            // 2. French Shirt Cuff
            var cuff = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cuff.name = "Shirt_Cuff";
            cuff.transform.SetParent(armRoot.transform, false);
            cuff.transform.localPosition = new Vector3(0f, 0.06f, 0.28f);
            cuff.transform.localRotation = Quaternion.Euler(75f, 0f, 0f);
            cuff.transform.localScale = new Vector3(0.088f, 0.025f, 0.088f);
            DestroyCollider(cuff);
            if (cuffMaterial != null) cuff.GetComponent<Renderer>().sharedMaterial = cuffMaterial;

            // 3. Brass Gold Cufflink
            var cufflink = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cufflink.name = "Brass_Cufflink";
            cufflink.transform.SetParent(cuff.transform, false);
            cufflink.transform.localPosition = new Vector3(side * 0.48f, 0f, 0f);
            cufflink.transform.localScale = new Vector3(0.25f, 0.35f, 0.25f);
            DestroyCollider(cufflink);
            if (brassMaterial != null) cufflink.GetComponent<Renderer>().sharedMaterial = brassMaterial;

            // 4. Detective Palm & Knuckles
            var hand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hand.name = "Hand_Palm";
            hand.transform.SetParent(armRoot.transform, false);
            hand.transform.localPosition = new Vector3(0f, 0.08f, 0.34f);
            hand.transform.localRotation = Quaternion.Euler(20f, side * 5f, 0f);
            hand.transform.localScale = new Vector3(0.075f, 0.038f, 0.085f);
            DestroyCollider(hand);
            if (skinMaterial != null) hand.GetComponent<Renderer>().sharedMaterial = skinMaterial;

            // 5. Fingers
            for (int i = 0; i < 4; i++)
            {
                var finger = GameObject.CreatePrimitive(PrimitiveType.Cube);
                finger.name = $"Finger_{i}";
                finger.transform.SetParent(hand.transform, false);
                float fx = -0.36f + (i * 0.24f);
                finger.transform.localPosition = new Vector3(fx, 0f, 0.55f);
                finger.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
                finger.transform.localScale = new Vector3(0.20f, 0.65f, 0.42f);
                DestroyCollider(finger);
                if (skinMaterial != null) finger.GetComponent<Renderer>().sharedMaterial = skinMaterial;
            }

            // 6. Thumb
            var thumb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            thumb.name = "Thumb";
            thumb.transform.SetParent(hand.transform, false);
            thumb.transform.localPosition = new Vector3(side * -0.52f, 0.15f, 0.12f);
            thumb.transform.localRotation = Quaternion.Euler(15f, side * -35f, side * 25f);
            thumb.transform.localScale = new Vector3(0.25f, 0.60f, 0.38f);
            DestroyCollider(thumb);
            if (skinMaterial != null) thumb.GetComponent<Renderer>().sharedMaterial = skinMaterial;

            return armRoot.transform;
        }

        private void DestroyCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
        }

        public void UpdateViewmodel(bool isGrounded, bool isMoving, bool isSprinting, bool isCrouching, float mouseX, float mouseY)
        {
            if (_viewmodelRoot == null) return;

            // 1. Mouse Look Inertia Sway
            float swayX = Mathf.Clamp(-mouseX * swayAmount, -maxSway, maxSway);
            float swayY = Mathf.Clamp(-mouseY * swayAmount, -maxSway, maxSway);
            Vector3 targetSwayPos = _baseViewmodelPos + new Vector3(swayX, swayY, 0f);

            // 2. Landing Spring Damping
            _landingDip = Mathf.SmoothDamp(_landingDip, 0f, ref _landingDipVelocity, 0.18f);

            // 3. Locomotion Bobbing & Sway
            Vector3 bobPos = Vector3.zero;
            Quaternion bobRot = Quaternion.identity;

            if (isMoving && isGrounded)
            {
                float freq = isSprinting ? runBobFrequency : walkBobFrequency;
                float ampH = isSprinting ? runBobHorizontal : walkBobHorizontal;
                float ampV = isSprinting ? runBobVertical : walkBobVertical;
                float pitch = isSprinting ? runArmPitch : walkArmPitch;

                _animTimer += Time.deltaTime * freq;

                float bx = Mathf.Cos(_animTimer * 0.5f) * ampH;
                float by = Mathf.Sin(_animTimer) * ampV;

                bobPos = new Vector3(bx, by, 0f);
                bobRot = Quaternion.Euler(Mathf.Sin(_animTimer) * pitch, bx * 25f, -bx * 40f);

                // Individual arm swing pump
                float armPump = Mathf.Sin(_animTimer) * (isSprinting ? 12f : 5f);
                if (_leftArm != null) _leftArm.localRotation = Quaternion.Euler(15f + armPump, -12f, 10f);
                if (_rightArm != null) _rightArm.localRotation = Quaternion.Euler(15f - armPump, 12f, -10f);
            }
            else
            {
                // Idle Breathing
                _animTimer = 0f;
                float breath = Mathf.Sin(Time.time * breatheFrequency);
                bobPos = new Vector3(0f, breath * breatheVerticalAmount, 0f);
                bobRot = Quaternion.Euler(breath * breatheRotationalAmount, 0f, 0f);

                if (_leftArm != null) _leftArm.localRotation = Quaternion.Slerp(_leftArm.localRotation, Quaternion.Euler(15f, -12f, 10f), Time.deltaTime * 6f);
                if (_rightArm != null) _rightArm.localRotation = Quaternion.Slerp(_rightArm.localRotation, Quaternion.Euler(15f, 12f, -10f), Time.deltaTime * 6f);
            }

            // Apply Landing Compression Dip
            bobPos.y += _landingDip;

            // Smooth Interpolation
            Vector3 finalTargetPos = targetSwayPos + bobPos;
            if (isCrouching) finalTargetPos.y -= 0.04f;

            _currentViewmodelPos = Vector3.Lerp(_currentViewmodelPos, finalTargetPos, Time.deltaTime * swaySmooth);
            _currentViewmodelRot = Quaternion.Slerp(_currentViewmodelRot, bobRot, Time.deltaTime * swaySmooth);

            _viewmodelRoot.transform.localPosition = _currentViewmodelPos;
            _viewmodelRoot.transform.localRotation = _currentViewmodelRot;

            // 4. Smooth Sprint FOV Transition
            if (_cam != null)
            {
                float targetFov = isSprinting ? sprintFov : normalFov;
                _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFov, Time.deltaTime * 5f);
            }
        }

        public void TriggerLandingImpact(float impactVelocity)
        {
            // Compressive shockwave dip
            _landingDip = -Mathf.Clamp(Mathf.Abs(impactVelocity) * 0.012f, 0.03f, 0.09f);
        }
    }
}
