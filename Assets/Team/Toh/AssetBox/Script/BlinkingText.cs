using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class FadeBlinkUI : MonoBehaviour
{
    [Header("UI Elements (Assign one or more)")]
    public Text uiText;
    public TextMeshProUGUI tmpText;
    public Image uiImage;
    public RawImage rawImage;

    [Header("Timing Settings")]
    public float blinkInterval = 10f;   // Time between fades
    public float visibleDuration = 4f;  // Time fully visible before fading out
    public float fadeDuration = 1f;     // Fade in/out time

    private CanvasRenderer textRenderer;
    private float timer = 0f;
    private bool isFading = false;

    void Start()
    {
        SetAlpha(0f); // Start invisible
    }

    void Update()
    {
        if (isFading) return;

        timer += Time.deltaTime;
        if (timer >= blinkInterval)
        {
            StartCoroutine(FadeSequence());
            timer = 0f;
        }
    }

    private IEnumerator FadeSequence()
    {
        isFading = true;

        // Fade In
        yield return Fade(0f, 1f);

        // Stay visible
        yield return new WaitForSeconds(visibleDuration);

        // Fade Out
        yield return Fade(1f, 0f);

        isFading = false;
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, t / fadeDuration);
            SetAlpha(alpha);
            yield return null;
        }
        SetAlpha(endAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (uiText != null)
        {
            Color c = uiText.color;
            c.a = alpha;
            uiText.color = c;
        }

        if (tmpText != null)
        {
            Color c = tmpText.color;
            c.a = alpha;
            tmpText.color = c;
        }

        if (uiImage != null)
        {
            Color c = uiImage.color;
            c.a = alpha;
            uiImage.color = c;
        }

        if (rawImage != null)
        {
            Color c = rawImage.color;
            c.a = alpha;
            rawImage.color = c;
        }
    }
}