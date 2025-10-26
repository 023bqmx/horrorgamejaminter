using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using System.Collections;

[DisallowMultipleComponent]
public class TimelineEndLoadScene : MonoBehaviour
{
    [Header("Refs")]
    public PlayableDirector director;

    [Header("Next Scene")]
    public string nextSceneName = "";     // กรอกชื่อซีน หรือ…
    public int nextSceneBuildIndex = -1;  // …กรอก Build Index (เลือกอย่างใดอย่างหนึ่ง)

    [Header("Options")]
    public bool playOnStart = false;      // ให้เริ่ม Timeline ตอน Start เลยไหม
    public bool useAsync = false;         // โหลดแบบ Async ไหม

    bool _fired;

    void Reset()
    {
        director = GetComponent<PlayableDirector>();
    }

    void Awake()
    {
        if (!director) director = GetComponent<PlayableDirector>();
        if (!director)
        {
            Debug.LogError("[TimelineEndLoadScene] No PlayableDirector assigned/found.");
            enabled = false;
            return;
        }
    }

    void OnEnable()
    {
        // บังคับให้ “หยุดจริง” ตอนจบ เพื่อให้ stopped ยิงแน่
        director.extrapolationMode = DirectorWrapMode.None;
        director.stopped += OnDirectorStopped;

        if (playOnStart)
        {
            director.time = 0;
            director.Play();
        }

        // สำรอง: เฝ้าดูจนจบ/เวลาถึงปลาย แล้วค่อยโหลด (กันเคส stopped ไม่ยิง)
        StartCoroutine(GuardWaitForEnd());
    }

    void OnDisable()
    {
        if (director) director.stopped -= OnDirectorStopped;
    }

    void OnDirectorStopped(PlayableDirector d)
    {
        BeginLoad();
    }

    IEnumerator GuardWaitForEnd()
    {
        // รอจนเล่น เเล้วคอยจนกว่าจะหยุด หรือเวลาแตะปลาย
        // (ถ้าเริ่มมาช้า ก็ยังเข้าเงื่อนไขเมื่อหยุด)
        while (director != null)
        {
            if (director.state != PlayState.Playing)
            {
                // หยุดแล้วและเคยเล่นไปบ้าง -> โหลดเลย
                if (director.time > 0.01 || director.duration <= 0) { BeginLoad(); yield break; }
            }
            else
            {
                // ยังเล่นอยู่; ถ้าเวลาจ่อปลายมาก ๆ ก็สั่งโหลด (กันกรณี stopped ไม่มา)
                if (director.duration > 0 && director.time >= director.duration - 0.02)
                {
                    BeginLoad();
                    yield break;
                }
            }
            yield return null;
        }
    }

    void BeginLoad()
    {
        if (_fired) return;
        _fired = true;

        if (!ValidateScene(out string reason))
        {
            Debug.LogError("[TimelineEndLoadScene] " + reason);
            return;
        }

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            if (useAsync) SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Single);
            else SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
        }
        else
        {
            if (useAsync) SceneManager.LoadSceneAsync(nextSceneBuildIndex, LoadSceneMode.Single);
            else SceneManager.LoadScene(nextSceneBuildIndex, LoadSceneMode.Single);
        }
    }

    bool ValidateScene(out string reason)
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                reason = $"Scene '{nextSceneName}' ไม่อยู่ใน Build Settings หรือสะกดผิด";
                return false;
            }
            reason = null; return true;
        }
        if (nextSceneBuildIndex >= 0)
        {
            if (!Application.CanStreamedLevelBeLoaded(nextSceneBuildIndex))
            {
                reason = $"Build Index {nextSceneBuildIndex} ไม่ถูกต้อง/ไม่ได้ใส่ใน Build Settings";
                return false;
            }
            reason = null; return true;
        }
        reason = "กรุณากรอก nextSceneName หรือ nextSceneBuildIndex อย่างใดอย่างหนึ่ง";
        return false;
    }
}
