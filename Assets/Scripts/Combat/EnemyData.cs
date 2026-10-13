using UnityEngine;

public enum EnemyArchetype { Giant, Witch, Other }

/// <summary>How the player can counter an attack.</summary>
public enum CounterType
{
    Dodge,        // dodge (B / D)
    Interrupt,    // interrupt the spell (A / 1), meant for the Witches
    Unavoidable   // cannot be avoided
}

[System.Serializable]
public class EnemyAttackData
{
    public string attackName = "Attack";
    [Tooltip("Damage per hit = enemy attackPower x this multiplier.")]
    public float damageMultiplier = 1f;
    [Min(1)] public int hits = 1;
    [Tooltip("Seconds from the start of the animation until the hit lands.")]
    public float windUpTime = 1.2f;
    [Tooltip("Window (seconds before impact) in which the dodge/interrupt works.")]
    public float counterWindow = 0.4f;
    public float timeBetweenHits = 0.6f;
    public CounterType counter = CounterType.Dodge;
    [Tooltip("Animator Trigger name (optional).")]
    public string animatorTrigger = "";
    [Min(0.1f)] public float weight = 1f;
    [Tooltip("true = the enemy lunges at Tlahuelli (melee). false = ranged attack / spell.")]
    public bool lungeAtTarget = true;
}

/// <summary>
/// Data for one enemy type. Create one with: right click > Create > Mictlan > Enemy Data.
/// A single script (EnemyController) serves Giants, Witches, etc.: only this data changes.
/// All distances are in TILES (1 unit = 1 tile = 16 px).
/// </summary>
[CreateAssetMenu(menuName = "Mictlan/Enemy Data", fileName = "NewEnemy")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "Enemy";
    public EnemyArchetype archetype = EnemyArchetype.Giant;

    [Header("Turn-based combat")]
    public float maxHealth = 90f;
    public float attackPower = 20f;
    [Tooltip("Charge the Macuahuitl receives when this enemy dies.")]
    public float soulsOnDefeat = 12f;
    public bool startsBattleFirst = false;
    [Tooltip("GDD section 12: Mictlan Fury unlocks when health gets critical against the Quinametzin.")]
    public bool grantsFuryAtCriticalHealth = false;

    [Header("Body (tiles)")]
    [Tooltip("Approximate sprite radius; sets how far from the target lunges stop.")]
    public float bodyRadius = 0.5f;
    [Tooltip("Height above the pivot where damage numbers appear.")]
    public float popupHeight = 1.5f;

    [Header("Exploration AI (tiles)")]
    public float patrolRadius = 5f;
    public float patrolSpeed = 1.2f;
    public float chaseSpeed = 3.2f;
    public float detectionRadius = 8f;
    [Range(30f, 360f)] public float fieldOfView = 140f;
    public float hearingRadius = 3f;
    public float loseSightRadius = 12f;
    public float loseTargetAfterSeconds = 3f;
    [Tooltip("Distance at which combat starts.")]
    public float engageDistance = 2.6f;

    [Header("Attacks")]
    public EnemyAttackData[] attacks = { new EnemyAttackData() };
}
