using UnityEngine;
using OpenSee;

public class SmileGateByMouthWideAuto : MonoBehaviour
{
    public OpenSee.OpenSee openSee;

    [Range(0f, 1f)] public float threshold = 0.50f;     // ��������� MouthWide >= threshold
    [Range(0f, 1f)] public float minConfidence = 0.25f; // �Ѵ˹��, ��Ҥ��������蹵�ӡ��ҹ��

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

        // ���͡ face ����դ�� "confidence �����" �٧�ش
        int best = -1; float bestConf = minConfidence;
        for (int i = 0; i < data.Length; i++)
        {
            float conf = Mean(data[i].confidence);              // confidence �� float[]
            if (conf > bestConf) { best = i; bestConf = conf; }
        }
        if (best < 0) { isSmiling = false; usedFaceIndex = -1; return; }

        usedFaceIndex = best;
        lastConfidence = bestConf;

        // ����Ϳ�Ŵ�Ẻ PascalCase ������� OpenSeeFeatures
        lastMouthWide = data[best].features.MouthWide;          // << ����� mouthWide ������

        isSmiling = lastMouthWide >= threshold;

        // �٤��� Console �ء ~10 ���
        if (Time.frameCount % 10 == 0)
            Debug.Log($"idx={usedFaceIndex} mw={lastMouthWide:F3} conf={lastConfidence:F2} thr={threshold:F2} smiling={isSmiling}");
    }
}
