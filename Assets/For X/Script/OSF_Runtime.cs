using UnityEngine;
using System.IO;
using OSFLauncher = OpenSee.OpenSeeLauncher; // alias
using OSFComponent = OpenSee.OpenSee;

[DefaultExecutionOrder(-1000)]
public class OSF_Runtime : MonoBehaviour
{
    public static OSF_Runtime I { get; private set; }   // <-- ให้สคริปต์อื่นเรียกได้
    static bool _started;                                // กันสตาร์ทซ้ำ

    [Header("Refs")]
    [SerializeField] OSFLauncher launcher;
    [SerializeField] OSFComponent osf;

    [Header("Defaults")]
    [SerializeField] int fallbackCameraIndex = 0;        // ใช้ถ้า Inspector เป็น -1

    void Awake()
    {
        // ----- Singleton + DDOL -----
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        // ----- Wire refs -----
        if (!launcher) launcher = GetComponent<OSFLauncher>();
        if (!osf) osf = GetComponent<OSFComponent>();

        // ----- Paths -----
        var root = Path.Combine(Application.streamingAssetsPath, "OpenSeeFace");
        if (launcher)
        {
            launcher.exePath = Path.Combine(root, "Binary", "facetracker.exe");
            launcher.modelPath = Path.Combine(root, "models");
            launcher.dynamicPort = true;

            // กล้อง: ถ้ายัง -1 ให้ใช้ค่า fallback
            if (launcher.cameraIndex < 0) launcher.cameraIndex = fallbackCameraIndex;
        }

        // แจ้ง service ให้รู้จัก root นี้
        OSF_Service.Register(gameObject);

        // Auto-start หนแรก (กันซ้ำด้วย _started)
        StartTracking();
    }

    public void StartTracking()
    {
        if (_started || launcher == null) return;
        _started = true;
        launcher.StartTracker();
    }

    public void StopTracking()
    {
        if (!_started) return;
        _started = false;
        launcher?.StopTracker();
    }
}
