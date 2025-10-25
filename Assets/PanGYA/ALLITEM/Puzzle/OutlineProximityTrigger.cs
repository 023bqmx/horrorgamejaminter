using UnityEngine;

[RequireComponent(typeof(Collider))]
public class OutlineProximityTrigger : MonoBehaviour
{
    [SerializeField] OutlineHighlighter target; // drag the ROOT’s OutlineHighlighter here

    void Reset()
    {
        if (!target) target = GetComponentInParent<OutlineHighlighter>();
        var c = GetComponent<Collider>();
        c.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) target?.SetProximityActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) target?.SetProximityActive(false);
    }
}
