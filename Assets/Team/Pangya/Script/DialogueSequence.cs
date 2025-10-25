// DialogueSequence.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Horror Dialogue/Dialogue Sequence", fileName = "NewDialogueSequence")]
public class DialogueSequence : ScriptableObject
{
    [Header("Lines")]
    public DialogueLine[] lines;

    [Header("Typewriter")]
    [Tooltip("Characters per second.")]
    [Min(1f)] public float charsPerSecond = 28f;

    [Tooltip("Extra delay after punctuation (.,!?).")]
    [Min(0f)] public float punctuationDelay = 0.12f;

    [Header("Auto")]
    [Tooltip("Fade-in time for the DialogueBox before first line.")]
    [Min(0f)] public float fadeIn = 0.15f;

    [Tooltip("Hold after each line (before moving to the next line).")]
    [Min(0f)] public float perLineHold = 0.25f;

    [Tooltip("Fade-out time after the last line.")]
    [Min(0f)] public float fadeOut = 0.2f;

    [Header("Horror Look")]
    [Tooltip("Subtle alpha flicker while typing.")]
    public bool enableFlicker = true;

    [Range(0f, 0.25f)] public float flickerAmplitude = 0.05f;
    [Range(0.5f, 20f)] public float flickerSpeed = 6f;
}
