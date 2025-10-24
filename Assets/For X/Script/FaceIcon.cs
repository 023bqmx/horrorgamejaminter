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
    [SerializeField, Tooltip("ทำให้ระดับหลอดไหลลื่นขึ้น")]
    private bool smoothFill = true;
    [SerializeField, Tooltip("ความไวการไหล (สูง=ไว)")]
    private float fillLerp = 12f;

    [Header("Shake FX (last 30%)")]
    [SerializeField, Tooltip("เริ่มสั่นเมื่อเติมสีถึงสัดส่วนนี้ขึ้นไป (0..1)")]
    private float shakeStartFill = 0.70f;      // 70% = ช่วง 30% สุดท้าย
    [SerializeField, Tooltip("แรงสั่นสูงสุด (พิกเซล)")]
    private float shakeAmplitude = 8f;
    [SerializeField, Tooltip("ความถี่การสั่น (Hz-ish)")]
    private float shakeFrequency = 18f;
    [SerializeField, Tooltip("ความไวในการคืนตำแหน่งฐาน")]
    private float shakeReturnLerp = 15f;

    public bool IsTracking => face && face.isTracking;

    float _currentFill; // 0..1 (0 = ไม่แดง, 1 = แดงเต็ม)

    // cache rects & base positions เพื่อกัน drift ตอนสลับหน้า
    RectTransform _normalRect, _smileRect;
    Vector2 _basePosNormal, _basePosSmile;
    float _shakeSeed;

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

        // กันลืม: บังคับ alpha = 1 และไม่รับ Raycast
        if (normalFill) { var c = normalFill.color; normalFill.color = new Color(c.r, c.g, c.b, 1f); normalFill.raycastTarget = false; }
        if (smileFill) { var c = smileFill.color; smileFill.color = new Color(c.r, c.g, c.b, 1f); smileFill.raycastTarget = false; }

        _currentFill = 0f;
        ApplyFillInstant(_currentFill);

        // cache rects & base positions
        _normalRect = normalFace ? normalFace.GetComponent<RectTransform>() : null;
        _smileRect = smileFace ? smileFace.GetComponent<RectTransform>() : null;
        _basePosNormal = _normalRect ? _normalRect.anchoredPosition : Vector2.zero;
        _basePosSmile = _smileRect ? _smileRect.anchoredPosition : Vector2.zero;

        _shakeSeed = Random.value * 1000f;

        EnsureFillOnTop();
    }

    void Update()
    {
        if (!IsTracking)
        {
            SetActiveSafe(trackingLost, true);
            SetActiveSafe(smileFace, false);
            SetActiveSafe(normalFace, true);
            SetFillAmount(0f, instant: true);

            // reset shake positions on tracking loss
            ResetToBasePositions();
            EnsureFillOnTop();
            return;
        }

        SetActiveSafe(trackingLost, false);

        bool gatedSmile = smileGate && smileGate.isSmiling;
        float charge01 = (smileGate ? smileGate.charge01 : 1f); // 1=ขาว, 0=แดง
        float targetFill = 1f - Mathf.Clamp01(charge01);         // 0..1 (แดงจากล่างขึ้นบน)

        // สลับหน้า
        SetActiveSafe(smileFace, gatedSmile);
        SetActiveSafe(normalFace, !gatedSmile);

        EnsureFillOnTop();

        // อัปเดตหลอดแดง
        SetFillAmount(targetFill, instant: !smoothFill);

        // อัปเดตเอฟเฟกต์สั่น (สั่นเฉพาะช่วงท้าย ๆ)
        UpdateShake(targetFill, gatedSmile);
    }

    // =============== Shake ===============
    void UpdateShake(float fill01, bool showingSmile)
    {
        // เลือก rect ปัจจุบันที่กำลังแสดง และทรงจำตำแหน่งฐาน
        RectTransform active = showingSmile ? _smileRect : _normalRect;
        RectTransform inactive = showingSmile ? _normalRect : _smileRect;

        if (inactive) // คืน inactive ให้กลับ base ทันที ป้องกันค้าง offset
        {
            Vector2 basePos = ReferenceBase(inactive);
            inactive.anchoredPosition = Vector2.Lerp(inactive.anchoredPosition, basePos, 1f - Mathf.Exp(-shakeReturnLerp * Time.deltaTime));
        }

        if (!active) return;

        float t = Mathf.InverseLerp(shakeStartFill, 1f, fill01); // 0..1 เข้าสู่ช่วงสั่น
        t = Mathf.Clamp01(t);

        Vector2 targetPos = ReferenceBase(active);

        if (t > 0f)
        {
            // amplitude scale โค้งขึ้นเล็กน้อยตอนใกล้หมด (t^1.2)
            float amp = shakeAmplitude * Mathf.Pow(t, 1.2f);

            // Perlin noise 2D เพื่อความลื่น
            float nX = (Mathf.PerlinNoise(_shakeSeed, Time.time * shakeFrequency) - 0.5f) * 2f;
            float nY = (Mathf.PerlinNoise(_shakeSeed + 77f, Time.time * shakeFrequency) - 0.5f) * 2f;

            Vector2 offset = new Vector2(nX, nY) * amp;

            // ไล่เข้าหาตำแหน่งเป้าหมาย + offset อย่างลื่น
            targetPos += offset;
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

    // =============== Fill helpers ===============
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
            float k = 1f - Mathf.Exp(-fillLerp * Time.deltaTime); // exp-smooth
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
