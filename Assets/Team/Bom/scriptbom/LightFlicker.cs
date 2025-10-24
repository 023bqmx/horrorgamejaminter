using UnityEngine;

/// <summary>
/// ทำให้ไฟกระพริบ/ไหวแบบสมจริง:
/// - Perlin noise ทำให้สว่างขึ้นลงตลอด
/// - มีโอกาส "ดับวูบ" สั้นๆ แบบสุ่ม
/// ใช้กับ Light ใดๆ และเลือกให้ Emission ของวัสดุไหวตามได้
/// </summary>
[DisallowMultipleComponent]
public class LightFlicker : MonoBehaviour
{
    [Header("Target")]
    public Light targetLight;                // ปล่อยว่างไว้จะหา Light ในตัวเอง
    [Tooltip("คูณผลรวมทั้งหมดเข้ากับ Range ด้วย (บางงานอยากให้ลำแสงสั้นยาวตามด้วย)")]
    public bool scaleRangeWithIntensity = false;

    [Header("Base / Flicker")]
    [Tooltip("ความสว่างพื้นฐานของไฟ")]
    public float BaseIntensity = 2f;
    [Tooltip("ช่วงแกว่งรอบฐาน (เช่น 0.6 = +/-60%)")]
    [Range(0f, 2f)] public float FlickerAmplitude = 0.6f;
    [Tooltip("ความถี่คลื่นไหว (ยิ่งมากยิ่งไว)")]
    public float NoiseFrequency = 15f;
    [Tooltip("ความนุ่มของการเปลี่ยนค่า")]
    public float Smooth = 12f;

    [Header("Hard Blink (ดับวูบ)")]
    [Tooltip("โอกาสดับวูบต่อวินาที")]
    [Range(0f, 5f)] public float HardBlinkChance = 0.2f;
    [Tooltip("ช่วงเวลาที่ดับวูบ (วินาที)")]
    public Vector2 HardBlinkDuration = new Vector2(0.03f, 0.12f);

    [Header("Emission (Optional)")]
    public bool AffectEmission = false;
    [Tooltip("Renderer ของวัสดุที่จะให้ Emission ไหวตาม")]
    public Renderer EmissionRenderer;
    [Tooltip("สีของ Emission (จะคูณด้วยความสว่างที่แกว่ง)")]
    public Color EmissionColor = Color.white;
    [Tooltip("ชื่อพารามิเตอร์ Emission (URP/HDRP ใช้ _EmissionColor)")]
    public string EmissionColorProperty = "_EmissionColor";
    [Tooltip("กำลังพื้นฐานของ Emission")]
    public float EmissionBase = 1.5f;
    [Tooltip("ช่วงแกว่งของ Emission")]
    public float EmissionAmplitude = 1.0f;

    [Header("Random Seed")]
    public bool RandomizeSeed = true;
    public float Seed = 0f;

    // ---- internal ----
    float _blinkTimer = 0f;
    float _currentIntensity;
    float _originalRange;
    MaterialPropertyBlock _mpb;

    void Reset()
    {
        targetLight = GetComponent<Light>();
    }

    void Awake()
    {
        if (targetLight == null) targetLight = GetComponent<Light>();
        if (targetLight != null) _originalRange = targetLight.range;
        if (RandomizeSeed) Seed = Random.Range(0f, 1000f);

        _currentIntensity = BaseIntensity;

        if (AffectEmission && EmissionRenderer != null)
            _mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // เริ่ม/นับเวลาการดับวูบ
        if (_blinkTimer <= 0f)
        {
            // ความน่าจะเป็นต่อวินาที
            if (HardBlinkChance > 0f && Random.value < HardBlinkChance * dt)
                _blinkTimer = Random.Range(HardBlinkDuration.x, HardBlinkDuration.y);
        }
        else
        {
            _blinkTimer -= dt;
        }

        float target = BaseIntensity;

        if (_blinkTimer > 0f)
        {
            // ขณะดับวูบ: intensity = 0
            target = 0f;
        }
        else
        {
            // ไหวด้วย Perlin noise ในช่วง [Base * (1 - A) , Base * (1 + A)]
            float n = Mathf.PerlinNoise(Seed, Time.time * NoiseFrequency); // 0..1
            float normalized = (n * 2f - 1f); // -1..1
            float mul = 1f + normalized * FlickerAmplitude;
            target = Mathf.Max(0f, BaseIntensity * mul);
        }

        // ไล่เข้าหาค่า target ให้ลื่น
        _currentIntensity = Mathf.Lerp(_currentIntensity, target, dt * Smooth);

        // Apply to Light
        if (targetLight != null)
        {
            targetLight.intensity = _currentIntensity;
            if (scaleRangeWithIntensity)
                targetLight.range = _originalRange * Mathf.Lerp(0.7f, 1.1f, Mathf.InverseLerp(0f, BaseIntensity * (1f + FlickerAmplitude), _currentIntensity));
        }

        // Apply to Emission (ถ้าต้องการ)
        if (AffectEmission && EmissionRenderer != null)
        {
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            EmissionRenderer.GetPropertyBlock(_mpb);

            float e = Mathf.Max(0f, EmissionBase + (_currentIntensity - BaseIntensity) * EmissionAmplitude);
            Color final = EmissionColor * e;

            _mpb.SetColor(EmissionColorProperty, final);
            EmissionRenderer.SetPropertyBlock(_mpb);
        }
    }
}
