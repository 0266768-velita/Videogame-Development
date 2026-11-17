using System.Collections;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Tonalli Burst: the ultimate. It costs no stamina but needs the Tonalli (concentration) bar full,
    /// which fills with blue orbs, PERFECT counters and critical hits. The player rises and releases
    /// the stored soul energy on every enemy, then the bar empties.
    /// </summary>
    [CreateAssetMenu(menuName = "TD_Game/Abilities/Tonalli Burst", fileName = "Ability_TonalliBurst")]
    public class TonalliBurstAbility : CombatAbility
    {
        [Min(0f)] public float damageMultiplier = 2.5f;
        [Min(0f)] public float riseHeight = 1.5f;
        [Min(0.1f)] public float riseTime = 0.7f;
        public Color burstColor = MexicaPalette.Turquoise;

        public override string GetBlockReason(CombatContext context) =>
            !context.Player.IsConcentrationFull ? "Tonalli is not full yet" : base.GetBlockReason(context);

        protected override IEnumerator Execute(CombatContext context)
        {
            var player = context.Player;
            player.ClearConcentration();
            CombatFeed.ShowBanner(displayName + "!", 1.4f);
            context.Run(CombatMotion.Flash(context.PlayerSprite, burstColor, riseTime + 0.3f));
            CombatFeed.RequestShake(0.06f, riseTime);

            float damage = player.AttackDamage(damageMultiplier);
            var fx = ActorFx.Of(player);
            var sprite = context.PlayerSprite;
            Vector3 feet = player.transform.position;

            // Rises slowly while turquoise rings close in on him: the stored tonalli gathers.
            var weapon = MacuahuitlRig.Active(player);
            if (weapon != null) { context.Run(weapon.RaiseTo(0f, riseTime * 0.5f)); context.Run(weapon.Glow(burstColor, riseTime)); }
            fx.Squash(new Vector2(0.8f, 1.25f));
            float nextRing = 0f;
            for (float t = 0f; t < riseTime; t += Time.deltaTime)
            {
                float k = t / riseTime;
                fx.Height = riseHeight * Mathf.SmoothStep(0f, 1f, k);
                fx.Tremble = 0.03f * k;
                if (t >= nextRing)
                {
                    Vector3 body = sprite != null ? sprite.transform.position + Vector3.up * 0.4f : feet;
                    Vfx.Ring(body, burstColor, 1.8f, 0.2f, 0.25f);
                    Vfx.Wisps(body, 3, burstColor, -1.2f, 1.2f);
                    nextRing += 0.14f;
                }
                yield return null;
            }
            fx.Tremble = 0f;

            // Release: a turquoise explosion that reaches every enemy at once.
            Vector3 center = sprite != null ? sprite.transform.position + Vector3.up * 0.4f : feet;
            fx.Squash(new Vector2(1.4f, 0.7f));
            if (weapon != null) context.Run(weapon.Strike(new MacuahuitlTechnique("Tonalli", 0f, -200f, 0.12f, 0f, 0f), null));
            CombatFeed.RequestShake(0.3f, 0.55f);
            CombatFeed.RequestHitStop(0.15f);
            Vfx.Ring(center, burstColor, 0.3f, 4f, 0.5f);
            Vfx.Ring(center, MexicaPalette.Bone, 0.2f, 3f, 0.4f);
            Vfx.Burst(center, 24, burstColor, new Vector2(3f, 7f), 4f, 1.6f, 0.8f, toGround: false, upward: 0f);
            foreach (var enemy in context.Enemies)
            {
                if (enemy == null || enemy.Health.IsDead) continue;
                Vfx.Ring(enemy.transform.position + Vector3.up * 0.5f, burstColor, 0.2f, 1.2f, 0.3f);
                context.DamageEnemy(enemy, damage, critical: true);
            }

            // Comes back down.
            yield return new WaitForSeconds(0.25f);
            for (float t = 0f; t < 0.25f; t += Time.deltaTime)
            {
                fx.Height = riseHeight * (1f - t / 0.25f);
                yield return null;
            }
            fx.Height = 0f;
            fx.Squash(new Vector2(1.3f, 0.75f));
            Vfx.Dust(feet, 4, MexicaPalette.Ash, 0.8f);
            yield return new WaitForSeconds(0.35f);
        }
    }
}
