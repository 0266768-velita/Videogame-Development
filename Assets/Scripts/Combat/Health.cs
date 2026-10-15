using System;
using UnityEngine;

/// <summary>Generic health. Used by Tlahuelli and by every enemy.</summary>
public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    [SerializeField] float current = -1f;

    public float Current => current;
    public float Normalized => maxHealth > 0f ? Mathf.Clamp01(current / maxHealth) : 0f;
    public bool IsDead => current <= 0f;

    public event Action<float> OnDamaged;   // damage amount
    public event Action OnHealed;
    public event Action OnDied;

    void Awake()
    {
        if (current < 0f) current = maxHealth;
    }

    /// <summary>Changes the maximum without touching the current value (level-up bonuses).</summary>
    public void SetMax(float max) { maxHealth = max; current = Mathf.Min(current < 0f ? max : current, max); }

    public void Initialize(float max)
    {
        maxHealth = max;
        current = max;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;
        current = Mathf.Max(0f, current - amount);
        OnDamaged?.Invoke(amount);
        if (current <= 0f) OnDied?.Invoke();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;
        current = Mathf.Min(maxHealth, current + amount);
        OnHealed?.Invoke();
    }

    public void HealFull()
    {
        current = maxHealth;
        OnHealed?.Invoke();
    }
}
