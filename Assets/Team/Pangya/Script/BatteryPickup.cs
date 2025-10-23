// BatteryPickup.cs
using UnityEngine;

public class BatteryPickup : MonoBehaviour
{
    public enum Mode { AddSeconds, ReplaceWithCapacity }

    [SerializeField] Mode mode = Mode.AddSeconds;
    [SerializeField, Min(0.1f)] float addSeconds = 60f;
    [SerializeField, Min(1f)] float newCapacitySeconds = 300f;
    [SerializeField] bool fillNewCapacity = true;

    [Tooltip("If null, will try to find on the entering collider or its parents.")]
    [SerializeField] FlashlightController target;

    [SerializeField] bool destroyOnUse = true;

    void OnTriggerEnter(Collider other)
    {
        var tgt = target ? target : other.GetComponentInParent<FlashlightController>();
        if (!tgt) return;

        if (mode == Mode.AddSeconds)
        {
            tgt.AddChargeSeconds(addSeconds);
        }
        else
        {
            tgt.ReplaceBattery(newCapacitySeconds, fillNewCapacity);
        }

        if (destroyOnUse) Destroy(gameObject);
    }
}
