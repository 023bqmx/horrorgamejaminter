using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    [Header("Puzzle UI")]
    public GameObject puzzleUI;

    private bool isActive = false;

    void Start()
    {
        if (puzzleUI)
            puzzleUI.SetActive(false);
    }

    public void ActivatePuzzle()
    {
        if (isActive) return;

        if (puzzleUI)
            puzzleUI.SetActive(true);

        isActive = true;
        Debug.Log("[PuzzleManager] Puzzle activated!");
    }

    public void DeactivatePuzzle()
    {
        if (!isActive) return;

        if (puzzleUI)
            puzzleUI.SetActive(false);

        isActive = false;
        Debug.Log("[PuzzleManager] Puzzle deactivated!");
    }

    public bool IsActive()
    {
        return isActive;
    }
}
