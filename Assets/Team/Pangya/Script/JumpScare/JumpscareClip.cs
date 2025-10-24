// JumpscareClip.cs
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Horror.Timeline
{
    [System.Serializable]
    public class JumpscareClip : PlayableAsset, ITimelineClipAsset
    {
        public Horror.JumpscareSettings settings;
        [Tooltip("Assign the scene's JumpscareController via the clip's inspector.")]
        public ExposedReference<Horror.JumpscareController> controller;

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<JumpscareBehaviour>.Create(graph);
            var b = playable.GetBehaviour();
            b.settings = settings;
            b.controller = controller.Resolve(graph.GetResolver());
            return playable;
        }
    }
}
