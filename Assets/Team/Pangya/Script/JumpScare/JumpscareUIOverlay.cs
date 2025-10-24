// JumpscareUIOverlay.cs
using System.Collections;
using UnityEngine;

namespace Horror
{
    public class JumpscareUIOverlay : MonoBehaviour
    {
        [Min(0f)] public float life = 1.25f;
        [Tooltip("Optional CanvasGroup fade out at the end.")]
        public bool fadeOut = true;
        [Min(0f)] public float fadeOutSeconds = 0.2f;

        void OnEnable() => StartCoroutine(LifeRoutine());

        IEnumerator LifeRoutine()
        {
            if (life > 0f) yield return new WaitForSecondsRealtime(life);

            if (fadeOut)
            {
                var cg = GetComponentInChildren<CanvasGroup>();
                if (cg != null)
                {
                    float t = 0f;
                    float dur = Mathf.Max(0.01f, fadeOutSeconds);
                    float start = cg.alpha;
                    while (t < dur)
                    {
                        t += Time.unscaledDeltaTime;
                        cg.alpha = Mathf.Lerp(start, 0f, t / dur);
                        yield return null;
                    }
                    cg.alpha = 0f;
                }
            }

            Destroy(gameObject);
        }
    }
}
