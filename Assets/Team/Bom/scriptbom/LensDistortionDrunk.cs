using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LensDistortionDrunk : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Global Volume ที่มีเอฟเฟกต์ Lens Distortion")]
    public Volume volume;

    [Header("Drunk Oscillation")]
    [Tooltip("ค่าเริ่มกลางของ X Multiplier (0..1)")]
    [Range(0f, 1f)] public float BaseX = 0.5f;
    [Tooltip("ค่าเริ่มกลางของ Y Multiplier (0..1)")]
    [Range(0f, 1f)] public float BaseY = 0.5f;

    [Tooltip("ความกว้างของการแกว่ง (0..1)")]
    [Range(0f, 1f)] public float Amplitude = 0.25f;

    [Tooltip("ความถี่การแกว่ง (รอบ/วินาทีโดยประมาณ)")]
    public float Frequency = 0.8f;

    [Tooltip("ทำให้ X/Y ตอบสนองนุ่มขึ้น")]
    public float SmoothLerp = 8f;

    [Header("Optional: Animate Intensity")]
    public bool AnimateIntensity = false;
    [Range(0f, 1f)] public float BaseIntensity = 0.45f;
    [Range(0f, 1f)] public float IntensityAmplitude = 0.1f;

    [Header("Randomness (optional)")]
    [Tooltip("สุ่มเฟสเริ่มต้นเพื่อไม่ให้แกว่งเหมือนเดิมทุกครั้ง")]
    public bool RandomizePhase = true;

    private LensDistortion _lens;
    private float _phase;

    private void Reset()
    {
        volume = GetComponent<Volume>();
    }

    private void Awake()
    {
        if (volume == null) volume = GetComponent<Volume>();
    }

    private void Start()
    {
        if (volume == null)
        {
            Debug.LogError("[LensDistortionDrunk] Volume reference is missing.");
            enabled = false;
            return;
        }

        // ใช้ profile ที่ runtime ได้ (ถ้าไม่มี profile ให้ fallback ไป sharedProfile)
        var profile = volume.profile != null ? volume.profile : volume.sharedProfile;
        if (profile == null || !profile.TryGet(out _lens))
        {
            Debug.LogError("[LensDistortionDrunk] LensDistortion override not found in Volume Profile.");
            enabled = false;
            return;
        }

        _phase = RandomizePhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;
    }

    private void Update()
    {
        if (_lens == null) return;

        // สร้างคลื่นไซน์
        float t = Time.time * (Mathf.Max(0.0001f, Frequency) * 2f * Mathf.PI);
        float s = Mathf.Sin(t + _phase);          // -1..1

        // X/Y สลับกัน: X = base + A*s, Y = base - A*s
        float targetX = Mathf.Clamp01(BaseX + Amplitude * s);
        float targetY = Mathf.Clamp01(BaseY - Amplitude * s);

        // ไล่เข้าเป้าด้วย lerp ให้ลื่นๆ
        _lens.xMultiplier.value = Mathf.Lerp(_lens.xMultiplier.value, targetX, Time.deltaTime * SmoothLerp);
        _lens.yMultiplier.value = Mathf.Lerp(_lens.yMultiplier.value, targetY, Time.deltaTime * SmoothLerp);

        // (ทางเลือก) ทำให้ Intensity แกว่งเบาๆ ด้วย
        if (AnimateIntensity)
        {
            float i = Mathf.Clamp01(BaseIntensity + IntensityAmplitude * Mathf.Sin(t * 0.5f));
            _lens.intensity.value = Mathf.Lerp(_lens.intensity.value, i, Time.deltaTime * SmoothLerp);
        }
    }
}
