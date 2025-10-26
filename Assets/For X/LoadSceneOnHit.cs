using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[DisallowMultipleComponent]
public class LoadSceneOnHit : MonoBehaviour
{
    [Header("Next Scene")]
    public string nextSceneName = "";
    public int nextSceneBuildIndex = -1;

    [Header("Behavior")]
    public float delayBeforeLoad = 0f;
    public LoadSceneMode loadMode = LoadSceneMode.Single;
    public bool useAsync = false;
    [Tooltip("ใช้เวลาที่ไม่ขึ้นกับ Time.timeScale (เหมาะกับเมนู pause)")]
    public bool useUnscaledTime = true;

    [Header("Filters (optional)")]
    public bool requireTag = true;
    public string requiredTag = "Player";
    public bool filterByLayer = false;
    public LayerMask allowedLayers = ~0;

    [Header("Trigger / Collision")]
    public bool reactToTrigger = true;
    public bool reactToCollision = true;
    public bool oneShot = true;

    [Header("Optional Fade")]
    public CanvasGroup blackFade;
    public float fadeDuration = 0.5f;
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Debug")]
    public bool debugLogs = true;         // เปิดไว้ก่อนเพื่อดูสาเหตุ
    public KeyCode debugHotkey = KeyCode.None; // กดคีย์นี้เพื่อโหลดทดสอบ (ไม่ผ่านชน)

    bool _fired = false;

    void Awake()
    {
        if (blackFade)
        {
            blackFade.gameObject.SetActive(true);
            blackFade.blocksRaycasts = false;
            blackFade.alpha = Mathf.Clamp01(blackFade.alpha); // กันค่าหลุด
        }

        if (debugLogs)
        {
            Debug.Log($"[LoadSceneOnHit] Awake on '{name}'. Next=('{nextSceneName}', idx={nextSceneBuildIndex}) " +
                      $"reactToTrigger={reactToTrigger}, reactToCollision={reactToCollision}, oneShot={oneShot}");
        }
    }

    void Update()
    {
        if (debugHotkey != KeyCode.None && Input.GetKeyDown(debugHotkey))
        {
            if (debugLogs) Debug.Log($"[LoadSceneOnHit] DEBUG hotkey pressed -> BeginLoad()");
            BeginLoad();
        }
    }

    // ----------- Trigger / Collision -----------
    void OnTriggerEnter(Collider other)
    {
        if (!reactToTrigger) return;
        if (debugLogs) Debug.Log($"[LoadSceneOnHit] OnTriggerEnter by '{other.name}' (tag={other.tag}, layer={other.gameObject.layer})");
        TryFire(other.gameObject);
    }

    void OnCollisionEnter(Collision col)
    {
        if (!reactToCollision) return;
        if (debugLogs) Debug.Log($"[LoadSceneOnHit] OnCollisionEnter by '{col.collider.name}' (tag={col.collider.tag}, layer={col.collider.gameObject.layer})");
        TryFire(col.collider.gameObject);
    }

    void TryFire(GameObject hitter)
    {
        if (_fired && oneShot) { if (debugLogs) Debug.Log("[LoadSceneOnHit] Blocked: oneShot already fired."); return; }

        // Tag filter
        if (requireTag && !hitter.CompareTag(requiredTag))
        {
            if (debugLogs) Debug.Log($"[LoadSceneOnHit] Blocked: requireTag=true but hitter.tag='{hitter.tag}' != '{requiredTag}'");
            return;
        }

        // Layer filter
        if (filterByLayer && (allowedLayers.value & (1 << hitter.layer)) == 0)
        {
            if (debugLogs) Debug.Log($"[LoadSceneOnHit] Blocked: layer {hitter.layer} not in allowedLayers {allowedLayers.value}");
            return;
        }

        BeginLoad();
    }

    void BeginLoad()
    {
        if (_fired && oneShot) return;
        _fired = true;

        // ตรวจซีนอยู่ใน Build ไหม (ชื่อหรือ index) — กันเคสสะกดผิด/ไม่ได้ Add
        if (!ValidateSceneSetting(out string msg))
        {
            Debug.LogError($"[LoadSceneOnHit] {msg}");
            _fired = false; // ยอมให้ยิงใหม่หลังแก้ค่า
            return;
        }

        StartCoroutine(FadeAndLoadRoutine());
    }

    IEnumerator FadeAndLoadRoutine()
    {
        // เฟดดำก่อน (ถ้ามี)
        if (blackFade && fadeDuration > 0f)
        {
            blackFade.blocksRaycasts = true;
            float t = 0f;
            float start = blackFade.alpha;
            while (t < fadeDuration)
            {
                t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
                float k = Mathf.Clamp01(t / fadeDuration);
                float e = fadeCurve != null ? fadeCurve.Evaluate(k) : k;
                blackFade.alpha = Mathf.LerpUnclamped(start, 1f, e);
                yield return null;
            }
            blackFade.alpha = 1f;
        }

        // หน่วงก่อนโหลด
        if (delayBeforeLoad > 0f)
        {
            if (useUnscaledTime) yield return new WaitForSecondsRealtime(delayBeforeLoad);
            else yield return new WaitForSeconds(delayBeforeLoad);
        }

        // โหลดซีนจริง
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            if (debugLogs) Debug.Log($"[LoadSceneOnHit] Loading scene by Name='{nextSceneName}' (mode={loadMode}, async={useAsync})");
            if (useAsync) yield return SceneManager.LoadSceneAsync(nextSceneName, loadMode);
            else SceneManager.LoadScene(nextSceneName, loadMode);
        }
        else // ใช้ index
        {
            if (debugLogs) Debug.Log($"[LoadSceneOnHit] Loading scene by Index={nextSceneBuildIndex} (mode={loadMode}, async={useAsync})");
            if (useAsync) yield return SceneManager.LoadSceneAsync(nextSceneBuildIndex, loadMode);
            else SceneManager.LoadScene(nextSceneBuildIndex, loadMode);
        }
    }

    // ----------- PUBLIC: ใช้กับปุ่ม UI -----------
    public void LoadScene() { BeginLoad(); }
    public void LoadSceneByName(string name) { nextSceneName = name; nextSceneBuildIndex = -1; BeginLoad(); }
    public void LoadSceneByIndex(int index) { nextSceneBuildIndex = index; nextSceneName = ""; BeginLoad(); }

    // ----------- Validate scene presence -----------
    bool ValidateSceneSetting(out string reason)
    {
        // ตรวจด้วย API runtime — ใช้ได้ทั้งใน Editor/Build
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                reason = $"Scene name '{nextSceneName}' is NOT in Build Settings or spelled wrong.";
                return false;
            }
            reason = null; return true;
        }
        if (nextSceneBuildIndex >= 0)
        {
            if (!Application.CanStreamedLevelBeLoaded(nextSceneBuildIndex))
            {
                reason = $"Scene build index {nextSceneBuildIndex} is NOT valid (not in Build Settings?).";
                return false;
            }
            reason = null; return true;
        }
        reason = "Please set nextSceneName or nextSceneBuildIndex.";
        return false;
    }
}
