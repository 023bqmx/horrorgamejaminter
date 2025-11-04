using UnityEngine;
using System.IO;
using OSFLauncher = OpenSee.OpenSeeLauncher;   // alias
using OSFComponent = OpenSee.OpenSee;

[DefaultExecutionOrder(-1000)]
public class OSF_Runtime : MonoBehaviour
{
    private static OSF_Runtime _inst;          // singleton guard
    private static bool _started;              // กันสตาร์ทซ้ำ

    [Header("Refs")]
    [SerializeField] OSFLauncher launcher;
    [SerializeField] OSFComponent osf;

    [Header("Defaults")]
    [SerializeField] int fallbackCameraIndex = 0;  // ใช้ถ้าใน Inspector ยังเป็น -1

    void Awake()
    {
        // ----- Singleton + DDOL -----
        if (_inst && _inst != this) { Destroy(gameObject); return; }
        _inst = this;
        DontDestroyOnLoad(gameObject);

        // ----- Wire refs -----
        if (!launcher) launcher = GetComponent<OSFLauncher>();
        if (!osf) osf = GetComponent<OSFComponent>();

        // ----- Paths (StreamingAssets/OpenSeeFace) -----
        var root = Path.Combine(Application.streamingAssetsPath, "OpenSeeFace");
        if (launcher)
        {
            launcher.exePath = Path.Combine(root, "Binary", "facetracker.exe");
            launcher.modelPath = Path.Combine(root, "models");
            launcher.dynamicPort = true;

            // กล้อง: ถ้ายังเป็น -1 ให้ตั้ง fallback
            if (launcher.cameraIndex < 0) launcher.cameraIndex = fallbackCameraIndex;
        }

        // บอก service ให้รู้จักชุดคอมโพเนนต์นี้
        OSF_Service.Register(gameObject);

        // ----- Start once -----
        if (!_started && launcher != null)
        {
            _started = true;
            launcher.StartTracker();           // จะวิ่งอยู่ข้ามซีน
        }
    }
}
