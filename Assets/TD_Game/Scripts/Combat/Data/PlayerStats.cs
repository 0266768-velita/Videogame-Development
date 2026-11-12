using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// The player's base combat values. Starting values come from Mictlán v5 and are meant to be tuned.
    /// Ability costs live in each ability asset; permanent growth lives in PlayerProgression.
    /// Create with: right click > Create > TD_Game > Player Stats.
    /// </summary>
    [CreateAssetMenu(menuName = "TD_Game/Player Stats", fileName = "PlayerStats")]
    public class PlayerStats : ScriptableObject
    {
        [Header("Health")]
        [Min(1f)] public float maxHealth = 100f;
        [Tooltip("Below this fraction of health the player is in critical state (Last Stand).")]
        [Range(0.05f, 0.5f)] public float criticalHealthFraction = 0.25f;

        [Header("Stamina")]
        [Min(1f)] public float maxStamina = 45f;
        [Tooltip("Fraction of max stamina recovered at the start of each player turn (1 = full).")]
        [Range(0f, 1f)] public float staminaRegenFraction = 0.6f;
        [Tooltip("Stamina gained with a PERFECT dodge or interrupt.")]
        [Min(0f)] public float perfectCounterStaminaBonus = 10f;

        [Header("Damage")]
        [Min(0f)] public float baseDamage = 14f;

        [Header("Macuahuitl (charged with the souls of defeated enemies)")]
        [Min(1f)] public float maxCharge = 100f;
        [Tooltip("Extra damage at 100% charge (1 = +100%).")]
        [Min(0f)] public float chargeDamageBonus = 1f;

        [Header("Potions")]
        [Min(0)] public int startingPotions = 3;
        [Min(0)] public int maxPotions = 5;
        [Min(0)] public int potionsOnVictory = 1;
        [Tooltip("A retry after a defeat never starts with fewer potions than this.")]
        [Min(0)] public int minPotionsOnRetry = 2;

        [Header("Body (tiles)")]
        [Tooltip("Approximate body radius, used to stop lunges in front of the target.")]
        [Min(0.1f)] public float bodyRadius = 0.4f;
    }
}
