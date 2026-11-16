using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Data for one enemy type (exploration, combat and rewards), adapted from Mictlán v5.
    /// Create with: right click > Create > TD_Game > Enemy Data.
    /// </summary>
    [CreateAssetMenu(menuName = "TD_Game/Enemy Data", fileName = "EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Enemy";
        [Tooltip("The final boss: its defeat rolls the credits.")]
        public bool isBoss = false;

        [Header("Exploration (used by EnemyAI; distances in tiles)")]
        [Min(0f)] public float patrolSpeed = 1.5f;
        [Min(0f)] public float chaseSpeed = 3f;
        [Tooltip("Sees the player this close (if no wall is in between).")]
        [Min(0f)] public float detectRadius = 5f;
        [Tooltip("Stops chasing when the player is this far.")]
        [Min(0f)] public float loseRadius = 8f;
        [Tooltip("Never goes farther than this from its home position; it returns home instead.")]
        [Min(0f)] public float leashRadius = 10f;
        [Tooltip("Random patrol around home within this radius (when it has no patrol points).")]
        [Min(0f)] public float patrolRadius = 2.5f;
        [Tooltip("Seconds it waits at each patrol point.")]
        [Min(0f)] public float patrolWait = 1.5f;
        [Tooltip("When it sees the player, allies this close also start chasing (wolf howl). 0 = off.")]
        [Min(0f)] public float alertAlliesRadius = 0f;

        [Header("Combat")]
        [Min(1f)] public float maxHealth = 60f;
        [Min(0f)] public float attackPower = 12f;
        [Tooltip("Passive: multiplies all the damage it receives (0.8 = thick skin, takes 20% less).")]
        [Min(0.1f)] public float damageTakenMultiplier = 1f;
        [Tooltip("The enemies act before the player in the first round.")]
        public bool startsBattleFirst = false;
        [Tooltip("Body radius in tiles; lunges stop in front of it.")]
        [Min(0.1f)] public float bodyRadius = 0.45f;
        public EnemyAttack[] attacks = { new EnemyAttack() };

        [Header("Second phase (boss). Empty attacks = no second phase")]
        [Tooltip("Enters the second phase when its health falls to this fraction.")]
        [Range(0.05f, 0.95f)] public float phaseTwoAtHealth = 0.5f;
        public string phaseTwoName = "";
        [Tooltip("Attack power multiplier in the second phase.")]
        [Min(0.1f)] public float phaseTwoPowerMultiplier = 1.25f;
        public EnemyAttack[] phaseTwoAttacks;

        public bool HasPhaseTwo => phaseTwoAttacks != null && phaseTwoAttacks.Length > 0;

        [Header("Orbs dropped on death (collected by walking over them after the battle)")]
        [Tooltip("Total Macuahuitl charge in gold soul orbs.")]
        [Min(0f)] public float soulsOnDefeat = 12f;
        [Tooltip("Total health in green jade orbs.")]
        [Min(0f)] public float healthOnDefeat = 10f;
        [Tooltip("Total Tonalli (concentration) in blue orbs.")]
        [Min(0f)] public float concentrationOnDefeat = 20f;

        [Header("Rewards")]
        [Tooltip("Permanent increase of the player's max health.")]
        [Min(0f)] public float bonusMaxHealth = 0f;
        [Tooltip("Permanent increase of the player's max stamina.")]
        [Min(0f)] public float bonusMaxStamina = 0f;
        [Tooltip("Abilities unlocked when the player wins a battle against this enemy.")]
        public CombatAbility[] unlocksOnDefeat;

        [Header("Last Stand")]
        [Tooltip("If set, the first time this enemy would kill the player, the player survives with 1 HP and this ability is unlocked and used.")]
        public CombatAbility lastStandAbility;

        /// <summary>Picks an attack using the weights (from the second-phase list if phaseTwo is true).</summary>
        public EnemyAttack PickAttack(bool phaseTwo = false)
        {
            var attacks = phaseTwo && HasPhaseTwo ? phaseTwoAttacks : this.attacks;
            if (attacks == null || attacks.Length == 0) return new EnemyAttack();
            float total = 0f;
            foreach (var a in attacks) total += a.weight;
            float roll = Random.value * total;
            foreach (var a in attacks)
            {
                roll -= a.weight;
                if (roll <= 0f) return a;
            }
            return attacks[attacks.Length - 1];
        }
    }
}
