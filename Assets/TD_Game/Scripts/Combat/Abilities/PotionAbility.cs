using System.Collections;
using UnityEngine;

namespace TD_Game
{
    /// <summary>Drinks a potion: heals a fraction of the max health.</summary>
    [CreateAssetMenu(menuName = "TD_Game/Abilities/Potion", fileName = "Ability_Potion")]
    public class PotionAbility : CombatAbility
    {
        [Range(0.05f, 1f)] public float healFraction = 0.35f;
        public Color healColor = MexicaPalette.Jade;

        public override string GetBlockReason(CombatContext context) =>
            context.Player.Potions <= 0 ? "No potions left" : base.GetBlockReason(context);

        protected override IEnumerator Execute(CombatContext context)
        {
            var player = context.Player;
            if (!player.TryUsePotion()) yield break;

            float heal = player.Health.Max * healFraction;
            player.Health.Heal(heal);
            CombatFeed.ShowPopup(player.transform.position + Vector3.up, "+" + Mathf.RoundToInt(heal), healColor);
            yield return CombatMotion.Flash(context.PlayerSprite, healColor, 0.4f);
        }
    }
}
