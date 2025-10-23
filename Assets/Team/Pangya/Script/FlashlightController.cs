// FlashlightController.cs
// Unity 6 / Input System
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class FlashlightController : MonoBehaviour
{
    [Header("Light")]
    [SerializeField] Light flashlight;                    // Auto-found if left empty (child Light)
    [SerializeField] bool startOn = false;
    [SerializeField, Min(0f)] float maxIntensity = 2f;    // Peak intensity at 100% battery
    [SerializeField, Min(0f)] float onOffLerpSpeed = 16f; // Smooth fade on/off

    // ---------- BATTERY (ADVANCED) ----------
    [Header("Battery (advanced)")]
    [SerializeField] bool useBattery = true;

    [Tooltip("Total capacity in seconds at 1.0x drain. (Initial and max capacity)")]
    [SerializeField, Min(1f)] float batterySeconds = 300f;

    [Tooltip("Base drain while ON (seconds per real second). 1 = old behavior.")]
    [SerializeField, Min(0f)] float baseDrainPerSecond = 1f;

    [Tooltip("Extra drain scales with current intensity / maxIntensity.")]
    [SerializeField, Min(0f)] float intensityDrainMultiplier = 1f;

    [Tooltip("Very small drain while OFF (for realism). 0 = no idle drain.")]
    [SerializeField, Min(0f)] float idleDrainPerSecond = 0f;

    [Tooltip("Maps battery % (0..1) to intensity multiplier (0..1).")]
    [SerializeField] AnimationCurve batteryDimCurve = AnimationCurve.Linear(0, 0.25f, 1, 1);

    [Tooltip("When battery % <= this, trigger low-battery events and optional flicker.")]
    [SerializeField, Range(0f,1f)] float lowBatteryThreshold = 0.1f;

    [Tooltip("Low battery flicker speed (0 = disable flicker).")]
    [SerializeField, Min(0f)] float lowBatteryFlickerHz = 6f;

    [Tooltip("Prevent turning on when empty; plays optional 'empty' click.")]
    [SerializeField] bool blockOnWhenEmpty = true;

    [Header("Battery Events")]
    public UnityEvent<float> onBatteryChanged;     // 0..1
    public UnityEvent onBatteryBecameLow;          // fires once when crossing low threshold
    public UnityEvent onBatteryEmptied;            // fires once when hits 0

    // ---------- INPUT ----------
#if ENABLE_INPUT_SYSTEM
    [Header("Input (Input System)")]
    [Tooltip("Action should be a Button. Example bindings: Keyboard F, Gamepad North.")]
    [SerializeField] InputActionReference toggleAction;
    [Tooltip("Optional: hold to shine only while pressed. Leave null to use toggle.")]
    [SerializeField] InputActionReference holdAction;
#endif

    // ---------- AUDIO ----------
    [Header("Audio (optional)")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip clickOn;
    [SerializeField] AudioClip clickOff;
    [SerializeField] AudioClip clickEmpty; // played when trying to turn on with empty battery

    // ---------- AIM (SMOOTH) ----------
    [Header("Aim")]
    [Tooltip("Camera used for aiming. If empty, uses Camera.main (Cinemachine output).")]
    [SerializeField] Camera aimCamera;

    [Tooltip("Transform to rotate for aiming. If empty, uses the Light's transform.")]
    [SerializeField] Transform rotateThis;

    [Tooltip("If true, raycast from cursor/screen-center; if false, just match camera forward.")]
    [SerializeField] bool useCursorRay = true;

    [Tooltip("Max distance for aim ray when no collider is hit.")]
    [SerializeField, Min(0.1f)] float maxAimDistance = 100f;

    [Tooltip("Layers considered for aiming raycasts.")]
    [SerializeField] LayerMask aimLayers = ~0;

    [Header("Aim Smoothing")]
    [Tooltip("Time (seconds) to smooth the aim point; higher = smoother/laggier.")]
    [SerializeField, Min(0f)] float aimSmoothTime = 0.08f;

    [Tooltip("Max angular speed for the flashlight rotation (deg/sec).")]
    [SerializeField, Min(0f)] float aimMaxDegreesPerSecond = 540f;

    [Tooltip("Minimum distance to aim at to avoid jitter on very close hits.")]
    [SerializeField, Min(0.01f)] float minAimDistance = 0.5f;

    // ---------- STATE ----------
    float _battery;           // current charge in seconds
    bool  _isOn;
    float _targetIntensity;

    // smoothing state
    Vector3 _smoothedAimPoint;
    Vector3 _aimPointVel;
    bool _hasAimPoint;

    // one-shot event latches
    bool _lowInvoked;
    bool _emptyInvoked;

    public bool IsOn => _isOn;
    public float BatteryPercent => useBattery ? Mathf.Clamp01(_battery / Mathf.Max(1f, batterySeconds)) : 1f;
    public float BatterySeconds => _battery;
    public float BatteryCapacitySeconds => batterySeconds;

    void Awake()
    {
        if (!flashlight) flashlight = GetComponentInChildren<Light>(true);
        if (!rotateThis) rotateThis = flashlight ? flashlight.transform : transform;
        if (flashlight) flashlight.enabled = true; // we drive intensity smoothly
        _battery = Mathf.Clamp(batterySeconds, 0f, Mathf.Max(1f, batterySeconds));

        SetOnImmediate(startOn);

        _smoothedAimPoint = rotateThis.position + rotateThis.forward * Mathf.Max(2f, minAimDistance);
        _hasAimPoint = true;
    }

    void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        if (toggleAction) {
            toggleAction.action.performed += OnTogglePerformed;
            toggleAction.action.Enable();
        }
        if (holdAction) {
            holdAction.action.performed += OnHoldPerformed;
            holdAction.action.canceled  += OnHoldCanceled;
            holdAction.action.Enable();
        }
#endif
        onBatteryChanged?.Invoke(BatteryPercent);
    }

    void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        if (toggleAction) { toggleAction.action.performed -= OnTogglePerformed; toggleAction.action.Disable(); }
        if (holdAction)   { holdAction.action.performed -= OnHoldPerformed;     holdAction.action.canceled -= OnHoldCanceled; holdAction.action.Disable(); }
#endif
    }

    void Update()
    {
        // ---- BATTERY DRAIN ----
        if (useBattery)
        {
            float drain = 0f;

            if (_isOn && _battery > 0f)
            {
                // scale by current (or desired) intensity
                float normIntensity = (flashlight ? flashlight.intensity : (_isOn ? maxIntensity : 0f)) / Mathf.Max(0.0001f, maxIntensity);
                drain = (baseDrainPerSecond + normIntensity * intensityDrainMultiplier) * Time.deltaTime;
            }
            else if (!_isOn && idleDrainPerSecond > 0f)
            {
                drain = idleDrainPerSecond * Time.deltaTime;
            }

            if (drain > 0f)
            {
                float oldPercent = BatteryPercent;
                _battery = Mathf.Max(0f, _battery - drain);
                float newPercent = BatteryPercent;

                if (Mathf.Abs(newPercent - oldPercent) > 0.0001f)
                    onBatteryChanged?.Invoke(newPercent);

                if (!_lowInvoked && newPercent <= lowBatteryThreshold)
                {
                    _lowInvoked = true;
                    onBatteryBecameLow?.Invoke();
                }

                if (!_emptyInvoked && _battery <= 0f)
                {
                    _emptyInvoked = true;
                    onBatteryEmptied?.Invoke();
                    if (_isOn) SetOn(false); // force-off when empty
                }
            }
        }

        // ---- LIGHT OUTPUT ----
        float batteryMul = useBattery ? batteryDimCurve.Evaluate(BatteryPercent) : 1f;
        float desired = _isOn ? maxIntensity * batteryMul : 0f;

        // Optional low-battery flicker
        if (_isOn && useBattery && BatteryPercent <= lowBatteryThreshold && _battery > 0f && lowBatteryFlickerHz > 0f)
        {
            float flicker = Mathf.PerlinNoise(0f, Time.time * lowBatteryFlickerHz);
            desired *= Mathf.Lerp(0.7f, 1.0f, flicker); // subtle jitter
        }

        _targetIntensity = desired;

        if (flashlight)
        {
            flashlight.intensity = Mathf.Lerp(
                flashlight.intensity,
                _targetIntensity,
                1f - Mathf.Exp(-onOffLerpSpeed * Time.deltaTime)
            );
            flashlight.enabled = flashlight.intensity > 0.01f;
        }
    }

    void LateUpdate() => AimUpdate();

    // ---------- PUBLIC API ----------
    public void SetOn(bool on)
    {
        if (on && useBattery && blockOnWhenEmpty && _battery <= 0f)
        {
            PlayClip(clickEmpty);
            return;
        }
        if (_isOn == on) return;
        _isOn = on;
        PlayClip(on ? clickOn : clickOff);
        // reset latches if recharged above thresholds later
        if (BatteryPercent > lowBatteryThreshold) _lowInvoked = false;
        if (_battery > 0f) _emptyInvoked = false;
    }

    public void Toggle() => SetOn(!_isOn);

    /// <summary>Adds charge (seconds). Clamped to capacity.</summary>
    public void AddChargeSeconds(float seconds)
    {
        if (!useBattery || seconds <= 0f) return;
        float before = BatteryPercent;
        _battery = Mathf.Clamp(_battery + seconds, 0f, batterySeconds);
        float after = BatteryPercent;
        if (Mathf.Abs(after - before) > 0.0001f) onBatteryChanged?.Invoke(after);
        if (after > lowBatteryThreshold) _lowInvoked = false;
        if (_battery > 0f) _emptyInvoked = false;
    }

    /// <summary>Set battery to full capacity.</summary>
    public void RechargeFull()
    {
        if (!useBattery) return;
        bool wasEmpty = _battery <= 0f;
        _battery = batterySeconds;
        onBatteryChanged?.Invoke(BatteryPercent);
        if (BatteryPercent > lowBatteryThreshold) _lowInvoked = false;
        if (wasEmpty && _battery > 0f) _emptyInvoked = false;
    }

    /// <summary>Replace the battery with a new capacity and (optionally) full charge.</summary>
    public void ReplaceBattery(float newCapacitySeconds, bool fillToFull = true)
    {
        batterySeconds = Mathf.Max(1f, newCapacitySeconds);
        _battery = Mathf.Clamp(fillToFull ? batterySeconds : _battery, 0f, batterySeconds);
        onBatteryChanged?.Invoke(BatteryPercent);
        _lowInvoked = BatteryPercent <= lowBatteryThreshold;
        _emptyInvoked = _battery <= 0f;
    }

    // ---------- INPUT ----------
