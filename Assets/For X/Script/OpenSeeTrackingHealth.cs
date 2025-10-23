using UnityEngine;
using OpenSee;

public class OpenSeeTrackingHealth : MonoBehaviour
{
    public OpenSee.OpenSee openSee;

    [Header("Criteria")]
    [Range(0f, 1f)] public float minConfidence = 0.25f;
    public float receiveTimeout = 0.5f; // �Թҷ�: ����������Թ��� = ��¢Ҵ

    [Header("Outputs (read-only)")]
    public bool isReceiving { get; private set; }   // UDP �ѧ������
    public bool hasFace { get; private set; }       // ��˹�����ҧ���� 1 ˹��
    public bool confident { get; private set; }     // �������� confidence ��ҹࡳ��
    public bool has3D { get; private set; }         // �� 3D landmarks
    public bool isTracking { get; private set; }    // ��Ƿ��س��ͧ�����
    public int usedFaceIndex { get; private set; } = -1;
    public float lastConfidence { get; private set; }
    public double lastTrackerTime { get; private set; }

    int lastPackets = -1;
    float lastReceiveAt = -999f;

    float Mean(float[] a)
    {
        if (a == null || a.Length == 0) return 0f;
        float s = 0f; for (int i = 0; i < a.Length; i++) s += a[i];
        return s / a.Length;
    }

    void Update()
    {
        // 1) Heartbeat �ҡ��ǹѺ����
        int packets = openSee ? openSee.receivedPackets : 0;
        if (packets != lastPackets)
        {
            lastPackets = packets;
            lastReceiveAt = Time.realtimeSinceStartup;
        }
        isReceiving = (Time.realtimeSinceStartup - lastReceiveAt) <= receiveTimeout;

        // 2) ��˹�� + 3) �س�Ҿ
        var td = openSee?.trackingData;
        hasFace = td != null && td.Length > 0;
        confident = false; has3D = false; usedFaceIndex = -1; lastConfidence = 0f;

        if (hasFace)
        {
            int best = -1; float bestConf = minConfidence;
            for (int i = 0; i < td.Length; i++)
            {
                float conf = Mean(td[i].confidence);   // confidence �� float[]
                if (conf > bestConf) { bestConf = conf; best = i; }
            }
            if (best >= 0)
            {
                usedFaceIndex = best;
                lastConfidence = bestConf;
                has3D = td[best].got3DPoints;
                lastTrackerTime = td[best].time;
                confident = true;
            }
        }

        // ࡳ���ش����: ����ѧ�� + ��˹�Ҥس�Ҿ��
        // �������鹡��� && has3D ��
        isTracking = isReceiving && confident;


        //Debug.Log("isTracking : " + isTracking);
    }
}
