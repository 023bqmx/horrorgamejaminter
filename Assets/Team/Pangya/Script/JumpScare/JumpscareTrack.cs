using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Horror.Timeline
{
    [TrackColor(0.9f, 0.1f, 0.1f)]
    [TrackClipType(typeof(JumpscareClip))]
    public sealed class JumpscareTrack : TrackAsset
    {
        // local concrete behaviour so ScriptPlayable has a non-abstract type
        private sealed class Mixer : PlayableBehaviour { }

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<Mixer>.Create(graph, inputCount);
        }
    }
}
