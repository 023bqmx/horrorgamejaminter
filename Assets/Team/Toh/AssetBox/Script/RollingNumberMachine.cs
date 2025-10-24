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

    [Header("Rolling Settings")]
    [Tooltip("Total time before the first digit starts stopping")]
    public float rollDuration = 3f;
    [Tooltip("Delay between each digit stopping")]
    public float stopDelay = 0.4f;
    [Tooltip("How fast the digits change")]
    public float rollSpeed = 0.05f;

    [Header("UI Fade Settings")]
    [Tooltip("How long to wait before hiding the UI after rolling stops")]
    public float hideDelay = 1.5f;
    [Tooltip("How fast the UI fades out")]
    public float fadeOutDuration = 0.5f;
    [Tooltip("How long each result text fades in")]
    public float resultFadeDuration = 0.6f;
    [Tooltip("Delay between resultText1 and resultText2 fade-ins")]
    public float resultGapDelay = 0.4f;
    [Tooltip("Delay before showing the first result text after rolling stops")]
    public float resultStartDelay = 0.6f;

    [Header("Audio (voice after Digit3 starts)")]
    public AudioSource voiceSource;     // ลาก AudioSource มาใส่
    public AudioClip voiceClip;         // ลากไฟล์เสียงที่มีอยู่แล้ว
    [Range(0f,1f)] public float voiceVolume = 1f;
    public bool playOneShot = true;
    [Tooltip("ดีเลย์เล็กน้อยหลัง Digit3 เริ่ม ก่อนเล่นเสียง (ถ้าไม่ต้องการให้ใส่ 0)")]
    public float voiceDelayAfterDigit3Start = 0f;

    private bool isRolling = false;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        // Ensure CanvasGroup exists
        canvasGroup = numberSlotUI.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = numberSlotUI.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        // Hide result texts initially
        SetTextAlpha(resultText1, 0);
        SetTextAlpha(resultText2, 0);
    }

    public void TriggerRoll()
    {
        if (!isRolling)
            StartCoroutine(RollSequence());
    }

    private IEnumerator RollSequence()
    {
        isRolling = true;

        // Show UI with pop-in + fade-in
        numberSlotUI.SetActive(true);
        numberSlotUI.transform.localScale = Vector3.zero;

        float popTime = 0.25f;
        float t = 0;
        while (t < popTime)
        {
            t += Time.deltaTime;
            numberSlotUI.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t / popTime);
            canvasGroup.alpha = Mathf.Lerp(0, 1, t / popTime);
            yield return null;
        }

        // Start rolling all digits
        StartCoroutine(RollDigit(digit1, rollDuration + 0 * stopDelay));
        StartCoroutine(RollDigit(digit2, rollDuration + 1 * stopDelay));
        StartCoroutine(RollDigit(digit3, rollDuration + 2 * stopDelay));

        // >>> เล่นเสียง "หลังจากขึ้น Digit3" (เริ่มหมุนตัวที่สาม) <<<
        if (voiceSource != null && voiceClip != null)
        {
            if (voiceDelayAfterDigit3Start > 0f)
                yield return new WaitForSeconds(voiceDelayAfterDigit3Start);

            if (playOneShot) voiceSource.PlayOneShot(voiceClip, voiceVolume);
            else
            {
                voiceSource.clip = voiceClip;
                voiceSource.volume = voiceVolume;
                voiceSource.Play();
            }
        }

        // Wait until all digits finish rolling
        yield return new WaitForSeconds(rollDuration + 2 * stopDelay);

        // Wait a moment before showing result
        yield return new WaitForSeconds(resultStartDelay);

        // Show result texts one by one
        yield return StartCoroutine(FadeInResult(resultText1));
        yield return new WaitForSeconds(resultGapDelay);
        yield return StartCoroutine(FadeInResult(resultText2));

        // Wait before hiding
        yield return new WaitForSeconds(hideDelay);

        // Fade out all together
        yield return StartCoroutine(FadeOutUI());

        isRolling = false;
    }

    private IEnumerator RollDigit(TextMeshProUGUI digitText, float rollTime)
    {
        float elapsed = 0f;
        while (elapsed < rollTime)
        {
            digitText.text = Random.Range(0, 10).ToString();
            yield return new WaitForSeconds(rollSpeed);
            elapsed += rollSpeed;
        }

        // Final number
        digitText.text = Random.Range(0, 10).ToString();
    }

    private IEnumerator FadeInResult(TextMeshProUGUI text)
    {
        float elapsed = 0;
        SetTextAlpha(text, 0);

        while (elapsed < resultFadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0, 1, elapsed / resultFadeDuration);
            SetTextAlpha(text, alpha);
            yield return null;
        }

        SetTextAlpha(text, 1);
        yield break;
    }

    private IEnumerator FadeOutUI()
    {
        float elapsed = 0;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 0, elapsed / fadeOutDuration);
            canvasGroup.alpha = alpha;
            SetTextAlpha(resultText1, alpha);
            SetTextAlpha(resultText2, alpha);
            yield return null;
        }

        canvasGroup.alpha = 0;
        numberSlotUI.SetActive(false);
    }

    private void SetTextAlpha(TextMeshProUGUI text, float alpha)
    {
        if (text == null) return;
        Color c = text.color;
        c.a = alpha;
        text.color = c;
    }
}
