using UnityEngine;
using System.IO;
using OpenSee;

[DefaultExecutionOrder(-1000)]
public class OSF_LauncherBootstrap : MonoBehaviour
{
    [SerializeField] private OpenSeeLauncher launcher;
    [SerializeField] private OpenSee openSee;

    void Reset()
    {
        launcher = GetComponent<OpenSeeLauncher>();
        openSee = GetComponent<OpenSee>();
    }

    void Awake()
    {
        if (!launcher) launcher = FindObjectOfType<OpenSeeLauncher>(true);
        if (!openSee) openSee = FindObjectOfType<OpenSee>(true);
        if (launcher && openSee) launcher.openSeeTarget = openSee;

        // ชี้ path แบบ absolute เพื่อไม่งง working directory
        var root = Path.Combine(Application.streamingAssetsPath, "OpenSeeFace");
        launcher.executablePath = Path.Combine(root, "Binary", "facetracker.exe");
        launcher.modelPath = Path.Combine(root, "models");  // ไม่ต้อง ../ อะไรทั้งนั้น

        // ค่าพื้นฐานที่ปลอดภัย
        launcher.cameraIndex = -1;            // เดี๋ยวเราให้ UI เลือก
        launcher.dynamicPort = true;          // ให้ launcher + OpenSee คุยกันเอง
        launcher.dontPrint = false;         // ช่วงดีบักให้เห็น console; โปรดักชันค่อยติ๊ก

        // ปรับ args ตามงบ CPU/GPU ของเกมคุณ
        launcher.commandlineOptions = new string[] 
        {
            "--faces","1",
            "--fps","30",
            "--model","1",
            "--max-threads","2"
            // "--silent"     // เปิดใช้งานภายหลังถ้าต้องการเงียบสนิท
        };
    }
}
