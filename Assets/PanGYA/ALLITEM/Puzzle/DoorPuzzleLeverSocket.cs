// DoorPuzzleLeverSocket.cs — Timeline-first version (Unity 6)
// Plays a Timeline on the spawned lever prefab, then completes & notifies receivers.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

[DisallowMultipleComponent]
public class DoorPuzzleLeverSocket : MonoBehaviour, IItemUseHandler
{
    [Header("Requirements")]
    [SerializeField] ItemDefinition requiredItem;
    [SerializeField] bool onlyOnce = true;

    [Header("Visuals")]
    [SerializeField] GameObject brokenVisual;
    [SerializeField] GameObject leverFixedPrefab;
    [SerializeField] Transform socketMount; // parent/mount for leverFixedPrefab

    [Header("Timeline (fallback on socket)")]
    [Tooltip("Used only if the spawned prefab has no PlayableDirector.")]
    [SerializeField] PlayableDirector directorOnSocket;

    [Header("Notify receivers when the sequence finishes")]
    [SerializeField] List<MonoBehaviour> receivers = new(); // must implement ILeverUseReceiver

    [Header("Optional: block re-use after complete")]
    [SerializeField] Collider interactBlocker;

    [Header("Debug")]
    [SerializeField] bool debugLogs = false;

    bool _completed;
    bool _running;

    void Log(string msg, Object ctx = null)
    {
        if (debugLogs) Debug.Log($"[DoorPuzzleLeverSocket] {msg}", ctx ? ctx : this);
    }

    public bool CanUseItem(ItemDefinition item)
    {
        if (_completed && onlyOnce) return false;
        return item && item == requiredItem;
    }

    public void UseItem(ItemDefinition item, PlayerInventory user)
    {
        if (!CanUseItem(item)) { Log("UseItem rejected by CanUseItem"); return; }
        if (_running) { Log("UseItem ignored: already running"); return; }
        _running = true;
        Log("UseItem START");

        // 1) consume if needed
        if (item.ConsumableOnUse && user)
        {
            var ok = user.TryRemove(item);
            Log($"ConsumableOnUse → removed from inventory = {ok}");
        }

        // 2) visuals swap
        if (brokenVisual) brokenVisual.SetActive(false);

        PlayableDirector toPlay = null;

        // 3) spawn lever prefab and try to play its own director
        if (leverFixedPrefab && socketMount)
        {
            var inst = Instantiate(leverFixedPrefab, socketMount.position, socketMount.rotation, socketMount);
            Log($"Instantiated lever prefab '{inst.name}'", inst);

            // Prefer a director on the instance
            toPlay = inst.GetComponentInChildren<PlayableDirector>(true);
            if (toPlay)
            {
                // Make sure the director references are valid (usually already set inside the prefab)
                toPlay.time = 0;
                toPlay.stopped -= OnTimelineStopped;
                toPlay.stopped += OnTimelineStopped;
                toPlay.Play();
                Log("Playing prefab's PlayableDirector");
            }
        }

        // 4) fallback to the socket’s director
        if (!toPlay && directorOnSocket)
        {
            directorOnSocket.time = 0;
            directorOnSocket.stopped -= OnTimelineStopped;
            directorOnSocket.stopped += OnTimelineStopped;
            directorOnSocket.Play();
            Log("Playing socket PlayableDirector (fallback)");
            toPlay = directorOnSocket;
        }

        // 5) if there was no director at all, finish immediately
        if (!toPlay)
        {
            Log("No PlayableDirector found — finishing immediately");
            FinishSequence();
        }
    }

    void OnTimelineStopped(PlayableDirector d)
    {
        d.stopped -= OnTimelineStopped;
        Log("Timeline finished");
        FinishSequence();
    }

    void FinishSequence()
    {
        if (_completed) return;
        _completed = true;
        _running = false;

        // Notify receivers
        int notified = 0;
        for (int i = 0; i < receivers.Count; i++)
        {
            var mb = receivers[i];
            if (!mb) continue;
            if (mb is ILeverUseReceiver r)
            {
                r.OnLeverUsed(this);
                notified++;
            }
        }
        Log($"Notified {notified} ILeverUseReceiver(s)");

        // Block interaction if needed
        if (interactBlocker) interactBlocker.enabled = false;

        Log("UseItem END → completed=true");
    }
}
