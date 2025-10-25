using System.Collections;
using UnityEngine;

public class BGMChaseDirector : MonoBehaviour
{
    [Header("Reference")]
    public AIController grinlock;

    [Header("Clips & Sources")]
    public AudioSource ambientSrc;     // 2D
    public AudioSource chaseSrc;       // 2D
    public AudioSource disturbSrc;     // optional 2D
    public AudioClip ambientClip;
    public AudioClip chaseClip;
    public AudioClip disturbClip;

    [Header("Crossfade")]
    public float fadeTime = 1.5f;
    public bool autoPlayAmbient = true;

    [Header("Levels (per track)")]
    [Range(0f, 1f)] public float ambientVolume = 1f;
    [Range(0f, 1f)] public float chaseVolume = 1f;
    [Min(0.05f)] public float ambientBasePitch = 1f;
    [Min(0.05f)] public float chaseBasePitch = 1f;
    public float pitchLerp = 6f;

    [Header("Disturb FX")]
    public bool useDisturbFX = true;
    public float fxIntensityLerp = 3f;
    public Vector2 lowpassCutoffRange = new Vector2(500f, 6000f);
    public float maxDistortion = 0.35f;
    public float wobbleDepth = 0.06f;
    public float wobbleFreq = 2.7f;

    [Header("Start Gate (Trigger)")]
    [Tooltip("เริ่มระบบเมื่อผู้เล่นเข้าโซนนี้")]
    public bool startOnPlayerEnter = true;
    public float startDelay = 5f;
    public string playerTag = "Player";
    [Tooltip("ปิดโซนหลังเริ่มแล้ว (กันยิงซ้ำ)")]
    public bool disableTriggerAfterStart = true;

    // --- internal ---
    float _fadeA = 1f, _fadeB = 0f;
    float _intensity = 0f;
    bool _isChasing = false;
    bool _starting = false;
    bool _started = false;

    AudioLowPassFilter _lpA, _lpB;
    AudioDistortionFilter _distA, _distB;

    Coroutine _fadeRoutine;
    Coroutine _pollRoutine;
    Coroutine _gateRoutine;

    void Reset()
    {
        if (!grinlock) grinlock = FindObjectOfType<AIController>();
    }

    void Awake()
    {
        EnsureSources();

        // เตรียม Trigger ชัวร์ ๆ
        if (startOnPlayerEnter)
        {
            var col = GetComponent<Collider>();
            if (!col)
            {
                col = gameObject.AddComponent<BoxCollider>();
                ((BoxCollider)col).isTrigger = true;
            }
            col.isTrigger = true;

            var rb = GetComponent<Rigidbody>();
            if (!rb) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;
        }
    }

    void OnEnable()
    {
        if (grinlock) grinlock.OnChaseStateChanged += HandleChaseChanged;
    }

    void OnDisable()
    {
        if (grinlock) grinlock.OnChaseStateChanged -= HandleChaseChanged;
    }

    void Start()
    {
        // ใส่คลิป
        if (ambientSrc && ambientClip) ambientSrc.clip = ambientClip;
        if (chaseSrc && chaseClip) chaseSrc.clip = chaseClip;
        if (disturbClip)
        {
            if (!disturbSrc) disturbSrc = CreateChildSource("BGM_Disturb");
            disturbSrc.clip = disturbClip; disturbSrc.loop = true; disturbSrc.spatialBlend = 0f;
        }

        // ใส่ฟิลเตอร์ (ปลอดภัย)
        if (useDisturbFX)
        {
            EnsureFiltersSafe(ambientSrc, ref _lpA, ref _distA);
            EnsureFiltersSafe(chaseSrc, ref _lpB, ref _distB);
        }

        if (startOnPlayerEnter)
        {
            // รอผู้เล่นเข้าโซนก่อน – ดับทุกอย่างไว้
            StopAllAudio();
            _started = false;
        }
        else
        {
            ActivateBGM(); // เริ่มเลยเหมือนเดิม
        }

        if (!grinlock) _pollRoutine = StartCoroutine(PollChase()); // fallback
    }

