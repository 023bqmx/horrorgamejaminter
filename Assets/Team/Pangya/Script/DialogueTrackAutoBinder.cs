using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class DialogueTrackAutoBinder : MonoBehaviour
{
    [SerializeField] PlayableDirector director;
    [SerializeField] DialogueController dialogueController;

    void Awake()
    {
        if (!director || !dialogueController) return;
        var timeline = director.playableAsset as TimelineAsset;
        if (!timeline) return;

        foreach (var track in timeline.GetOutputTracks())
        {
            if (track is DialogueTrack)
            {
                director.SetGenericBinding(track, dialogueController);
            }
        }
    }
}
