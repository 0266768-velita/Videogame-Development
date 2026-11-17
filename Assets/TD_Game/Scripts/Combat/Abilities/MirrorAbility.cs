using System.Collections;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Smoking Mirror of Tezcatlipoca (artifact sold by the merchant, key 6).
    /// A dark flash hits EVERY enemy and Tlahuelli drinks part of that damage as health.
    /// </summary>
    [CreateAssetMenu(menuName = "TD_Game/Abilities/Smoking Mirror", fileName = "Ability_SmokingMirror")]
    public class MirrorAbility : CombatAbility
    {
        [Min(0f)] public float damageMultiplier = 1.2f;   // x the normal attack damage, to each enemy
        [Range(0f, 1f)] public float lifeSteal = 0.5f;    // part of the damage that heals Tlahuelli

        protected override IEnumerator Execute(CombatContext context)
        {
            var player = context.Player;
            CombatFeed.ShowBanner(displayName + "!", 1.2f);
            yield return CombatMotion.Flash(context.PlayerSprite, MexicaPalette.Obsidian, 0.3f);

            float total = 0f;
            foreach (var enemy in context.Enemies)
            {
                if (enemy == null || enemy.Health.IsDead) continue;
                float damage = player.AttackDamage(damageMultiplier);
                context.DamageEnemy(enemy, damage, critical: true, label: "MIRROR");
                total += damage;
                yield return new WaitForSeconds(0.15f);
            }

            float heal = Mathf.Round(total * lifeSteal);
            player.Health.Heal(heal);
            CombatFeed.ShowPopup(player.transform.position + Vector3.up, "+" + heal, MexicaPalette.Jade);
            yield return new WaitForSeconds(0.3f);
        }
    }
}
