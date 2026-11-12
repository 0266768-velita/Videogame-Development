using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TD_Game
{
    /// <summary>
    /// One enemy's turn: red warning, a window to dodge or interrupt, PERFECT timing (+stamina),
    /// anti-spam lock for early presses, the hit and the attack's unique effect.
    /// </summary>
    public class EnemyTurn : MonoBehaviour
    {
        [SerializeField] CombatInput input;
        [SerializeField] Color telegraphColor = new Color(1f, 0.2f, 0.15f);
        [Tooltip("The last part of the window (this fraction) counts as PERFECT.")]
        [SerializeField, Range(0.1f, 0.9f)] float perfectWindowFraction = 0.5f;
        [Tooltip("Pressing before the window opens locks the counter for this many seconds.")]
        [SerializeField, Min(0f)] float earlyPressLockout = 0.3f;
        [SerializeField, Min(0f)] float dodgeStepDistance = 1f;
        [SerializeField, Min(0.05f)] float enemyLungeTime = 0.18f;

        public IEnumerator Run(CombatContext context, CombatEnemy enemy)
        {
            CombatFeed.SetTurnLabel(enemy.Data.displayName + " attacks");
            yield return new WaitForSeconds(0.5f);

            var attack = enemy.Data.PickAttack();
            CombatFeed.ShowBanner(attack.attackName, 1f);

            for (int i = 0; i < attack.hits && !context.PlayerDead; i++)
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

            bool counterable = attack.counter != CounterType.Unavoidable;
            InputAction counterAction = attack.counter == CounterType.Interrupt ? input.Interrupt : input.Dodge;
            string prompt = attack.counter == CounterType.Interrupt ? "INTERRUPT!" : "DODGE!";
            float windowStart = windUp - Mathf.Clamp(attack.counterWindow, 0.1f, windUp);
            float perfectFrom = windowStart + (windUp - windowStart) * (1f - perfectWindowFraction);

            bool success = false, perfect = false;
            float lockedUntil = 0f;
            for (float t = 0f; t < windUp; t += Time.deltaTime)
            {
                bool open = t >= windowStart;
                CombatFeed.SetCounterPrompt(counterable && open && !success ? prompt : null);

                if (counterable && !success && counterAction.WasPressedThisFrame())
                {
                    if (open && t >= lockedUntil) { success = true; perfect = t >= perfectFrom; }
                    else lockedUntil = t + earlyPressLockout;   // too early: anti-spam lock
                }
                yield return null;
            }
            CombatFeed.SetCounterPrompt(null);

            if (success) yield return Countered(context, attack, perfect);
            else yield return Land(context, enemy, attack);

            if (!success || attack.effectIgnoresCounter) ApplyEffect(context, enemy, attack);
        }

        IEnumerator Countered(CombatContext context, EnemyAttack attack, bool perfect)
        {
            bool interrupt = attack.counter == CounterType.Interrupt;
            CombatFeed.ShowBanner(perfect ? "PERFECT!" : interrupt ? "Interrupted!" : "Dodged!", 0.9f);
            if (perfect) context.Player.AddStamina(context.Player.Stats.perfectCounterStaminaBonus);

            if (interrupt) yield return new WaitForSeconds(0.4f);
            else yield return CombatMotion.Step(context.Player.transform, new Vector3(-dodgeStepDistance, 0.4f, 0f), 0.12f);
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
                    CombatFeed.ShowPopup(player.transform.position + above, "-STAMINA", Color.yellow);
                    break;
                case AttackEffect.HealSelf:
                    enemy.HealFraction(attack.effectAmount);
                    CombatFeed.ShowPopup(enemy.transform.position + above, "HEAL", Color.green);
                    break;
                case AttackEffect.Weaken:
                    player.Weaken(attack.effectAmount);
                    CombatFeed.ShowPopup(player.transform.position + above, "WEAKENED", Color.magenta);
                    break;
            }
        }
    }
}
