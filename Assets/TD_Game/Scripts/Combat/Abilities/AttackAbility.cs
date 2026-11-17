using System.Collections;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Melee hit with the Macuahuitl against the current target.
    /// Action command: press Attack again right before the impact for a CRITICAL (more damage + Tonalli).
    /// Consecutive attacks in the same turn build a combo that adds damage, and each blow of the combo is
    /// a different technique (cut, sweep, rising cut, whirlwind), so the fight never looks repetitive.
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

        [Header("Macuahuitl techniques")]
        [Tooltip("One per blow of the combo, in order; it starts again after the last one.")]
        public MacuahuitlTechnique[] comboSwings =
        {
            MacuahuitlTechnique.Cleave, MacuahuitlTechnique.Sweep, MacuahuitlTechnique.RisingCut, MacuahuitlTechnique.Whirlwind
        };
        [Tooltip("Used instead when the action command is a critical.")]
        public MacuahuitlTechnique criticalSwing = MacuahuitlTechnique.ObsidianStrike;

        public override string GetBlockReason(CombatContext context) =>
            context.CurrentTarget == null ? "No target" : base.GetBlockReason(context);

        protected override IEnumerator Execute(CombatContext context)
        {
            var target = context.CurrentTarget;
            if (target == null) yield break;
            var player = context.Player;
            var rig = player.GetComponent<MacuahuitlRig>();
            if (rig != null && !rig.IsShown) rig = null;
            var swing = comboSwings != null && comboSwings.Length > 0
                ? comboSwings[player.ComboCount % comboSwings.Length]
                : MacuahuitlTechnique.Cleave;

            var result = ActionCommand.Result.Miss;
            // Wind-up: crouches and trembles while the Macuahuitl is raised into position and gleams.
            context.Run(CombatMotion.ChargeUp(player.transform, windUpTime));
            context.Run(CombatMotion.Flash(context.PlayerSprite, MexicaPalette.AgedGold, windUpTime));
            if (rig != null)
            {
                context.Run(rig.RaiseTo(swing.windAngle, windUpTime * 0.6f));
                context.Run(rig.Glow(MexicaPalette.AgedGold, windUpTime));
            }
            yield return ActionCommand.Run(context.Input.Attack, windUpTime, criticalWindow, perfectFraction,
                earlyPressLockout, "NOW!    1 / A", r => result = r);

            bool critical = result != ActionCommand.Result.Miss;
            if (critical)
            {
                swing = criticalSwing;
                if (rig != null) context.Run(rig.RaiseTo(swing.windAngle, 0.06f));   // snaps high for the smash
            }
            float multiplier = damageMultiplier * player.ComboMultiplier;
            if (critical) multiplier *= player.Stats.criticalMultiplier * (result == ActionCommand.Result.Perfect ? 1.15f : 1f);
            float damage = player.AttackDamage(multiplier);
            string label = player.ComboCount > 0 ? $"x{player.ComboCount + 1}" : null;

            void OnHit()
            {
                Vector3 feet = target.transform.position;
                Vector3 hit = feet + Vector3.up * swing.slashHeight;
                float side = Mathf.Sign(feet.x - player.transform.position.x);
                Color slashColor = critical ? MexicaPalette.Critical : MexicaPalette.Bone;
                Vfx.Slash(hit, side, slashColor, critical ? 1.5f : 1.1f, swing.slashAngle);
                if (swing.spin) Vfx.Slash(hit, side, MexicaPalette.Bone, 1.2f, swing.slashAngle + 180f);   // second pass
                if (critical || swing.heavy)
                {
                    Vfx.Slash(hit, side, MexicaPalette.Bone, 1.2f, swing.slashAngle + 75f);           // crossed cut
                    Vfx.Ring(hit, MexicaPalette.Critical, 0.1f, 0.9f, 0.22f);
                    Vfx.Crack(feet, 1.2f, 1f);
                }
                Vfx.Burst(hit, critical ? 14 : 7, slashColor, new Vector2(2f, 5f), 10f, 1.2f, 0.4f, toGround: false, upward: 0.3f);
                context.DamageEnemy(target, damage, critical, label);
                player.RegisterComboHit();
                if (critical) player.AddConcentration(player.Stats.concentrationPerCritical);
            }

            yield return CombatMotion.Lunge(player.transform, target.transform.position, context.StopDistance(target),
                lungeTime, OnHit, rig != null ? impact => rig.Strike(swing, impact) : null);

            CombatFeed.ShowBanner(critical
                ? (result == ActionCommand.Result.Perfect ? "PERFECT CRITICAL!" : "Critical!")
                : swing.name, critical ? 0.7f : 0.5f);
        }
    }
}