#if ENABLE_INPUT_SYSTEM
    void OnTogglePerformed(InputAction.CallbackContext _) { if (!holdAction) Toggle(); }
    void OnHoldPerformed (InputAction.CallbackContext _) => SetOn(true);
    void OnHoldCanceled  (InputAction.CallbackContext _) => SetOn(false);
#endif

    // ---------- INTERNALS ----------
    void SetOnImmediate(bool on)
    {
        _isOn = on && (!useBattery || !blockOnWhenEmpty || _battery > 0f);
        if (flashlight)
        {
            flashlight.intensity = _isOn ? maxIntensity : 0f;
            flashlight.enabled = _isOn;
        }
        onBatteryChanged?.Invoke(BatteryPercent);
    }

    void PlayClip(AudioClip clip)
    {
        if (!audioSource || !clip) return;
        audioSource.PlayOneShot(clip);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        maxIntensity = Mathf.Max(0f, maxIntensity);
        onOffLerpSpeed = Mathf.Max(0f, onOffLerpSpeed);
        batterySeconds = Mathf.Max(1f, batterySeconds);
        baseDrainPerSecond = Mathf.Max(0f, baseDrainPerSecond);
        intensityDrainMultiplier = Mathf.Max(0f, intensityDrainMultiplier);
        idleDrainPerSecond = Mathf.Max(0f, idleDrainPerSecond);
        lowBatteryFlickerHz = Mathf.Max(0f, lowBatteryFlickerHz);
        aimSmoothTime = Mathf.Max(0f, aimSmoothTime);
        aimMaxDegreesPerSecond = Mathf.Max(0f, aimMaxDegreesPerSecond);
        minAimDistance = Mathf.Max(0.01f, minAimDistance);
    }
