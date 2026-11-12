using System.Collections;
using UnityEngine;

namespace TD_Game
{
    /// <summary>Seismic Jump: the player jumps and slams the ground, hitting every enemy.</summary>
    [CreateAssetMenu(menuName = "TD_Game/Abilities/Seismic Jump", fileName = "Ability_SeismicJump")]
    public class SeismicAbility : CombatAbility
    {
        [Min(0f)] public float damageMultiplier = 1.2f;
        [Tooltip("Jump height in tiles.")]
        [Min(0f)] public float jumpHeight = 1.2f;
        [Min(0.1f)] public float jumpTime = 0.45f;
        [Tooltip("Speed of the shockwave (tiles/s): farther enemies are hit later.")]
        [Min(1f)] public float waveSpeed = 12f;

        protected override IEnumerator Execute(CombatContext context)
        {
            CombatFeed.ShowBanner(displayName + "!", 1f);
            float damage = context.Player.AttackDamage(damageMultiplier);
            yield return CombatMotion.Hop(context.Player.transform, jumpHeight, jumpTime, null);

            Vector3 origin = context.Player.transform.position;
            float longestDelay = 0f;
            foreach (var enemy in context.Enemies)
            {
                if (enemy == null || enemy.Health.IsDead) continue;
                float delay = Vector2.Distance(origin, enemy.transform.position) / waveSpeed;
                longestDelay = Mathf.Max(longestDelay, delay);
                context.Run(HitAfter(context, enemy, damage, delay));
            }
            yield return new WaitForSeconds(longestDelay + 0.3f);
        }

        static IEnumerator HitAfter(CombatContext context, CombatEnemy enemy, float damage, float delay)
        {
            yield return new WaitForSeconds(delay);
            context.DamageEnemy(enemy, damage);
        }
    }
}
