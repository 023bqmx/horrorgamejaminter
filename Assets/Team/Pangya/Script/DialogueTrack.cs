// DialogueTrack.cs
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(0.15f, 0.15f, 0.18f)]
[TrackBindingType(typeof(DialogueController))]
[TrackClipType(typeof(DialogueClip))]
public class DialogueTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        var mixer = ScriptPlayable<DialogueTrackMixerBehaviour>.Create(graph, inputCount);
        var behaviour = mixer.GetBehaviour();

        var director = graph.GetResolver() as PlayableDirector;
        behaviour.director = director;

        // Get the track binding (assign your DialogueController here)
        if (director)
            behaviour.controller = director.GetGenericBinding(this) as DialogueController;

        return mixer;
    }

#if UNITY_EDITOR
    // Makes Timeline clip bar show a helpful duration (visual only)
    protected override void OnCreateClip(TimelineClip clip)
    {
        base.OnCreateClip(clip);
        if (clip.asset is DialogueClip dc)
            clip.duration = Mathf.Max(0.1f, (float)dc.editorHintDuration);
    }
#endif
}

public class DialogueTrackMixerBehaviour : PlayableBehaviour
{
    public PlayableDirector director;
    public DialogueController controller;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        // Propagate references to all active clip behaviours every frame
        int inputCount = playable.GetInputCount();
        for (int i = 0; i < inputCount; i++)
        {
            var inputPlayable = (ScriptPlayable<DialogueBehaviour>)playable.GetInput(i);
            if (!inputPlayable.IsValid()) continue;

            var b = inputPlayable.GetBehaviour();
            b.director = director;
            b.controller = controller;
        }
    }
}
