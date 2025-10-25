using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using System.Collections;

public class TimelineFadeToScene : MonoBehaviour
{
    [Header("References")]
    public PlayableDirector director;   // ตัว Timeline
    public CanvasGroup blackFade;       // แผงดำเต็มจอ (CanvasGroup)

    [Header("Next Scene")]
    public string nextSceneName = "Stang";
    public int nextSceneBuildIndex = -1;  // เผื่ออยากใช้ Index แทนชื่อ

    [Header("Fade Settings")]
    public float fadeDuration = 0.6f;      // เวลาที่ใช้เฟดเป็นดำ
    public float holdBlack = 0.15f;        // ค้างดำก่อนโหลดซีน
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0,0,1,1);

    [Header("Options")]
    public bool playDirectorOnStart = false;   // ให้เล่น Timeline ทันทีที่เริ่มซีน
    public bool ensureStopEvent = true;        // บังคับให้หยุดจริง (DirectorWrapMode.None)

    void Reset()
    {
        // Auto-find เล็กน้อย
        director = GetComponent<PlayableDirector>();
        if (!blackFade && TryGetComponent(out CanvasGroup cg)) blackFade = cg;
    }

    void Awake()
    {
        // เตรียมแผงดำให้พร้อมใช้งาน (เริ่มโปร่ง)
        if (blackFade)
        {
            blackFade.gameObject.SetActive(true);
            if (blackFade.alpha > 0f) blackFade.alpha = 0f;
            blackFade.blocksRaycasts = false;
        }
    }

    void OnEnable()
    {
        if (!director)
        {
            Debug.LogWarning("[TimelineFadeToScene] PlayableDirector is not assigned.");
            return;
        }

        if (ensureStopEvent)
            director.extrapolationMode = DirectorWrapMode.None; // เพื่อให้ event stopped ถูกเรียก

        director.stopped += OnDirectorStopped;

        if (playDirectorOnStart && director.state != PlayState.Playing)
        {
            director.time = 0;
            director.Play();
        }
    }

    void OnDisable()
    {
        if (director) director.stopped -= OnDirectorStopped;
    }

    private void OnDirectorStopped(PlayableDirector d)
    {
        // เมื่อ Timeline จบ → เฟดดำและโหลดซีน
        StartCoroutine(FadeAndLoad());
    }

    private IEnumerator FadeAndLoad()
    {
        // เฟดเป็นดำ
        if (blackFade)
        {
            blackFade.blocksRaycasts = true;
            float t = 0f;
            float start = blackFade.alpha;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / fadeDuration);
                float e = fadeCurve != null ? fadeCurve.Evaluate(k) : k;
                blackFade.alpha = Mathf.LerpUnclamped(start, 1f, e);
                yield return null;
            }
            blackFade.alpha = 1f;

            if (holdBlack > 0f)
                yield return new WaitForSeconds(holdBlack);
        }

        LoadNextScene();
    }

    public void ForceSkipToNextScene()
    {
        // เผื่ออยากสั่งข้ามด้วย Signal/ปุ่ม
        StartCoroutine(FadeAndLoad());
    }

    private void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else if (nextSceneBuildIndex >= 0)
        {
            SceneManager.LoadScene(nextSceneBuildIndex);
        }
        else
        {
            Debug.LogError("[TimelineFadeToScene] Please set nextSceneName or nextSceneBuildIndex.");
        }
    }
}
