using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

// alias กันชน namespace/คลาส
using OSFLauncher = OpenSee.OpenSeeLauncher;
using OSFComponent = OpenSee.OpenSee;

public static class OSF_GlobalBootstrap
{
    const string FaceTrackingName = "FaceTracking Obj";
    // ถ้าจะโหลดจาก Resources ให้สร้างไฟล์: Assets/Resources/FaceTracking/FaceTracking Obj.prefab
    const string FaceTrackingResourcePath = "FaceTracking/FaceTracking Obj";

    static bool _booted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetFlag() { _booted = false; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (_booted) return;
        _booted = true;
        SceneManager.sceneLoaded += (_, __) => EnsureAndStart();
        EnsureAndStart();
    }

    static void EnsureAndStart()
    {
        // 1) พยายามหา FaceTracking Obj ที่ “ถูกโหลดแล้ว” (รวม DDOL)
        var go = FindLoadedObjectByName(FaceTrackingName);

        // 2) ถ้าไม่เจอ ลองหาใต้ Player (tag=Player)
        if (!go)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player) go = FindChildRecursive(player.transform, FaceTrackingName)?.gameObject;
        }

        // 3) ถ้าไม่เจออีก โหลดจาก Resources
        if (!go)
        {
            var prefab = Resources.Load<GameObject>(FaceTrackingResourcePath);
            if (prefab)
            {
                go = Object.Instantiate(prefab);
                go.name = FaceTrackingName; // กัน (Clone)
                Object.DontDestroyOnLoad(go);
                Debug.Log("[OSF] Instantiated FaceTracking Obj from Resources.");
            }
        }

        // 4) ถ้ายังไม่เจอเลย สร้าง runtime เปล่าเป็นทางสุดท้าย
        OSFLauncher launcher = null;
        OSFComponent osf = null;

        if (go)
        {
            // ใช้ของใน prefab/scene ที่มีอยู่
            launcher = go.GetComponentInChildren<OSFLauncher>(true) ?? go.AddComponent<OSFLauncher>();
            osf = go.GetComponentInChildren<OSFComponent>(true) ?? go.AddComponent<OSFComponent>();
            launcher.openSeeTarget = osf;
        }
        else
        {
            var auto = new GameObject("OSF_Runtime(auto)");
            Object.DontDestroyOnLoad(auto);
            osf = auto.AddComponent<OSFComponent>();
            launcher = auto.AddComponent<OSFLauncher>();
            launcher.openSeeTarget = osf;
            Debug.LogWarning("[OSF] FaceTracking Obj not found. Using minimal runtime.");
        }

        // 5) ตั้ง path แบบ absolute จาก StreamingAssets (ทับค่าจาก Inspector เสมอ)
        var root = Path.Combine(Application.streamingAssetsPath, "OpenSeeFace");
        var exe = Path.Combine(root, "Binary", "facetracker.exe");
        var models = Path.Combine(root, "models");

        launcher.exePath = exe;
        launcher.modelPath = models;
        launcher.dynamicPort = true;
        launcher.cameraIndex = PlayerPrefs.GetInt("OSF.CameraIndex", -1);

#if UNITY_EDITOR
        launcher.dontPrint = false; // dev: ให้เห็นคอนโซล facetracker
#endif
        Debug.Log($"[OSF] exePath exists={File.Exists(exe)}, modelPath exists={Directory.Exists(models)}");

        launcher.StartTracker();
        Debug.Log("[OSF] StartTracker() issued");
    }

    // ---------- helpers ----------
    static GameObject FindLoadedObjectByName(string name)
    {
        var all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var g in all)
        {
            if (!g || g.hideFlags != HideFlags.None) continue; // ตัด asset/prefab ใน Project window
            if (g.name == name) return g;
        }
        return null;
    }

    static Transform FindChildRecursive(Transform root, string name)
    {
        if (!root) return null;
        foreach (Transform c in root)
        {
            if (c.name == name) return c;
            var t = FindChildRecursive(c, name);
            if (t) return t;
        }
        return null;
    }
}
