// FlashlightInventoryBinder.cs
// Uses BatteryItemDefinition from the bag to add seconds or replace capacity.

using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class FlashlightInventoryBinder : MonoBehaviour
{
    [Header("Inventory + Camera")]
    [SerializeField] PlayerInventory inventory;
    [SerializeField] Camera playerCamera;

    [Header("Items")]
    [Tooltip("ItemDefinition asset for the Flashlight (non-consumable).")]
    [SerializeField] ItemDefinition flashlightItem;

    [Header("Flashlight Prefab & Mount")]
    [SerializeField] GameObject flashlightPrefab;
    [SerializeField] Transform mount;
    [SerializeField] Vector3 localPosition = new(0.05f, -0.07f, 0.15f);
    [SerializeField] Vector3 localEulerAngles = Vector3.zero;

    [Header("Reload")]
    [Tooltip("Auto consume a Battery item when flashlight becomes empty.")]
    [SerializeField] bool autoReloadOnEmpty = true;
    [Tooltip("Manual reload key (consume one Battery from bag).")]
    [SerializeField] KeyCode reloadKey = KeyCode.R;

    [Header("Optional UI ping")]
    [SerializeField] ItemHoverUI hoverUI;

    FlashlightController _instance;
    float _syncTimer;

    void Reset()
    {
        if (!inventory) inventory = GetComponent<PlayerInventory>();
        if (!playerCamera) playerCamera = Camera.main;
    }

    void Awake()
    {
        if (!playerCamera) playerCamera = Camera.main;
        if (!mount) mount = playerCamera ? playerCamera.transform : transform;
    }

    void OnEnable()  { SyncAttachment(true); }
    void OnDisable() { UnsubscribeFlashlightEvents(); }

    void Update()
    {
        _syncTimer += Time.deltaTime;
        if (_syncTimer >= 0.25f) { _syncTimer = 0f; SyncAttachment(); }

        if (Input.GetKeyDown(reloadKey))
            UseAnyBatteryFromBag();
    }

    // ---------- Attachment ----------
    void SyncAttachment(bool force = false)
    {
        bool hasFlashlightItem = inventory && inventory.Items.Any(i => SameItem(i, flashlightItem));

        if (hasFlashlightItem && !_instance)
        {
            SpawnAndMountFlashlight();
        }
        else if (!hasFlashlightItem && _instance)
        {
            UnsubscribeFlashlightEvents();
            Destroy(_instance.gameObject);
            _instance = null;
        }
        else if (force && _instance)
        {
            WireInstanceToCamera(_instance);
        }
    }

    void SpawnAndMountFlashlight()
    {
        if (!flashlightPrefab || !mount) return;

        var go = Instantiate(flashlightPrefab, mount);
        go.transform.localPosition = localPosition;
        go.transform.localEulerAngles = localEulerAngles;

        _instance = go.GetComponentInChildren<FlashlightController>(true);
        if (!_instance) { Debug.LogWarning("FlashlightInventoryBinder: prefab missing FlashlightController."); return; }

        WireInstanceToCamera(_instance);

        if (autoReloadOnEmpty)
            _instance.onBatteryEmptied.AddListener(OnFlashlightEmpty);
    }

    void WireInstanceToCamera(FlashlightController f)
    {
        if (!playerCamera) playerCamera = Camera.main;
        var fld = typeof(FlashlightController).GetField("aimCamera",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        fld?.SetValue(f, playerCamera);
    }

    void UnsubscribeFlashlightEvents()
    {
        if (_instance && autoReloadOnEmpty)
            _instance.onBatteryEmptied.RemoveListener(OnFlashlightEmpty);
    }

    // ---------- Battery logic ----------
    void OnFlashlightEmpty()
    {
        if (!autoReloadOnEmpty) return;
        UseAnyBatteryFromBag();
    }

    /// <summary>Consume the first battery found in the bag.</summary>
    public bool UseAnyBatteryFromBag()
    {
        if (!_instance || !inventory) return false;

        var batt = inventory.Items.OfType<BatteryItemDefinition>().FirstOrDefault();
        if (!batt) { hoverUI?.Show("<b>No battery in bag</b>"); return false; }

        if (!ApplyBattery(batt)) return false;

        // Remove the specific ScriptableObject instance from the bag
        inventory.TryRemove(batt);  // bag removal after success :contentReference[oaicite:5]{index=5}
        hoverUI?.Show("<b>Flashlight reloaded</b>");
        return true;
    }

    /// <summary>Consume the battery in a specific slot (for “use from bag” flows).</summary>
    public bool UseBatteryFromInventorySlot(int slot)
    {
        if (!_instance || !inventory) return false;
        if (slot < 0 || slot >= inventory.Items.Count) return false;

        var item = inventory.Items[slot] as BatteryItemDefinition;
        if (!item) return false;

        if (!ApplyBattery(item)) return false;

        inventory.TryRemove(item);  // consume the one actually used :contentReference[oaicite:6]{index=6}
        hoverUI?.Show("<b>Flashlight reloaded</b>");
        return true;
    }

    bool ApplyBattery(BatteryItemDefinition def)
    {
        if (!_instance || def == null) return false;

        // If SecondsToUse > 0 → add seconds; else replace capacity (full)
        if (def.SecondsToUse > 0f)
        {
            _instance.AddChargeSeconds(def.SecondsToUse); // clamp to capacity :contentReference[oaicite:7]{index=7}
        }
        else
        {
            _instance.ReplaceBattery(def.CapacitySeconds, fillToFull: true); // set new capacity & fill :contentReference[oaicite:8]{index=8}
        }
        return true;
    }

    // ---------- Utils ----------
    static bool SameItem(ItemDefinition a, ItemDefinition b)
    {
        if (!a || !b) return false;
        if (ReferenceEquals(a, b)) return true;
        return !string.IsNullOrEmpty(a.Id) && a.Id == b.Id;
    }
}
