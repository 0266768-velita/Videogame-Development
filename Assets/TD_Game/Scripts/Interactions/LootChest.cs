using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// A chest of offerings on the map (the K of the design map). Walk into it during exploration to open it:
    /// it gives gold, potions and some life once, and stays open (darker). Needs a Collider2D marked "Is Trigger".
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LootChest : MonoBehaviour
    {
        [SerializeField] SpriteRenderer sprite;
        [SerializeField, Min(0)] int gold = 30;
        [SerializeField, Min(0)] int potions = 1;
        [SerializeField, Min(0f)] float heal = 20f;
        [SerializeField] string title = "AN OFFERING FOR THE DEAD";
        [SerializeField] Color openedTint = new Color(0.45f, 0.4f, 0.38f);

        bool opened;

        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (opened || !GameState.IsExploring) return;
            var player = other.GetComponentInParent<PlayerCombatant>();
            if (player == null) return;

            opened = true;
            player.Progression.AddGold(gold);
            player.AddPotions(potions);
            if (heal > 0f) player.Health.Heal(heal);
            if (sprite != null) sprite.color = openedTint;
            GameAudio.PlaySfx("key");
            string loot = $"+{gold} gold" + (potions > 0 ? $"   +{potions} potion" : "") + (heal > 0f ? $"   +{heal:0} HP" : "");
            CombatFeed.ShowBanner($"{title}\n{loot}", 2.5f);
        }
    }
}
