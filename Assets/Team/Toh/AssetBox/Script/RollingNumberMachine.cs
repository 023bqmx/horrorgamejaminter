using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Playables; // สำหรับ PlayableDirector (Timeline)

public class RollingNumberMachine : MonoBehaviour
{
    [Header("UI References")]
    public GameObject numberSlotUI;
    public TextMeshProUGUI digit1;
    public TextMeshProUGUI digit2;
    public TextMeshProUGUI digit3;
    public TextMeshProUGUI resultText1;
    public TextMeshProUGUI resultText2;

    [Header("Data")]
    public GameData gameData;

    [Header("Rolling Settings")]
    public float rollDuration = 3f;
    public float stopDelay = 0.4f;
    public float rollSpeed = 0.05f;

    [Header("UI Anim/Fade")]
    public float popInDuration = 0.25f;
    public float hideDelay = 1.5f;
    public float fadeOutDuration = 0.5f;
    public float resultFadeDuration = 0.6f;
    public float resultGapDelay = 0.4f;
    public float resultStartDelay = 0.6f;

    [Header("Screen Fade (before + after UI)")]
    [Tooltip("CanvasGroup ของภาพ/Panel สีดำเต็มจอ (อยู่บนสุด)")]
    public CanvasGroup blackFade;
    [Tooltip("เวลาเฟดเข้าดำก่อนโชว์ UI")]
    public float preFadeToBlackDuration = 0.6f;
    [Tooltip("ค้างดำไว้ก่อนเริ่ม UI")]
    public float preHoldBlack = 0.2f;

    [Tooltip("หน่วงก่อนเริ่ม 'สว่างขึ้น' หลัง UI จบทั้งหมด")]
    public float eyeOpenDelay = 0.0f;
    [Tooltip("ระยะเวลาค่อยๆสว่างเหมือนลืมตา")]
    public float eyeOpenDuration = 0.9f;
    [Tooltip("เส้นโค้งการสว่าง (0→1)")]
    public AnimationCurve eyeOpenCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Audio (play AFTER Result Text 2)")]
    public AudioSource voiceSource;
    public AudioClip voiceClip;
    [Range(0f, 1f)] public float voiceVolume = 1f;
    public bool playOneShot = true;
    public float voiceDelayAfterResult2 = 0f;

    [Header("Rolling Audio (Digit1 start → Digit3 stop)")]
    public AudioSource rollingSource;
    public AudioClip rollingLoopClip;
    [Range(0f,1f)] public float rollingLoopVolume = 0.6f;
    public float rollingLoopFade = 0.1f;
    public AudioClip tickClip;
    [Range(0f,1f)] public float tickVolume = 0.6f;

    [Tooltip("ให้เสียงหมุนเริ่มก่อน Digit1 เริ่มหมุนกี่วินาที")]
    public float preRollLeadTime = 1f;

    [Header("Post UI → Timeline / Animator")]
    public PlayableDirector timelineDirector;
    public float postPlayDelay = 0f;
    public Animator nextAnimator;
    public string animTriggerName = "Play";

    [Header("Camera Switch")]
    public Camera playerCamera;
    public Camera timelineCamera;

    private bool isRolling = false;
    private bool d1Done, d2Done, d3Done;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        if (!numberSlotUI) { Debug.LogError("[RollingNumberMachine] numberSlotUI is null"); enabled = false; return; }

        canvasGroup = numberSlotUI.GetComponent<CanvasGroup>();
        if (!canvasGroup) canvasGroup = numberSlotUI.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        if (!gameData) gameData = GetComponent<GameData>();

        SetTextAlpha(resultText1, 0f);
        SetTextAlpha(resultText2, 0f);
        numberSlotUI.SetActive(false);

        if (rollingSource == null)
        {
            rollingSource = gameObject.AddComponent<AudioSource>();
            rollingSource.playOnAwake = false;
            rollingSource.spatialBlend = 0f;
        }

