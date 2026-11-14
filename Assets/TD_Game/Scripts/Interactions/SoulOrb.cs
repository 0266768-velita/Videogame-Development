using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// The three orbs a defeated enemy leaves behind (in the spirit of the old God of War orbs):
    /// Soul (gold) charges the Macuahuitl, Jade (green) heals and Tonalli (blue) fills the concentration.
    /// </summary>
    public enum OrbType { Soul, Jade, Tonalli }

    /// <summary>
    /// One orb on the ground. It pops out of the enemy, floats, and is collected by walking over it
    /// (or jumping over it) during exploration. It cannot be collected in the middle of a battle.
    /// When the player is close it is pulled towards them.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class SoulOrb : MonoBehaviour
    {
        [SerializeField] SpriteRenderer sprite;
        [SerializeField, Min(0f)] float bobHeight = 0.08f;
        [SerializeField, Min(0f)] float bobSpeed = 3f;
        [Tooltip("Distance (tiles) at which the orb flies to the player.")]
        [SerializeField, Min(0f)] float magnetRadius = 1.2f;
        [SerializeField, Min(0f)] float magnetSpeed = 7f;
        [Tooltip("Slow breathing glow, like a soul, not a toy.")]
        [SerializeField, Range(0f, 1f)] float glowPulse = 0.3f;

        OrbType type;
        float value;
        Vector3 restPosition;
        Vector3 popFrom;
        float popTime = 0.35f, popElapsed = 1f, phase;
        Color baseColor = Color.white;
        PlayerCombatant player;

        public OrbType Type => type;
        public float Value => value;

        /// <summary>Called by OrbSpawner right after creating the orb.</summary>
        public void Launch(OrbType orbType, float orbValue, Color color, Vector3 from, Vector3 to)
        {
            type = orbType;
            value = orbValue;
            baseColor = color;
            if (sprite != null) sprite.color = color;
            popFrom = from;
            restPosition = to;
            popElapsed = 0f;
            phase = Random.value * 10f;
            transform.position = from;
        }

        void Update()
        {
            if (popElapsed < popTime)
            {
                // Arc from the enemy to the resting spot.
                popElapsed += Time.deltaTime;
                float k = Mathf.Clamp01(popElapsed / popTime);
                transform.position = Vector3.Lerp(popFrom, restPosition, k) + Vector3.up * (4f * 0.6f * k * (1f - k));
                return;
            }

            if (GameState.IsExploring && TryFindPlayer() &&
                Vector2.Distance(player.transform.position, restPosition) <= magnetRadius)
            {
                restPosition = Vector3.MoveTowards(restPosition, player.transform.position, magnetSpeed * Time.deltaTime);
            }
            transform.position = restPosition + Vector3.up * (Mathf.Sin((Time.time + phase) * bobSpeed) * bobHeight);
            if (sprite != null)
                sprite.color = MexicaPalette.WithAlpha(baseColor, 1f - glowPulse * (0.5f + 0.5f * Mathf.Sin((Time.time + phase) * 2.2f)));
        }

        bool TryFindPlayer()
        {
            if (player == null) player = FindAnyObjectByType<PlayerCombatant>();
            return player != null;
        }

        void OnTriggerEnter2D(Collider2D other) => OnTriggerStay2D(other);

        // Stay as well as Enter: an orb under the player when the battle ends is collected as soon as exploration resumes.
        void OnTriggerStay2D(Collider2D other)
        {
            if (!GameState.IsExploring || popElapsed < popTime) return;
            var collector = other.GetComponentInParent<PlayerCombatant>();
            if (collector == null) return;
            Collect(collector);
        }

        void Collect(PlayerCombatant collector)
        {
            switch (type)
            {
                case OrbType.Soul: collector.AddCharge(value); break;
                case OrbType.Jade: collector.Health.Heal(value); break;
                case OrbType.Tonalli: collector.AddConcentration(value); break;
            }
            OrbSpawner.NotifyCollected(this, collector);
            Destroy(gameObject);
        }
    }
}
