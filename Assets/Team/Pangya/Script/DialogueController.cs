// DialogueController.cs
// Unity 6 / TextMeshPro
// Plays ONE SFX when the dialogue starts, ONE SFX at the beginning of each new line,
// and (optional) ONE SFX when the dialogue ends. No per-character SFX.

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
    [SerializeField] AudioSource voiceSource;  // per-line voice (optional)
    [SerializeField] AudioSource sfxSource;    // SFX for start-of-dialogue / start-of-line / end-of-dialogue

    [Header("SFX Clips (one-shots)")]
    [Tooltip("Played once when a dialogue sequence starts (when triggered).")]
    [SerializeField] AudioClip dialogueStartSfx;
    [Tooltip("Played at the start of EVERY line (new line).")]
    [SerializeField] AudioClip lineStartSfx;
    [Tooltip("Played once when the dialogue sequence finishes (after fade out).")]
    [SerializeField] AudioClip dialogueEndSfx;

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

        if (voiceSource) { voiceSource.loop = false; }
        if (sfxSource)   { sfxSource.loop   = false; }
    }

    public bool IsRunning => _isRunning;

    /// <summary>
    /// Start a dialogue sequence. (If you call this from a trigger, the start SFX will fire once.)
    /// </summary>
    public void RunSequence(DialogueSequence seq, System.Action onComplete = null)
    {
        if (seq == null) return;
        if (_isRunning)
        {
            Debug.LogWarning("[Dialogue] Ignored: already running.");
            return;
        }

        _isRunning = true; // latch immediately to prevent double-start in same frame
        _runRoutine = StartCoroutine(RunSequenceCo(seq, onComplete));
    }

    IEnumerator RunSequenceCo(DialogueSequence seq, System.Action onComplete)
    {
        // Dialogue START SFX (one-shot)
        PlayOneShotExclusive(dialogueStartSfx);

        // Enable UI interaction while visible
        if (dialogueGroup)
        {
            dialogueGroup.interactable = true;
            dialogueGroup.blocksRaycasts = true;
        }

        // Fade in
        yield return Fade(dialogueGroup, dialogueGroup ? dialogueGroup.alpha : 0f, 1f, seq.fadeIn);

        var originalAlpha = dialogueGroup ? dialogueGroup.alpha : 1f;
        float flickerT = 0f;

        for (int li = 0; li < seq.lines.Length; li++)
        {
            var line = seq.lines[li];

            if (speakerText) speakerText.text = line.speaker;
            if (bodyText) bodyText.text = "";

            // LINE START SFX (one-shot)
            PlayOneShotExclusive(lineStartSfx);

            // Voice at line start (stop previous to avoid overlap)
            if (voiceSource) voiceSource.Stop();
            if (voiceSource && line.voiceClip)
            {
                voiceSource.clip = line.voiceClip;
                voiceSource.volume = Mathf.Clamp01(line.voiceVolume);
                voiceSource.Play();
            }

            // Typewriter (no per-char SFX)
            string full = line.body ?? "";
            int revealed = 0;
            float cps = Mathf.Max(1f, seq.charsPerSecond);
            float perChar = 1f / cps;

            while (revealed < full.Length)
            {
                revealed++;
                if (bodyText) bodyText.text = full.Substring(0, revealed);

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

        // Dialogue END SFX (one-shot)
        PlayOneShotExclusive(dialogueEndSfx);

        // Cleanup UI & audio
        if (speakerText) speakerText.text = "";
        if (bodyText) bodyText.text = "";
        if (voiceSource) { voiceSource.Stop(); voiceSource.clip = null; }

        if (dialogueGroup)
        {
            dialogueGroup.interactable = false;
            dialogueGroup.blocksRaycasts = false;
        }

        _isRunning = false;
        onComplete?.Invoke();
    }

    // Plays a clip on sfxSource ensuring we don't overlap long tails:
    // We stop the source, set the clip, then Play (instead of PlayOneShot).
    void PlayOneShotExclusive(AudioClip clip, float volume = 1f)
    {
        if (!sfxSource || !clip) return;
        sfxSource.Stop();
        sfxSource.clip = clip;
        sfxSource.volume = Mathf.Clamp01(volume);
        sfxSource.Play();
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
