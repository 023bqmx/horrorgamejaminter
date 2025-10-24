// JumpscareBehaviour.cs
using UnityEngine;
using UnityEngine.Playables;

namespace Horror.Timeline
{
    public class JumpscareBehaviour : PlayableBehaviour
    {
        public Horror.JumpscareSettings settings;
        public Horror.JumpscareController controller;

        bool _done;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (_done || settings == null) return;

            var c = controller;
            if (c == null)
                c = Object.FindAnyObjectByType<Horror.JumpscareController>();

            if (c != null)
            {
                c.Trigger(settings);
                _done = true;
            }
        }
    }
}
