// CrosshairHoldRing.cs
using UnityEngine;
using UnityEngine.UI;

public class CrosshairHoldRing : MonoBehaviour
{
    [SerializeField] Image ring;                 // Image ที่เป็นวงแหวน (Filled, Radial360)
    [SerializeField] CanvasGroup cg;             // ใส้ก็ได้ ไม่ใส่ก็ได้
    [SerializeField, Min(0.1f)] float showLerp = 12f;
    [SerializeField, Min(0.1f)] float hideLerp = 12f;

    float targetAlpha = 0f;

    void Reset()
    {
        if (!ring) ring = GetComponentInChildren<Image>(true);
        if (!cg) cg = GetComponentInChildren<CanvasGroup>(true);
    }

    void Awake()
    {
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        if (ring) ring.fillAmount = 0f;
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (!cg) return;
        float speed = (targetAlpha > cg.alpha) ? showLerp : hideLerp;
        cg.alpha = Mathf.MoveTowards(cg.alpha, targetAlpha, Time.unscaledDeltaTime * speed);
    }

    public void SetProgress(float t)            // t = 0..1
    {
        if (!ring) return;
        ring.fillAmount = Mathf.Clamp01(t);
        targetAlpha = (t > 0f) ? 1f : 0f;       // มีค่าเมื่อมี progress
    }

    public void ShowEmpty()
    {
        if (ring) ring.fillAmount = 0f;
        targetAlpha = 1f;
    }

    public void HideImmediate()
    {
        if (ring) ring.fillAmount = 0f;
        if (cg) cg.alpha = 0f;
        targetAlpha = 0f;
    }
}
