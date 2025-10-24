// JumpscareController.cs (Unity 6)
// Drop this on a GameObject in your scene (e.g., _Game/JumpscareController)
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Horror
{
    [DisallowMultipleComponent]
    public class JumpscareController : MonoBehaviour
    {
        [Header("Optional")]
        [Tooltip("If left null, an AudioSource will be created at runtime.")]
        [SerializeField] AudioSource audioSource;

        bool _isRunning;

        void Awake()
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        public void Trigger(JumpscareSettings settings, Transform sfxAt = null)
        {
            if (settings == null || _isRunning) return;
            StartCoroutine(RunJumpscare(settings, sfxAt));
        }

        IEnumerator RunJumpscare(JumpscareSettings s, Transform sfxAt)
        {
            _isRunning = true;

            // lock cursor if requested (ใช้ล็อคเมาส์ซ่อนเคอร์เซอร์ระหว่าง Jumpscare)
            if (s.lockAndHideCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            // Play SFX (3D if sfxSpatialBlend > 0 using a temp AudioSource at position)
            if (s.sfx != null)
            {
                if (sfxAt != null && s.sfxSpatialBlend > 0f)
                {
                    AudioSource.PlayClipAtPoint(s.sfx, sfxAt.position, s.sfxVolume);
                }
                else
                {
                    audioSource.spatialBlend = 0f;
                    audioSource.volume = s.sfxVolume;
                    audioSource.clip = s.sfx;
                    audioSource.Play();
                }
            }

            // Optional brief freeze using unscaled time
            if (s.freezeTimeBriefly && s.freezeSeconds > 0f)
            {
                float pre = Time.timeScale;
                Time.timeScale = 0f;
                yield return new WaitForSecondsRealtime(s.freezeSeconds);
                Time.timeScale = pre;
            }

            switch (s.mode)
            {
                case JumpscareMode.UIOverlay:
                    yield return ShowUIOverlay(s);
                    break;

                case JumpscareMode.LoadScene:
                    if (s.delayBeforeSceneLoad > 0f)
                        yield return new WaitForSecondsRealtime(s.delayBeforeSceneLoad);

                    if (!string.IsNullOrEmpty(s.sceneName))
                        SceneManager.LoadScene(s.sceneName);
                    break;
            }

            // unlock cursor after (ถ้าต้องการปล่อยเคอร์เซอร์หลังจบ)
            if (s.lockAndHideCursor)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            _isRunning = false;
        }

        IEnumerator ShowUIOverlay(JumpscareSettings s)
        {
            if (s.uiPrefab == null)
                yield break;

            var spawned = Instantiate(s.uiPrefab);
            // ensure overlay cleans itself in real time
            var maybe = spawned.GetComponent<JumpscareUIOverlay>();
            if (maybe == null)
            {
                maybe = spawned.AddComponent<JumpscareUIOverlay>();
                maybe.life = s.uiLifetime;
            }
            else
            {
                if (maybe.life <= 0f) maybe.life = s.uiLifetime;
            }

            // Wait unscaled so overlay timing isn’t affected by slow-mo
            yield return new WaitForSecondsRealtime(maybe.life);
        }
    }
}
