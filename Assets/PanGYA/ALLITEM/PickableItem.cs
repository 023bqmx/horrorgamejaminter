using UnityEngine;

// Requires the Quick Outline asset (component type: Outline)
[DisallowMultipleComponent]
public class PickableItem : MonoBehaviour
{
    [SerializeField] ItemDefinition item;
    [Header("Quick Outline")]
    [SerializeField] Color outlineColor = Color.yellow;
    [SerializeField, Min(0f)] float outlineWidth = 5f;

    Outline outline;

    public ItemDefinition Item => item;

    void Awake()
    {
        // Ensure Outline component exists, configure, and keep it disabled until hovered.
        outline = GetComponent<Outline>();
        if (!outline) outline = gameObject.AddComponent<Outline>();

        outline.OutlineMode = Outline.Mode.OutlineAll;
        outline.OutlineColor = outlineColor;
        outline.OutlineWidth = outlineWidth;
        outline.enabled = false; // toggle enabled for hover per asset guidance. :contentReference[oaicite:0]{index=0}
    }

    public void SetHighlighted(bool value)
    {
        if (outline) outline.enabled = value; // asset recommends toggling enabled vs re-adding. :contentReference[oaicite:1]{index=1}
    }
}
