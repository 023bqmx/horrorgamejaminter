// PuzzleOutlineAndLabel.cs
// Small helper to quickly configure an Outline + TMP label on a puzzle object
// and let other scripts turn them off once solved.

using UnityEngine;

[DisallowMultipleComponent]
public class PuzzleOutlineAndLabel : MonoBehaviour
{
    [Header("Quick Outline")]
    [SerializeField] Color outlineColor = Color.cyan;
    [SerializeField, Min(0f)] float outlineWidth = 5f;
    [SerializeField] bool startEnabled = true;

    [Header("Label (TMP)")]
    [SerializeField] TMPro.TextMeshPro label;
    [SerializeField] string text = "Use item";
    [SerializeField] bool billboard = true;

    Outline _outline;

    void Awake()
    {
        // Ensure Outline exists and configure
        _outline = GetComponent<Outline>();
        if (!_outline) _outline = gameObject.AddComponent<Outline>();
        _outline.OutlineMode = Outline.Mode.OutlineAll;
        _outline.OutlineColor = outlineColor;
        _outline.OutlineWidth = outlineWidth;
        _outline.enabled = startEnabled;

        if (label)
        {
            label.text = text;
            label.gameObject.SetActive(startEnabled);
        }
    }

    void LateUpdate()
    {
        if (!billboard || !label) return;
        var cam = Camera.main;
        if (cam)
            label.transform.forward = (label.transform.position - cam.transform.position).normalized;
    }

    public void SetActive(bool value)
    {
        if (_outline) _outline.enabled = value;
        if (label) label.gameObject.SetActive(value);
    }

    public void MarkSolved()
    {
        SetActive(false);
        // Optional: disable this component too
        enabled = false;
    }
}
