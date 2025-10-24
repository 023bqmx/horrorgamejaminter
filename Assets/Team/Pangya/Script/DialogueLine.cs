// DialogueLine.cs
using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    [TextArea(1, 2)] public string speaker;
    [TextArea(2, 6)] public string body;

    [Header("Audio (optional)")]
    public AudioClip voiceClip;         // Plays once at the start of this line
    [Range(0f, 1f)] public float voiceVolume = 1f;

    [Header("Timing (optional)")]
    [Tooltip("Extra hold after the full line is revealed.")]
    [Min(0f)] public float holdExtra = 0.35f;
}
