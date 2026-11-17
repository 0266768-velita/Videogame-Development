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

            // Drinks: tips the head back, then jade light rises around him.
            var fx = ActorFx.Of(player);
            fx.Lean = 12f;
            var weapon = MacuahuitlRig.Active(player);
            if (weapon != null) context.Run(weapon.RaiseTo(-160f, 0.15f));   // lowers the weapon to drink
            fx.Squash(new Vector2(0.9f, 1.1f));
            context.Run(CombatMotion.Flash(context.PlayerSprite, healColor, 0.4f));
            Vfx.Wisps(player.transform.position, 10, healColor, 1.5f, 0.45f);
            Vfx.Ring(player.transform.position, healColor, 0.2f, 1f, 0.4f, onGround: true, squashY: 0.45f);
            yield return new WaitForSeconds(0.35f);
            fx.Lean = 0f;
            if (weapon != null) weapon.Release();
            yield return new WaitForSeconds(0.15f);
        }
    }
}
