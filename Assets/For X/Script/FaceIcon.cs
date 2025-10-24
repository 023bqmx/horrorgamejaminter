using UnityEngine;
using UnityEngine.UI;

public class FaceIcon : MonoBehaviour
{
    [Header("Signals (from SmileGate)")]
    [SerializeField] private SmileGateByMouthWideAuto smileGate; // isSmiling, isCoolingDown, charge01
    [SerializeField] private OpenSeeTrackingHealth face;         // isTracking

    [Header("Icons")]
    [SerializeField] private GameObject normalFace;
    [SerializeField] private GameObject smileFace;
    [SerializeField] private GameObject trackingLost;

    [Header("Vertical Fill (bottom->top)")]
    [SerializeField] private Image normalFill; // child Image ของ normalFace (Type=Filled Vertical Bottom)
    [SerializeField] private Image smileFill;  // child Image ของ smileFace  (Type=Filled Vertical Bottom)
    [SerializeField] private bool smoothFill = true;
    [SerializeField] private float fillLerp = 12f;

    [Header("Shake FX (last N%)")]
    [SerializeField, Tooltip("เริ่มสั่นเมื่อเติมสีถึงสัดส่วนนี้ขึ้นไป (0..1). 0.55 = ช่วงท้าย 45%")]
    private float shakeStartFill = 0.55f;
    [SerializeField] private float shakeAmplitude = 8f;
    [SerializeField] private float shakeFrequency = 18f;
    [SerializeField] private float shakeReturnLerp = 15f;

    [Header("Audio (trend & shake)")]
    [SerializeField] private AudioSource trendSource;      // สำหรับขึ้น/ลง
    [SerializeField] private AudioClip riseLoop;           // ยิ้มค้าง  แท่งขึ้น (แดงเพิ่ม)
    [SerializeField] private AudioClip fallLoop;           // เลิกยิ้ม  แท่งลง (แดงลด)
    [SerializeField] private float trendBaseVolume = 0.6f;
    [SerializeField] private float trendMaxExtraVolume = 0.4f; // เพิ่มตามความเร็วการเปลี่ยน
    [SerializeField] private float trendPitch = 1f;
    [SerializeField] private float trendVolumeLerp = 10f;

    [SerializeField] private AudioSource shakeSource;      // สำหรับสั่น
    [SerializeField] private AudioClip shakeLoop;
    [SerializeField] private float shakeBaseVolume = 0.45f;
    [SerializeField] private float shakeVolumeLerp = 10f;

    // NEW: ----- Full bar SFX -----
    [Header("Audio (full bar SFX)")]
    [SerializeField] private AudioSource sfxSource;        // ยิง one-shot
    [SerializeField] private AudioClip fullFillSfx;        // คลิปเสียงตอนเต็มหลอด
    [SerializeField, Range(0f, 1f)] private float fullFillVolume = 1f;
    [SerializeField, Tooltip("ทริกเกอร์เมื่อ fill ข้ามค่านี้ขึ้นไป (ใกล้ 1 = ต้องเกือบเต็มจริง)")]
    [Range(0.9f, 1f)] private float fullFillThreshold = 0.995f;
    [SerializeField, Tooltip("กันสแปม: เวลาขั้นต่ำระหว่างการยิงซ้ำ")]
    private float fullSfxCooldown = 0.5f;
    float _lastFullSfxTime = -999f;                         // NEW

    public bool IsTracking => face && face.isTracking;

    float _currentFill; // 0..1 (0 = ไม่แดง, 1 = แดงเต็ม)
    float _prevFill;

    RectTransform _normalRect, _smileRect;
    Vector2 _basePosNormal, _basePosSmile;
    float _shakeSeed;

    enum Trend { Idle, Rising, Falling }
    Trend _trend = Trend.Idle;

    void Reset()
    {
        smileGate ??= GetComponent<SmileGateByMouthWideAuto>();
        face ??= GetComponent<OpenSeeTrackingHealth>();
    }

