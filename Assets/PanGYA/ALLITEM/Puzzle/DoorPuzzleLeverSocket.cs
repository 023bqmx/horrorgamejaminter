using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;   // PlayableDirector for Timeline
// If you also use UnityEngine.Timeline, it's optional to include: using UnityEngine.Timeline;

[DisallowMultipleComponent]
public class DoorPuzzleLeverSocket : MonoBehaviour, IItemUseHandler
{
    [Header("Requirements")]
    [SerializeField] ItemDefinition requiredItem;          // e.g., Lever item
    [SerializeField] bool onlyOnce = true;                 // prevent reuse
    bool _completed;

    [Header("Visuals (optional)")]
    [Tooltip("Shown while broken (optional). Will be disabled when fixed.")]
    [SerializeField] GameObject brokenVisual;
    [Tooltip("Prefab to instantiate as the fixed lever when the item is used.")]
    [SerializeField] GameObject leverFixedPrefab;
    [Tooltip("Where to spawn the fixed lever prefab.")]
    [SerializeField] Transform socketMount;

    [Header("Timeline (optional)")]
    [Tooltip("PlayableDirector to play when the lever is inserted.")]
    [SerializeField] PlayableDirector director;            // call Play() on success

    [Header("Notify others (optional)")]
    [Tooltip("Scripts to notify (must implement ILeverUseReceiver).")]
    [SerializeField] List<MonoBehaviour> receivers = new(); // drag any components that implement ILeverUseReceiver

    [Header("Colliders / Interaction (optional)")]
    [Tooltip("Disable this collider after completion so it can't be reused.")]
    [SerializeField] Collider interactBlocker;

    public bool CanUseItem(ItemDefinition item)
    {
        if (_completed && onlyOnce) return false;
        return item && item == requiredItem;
    }

    public void UseItem(ItemDefinition item, PlayerInventory user)
    {
        if (!CanUseItem(item)) return;

        // 1) consume from inventory if the item is consumable
        if (item.ConsumableOnUse && user) user.TryRemove(item);

        // 2) swap visuals
        if (brokenVisual) brokenVisual.SetActive(false);
        if (leverFixedPrefab && socketMount)
        {
            Instantiate(leverFixedPrefab, socketMount.position, socketMount.rotation, socketMount);
        }

        // 3) play timeline
        if (director)
        {
            director.time = 0;
            director.Play();
        }

        // 4) notify listeners
        for (int i = 0; i < receivers.Count; i++)
        {
            var mb = receivers[i];
            if (!mb) continue;
            if (mb is ILeverUseReceiver r)
                r.OnLeverUsed(this);
        }

        // 5) lock it if one-shot
        _completed = true;
        if (interactBlocker) interactBlocker.enabled = false;
    }
}
