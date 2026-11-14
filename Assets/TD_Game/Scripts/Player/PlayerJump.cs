using System.Collections;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Exploration jump. Top-down has no real height, so the jump is visual: the sprite rises
    /// over a shadow that stays on the ground. While airborne the player passes over enemies
    /// (no contact); landing on top of one starts the battle with an AMBUSH (stomp + first turn).
    /// </summary>
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerJump : MonoBehaviour
    {
        [SerializeField] Transform visual;
        [Tooltip("Shadow on the ground (optional). It shrinks while the player is in the air.")]
        [SerializeField] Transform shadow;

        [Tooltip("Visual height in tiles.")]
        [SerializeField, Min(0.1f)] float jumpHeight = 0.7f;
        [SerializeField, Min(0.1f)] float airTime = 0.5f;
        [SerializeField, Min(0f)] float cooldown = 0.1f;
        [Tooltip("Seconds after landing during which touching an enemy counts as an ambush.")]
        [SerializeField, Min(0f)] float ambushWindow = 0.2f;

        PlayerInputReader input;
        Collider2D[] playerColliders;
        Vector3 visualBase, shadowScale;
        float readyAt, landedAt = -10f;

        public bool IsAirborne { get; private set; }
        /// <summary>True right after landing: an enemy touched now is ambushed.</summary>
        public bool CanAmbush => !IsAirborne && Time.time - landedAt <= ambushWindow;

        void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            playerColliders = GetComponents<Collider2D>();
            if (visual != null) visualBase = visual.localPosition;
            if (shadow != null) shadowScale = shadow.localScale;
        }

        void Update()
        {
            if (input.JumpPressed && !IsAirborne && Time.time >= readyAt && GameState.IsExploring)
                StartCoroutine(Jump());
        }

        IEnumerator Jump()
        {
            IsAirborne = true;
            SetEnemyCollisions(ignore: true);

            for (float t = 0f; t < airTime; t += Time.deltaTime)
            {
                float k = t / airTime;
                float height = 4f * jumpHeight * k * (1f - k);   // parabola: 0 -> top -> 0
                if (visual != null) visual.localPosition = visualBase + Vector3.up * height;
                if (shadow != null) shadow.localScale = shadowScale * Mathf.Lerp(1f, 0.6f, height / jumpHeight);
                yield return null;
            }

            if (visual != null) visual.localPosition = visualBase;
            if (shadow != null) shadow.localScale = shadowScale;
            IsAirborne = false;
            landedAt = Time.time;
            readyAt = Time.time + cooldown;
            // Collisions come back on landing: if the player lands on an enemy, contact starts the ambush.
            SetEnemyCollisions(ignore: false);
        }

        void SetEnemyCollisions(bool ignore)
        {
            foreach (var enemy in FindObjectsByType<CombatEnemy>())
                foreach (var enemyCollider in enemy.GetComponents<Collider2D>())
                    foreach (var own in playerColliders)
                        Physics2D.IgnoreCollision(own, enemyCollider, ignore);
        }

        void OnDisable()
        {
            if (!IsAirborne) return;
            IsAirborne = false;
            if (visual != null) visual.localPosition = visualBase;
            if (shadow != null) shadow.localScale = shadowScale;
            SetEnemyCollisions(ignore: false);
        }
    }
}
