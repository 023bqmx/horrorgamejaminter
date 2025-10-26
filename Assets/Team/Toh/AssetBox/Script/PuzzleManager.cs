// === PuzzleManager.cs ===
using UnityEngine;
using UnityEngine.Events;

public class PuzzleManager : MonoBehaviour
{
    [Header("Puzzle UI")]
    public GameObject puzzleUI;

    [Header("Lifecycle Events (optional)")]
    public UnityEvent onActivated;
    public UnityEvent onDeactivated;

    bool isActive = false;

    void Start()
    {
        if (puzzleUI) puzzleUI.SetActive(false);
        isActive = false;
    }

    public void ActivatePuzzle()
    {
        if (isActive) return;
        if (puzzleUI) puzzleUI.SetActive(true);

        // --- ถ้าต้องการ lock เมาส์/หยุดเวลา ให้ uncomment ---
        // Cursor.lockState = CursorLockMode.None;
        // Cursor.visible = true;
        // Time.timeScale = 0f;

        isActive = true;
        onActivated?.Invoke();
        Debug.Log("[PuzzleManager] Activated");
    }

    public void DeactivatePuzzle()
    {
        if (!isActive) return;
        if (puzzleUI) puzzleUI.SetActive(false);

        // --- ถ้าต้องการคืนค่า cursor/time ---
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
        // Time.timeScale = 1f;

        isActive = false;
        onDeactivated?.Invoke();
        Debug.Log("[PuzzleManager] Deactivated");
    }

    public void TogglePuzzle()
    {
        if (isActive) DeactivatePuzzle();
        else ActivatePuzzle();
    }

    public bool IsActive() => isActive;
}