    // ===== Gate =====
    void OnTriggerEnter(Collider other)
    {
        if (!startOnPlayerEnter || _started || _starting) return;
        if (!IsPlayer(other)) return;

        _gateRoutine = StartCoroutine(StartAfterDelay());
    }

    IEnumerator StartAfterDelay()
    {
        _starting = true;
        Debug.Log($"[BGM] Player entered zone. Start in {startDelay:0.0}s...");
        yield return new WaitForSeconds(startDelay);
        ActivateBGM();
        _starting = false;

        if (disableTriggerAfterStart)
        {
            var col = GetComponent<Collider>();
            if (col) col.enabled = false;
        }
    }

    void ActivateBGM()
    {
        _started = true;

        _fadeA = 1f; _fadeB = 0f;
        if (ambientSrc) { ambientSrc.volume = ambientVolume * _fadeA; ambientSrc.pitch = ambientBasePitch; }
        if (chaseSrc) { chaseSrc.volume = chaseVolume * _fadeB; chaseSrc.pitch = chaseBasePitch; }

        if (autoPlayAmbient && ambientSrc && ambientSrc.clip)
        {
            ambientSrc.Play();
            Debug.Log($"[BGM] Play ambient '{ambientSrc.clip.name}'");
        }
    }

    void StopAllAudio()
    {
        foreach (var s in new[] { ambientSrc, chaseSrc, disturbSrc })
        {
            if (!s) continue;
            s.Stop();
            s.volume = 0f;
        }
        _fadeA = 0f; _fadeB = 0f;
    }

    // ===== Loop =====
    void Update()
    {
        if (!_started) return; // ยังไม่เริ่ม – เงียบไว้ก่อน

        // intensity
        float target = _isChasing ? 1f : 0f;
        _intensity = Mathf.MoveTowards(_intensity, target, Time.deltaTime * fxIntensityLerp);

        // FX
        if (useDisturbFX)
        {
            ApplyFX(ambientSrc, _lpA, _distA, _intensity);
            ApplyFX(chaseSrc, _lpB, _distB, _intensity);
        }

        // base volume (คูณ crossfade)
        if (ambientSrc) ambientSrc.volume = ambientVolume * _fadeA;
        if (chaseSrc) chaseSrc.volume = chaseVolume * _fadeB;

        // disturb layer
        if (disturbSrc && disturbClip)
        {
            float targetVol = _intensity;
            disturbSrc.volume = Mathf.MoveTowards(disturbSrc.volume, targetVol, Time.deltaTime * 2.5f);
            if (_intensity > 0.01f && !disturbSrc.isPlaying) disturbSrc.Play();
            if (_intensity < 0.01f && disturbSrc.isPlaying && disturbSrc.volume <= 0.01f) disturbSrc.Stop();
        }

        // pitch wobble
        float wob = (useDisturbFX ? Mathf.Sin(Time.time * wobbleFreq) * wobbleDepth * _intensity : 0f);
        float ambTargetPitch = ambientBasePitch * (1f + wob);
        float chaseTargetPitch = chaseBasePitch * (1f + wob);

        if (ambientSrc) ambientSrc.pitch = Mathf.Lerp(ambientSrc.pitch, ambTargetPitch, Time.unscaledDeltaTime * pitchLerp);
        if (chaseSrc) chaseSrc.pitch = Mathf.Lerp(chaseSrc.pitch, chaseTargetPitch, Time.unscaledDeltaTime * pitchLerp);
    }

    // ===== Helpers =====
    public void HandleChaseChanged(bool chasing)
    {
        _isChasing = chasing;
        if (!_started) return; // ยังไม่เปิดระบบ อย่า crossfade
        CrossfadeTo(chasing);
    }

    IEnumerator PollChase()
    {
        var ai = FindObjectOfType<AIController>();
        while (true)
        {
            if (!grinlock && ai) grinlock = ai;
            bool next = (grinlock ? grinlock.IsChasing : false);
            if (next != _isChasing) HandleChaseChanged(next);
            yield return new WaitForSeconds(0.1f);
        }
    }

