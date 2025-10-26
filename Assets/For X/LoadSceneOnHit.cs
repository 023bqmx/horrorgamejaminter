using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[DisallowMultipleComponent]
public class LoadSceneOnHit : MonoBehaviour
{
    [Header("Next Scene")]
    public string nextSceneName = "";     // ใส่ชื่อซีน หรือ…
    public int nextSceneBuildIndex = -1;  // …ใส่ Build Index (อย่างใดอย่างหนึ่ง)

    [Header("Behavior")]
    public float delayBeforeLoad = 0f;           // 0 = โหลดทันที
    public LoadSceneMode loadMode = LoadSceneMode.Single;
    public bool useAsync = false;                // true = LoadSceneAsync

    [Header("Filters (optional)")]
    public bool requireTag = true;
    public string requiredTag = "Player";        // ให้ชนเฉพาะแท็กนี้
    public bool filterByLayer = false;
    public LayerMask allowedLayers = ~0;         // เลเยอร์ที่อนุญาต

    [Header("Trigger / Collision")]
    public bool reactToTrigger = true;           // ฟัง OnTriggerEnter
    public bool reactToCollision = true;         // ฟัง OnCollisionEnter
    public bool oneShot = true;                  // กันยิงซ้ำ

    [Header("Optional Fade")]
    public CanvasGroup blackFade;                // ถ้ามี จะเฟดเป็นดำก่อนโหลด
    public float fadeDuration = 0.5f;
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    bool _fired = false;

    void Awake()
    {
        if (blackFade)
        {
            blackFade.gameObject.SetActive(true);
            blackFade.blocksRaycasts = false;
            if (blackFade.alpha < 0f || blackFade.alpha > 1f) blackFade.alpha = 0f;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!reactToTrigger) return;
        TryFire(other.gameObject, "TriggerEnter");
    }

    void OnCollisionEnter(Collision col)
    {
        if (!reactToCollision) return;
        TryFire(col.collider.gameObject, "CollisionEnter");
    }

    void TryFire(GameObject hitter, string from)
    {
        if (_fired && oneShot) return;
        if (requireTag && !hitter.CompareTag(requiredTag)) return;
        if (filterByLayer && (allowedLayers.value & (1 << hitter.layer)) == 0) return;

        _fired = true;
        StartCoroutine(FadeAndLoadRoutine());
    }

    IEnumerator FadeAndLoadRoutine()
    {
        // เฟดเป็นดำก่อน (ถ้ามี)
        if (blackFade && fadeDuration > 0f)
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
        }

        if (delayBeforeLoad > 0f) yield return new WaitForSeconds(delayBeforeLoad);

        // โหลดซีน
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            if (useAsync)
                yield return SceneManager.LoadSceneAsync(nextSceneName, loadMode);
            else
                SceneManager.LoadScene(nextSceneName, loadMode);
        }
        else if (nextSceneBuildIndex >= 0)
        {
            if (useAsync)
                yield return SceneManager.LoadSceneAsync(nextSceneBuildIndex, loadMode);
            else
                SceneManager.LoadScene(nextSceneBuildIndex, loadMode);
        }
        else
        {
            Debug.LogError("[LoadSceneOnHit] กรุณาตั้ง nextSceneName หรือ nextSceneBuildIndex");
        }
    }
}
