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
    [Tooltip("ใส่ GameData จาก Inspector ได้เลย (ถ้าไม่ใส่จะพยายามหาในตัวเอง)")]
    public GameData gameData;

    [Header("Rolling Settings")]
    [Tooltip("เวลารวมก่อนเริ่มหยุดหลักแรก")]
    public float rollDuration = 3f;
    [Tooltip("ดีเลย์หยุดระหว่างแต่ละหลัก")]
    public float stopDelay = 0.4f;
    [Tooltip("ความถี่การเปลี่ยนตัวเลข (วินาที/ครั้ง)")]
    public float rollSpeed = 0.05f;

    [Header("UI Anim/Fade")]
    [Tooltip("เวลา pop-in UI")]
    public float popInDuration = 0.25f;
    [Tooltip("รอก่อนซ่อน UI หลังโชว์ผล")]
    public float hideDelay = 1.5f;
    [Tooltip("เวลา fade-out UI")]
    public float fadeOutDuration = 0.5f;
    [Tooltip("เวลา fade-in ของ result แต่ละอัน")]
    public float resultFadeDuration = 0.6f;
    [Tooltip("ดีเลย์คั่นระหว่าง resultText1 -> resultText2")]
    public float resultGapDelay = 0.4f;
    [Tooltip("รอก่อนเริ่มโชว์ผลหลังหมุนหยุด")]
    public float resultStartDelay = 0.6f;

    [Header("Audio (voice after Digit3 starts)")]
    public AudioSource voiceSource;
    public AudioClip voiceClip;
    [Range(0f, 1f)] public float voiceVolume = 1f;
    public bool playOneShot = true;
    [Tooltip("ดีเลย์เล็กน้อยหลัง 'เริ่ม' Digit3 ก่อนเล่นเสียง")]
    public float voiceDelayAfterDigit3Start = 0f;

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

        // --- start rolling all digits ---
        StartCoroutine(RollDigit(digit1, rollDuration + 0f * stopDelay, () => d1Done = true));
        StartCoroutine(RollDigit(digit2, rollDuration + 1f * stopDelay, () => d2Done = true));
        StartCoroutine(RollDigit(digit3, rollDuration + 2f * stopDelay, () => d3Done = true));

        // --- voice: after digit3 has STARTED ---
        if (voiceSource && voiceClip)
        {
            if (voiceDelayAfterDigit3Start > 0f)
                yield return new WaitForSeconds(voiceDelayAfterDigit3Start);

            if (playOneShot) voiceSource.PlayOneShot(voiceClip, voiceVolume);
            else { voiceSource.clip = voiceClip; voiceSource.volume = voiceVolume; voiceSource.Play(); }
        }

        // --- wait until all digits finished (robust vs. drift) ---
        yield return new WaitUntil(() => d1Done && d2Done && d3Done);

        // --- save result (guard null) ---
        int v1 = SafeParse(digit1);
        int v2 = SafeParse(digit2);
        int v3 = SafeParse(digit3);

        if (gameData)
        {
            gameData.Digit1 = v1;
            gameData.Digit2 = v2;
            gameData.Digit3 = v3;
        }
        Debug.Log($"[Saved Result] {v1}{v2}{v3}");

        // --- show result texts ---
        yield return new WaitForSeconds(resultStartDelay);
        yield return StartCoroutine(FadeInResult(resultText1));
        yield return new WaitForSeconds(resultGapDelay);
        yield return StartCoroutine(FadeInResult(resultText2));

        // --- hide & reset ---
        yield return new WaitForSeconds(hideDelay);
        yield return StartCoroutine(FadeOutUI());

        isRolling = false;
    }

    private IEnumerator RollDigit(TextMeshProUGUI digitText, float rollTime, System.Action onDone)
    {
        if (!digitText) { onDone?.Invoke(); yield break; }

        float elapsed = 0f;
        while (elapsed < rollTime)
        {
            digitText.text = Random.Range(0, 10).ToString();
            float step = Mathf.Max(0.0001f, rollSpeed);
            yield return new WaitForSeconds(step);
            elapsed += step;
        }

        // final settle
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

        // reset result alphas soรอบหน้าเริ่มที่ 0
        SetTextAlpha(resultText1, 0f);
        SetTextAlpha(resultText2, 0f);
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
