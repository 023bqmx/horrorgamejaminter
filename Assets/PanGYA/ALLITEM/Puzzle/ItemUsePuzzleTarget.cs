// ItemUsePuzzleTarget.cs
// Unity 6 — Inventory OR UI puzzle with hover label, Outline lock, Timeline/Animator, SFX,
// and robust Animator playback (trigger/bool/crossfade/state-play).
//
// Key fixes to ensure animation plays:
//  • Option to include Animator(s) from the spawned prefab
//  • Ensure objects are active/enabled before play
//  • Optional Rebind + Update(0) before triggering
//  • Optional one-frame delay before animator actions

using System.Collections;
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
    public UnityEvent OnOpenUIPuzzle;
    [SerializeField] bool uiRequiresMatchingItem = false;
    [SerializeField] bool uiConsumeItemOnOpen = false;

    [Header("Quick Outline (root)")]
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

    [Header("Animator (targets you want to drive)")]
    [SerializeField] Animator[] animators;

    [Header("Animator – Playback Options")]
    [Tooltip("Also include any Animator found on the spawned prefab (and its children).")]
    [SerializeField] bool includeSpawnedPrefabAnimator = true;

    [Tooltip("If true, set this Trigger on all target animators.")]
    [SerializeField] bool useTrigger = true;
    [SerializeField] string triggerOnSolve = "Open";

    [Tooltip("If true, set this Bool on all target animators.")]
    [SerializeField] bool useBool = false;
    [SerializeField] string boolParamOnSolve = "";
    [SerializeField] bool   boolValueOnSolve = true;

    [Tooltip("If true, crossfade to this state on all target animators (layer 0).")]
    [SerializeField] bool playStateOnSolve = false;
    [SerializeField] string stateNameOnSolve = "";
    [SerializeField, Min(0f)] float crossFadeDuration = 0.1f;

    [Tooltip("Call Rebind() and Update(0) before playing (helps when just enabled/instantiated).")]
    [SerializeField] bool rebindBeforePlay = true;

    [Tooltip("Wait one frame before doing animator actions (lets newly spawned objects initialize).")]
    [SerializeField] bool delayOneFrameBeforeAnimatorActions = false;

    [Header("Audio (optional)")]
    [SerializeField] AudioSource audioSource;       // If null, uses PlayClipAtPoint
    [SerializeField] AudioClip sfxOpenUI;           // When UI opens
    [SerializeField] AudioClip sfxUseSuccess;       // When solved

    [Header("Events (optional)")]
    public UnityEvent OnSolved;

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

    // Optional non-item path (e.g., E to open keypad UI)
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

        GameObject spawned = null;
        if (spawnPrefab)
        {
            var pos = spawnAt ? spawnAt.position : transform.position;
            var rot = spawnAt ? spawnAt.rotation : transform.rotation;
            spawned = Instantiate(spawnPrefab, pos, rot);
            if (parentSpawn && spawnAt) spawned.transform.SetParent(spawnAt, true);
        }

        // Timeline first (often fine either order, but doing this early can help if Timeline prepares bindings)
        if (directorToPlay) directorToPlay.Play();

        if (playSuccessSfx) PlayOneShot(sfxUseSuccess);

        // Animator actions (can be delayed 1 frame to avoid race with instantiation/bindings)
        if (delayOneFrameBeforeAnimatorActions)
            StartCoroutine(DoAnimatorActionsNextFrame(spawned));
        else
            DoAnimatorActions(spawned);

        OnSolved?.Invoke();

        if (outlineHighlighter) outlineHighlighter.LockOff();

        _completed = true;

        if (onlyOnce)
        {
            foreach (var c in GetComponentsInChildren<Collider>(true))
                c.enabled = false;
        }
    }

    IEnumerator DoAnimatorActionsNextFrame(GameObject spawned)
    {
        yield return null; // wait one frame
        DoAnimatorActions(spawned);
    }

    void DoAnimatorActions(GameObject spawned)
    {
        // Build the animator list
        var list = animators != null ? animators.Where(a => a).ToList() : new System.Collections.Generic.List<Animator>();

        if (includeSpawnedPrefabAnimator && spawned)
        {
            var spawnedAnims = spawned.GetComponentsInChildren<Animator>(true);
            foreach (var a in spawnedAnims)
                if (a && !list.Contains(a)) list.Add(a);
        }

        if (list.Count == 0) return;

        foreach (var a in list)
        {
            // Ensure active/enabled
            if (!a.gameObject.activeInHierarchy) a.gameObject.SetActive(true);
            if (!a.enabled) a.enabled = true;

            // Rebind/prime if requested (prevents “first frame swallowed”)
            if (rebindBeforePlay)
            {
                a.Rebind();
                a.Update(0f);
            }

            // Trigger / Bool
            if (useTrigger && !string.IsNullOrEmpty(triggerOnSolve))
            {
                a.ResetTrigger(triggerOnSolve);
                a.SetTrigger(triggerOnSolve);
            }
            if (useBool && !string.IsNullOrEmpty(boolParamOnSolve))
            {
                a.SetBool(boolParamOnSolve, boolValueOnSolve);
            }

            // Direct state play (optional)
            if (playStateOnSolve && !string.IsNullOrEmpty(stateNameOnSolve))
            {
                if (crossFadeDuration > 0f)
                    a.CrossFade(stateNameOnSolve, crossFadeDuration, 0, 0f);
                else
                    a.Play(stateNameOnSolve, 0, 0f);
            }
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
