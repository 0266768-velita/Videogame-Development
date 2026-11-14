using System.Collections;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Makes an enemy able to fight: its health, its data and starting a battle on contact with the player.
    /// The exploration AI (patrol, detection, chase) is a separate component that can also call
    /// CombatManager.Instance.TryStartCombat(this) when it reaches the player.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class CombatEnemy : MonoBehaviour
    {
        [SerializeField] EnemyData data;
        [Tooltip("Sprite used for hit flashes and the attack warning. Empty = first one in the children.")]
        [SerializeField] SpriteRenderer sprite;
        [SerializeField, Min(0f)] float deathFadeTime = 0.4f;

        Vector3 homePosition;
        Color baseColor = Color.white;

        public EnemyData Data => data;
        public Health Health { get; private set; }
        public SpriteRenderer Sprite => sprite;
        public bool InCombat { get; private set; }
        public bool IsAvailable => isActiveAndEnabled && !InCombat && !Health.IsDead;

        void Awake()
        {
            Health = GetComponent<Health>();
            if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite != null) baseColor = sprite.color;
            if (data == null)
            {
                Debug.LogError($"[{nameof(CombatEnemy)}] EnemyData is not assigned on {name}.", this);
                enabled = false;
                return;
            }
            Health.Initialize(data.maxHealth);
            homePosition = transform.position;
        }

        void OnCollisionEnter2D(Collision2D collision) => TryEngage(collision.collider);
        void OnTriggerEnter2D(Collider2D other) => TryEngage(other);

        void TryEngage(Collider2D other)
        {
            if (!GameState.IsExploring || !IsAvailable) return;
            var player = other.GetComponentInParent<PlayerCombatant>();
            if (player == null) return;

            // Landing on an enemy right after a jump is an ambush: stomp damage and the player acts first.
            var jump = player.GetComponent<PlayerJump>();
            if (jump != null && jump.IsAirborne) return;
            bool ambush = jump != null && jump.CanAmbush;
            if (CombatManager.Instance != null) CombatManager.Instance.TryStartCombat(this, ambush);
        }

        public void EnterCombat() => InCombat = true;
        public void ExitCombat() => InCombat = false;

        public void HealFraction(float fraction) => Health.Heal(Health.Max * fraction);

        /// <summary>Raised when any enemy dies (orbs, quests, statistics).</summary>
        public static event System.Action<CombatEnemy> Defeated;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Defeated = null;

        /// <summary>Called by the combat when its health reaches zero.</summary>
        public void OnDefeated()
        {
            Defeated?.Invoke(this);
            StartCoroutine(FadeOut());
        }

        /// <summary>After the player loses, the enemy goes back home with full health.</summary>
        public void ResetAfterPlayerDefeat()
        {
            InCombat = false;
            transform.position = homePosition;
            Health.Restore();
            if (sprite != null) sprite.color = baseColor;
        }

        IEnumerator FadeOut()
        {
            if (sprite != null)
            {
                Color c = sprite.color;
                for (float t = 0f; t < deathFadeTime; t += Time.deltaTime)
                {
                    sprite.color = new Color(c.r, c.g, c.b, 1f - t / deathFadeTime);
                    yield return null;
                }
            }
            InCombat = false;
            gameObject.SetActive(false);
        }
    }
}
