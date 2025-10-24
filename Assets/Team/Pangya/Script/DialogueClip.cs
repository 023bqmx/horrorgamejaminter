// DialogueClip.cs
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[System.Serializable]
public class DialogueClip : PlayableAsset, ITimelineClipAsset
{
    public DialogueSequence sequence;
    public bool pauseDirector = true;

    // Optional editor hint: show an approximate length in Timeline UI
    [Min(0.1f)] public double editorHintDuration = 2.0;

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<DialogueBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.sequence = sequence;
        behaviour.pauseDirector = pauseDirector;
        return playable;
    }
}
