// JumpscareSettings.cs
// Create one or more assets via: Right-click Project → Create → Horror → Jumpscare Settings
using UnityEngine;

namespace Horror
{
    public enum JumpscareMode { UIOverlay, LoadScene }

    [CreateAssetMenu(menuName = "Horror/Jumpscare Settings", fileName = "JumpscareSettings")]
    public class JumpscareSettings : ScriptableObject
    {
        [Header("Mode")]
        public JumpscareMode mode = JumpscareMode.UIOverlay;

        [Tooltip("For LoadScene mode. Add scene to Build Settings first.")]
        public string sceneName;

        [Header("UI Overlay (if used)")]
        [Tooltip("Full-screen prefab (e.g., Canvas with Image/Animator).")]
        public GameObject uiPrefab;
        [Min(0f)] public float uiLifetime = 1.25f;

        [Header("Audio")]
        public AudioClip sfx;
        [Range(0f,1f)] public float sfxVolume = 1f;
        [Range(0f,1f)] public float sfxSpatialBlend = 0f;

        [Header("Player Flow")]
        [Tooltip("Lock&hide cursor during the jumpscare action.")]
        public bool lockAndHideCursor = true;

        [Tooltip("Temporarily freeze gameplay time (Time.timeScale=0) for a quick sting.")]
        public bool freezeTimeBriefly = true;
        [Min(0f)] public float freezeSeconds = 0.25f;

        [Header("Load Scene Timing")]
        [Tooltip("Optional delay before switching scene (real-time).")]
        [Min(0f)] public float delayBeforeSceneLoad = 0f;
    }
}
