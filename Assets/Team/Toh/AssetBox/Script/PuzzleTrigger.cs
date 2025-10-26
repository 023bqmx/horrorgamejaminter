// === PuzzleTrigger.cs ===
// กด E ขณะ "อยู่ในระยะ" เพื่อ Toggle puzzle + เปิด/ปิด GameObject เป้าหมาย (ทางลัด SetActive)
using UnityEngine;

public class PuzzleTrigger : MonoBehaviour
{
    [Header("References")]
    public PuzzleManager puzzleManager;
    public GameObject interactPrompt;                // "Press E"
    public string playerTag = "Player";

    [Header("Target to toggle (optional)")]
    [Tooltip("ถ้าเว้นว่าง จะไม่ยุ่งกับ GameObject อื่น ใช้แค่ PuzzleManager")]
    public GameObject targetToToggle;

    [Header("Config")]
    public KeyCode key = KeyCode.E;
    [Tooltip("ตั้ง > 0 เพื่อใช้ระยะ (ไม่ต้องมี Trigger) | ตั้ง = 0 เพื่อใช้ Trigger Collider")]
    public float range = 0f;
    public bool autoCloseOnExit = true;

    bool playerInRange = false;
    Transform _player;

    void Start()
    {
        if (interactPrompt) interactPrompt.SetActive(false);

        // Sync สถานะเริ่มต้นกับ Manager
        bool startActive = puzzleManager && puzzleManager.IsActive();
        if (targetToToggle) targetToToggle.SetActive(startActive);
    }

    void Update()
    {
        // โหมดวัดระยะ (ไม่ใช้ Trigger)
        if (range > 0f)
        {
            if (!_player)
            {
                var p = GameObject.FindGameObjectWithTag(playerTag);
                if (p) _player = p.transform;
            }

            playerInRange = _player &&
                            (Vector3.SqrMagnitude(_player.position - transform.position) <= range * range);

            if (interactPrompt) interactPrompt.SetActive(playerInRange && !IsOpen());
        }

        if (!playerInRange || puzzleManager == null) return;

        if (Input.GetKeyDown(key))
        {
            // Toggle ผ่าน Manager เป็นแหล่งความจริงเดียว
            if (IsOpen()) Close();
            else Open();
        }
    }

    void Open()
    {
        puzzleManager.ActivatePuzzle();
        if (targetToToggle) targetToToggle.SetActive(true);
        if (interactPrompt) interactPrompt.SetActive(false);
    }

    void Close()
    {
        puzzleManager.DeactivatePuzzle();
        if (targetToToggle) targetToToggle.SetActive(false);
        if (playerInRange && interactPrompt) interactPrompt.SetActive(true);
    }

    bool IsOpen() => puzzleManager && puzzleManager.IsActive();

    // ---------- ใช้ร่วมกับ Trigger Collider ----------
    void OnTriggerEnter(Collider other)
    {
        if (range > 0f) return;                     // ใช้โหมดระยะอยู่
        if (!other.CompareTag(playerTag)) return;

        playerInRange = true;
        if (interactPrompt) interactPrompt.SetActive(!IsOpen());
    }

    void OnTriggerExit(Collider other)
    {
        if (range > 0f) return;
        if (!other.CompareTag(playerTag)) return;

        playerInRange = false;
        if (interactPrompt) interactPrompt.SetActive(false);
        if (autoCloseOnExit && IsOpen()) Close();
    }

    void OnDrawGizmosSelected()
    {
        if (range <= 0f) return;
        Gizmos.color = new Color(0f, 1f, 1f, .35f);
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
