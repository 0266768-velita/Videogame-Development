using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// End of battle. Victory: growth, unlocks, potion, Macuahuitl charge, full health and new checkpoint.
    /// Defeat: basic Game Over and retry from the checkpoint (the full Game Over screen comes later).
    /// </summary>
    public class CombatRewards : MonoBehaviour
    {
        [SerializeField, Min(0f)] float victoryBannerTime = 2.5f;
        [SerializeField, Min(0f)] float unlockBannerTime = 2f;
        [SerializeField, Min(0f)] float defeatBannerTime = 2f;

        public IEnumerator Victory(CombatContext context, Vector3 returnPosition)
        {
            var player = context.Player;
            var progression = player.Progression;

            float healthBonus = 0f, staminaBonus = 0f;
            var newAbilities = new List<CombatAbility>();
            foreach (var enemy in context.Defeated)
            {
                healthBonus += enemy.bonusMaxHealth;
                staminaBonus += enemy.bonusMaxStamina;
                if (enemy.unlocksOnDefeat == null) continue;
                foreach (var ability in enemy.unlocksOnDefeat)
                    if (progression.Unlock(ability)) newAbilities.Add(ability);
            }

            progression.Grow(healthBonus, staminaBonus);
            progression.RegisterVictory();
            player.AddCharge(context.Souls);
            player.AddPotions(player.Stats.potionsOnVictory);
            player.FullRestore();   // health refills after winning, as in Mictlán v5

            string growth = healthBonus > 0f || staminaBonus > 0f
                ? $"\nMax HP +{Mathf.RoundToInt(healthBonus)}   Max Stamina +{Mathf.RoundToInt(staminaBonus)}"
                : "";
            CombatFeed.ShowBanner(
                $"Victory!  +{Mathf.RoundToInt(context.Souls)} souls  (Macuahuitl {Mathf.RoundToInt(player.ChargeNormalized * 100f)}%){growth}",
                victoryBannerTime);
            yield return new WaitForSeconds(victoryBannerTime);

            foreach (var ability in newAbilities)
            {
                CombatFeed.ShowBanner($"New skill: {ability.displayName}\n{ability.description}", unlockBannerTime);
                yield return new WaitForSeconds(unlockBannerTime);
            }

            player.Teleport(returnPosition);
            progression.SetCheckpoint(returnPosition);
        }

        public IEnumerator Defeat(CombatContext context)
        {
            CombatFeed.ShowBanner("You have fallen...", defeatBannerTime);
            yield return new WaitForSeconds(defeatBannerTime);

            var player = context.Player;
            player.Teleport(player.Progression.Checkpoint);
            player.FullRestore();
            player.EnsureRetryPotions();
            foreach (var enemy in context.Enemies)
                if (enemy != null && !enemy.Health.IsDead) enemy.ResetAfterPlayerDefeat();
        }
    }
}
