// FootstepLoopSwap.cs
// Unity 6 — Single AudioSource that swaps between WALK and SPRINT loops (no overlap)

using UnityEngine;
using StarterAssets; // for StarterAssetsInputs (sprint flag)

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class FootstepSFX : MonoBehaviour
{
    [Header("Loop Clips")]
    [SerializeField] AudioClip walkLoop;
    [SerializeField] AudioClip sprintLoop;

    [Header("Playback")]
    [SerializeField, Range(0f, 1f)] float maxVolume = 0.7f;
    [SerializeField, Min(0f)] float fadeInSpeed  = 8f;     // volume units/sec
    [SerializeField, Min(0f)] float fadeOutSpeed = 10f;    // volume units/sec
    [SerializeField, Min(0f)] float minMoveSpeed = 0.15f;  // ignore tiny jitters
    [SerializeField] float walkPitch   = 1.00f;
    [SerializeField] float sprintPitch = 1.00f;

    [Header("Fallback (if Inputs missing)")]
    [SerializeField] float fallbackSprintSpeedThreshold = 5f;

    CharacterController cc;
    StarterAssetsInputs inputs; // provides .sprint
    AudioSource src;

    void Awake()
    {
        cc     = GetComponent<CharacterController>();
        inputs = GetComponent<StarterAssetsInputs>();
        src    = GetComponent<AudioSource>();

        src.playOnAwake  = false;
        src.loop         = true;
        src.spatialBlend = 1f;
        src.dopplerLevel = 0f;
        src.minDistance  = 1.2f;
        src.maxDistance  = 16f;
        src.volume       = 0f;
    }

    void Update()
    {
        // Are we grounded and actually moving horizontally?
        Vector3 hv = cc.velocity; hv.y = 0f;
        float speed = hv.magnitude;
        bool moving = cc.isGrounded && speed > minMoveSpeed;

        // Sprint state: prefer your input flag; otherwise fall back to speed threshold
        bool sprinting = inputs ? inputs.sprint : speed > fallbackSprintSpeedThreshold;

        // Decide desired clip & pitch
        AudioClip desired = sprinting ? sprintLoop : walkLoop;
        float    targetVolume = (moving && desired) ? maxVolume : 0f;
        float    targetPitch  = sprinting ? sprintPitch : walkPitch;

        // Swap clip ONLY when needed (ensures no overlap)
        if (moving && desired)
        {
            if (src.clip != desired)
            {
                // Hard stop the old clip to guarantee no double-play
                if (src.isPlaying) src.Stop();
                src.clip  = desired;
                src.pitch = targetPitch;
                src.Play();
                // Start quiet; fade up this frame
                if (src.volume > targetVolume) src.volume = targetVolume;
            }
            else
            {
                // Keep pitch in sync if sprint state changed
                src.pitch = targetPitch;
                if (!src.isPlaying) src.Play();
            }
        }

        // Smooth volume each frame
        float fadeSpeed = (targetVolume > src.volume) ? fadeInSpeed : fadeOutSpeed;
        src.volume = Mathf.MoveTowards(src.volume, targetVolume, fadeSpeed * Time.deltaTime);

        // If fully faded out or we’re not moving, stop to save CPU
        if (src.volume <= 0.001f || !moving || !desired)
        {
            if (src.isPlaying) src.Stop();
            // Keep the last chosen clip; it will swap next time if needed
        }
    }

    void OnDisable()
    {
        if (src) src.Stop();
    }
}
