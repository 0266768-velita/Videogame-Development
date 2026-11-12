using UnityEngine;

namespace TD_Game
{
    /// <summary>How the player can counter an enemy attack.</summary>
    public enum CounterType
    {
        Dodge,       // Combat/Dodge
        Interrupt,   // Combat/Interrupt (meant for spells)
        Unavoidable
    }

    /// <summary>Extra effect of an attack. This is how each enemy gets its unique abilities as data.</summary>
    public enum AttackEffect
    {
        None,
        DrainStamina,   // removes effectAmount stamina from the player
        HealSelf,       // the enemy heals effectAmount (fraction of its max health)
        Weaken          // the player's damage is multiplied by effectAmount during their next turn
    }

    /// <summary>One enemy attack (adapted from Mictlán v5's EnemyAttackData).</summary>
    [System.Serializable]
    public class EnemyAttack
    {
        public string attackName = "Attack";
        [Tooltip("Damage per hit = enemy attackPower x this.")]
        [Min(0f)] public float damageMultiplier = 1f;
        [Min(1)] public int hits = 1;
        [Tooltip("Seconds of red warning before the hit lands.")]
        [Min(0.3f)] public float windUpTime = 1.2f;
        [Tooltip("Seconds before the impact during which the counter works.")]
        [Min(0.1f)] public float counterWindow = 0.4f;
        [Min(0f)] public float timeBetweenHits = 0.6f;
        public CounterType counter = CounterType.Dodge;
        [Tooltip("true = the enemy lunges at the player (melee). false = ranged attack or spell.")]
        public bool lungeAtTarget = true;
        [Tooltip("Relative chance of picking this attack.")]
        [Min(0.1f)] public float weight = 1f;

        [Header("Unique effect")]
        public AttackEffect effect = AttackEffect.None;
        [Tooltip("DrainStamina: stamina points. HealSelf: fraction of max health. Weaken: damage multiplier.")]
        public float effectAmount = 0f;
        [Tooltip("Off = the effect only applies when the hit lands. On = it applies even if countered.")]
        public bool effectIgnoresCounter = false;
    }
}