        // เตรียมแผ่นดำ
        if (blackFade != null)
        {
            blackFade.gameObject.SetActive(true);
            blackFade.alpha = 0f;           // เริ่มสว่าง
            blackFade.blocksRaycasts = true; // กันคลิกลอด (ตามต้องการ)
        }
    }

    public void TriggerRoll()
    {
        if (isRolling) return;
        StartCoroutine(RollSequence());
    }

    private IEnumerator RollSequence()
    {
        isRolling = true;
        d1Done = d2Done = d3Done = false;

        // ===== 1) ดำเข้าก่อนขึ้น UI =====
        if (blackFade != null && preFadeToBlackDuration > 0f)
        {
            yield return StartCoroutine(FadeCanvasGroupCurve(blackFade, blackFade.alpha, 1f, preFadeToBlackDuration, AnimationCurve.EaseInOut(0,0,1,1)));
            if (preHoldBlack > 0f) yield return new WaitForSeconds(preHoldBlack);
        }

        // ===== 2) Pop-in UI =====
        numberSlotUI.SetActive(true);
        numberSlotUI.transform.localScale = Vector3.zero;

        float t = 0f;
        while (t < popInDuration)
        {
            t += Time.deltaTime;
            float k = t / popInDuration;
            numberSlotUI.transform.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, k);
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, k);
            yield return null;
        }
        numberSlotUI.transform.localScale = Vector3.one;
        canvasGroup.alpha = 1f;

        // ===== 3) เสียงหมุน (เริ่มก่อน Digit1) =====
        if (rollingLoopClip != null && rollingSource != null)
        {
            rollingSource.clip = rollingLoopClip;
            rollingSource.loop = true;
            rollingSource.volume = 0f;
            rollingSource.Play();
            if (rollingLoopFade > 0f)
                yield return StartCoroutine(FadeAudio(rollingSource, 0f, rollingLoopVolume, rollingLoopFade));
            else
                rollingSource.volume = rollingLoopVolume;
        }

        if (preRollLeadTime > 0f)
            yield return new WaitForSeconds(preRollLeadTime);

        // ===== 4) เริ่มหมุนตัวเลข =====
        StartCoroutine(RollDigit(digit1, rollDuration + 0f * stopDelay, () => d1Done = true));
        StartCoroutine(RollDigit(digit2, rollDuration + 1f * stopDelay, () => d2Done = true));
        StartCoroutine(RollDigit(digit3, rollDuration + 2f * stopDelay, () => d3Done = true));

        yield return new WaitUntil(() => d1Done && d2Done && d3Done);

        // ปิดเสียงหมุน
        if (rollingSource != null && rollingSource.isPlaying)
        {
            if (rollingLoopFade > 0f)
                yield return StartCoroutine(FadeAudio(rollingSource, rollingSource.volume, 0f, rollingLoopFade));
            rollingSource.Stop();
            rollingSource.volume = rollingLoopVolume;
        }

        // บันทึกผล
        int v1 = SafeParse(digit1);
        int v2 = SafeParse(digit2);
        int v3 = SafeParse(digit3);
        if (gameData) { gameData.Digit1 = v1; gameData.Digit2 = v2; gameData.Digit3 = v3; }
        Debug.Log($"[Saved Result] {v1}{v2}{v3}");

        // ===== 5) โชว์ผล =====
        yield return new WaitForSeconds(resultStartDelay);
        yield return StartCoroutine(FadeInResult(resultText1));
        yield return new WaitForSeconds(resultGapDelay);
        yield return StartCoroutine(FadeInResult(resultText2));

        // เล่นเสียงหลัง Result Text 2
        if (voiceSource && voiceClip)
        {
            if (voiceDelayAfterResult2 > 0f)
                yield return new WaitForSeconds(voiceDelayAfterResult2);

            if (playOneShot) voiceSource.PlayOneShot(voiceClip, voiceVolume);
            else { voiceSource.clip = voiceClip; voiceSource.volume = voiceVolume; voiceSource.Play(); }
        }

        // ===== 6) ซ่อน UI =====
        yield return new WaitForSeconds(hideDelay);
        yield return StartCoroutine(FadeOutUI());

        // ===== 7) “ลืมตา” ค่อยๆสว่างขึ้น =====
        if (blackFade != null && eyeOpenDuration > 0f)
        {
            if (eyeOpenDelay > 0f) yield return new WaitForSeconds(eyeOpenDelay);
            // จากดำ (1) → สว่าง (0)
            yield return StartCoroutine(FadeCanvasGroupCurve(blackFade, 1f, 0f, eyeOpenDuration, eyeOpenCurve));
            // เปิดคลิกลอดได้หลังสว่าง (ถ้าต้องการ)
            blackFade.blocksRaycasts = false;
        }

        // ===== 8) สลับกล้อง → เล่น Timeline/Animator =====
        yield return StartCoroutine(PlayPostSequence());

        isRolling = false;
    }

    private IEnumerator RollDigit(TextMeshProUGUI digitText, float rollTime, System.Action onDone)
    {
        if (!digitText) { onDone?.Invoke(); yield break; }

        float elapsed = 0f;
        float step = Mathf.Max(0.0001f, rollSpeed);

        while (elapsed < rollTime)
        {
            digitText.text = Random.Range(0, 10).ToString();

            if (tickClip != null && rollingSource != null)
                rollingSource.PlayOneShot(tickClip, tickVolume);

            yield return new WaitForSeconds(step);
            elapsed += step;
        }

        digitText.text = Random.Range(0, 10).ToString();
        onDone?.Invoke();
    }

    private IEnumerator FadeInResult(TextMeshProUGUI text)
    {
        if (!text) yield break;

        float elapsed = 0f;
        SetTextAlpha(text, 0f);

        while (elapsed < resultFadeDuration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Clamp01(elapsed / resultFadeDuration);
            SetTextAlpha(text, a);
            yield return null;
        }
        SetTextAlpha(text, 1f);
    }

    private IEnumerator FadeOutUI()
    {
        float elapsed = 0f;
        float startAlpha = canvasGroup ? canvasGroup.alpha : 1f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
            if (canvasGroup) canvasGroup.alpha = a;
            SetTextAlpha(resultText1, a);
            SetTextAlpha(resultText2, a);
            yield return null;
        }

        if (canvasGroup) canvasGroup.alpha = 0f;
        numberSlotUI.SetActive(false);
        SetTextAlpha(resultText1, 0f);
        SetTextAlpha(resultText2, 0f);
    }

    // ---------- play Timeline / Animator ----------
    private IEnumerator PlayPostSequence()
    {
        SwitchToTimelineCamera();

        if (postPlayDelay > 0f)
            yield return new WaitForSeconds(postPlayDelay);

        if (timelineDirector != null)
        {
            timelineDirector.time = 0;
            timelineDirector.Play();
        }

        if (nextAnimator != null && !string.IsNullOrEmpty(animTriggerName))
        {
            nextAnimator.ResetTrigger(animTriggerName);
            nextAnimator.SetTrigger(animTriggerName);
        }
    }

    private void SwitchToTimelineCamera()
    {
        if (playerCamera != null)
        {
            var al = playerCamera.GetComponent<AudioListener>();
            if (al) al.enabled = false;
            playerCamera.enabled = false;
            if (playerCamera.gameObject.activeSelf) playerCamera.gameObject.SetActive(false);
        }

        if (timelineCamera != null)
        {
            if (!timelineCamera.gameObject.activeSelf) timelineCamera.gameObject.SetActive(true);
            timelineCamera.enabled = true;
            var al = timelineCamera.GetComponent<AudioListener>();
            if (al) al.enabled = true;
        }
        else
        {
            Debug.LogWarning("[RollingNumberMachine] timelineCamera is not assigned.");
        }
    }

    // ---------- helpers ----------
    private IEnumerator FadeAudio(AudioSource src, float from, float to, float dur)
    {
        if (src == null || dur <= 0f) yield break;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(from, to, t / dur);
            yield return null;
        }
        src.volume = to;
    }

    private IEnumerator FadeCanvasGroupCurve(CanvasGroup cg, float from, float to, float dur, AnimationCurve curve)
    {
        if (cg == null) yield break;
        float t = 0f;
        cg.alpha = from;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            cg.alpha = Mathf.LerpUnclamped(from, to, curve != null ? curve.Evaluate(k) : k);
            yield return null;
        }
        cg.alpha = to;
    }

    private void SetTextAlpha(TextMeshProUGUI text, float alpha)
    {
        if (!text) return;
        var c = text.color; c.a = alpha; text.color = c;
    }

    private int SafeParse(TextMeshProUGUI text)
    {
        if (!text) return 0;
        return int.TryParse(text.text, out var v) ? v : 0;
    }
}
