using System.Collections;
using UnityEngine;

namespace TD_Game
{
    /// <summary>Mictlan Fury: heals, fills the stamina and boosts the damage for a few turns.</summary>
    [CreateAssetMenu(menuName = "TD_Game/Abilities/Mictlan Fury", fileName = "Ability_MictlanFury")]
    public class FuryAbility : CombatAbility
    {
        [Range(0f, 1f)] public float healFraction = 0.4f;
        [Min(1f)] public float damageMultiplier = 1.5f;
        [Tooltip("Player turns the damage boost lasts (the turn it is used counts).")]
        [Min(1)] public int turns = 3;
        public Color flashColor = MexicaPalette.Deathly;   // the pale power of Mictlantecuhtli

        protected override IEnumerator Execute(CombatContext context)
        {
            var player = context.Player;
            player.Health.Heal(player.Health.Max * healFraction);
            player.AddStamina(player.MaxStamina);
            player.ApplyDamageBuff(damageMultiplier, turns);

            CombatFeed.ShowBanner(displayName + "!", 1.4f);
            CombatFeed.ShowPopup(player.transform.position + Vector3.up, "FURY", flashColor);
            yield return CombatMotion.Flash(context.PlayerSprite, flashColor, 0.8f);
        }
    }
}
