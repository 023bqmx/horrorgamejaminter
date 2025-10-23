using UnityEngine;

public class MachineShake : MonoBehaviour
{
    [Header("Shake Settings")]
    public float shakeAmount = 0.05f;  // how far it moves
    public float shakeSpeed = 20f;     // how fast it shakes
    public bool bobUpDown = false;     // toggle for up-down bobbing motion

    private Vector3 originalPosition;

    void Start()
    {
        originalPosition = transform.localPosition;
    }

    void Update()
    {
        if (bobUpDown)
        {
            // Smooth up-and-down bobbing
            float newY = originalPosition.y + Mathf.Sin(Time.time * shakeSpeed) * shakeAmount;
            transform.localPosition = new Vector3(originalPosition.x, newY, originalPosition.z);
        }
        else
        {
            // Random shaking
            transform.localPosition = originalPosition + Random.insideUnitSphere * shakeAmount;
        }
    }

    void OnDisable()
    {
        // Reset position when disabled
        transform.localPosition = originalPosition;
    }
}