using System;
using System.Linq;
using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class OSF_Service : MonoBehaviour
{
    public static OSF_Service Instance { get; private set; }
    public static bool Ready { get; private set; }
    public static SmileGateByMouthWideAuto SmileGate { get; private set; }
    public static OpenSeeTrackingHealth TrackingHealth { get; private set; }

    public static event Action OnReady;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance) return;
        var go = new GameObject("OSF_Service");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<OSF_Service>();
    }

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        StartCoroutine(ResolveLoop());
    }

    IEnumerator ResolveLoop()
    {
        var wait = new WaitForSecondsRealtime(0.25f); // ค้นหาทุก 0.25s
        while (!TryResolve())
            yield return wait;

        Ready = true;
        OnReady?.Invoke();
    }
    bool TryResolve()
    {
        // 1) หา GameObject ชื่อ "FaceTracking Obj" ที่อยู่ใน scene ชื่อ "DontDestroyOnLoad"
        var allGO = Resources.FindObjectsOfTypeAll<GameObject>();
        var ddolGO = allGO.FirstOrDefault(g =>
            g && g.name == "FaceTracking Obj" && g.scene.name == "DontDestroyOnLoad");

        if (ddolGO)
        {
            SmileGate = ddolGO.GetComponentInChildren<SmileGateByMouthWideAuto>(true);
            TrackingHealth = ddolGO.GetComponentInChildren<OpenSeeTrackingHealth>(true);
        }

        // 2) สำรอง: เจาะหา Component โดยตรง “ที่อยู่ใน DDOL” ก่อน
        if (!SmileGate)
            SmileGate = Resources.FindObjectsOfTypeAll<SmileGateByMouthWideAuto>()
                .FirstOrDefault(c => c && c.gameObject.scene.name == "DontDestroyOnLoad");
        if (!TrackingHealth)
            TrackingHealth = Resources.FindObjectsOfTypeAll<OpenSeeTrackingHealth>()
                .FirstOrDefault(c => c && c.gameObject.scene.name == "DontDestroyOnLoad");

        return (SmileGate && TrackingHealth);
    }

    public static void Register(GameObject root)
    {
        if (!root) return;

        // ดึงคอมโพเนนต์จาก FaceTracking Obj ที่ส่งมา
        SmileGate ??= root.GetComponentInChildren<SmileGateByMouthWideAuto>(true);
        TrackingHealth ??= root.GetComponentInChildren<OpenSeeTrackingHealth>(true);

        if (SmileGate && TrackingHealth)
        {
            Ready = true;
            OnReady?.Invoke();
        }
    }
}
