// ExclusiveTimelineTriggerZone.cs
// Only one timeline in the same group can play at once.

using UnityEngine;
using UnityEngine.Playables;

[RequireComponent(typeof(Collider))]
public class ExclusiveTimelineTriggerZone : MonoBehaviour
{
    [Header("Exclusive Group")]
    [SerializeField] string group = "Cutscene";  // same text for all zones that must be exclusive

    [Header("Trigger")]
    [SerializeField] string requiredTag = "Player";
    [SerializeField] LayerMask layerMask = ~0;

    [Header("Timeline")]
    [SerializeField] PlayableDirector director;
    [SerializeField] bool rewindOnEnter = true;
    [SerializeField] bool onlyOnce = true;

    bool _armed = true;

    void Awake()
    {
        var c = GetComponent<Collider>();
        if (c) c.isTrigger = true;
        if (director) director.stopped += OnDirectorStopped;
    }

    void OnDestroy()
    {
        if (director) director.stopped -= OnDirectorStopped;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!_armed) return;
        if (!IsValid(other)) return;
        if (!director) { Debug.LogWarning("[ExclusiveTimelineTriggerZone] No director assigned.", this); return; }

        // if someone else in the group is running, ignore
        if (!CutsceneGate.TryEnter(group)) return;

        _armed = !onlyOnce;

        if (rewindOnEnter) director.time = 0;
        director.extrapolationMode = DirectorWrapMode.None;
        director.Play();
    }

    void OnDirectorStopped(PlayableDirector d)
    {
        // free the group when this timeline ends
        CutsceneGate.Exit(group);
    }

    bool IsValid(Collider other)
    {
        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return false;
        if (((1 << other.gameObject.layer) & layerMask) == 0) return false;
        return true;
    }
}
