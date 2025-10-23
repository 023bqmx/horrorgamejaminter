using UnityEngine;
using OpenSee;

public class SmileGateByMouthWideAuto : MonoBehaviour
{
    public OpenSee.OpenSee openSee;

    [Range(0f, 1f)] public float threshold = 0.50f;     // ยิ้มเมื่อ MouthWide >= threshold
    [Range(0f, 1f)] public float minConfidence = 0.25f; // ตัดหน้า, ถ้าค่าเชื่อมั่นต่ำกว่านี้

    // Debug / Output
    public bool isSmiling { get; private set; }
    public int usedFaceIndex = -1;
    public float lastMouthWide;
    public float lastConfidence;

    float Mean(float[] a)
    {
        if (a == null || a.Length == 0) return 0f;
        float s = 0f; for (int i = 0; i < a.Length; i++) s += a[i];
        return s / a.Length;
    }

    void Update()
    {
        var data = openSee ? openSee.trackingData : null;       // OpenSeeData[]
        if (data == null || data.Length == 0) { isSmiling = false; usedFaceIndex = -1; return; }

        // เลือก face ที่มีค่า "confidence เฉลี่ย" สูงสุด
        int best = -1; float bestConf = minConfidence;
        for (int i = 0; i < data.Length; i++)
        {
            float conf = Mean(data[i].confidence);              // confidence เป็น float[]
            if (conf > bestConf) { best = i; bestConf = conf; }
        }
        if (best < 0) { isSmiling = false; usedFaceIndex = -1; return; }

        usedFaceIndex = best;
        lastConfidence = bestConf;

        // ใช้ชื่อฟิลด์แบบ PascalCase ตามคลาส OpenSeeFeatures
        lastMouthWide = data[best].features.MouthWide;          // << ไม่ใช่ mouthWide ตัวเล็ก

        isSmiling = lastMouthWide >= threshold;

        // ดูค่าใน Console ทุก ~10 เฟรม
        if (Time.frameCount % 10 == 0)
            Debug.Log($"idx={usedFaceIndex} mw={lastMouthWide:F3} conf={lastConfidence:F2} thr={threshold:F2} smiling={isSmiling}");
    }
}
