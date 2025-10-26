using UnityEngine;

[DefaultExecutionOrder(-1000)] // ให้รันก่อนสคริปต์อื่น ๆ
public class MouseLocker : MonoBehaviour
{
    void Awake() => LockCursor();   // เข้าซีนปุ๊บล็อคเลย
    void OnEnable() => LockCursor();

    void Update()
    {
        // ปลดล็อคชั่วคราว (ทดสอบใน Editor)
        if (Input.GetKeyDown(KeyCode.Escape)) UnlockCursor();

        // ล็อคกลับเมื่อพร้อม (เช่น กด F1)
        if (Input.GetKeyDown(KeyCode.F1)) LockCursor();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        // สลับหน้าต่างแล้วกลับมา ให้ล็อคให้อีก
        if (hasFocus) LockCursor();
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked; // ล็อคไว้กลางจอ
        Cursor.visible = false;                    // ซ่อนเคอร์เซอร์
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;    // ปลดล็อค
        Cursor.visible = true;                     // แสดงเคอร์เซอร์
    }
}
