using System.Collections;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Exploration jump. Top-down has no real height, so the jump is visual: the sprite rises
    /// over a shadow that stays on the ground. While airborne the player:
    ///   - passes over enemies (no contact); landing on top of one starts the battle with an AMBUSH;
    ///   - passes over LOW obstacles (layer "LowObstacle": fallen logs, small rocks, low walls);
    ///   - flies over water and ravines (layer "Hazard"): PlayerHazards decides on landing whether there is ground below.
    /// Tall things (trees, walls, buildings, gates) always block, in the air too.
    /// Sprinting makes the jump longer (the player keeps its speed in the air).
    /// </summary>
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerJump : MonoBehaviour
    {
        public const string LowObstacleLayer = "LowObstacle";
        public const string PlayerLayer = "Player";
        public const string HazardLayer = "Hazard";

        [SerializeField] Transform visual;
        [Tooltip("Shadow on the ground (optional). It shrinks while the player is in the air.")]
        [SerializeField] Transform shadow;

        [Tooltip("Visual height in tiles.")]
        [SerializeField, Min(0.1f)] float jumpHeight = 0.7f;
        [Tooltip("Seconds in the air. Walking (4 tiles/s) x 0.55 s = 2.2 tiles; sprinting = about 3.9 tiles.")]
        [SerializeField, Min(0.1f)] float airTime = 0.55f;
        [SerializeField, Min(0f)] float cooldown = 0.1f;
        [Tooltip("Seconds after landing during which touching an enemy counts as an ambush.")]
        [SerializeField, Min(0f)] float ambushWindow = 0.2f;

        PlayerInputReader input;
        Collider2D[] playerColliders;
        Vector3 visualBase, shadowScale;
        float readyAt, landedAt = -10f;
        int playerLayer = -1, lowLayer = -1;

        public bool IsAirborne { get; private set; }
        /// <summary>True right after landing: an enemy touched now is ambushed.</summary>
        public bool CanAmbush => !IsAirborne && Time.time - landedAt <= ambushWindow;
        /// <summary>Raised on the frame the player touches the ground again.</summary>
        public event System.Action Landed;

        void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            playerColliders = GetComponents<Collider2D>();
            if (visual != null) visualBase = visual.localPosition;
            if (shadow != null) shadowScale = shadow.localScale;
            playerLayer = LayerMask.NameToLayer(PlayerLayer);
            lowLayer = LayerMask.NameToLayer(LowObstacleLayer);
            int hazard = LayerMask.NameToLayer(HazardLayer);
            // The player never bumps into water or ravines: it walks (or falls) into them. Enemies are blocked by them.
            if (playerLayer >= 0 && hazard >= 0) Physics2D.IgnoreLayerCollision(playerLayer, hazard, true);
        }

        void Update()
        {
            if (input.JumpPressed && !IsAirborne && Time.time >= readyAt && GameState.IsExploring)
                StartCoroutine(Jump());
        }

        [Tooltip("While in the air the player is drawn this many orders above the map props, so he goes OVER a log or a rock " +
                 "instead of looking like he walks through it (needs a SortingGroup on the player).")]
        [SerializeField] int airborneOrder = 4;

        IEnumerator Jump()
        {
            IsAirborne = true;
            var group = GetComponent<UnityEngine.Rendering.SortingGroup>();
            int groundOrder = group != null ? group.sortingOrder : 0;
            if (group != null) group.sortingOrder = groundOrder + airborneOrder;
            SetEnemyCollisions(ignore: true);
            SetLowObstacles(ignore: true);

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
            if (group != null && group.sortingOrder == groundOrder + airborneOrder) group.sortingOrder = groundOrder;
            IsAirborne = false;
            landedAt = Time.time;
            readyAt = Time.time + cooldown;
            // Collisions come back on landing: if the player lands on an enemy, contact starts the ambush.
            // Landing on top of a log or a rock pushes the player off it (the physics separates the bodies).
            SetEnemyCollisions(ignore: false);
            SetLowObstacles(ignore: false);
            Landed?.Invoke();
        }

        void SetLowObstacles(bool ignore)
        {
            if (playerLayer >= 0 && lowLayer >= 0) Physics2D.IgnoreLayerCollision(playerLayer, lowLayer, ignore);
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
            SetLowObstacles(ignore: false);
        }
    }
}
