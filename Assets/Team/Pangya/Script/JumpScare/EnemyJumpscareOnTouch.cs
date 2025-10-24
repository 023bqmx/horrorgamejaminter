// EnemyJumpscareOnTouch.cs
using UnityEngine;

namespace Horror
{
    [RequireComponent(typeof(Collider))]
    public class EnemyJumpscareOnTouch : MonoBehaviour
    {
        public JumpscareSettings settings;
        [TagSelector] public string playerTag = "Player";
        [Tooltip("If your enemy's collider is 'Is Trigger', keep this ON.")]
        public bool useTrigger = true;

        [Header("SFX Position (optional)")]
        public Transform sfxAt;

        bool _fired;

        void OnTriggerEnter(Collider other)
        {
            if (!useTrigger) return;
            TryFire(other.gameObject);
        }

        void OnCollisionEnter(Collision c)
        {
            if (useTrigger) return;
            TryFire(c.gameObject);
        }

        void TryFire(GameObject other)
        {
            if (settings == null) return;
            if (_fired) return;
            if (!other.CompareTag(playerTag)) return;

            var controller = FindAnyObjectByType<JumpscareController>();
            if (controller == null) return;

            controller.Trigger(settings, sfxAt != null ? sfxAt : transform);
            _fired = true;
        }
    }
}
