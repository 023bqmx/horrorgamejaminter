// ItemDefinition.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item Definition", fileName = "NewItem")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField, HideInInspector] string id;          // keep unique but not hand-edited
    [SerializeField] string displayName = "Item";
    [SerializeField] Sprite icon;

    [Header("Behavior")]
    [Tooltip("If true, the item is removed after a successful Use on a target.")]
    [SerializeField] bool consumableOnUse = true;

    public string Id => id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public Sprite Icon => icon;
    public bool ConsumableOnUse => consumableOnUse;

#if UNITY_EDITOR
    // Ensure the ID is stable & unique:
    // - Prefer the asset's .meta GUID (unique per asset, stays stable across moves/renames)
    // - If not available (e.g., during temporary creation), fall back to a GUID
    void OnValidate()
    {
        // Keep displayName in sync with asset name if blank
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = name;

        var path = UnityEditor.AssetDatabase.GetAssetPath(this);
        var metaGuid = !string.IsNullOrEmpty(path)
            ? UnityEditor.AssetDatabase.AssetPathToGUID(path)
            : null;

        if (!string.IsNullOrEmpty(metaGuid))
        {
            if (id != metaGuid)
            {
                id = metaGuid;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
        else if (string.IsNullOrEmpty(id))
        {
            id = System.Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }

    // Handy context menu to force a new random ID (rarely needed)
    [ContextMenu("Inventory/Regenerate Random ID")]
    void RegenerateRandomId()
    {
        id = System.Guid.NewGuid().ToString();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
