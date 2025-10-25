// PlayerInteractor.cs
// Add hover for ItemUsePuzzleTarget (shows name; F to use).
// Keeps existing pickup hover. Also toggles OutlineHighlighter on the root of puzzles.

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

    PickableItem currentHoverPickup;
    ItemUsePuzzleTarget currentHoverPuzzle;

    OutlineHighlighter _lastHoverHL;

    public void SetActiveSlot(int slot)
    {
        activeSlot = Mathf.Clamp(slot, 0, 4);
    }

    void Reset()
    {
        if (!playerCamera) playerCamera = Camera.main;
        if (!inventory) inventory = GetComponent<PlayerInventory>();
    }

    void Update()
    {
        UpdateHover();

        if (currentHoverPickup && Input.GetKeyDown(pickKey))
            TryPickCurrent();

        // number keys 1..5
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
        // 1) Check for pickable first (keeps your original behavior)
        var hitPickup = RaycastFor<PickableItem>(interactRange, interactMask);

        // 2) If no pickable, check for puzzle target (child) or interface on parent
        ItemUsePuzzleTarget hitPuzzle = null;
        if (!hitPickup)
            hitPuzzle = RaycastFor<ItemUsePuzzleTarget>(interactRange, interactMask);

        // ----- Toggle pickup highlight exactly like before -----
        if (hitPickup != currentHoverPickup)
        {
            if (currentHoverPickup) currentHoverPickup.SetHighlighted(false);
            currentHoverPickup = hitPickup;
            if (currentHoverPickup) currentHoverPickup.SetHighlighted(true);
        }

        // ----- Handle OutlineHighlighter (root) for puzzles on hover -----
        var newHL = hitPuzzle ? hitPuzzle.GetComponentInParent<OutlineHighlighter>() : null;
        if (newHL != _lastHoverHL)
        {
            if (_lastHoverHL) _lastHoverHL.SetHoverActive(false);
            if (newHL)        newHL.SetHoverActive(true);
            _lastHoverHL = newHL;
        }

        // ----- Build hover UI label -----
        if (currentHoverPickup)
        {
            string label = $"<b>{currentHoverPickup.Item.DisplayName}</b>\n<alpha=#AA>Press [{pickKey}]";
            hoverUI?.Show(label); // ItemHoverUI.Show(string) provided in your project
            currentHoverPuzzle = null;
        }
        else if (hitPuzzle)
        {
            currentHoverPuzzle = hitPuzzle;
            string label = $"<b>{hitPuzzle.PuzzleName}</b>\n<alpha=#AA>Press [{useKey}]";
            hoverUI?.Show(label);
        }
        else
        {
            currentHoverPuzzle = null;
            hoverUI?.Hide();
        }
    }

    void TryPickCurrent()
    {
        if (!currentHoverPickup || !inventory) return;

        if (inventory.TryAdd(currentHoverPickup.Item))
        {
            currentHoverPickup.SetHighlighted(false);
            Destroy(currentHoverPickup.gameObject);
            currentHoverPickup = null;
            hoverUI?.Hide();
        }
        else
        {
            hoverUI?.Show("<b>Bag full (max 5)</b>");
        }
    }

    void TryUseActiveOnTarget()
    {
        if (!inventory) return;
        if (activeSlot < 0 || activeSlot >= inventory.Items.Count) return;

        var selectedItem = inventory.Items[activeSlot];
        if (!selectedItem) return;

        // Find any IItemUseHandler (child/parent) at the crosshair
        var target = RaycastForComponentOrParent<IItemUseHandler>(interactRange, interactMask);
        if (target == null) return;

        if (target.CanUseItem(selectedItem))
        {
            target.UseItem(selectedItem, inventory);
        }
        else
        {
            hoverUI?.Show("<b>Can't use that here</b>");
        }
    }

    // -------- Ray helpers (as in your original file) --------
    T RaycastFor<T>(float range, LayerMask mask) where T : Component
    {
        if (!playerCamera) return null;
        var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out var hit, range, mask, QueryTriggerInteraction.Collide))
            return hit.collider.GetComponentInParent<T>();
        return null;
    }

    T RaycastForComponentOrParent<T>(float range, LayerMask mask) where T : class
    {
        if (!playerCamera) return null;

        var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out var hit, range, mask, QueryTriggerInteraction.Collide))
        {
            // exact object
            var asComp = hit.collider.GetComponent(typeof(T)) as T;
            if (asComp != null) return asComp;

            // walk up parents
            var t = hit.collider.transform;
            while (t != null)
            {
                var maybe = t.GetComponent(typeof(T)) as T;
                if (maybe != null) return maybe;
                t = t.parent;
            }

            // search children (covers “socket child under big wall mesh” case)
            var mbs = hit.collider.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < mbs.Length; i++)
                if (mbs[i] is T found) return found;
        }
        return null;
    }
}
