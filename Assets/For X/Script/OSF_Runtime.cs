using UnityEngine;
using System.IO;
using OSFLauncher = OpenSee.OpenSeeLauncher;   // type alias กันชนชื่อ namespace
using OSFComponent = OpenSee.OpenSee;

[DefaultExecutionOrder(-1000)]
public class OSF_Runtime : MonoBehaviour
{
    public static OSF_Runtime I;

    [SerializeField] OSFLauncher launcher;
    [SerializeField] OSFComponent osf;

    void Awake()
    {
        // wire field ที่ว่าง
        if (!launcher) launcher = GetComponent<OSFLauncher>();
        if (!osf) osf = GetComponent<OSFComponent>();

        // ตั้ง path (เหมือนที่ทำอยู่)
        var root = Path.Combine(Application.streamingAssetsPath, "OpenSeeFace");
        if (launcher)
        {
            launcher.exePath = Path.Combine(root, "Binary", "facetracker.exe");
            launcher.modelPath = Path.Combine(root, "models");
            launcher.dynamicPort = true;
        }

        // สมัครเข้าศูนย์กลาง  ทำให้ SmileGate/TrackingHealth มีแน่นอนและถูกผูกกับ OpenSee
        OSF_Service.Register(gameObject);

        // แล้วค่อยสตาร์ท (จะเจอ exe ไม่เจอ ค่อยไล่ log ต่อ)
        launcher?.StartTracker();
    }

    public void StartTracking() => launcher?.StartTracker();
    public void StopTracking() => launcher?.StopTracker();
}
