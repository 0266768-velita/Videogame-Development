using System.Collections;
using UnityEngine;

namespace TD_Game
{
    /// <summary>Melee hit with the Macuahuitl against the current target.</summary>
    [CreateAssetMenu(menuName = "TD_Game/Abilities/Attack", fileName = "Ability_Attack")]
    public class AttackAbility : CombatAbility
    {
        [Tooltip("Damage = base damage x player multipliers x this.")]
        [Min(0f)] public float damageMultiplier = 1f;
        [Min(0.05f)] public float lungeTime = 0.22f;

        public override string GetBlockReason(CombatContext context) =>
            context.CurrentTarget == null ? "No target" : base.GetBlockReason(context);

        protected override IEnumerator Execute(CombatContext context)
        {
            var target = context.CurrentTarget;
            if (target == null) yield break;

            float damage = context.Player.AttackDamage(damageMultiplier);
            yield return CombatMotion.Lunge(context.Player.transform, target.transform.position,
                context.StopDistance(target), lungeTime, () => context.DamageEnemy(target, damage));
        }
    }
}
