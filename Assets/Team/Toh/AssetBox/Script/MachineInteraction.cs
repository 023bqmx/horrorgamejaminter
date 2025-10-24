using UnityEngine;
using UnityEngine.Playables;

public class MachineInteraction : MonoBehaviour
{
    public PlayableDirector timeline;   // Assign your timeline here
    private bool isPlayerNearby = false;

    void Update()
    {
        // If player is near and presses E
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            if (timeline != null)
            {
                timeline.Play();
                Debug.Log("Machine activated!");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            Debug.Log("Player near machine");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
        }
    }
}