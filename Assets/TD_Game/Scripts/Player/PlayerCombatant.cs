using System.Collections.Generic;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// The player's combat state during and between battles: stamina, potions, Macuahuitl charge
    /// and temporary buffs. Base values come from PlayerStats; growth comes from PlayerProgression.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(PlayerProgression))]
    [RequireComponent(typeof(PlayerDirection))]
    public class PlayerCombatant : MonoBehaviour
    {
        [SerializeField] PlayerStats stats;

        readonly HashSet<CombatAbility> usedThisBattle = new HashSet<CombatAbility>();
        float buffMultiplier = 1f;
        float weakenMultiplier = 1f;

        public PlayerStats Stats => stats;
        public Health Health { get; private set; }
        public PlayerProgression Progression { get; private set; }
        public PlayerDirection Direction { get; private set; }
        public Rigidbody2D Body { get; private set; }

        public float Stamina { get; private set; }
        public float MaxStamina => stats.maxStamina + Progression.BonusMaxStamina;
        public int Potions { get; private set; }
        public float Charge { get; private set; }
        public float ChargeNormalized => Mathf.Clamp01(Charge / stats.maxCharge);
        public int BuffTurnsLeft { get; private set; }
        public bool IsWeakened => weakenMultiplier < 1f;
        public bool IsCritical => Health.Normalized <= stats.criticalHealthFraction;

        /// <summary>Tonalli: concentration that persists between battles and powers Tonalli Burst.</summary>
        public float Concentration { get; private set; }
        public float MaxConcentration => stats.maxConcentration;
        public bool IsConcentrationFull => Concentration >= stats.maxConcentration;

        /// <summary>Consecutive attacks landed this turn.</summary>
        public int ComboCount { get; private set; }
        public float ComboMultiplier => 1f + ComboCount * stats.comboBonusPerHit;

        public float DamageMultiplier =>
            (1f + ChargeNormalized * stats.chargeDamageBonus)
            * (BuffTurnsLeft > 0 ? buffMultiplier : 1f)
            * weakenMultiplier;

        void Awake()
        {
            Health = GetComponent<Health>();
            Progression = GetComponent<PlayerProgression>();
            Direction = GetComponent<PlayerDirection>();
            Body = GetComponent<Rigidbody2D>();

            if (stats == null)
            {
                Debug.LogError($"[{nameof(PlayerCombatant)}] PlayerStats is not assigned.", this);
                stats = ScriptableObject.CreateInstance<PlayerStats>();
            }
            Health.Initialize(stats.maxHealth);
            Potions = stats.startingPotions;
            Stamina = MaxStamina;
        }

        public float AttackDamage(float multiplier) => Mathf.Round(stats.baseDamage * DamageMultiplier * multiplier);

        // ---------------- Battle and turn lifecycle ----------------

        public void BeginBattle()
        {
            usedThisBattle.Clear();
            BuffTurnsLeft = 0;
            weakenMultiplier = 1f;
            ComboCount = 0;
            Stamina = MaxStamina;
            // Kinematic during combat so scripted lunges and dodges are not pushed by the physics.
            if (Body != null) { Body.linearVelocity = Vector2.zero; Body.bodyType = RigidbodyType2D.Kinematic; }
        }

        public void EndBattle()
        {
            BuffTurnsLeft = 0;
            weakenMultiplier = 1f;
            if (Body != null) Body.bodyType = RigidbodyType2D.Dynamic;
        }

        public void BeginTurn()
        {
            ComboCount = 0;
            AddStamina(MaxStamina * stats.staminaRegenFraction);
        }

        /// <summary>Counts a landed attack for the combo bonus.</summary>
        public void RegisterComboHit() => ComboCount++;

        public void AddConcentration(float amount) =>
            Concentration = Mathf.Clamp(Concentration + amount, 0f, stats.maxConcentration);

        public void ClearConcentration() => Concentration = 0f;

        public void EndTurn()
        {
            if (BuffTurnsLeft > 0) BuffTurnsLeft--;
            weakenMultiplier = 1f;   // a Weaken only lasts one player turn
        }

        // ---------------- Resources ----------------

        public bool CanAfford(float cost) => Stamina >= cost;

        public bool TrySpend(float cost)
        {
            if (Stamina < cost) return false;
            Stamina -= cost;
            return true;
        }

        public void AddStamina(float amount) => Stamina = Mathf.Clamp(Stamina + amount, 0f, MaxStamina);

        public void AddCharge(float amount) => Charge = Mathf.Clamp(Charge + amount, 0f, stats.maxCharge);

        public void AddPotions(int amount) => Potions = Mathf.Clamp(Potions + amount, 0, stats.maxPotions);

        public bool TryUsePotion()
        {
            if (Potions <= 0) return false;
            Potions--;
            return true;
        }

        public void ApplyDamageBuff(float multiplier, int turns)
        {
            buffMultiplier = multiplier;
            BuffTurnsLeft = turns;
        }

        public void Weaken(float multiplier) => weakenMultiplier = Mathf.Clamp01(multiplier);

        public void MarkUsed(CombatAbility ability) => usedThisBattle.Add(ability);
        public bool WasUsedThisBattle(CombatAbility ability) => usedThisBattle.Contains(ability);

        /// <summary>Applies progression growth to the maximum health.</summary>
        public void RefreshMaxHealth() => Health.SetMax(stats.maxHealth + Progression.BonusMaxHealth);

        public void FullRestore()
        {
            RefreshMaxHealth();
            Health.Restore();
            Stamina = MaxStamina;
            BuffTurnsLeft = 0;
            weakenMultiplier = 1f;
        }

        /// <summary>A retry after a defeat never starts without potions.</summary>
        public void EnsureRetryPotions() => Potions = Mathf.Max(Potions, stats.minPotionsOnRetry);

        public void Teleport(Vector3 position)
        {
            transform.position = position;
            if (Body != null) Body.position = position;
        }
    }
}
