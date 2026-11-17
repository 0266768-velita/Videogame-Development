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

            // Mictlantecuhtli's power: the player crouches, then rises tall while pale rings and ashes surround him.
            var fx = ActorFx.Of(player);
            Vector3 feet = player.transform.position;
            fx.Pose = new Vector2(1.25f, 0.75f);
            fx.Tremble = 0.02f;
            context.Run(CombatMotion.Flash(context.PlayerSprite, flashColor, 0.9f));
            var weapon = MacuahuitlRig.Active(player);
            if (weapon != null) { context.Run(weapon.RaiseTo(-5f, 0.3f)); context.Run(weapon.Glow(flashColor, 0.9f)); }   // raised to the sky
            for (int i = 0; i < 3; i++)
            {
                Vfx.Ring(feet + Vector3.up * 0.4f, flashColor, 1.4f, 0.2f, 0.25f);   // closing in: gathering power
                Vfx.Wisps(feet, 5, MexicaPalette.Ash, 1.8f, 0.6f);
                yield return new WaitForSeconds(0.12f);
            }
            fx.Pose = Vector2.one;
            fx.Tremble = 0f;
            fx.Squash(new Vector2(0.75f, 1.35f));
            CombatFeed.RequestShake(0.12f, 0.3f);
            Vfx.Ring(feet, flashColor, 0.3f, 1.8f, 0.45f, onGround: true, squashY: 0.45f);
            Vfx.Wisps(feet, 14, flashColor, 2.4f, 0.5f);
            yield return new WaitForSeconds(0.45f);
            if (weapon != null) weapon.Release();
        }
    }
}
