using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VignetteFaintOnTimeline : MonoBehaviour
{
    [Header("References")]
    public PlayableDirector director;
    public Volume volume;

    [Header("Idle Pulse (normal state)")]
    [Tooltip("เปิดพัลส์เบาๆ ตอนปกติ (ก่อน/หลัง Timeline)")]
    public bool idlePulseEnabled = true;
    [Range(0f,1f)] public float idleBaseIntensity = 0.12f;
    [Range(0f,1f)] public float idleAmplitude     = 0.04f;   // เบาๆ
    [Tooltip("รอบ/วินาที (ยิ่งมากยิ่งเร็ว)")]
    public float idleFrequency   = 0.7f;
    [Tooltip("ความนุ่มเวลาขยับเข้าหาค่าเป้าหมาย")]
    public float idleSmoothing   = 5f;

    [Header("Faint Effect")]
    [Range(0f, 1f)] public float startIntensity = 0.12f;
    [Range(0f, 1f)] public float endIntensity   = 0.95f;
    public float flickerDuration = 4f;
    [Range(0f, 1f)] public float pulseAmplitude = 0.18f;
    public float startFrequency = 8f;
    public float endFrequency   = 1.5f;
    public float finalRamp      = 0.6f;
    public float holdAtEnd      = 0.5f;

    [Header("Recovery")]
    public bool recoverOnTimelineStop = true;
    public float recoverDuration = 0.4f;

    private Vignette _vig;
    private float _originalIntensity = 0.12f;
    private Coroutine _coFaint, _coIdle, _coRecover;

    void Reset()
    {
        director ??= GetComponent<PlayableDirector>();
        if (volume == null) volume = FindObjectOfType<Volume>();
    }

    void OnEnable()
    {
        if (director != null) {
            director.played  += OnTimelinePlayed;
            director.stopped += OnTimelineStopped;
        }
        CacheVignette();
        if (idlePulseEnabled) StartIdle();
    }

    void OnDisable()
    {
        if (director != null) {
            director.played  -= OnTimelinePlayed;
            director.stopped -= OnTimelineStopped;
        }
        StopAllCoroutines();
    }

    void CacheVignette()
    {
        if (volume == null || volume.profile == null) return;
        if (volume.profile.TryGet(out _vig))
        {
            _originalIntensity = _vig.intensity.value;
        }
        else
        {
            Debug.LogWarning("[VignetteFaintOnTimeline] Volume ไม่มี Vignette component");
        }
    }

    // ===== Public controls =====
    public void StartEffect()  { if (_vig == null) { CacheVignette(); if (_vig == null) return; } StopIdle(); StartFaint(); }
    public void StopAndRecover()
    {
        if (_vig == null) return;
        if (_coFaint != null) { StopCoroutine(_coFaint); _coFaint = null; }
        if (_coRecover != null) StopCoroutine(_coRecover);
        if (recoverOnTimelineStop) _coRecover = StartCoroutine(RecoverRoutine(true));
    }

    // ===== Timeline events =====
    private void OnTimelinePlayed(PlayableDirector d)  => StartEffect();
    private void OnTimelineStopped(PlayableDirector d) => StopAndRecover();

    // ===== Idle Pulse =====
    void StartIdle()
    {
        if (_vig == null) { CacheVignette(); if (_vig == null) return; }
        if (_coIdle != null) StopCoroutine(_coIdle);
        _coIdle = StartCoroutine(IdleRoutine());
    }
    void StopIdle()
    {
        if (_coIdle != null) { StopCoroutine(_coIdle); _coIdle = null; }
    }

    IEnumerator IdleRoutine()
    {
        _vig.intensity.overrideState = true;
        // เริ่มที่ค่าปัจจุบัน เพื่อความลื่น
        float current = _vig.intensity.value;
        while (true)
        {
            // ค่าเป้าหมาย = base + sin * amp (เบาๆ)
            float target = Mathf.Clamp01(
                idleBaseIntensity + Mathf.Sin(Time.time * Mathf.PI * 2f * idleFrequency) * idleAmplitude
            );
            // ไล่เข้าเป้าหมายแบบนุ่มๆ
            current = Mathf.Lerp(current, target, Time.deltaTime * Mathf.Max(0.01f, idleSmoothing));
            _vig.intensity.value = current;
            yield return null;
        }
    }

    // ===== Faint =====
    void StartFaint()
    {
        if (_coFaint != null) StopCoroutine(_coFaint);
        _coFaint = StartCoroutine(FaintRoutine());
    }

    IEnumerator FaintRoutine()
    {
        _vig.intensity.overrideState = true;
        _vig.intensity.value = startIntensity;

        float t = 0f;
        while (t < flickerDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / flickerDuration);

            float baseI = Mathf.Lerp(startIntensity, endIntensity, k);
            float freq  = Mathf.Lerp(startFrequency, endFrequency, k);
            float amp   = pulseAmplitude * (1f - 0.6f * k);
            float pulse = Mathf.Sin(Time.time * freq * Mathf.PI * 2f) * amp;

            _vig.intensity.value = Mathf.Clamp01(baseI + pulse);
            yield return null;
        }

        // ramp เข้าหา endIntensity
        float from = _vig.intensity.value;
        float tt = 0f;
        while (tt < finalRamp)
        {
            tt += Time.deltaTime;
            _vig.intensity.value = Mathf.Lerp(from, endIntensity, tt / finalRamp);
            yield return null;
        }
        _vig.intensity.value = endIntensity;

        if (holdAtEnd > 0f) yield return new WaitForSeconds(holdAtEnd);
        _coFaint = null;
    }

    // ===== Recover =====
    IEnumerator RecoverRoutine(bool restartIdleAfter)
    {
        float from = _vig.intensity.value;
        float t = 0f;
        while (t < recoverDuration)
        {
            t += Time.deltaTime;
            _vig.intensity.value = Mathf.Lerp(from, _originalIntensity, t / recoverDuration);
            yield return null;
        }
        _vig.intensity.value = _originalIntensity;
        _vig.intensity.overrideState = true; // คุมต่อสำหรับ Idle

        _coRecover = null;

        if (idlePulseEnabled && restartIdleAfter) StartIdle();
    }
}
