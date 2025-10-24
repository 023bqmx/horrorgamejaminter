// JumpscareZone.cs
using UnityEngine;

namespace Horror
{
    [RequireComponent(typeof(Collider))]
    public class JumpscareZone : MonoBehaviour
    {
        [Header("Setup")]
        public JumpscareSettings settings;
        [TagSelector] public string playerTag = "Player";
        public bool oneShot = true;

        [Header("SFX Position (optional)")]
        public Transform sfxAt;

        bool _fired;

        void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (settings == null) return;
            if (oneShot && _fired) return;
            if (!other.CompareTag(playerTag)) return;

            // Need a controller in the scene
            var controller = FindAnyObjectByType<JumpscareController>();
            if (controller == null) return;

            controller.Trigger(settings, sfxAt != null ? sfxAt : transform);
            _fired = true;
        }
    }

    // Small attribute for nicer tag field in inspector (optional).
    public class TagSelectorAttribute : PropertyAttribute { }
}
