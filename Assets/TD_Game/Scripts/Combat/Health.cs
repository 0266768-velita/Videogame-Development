using System;
using UnityEngine;

namespace TD_Game
{
    /// <summary>Generic health for the player and every enemy (adapted from Mictlán v5).</summary>
    public class Health : MonoBehaviour
    {
        [SerializeField, Min(1f)] float maxHealth = 100f;
        [SerializeField] float current = -1f;

        public float Max => maxHealth;
        public float Current => current;
        public float Normalized => maxHealth > 0f ? Mathf.Clamp01(current / maxHealth) : 0f;
        public bool IsDead => current <= 0f;

        /// <summary>Raised with the damage amount.</summary>
        public event Action<float> Damaged;
        /// <summary>Raised with the healed amount.</summary>
        public event Action<float> Healed;
        public event Action Died;

        void Awake()
        {
            if (current < 0f) current = maxHealth;
        }

        /// <summary>Sets a new maximum and fills the health completely.</summary>
        public void Initialize(float max)
        {
            maxHealth = Mathf.Max(1f, max);
            current = maxHealth;
        }

        /// <summary>Changes the maximum without healing (level-up bonuses).</summary>
        public void SetMax(float max)
        {
            maxHealth = Mathf.Max(1f, max);
            current = Mathf.Min(current < 0f ? maxHealth : current, maxHealth);
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f) return;
            current = Mathf.Max(0f, current - amount);
            Damaged?.Invoke(amount);
            if (IsDead) Died?.Invoke();
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            float before = current;
            current = Mathf.Min(maxHealth, current + amount);
            Healed?.Invoke(current - before);
        }

        /// <summary>Full health, also brings the owner back from death (retry, respawn).</summary>
        public void Restore()
        {
            float before = Mathf.Max(0f, current);
            current = maxHealth;
            Healed?.Invoke(current - before);
        }
    }
}
