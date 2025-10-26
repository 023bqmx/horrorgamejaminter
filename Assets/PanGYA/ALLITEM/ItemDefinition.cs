// ItemDefinition.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item Definition", fileName = "NewItem")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField, HideInInspector] string id;
    [SerializeField] string displayName = "Item";
    [SerializeField] Sprite icon;

    [Header("Behavior")]
    [Tooltip("If true, the item is removed after a successful Use on a target.")]
    [SerializeField] bool consumableOnUse = true;

    [Header("Pickup Mode")]
    [Tooltip("ถ้าเปิด = ต้องกดปุ่มค้างเพื่อเก็บ (แบบคันโยก) พร้อมแถบความคืบหน้า")]
    [SerializeField] bool lever = false;

    public string Id => id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public Sprite Icon => icon;
    public bool ConsumableOnUse => consumableOnUse;
    public bool Lever => lever;             // <<--- ใช้เช็คฝั่ง PlayerInteractor

#if UNITY_EDITOR
    void OnValidate()
    {
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

    [ContextMenu("Inventory/Regenerate Random ID")]
    void RegenerateRandomId()
    {
        id = System.Guid.NewGuid().ToString();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
