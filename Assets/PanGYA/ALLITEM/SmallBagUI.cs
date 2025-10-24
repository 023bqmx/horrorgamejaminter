// SmallBagUI.cs
// Unity 6 — Bag UI with toggle key + smooth slide + selected-slot highlight

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UIOutline = UnityEngine.UI.Outline;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class SmallBagUI : MonoBehaviour
{
    [Header("Inventory & Interactor")]
    [SerializeField] PlayerInventory inventory;          // your player's bag
    [SerializeField] PlayerInteractor interactor;        // to read ActiveSlot

    [Header("Slots (size should be 5)")]
    [SerializeField] List<Image> slotIcons = new();      // 5 icon Images (top→bottom or left→right)
    [SerializeField] List<TextMeshProUGUI> slotLabels = new(); // optional 5 labels

    [Header("Optional Frames (if you have a background/border per slot)")]
    [Tooltip("Optional: background/border Images behind each icon; if empty, the script will auto-add a UI Outline to icons.")]
    [SerializeField] List<Image> slotFrames = new();     // 5 frame Images (optional)

    [Header("Empty Slot Appearance")]
    [SerializeField] Sprite emptyIcon;
    [SerializeField] string emptyLabel = "";

    // ---------- Toggle + Slide ----------
    [Header("Toggle / Slide")]
    [SerializeField] KeyCode toggleKey = KeyCode.Tab;
    [SerializeField] bool startOpen = false;
    [SerializeField, Min(0.05f)] float slideDuration = 0.25f;
    [SerializeField] Vector2 hiddenOffset = new Vector2(0f, -360f);
    [SerializeField] AnimationCurve slideCurve = null;
    [SerializeField] bool fadeWithCanvasGroup = true;
    [SerializeField, Range(0f, 1f)] float closedAlpha = 0f;
    [SerializeField] bool useUnscaledTime = true;

    // ---------- Selected-slot visuals ----------
    [Header("Selected Slot Visuals")]
    [SerializeField] Color selectedFrameColor = new Color(1, 1, 1, 0.9f);
    [SerializeField] Color normalFrameColor   = new Color(1, 1, 1, 0.25f);
    [SerializeField, Min(1f)] float selectedScale = 1.08f;
    [SerializeField, Min(0.01f)] float selectAnimDuration = 0.12f;

    RectTransform _rect;
    CanvasGroup _cg;

    Vector2 _openPos, _closedPos;
    bool _isOpen;
    Coroutine _slideCo;

    // FIXED: use UI Outline, not Quick Outline
    readonly List<UIOutline> _autoOutlines = new();      // fallback outlines (if no frames provided)
    readonly List<RectTransform> _iconRTs = new();
    int _currentSelected = -1;

    bool HasFrames => slotFrames != null && slotFrames.Count == slotIcons.Count && slotFrames.Count > 0;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _cg = GetComponent<CanvasGroup>();
        if (fadeWithCanvasGroup && !_cg) _cg = gameObject.AddComponent<CanvasGroup>();
        if (slideCurve == null) slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        _openPos = _rect.anchoredPosition;
        _closedPos = _openPos + hiddenOffset;

        // Cache icon RectTransforms
        _iconRTs.Clear();
        foreach (var img in slotIcons)
            _iconRTs.Add(img ? img.rectTransform : null);

        // If no frames provided, auto-add a UI Outline to each icon as the highlight
        if (!HasFrames)
        {
            _autoOutlines.Clear();
            foreach (var img in slotIcons)
            {
                if (!img) { _autoOutlines.Add(null); continue; }
                var o = img.GetComponent<UIOutline>();
                if (!o) o = img.gameObject.AddComponent<UIOutline>();
                o.effectDistance = new Vector2(2f, -2f);
                o.effectColor = selectedFrameColor;
                o.enabled = false; // off until selected
                _autoOutlines.Add(o);
            }
        }

        SetOpen(startOpen, instant: true);
    }

    void OnEnable()
    {
        if (!inventory)  inventory  = FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Exclude);
        if (!interactor) interactor = FindFirstObjectByType<PlayerInteractor>(FindObjectsInactive.Exclude);

        if (inventory) inventory.OnInventoryChanged.AddListener(Repaint);
        Repaint();

        // Initialize highlight to current active slot if available
        if (interactor) ApplyHighlight(Mathf.Clamp(interactor.ActiveSlot, 0, slotIcons.Count - 1), instant:true);
    }

    void OnDisable()
    {
        if (inventory) inventory.OnInventoryChanged.RemoveListener(Repaint);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            Toggle();

        // Poll active slot and update highlight if it changes
        if (interactor)
        {
            int desired = Mathf.Clamp(interactor.ActiveSlot, 0, slotIcons.Count - 1);
            if (desired != _currentSelected)
                ApplyHighlight(desired, instant:false);
        }
    }

    // -------- Public API --------
    public void Toggle() => SetOpen(!_isOpen, instant: false);
    public void Open()   => SetOpen(true,  instant: false);
    public void Close()  => SetOpen(false, instant: false);

    // -------- Internals: open/close --------
    void SetOpen(bool open, bool instant)
    {
        _isOpen = open;

        if (_slideCo != null) { StopCoroutine(_slideCo); _slideCo = null; }

        if (instant)
        {
            _rect.anchoredPosition = open ? _openPos : _closedPos;
            if (_cg)
            {
                _cg.alpha = open ? 1f : closedAlpha;
                _cg.interactable = open;
                _cg.blocksRaycasts = open;
            }
            return;
        }
        _slideCo = StartCoroutine(SlideRoutine(open));
    }

    System.Collections.IEnumerator SlideRoutine(bool open)
    {
        float dur = Mathf.Max(0.01f, slideDuration);
        float t = 0f;

        Vector2 from = _rect.anchoredPosition;
        Vector2 to   = open ? _openPos : _closedPos;

        float startA = _cg ? _cg.alpha : 1f;
        float endA   = open ? 1f : closedAlpha;

        if (_cg && open) { _cg.blocksRaycasts = true; _cg.interactable = true; }

        while (t < 1f)
        {
            t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) / dur;
            float e = slideCurve.Evaluate(Mathf.Clamp01(t));
            _rect.anchoredPosition = Vector2.LerpUnclamped(from, to, e);
            if (_cg) _cg.alpha = Mathf.Lerp(startA, endA, e);
            yield return null;
        }

        _rect.anchoredPosition = to;
        if (_cg) { _cg.alpha = endA; _cg.blocksRaycasts = open; _cg.interactable = open; }
        _slideCo = null;
    }

    // -------- Selected-slot highlight --------
    void ApplyHighlight(int index, bool instant)
    {
        // turn off previous
        if (_currentSelected >= 0 && _currentSelected < slotIcons.Count)
        {
            if (HasFrames && slotFrames[_currentSelected])
                slotFrames[_currentSelected].color = normalFrameColor;
            else if (!HasFrames && _autoOutlines.Count == slotIcons.Count && _autoOutlines[_currentSelected])
                _autoOutlines[_currentSelected].enabled = false;

            var prevRT = _iconRTs[_currentSelected];
            if (prevRT) StartCoroutine(ScaleRT(prevRT, Vector3.one, instant ? 0f : selectAnimDuration));
        }

        _currentSelected = index;

        if (_currentSelected >= 0 && _currentSelected < slotIcons.Count)
        {
            if (HasFrames && slotFrames[_currentSelected])
                slotFrames[_currentSelected].color = selectedFrameColor;
            else if (!HasFrames && _autoOutlines.Count == slotIcons.Count && _autoOutlines[_currentSelected])
            {
                _autoOutlines[_currentSelected].effectColor = selectedFrameColor;
                _autoOutlines[_currentSelected].enabled = true;
            }

            var rt = _iconRTs[_currentSelected];
            if (rt) StartCoroutine(ScaleRT(rt, Vector3.one * selectedScale, instant ? 0f : selectAnimDuration));
        }
    }

    System.Collections.IEnumerator ScaleRT(RectTransform rt, Vector3 target, float dur)
    {
        if (!rt || dur <= 0f) { if (rt) rt.localScale = target; yield break; }
        Vector3 start = rt.localScale;
        float t = 0f;
        while (t < 1f)
        {
            t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) / dur;
            rt.localScale = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        rt.localScale = target;
    }

    // -------- Inventory repaint --------
    public void Repaint()
    {
        if (!inventory) return;

        for (int i = 0; i < slotIcons.Count; i++)
        {
            bool hasItem = i < inventory.Items.Count && inventory.Items[i];
            var icon = hasItem ? inventory.Items[i].Icon : emptyIcon;

            if (slotIcons[i]) slotIcons[i].sprite = icon;

            if (slotLabels != null && i < slotLabels.Count && slotLabels[i])
                slotLabels[i].text = hasItem ? inventory.Items[i].DisplayName : emptyLabel;
        }
    }
}
