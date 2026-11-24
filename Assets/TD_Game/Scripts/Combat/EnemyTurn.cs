using System.Collections;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// One enemy's turn: red warning, then ONE button (Dodge: Space / B) pressed right before the impact
    /// (in the spirit of Clair Obscur: Expedition 33). In time = no damage and an automatic counter-attack;
    /// PERFECT = a stronger counter, +stamina and +Tonalli. Too early or too late = full damage, and pressing out of
    /// time locks the button (anti-spam). Spells use the same button to INTERRUPT. Then the attack's unique effect.
    /// </summary>
    public class EnemyTurn : MonoBehaviour
    {
        [SerializeField] CombatInput input;
        [SerializeField] Color telegraphColor = MexicaPalette.Blood;
        [Tooltip("The last part of the window (this fraction) counts as PERFECT.")]
        [SerializeField, Range(0.1f, 0.9f)] float perfectWindowFraction = 0.35f;
        [Tooltip("Pressing out of time locks the button for this many seconds (x1.5 for every extra press: mashing never works).")]
        [SerializeField, Min(0f)] float earlyPressLockout = 0.3f;
        [SerializeField, Min(0f)] float dodgeStepDistance = 1f;
        [SerializeField, Min(0.05f)] float enemyLungeTime = 0.18f;
        [Tooltip("Chance that an enemy attack simply misses (0.1 = 1 in 10).")]
        [SerializeField, Range(0f, 0.5f)] float missChance = 0.05f;
        [Tooltip("Every enemy's dodge window is multiplied by this (smaller = harder).")]
        [SerializeField, Range(0.3f, 1.5f)] float counterWindowScale = 0.75f;
        [Tooltip("Extra shrink for the boss's window.")]
        [SerializeField, Range(0.3f, 1f)] float bossWindowScale = 0.75f;
        [Tooltip("The counter-attack of a normal (not PERFECT) dodge hits this fraction of the PERFECT one.")]
        [SerializeField, Range(0.1f, 1f)] float goodCounterFraction = 0.6f;

        public IEnumerator Run(CombatContext context, CombatEnemy enemy)
        {
            CombatFeed.SetTurnLabel(enemy.Data.displayName.ToUpperInvariant() + " ATTACKS");
            enemy.BeginTurn();
            yield return new WaitForSeconds(0.5f);

            var attack = enemy.PickAttack();
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
            context.Run(CombatMotion.ChargeUp(enemy.transform, windUp));
            if (!attack.lungeAtTarget) Vfx.Wisps(enemy.transform.position, 6, telegraphColor, 1.2f, 0.4f);   // a spell gathering

            var result = ActionCommand.Result.Miss;
            if (attack.counter == CounterType.Unavoidable) yield return new WaitForSeconds(windUp);
            else
            {
                bool interrupt = attack.counter == CounterType.Interrupt;
                // One button for every defence: "Press [SPACE] to DODGE!" right before the impact.
                float window = attack.counterWindow * counterWindowScale * (enemy.Data.isBoss ? bossWindowScale : 1f);
                yield return ActionCommand.Run(input.Dodge, interrupt ? "INTERRUPT" : "DODGE", windUp, window,
                    perfectWindowFraction, earlyPressLockout, context.Player.transform, r => result = r);
            }

            bool success = result != ActionCommand.Result.Miss;
            // Sometimes the enemy just misses (only real hits, not howls or guards).
            bool missed = !success && attack.damageMultiplier > 0f && Random.value < missChance;
            if (success) yield return Countered(context, enemy, attack, result == ActionCommand.Result.Perfect);
            else if (missed)
            {
                CombatFeed.ShowPopup(context.Player.transform.position + Vector3.up * 1.3f, "MISS", MexicaPalette.Bone);
                CombatFeed.ShowBanner(enemy.Data.displayName + " missed!", 0.8f);
                yield return new WaitForSeconds(0.5f);
            }
            else yield return Land(context, enemy, attack);

            if ((!success && !missed) || attack.effectIgnoresCounter) ApplyEffect(context, enemy, attack);
        }

        IEnumerator Countered(CombatContext context, CombatEnemy enemy, EnemyAttack attack, bool perfect)
        {
            var player = context.Player;
            bool interrupt = attack.counter == CounterType.Interrupt;
            CombatFeed.ShowBanner(perfect ? "PERFECT!" : interrupt ? "Interrupted!" : "Dodged!", 0.9f);   // GameplayAudio plays the sound of each banner
            CombatFeed.RequestShake(perfect ? 0.08f : 0.04f, 0.15f);
            if (interrupt)
            {
                // The spell breaks: a flash on the caster, who staggers.
                Vfx.Ring(enemy.transform.position + Vector3.up * 0.5f, MexicaPalette.Bone, 0.2f, 1.2f, 0.3f);
                Vfx.Burst(enemy.transform.position + Vector3.up * 0.5f, 10, MexicaPalette.Bone, new Vector2(2f, 4f), 8f, 1.2f, 0.4f, toGround: false, upward: 0.2f);
                CombatMotion.HitReaction(enemy.transform, player.transform.position, 0.8f);
            }
            Vfx.Ring(player.transform.position + Vector3.up * 0.4f, perfect ? MexicaPalette.Turquoise : MexicaPalette.Bone, 0.2f, perfect ? 1.3f : 0.9f, 0.35f);
            if (!interrupt) Vfx.Afterimage(context.PlayerSprite, perfect ? MexicaPalette.Turquoise : MexicaPalette.Bone, 0.3f);

            if (!interrupt)
                yield return CombatMotion.Step(player.transform, new Vector3(-dodgeStepDistance, 0.4f, 0f), 0.12f);
            else
                yield return new WaitForSeconds(0.2f);

            if (enemy.Health.IsDead) yield break;

            // Every successful defence answers with an automatic counter-attack; PERFECT hits harder and also
            // gives stamina and Tonalli.
            if (perfect)
            {
                player.AddStamina(player.Stats.perfectCounterStaminaBonus);
                player.AddConcentration(player.Stats.concentrationPerPerfect);
            }
            float damage = player.AttackDamage(player.Stats.counterAttackMultiplier * (perfect ? 1f : goodCounterFraction));
            GameAudio.PlaySfx("swing");
            var weapon = MacuahuitlRig.Active(player);
            if (weapon != null) context.Run(weapon.RaiseTo(MacuahuitlTechnique.Backhand.windAngle, 0.08f));
            yield return CombatMotion.Lunge(player.transform, enemy.transform.position, context.StopDistance(enemy), 0.12f,
                () =>
                {
                    Vfx.Slash(enemy.transform.position + Vector3.up * 0.5f, 1f, perfect ? MexicaPalette.Turquoise : MexicaPalette.Bone, perfect ? 1.3f : 1f, MacuahuitlTechnique.Backhand.slashAngle);
                    context.DamageEnemy(enemy, damage, critical: perfect, label: "COUNTER");
                },
                weapon != null ? impact => weapon.Strike(MacuahuitlTechnique.Backhand, impact) : null);
        }

        IEnumerator Land(CombatContext context, CombatEnemy enemy, EnemyAttack attack)
        {
            float damage = Mathf.Round(enemy.AttackPower * attack.damageMultiplier);
            if (damage <= 0f)
            {
                // Pure support ability (howl, guard...): no hit, only its effect.
                yield return new WaitForSeconds(0.4f);
            }
            else if (attack.lungeAtTarget)
            {
                yield return CombatMotion.Lunge(enemy.transform, context.Player.transform.position,
                    context.StopDistance(enemy), enemyLungeTime, () => context.DamagePlayer(damage, enemy.Data));
            }
            else
            {
                // Ranged attack or spell: a projectile flies to the player.
                yield return Vfx.Projectile(enemy.transform.position + Vector3.up * 0.5f,
                    context.Player.transform.position + Vector3.up * 0.45f, telegraphColor, 0.3f);
                context.DamagePlayer(damage, enemy.Data);
                yield return new WaitForSeconds(0.3f);
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
                case AttackEffect.EmpowerSelf:
                    if (enemy.Health.IsDead) break;
                    enemy.Empower(attack.effectAmount);
                    CombatFeed.ShowPopup(enemy.transform.position + above, "POWER UP", MexicaPalette.Cinnabar);
                    break;
                case AttackEffect.LifeSteal:
                    if (enemy.Health.IsDead) break;
                    float stolen = Mathf.Round(enemy.AttackPower * attack.damageMultiplier * attack.effectAmount);
                    enemy.Health.Heal(stolen);
                    CombatFeed.ShowPopup(enemy.transform.position + above, "+" + stolen, MexicaPalette.Jade);
                    break;
                case AttackEffect.Stun:
                    player.Stun();
                    CombatFeed.ShowPopup(player.transform.position + above, "STUNNED", MexicaPalette.AgedGold);
                    break;
                case AttackEffect.Guard:
                    if (enemy.Health.IsDead) break;
                    enemy.SetGuard(attack.effectAmount);
                    CombatFeed.ShowPopup(enemy.transform.position + above, "GUARD", MexicaPalette.Bone);
                    break;
            }
        }
    }
}
