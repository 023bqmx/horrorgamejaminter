public interface IItemUseHandler
{
    // Return true if this target can accept/use the given item.
    bool CanUseItem(ItemDefinition item);

    // Called when the item is used on this target. The 'user' is passed in case
    // the target wants to remove items / trigger inventory feedback.
    void UseItem(ItemDefinition item, PlayerInventory user);
}
