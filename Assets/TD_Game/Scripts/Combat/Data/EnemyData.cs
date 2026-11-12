using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Data for one enemy type (combat and rewards), adapted from Mictlán v5.
    /// Exploration AI values (patrol, detection, chase) are added by the enemy AI task.
    /// Create with: right click > Create > TD_Game > Enemy Data.
    /// </summary>
    [CreateAssetMenu(menuName = "TD_Game/Enemy Data", fileName = "EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Enemy";

        [Header("Combat")]
        [Min(1f)] public float maxHealth = 60f;
        [Min(0f)] public float attackPower = 12f;
        [Tooltip("The enemies act before the player in the first round.")]
        public bool startsBattleFirst = false;
        [Tooltip("Body radius in tiles; lunges stop in front of it.")]
        [Min(0.1f)] public float bodyRadius = 0.45f;
        public EnemyAttack[] attacks = { new EnemyAttack() };

        [Header("Rewards")]
        [Tooltip("Macuahuitl charge given when this enemy dies.")]
        [Min(0f)] public float soulsOnDefeat = 12f;
        [Tooltip("Permanent increase of the player's max health.")]
        [Min(0f)] public float bonusMaxHealth = 0f;
        [Tooltip("Permanent increase of the player's max stamina.")]
        [Min(0f)] public float bonusMaxStamina = 0f;
        [Tooltip("Abilities unlocked when the player wins a battle against this enemy.")]
        public CombatAbility[] unlocksOnDefeat;

        [Header("Last Stand")]
        [Tooltip("If set, the first time this enemy would kill the player, the player survives with 1 HP and this ability is unlocked and used.")]
        public CombatAbility lastStandAbility;

        /// <summary>Picks an attack using the weights.</summary>
        public EnemyAttack PickAttack()
        {
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
