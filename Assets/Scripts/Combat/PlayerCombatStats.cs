using System;
using UnityEngine;

public enum MacuahuitlForm { Base, Empowered, Ascended }

/// <summary>
/// Tlahuelli's combat stats: stamina (the "peanut bar"), Macuahuitl charge, unlocked skills and potions.
/// Every number here is a STARTING VALUE TO TUNE (the GDD does not define them yet).
/// </summary>
[RequireComponent(typeof(Health))]
public class PlayerCombatStats : MonoBehaviour
{
    [Header("Stamina (the \"peanut bar\")")]
    public float maxStamina = 45f;      // grows as enemies are defeated (see EnemyData.bonusMaxStamina)
    public float stamina = 45f;
    [Tooltip("Fraction of max stamina recovered at the start of each turn (1 = full refill).")]
    [Range(0f, 1f)] public float staminaRegenFraction = 0.6f;

    [Header("Stamina costs")]
    public float attackCost = 20f;
    public float furyCost = 30f;
    public float seismicCost = 40f;
    public float potionCost = 15f;

    [Header("Damage")]
    public float baseDamage = 14f;
    [Tooltip("Damage bonus with the Macuahuitl at 100% charge (1 = +100%).")]
    public float chargeDamageBonus = 1f;

    [Header("Macuahuitl (charged by defeated enemies' souls)")]
    public float maxCharge = 100f;
    public float charge = 0f;
    public float empoweredThreshold = 35f;
    public float ascendedThreshold = 100f;

    [Header("Skills (GDD section 12)")]
    public bool furyUnlocked = false;
    public bool seismicUnlocked = false;
    public float furyHealFraction = 0.4f;
    public float furyDamageMultiplier = 1.5f;
    public int furyTurns = 3;
    [Range(0.05f, 0.5f)] public float criticalHealthFraction = 0.25f;

    [Header("Potions")]
    public int potions = 3;
    [Range(0.1f, 1f)] public float potionHealFraction = 0.35f;
    [Tooltip("Potions can never go above this.")]
    public int maxPotions = 5;
    [Tooltip("Potions gained after each victory (there is no merchant yet, so fights refill you a little).")]
    public int potionsOnVictory = 1;

    [Header("Respawn (simple checkpoint, the GDD has not defined the system yet)")]
    public Vector3 checkpoint;

    public bool FuryUsedThisBattle { get; private set; }
    public int FuryTurnsLeft { get; private set; }
    public Health Health { get; private set; }
    public event Action<MacuahuitlForm> OnFormChanged;

    MacuahuitlForm lastForm;

    public float ChargeNormalized => maxCharge > 0f ? Mathf.Clamp01(charge / maxCharge) : 0f;
    public MacuahuitlForm Form =>
        charge >= ascendedThreshold ? MacuahuitlForm.Ascended :
        charge >= empoweredThreshold ? MacuahuitlForm.Empowered : MacuahuitlForm.Base;

    public float DamageMultiplier =>
        (1f + ChargeNormalized * chargeDamageBonus) * (FuryTurnsLeft > 0 ? furyDamageMultiplier : 1f);

    public float MinActionCost => potions > 0 ? Mathf.Min(attackCost, potionCost) : attackCost;

    void Awake()
    {
        Health = GetComponent<Health>();
        stamina = maxStamina;
        checkpoint = transform.position;
        lastForm = Form;
    }

    public bool CanAfford(float cost) => stamina >= cost;

    public bool TrySpend(float cost)
    {
        if (stamina < cost) return false;
        stamina -= cost;
        return true;
    }

    public void AddStamina(float v) => stamina = Mathf.Clamp(stamina + v, 0f, maxStamina);

    public void AddCharge(float v)
    {
        charge = Mathf.Clamp(charge + v, 0f, maxCharge);
        var f = Form;
        if (f != lastForm)
        {
            lastForm = f;
            OnFormChanged?.Invoke(f);
        }
    }

    public void SetCheckpoint(Vector3 pos) => checkpoint = pos;

    /// <summary>Permanent growth (called after a victory with the sum of the defeated enemies' bonuses).</summary>
    public void Grow(float healthBonus, float staminaBonus)
    {
        if (healthBonus > 0f) Health.SetMax(Health.maxHealth + healthBonus);
        if (staminaBonus > 0f) { maxStamina += staminaBonus; stamina = maxStamina; }
    }
    public void UnlockSeismic() => seismicUnlocked = true;   // call after surviving the Valley of Tlazolteotl
    public void UnlockFury() => furyUnlocked = true;

    public void BeginBattle()
    {
        FuryUsedThisBattle = false;
        FuryTurnsLeft = 0;
        stamina = maxStamina;
    }

    public void BeginTurn()
    {
        stamina = Mathf.Min(maxStamina, stamina + maxStamina * staminaRegenFraction);
    }

    public void EndPlayerTurn()
    {
        if (FuryTurnsLeft > 0) FuryTurnsLeft--;
    }

    public void ActivateFury()
    {
        FuryUsedThisBattle = true;
        FuryTurnsLeft = furyTurns;
        Health.Heal(Health.maxHealth * furyHealFraction);
        stamina = maxStamina;
    }

    /// <summary>Called after a victory: a potion drops, up to maxPotions.</summary>
    public void GrantVictoryPotions()
    {
        potions = Mathf.Min(maxPotions, potions + potionsOnVictory);
    }

    public void FullRestore()
    {
        Health.HealFull();
        stamina = maxStamina;
        FuryTurnsLeft = 0;
    }

    [ContextMenu("DEBUG: unlock Fury and Seismic Jump")]
    void DebugUnlockAll()
    {
        furyUnlocked = true;
        seismicUnlocked = true;
    }

    [ContextMenu("DEBUG: Macuahuitl to 100%")]
    void DebugFullCharge() => AddCharge(maxCharge);
}
