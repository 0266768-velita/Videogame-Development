using System.Collections.Generic;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// The obsidian key of Mictlán v5: it appears next to Tlahuelli when every normal enemy of the map is defeated.
    /// Walk over it to pick it up; with it, the KeyDoor in front of Quetzalcóatl opens.
    /// Needs a SpriteRenderer and a Collider2D marked "Is Trigger". It starts hidden.
    /// </summary>
    public class ObsidianKey : MonoBehaviour
    {
        [SerializeField] SpriteRenderer sprite;
        [SerializeField] Collider2D pickupArea;

        void Start()
        {
            Show(false);
            Invoke(nameof(CheckAllDefeated), 0.3f);   // after loading a save (wait for it), the enemies may be defeated
        }

        void OnEnable() => CombatManager.CombatWon += OnCombatWon;
        void OnDisable() => CombatManager.CombatWon -= OnCombatWon;

        void OnCombatWon(IReadOnlyList<EnemyData> _) => CheckAllDefeated();

        void CheckAllDefeated()
        {
            var player = FindAnyObjectByType<PlayerCombatant>();
            if (player == null || player.Progression.HasKey || sprite.enabled) return;

            foreach (var enemy in FindObjectsByType<CombatEnemy>())   // only active enemies are found
                if (!enemy.Data.isBoss && !enemy.Health.IsDead) return;   // someone is still alive

            // Everyone is defeated: the key appears next to the player.
            transform.position = player.transform.position + new Vector3(1.2f, 0.4f, 0f);
            Show(true);
            GameAudio.PlaySfx("key");
            CombatFeed.ShowBanner("An obsidian key fell from the shadows!", 2.5f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerCombatant>();
            if (player == null || !sprite.enabled) return;
            player.Progression.GiveKey();
            GameAudio.PlaySfx("key");
            CombatFeed.ShowBanner("You have the OBSIDIAN KEY\nThe door to Quetzalcóatl will open", 3f);
            ContextTutorial.ShowOnce("key", "The obsidian key opens the red door in the east: Quetzalcóatl waits behind it.");
            Destroy(gameObject);
        }

        void Show(bool visible)
        {
            sprite.enabled = visible;
            pickupArea.enabled = visible;
        }
    }
}
