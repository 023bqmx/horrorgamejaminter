using UnityEngine;

[DisallowMultipleComponent]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Camera playerCamera;
    [SerializeField] PlayerInventory inventory;
    [SerializeField] ItemHoverUI hoverUI;

    [Header("Interact")]
    [SerializeField, Min(0.5f)] float interactRange = 3.0f;
    [SerializeField] LayerMask interactMask = ~0; // everything
    [SerializeField] KeyCode pickKey = KeyCode.E;
    [SerializeField] KeyCode useKey  = KeyCode.F;

    [Header("Selection (for using items)")]
    [Tooltip("Number keys 1-5 select which item to Use on a target with the Use key.")]

    public int ActiveSlot => activeSlot;
    [SerializeField] int activeSlot = 0; // 0..4
    

    PickableItem currentHover;

    void Reset()
    {
        if (!playerCamera) playerCamera = Camera.main;
        if (!inventory) inventory = GetComponent<PlayerInventory>();
    }

    void Update()
    {
        UpdateHover();

        if (currentHover && Input.GetKeyDown(pickKey))
            TryPickCurrent();

        // Select active slot with number keys 1..5
        if (Input.GetKeyDown(KeyCode.Alpha1)) activeSlot = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) activeSlot = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) activeSlot = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) activeSlot = 3;
        if (Input.GetKeyDown(KeyCode.Alpha5)) activeSlot = 4;

        if (Input.GetKeyDown(useKey))
            TryUseActiveOnTarget();
    }

    void UpdateHover()
    {
        var hitItem = RaycastFor<PickableItem>(interactRange, interactMask);

        if (hitItem != currentHover)
        {
            if (currentHover) currentHover.SetHighlighted(false);
            currentHover = hitItem;
            if (currentHover) currentHover.SetHighlighted(true);
        }

        if (currentHover)
        {
            string label = $"<b>{currentHover.Item.DisplayName}</b>\n<alpha=#AA>Press[{pickKey}]";
            hoverUI?.Show(label);
        }
        else
        {
            hoverUI?.Hide();
        }
    }

    void TryPickCurrent()
    {
        if (!currentHover || !inventory) return;

        if (inventory.TryAdd(currentHover.Item))
        {
            currentHover.SetHighlighted(false);
            Destroy(currentHover.gameObject);
            currentHover = null;
            hoverUI?.Hide();
        }
        else
        {
            // Optional: flash "Bag full" somewhere in your UI
            hoverUI?.Show("<b>Bag full (max 5)</b>");
        }
    }

    void TryUseActiveOnTarget()
    {
        if (!inventory) return;
        if (activeSlot < 0 || activeSlot >= inventory.Items.Count) return;

        var selectedItem = inventory.Items[activeSlot];
        if (!selectedItem) return;

        var target = RaycastForComponentOrParent<IItemUseHandler>(interactRange, interactMask);
        if (target == null) return;

        if (target.CanUseItem(selectedItem))
        {
            target.UseItem(selectedItem, inventory);
        }
        else
        {
            // Optional: feedback (wrong item)
            hoverUI?.Show("<b>Can't use that here</b>");
        }
    }

    T RaycastFor<T>(float range, LayerMask mask) where T : Component
    {
        if (!playerCamera) return null;
        var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out var hit, range, mask, QueryTriggerInteraction.Collide))
            return hit.collider.GetComponentInParent<T>();
        return null;
    }

    // Helper that finds interface on hit object or parents
    T RaycastForComponentOrParent<T>(float range, LayerMask mask) where T : class
    {
        if (!playerCamera) return null;
        var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out var hit, range, mask, QueryTriggerInteraction.Collide))
        {
            // Try on this object
            var asComp = hit.collider.GetComponent(typeof(T)) as T;
            if (asComp != null) return asComp;

            // Or any parent
            var t = hit.collider.transform;
            while (t != null)
            {
                var maybe = t.GetComponent(typeof(T)) as T;
                if (maybe != null) return maybe;
                t = t.parent;
            }
        }
        return null;
    }
}
