using UnityEngine;
using OpenSee;

public class OpenSeeTrackingHealth : MonoBehaviour
{
    public OpenSee.OpenSee openSee;

    [Header("Criteria")]
    [Range(0f, 1f)] public float minConfidence = 0.25f;
    public float receiveTimeout = 0.5f; // วินาที: ไม่มีแพ็กเก็ตเกินนี้ = สายขาด

    [Header("Outputs (read-only)")]
    public bool isReceiving { get; private set; }   // UDP ยังมาอยู่
    public bool hasFace { get; private set; }       // มีหน้าอย่างน้อย 1 หน้า
    public bool confident { get; private set; }     // ค่าเฉลี่ย confidence ผ่านเกณฑ์
    public bool has3D { get; private set; }         // ได้ 3D landmarks
    public bool isTracking { get; private set; }    // ตัวที่คุณต้องการใช้
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
        // 1) Heartbeat จากตัวนับแพ็กเก็ต
        int packets = openSee ? openSee.receivedPackets : 0;
        if (packets != lastPackets)
        {
            lastPackets = packets;
            lastReceiveAt = Time.realtimeSinceStartup;
        }
        isReceiving = (Time.realtimeSinceStartup - lastReceiveAt) <= receiveTimeout;

        // 2) มีหน้า + 3) คุณภาพ
        var td = openSee?.trackingData;
        hasFace = td != null && td.Length > 0;
        confident = false; has3D = false; usedFaceIndex = -1; lastConfidence = 0f;

        if (hasFace)
        {
            int best = -1; float bestConf = minConfidence;
            for (int i = 0; i < td.Length; i++)
            {
                float conf = Mean(td[i].confidence);   // confidence เป็น float[]
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

        // เกณฑ์สุดท้าย: สายยังมา + มีหน้าคุณภาพดี
        // จะเข้มขึ้นก็ใช้ && has3D ได้
        isTracking = isReceiving && confident;


        //Debug.Log("isTracking : " + isTracking);
    }
}
