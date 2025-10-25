// ItemUsePuzzleTarget.cs
// Unity 6 — Inventory OR UI puzzle with hover name, Outline lock, Timeline/Animator,
// and SFX on UI open / success. Drop this on the CHILD (hover collider) object.

using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

[DisallowMultipleComponent]
public class ItemUsePuzzleTarget : MonoBehaviour, IItemUseHandler
{
    public enum ActivationMode { UseInventoryItem, OpenUIPuzzle }

    [Header("Mode")]
    [SerializeField] ActivationMode mode = ActivationMode.UseInventoryItem;

    [Header("Identity")]
    [SerializeField] string puzzleName = "Puzzle";
    public string PuzzleName => string.IsNullOrWhiteSpace(puzzleName) ? gameObject.name : puzzleName;

    [Header("Requirement (UseInventoryItem mode)")]
    [SerializeField] ItemDefinition requiredItem;
    [SerializeField] bool onlyOnce = true;

    [Header("UI Puzzle Hook (OpenUIPuzzle mode)")]
    public UnityEvent OnOpenUIPuzzle;        // Hook your UI opener
    [SerializeField] bool uiRequiresMatchingItem = false;
    [SerializeField] bool uiConsumeItemOnOpen = false;

    [Header("Quick Outline (root)")]
    [Tooltip("Highlighter on the ROOT (handles proximity + hover glow).")]
    [SerializeField] OutlineHighlighter outlineHighlighter;

    [Header("Label (optional TMP)")]
    [SerializeField] TMPro.TextMeshPro label;
    [SerializeField] string labelTextFallback = "Use item";
    [SerializeField] bool billboardLabel = true;

    [Header("Prefab Swap / Spawn (optional)")]
    [SerializeField] GameObject originalToDisable;
    [SerializeField] GameObject spawnPrefab;
    [SerializeField] Transform spawnAt;
    [SerializeField] bool parentSpawn = true;

    [Header("Timeline (optional)")]
    [SerializeField] PlayableDirector directorToPlay;

    [Header("Animator (optional)")]
    [SerializeField] Animator[] animators;
    [SerializeField] string triggerOnSolve = "";
    [SerializeField] string boolParamOnSolve = "";
    [SerializeField] bool   boolValueOnSolve = true;

    [Header("Audio (optional)")]
    [SerializeField] AudioSource audioSource;       // If null, uses PlayClipAtPoint
    [SerializeField] AudioClip sfxOpenUI;           // When UI opens (Interact or UI mode via UseItem)
    [SerializeField] AudioClip sfxUseSuccess;       // When puzzle successfully solves

    [Header("Events (optional)")]
    public UnityEvent OnSolved;                     // Great for hooking external doors/lights

    bool _completed;

    void Reset()
    {
        if (!spawnAt) spawnAt = transform;
        if (!outlineHighlighter) outlineHighlighter = GetComponentInParent<OutlineHighlighter>();
        if (!audioSource) audioSource = GetComponent<AudioSource>();
    }

    void Awake()
    {
        if (!spawnAt) spawnAt = transform;
        if (!outlineHighlighter) outlineHighlighter = GetComponentInParent<OutlineHighlighter>();

        if (label)
            label.text = requiredItem ? $"Use: {requiredItem.DisplayName}"
                                      : (string.IsNullOrWhiteSpace(labelTextFallback) ? PuzzleName : labelTextFallback);
    }

    void LateUpdate()
    {
        if (billboardLabel && label)
        {
            var cam = Camera.main;
            if (cam) label.transform.forward = (label.transform.position - cam.transform.position).normalized;
        }
    }

    // Optional non-item path for UI mode (e.g., press E to open keypad UI)
    public void Interact()
    {
        if (_completed && onlyOnce) return;
        if (mode != ActivationMode.OpenUIPuzzle) return;

        PlayOneShot(sfxOpenUI);
        OnOpenUIPuzzle?.Invoke();
    }

    // ---------------- IItemUseHandler ----------------
    public bool CanUseItem(ItemDefinition item)
    {
        if (_completed && onlyOnce) return false;

        if (mode == ActivationMode.OpenUIPuzzle)
        {
            if (!uiRequiresMatchingItem) return true;
            return Match(item, requiredItem);
        }

        return Match(item, requiredItem);
    }

    public void UseItem(ItemDefinition item, PlayerInventory user)
    {
        if (!CanUseItem(item)) return;

        if (mode == ActivationMode.OpenUIPuzzle)
        {
            if (uiConsumeItemOnOpen && item && item.ConsumableOnUse && user != null)
                user.TryRemove(item);

            PlayOneShot(sfxOpenUI);
            OnOpenUIPuzzle?.Invoke();     // Your UI must call MarkSolved() when done
            return;
        }

        // Inventory mode: consume then solve
        if (item && item.ConsumableOnUse && user != null)
            user.TryRemove(item);

        ApplySolved(playSuccessSfx: true);
    }

    [ContextMenu("Mark Solved (Test)")]
    public void MarkSolved()
    {
        if (_completed && onlyOnce) return;
        ApplySolved(playSuccessSfx: true);
    }

    void ApplySolved(bool playSuccessSfx)
    {
        if (label) label.gameObject.SetActive(false);
        if (originalToDisable) originalToDisable.SetActive(false);

        if (spawnPrefab)
        {
            var pos = spawnAt ? spawnAt.position : transform.position;
            var rot = spawnAt ? spawnAt.rotation : transform.rotation;
            var spawned = Instantiate(spawnPrefab, pos, rot);
            if (parentSpawn && spawnAt) spawned.transform.SetParent(spawnAt, true);
        }

        if (directorToPlay) directorToPlay.Play();

        if (animators != null)
        {
            foreach (var a in animators.Where(a => a))
            {
                if (!string.IsNullOrEmpty(triggerOnSolve)) a.SetTrigger(triggerOnSolve);
                if (!string.IsNullOrEmpty(boolParamOnSolve)) a.SetBool(boolParamOnSolve, boolValueOnSolve);
            }
        }

        if (playSuccessSfx) PlayOneShot(sfxUseSuccess);

        OnSolved?.Invoke();                         // Pair external actions (open door, etc.)

        if (outlineHighlighter) outlineHighlighter.LockOff();

        _completed = true;

        if (onlyOnce)
        {
            foreach (var c in GetComponentsInChildren<Collider>(true))
                c.enabled = false;
        }
    }

    static bool Match(ItemDefinition a, ItemDefinition b)
    {
        if (!a || !b) return false;
        if (a == b) return true;
        return !string.IsNullOrEmpty(a.Id) && a.Id == b.Id;
    }

    void PlayOneShot(AudioClip clip)
    {
        if (!clip) return;
        if (audioSource) audioSource.PlayOneShot(clip);
        else AudioSource.PlayClipAtPoint(clip, transform.position);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!spawnAt) spawnAt = transform;
        if (!outlineHighlighter) outlineHighlighter = GetComponentInParent<OutlineHighlighter>();
        if (!audioSource) audioSource = GetComponent<AudioSource>();

        if (label && string.IsNullOrWhiteSpace(label.text))
            label.text = string.IsNullOrWhiteSpace(labelTextFallback) ? PuzzleName : labelTextFallback;
    }
#endif
}
