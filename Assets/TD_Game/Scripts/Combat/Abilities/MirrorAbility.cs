using System.Collections;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Smoking Mirror of Tezcatlipoca (artifact sold by the merchant, key 6).
    /// Animation: Tlahuelli rises in black smoke, an obsidian mirror opens above him, dark rays fly to every
    /// enemy and cut them, and their stolen life flies back to him as green souls.
    /// </summary>
    [CreateAssetMenu(menuName = "TD_Game/Abilities/Smoking Mirror", fileName = "Ability_SmokingMirror")]
    public class MirrorAbility : CombatAbility
    {
        [Min(0f)] public float damageMultiplier = 1.2f;   // x the normal attack damage, to each enemy
        [Range(0f, 1f)] public float lifeSteal = 0.5f;    // part of the damage that heals Tlahuelli

        static readonly Color Smoke = new Color(0.12f, 0.08f, 0.16f);
        static readonly Color Violet = new Color(0.55f, 0.35f, 0.85f);

        protected override IEnumerator Execute(CombatContext context)
        {
            var player = context.Player;
            var fx = ActorFx.Of(player);
            var sprite = context.PlayerSprite;
            CombatFeed.ShowBanner(displayName + "!", 1.4f);

            // 1. Rises inside black smoke while the mirror opens over his head.
            Vector3 feet = player.transform.position;
            fx.Squash(new Vector2(0.85f, 1.2f));
            for (float t = 0f; t < 0.7f; t += Time.deltaTime)
            {
                fx.Height = Mathf.SmoothStep(0f, 0.8f, t / 0.7f);
                fx.Tremble = 0.02f;
                if (Random.value < 0.35f) Vfx.Wisps(feet, 2, Smoke, 1.8f, 0.6f);
                yield return null;
            }
            fx.Tremble = 0f;
            Vector3 mirror = feet + Vector3.up * 2.3f;
            Vfx.Ring(mirror, MexicaPalette.Obsidian, 0.1f, 0.9f, 0.35f);
            Vfx.Ring(mirror, Violet, 0.1f, 1.3f, 0.45f);
            Vfx.Burst(mirror, 14, Violet, new Vector2(2f, 5f), 0f, 1.2f, 0.5f, toGround: false, upward: 0f);
            context.Run(CombatMotion.Flash(sprite, Violet, 0.4f));
            CombatFeed.RequestShake(0.1f, 0.3f);
            yield return new WaitForSeconds(0.25f);

            // 2. A dark ray to every enemy: it cuts them and steals their life.
            float total = 0f;
            foreach (var enemy in context.Enemies)
            {
                if (enemy == null || enemy.Health.IsDead) continue;
                Vector3 body = enemy.transform.position + Vector3.up * 0.6f;
                yield return Vfx.Projectile(mirror, body, Violet, 0.18f);
                Vfx.Slash(body, -1f, Violet, 1.3f, 30f);
                Vfx.Ring(body, MexicaPalette.Obsidian, 0.2f, 1.4f, 0.3f);
                float damage = player.AttackDamage(damageMultiplier);
                context.DamageEnemy(enemy, damage, critical: true, label: "MIRROR");
                total += damage;
                context.Run(Vfx.Projectile(body, feet + Vector3.up * 0.7f, MexicaPalette.Jade, 0.45f));   // the stolen soul returns
            }

            // 3. Comes down and drinks the stolen life.
            yield return new WaitForSeconds(0.45f);
            float heal = Mathf.Round(total * lifeSteal);
            player.Health.Heal(heal);
            Vfx.Ring(feet + Vector3.up * 0.5f, MexicaPalette.Jade, 0.3f, 1.4f, 0.4f);
            Vfx.Wisps(feet, 6, MexicaPalette.Jade, 1.4f, 0.5f);
            CombatFeed.ShowPopup(player.transform.position + Vector3.up, "+" + heal, MexicaPalette.Jade);
            for (float t = 0f; t < 0.2f; t += Time.deltaTime)
            {
                fx.Height = 0.8f * (1f - t / 0.2f);
                yield return null;
            }
            fx.Height = 0f;
            fx.Squash(new Vector2(1.25f, 0.8f));
            Vfx.Dust(feet, 8, MexicaPalette.Ash);
            yield return new WaitForSeconds(0.2f);
        }
    }
}
