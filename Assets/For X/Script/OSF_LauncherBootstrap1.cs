using UnityEngine;
using System.IO;

// alias ให้ชัดเจน (ในแพ็ก OSF มีทั้ง namespace OpenSee และคลาส OpenSee)
using OSFLauncher = OpenSee.OpenSeeLauncher;  // ตัว launcher
using OSFComponent = OpenSee.OpenSee;          // คอมโพเนนต์ตัวรับใน Unity

[DefaultExecutionOrder(-1000)]
public class OSF_LauncherBootstrap : MonoBehaviour
{
    [SerializeField] private OSFLauncher launcher;
    [SerializeField] private OSFComponent osf;

    void Awake()
    {
        if (!launcher) launcher = FindObjectOfType<OSFLauncher>(true);
        if (!osf) osf = FindObjectOfType<OSFComponent>(true);
        if (launcher) launcher.openSeeTarget = osf;

        // ใช้ absolute path จาก StreamingAssets เพื่อกันพังตอน Build
        var root = Path.Combine(Application.streamingAssetsPath, "OpenSeeFace");
        launcher.exePath = Path.Combine(root, "Binary", "facetracker.exe"); // << ชื่อฟิลด์ที่ถูก
        launcher.modelPath = Path.Combine(root, "models");                     // << ชื่อฟิลด์ที่ถูก

        launcher.dynamicPort = true;   // ให้เจรจาพอร์ตกับ OpenSee อัตโนมัติ
        launcher.cameraIndex = -1;     // ให้ UI ไปตั้งค่าเอง (ปลอดภัยกว่า)


        // ช่วงดีบักอย่าเปิด dontPrint จะได้เห็นหน้าต่างคอนโซล
        // launcher.dontPrint = true; // ใช้เฉพาะตอนโปรดักชันถ้าอยากเงียบ
    }
}
