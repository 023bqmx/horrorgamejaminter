// DialogueController.cs
// Unity 6 / TextMeshPro
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DialogueController : MonoBehaviour
{
    public static DialogueController Instance { get; private set; }

    [Header("UI")]
    [SerializeField] CanvasGroup dialogueGroup; // Parent box (CanvasGroup for fade)
    [SerializeField] TMP_Text speakerText;
    [SerializeField] TMP_Text bodyText;
    [SerializeField] Image backdrop; // optional background image (can be null)

    [Header("Audio")]
    [SerializeField] AudioSource voiceSource;  // for per-line voice
    [SerializeField] AudioSource sfxSource;    // for per-char bleeps

    [Header("Safety")]
    [SerializeField] bool hideOnAwake = true;

    bool _isRunning;
    Coroutine _runRoutine;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (dialogueGroup)
        {
            dialogueGroup.alpha = hideOnAwake ? 0f : dialogueGroup.alpha;
            dialogueGroup.interactable = false;
            dialogueGroup.blocksRaycasts = false;
        }
        if (speakerText) speakerText.text = "";
        if (bodyText) bodyText.text = "";
    }

    public bool IsRunning => _isRunning;

    public void RunSequence(DialogueSequence seq, System.Action onComplete = null)
    {
        if (_isRunning)
        {
            // Queueing could be added; for now, ignore overlapped calls.
            Debug.LogWarning("[Dialogue] Ignored: already running.");
            return;
        }
        _runRoutine = StartCoroutine(RunSequenceCo(seq, onComplete));
    }

    IEnumerator RunSequenceCo(DialogueSequence seq, System.Action onComplete)
    {
        _isRunning = true;

        // Fade in
        yield return Fade(dialogueGroup, 0f, 1f, seq.fadeIn);

        var originalAlpha = dialogueGroup ? dialogueGroup.alpha : 1f;
        float flickerT = 0f;

        for (int li = 0; li < seq.lines.Length; li++)
        {
            var line = seq.lines[li];

            if (speakerText) speakerText.text = line.speaker;
            if (bodyText) bodyText.text = "";

            // Voice at line start
            if (voiceSource && line.voiceClip)
            {
                voiceSource.clip = line.voiceClip;
                voiceSource.volume = line.voiceVolume;
                voiceSource.Play();
            }

            // Typewriter
            string full = line.body ?? "";
            int revealed = 0;
            float cps = Mathf.Max(1f, seq.charsPerSecond);
            float perChar = 1f / cps;

            while (revealed < full.Length)
            {
                revealed++;
                if (bodyText) bodyText.text = full.Substring(0, revealed);

                // Optional per-char SFX
                if (seq.charSfx && sfxSource && (revealed % Mathf.Max(1, seq.sfxEveryNChars) == 0))
                {
                    sfxSource.PlayOneShot(seq.charSfx, seq.charSfxVolume);
                }

                // Subtle horror flicker while typing
                if (seq.enableFlicker && dialogueGroup)
                {
                    flickerT += Time.deltaTime * seq.flickerSpeed;
                    dialogueGroup.alpha = originalAlpha + Mathf.Sin(flickerT) * seq.flickerAmplitude;
                }

                // Base delay
                float d = perChar;

                // Extra punctuation delay
                char c = full[revealed - 1];
                if (c == '.' || c == ',' || c == '!' || c == '?') d += seq.punctuationDelay;

                yield return new WaitForSeconds(d);
            }

            // Stop flicker, ensure alpha resets
            if (dialogueGroup) dialogueGroup.alpha = originalAlpha;

            // Per-line hold
            float hold = seq.perLineHold + line.holdExtra;
            if (hold > 0f) yield return new WaitForSeconds(hold);
        }

        // Fade out
        yield return Fade(dialogueGroup, dialogueGroup ? dialogueGroup.alpha : 1f, 0f, seq.fadeOut);

        if (speakerText) speakerText.text = "";
        if (bodyText) bodyText.text = "";

        _isRunning = false;
        onComplete?.Invoke();
    }

    static IEnumerator Fade(CanvasGroup g, float from, float to, float time)
    {
        if (!g || time <= 0f) { if (g) g.alpha = to; yield break; }
        float t = 0f;
        g.alpha = from;
        while (t < time)
        {
            t += Time.deltaTime;
            g.alpha = Mathf.Lerp(from, to, t / time);
            yield return null;
        }
        g.alpha = to;
    }
}
