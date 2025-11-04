using UnityEngine;

public class OSF_SceneHook : MonoBehaviour
{
    public bool autoStart = true;

    void OnEnable() { if (autoStart) OSF_Runtime.I?.StartTracking(); }

    // ถ้าอยากให้ตัวติดตามหน้าค้างข้ามซีน อย่า Stop ตอน OnDisable
    // ถ้าเป็นซีนเทสต์เท่านั้นค่อยเปิดบรรทัดล่าง
    // void OnDisable() { OSF_Runtime.I?.StopTracking(); }
}
