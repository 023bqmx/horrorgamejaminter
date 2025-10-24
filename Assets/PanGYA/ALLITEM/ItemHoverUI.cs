using TMPro;
using UnityEngine;

public class ItemHoverUI : MonoBehaviour
{
    [SerializeField] Canvas rootCanvas;              // Screen Space - Overlay
    [SerializeField] TextMeshProUGUI label;          // E.g., "E — Pick up Lever"
    [SerializeField] Vector2 offset = new Vector2(16f, -96f);

    void Awake()
    {
    if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>();
    if (label) label.rectTransform.pivot = new Vector2(0f, 1f); // top-left pivot
    }


    void Reset()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        label = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void Show(string text)
    {
        if (!label) return;
        label.gameObject.SetActive(true);
        label.text = text;
    }

    public void Hide()
    {
        if (!label) return;
        label.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
    if (!label || !rootCanvas) return;
    var rt = label.rectTransform;

    // position below cursor with your offset
    Vector2 pos = (Vector2)Input.mousePosition + offset;

    // keep it on-screen (optional)
    Vector2 size = rt.sizeDelta * rootCanvas.scaleFactor;
    pos.x = Mathf.Clamp(pos.x, 8f, Screen.width  - size.x - 8f);
    pos.y = Mathf.Clamp(pos.y, 8f + size.y, Screen.height - 8f);

    rt.position = pos;
    }
}
