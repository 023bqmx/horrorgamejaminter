// BatteryItemDefinition.cs
// ScriptableObject battery item with only the fields you asked for.
// Use PickableItem to collect it into the bag. When used, it will either:
// - Add "Seconds To Use" to the flashlight, if > 0
// - Otherwise replace the battery capacity (full), if "Seconds To Use" == 0

using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Battery Item", fileName = "BatteryItem")]
public class BatteryItemDefinition : ItemDefinition
{
    [Header("Battery")]
    [Tooltip("New total capacity (seconds) if this battery REPLACES the old one. Used when Seconds To Use = 0.")]
    [SerializeField, Min(1f)] float capacitySeconds = 300f;

    [Tooltip("If > 0, using this item will ADD this many seconds instead of replacing capacity.")]
    [SerializeField, Min(0f)] float secondsToUse = 90f;

    public float CapacitySeconds => capacitySeconds;
    public float SecondsToUse   => secondsToUse;
}