    void CrossfadeTo(bool chase)
    {
        if (!_started) return;
        if (ambientSrc && ambientSrc.clip && !ambientSrc.isPlaying) ambientSrc.Play();
        if (chaseSrc && chaseSrc.clip && !chaseSrc.isPlaying) chaseSrc.Play();

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeRoutine(chase ? 0f : 1f, chase ? 1f : 0f));
    }

    IEnumerator FadeRoutine(float targetA, float targetB)
    {
        float t = 0f;
        float a0 = _fadeA, b0 = _fadeB;

        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeTime);
            _fadeA = Mathf.Lerp(a0, targetA, k);
            _fadeB = Mathf.Lerp(b0, targetB, k);

            if (ambientSrc) ambientSrc.volume = ambientVolume * _fadeA;
            if (chaseSrc) chaseSrc.volume = chaseVolume * _fadeB;

            yield return null;
        }

        _fadeA = targetA; _fadeB = targetB;
        if (ambientSrc) ambientSrc.volume = ambientVolume * _fadeA;
        if (chaseSrc) chaseSrc.volume = chaseVolume * _fadeB;

        if (ambientSrc && _fadeA <= 0.001f) ambientSrc.Stop();
        if (chaseSrc && _fadeB <= 0.001f) chaseSrc.Stop();
        _fadeRoutine = null;
    }

    void ApplyFX(AudioSource src, AudioLowPassFilter lp, AudioDistortionFilter dist, float x)
    {
        if (!src) return;
        if (lp) lp.cutoffFrequency = Mathf.Lerp(lowpassCutoffRange.y, lowpassCutoffRange.x, x);
        if (dist) dist.distortionLevel = maxDistortion * Mathf.Pow(x, 1.2f);
    }

    void EnsureSources()
    {
        if (!ambientSrc) ambientSrc = CreateChildSource("BGM_Ambient");
        if (!chaseSrc) chaseSrc = CreateChildSource("BGM_Chase");

        foreach (var s in new[] { ambientSrc, chaseSrc })
        {
            if (!s) continue;
            s.loop = true; s.playOnAwake = false; s.spatialBlend = 0f;
        }
    }

    AudioSource CreateChildSource(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var s = go.AddComponent<AudioSource>();
        s.outputAudioMixerGroup = null;
        return s;
    }

    bool IsPlayer(Collider c)
    {
        if (!c) return false;
        if (c.CompareTag(playerTag)) return true;
        return c.GetComponentInParent<StarterAssets.FirstPersonController>() != null;
    }

    // Inspector test
    [ContextMenu("Test Play Ambient (Force Activate)")]
    void TestPlayAmbient()
    {
        if (!_started) ActivateBGM();
        if (!ambientSrc || !ambientSrc.clip) { Debug.LogWarning("[BGM] ไม่มี ambientSrc/clip"); return; }
        _fadeA = 1f; _fadeB = 0f;
        ambientSrc.volume = ambientVolume * _fadeA;
        ambientSrc.pitch = ambientBasePitch;
        ambientSrc.Play();
        Debug.Log("[BGM] Test ambient playing");
    }

    // เพิ่มเข้าไปในคลาส BGMChaseDirector
    void EnsureFiltersSafe(AudioSource src, ref AudioLowPassFilter lp, ref AudioDistortionFilter dist)
    {
        if (!src) return;

        // หา/ใส่คอมโพเนนต์ให้ถ้ายังไม่มี
        if (!src.TryGetComponent(out lp)) lp = src.gameObject.AddComponent<AudioLowPassFilter>();
        if (!src.TryGetComponent(out dist)) dist = src.gameObject.AddComponent<AudioDistortionFilter>();

        // ค่าเริ่มต้น (เสียงโปร่ง, ไม่มี distortion)
        if (lp)
        {
            lp.enabled = true;
            lp.cutoffFrequency = lowpassCutoffRange.y;   // ค่าสูงสุด = โปร่ง
        }
        if (dist)
        {
            dist.enabled = true;
            dist.distortionLevel = 0f;
        }
    }
}
