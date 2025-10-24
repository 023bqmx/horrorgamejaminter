using UnityEngine;

[DisallowMultipleComponent]
public class LockedDoor : MonoBehaviour, IItemUseHandler
{
    [SerializeField] ItemDefinition requiredItem;
    [SerializeField] Animator animator;
    [SerializeField] string openTrigger = "Open";
    [SerializeField] bool isLocked = true;
    [SerializeField] Collider doorBlocker; // optional collider that blocks passage

    public bool CanUseItem(ItemDefinition item) => isLocked && item && item == requiredItem;

    public void UseItem(ItemDefinition item, PlayerInventory user)
    {
        if (!CanUseItem(item)) return;

        isLocked = false;

        if (animator && !string.IsNullOrEmpty(openTrigger))
            animator.SetTrigger(openTrigger);

        if (doorBlocker) doorBlocker.enabled = false;

        // If consumable, remove from bag.
        if (item.ConsumableOnUse && user)
            user.TryRemove(item);
    }
}
