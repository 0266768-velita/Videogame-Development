using System.Collections;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// One enemy's turn: red warning, a window to dodge or interrupt (with anti-spam lock),
    /// PERFECT timing (+stamina, +Tonalli and an automatic counter-attack), the hit and the attack's unique effect.
    /// </summary>
    public class EnemyTurn : MonoBehaviour
    {
        [SerializeField] CombatInput input;
        [SerializeField] Color telegraphColor = MexicaPalette.Blood;
        [Tooltip("The last part of the window (this fraction) counts as PERFECT.")]
        [SerializeField, Range(0.1f, 0.9f)] float perfectWindowFraction = 0.5f;
        [Tooltip("Pressing before the window opens locks the counter for this many seconds.")]
        [SerializeField, Min(0f)] float earlyPressLockout = 0.3f;
        [SerializeField, Min(0f)] float dodgeStepDistance = 1f;
        [SerializeField, Min(0.05f)] float enemyLungeTime = 0.18f;

        public IEnumerator Run(CombatContext context, CombatEnemy enemy)
        {
            CombatFeed.SetTurnLabel(enemy.Data.displayName.ToUpperInvariant() + " ATTACKS");
            yield return new WaitForSeconds(0.5f);

            var attack = enemy.Data.PickAttack();
            CombatFeed.ShowBanner(attack.attackName, 1f);

            for (int i = 0; i < attack.hits && !context.PlayerDead && !enemy.Health.IsDead; i++)
            {
                yield return Hit(context, enemy, attack);
                if (context.HasPendingLastStand) yield return context.ResolveLastStand();
                if (i < attack.hits - 1) yield return new WaitForSeconds(attack.timeBetweenHits);
            }

            yield return new WaitForSeconds(0.4f);
            CombatFeed.SetTurnLabel("");
        }

        IEnumerator Hit(CombatContext context, CombatEnemy enemy, EnemyAttack attack)
        {
            float windUp = attack.windUpTime;
            context.Run(CombatMotion.Telegraph(enemy.Sprite, windUp, telegraphColor));

            var result = ActionCommand.Result.Miss;
            if (attack.counter == CounterType.Unavoidable) yield return new WaitForSeconds(windUp);
            else
            {
                bool interrupt = attack.counter == CounterType.Interrupt;
                yield return ActionCommand.Run(interrupt ? input.Interrupt : input.Dodge, windUp, attack.counterWindow,
                    perfectWindowFraction, earlyPressLockout, interrupt ? "INTERRUPT!    1 / A" : "DODGE!    SPACE / B", r => result = r);
            }

            bool success = result != ActionCommand.Result.Miss;
            if (success) yield return Countered(context, enemy, attack, result == ActionCommand.Result.Perfect);
            else yield return Land(context, enemy, attack);

            if (!success || attack.effectIgnoresCounter) ApplyEffect(context, enemy, attack);
        }

        IEnumerator Countered(CombatContext context, CombatEnemy enemy, EnemyAttack attack, bool perfect)
        {
            var player = context.Player;
            bool interrupt = attack.counter == CounterType.Interrupt;
            CombatFeed.ShowBanner(perfect ? "PERFECT!" : interrupt ? "Interrupted!" : "Dodged!", 0.9f);

            if (!interrupt)
                yield return CombatMotion.Step(player.transform, new Vector3(-dodgeStepDistance, 0.4f, 0f), 0.12f);
            else
                yield return new WaitForSeconds(0.2f);

            if (!perfect) yield break;

            // PERFECT: stamina, Tonalli and a quick counter-attack.
            player.AddStamina(player.Stats.perfectCounterStaminaBonus);
            player.AddConcentration(player.Stats.concentrationPerPerfect);
            float damage = player.AttackDamage(player.Stats.counterAttackMultiplier);
            yield return CombatMotion.Lunge(player.transform, enemy.transform.position, context.StopDistance(enemy), 0.12f,
                () => context.DamageEnemy(enemy, damage, critical: true, label: "COUNTER"));
        }

        IEnumerator Land(CombatContext context, CombatEnemy enemy, EnemyAttack attack)
        {
            float damage = Mathf.Round(enemy.Data.attackPower * attack.damageMultiplier);
            if (attack.lungeAtTarget)
            {
                yield return CombatMotion.Lunge(enemy.transform, context.Player.transform.position,
                    context.StopDistance(enemy), enemyLungeTime, () => context.DamagePlayer(damage, enemy.Data));
            }
            else
            {
                context.DamagePlayer(damage, enemy.Data);
                yield return new WaitForSeconds(0.4f);
            }
        }

        static void ApplyEffect(CombatContext context, CombatEnemy enemy, EnemyAttack attack)
        {
            var player = context.Player;
            Vector3 above = Vector3.up * 1.3f;
            switch (attack.effect)
            {
                case AttackEffect.DrainStamina:
                    player.AddStamina(-attack.effectAmount);
                    CombatFeed.ShowPopup(player.transform.position + above, "-STAMINA", MexicaPalette.Ochre);
                    break;
                case AttackEffect.HealSelf:
                    if (enemy.Health.IsDead) break;
                    enemy.HealFraction(attack.effectAmount);
                    CombatFeed.ShowPopup(enemy.transform.position + above, "HEAL", MexicaPalette.Jade);
                    break;
                case AttackEffect.Weaken:
                    player.Weaken(attack.effectAmount);
                    CombatFeed.ShowPopup(player.transform.position + above, "WEAKENED", MexicaPalette.Ash);
                    break;
            }
        }
    }
}
