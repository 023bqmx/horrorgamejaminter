using UnityEngine;
using TMPro;
using System.Collections;

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

    [Header("Audio (play AFTER Result Text 2)")]
    public AudioSource voiceSource;
    public AudioClip voiceClip;
    [Range(0f, 1f)] public float voiceVolume = 1f;
    public bool playOneShot = true;
    [Tooltip("ดีเลย์หลัง Result Text 2 แสดงเสร็จ ก่อนเริ่มเสียง")]
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
            rollingSource.spatialBlend = 0f; // 2D
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

        // --- pop-in ---
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

        // ===== เริ่ม "เสียงหมุน" ล่วงหน้าก่อน Digit1 =====
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

        // ===== Start Rolling =====
        StartCoroutine(RollDigit(digit1, rollDuration + 0f * stopDelay, () => d1Done = true));
        StartCoroutine(RollDigit(digit2, rollDuration + 1f * stopDelay, () => d2Done = true));
        StartCoroutine(RollDigit(digit3, rollDuration + 2f * stopDelay, () => d3Done = true));

        // (ย้ายการเล่น voice ออกไปหลัง Result Text 2 แล้ว)

        // รอจนทั้งสามหลักหยุด
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

        // โชว์ผล
        yield return new WaitForSeconds(resultStartDelay);
        yield return StartCoroutine(FadeInResult(resultText1));
        yield return new WaitForSeconds(resultGapDelay);
        yield return StartCoroutine(FadeInResult(resultText2));

        // ===== เล่นเสียงหลัง Result Text 2 แสดงเสร็จ =====
        if (voiceSource && voiceClip)
        {
            if (voiceDelayAfterResult2 > 0f)
                yield return new WaitForSeconds(voiceDelayAfterResult2);

            if (playOneShot) voiceSource.PlayOneShot(voiceClip, voiceVolume);
            else { voiceSource.clip = voiceClip; voiceSource.volume = voiceVolume; voiceSource.Play(); }
        }

        // ซ่อน UI
        yield return new WaitForSeconds(hideDelay);
        yield return StartCoroutine(FadeOutUI());

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

            // เสียงติ๊กทุกครั้งที่เปลี่ยนเลข (ถ้ามี)
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
