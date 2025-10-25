// TimelineTriggerZone.cs
// Unity 6 — Plays a PlayableDirector when the Player enters a trigger.
// Works great with your Dialogue Track (Timeline pauses on dialogue clips until they finish).

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class TimelineTriggerZone : MonoBehaviour
{
    [Header("Who can trigger")]
    [SerializeField] string requiredTag = "Player";
    [SerializeField] LayerMask layerMask = ~0;

    [Header("Timeline")]
    [SerializeField] PlayableDirector director;
    [SerializeField] bool rewindOnEnter = true;
    [SerializeField] bool onlyOnce = true;
    [SerializeField, Min(0f)] float startDelay = 0f;

    [Header("Hooks (optional)")]
    public UnityEvent onBeforePlay;
    public UnityEvent onAfterPlayStart;

    bool _armed = true;     // prevents spamming while inside
    Collider _col;

    void Reset()
    {
        _col = GetComponent<Collider>();
        if (_col) _col.isTrigger = true;
        if (!director) director = GetComponentInParent<PlayableDirector>();
    }

    void Awake()
    {
        _col = GetComponent<Collider>();
        if (_col) _col.isTrigger = true;
        if (!director) director = GetComponentInParent<PlayableDirector>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!_armed) return;
        if (!IsValid(other)) return;
        if (!director)
        {
            Debug.LogWarning("[TimelineTriggerZone] No PlayableDirector assigned.", this);
            return;
        }

        _armed = !onlyOnce; // disarm if one-shot
        StartCoroutine(PlayRoutine());
    }

    void OnTriggerExit(Collider other)
    {
        // Re-arm when leaving if not one-shot (allows re-trigger on re-entry)
        if (!onlyOnce && IsValid(other)) _armed = true;
    }

    System.Collections.IEnumerator PlayRoutine()
    {
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        onBeforePlay?.Invoke();

        if (rewindOnEnter) director.time = 0;
        director.extrapolationMode = DirectorWrapMode.None;
        director.Play();

        onAfterPlayStart?.Invoke();
    }

    bool IsValid(Collider other)
    {
        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return false;
        if (((1 << other.gameObject.layer) & layerMask) == 0) return false;
        return true;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.25f);
        var c = GetComponent<Collider>();
        if (c is BoxCollider b)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(b.center, b.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
        else if (c is SphereCollider s)
        {
            Gizmos.DrawSphere(transform.TransformPoint(s.center),
                s.radius * Mathf.Max(transform.lossyScale.x, Mathf.Max(transform.lossyScale.y, transform.lossyScale.z)));
        }
    }
#endif
}
