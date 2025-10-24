using UnityEngine;
using OpenSee;

public class SmileGateByMouthWideAuto : MonoBehaviour
{
    [Header("OpenSee")]
    public OpenSee.OpenSee openSee;

    [Header("Smile Detection")]
    [Range(0f, 1f)] public float threshold = 0.50f;     // ยิ้มเมื่อ MouthWide >= threshold
    [Range(0f, 1f)] public float minConfidence = 0.25f; // ค่าความเชื่อมั่นขั้นต่ำของใบหน้า

    [Header("Smile Budget / Cooldown")]
    [Tooltip("ยิ้มต่อเนื่องได้สูงสุดกี่วินาที (หมดแล้วตัดทันที)")]
    public float maxSmileSeconds = 2f;
    [Tooltip("เวลาคูลดาวน์หลังหมดพลังยิ้ม")]
    public float cooldownSeconds = 2f;
    [Tooltip("อัตราฟื้นพลังยิ้มต่อวินาที (ระหว่างไม่ได้ยิ้ม/คูลดาวน์)")]
    public float regenPerSecond = 1.5f;

    // ===== Output (ให้ที่อื่นใช้) =====
    /// <summary>สัญญาณดิบจาก OpenSee (ตาม threshold/confidence) — ไม่ถูกจำกัดเวลา</summary>
    public bool rawSmiling { get; private set; }
    /// <summary>สัญญาณที่ "อนุญาตแล้ว" สำหรับเกมเพลย์ (มีลิมิต/คูลดาวน์) — ใช้อันนี้กับ AI</summary>
    public bool isSmiling { get; private set; }
    /// <summary>กำลังคูลดาวน์อยู่หรือไม่</summary>
    public bool isCoolingDown => Time.time < _lockUntil;
    /// <summary>พลังยิ้มที่เหลือคิดเป็น 0..1 (เอาไปไล่สี/ทำ UI ได้)</summary>
    public float charge01 => Mathf.Clamp01(_charge / Mathf.Max(0.0001f, maxSmileSeconds));

    // Debug
    public int usedFaceIndex = -1;
    public float lastMouthWide;
    public float lastConfidence;

    // ===== Internal =====
    float _charge;                // วินาทียิ้มที่เหลือ
    float _lockUntil = -999f;     // เวลาเลิกคูลดาวน์

    void Awake()
    {
        _charge = maxSmileSeconds;   // เริ่มเต็มถัง
    }

    void Update()
    {
        // 1) อ่านหน้าและคำนวณ "สัญญาณดิบ"
        var data = openSee ? openSee.trackingData : null;
        if (data == null || data.Length == 0)
        {
            usedFaceIndex = -1;
            lastConfidence = 0f;
            lastMouthWide = 0f;
            rawSmiling = false;

            // หลุดหน้า: รีเซ็ตพลังกลับไปเต็ม เพื่อกันติดสถานะ
            HardResetToFull();
            return;
        }

        int best = -1; float bestConf = minConfidence;
        for (int i = 0; i < data.Length; i++)
        {
            float conf = Mean(data[i].confidence);
            if (conf > bestConf) { best = i; bestConf = conf; }
        }
        if (best < 0)
        {
            usedFaceIndex = -1;
            lastConfidence = 0f;
            lastMouthWide = 0f;
            rawSmiling = false;

            HardResetToFull();
            return;
        }

        usedFaceIndex = best;
        lastConfidence = bestConf;
        lastMouthWide = data[best].features.MouthWide; // PascalCase ตรงกับคลาส

        rawSmiling = lastMouthWide >= threshold;

        // 2) Gate ด้วยลอจิกเวลา (state machine)
        if (isCoolingDown)
        {
            // คูลดาวน์อยู่: ห้ามยิ้ม แต่ฟื้นพลังกลับไปหาเต็ม
            isSmiling = false;
            RegenTowardFull();
        }
        else
        {
            if (isSmiling)
            {
                // อยู่ใน session ยิ้มแล้ว
                if (rawSmiling)
                {
                    _charge -= Time.deltaTime;
                    if (_charge <= 0f)
                    {
                        // หมดถัง ปิดยิ้ม + เริ่มคูลดาวน์
                        _charge = 0f;
                        isSmiling = false;
                        _lockUntil = Time.time + cooldownSeconds;
                    }
                }
                else
                {
                    // เลิกยิ้มกลางคัน ปิดยิ้ม แล้วเริ่มฟื้น
                    isSmiling = false;
                }
            }
            else
            {
                // ยังไม่อยู่ใน session ยิ้ม
                if (rawSmiling && IsFullCharge())
                {
                    // อนุญาตเริ่มยิ้มเฉพาะตอน "ขาวสุด/เต็มถัง" เท่านั้น
                    isSmiling = true;
                    _charge = Mathf.Max(0f, _charge - Time.deltaTime); // หักทันทีเฟรมแรก
                }
                else
                {
                    // ไม่ได้ยิ้ม ฟื้นกลับไปหาเต็ม
                    RegenTowardFull();
                }
            }
        }

        // 3) Debug ทุก ~10 เฟรม
        if (Time.frameCount % 10 == 0)
            Debug.Log($"[SmileGate] idx={usedFaceIndex} mw={lastMouthWide:F3} conf={lastConfidence:F2} raw={rawSmiling} gate={isSmiling} cd={isCoolingDown} charge={charge01:F2}");
    }

    // ===== Helpers =====
    float Mean(float[] a)
    {
        if (a == null || a.Length == 0) return 0f;
        float s = 0f; for (int i = 0; i < a.Length; i++) s += a[i];
        return s / a.Length;
    }

    void RegenTowardFull()
    {
        if (_charge < maxSmileSeconds)
        {
            _charge += regenPerSecond * Time.deltaTime;
            if (_charge >= maxSmileSeconds) _charge = maxSmileSeconds;
        }
    }

    bool IsFullCharge() => _charge >= maxSmileSeconds - 1e-4f;

    void HardResetToFull()
    {
        isSmiling = false;
        _lockUntil = -999f;
        _charge = maxSmileSeconds;
    }
}
