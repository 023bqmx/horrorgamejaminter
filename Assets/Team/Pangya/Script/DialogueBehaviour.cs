// DialogueBehaviour.cs
using UnityEngine;
using UnityEngine.Playables;

public class DialogueBehaviour : PlayableBehaviour
{
    [HideInInspector] public DialogueController controller;
    [HideInInspector] public PlayableDirector director;

    [HideInInspector] public DialogueSequence sequence;
    [HideInInspector] public bool pauseDirector = true;

    bool _started;
    bool _finished;

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (!Application.isPlaying) return;
        if (_started || _finished) return;
        if (controller == null || sequence == null || director == null) return;

        _started = true;

        if (pauseDirector)
            director.Pause();

        controller.RunSequence(sequence, onComplete: () =>
        {
            _finished = true;
            if (pauseDirector)
                director.Play();   // Timeline resumes only AFTER dialogue is fully done
        });
    }

    public override void OnGraphStop(Playable playable)
    {
        // Safety: reset flags if Timeline stops/rewinds
        _started = false;
        _finished = false;
    }
}
