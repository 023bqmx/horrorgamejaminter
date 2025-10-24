using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ควบคุม "ภาพที่ 2" ให้แสดงเป็นจังหวะสุ่ม โดยภาพหลักอยู่นิ่งตลอด
/// ใช้ได้ทั้ง UI Image และ SpriteRenderer (ใส่อย่างใดอย่างหนึ่งบน GameObject นี้)
/// </summary>
public class RandomPulsingOverlay : MonoBehaviour
{
    [Header("Frames ของภาพที่ 2 (เช่น ตา: open->half->close)")]
    public List<Sprite> frames = new List<Sprite>();

    [Header("Random timing (ช่วงที่ภาพที่ 2 จะ 'โผล่' อีกครั้ง)")]
    public float idleMin = 2.0f;
    public float idleMax = 5.0f;

    [Header("Animation inside each pulse")]
    [Tooltip("เวลาต่อเฟรมตอนเล่น (วินาที)")]
    public float frameTime = 0.06f;
    [Tooltip("เล่นไป-กลับ (เปิด->ปิด->เปิด)")]
    public bool pingPong = true;
    [Tooltip("เวลาเฟดเข้า/ออกของ overlay (0 = ไม่เฟด)")]
    public float fadeDuration = 0.15f;

    [Header("Misc")]
    public bool playOnStart = true;
    public bool hideOverlayWhenIdle = true; // เวลารอสุ่ม ให้ซ่อน overlay

    // รองรับทั้งสองแบบ
    private Image uiImage;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        uiImage = GetComponent<Image>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (uiImage == null && spriteRenderer == null)
            Debug.LogError("[RandomPulsingOverlay] ต้องมี Image หรือ SpriteRenderer บน GameObject นี้");

        // เริ่มต้นซ่อนถ้าตั้งไว้
        if (hideOverlayWhenIdle) SetAlpha(0f);
    }

    void Start()
    {
        if (playOnStart) StartCoroutine(Loop());
    }

    IEnumerator Loop()
    {
        if (frames == null || frames.Count == 0) yield break;

        while (true)
        {
            // รอแบบสุ่มก่อนแสดงครั้งถัดไป
            float wait = Random.Range(idleMin, idleMax);
            yield return new WaitForSeconds(wait);

            // เล่นพัลส์หนึ่งครั้ง (โผล่ -> เล่นเฟรม -> จบ)
            yield return StartCoroutine(PlayOnePulse());
        }
    }

    IEnumerator PlayOnePulse()
    {
        // เฟดเข้า
        if (fadeDuration > 0f)
            yield return StartCoroutine(FadeTo(1f, fadeDuration));
        else
            SetAlpha(1f);

        // เล่นเฟรมไปข้างหน้า
        for (int i = 0; i < frames.Count; i++)
        {
            SetSprite(frames[i]);
            yield return new WaitForSeconds(frameTime);
        }

        // เล่นย้อนกลับถ้า pingPong
        if (pingPong && frames.Count > 1)
        {
            for (int i = frames.Count - 2; i >= 0; i--)
            {
                SetSprite(frames[i]);
                yield return new WaitForSeconds(frameTime);
            }
        }

        // เฟดออกหรือซ่อนทันที
        if (hideOverlayWhenIdle)
        {
            if (fadeDuration > 0f)
                yield return StartCoroutine(FadeTo(0f, fadeDuration));
            else
                SetAlpha(0f);
        }
    }

    // ------- helpers -------
    void SetSprite(Sprite s)
    {
        if (uiImage != null) uiImage.sprite = s;
        if (spriteRenderer != null) spriteRenderer.sprite = s;
    }

    void SetAlpha(float a)
    {
        if (uiImage != null)
        {
            var c = uiImage.color; c.a = a; uiImage.color = c;
        }
        if (spriteRenderer != null)
        {
            var c = spriteRenderer.color; c.a = a; spriteRenderer.color = c;
        }
    }

    IEnumerator FadeTo(float target, float duration)
    {
        float start = 1f;
        if (uiImage != null) start = uiImage.color.a;
        else if (spriteRenderer != null) start = spriteRenderer.color.a;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(start, target, t / duration);
            SetAlpha(a);
            yield return null;
        }
        SetAlpha(target);
    }
}
