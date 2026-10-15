using UnityEngine;

public enum EnemyArchetype { Giant, Witch, Hound, Boss, Other }

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

    [Header("Progression (rewards for defeating this enemy)")]
    [Tooltip("Permanent increase of Tlahuelli's max health when this enemy dies.")]
    public float bonusMaxHealth = 0f;
    [Tooltip("Permanent increase of Tlahuelli's max stamina when this enemy dies.")]
    public float bonusMaxStamina = 0f;
    [Tooltip("Defeating this enemy unlocks Seismic Jump.")]
    public bool unlocksSeismic = false;
    [Tooltip("Defeating this enemy unlocks Mictlan Fury (it can also unlock earlier through the Last Stand).")]
    public bool unlocksFury = false;

    [Header("Boss")]
    public bool isBoss = false;
    [Range(0.1f, 0.9f)] public float phase2HealthFraction = 0.5f;
    [Tooltip("Attacks used once the boss is below phase2HealthFraction. Empty = same attacks.")]
    public EnemyAttackData[] phase2Attacks;
    [Min(1)] public int phase2AttacksPerTurn = 2;

    [Header("Look (sprite sheet in Resources/Sprites)")]
    public string spriteSheet = "";
    public int frameWidth = 24;
    public int frameHeight = 24;
    [Tooltip("Distance from the pivot (collider center) down to the feet, in tiles.")]
    public float feetOffset = 0.4f;
    [Tooltip("Cut the sprite into cols x rows pieces when it dies (gore).")]
    public int gorePieceCols = 2;
    public int gorePieceRows = 2;
    [Tooltip("Scales the amount of blood and gibs.")]
    public float goreAmount = 1f;

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
