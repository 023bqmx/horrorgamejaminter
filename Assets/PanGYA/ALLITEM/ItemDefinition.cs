using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item Definition", fileName = "NewItem")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] string id = System.Guid.NewGuid().ToString();
    [SerializeField] string displayName = "Item";
    [SerializeField] Sprite icon;

    [Header("Behavior")]
    [Tooltip("If true, the item is removed after a successful Use on a target.")]
    [SerializeField] bool consumableOnUse = true;

    public string Id => id;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public bool ConsumableOnUse => consumableOnUse;
}
