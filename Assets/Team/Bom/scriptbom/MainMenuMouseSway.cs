using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// ทำเอฟเฟกต์ส่ายกล้องเบาๆ ตามการขยับเมาส์
/// ใช้ใน Main Menu ได้เลย (ไม่ต้องมี Player) — แปะที่ Main Camera
/// ปลอดภัย: ถอด offset เก่าก่อนใส่ใหม่ทุกเฟรม จึงไม่เกิดอาการ drift
/// </summary>
public class MainMenuMouseSway : MonoBehaviour
{
    [Header("Strength")]
    [Tooltip("คูณความแรงจาก mouse delta -> องศาเอียง")]
    public float SwayAmount = 0.05f;

    [Tooltip("จำกัดองศาสูงสุดของ yaw/pitch (ซ้ายขวา / ขึ้นลง)")]
    public Vector2 MaxAngles = new Vector2(2.0f, 2.0f); // x = maxYaw, y = maxPitch

    [Tooltip("แรงเอียงจอ (roll) จากการเลื่อนเมาส์ซ้ายขวา")]
    public float RollAmount = 0.8f;

    [Header("Smoothing")]
    [Tooltip("ความไวในการตามเป้าหมาย (ยิ่งมากยิ่งนุ่ม)")]
    public float FollowSpeed = 14f;

    [Tooltip("ความไวในการเด้งกลับศูนย์เมื่อไม่ขยับเมาส์")]
    public float ReturnSpeed = 10f;

    [Header("Quality of Life")]
    [Tooltip("ถ้า true จะใช้เวลาแบบไม่โดน Time.timeScale (เผื่อเมนูหยุดเวลา)")]
    public bool UseUnscaledTime = true;

    [Tooltip("ล็อกให้ส่ายเฉพาะในแกน local (ควรเปิดไว้)")]
    public bool UseLocalRotation = true;

    // target offsets (degrees)
    private float _targetYaw;   // Y (ซ้าย/ขวา)
    private float _targetPitch; // X (ขึ้น/ลง)
    private float _targetRoll;  // Z (เอียงจอ)

    // smoothed (current) offsets
    private float _curYaw;
    private float _curPitch;
    private float _curRoll;

    // last applied offset for undo
    private Quaternion _lastOffset = Quaternion.identity;

    private float Dt => UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    private void OnDisable()
    {
        // ถอด offset ทิ้งเมื่อปิดคอมโพเนนต์
        var t = transform;
        if (UseLocalRotation) t.localRotation = t.localRotation * Quaternion.Inverse(_lastOffset);
        else t.rotation = t.rotation * Quaternion.Inverse(_lastOffset);
        _lastOffset = Quaternion.identity;

        _curYaw = _curPitch = _curRoll = 0f;
        _targetYaw = _targetPitch = _targetRoll = 0f;
    }

    private void Update()
    {
        Vector2 delta = ReadMouseDelta();

        // เป้าหมายใหม่ (อินเวิร์ตนิดให้ฟีลส่าย)
        float targetYaw   = Mathf.Clamp(-delta.x * SwayAmount, -MaxAngles.x, MaxAngles.x);
        float targetPitch = Mathf.Clamp( delta.y * SwayAmount, -MaxAngles.y, MaxAngles.y);
        float targetRoll  = Mathf.Clamp(-delta.x * SwayAmount * RollAmount, -MaxAngles.x, MaxAngles.x);

        bool hasInput = delta.sqrMagnitude > 0.0001f;
        float lerpSpeed = (hasInput ? FollowSpeed : ReturnSpeed);

        _targetYaw   = targetYaw;
        _targetPitch = targetPitch;
        _targetRoll  = targetRoll;

        _curYaw   = Mathf.Lerp(_curYaw, _targetYaw,   Dt * lerpSpeed);
        _curPitch = Mathf.Lerp(_curPitch, _targetPitch, Dt * lerpSpeed);
        _curRoll  = Mathf.Lerp(_curRoll, _targetRoll,  Dt * lerpSpeed);

        // ถอดออฟเซ็ตเก่าก่อน แล้วใส่ออฟเซ็ตใหม่
        var t = transform;
        if (UseLocalRotation)
        {
            t.localRotation = t.localRotation * Quaternion.Inverse(_lastOffset);
            Quaternion newOffset = Quaternion.Euler(_curPitch, _curYaw, _curRoll);
            t.localRotation = t.localRotation * newOffset;
            _lastOffset = newOffset;
        }
        else
        {
            t.rotation = t.rotation * Quaternion.Inverse(_lastOffset);
            Quaternion newOffset = Quaternion.Euler(_curPitch, _curYaw, _curRoll);
            t.rotation = t.rotation * newOffset;
            _lastOffset = newOffset;
        }
    }

    private Vector2 ReadMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            // delta เฟรมนี้ (px) — ทำให้ฟีลใกล้เคียงทุกแพลตฟอร์ม
            return Mouse.current.delta.ReadValue();
        }
#endif
        // Fallback (Legacy Input)
        float dx = Input.GetAxis("Mouse X");
        float dy = Input.GetAxis("Mouse Y");
        return new Vector2(dx, dy) * 6f;
    }
}
