using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// The sealed door of Quetzalcóatl's chamber. It blocks the way (solid collider) until Tlahuelli touches it
    /// carrying the obsidian key; then it opens forever.
    /// </summary>
    public class KeyDoor : MonoBehaviour
    {
        [SerializeField] SpriteRenderer sprite;
        [SerializeField] Sprite closedSprite;
        [SerializeField] Sprite openSprite;
        [SerializeField] Collider2D blocker;   // the solid part that stops you

        float messageCooldown;

        void Update()
        {
            if (messageCooldown > 0f) messageCooldown -= Time.deltaTime;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            var player = collision.collider.GetComponentInParent<PlayerCombatant>();
            if (player == null) return;

            if (player.Progression.HasKey) Open();
            else if (messageCooldown <= 0f)
            {
                CombatFeed.ShowBanner("The door is sealed.\nDefeat every servant of the gods to find the obsidian key.", 3f);
                GameAudio.PlaySfx("error");
                messageCooldown = 3f;
            }
        }

        /// <summary>Also called after loading a save where the player already has the key.</summary>
        public void Open()
        {
            if (!blocker.enabled) return;
            blocker.enabled = false;
            sprite.sprite = openSprite;
            GameAudio.PlaySfx("door");
        }

        void Start()
        {
            sprite.sprite = closedSprite;
            Invoke(nameof(OpenIfLoadedWithKey), 0.3f);   // wait for the save to load
        }

        void OpenIfLoadedWithKey()
        {
            var player = FindAnyObjectByType<PlayerCombatant>();
            if (player != null && player.Progression.HasKey) Open();
        }
    }
}
