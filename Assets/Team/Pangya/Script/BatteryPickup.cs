// BatteryPickup.cs — v2
// Adds inventory-friendly pickup modes while keeping the original recharge behavior.

using UnityEngine;

public class BatteryPickup : MonoBehaviour
{
    public enum Mode
    {
        AddSeconds,             // (old) add charge seconds to nearby flashlight
        ReplaceWithCapacity,    // (old) replace battery capacity (optionally fill)
        GiveInventoryItem,      // NEW: give a Battery item to PlayerInventory
        AutoRechargeElseGiveItem// NEW: recharge if flashlight present; otherwise give item
    }

    [Header("Behavior")]
    [SerializeField] Mode mode = Mode.AddSeconds;

    [Header("Recharge (legacy modes)")]
    [SerializeField, Min(0.1f)] float addSeconds = 60f;
    [SerializeField, Min(1f)] float newCapacitySeconds = 300f;
    [SerializeField] bool fillNewCapacity = true;

    [Header("Inventory (new modes)")]
    [Tooltip("ItemDefinition asset representing a single Battery item (consumable).")]
    [SerializeField] ItemDefinition batteryItem;
    [SerializeField, Min(1)] int amountToGive = 1;
    [Tooltip("Show a tiny message when picked up (optional).")]
    [SerializeField] ItemHoverUI hoverUI;

    [Header("Target search")]
    [Tooltip("If null, we search on the entering collider or its parents.")]
    [SerializeField] FlashlightController target;
    [Tooltip("Require the entering object (or its root) to have this tag before applying. Empty = no check.")]
    [SerializeField] string requiredTag = "Player";

    [Header("Lifecycle")]
    [SerializeField] bool destroyOnUse = true;

    void OnTriggerEnter(Collider other)
    {
        // Optional tag gate
        if (!string.IsNullOrEmpty(requiredTag))
        {
            var root = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;
            if (!root.CompareTag(requiredTag)) return;
        }

        // Find flashlight + inventory
        var tgt = target ? target : other.GetComponentInParent<FlashlightController>();
        var inv = other.GetComponentInParent<PlayerInventory>();

        switch (mode)
        {
            case Mode.AddSeconds:
                if (!tgt) return;
                tgt.AddChargeSeconds(addSeconds);
                break;

            case Mode.ReplaceWithCapacity:
                if (!tgt) return;
                tgt.ReplaceBattery(newCapacitySeconds, fillNewCapacity);
                break;

            case Mode.GiveInventoryItem:
                if (!inv || !batteryItem) return;
                GiveBatteriesToInventory(inv);
                break;

            case Mode.AutoRechargeElseGiveItem:
                if (tgt)
                {
                    tgt.AddChargeSeconds(addSeconds);
                }
                else
                {
                    if (!inv || !batteryItem) return;
                    GiveBatteriesToInventory(inv);
                }
                break;
        }

        if (destroyOnUse) Destroy(gameObject);
    }

    void GiveBatteriesToInventory(PlayerInventory inv)
    {
        int given = 0;
        for (int i = 0; i < amountToGive; i++)
        {
            if (inv.TryAdd(batteryItem)) // uses your existing bag logic
                given++;
            else
                break; // bag full
        }

        if (given > 0 && hoverUI)
            hoverUI.Show($"<b>Picked {given}× {batteryItem.DisplayName}</b>");
    }
}
