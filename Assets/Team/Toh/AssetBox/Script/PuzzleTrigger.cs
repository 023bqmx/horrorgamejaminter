using UnityEngine;

public class PuzzleTrigger : MonoBehaviour
{
    [Header("References")]
    public PuzzleManager puzzleManager;
    public GameObject interactPrompt; // "Press E" UI
    public string playerTag = "Player";

    private bool playerInRange = false;

    void Start()
    {
        if (interactPrompt)
            interactPrompt.SetActive(false);
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            puzzleManager?.ActivatePuzzle();

            if (interactPrompt)
                interactPrompt.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            if (interactPrompt)
                interactPrompt.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            if (interactPrompt)
                interactPrompt.SetActive(false);
        }
    }
}