    void Awake()
    {
        Reset();
        SetupFillImage(normalFill);
        SetupFillImage(smileFill);

        if (normalFill) { var c = normalFill.color; normalFill.color = new Color(c.r, c.g, c.b, 1f); normalFill.raycastTarget = false; }
        if (smileFill) { var c = smileFill.color; smileFill.color = new Color(c.r, c.g, c.b, 1f); smileFill.raycastTarget = false; }

        _currentFill = 0f;
        _prevFill = _currentFill;
        ApplyFillInstant(_currentFill);

        _normalRect = normalFace ? normalFace.GetComponent<RectTransform>() : null;
        _smileRect = smileFace ? smileFace.GetComponent<RectTransform>() : null;
        _basePosNormal = _normalRect ? _normalRect.anchoredPosition : Vector2.zero;
        _basePosSmile = _smileRect ? _smileRect.anchoredPosition : Vector2.zero;

        _shakeSeed = Random.value * 1000f;
        EnsureFillOnTop();

        // mute & stop audio at boot
        SafeStop(trendSource);
        SafeStop(shakeSource);
    }

    void Update()
    {
        if (!IsTracking)
        {
            SetActiveSafe(trackingLost, true);
            SetActiveSafe(smileFace, false);
            SetActiveSafe(normalFace, true);
            SetFillAmount(0f, instant: true);
            ResetToBasePositions();
            EnsureFillOnTop();

            StopAllAudioImmediate();
            _prevFill = _currentFill;
            return;
        }

        SetActiveSafe(trackingLost, false);

        bool gatedSmile = smileGate && smileGate.isSmiling;
        float charge01 = (smileGate ? smileGate.charge01 : 1f);
        float targetFill = 1f - Mathf.Clamp01(charge01);

        // swap face
        SetActiveSafe(smileFace, gatedSmile);
        SetActiveSafe(normalFace, !gatedSmile);
        EnsureFillOnTop();

        // update fill & shake
        SetFillAmount(targetFill, instant: !smoothFill);
        UpdateShake(_currentFill, gatedSmile);

        // NEW: ยิง SFX ตอน "ข้าม" เกณฑ์เต็มหลอดจากด้านล่างขึ้นบน
        TryPlayFullSfx(_currentFill, _prevFill);

        // AUDIO: trend + shake
        UpdateAudio(_currentFill, _prevFill);

        _prevFill = _currentFill;
    }

    // =============== FULL BAR SFX (NEW) ===============
    void TryPlayFullSfx(float fillNow, float fillPrev)
    {
        if (!sfxSource || !fullFillSfx) return;

        // ทริกเกอร์เฉพาะตอน "ข้ามเกณฑ์" จากด้านล่าง -> ด้านบน (กันยิงซ้ำตอนค้างที่ 1.0)
        if (fillPrev < fullFillThreshold && fillNow >= fullFillThreshold)
        {
            if (Time.time - _lastFullSfxTime >= fullSfxCooldown)
            {
                sfxSource.PlayOneShot(fullFillSfx, fullFillVolume);
                _lastFullSfxTime = Time.time;
            }
        }

        // (ออปชัน) จะรีเซ็ต _lastFullSfxTime เมื่อ fill ลดลงต่ำกว่าเกณฑ์เยอะ ๆ ก็ได้
        // ไม่จำเป็น เพราะเราเช็ค crossing อยู่แล้ว
    }

    // ===================== AUDIO =====================
    void UpdateAudio(float fillNow, float fillPrev)
    {
        float delta = fillNow - fillPrev; // >0 = ขึ้น(แดงเพิ่ม), <0 = ลง(แดงลด)
        float eps = 0.0005f;

        // Trend state
        Trend newTrend = Trend.Idle;
        if (delta > eps) newTrend = Trend.Rising;
        else if (delta < -eps) newTrend = Trend.Falling;

        if (newTrend != _trend)
        {
            switch (newTrend)
            {
                case Trend.Rising: PlayLoop(trendSource, riseLoop); break;
                case Trend.Falling: PlayLoop(trendSource, fallLoop); break;
                default: break;
            }
            _trend = newTrend;
        }

        // Trend vol/pitch
        if (trendSource)
        {
            float speedPerSec = Mathf.Abs(delta) / Mathf.Max(0.0001f, Time.deltaTime);
            float speed01 = Mathf.Clamp01(speedPerSec * 1.25f);
            float targetVol = (_trend == Trend.Idle) ? 0f : (trendBaseVolume + trendMaxExtraVolume * speed01);
            trendSource.volume = Mathf.Lerp(trendSource.volume, targetVol, Time.deltaTime * trendVolumeLerp);
            trendSource.pitch = trendPitch;

            if (_trend == Trend.Idle && trendSource.volume < 0.02f) SafeStop(trendSource);
        }

        // Shake layer
        float tShake = Mathf.Clamp01(Mathf.InverseLerp(shakeStartFill, 1f, fillNow));
        if (tShake > 0f && shakeSource && shakeLoop) PlayLoop(shakeSource, shakeLoop);

        if (shakeSource)
        {
            float targetVol = (tShake > 0f) ? (shakeBaseVolume * Mathf.Pow(tShake, 1.1f)) : 0f;
            shakeSource.volume = Mathf.Lerp(shakeSource.volume, targetVol, Time.deltaTime * shakeVolumeLerp);
            if (tShake <= 0f && shakeSource.volume < 0.02f) SafeStop(shakeSource);
        }
    }

