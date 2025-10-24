using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// หันกล้องตาม "ตำแหน่ง" เมาส์แบบเบา ๆ (ไม่ใช่ตามความเร็ว)
/// ใช้กับ Main Menu: แปะไว้ที่ Main Camera
/// </summary>
public class MainMenuLookAtMouse : MonoBehaviour
{
    [Header("Limits (degrees)")]
    [Tooltip("เอียงซ้ายขวาสูงสุด (Yaw)")]
    public float MaxYaw = 5f;
    [Tooltip("เงย/ก้มสูงสุด (Pitch)")]
    public float MaxPitch = 3f;
    [Tooltip("เอียงจอ Z (Roll) ตาม X")]
    public float MaxRoll = 2f;

    [Header("Smoothing")]
    [Tooltip("ความลื่นในการตามเป้าหมาย (มาก = นุ่ม)")]
    public float FollowSmooth = 8f;

    [Header("Options")]
    [Tooltip("ใช้เวลาที่ไม่โดน timeScale (เผื่อเมนู pause)")]
    public bool UseUnscaledTime = true;
    [Tooltip("คูณความไวของตำแหน่งเมาส์ (1 = พอดี)")]
    public float Sensitivity = 1f;
    [Tooltip("หันใน localRotation (ควรเปิดไว้สำหรับกล้องที่วางมุมไว้แล้ว)")]
    public bool UseLocalRotation = true;

    private Quaternion _baseRot;
    private float _curYaw, _curPitch, _curRoll;

    float Dt => UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    void OnEnable()
    {
        _baseRot = UseLocalRotation ? transform.localRotation : transform.rotation;
    }

    void Update()
    {
        // 1) อ่าน "ตำแหน่ง" เมาส์ และทำเป็นช่วง -1..1 (กลางจอ = 0)
        Vector2 mp = ReadMousePosition();               // พิกัดจอเป็นพิกเซล
        float nx = 0f, ny = 0f;
        if (Screen.width > 0 && Screen.height > 0)
        {
            nx = ((mp.x / Screen.width)  - 0.5f) * 2f;  // -1..1
            ny = ((mp.y / Screen.height) - 0.5f) * 2f;  // -1..1
        }

        nx *= Sensitivity;
        ny *= Sensitivity;

        // 2) คำนวณมุมเป้าหมาย (หันนิดๆ)
        float targetYaw   = Mathf.Clamp(nx * MaxYaw,   -MaxYaw,   MaxYaw);   // ซ้าย/ขวา
        float targetPitch = Mathf.Clamp(-ny * MaxPitch, -MaxPitch, MaxPitch); // ขึ้น/ลง (เมาส์ขึ้น = กล้องเงยนิดๆ)
        float targetRoll  = Mathf.Clamp(nx * MaxRoll,  -MaxRoll,  MaxRoll);  // เอียงจอตาม X

        // 3) ไล่เข้าอย่างนุ่มนวล
        _curYaw   = Mathf.Lerp(_curYaw,   targetYaw,   Dt * FollowSmooth);
        _curPitch = Mathf.Lerp(_curPitch, targetPitch, Dt * FollowSmooth);
        _curRoll  = Mathf.Lerp(_curRoll,  targetRoll,  Dt * FollowSmooth);

        // 4) ประกอบหมุมเข้ากับฐาน
        Quaternion offset = Quaternion.Euler(_curPitch, _curYaw, _curRoll);
        if (UseLocalRotation) transform.localRotation = _baseRot * offset;
        else                  transform.rotation      = _baseRot * offset;
    }

    Vector2 ReadMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null) return Mouse.current.position.ReadValue();
#endif
        return Input.mousePosition;
    }
}