#endif

    // ---------- AIM (SMOOTH) ----------
    void AimUpdate()
    {
        if (!rotateThis) return;

        if (!aimCamera)
        {
            aimCamera = Camera.main; // Cinemachine Brain output
            if (!aimCamera) return;
        }

        Vector3 origin = rotateThis.position;

        // Build aim ray (cursor or center)
        Vector2 screenPos;
#if ENABLE_INPUT_SYSTEM
        if (Cursor.lockState == CursorLockMode.Locked)
            screenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        else
            screenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
#else
        screenPos = (Cursor.lockState == CursorLockMode.Locked)
            ? new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
            : Input.mousePosition;
#endif
        Ray ray = useCursorRay ? aimCamera.ScreenPointToRay(screenPos)
                               : new Ray(aimCamera.transform.position, aimCamera.transform.forward);

        // Target point (hit or forward)
        Vector3 targetPoint;
        if (Physics.Raycast(ray, out var hit, maxAimDistance, aimLayers, QueryTriggerInteraction.Ignore))
        {
            float dist = Mathf.Max(hit.distance, minAimDistance);
            targetPoint = ray.origin + ray.direction * dist;
        }
        else
        {
            targetPoint = ray.origin + ray.direction * Mathf.Max(maxAimDistance * 0.5f, minAimDistance);
        }

        // Smooth the target point
        if (!_hasAimPoint) { _smoothedAimPoint = targetPoint; _hasAimPoint = true; }
        _smoothedAimPoint = Vector3.SmoothDamp(_smoothedAimPoint, targetPoint, ref _aimPointVel, aimSmoothTime);

        Vector3 dir = _smoothedAimPoint - origin;
        if (dir.sqrMagnitude < 1e-6f) return;
        dir.Normalize();

        // Cap angular speed (deg/sec)
        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        float step = aimMaxDegreesPerSecond * Time.deltaTime;
        rotateThis.rotation = Quaternion.RotateTowards(rotateThis.rotation, targetRot, step);
    }
}
