using UnityEngine;

[DisallowMultipleComponent]
public class OutlineHighlighter : MonoBehaviour
{
    [Header("Quick Outline (Chris Nolet)")]
    [SerializeField] Outline outline;                  // assign the Outline on the root mesh
    [SerializeField] bool autoAddOutline = false;      // set true if you want auto-add
    [SerializeField] Outline.Mode outlineMode = Outline.Mode.OutlineAll;
    [SerializeField] Color colorWhenNear  = new Color(0f, 1f, 1f, 1f);
    [SerializeField] Color colorWhenHover = new Color(1f, 1f, 0f, 1f);
    [SerializeField, Min(0f)] float widthWhenNear  = 4f;
    [SerializeField, Min(0f)] float widthWhenHover = 6f;
    [SerializeField] bool startDisabled = true;

    bool _proximity, _hover, _locked;

    void Reset() { EnsureOutline(); }
    void Awake()
    {
        EnsureOutline();
        ApplyStyle();
        SetEnabled(!startDisabled);
    }

    void EnsureOutline()
    {
        if (!outline && autoAddOutline)
        {
            outline = GetComponent<Outline>();
            if (!outline) outline = gameObject.AddComponent<Outline>();
        }
        if (outline) outline.OutlineMode = outlineMode;
    }

    void ApplyStyle()
    {
        if (!outline) return;
        bool hover = _hover;
        outline.OutlineColor = hover ? colorWhenHover : colorWhenNear;
        outline.OutlineWidth = hover ? widthWhenHover  : widthWhenNear;
    }

    void SetEnabled(bool v)
    {
        if (!outline) return;
        outline.enabled = v && !_locked;
    }

    void Refresh()
    {
        if (_locked) { SetEnabled(false); return; }
        ApplyStyle();
        SetEnabled(_proximity || _hover);
    }

    public void SetProximityActive(bool v) { _proximity = v; Refresh(); }
    public void SetHoverActive(bool v)     { _hover = v;     Refresh(); }
    public void LockOff()                  { _locked = true; SetEnabled(false); }

#if UNITY_EDITOR
    void OnValidate()
    {
        EnsureOutline();
        if (!Application.isPlaying) { ApplyStyle(); if (outline) outline.enabled = !startDisabled; }
    }
#endif
}
