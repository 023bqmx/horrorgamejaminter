// PlayerInteractor.cs
// Unity 6 — Picks items, shows hover, uses bag items on puzzles,
// opens UI-mode puzzles without selecting an item, toggles root OutlineHighlighter on hover,
// and (if no target) tries to use a battery in the active slot via FlashlightInventoryBinder.

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
    [SerializeField] int activeSlot = 0; // 0..4
    public int ActiveSlot => activeSlot;

    // Hover state
    PickableItem currentHoverPickup;
    ItemUsePuzzleTarget currentHoverPuzzle;
    OutlineHighlighter _lastHoverHL;

    // ===== เพิ่มฟิลด์ตั้งค่า/อ้างอิงด้านบนของคลาส =====
    [Header("Lever (hold-to-pick)")]
    [SerializeField, Min(0.2f)] float leverPickupSeconds = 10f;      // ต้องกดค้างรวมกันกี่วิ
    [SerializeField] CrosshairHoldRing holdRing;                       // วงแหวนรอบ crosshair

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

        // ===== ใน Update() แก้บล็อกกดเก็บของเดิม =====
        // เดิม: if (currentHoverPickup && Input.GetKeyDown(pickKey)) TryPickCurrent();
        // แทนที่ด้วย:
        if (currentHoverPickup)
        {
            var def = currentHoverPickup.Item;
            if (def != null && def.Lever)
                UpdateLeverPickup();                // กดค้างแบบมี progress
            else
            {
                // พฤติกรรมเดิม: กดครั้งเดียวเก็บ
                if (Input.GetKeyDown(pickKey)) TryPickCurrent();
                // ซ่อนวงแหวนถ้าไม่ใช่โหมด Lever
                if (holdRing) holdRing.HideImmediate();
            }
        }
        else
        {
            if (holdRing) holdRing.HideImmediate();
        }

        // number keys 1..5
        if (Input.GetKeyDown(KeyCode.Alpha1)) activeSlot = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) activeSlot = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) activeSlot = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) activeSlot = 3;
        if (Input.GetKeyDown(KeyCode.Alpha5)) activeSlot = 4;

        if (Input.GetKeyDown(useKey))
            TryUseActiveOnTarget();
    }
    void UpdateLeverPickup()
    {
        if (!currentHoverPickup) { if (holdRing) holdRing.HideImmediate(); return; }

        // ดึง/สร้างตัวเก็บ progress ไว้บนชิ้นของนั้น ๆ
        var prog = currentHoverPickup.GetComponent<LeverPickupProgress>();
        if (!prog) prog = currentHoverPickup.gameObject.AddComponent<LeverPickupProgress>();

        // อัปเดต UI hover ให้บอกว่า "Hold" ไม่ใช่ "Press"
        // (ตัว Show หลักทำใน UpdateHover() อยู่แล้ว ด้านล่างเราจะโชว์วงแหวน)
        float needed = Mathf.Max(0.01f, leverPickupSeconds);

        if (Input.GetKey(pickKey))
        {
            prog.secondsAccumulated = Mathf.Min(prog.secondsAccumulated + Time.deltaTime, needed);
        }
        // ไม่ลด progress เมื่อปล่อยปุ่ม (ตามที่ต้องการ)

        float t = prog.secondsAccumulated / needed;
        if (holdRing) holdRing.SetProgress(t);

        // ครบหลอด -> เก็บของ
        if (t >= 1f)
        {
            if (holdRing) holdRing.HideImmediate();
            TryPickCurrent();
        }
    }

    void UpdateHover()
    {
        // 1) Check for pickable first (keeps classic behavior)
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
            bool isLever = currentHoverPickup.Item != null && currentHoverPickup.Item.Lever;
            string action = isLever ? "Hold" : "Press";
            string label = $"<b>{currentHoverPickup.Item.DisplayName}</b>\n<alpha=#AA>{action} [{pickKey}]";
            hoverUI?.Show(label);

            // ถ้าเป็น Lever แต่ยังไม่มี progress ให้โชว์วงแหวนเปล่า (ไม่บังคับ)
            if (isLever && holdRing) holdRing.ShowEmpty();

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

            // turn off last hover HL if aim moved away
            if (_lastHoverHL)
            {
                _lastHoverHL.SetHoverActive(false);
                _lastHoverHL = null;
            }
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

        // Find any IItemUseHandler (child/parent) at the crosshair
        var target = RaycastForComponentOrParent<IItemUseHandler>(interactRange, interactMask);

        // Get selected item if any
        ItemDefinition selectedItem = (activeSlot >= 0 && activeSlot < inventory.Items.Count)
            ? inventory.Items[activeSlot] : null;

        if (target == null)
        {
            // NEW: allow using battery directly from bag when nothing is targeted
            var binder = FindFirstObjectByType<FlashlightInventoryBinder>(FindObjectsInactive.Exclude);
            if (binder && selectedItem != null)
            {
                // binder will validate it's a battery; returns true if consumed
                if (binder.UseBatteryFromInventorySlot(activeSlot))
                {
                    hoverUI?.Show("<b>Flashlight reloaded</b>");
                    return;
                }
            }
            return;
        }

        // If no item selected, allow ItemUsePuzzleTarget (UI mode) to open via Interact()
        if (selectedItem == null && target is ItemUsePuzzleTarget pit)
        {
            pit.Interact();    // safe: Interact() itself checks mode == OpenUIPuzzle
            return;
        }

        // Otherwise use the item normally
        if (selectedItem != null && target.CanUseItem(selectedItem))
        {
            target.UseItem(selectedItem, inventory);
        }
        else
        {
            hoverUI?.Show("<b>Can't use that here</b>");
        }
    }

    // -------- Ray helpers --------

    // Prefer non-triggers first; fallback includes triggers.
    T RaycastFor<T>(float range, LayerMask mask) where T : Component
    {
        if (!playerCamera) return null;
        var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        // Prefer non-triggers
        if (Physics.Raycast(ray, out var hit, range, mask, QueryTriggerInteraction.Ignore))
            return hit.collider.GetComponentInParent<T>();

        // Fallback including triggers
        if (Physics.Raycast(ray, out hit, range, mask, QueryTriggerInteraction.Collide))
            return hit.collider.GetComponentInParent<T>();

        return null;
    }

    // Finds interface/class on the hit, parents, the OutlineHighlighter root's children, or children of the hit.
    T RaycastForComponentOrParent<T>(float range, LayerMask mask) where T : class
    {
        if (!playerCamera) return null;
        var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        // Prefer non-triggers
        if (!Physics.Raycast(ray, out var hit, range, mask, QueryTriggerInteraction.Ignore))
        {
            // Then include triggers
            if (!Physics.Raycast(ray, out hit, range, mask, QueryTriggerInteraction.Collide))
                return null;
        }

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

        // If we hit a proximity trigger, jump to the group root and search all children
        var groupRoot = hit.collider.GetComponentInParent<OutlineHighlighter>();
        if (groupRoot)
        {
            var found = groupRoot.GetComponentInChildren(typeof(T), true) as T;
            if (found != null) return found;
        }

        // Last resort: search children under the hit object itself
        var mbs = hit.collider.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < mbs.Length; i++)
            if (mbs[i] is T foundChild) return foundChild;

        return null;
    }
}
