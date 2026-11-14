using System.Collections;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Melee hit with the Macuahuitl against the current target.
    /// Action command: press Attack again right before the impact for a CRITICAL (more damage + Tonalli).
    /// Consecutive attacks in the same turn build a combo that adds damage.
    /// </summary>
    [CreateAssetMenu(menuName = "TD_Game/Abilities/Attack", fileName = "Ability_Attack")]
    public class AttackAbility : CombatAbility
    {
        [Tooltip("Damage = base damage x player multipliers x this.")]
        [Min(0f)] public float damageMultiplier = 1f;
        [Min(0.05f)] public float lungeTime = 0.22f;

        [Header("Critical action command")]
        [Tooltip("Wind-up before the lunge; the prompt appears at its end.")]
        [Min(0.2f)] public float windUpTime = 0.55f;
        [Min(0.05f)] public float criticalWindow = 0.2f;
        [Range(0.1f, 1f)] public float perfectFraction = 0.5f;
        [Min(0f)] public float earlyPressLockout = 0.25f;

        public override string GetBlockReason(CombatContext context) =>
            context.CurrentTarget == null ? "No target" : base.GetBlockReason(context);

        protected override IEnumerator Execute(CombatContext context)
        {
            var target = context.CurrentTarget;
            if (target == null) yield break;
            var player = context.Player;

            var result = ActionCommand.Result.Miss;
            context.Run(CombatMotion.Flash(context.PlayerSprite, MexicaPalette.AgedGold, windUpTime));
            yield return ActionCommand.Run(context.Input.Attack, windUpTime, criticalWindow, perfectFraction,
                earlyPressLockout, "NOW!    1 / A", r => result = r);

            bool critical = result != ActionCommand.Result.Miss;
            float multiplier = damageMultiplier * player.ComboMultiplier;
            if (critical) multiplier *= player.Stats.criticalMultiplier * (result == ActionCommand.Result.Perfect ? 1.15f : 1f);
            float damage = player.AttackDamage(multiplier);
            string label = player.ComboCount > 0 ? $"x{player.ComboCount + 1}" : null;

            yield return CombatMotion.Lunge(player.transform, target.transform.position,
                context.StopDistance(target), lungeTime, () =>
                {
                    context.DamageEnemy(target, damage, critical, label);
                    player.RegisterComboHit();
                    if (critical) player.AddConcentration(player.Stats.concentrationPerCritical);
                });

            if (critical) CombatFeed.ShowBanner(result == ActionCommand.Result.Perfect ? "PERFECT CRITICAL!" : "Critical!", 0.7f);
        }
    }
}
