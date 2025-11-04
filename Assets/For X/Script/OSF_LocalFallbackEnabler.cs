using System.Collections;
using UnityEngine;

/// ใส่บน FaceTracking Obj (ตัวใน prefab)
public class OSF_LocalFallbackEnabler : MonoBehaviour
{
    [Header("Local tracker root (มี OpenSeeLauncher/OpenSee/SmileGate/TrackingHealth อยู่ใต้ตัวนี้)")]
    [SerializeField] GameObject localRoot;

    [Tooltip("ถ้าเริ่มซีนกลางแล้วไม่มี DDOL: จะเลื่อน localRoot ไปอยู่ DDOL เพื่ออยู่รอดข้ามซีน")]
    [SerializeField] bool promoteLocalToDDOL = false;

    IEnumerator Start()
    {
        // รอ 1 เฟรมให้ OSF_Service มีเวลาหา DDOL ก่อน
        yield return null;

        bool useLocal = !OSF_Service.Ready;

        if (useLocal)
        {
            if (localRoot) localRoot.SetActive(true);

            if (promoteLocalToDDOL && localRoot)
            {
                localRoot.name = "FaceTracking Obj";     // ให้ OSF_Service รู้จักชื่อ
                DontDestroyOnLoad(localRoot);            // เลื่อนเป็นตัวถาวร
            }
        }
        else
        {
            // มี DDOL แล้ว -> ไม่ต้องใช้ local เพื่อกันชนพอร์ต/กิน CPU
            if (localRoot) Destroy(localRoot); // หรือ SetActive(false) ถ้าอยากเก็บไว้เฉยๆ
        }
    }
}
