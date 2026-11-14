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
            yield return CombatMotion.Hop(player.transform, riseHeight, riseTime, null);

            CombatFeed.RequestShake(0.25f, 0.45f);
            CombatFeed.RequestHitStop(0.15f);
            foreach (var enemy in context.Enemies)
                if (enemy != null && !enemy.Health.IsDead) context.DamageEnemy(enemy, damage, critical: true);
            yield return new WaitForSeconds(0.7f);
        }
    }
}