    void PlayLoop(AudioSource src, AudioClip clip)
    {
        if (!src || !clip) return;
        if (src.clip != clip) { src.clip = clip; src.loop = true; src.Play(); }
        else if (!src.isPlaying) src.Play();
    }

    void SafeStop(AudioSource src)
    {
        if (!src) return;
        src.Stop();
    }

    void StopAllAudioImmediate()
    {
        SafeStop(trendSource);
        SafeStop(shakeSource);
        if (trendSource) trendSource.volume = 0f;
        if (shakeSource) shakeSource.volume = 0f;
        // sfxSource เป็น one-shot ไม่ต้อง stop
    }

    // ===================== SHAKE =====================
    void UpdateShake(float fill01, bool showingSmile)
    {
        RectTransform active = showingSmile ? _smileRect : _normalRect;
        RectTransform inactive = showingSmile ? _normalRect : _smileRect;

        if (inactive)
        {
            Vector2 basePos = ReferenceBase(inactive);
            inactive.anchoredPosition = Vector2.Lerp(inactive.anchoredPosition, basePos, 1f - Mathf.Exp(-shakeReturnLerp * Time.deltaTime));
        }

        if (!active) return;

        float t = Mathf.Clamp01(Mathf.InverseLerp(shakeStartFill, 1f, fill01));
        Vector2 targetPos = ReferenceBase(active);

        if (t > 0f)
        {
            float amp = shakeAmplitude * Mathf.Pow(t, 1.2f);
            float nX = (Mathf.PerlinNoise(_shakeSeed, Time.time * shakeFrequency) - 0.5f) * 2f;
            float nY = (Mathf.PerlinNoise(_shakeSeed + 77f, Time.time * shakeFrequency) - 0.5f) * 2f;
            targetPos += new Vector2(nX, nY) * amp;
        }

        active.anchoredPosition = Vector2.Lerp(active.anchoredPosition, targetPos, 1f - Mathf.Exp(-shakeReturnLerp * Time.deltaTime));
    }

    Vector2 ReferenceBase(RectTransform rt)
    {
        if (!rt) return Vector2.zero;
        if (rt == _normalRect) return _basePosNormal;
        if (rt == _smileRect) return _basePosSmile;
        return Vector2.zero;
    }

    void ResetToBasePositions()
    {
        if (_normalRect) _normalRect.anchoredPosition = _basePosNormal;
        if (_smileRect) _smileRect.anchoredPosition = _basePosSmile;
    }

    // ===================== FILL =====================
    void EnsureFillOnTop()
    {
        if (smileFill && smileFill.transform.parent) smileFill.transform.SetAsLastSibling();
        if (normalFill && normalFill.transform.parent) normalFill.transform.SetAsLastSibling();
    }

    void SetupFillImage(Image img)
    {
        if (!img) return;
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Vertical;
        img.fillOrigin = (int)Image.OriginVertical.Bottom;
        img.fillAmount = 0f;
    }

    void SetFillAmount(float target, bool instant)
    {
        if (instant)
        {
            _currentFill = target;
        }
        else
        {
            float k = 1f - Mathf.Exp(-fillLerp * Time.deltaTime);
            _currentFill = Mathf.Lerp(_currentFill, target, k);
        }
        ApplyFillInstant(_currentFill);
    }

    void ApplyFillInstant(float value01)
    {
        if (normalFill) normalFill.fillAmount = value01;
        if (smileFill) smileFill.fillAmount = value01;
    }

    static void SetActiveSafe(GameObject go, bool state)
    {
        if (go && go.activeSelf != state) go.SetActive(state);
    }
}
