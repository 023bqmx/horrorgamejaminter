using UnityEngine;
using UnityEngine.SceneManagement; // << เพิ่ม
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 4.0f;
        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 6.0f;
        [Tooltip("Rotation speed of the character")]
        public float RotationSpeed = 1.0f;
        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;
        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.1f;
        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;
        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;
        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.5f;
        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;
        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 90.0f;
        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -90.0f;

        // ==== Drunk / Wobble Effect ====
        [Header("Drunk Wobble")]
        [Tooltip("เปิด/ปิดเอฟเฟกต์เมา")]
        public bool DrunkEffectEnabled = true;

        [Tooltip("ความแรงโดยรวมของเอฟเฟกต์เมา")]
        [Range(0f, 2f)] public float DrunkIntensity = 1.0f;

        [Tooltip("ความถี่การแกว่ง (ยิ่งมากยิ่งไหวเร็ว)")]
        public float WobbleFrequency = 1.6f;

        [Tooltip("มุม Pitch/Yaw ที่ขยับเพิ่ม")]
        public float WobbleAngle = 2.0f;

        [Tooltip("มุมเอียงจอ (Roll)")]
        public float RollAngle = 3.0f;

        [Tooltip("Noise แบบสุ่มเล็กๆ")]
        public float NoiseAmplitude = 0.6f;
        public float NoiseSpeed = 1.2f;

        [Tooltip("ความลื่นไหลกลับสู่สภาพปกติ")]
        public float WobbleSmoothing = 10f;

        [Header("Drunk Scene Control")]
        [Tooltip("ถ้า true จะเปิดเมาเฉพาะ 'ซีนแรก' ที่เริ่มเกมนี้เท่านั้น ซีนอื่นจะปิดเมอัตโนมัติ")]
        public bool EnableDrunkOnlyInInitialScene = true;

        [Tooltip("รายชื่อซีนที่อนุญาตให้มีเอฟเฟกต์เมา (ถ้าใส่รายการนี้ จะใช้รายการนี้แทนโหมดซีนแรก)")]
        public string[] AllowedScenes;

        // cinemachine
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        // drunk cache
        private float _wobbleTimer;
        private Quaternion _targetLocalRot;
        private Quaternion _baseLocalRot;

        // scene cache
        private string _initialSceneName;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            if (_mainCamera == null)
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }

        private void Start()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#else
            Debug.LogError("Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            if (CinemachineCameraTarget != null)
            {
                _baseLocalRot = CinemachineCameraTarget.transform.localRotation;
                _targetLocalRot = _baseLocalRot;
            }

            // เก็บชื่อซีนแรก แล้วประเมินว่าจะให้เปิดเมาไหม
            _initialSceneName = SceneManager.GetActiveScene().name;
            EvaluateDrunkForScene(_initialSceneName);
        }

        private void Update()
        {
            JumpAndGravity();
            GroundedCheck();
            Move();
        }

        private void LateUpdate()
        {
            CameraRotation();
            ApplyDrunkWobble(); // จะทำงานหรือไม่ ขึ้นกับ DrunkEffectEnabled (ถูกประเมินจากซีน)
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
        }

        private void CameraRotation()
        {
            if (_input.look.sqrMagnitude >= _threshold)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
                _rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

                _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

                // ไม่ตั้ง localRotation ตรงนี้ ให้ ApplyDrunkWobble() จัดการ
                transform.Rotate(Vector3.up * _rotationVelocity);
            }
        }

        private void Move()
        {
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            if (_input.move != Vector2.zero)
            {
                inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
            }

            _controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;

                if (_verticalVelocity < 0.0f)
                    _verticalVelocity = -2f;

                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                if (_jumpTimeoutDelta >= 0.0f)
                    _jumpTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0.0f)
                    _fallTimeoutDelta -= Time.deltaTime;

                _input.jump = false;
            }

            if (_verticalVelocity < _terminalVelocity)
                _verticalVelocity += Gravity * Time.deltaTime;
        }

        // ===== Drunk wobble implementation =====
        private void ApplyDrunkWobble()
        {
            if (CinemachineCameraTarget == null)
                return;

            if (!DrunkEffectEnabled)
            {
                // ไม่มีเอฟเฟกต์: ไล่กลับสู่ pitch ปกติ
                Quaternion target = Quaternion.Euler(_cinemachineTargetPitch, 0f, 0f);
                CinemachineCameraTarget.transform.localRotation =
                    Quaternion.Slerp(CinemachineCameraTarget.transform.localRotation, target, Time.deltaTime * WobbleSmoothing);
                return;
            }

            float move01 = Mathf.Clamp01(_speed / Mathf.Max(0.0001f, SprintSpeed)) * DrunkIntensity;

            if (move01 <= 0.001f)
            {
                Quaternion target = Quaternion.Euler(_cinemachineTargetPitch, 0f, 0f);
                CinemachineCameraTarget.transform.localRotation =
                    Quaternion.Slerp(CinemachineCameraTarget.transform.localRotation, target, Time.deltaTime * WobbleSmoothing);
                return;
            }

            _wobbleTimer += Time.deltaTime * (WobbleFrequency * (0.6f + 0.4f * move01));

            float pitchOffset = Mathf.Sin(_wobbleTimer * 2f) * WobbleAngle * move01;
            float yawOffset   = Mathf.Sin(_wobbleTimer * 2f + Mathf.PI * 0.5f) * (WobbleAngle * 0.6f) * move01;
            float rollOffset  = Mathf.Sin(_wobbleTimer) * RollAngle * move01;

            float n1 = (Mathf.PerlinNoise(Time.time * NoiseSpeed, 10.123f) - 0.5f) * 2f * NoiseAmplitude * move01;
            float n2 = (Mathf.PerlinNoise(3.33f, Time.time * NoiseSpeed) - 0.5f) * 2f * NoiseAmplitude * move01;

            float finalPitch = _cinemachineTargetPitch + pitchOffset + n1;
            float finalYaw   = yawOffset + n2;
            float finalRoll  = rollOffset;

            _targetLocalRot = Quaternion.Euler(finalPitch, finalYaw, finalRoll);

            CinemachineCameraTarget.transform.localRotation =
                Quaternion.Slerp(CinemachineCameraTarget.transform.localRotation, _targetLocalRot, Time.deltaTime * WobbleSmoothing);
        }

        // ===== Scene gating for drunk effect =====
        private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            EvaluateDrunkForScene(newScene.name);
        }

        private void EvaluateDrunkForScene(string sceneName)
        {
            // ถ้าระบุ AllowedScenes จะใช้ whitelist นี้
            if (AllowedScenes != null && AllowedScenes.Length > 0)
            {
                bool allowed = System.Array.Exists(AllowedScenes, s => s == sceneName);
                DrunkEffectEnabled = allowed;
                return;
            }

            // ไม่ได้ระบุ whitelist: ใช้โหมด "เฉพาะซีนแรก"
            if (EnableDrunkOnlyInInitialScene)
            {
                DrunkEffectEnabled = sceneName == _initialSceneName;
            }
            // else: คงค่าที่ตั้งไว้ใน Inspector (ไม่บังคับปิด/เปิด)
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            Gizmos.color = Grounded ? transparentGreen : transparentRed;

            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
        }
    }
}
