// FlashlightBatteryUI.cs
// Shows while flashlight is ON, hides while OFF, with smooth fade/scale.

using UnityEngine;
using UnityEngine.UI;

public class FlashlightBatteryUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] FlashlightController flashlight;   // can be set at runtime via SetFlashlight()
    [SerializeField] Slider slider;
    [SerializeField] Image  fillImage;

    [Header("Auto Show/Hide")]
    [SerializeField] bool autoShowHide = true;
    [SerializeField] bool hideWhenNoFlashlight = true;
    [SerializeField] bool requireBatteryToShow = false;   // if true, hides when battery = 0

    [Header("Transition")]
    [SerializeField, Min(0.01f)] float fadeInDuration  = 0.18f;
    [SerializeField, Min(0.01f)] float fadeOutDuration = 0.18f;
    [SerializeField] AnimationCurve fadeCurve = null;     // default ease in/out
    [SerializeField] bool scalePop = true;
    [SerializeField, Min(0f)] float popScale = 1.03f;     // slight scale on show
    [SerializeField, Min(0.01f)] float popDuration = 0.12f;
    [SerializeField] bool useUnscaledTime = true;

    CanvasGroup _cg;
    RectTransform _rt;
    bool _visible;                // current visibility state
    bool _pendingShow;            // desired visibility
    float _vel;                   // for SmoothDamp (alpha)
    float _targetAlpha;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _cg = GetComponent<CanvasGroup>();
        if (!_cg) _cg = gameObject.AddComponent<CanvasGroup>();
        if (fadeCurve == null) fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        // start hidden until we evaluate state in OnEnable/Update
        _cg.alpha = 0f;
        _cg.interactable = false;
        _cg.blocksRaycasts = false;
        _visible = false;
        _targetAlpha = 0f;
    }

    void OnEnable()
    {
        if (!flashlight)
            flashlight = FindFirstObjectByType<FlashlightController>(FindObjectsInactive.Exclude);

        if (flashlight)
        {
            flashlight.onBatteryChanged.AddListener(OnBattery); // update bar on changes
            OnBattery(flashlight.BatteryPercent);               // init display 0..1  :contentReference[oaicite:2]{index=2}
        }

        // evaluate initial visibility immediately
        EvaluateDesiredVisibility(force:true);
    }

    void OnDisable()
    {
        if (flashlight) flashlight.onBatteryChanged.RemoveListener(OnBattery);
    }

    void Update()
    {
        if (autoShowHide) EvaluateDesiredVisibility(force:false);
        TickFade();
    }

    // ---------- Visibility logic ----------
    void EvaluateDesiredVisibility(bool force)
    {
        bool want =
            flashlight != null &&
            (!hideWhenNoFlashlight || flashlight) &&
            (!requireBatteryToShow || flashlight.BatteryPercent > 0f) && // optional battery check
            flashlight.IsOn;                                              // show only when ON  :contentReference[oaicite:3]{index=3}

        if (force || want != _pendingShow)
        {
            _pendingShow = want;
            StartFade(_pendingShow);
        }
    }

    void StartFade(bool show)
    {
        float dur = show ? fadeInDuration : fadeOutDuration;

        // reset smoothing
        _vel = 0f;
        _targetAlpha = show ? 1f : 0f;

        // scale pop on show
        if (scalePop && _rt)
            StartCoroutine(ScaleRoutine(show ? popScale : 1f, show ? popDuration : popDuration * 0.75f));
    }

    void TickFade()
    {
        // Exponential-ish smoothing using SmoothDamp on alpha, then remap with curve
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float desired = _targetAlpha;
        float current = _cg.alpha;

        // smooth towards target
        float smoothed = Mathf.SmoothDamp(current, desired, ref _vel,
            (_targetAlpha > current ? fadeInDuration : fadeOutDuration) * 0.5f,
            Mathf.Infinity, dt);

        // optional curve shaping
        float a = fadeCurve.Evaluate((_targetAlpha <= 0f) ? (1f - smoothed) : smoothed);
        _cg.alpha = (_targetAlpha <= 0f) ? (1f - a) : a;

        bool nowVisible = _cg.alpha > 0.001f;
        if (nowVisible != _visible)
        {
            _visible = nowVisible;
            _cg.blocksRaycasts = _visible;
            _cg.interactable   = _visible;
        }
    }

    System.Collections.IEnumerator ScaleRoutine(float toScale, float dur)
    {
        if (!_rt || dur <= 0f) yield break;
        Vector3 from = _rt.localScale;
        Vector3 to   = Vector3.one * toScale;
        float t = 0f;
        while (t < 1f)
        {
            t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) / dur;
            float e = Mathf.SmoothStep(0f, 1f, t);
            _rt.localScale = Vector3.LerpUnclamped(from, to, e);
            yield return null;
        }
        // settle back to 1x if we popped up
        if (toScale != 1f)
        {
            t = 0f; from = _rt.localScale; to = Vector3.one;
            while (t < 1f)
            {
                t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) / (dur * 0.75f);
                float e = Mathf.SmoothStep(0f, 1f, t);
                _rt.localScale = Vector3.LerpUnclamped(from, to, e);
                yield return null;
            }
        }
    }

    // ---------- Battery bar ----------
    void OnBattery(float percent)
    {
        if (slider)    slider.value = percent;
        if (fillImage) fillImage.fillAmount = percent; // keeps your original UI behaviour  :contentReference[oaicite:4]{index=4}
    }

    // ---------- Public: set from your binder after spawning flashlight ----------
    public void SetFlashlight(FlashlightController f)
    {
        // unhook old
        if (isActiveAndEnabled && flashlight)
            flashlight.onBatteryChanged.RemoveListener(OnBattery);

        flashlight = f;

        if (isActiveAndEnabled && flashlight)
        {
            flashlight.onBatteryChanged.AddListener(OnBattery);
            OnBattery(flashlight.BatteryPercent);
        }

        EvaluateDesiredVisibility(force:true);
    }
}
