using System;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Drops orbs where an enemy dies, using the amounts in its EnemyData.
    /// Each orb type has a fixed value per orb, so stronger enemies simply drop more orbs.
    /// </summary>
    public class OrbSpawner : MonoBehaviour
    {
        /// <summary>Raised when the player picks up an orb (UI, sounds).</summary>
        public static event Action<SoulOrb, PlayerCombatant> Collected;

        [SerializeField] SoulOrb orbPrefab;

        [Header("Value of one orb")]
        [SerializeField, Min(1f)] float soulPerOrb = 4f;
        [SerializeField, Min(1f)] float healthPerOrb = 5f;
        [SerializeField, Min(1f)] float concentrationPerOrb = 5f;
        [SerializeField, Range(1, 20)] int maxOrbsPerType = 8;

        [Header("Look")]
        [SerializeField] Color soulColor = MexicaPalette.AgedGold;
        [SerializeField] Color jadeColor = MexicaPalette.Jade;
        [SerializeField] Color tonalliColor = MexicaPalette.Turquoise;
        [Tooltip("Orbs land in a ring around the enemy (tiles).")]
        [SerializeField] Vector2 scatterRadius = new Vector2(0.4f, 1.1f);

        void OnEnable() => CombatEnemy.Defeated += Drop;
        void OnDisable() => CombatEnemy.Defeated -= Drop;

        public static void NotifyCollected(SoulOrb orb, PlayerCombatant player) => Collected?.Invoke(orb, player);

        void Drop(CombatEnemy enemy)
        {
            if (orbPrefab == null || enemy.Data == null) return;
            Vector3 origin = enemy.transform.position;
            Spawn(OrbType.Soul, enemy.Data.soulsOnDefeat, soulPerOrb, soulColor, origin);
            Spawn(OrbType.Jade, enemy.Data.healthOnDefeat, healthPerOrb, jadeColor, origin);
            Spawn(OrbType.Tonalli, enemy.Data.concentrationOnDefeat, concentrationPerOrb, tonalliColor, origin);
        }

        void Spawn(OrbType type, float total, float perOrb, Color color, Vector3 origin)
        {
            if (total <= 0f) return;
            int count = Mathf.Clamp(Mathf.CeilToInt(total / perOrb), 1, maxOrbsPerType);
            float value = total / count;   // the total is always exact, whatever the count
            for (int i = 0; i < count; i++)
            {
                Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
                float distance = UnityEngine.Random.Range(scatterRadius.x, scatterRadius.y);
                var orb = Instantiate(orbPrefab, origin, Quaternion.identity);
                orb.Launch(type, value, color, origin, origin + (Vector3)(dir * distance));
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Collected = null;
    }
}
