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
        [Tooltip("Zone difficulty: multiplies health and damage (zone 1 = 1, deeper zones are stronger).")]
        [SerializeField, Min(0.1f)] float strength = 1f;
        [Tooltip("Enemies with the same encounter id fight together: when one starts a battle, the others join it " +
                 "(even if they wandered a bit away). Empty = alone.")]
        [SerializeField] string encounter = "";
        [Tooltip("Optional enemies (side quests) do not count for the obsidian key nor for the Enemies Left counter.")]
        [SerializeField] bool optional;

        Vector3 homePosition;

        public string Encounter => encounter;
        public bool Optional => optional;

        /// <summary>True while it still has to be defeated to get the obsidian key (not boss, not optional, alive).</summary>
        public bool CountsAsRemaining
        {
            get
            {
                if (optional || data == null || data.isBoss || !gameObject.activeInHierarchy) return false;
                var health = Health != null ? Health : GetComponent<Health>();
                return health == null || !health.IsDead;
            }
        }

        /// <summary>How many enemies are still in the way of the obsidian key (the Enemies Left counter).</summary>
        public static int Remaining()
        {
            int count = 0;
            foreach (var enemy in FindObjectsByType<CombatEnemy>())
                if (enemy.CountsAsRemaining) count++;
            return count;
        }
        Color baseColor = Color.white;

        public EnemyData Data => data;
        public Health Health { get; private set; }
        public SpriteRenderer Sprite => sprite;
        public bool InCombat { get; private set; }
        public bool IsAvailable => isActiveAndEnabled && !InCombat && !Health.IsDead;
        /// <summary>Where it starts; it comes back here after a chase or a player defeat.</summary>
        public Vector3 HomePosition => homePosition;

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
            Health.Initialize(data.maxHealth * strength);
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

        // ---------------- State during one battle (unique abilities and boss phases) ----------------

        /// <summary>Multiplies its attack power (EmpowerSelf and the boss's second phase).</summary>
        public float PowerMultiplier { get; private set; } = 1f;
        /// <summary>Guard from its last attack; lasts until its next turn.</summary>
        public float GuardMultiplier { get; private set; } = 1f;
        public bool InPhaseTwo { get; private set; }
        public float AttackPower => data.attackPower * PowerMultiplier * strength;
        /// <summary>Damage it really takes: passive thick skin x current guard.</summary>
        public float DamageTakenMultiplier => data.damageTakenMultiplier * GuardMultiplier;

        /// <summary>Unique id in the scene, used by the save to remember defeated enemies.</summary>
        public string SaveId => gameObject.scene.name + "/" + GetPath(transform);

        public void EnterCombat()
        {
            InCombat = true;
            ResetBattleState();
        }

        public void ExitCombat() => InCombat = false;

        void ResetBattleState()
        {
            PowerMultiplier = 1f;
            GuardMultiplier = 1f;
            InPhaseTwo = false;
        }

        public void Empower(float multiplier) => PowerMultiplier = Mathf.Min(2f, PowerMultiplier * Mathf.Max(1f, multiplier));
        public void SetGuard(float multiplier) => GuardMultiplier = Mathf.Clamp(multiplier, 0.1f, 1f);
        /// <summary>Called at the start of its turn: the guard ends.</summary>
        public void BeginTurn() => GuardMultiplier = 1f;

        /// <summary>Enters the second phase once, when the health falls low enough. Returns true at that moment.</summary>
        public bool TryEnterPhaseTwo()
        {
            if (InPhaseTwo || !data.HasPhaseTwo || Health.IsDead || Health.Normalized > data.phaseTwoAtHealth) return false;
            InPhaseTwo = true;
            PowerMultiplier *= data.phaseTwoPowerMultiplier;
            return true;
        }

        public EnemyAttack PickAttack() => data.PickAttack(InPhaseTwo);

        public void HealFraction(float fraction) => Health.Heal(Health.Max * fraction);

        static string GetPath(Transform t) => t.parent == null ? t.name : GetPath(t.parent) + "/" + t.name;

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
            ResetBattleState();
            transform.position = homePosition;
            var body = GetComponent<Rigidbody2D>();
            if (body != null) body.position = homePosition;
            Health.Restore();
            if (sprite != null) sprite.color = baseColor;
            var fx = GetComponent<ActorFx>();
            if (fx != null) fx.Restore();
        }

        IEnumerator FadeOut()
        {
            // Flattens, its souls rise as gold wisps, then it fades (CombatMotion.Death).
            yield return CombatMotion.Death(transform, sprite, Mathf.Max(deathFadeTime, 0.6f));
            InCombat = false;
            gameObject.SetActive(false);
        }
    }
}
