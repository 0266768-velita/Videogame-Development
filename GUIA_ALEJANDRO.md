# Mictlán — Guía de Alejandro: enemigos, menús, audio y guardado

Esta es tu parte del juego (créditos: *enemies · menus · audio · saving*). Cada archivo trae primero qué hace y con qué se conecta, y luego su código completo. Los comentarios /// y // dentro del código explican cada parte.

## Mapa de conexiones

```
TD_Boot   : BootScreens --SceneFader--> TD_MainMenu
TD_MainMenu: TitleMenu --GameSave.NewGame/Continue--> SceneFader --> Prólogo / MainMap_CSV
MainMap_CSV:
  EnemyAI (patrulla, persigue) -> CombatEnemy (al tocar) -> CombatManager.TryStartCombat
      CombatManager -> PlayerTurn / EnemyTurn (usa EnemyData + EnemyAttack)
      victoria -> CombatRewards -> CombatWon -> SaveController.Save -> GameSave.Write
      derrota  -> DefeatScreen
  CombatEnemy.Defeated -> OrbSpawner, GameplayAudio, SaveController, EnemiesLeftCounter, ObsidianKey
  GameplayAudio / AmbientSoundscape / cualquier script -> GameAudio -> AudioLibrary
  PauseScreen (ESC) -> GameState.Paused
  Jefe vencido -> CreditsScreen -> TD_MainMenu
Todos los menús se construyen con MenuWidgets.
```

## Índice

- **Enemigos**: `EnemyData.cs`, `EnemyAttack.cs`, `CombatEnemy.cs`, `EnemyAI.cs`, `EnemyTurn.cs`
- **Menús y pantallas**: `MenuWidgets.cs`, `SceneFader.cs`, `BootScreens.cs`, `TitleMenu.cs`, `PauseScreen.cs`, `DefeatScreen.cs`, `CreditsScreen.cs`
- **Audio**: `AudioLibrary.cs`, `GameAudio.cs`, `GameplayAudio.cs`, `AmbientSoundscape.cs`
- **Guardado**: `GameSave.cs`, `SaveController.cs`

---

## Enemigos

### EnemyData.cs

Ruta: `Assets/TD_Game/Scripts/Combat/Data/EnemyData.cs` · 93 líneas

Ficha de cada enemigo (ScriptableObject): vida, ataque, velocidades de la IA, orbes y oro que suelta, y si es jefe. **Se conecta con:** lo usan CombatEnemy (vida y fuerza), EnemyAI (velocidades y radios), EnemyTurn (qué ataque usa), CombatRewards y OrbSpawner (recompensas). Los assets están en ScriptableObjects/Enemies.

```csharp
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Data for one enemy type (exploration, combat and rewards), adapted from Mictlán v5.
    /// Create with: right click > Create > TD_Game > Enemy Data.
    /// </summary>
    [CreateAssetMenu(menuName = "TD_Game/Enemy Data", fileName = "EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Enemy";
        [Tooltip("The final boss: its defeat rolls the credits.")]
        public bool isBoss = false;

        [Header("Exploration (used by EnemyAI; distances in tiles)")]
        [Min(0f)] public float patrolSpeed = 1.5f;
        [Min(0f)] public float chaseSpeed = 3f;
        [Tooltip("Sees the player this close (if no wall is in between).")]
        [Min(0f)] public float detectRadius = 5f;
        [Tooltip("Stops chasing when the player is this far.")]
        [Min(0f)] public float loseRadius = 8f;
        [Tooltip("Never goes farther than this from its home position; it returns home instead.")]
        [Min(0f)] public float leashRadius = 10f;
        [Tooltip("Random patrol around home within this radius (when it has no patrol points).")]
        [Min(0f)] public float patrolRadius = 2.5f;
        [Tooltip("Seconds it waits at each patrol point.")]
        [Min(0f)] public float patrolWait = 1.5f;
        [Tooltip("When it sees the player, allies this close also start chasing (wolf howl). 0 = off.")]
        [Min(0f)] public float alertAlliesRadius = 0f;

        [Header("Combat")]
        [Min(1f)] public float maxHealth = 60f;
        [Min(0f)] public float attackPower = 12f;
        [Tooltip("Passive: multiplies all the damage it receives (0.8 = thick skin, takes 20% less).")]
        [Min(0.1f)] public float damageTakenMultiplier = 1f;
        [Tooltip("The enemies act before the player in the first round.")]
        public bool startsBattleFirst = false;
        [Tooltip("Body radius in tiles; lunges stop in front of it.")]
        [Min(0.1f)] public float bodyRadius = 0.45f;
        public EnemyAttack[] attacks = { new EnemyAttack() };

        [Header("Second phase (boss). Empty attacks = no second phase")]
        [Tooltip("Enters the second phase when its health falls to this fraction.")]
        [Range(0.05f, 0.95f)] public float phaseTwoAtHealth = 0.5f;
        public string phaseTwoName = "";
        [Tooltip("Attack power multiplier in the second phase.")]
        [Min(0.1f)] public float phaseTwoPowerMultiplier = 1.25f;
        public EnemyAttack[] phaseTwoAttacks;

        public bool HasPhaseTwo => phaseTwoAttacks != null && phaseTwoAttacks.Length > 0;

        [Header("Orbs dropped on death (collected by walking over them after the battle)")]
        [Tooltip("Total Macuahuitl charge in gold soul orbs.")]
        [Min(0f)] public float soulsOnDefeat = 12f;
        [Tooltip("Total health in green jade orbs.")]
        [Min(0f)] public float healthOnDefeat = 10f;
        [Tooltip("Total Tonalli (concentration) in blue orbs.")]
        [Min(0f)] public float concentrationOnDefeat = 20f;

        [Header("Gold (spent in the shop)")]
        [Min(0)] public int goldOnDefeat = 10;

        [Header("Rewards")]
        [Tooltip("Permanent increase of the player's max health.")]
        [Min(0f)] public float bonusMaxHealth = 0f;
        [Tooltip("Permanent increase of the player's max stamina.")]
        [Min(0f)] public float bonusMaxStamina = 0f;
        [Tooltip("Abilities unlocked when the player wins a battle against this enemy.")]
        public CombatAbility[] unlocksOnDefeat;

        [Header("Last Stand")]
        [Tooltip("If set, the first time this enemy would kill the player, the player survives with 1 HP and this ability is unlocked and used.")]
        public CombatAbility lastStandAbility;

        /// <summary>Picks an attack using the weights (from the second-phase list if phaseTwo is true).</summary>
        public EnemyAttack PickAttack(bool phaseTwo = false)
        {
            var attacks = phaseTwo && HasPhaseTwo ? phaseTwoAttacks : this.attacks;
            if (attacks == null || attacks.Length == 0) return new EnemyAttack();
            float total = 0f;
            foreach (var a in attacks) total += a.weight;
            float roll = Random.value * total;
            foreach (var a in attacks)
            {
                roll -= a.weight;
                if (roll <= 0f) return a;
            }
            return attacks[attacks.Length - 1];
        }
    }
}
```
### EnemyAttack.cs

Ruta: `Assets/TD_Game/Scripts/Combat/Data/EnemyAttack.cs` · 54 líneas

Un ataque de un enemigo: multiplicador de daño, si se puede esquivar o interrumpir y su efecto único (DrainStamina, HealSelf, Weaken). **Se conecta con:** vive dentro de EnemyData; EnemyTurn lo ejecuta.

```csharp
using UnityEngine;

namespace TD_Game
{
    /// <summary>How the player can counter an enemy attack.</summary>
    public enum CounterType
    {
        Dodge,       // Combat/Dodge
        Interrupt,   // Combat/Interrupt (meant for spells)
        Unavoidable
    }

    /// <summary>Extra effect of an attack. This is how each enemy gets its unique abilities as data.</summary>
    public enum AttackEffect
    {
        None,
        DrainStamina,   // removes effectAmount stamina from the player
        HealSelf,       // the enemy heals effectAmount (fraction of its max health)
        Weaken,         // the player's damage is multiplied by effectAmount during their next turn
        // Unique abilities of the 3 final enemies (3.8, 3.13, 3.17) and the boss (4.6). Add new ones at the END
        // so the numbers already saved in the assets do not change.
        EmpowerSelf,    // the enemy's attack power is multiplied by effectAmount for the rest of the battle (max x2)
        LifeSteal,      // the enemy heals effectAmount x the damage it dealt
        Stun,           // the player loses their next turn
        Guard           // the enemy takes effectAmount x damage until its next turn (0.5 = half damage)
    }

    /// <summary>One enemy attack (adapted from Mictlán v5's EnemyAttackData).</summary>
    [System.Serializable]
    public class EnemyAttack
    {
        public string attackName = "Attack";
        [Tooltip("Damage per hit = enemy attackPower x this.")]
        [Min(0f)] public float damageMultiplier = 1f;
        [Min(1)] public int hits = 1;
        [Tooltip("Seconds of red warning before the hit lands.")]
        [Min(0.3f)] public float windUpTime = 1.2f;
        [Tooltip("Seconds before the impact during which the counter works.")]
        [Min(0.1f)] public float counterWindow = 0.4f;
        [Min(0f)] public float timeBetweenHits = 0.6f;
        public CounterType counter = CounterType.Dodge;
        [Tooltip("true = the enemy lunges at the player (melee). false = ranged attack or spell.")]
        public bool lungeAtTarget = true;
        [Tooltip("Relative chance of picking this attack.")]
        [Min(0.1f)] public float weight = 1f;

        [Header("Unique effect")]
        public AttackEffect effect = AttackEffect.None;
        [Tooltip("DrainStamina: stamina points. HealSelf: fraction of max health. Weaken, EmpowerSelf, Guard: multiplier. LifeSteal: fraction of the damage dealt. Stun: not used.")]
        public float effectAmount = 0f;
        [Tooltip("Off = the effect only applies when the hit lands. On = it applies even if countered.")]
        public bool effectIgnoresCounter = false;
    }
}
```
### CombatEnemy.cs

Ruta: `Assets/TD_Game/Scripts/Enemies/CombatEnemy.cs` · 175 líneas

El componente que convierte un objeto en enemigo: inicia el combate al tocar al jugador, guarda su vida y fuerza, su campamento (encounter) y si es opcional. Avisa con el evento Defeated cuando muere. **Se conecta con:** Health, EnemyData, CombatManager.TryStartCombat, PlayerJump (emboscada), SaveController (lo recuerda derrotado), ObsidianKey y EnemiesLeftCounter (Remaining()), OrbSpawner y GameplayAudio (escuchan Defeated).

```csharp
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
```
### EnemyAI.cs

Ruta: `Assets/TD_Game/Scripts/Enemies/EnemyAI.cs` · 253 líneas

La IA en el mapa: patrulla, ve al jugador (las paredes tapan la vista), lo persigue y regresa a casa. Si uno de un campamento te ve, avisa a los demás. **Se conecta con:** CombatEnemy (datos y estado), GameState (solo se mueve en exploración), Rigidbody2D cinemático.

```csharp
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Exploration AI of an enemy (task 2.11): patrol, detect the player, chase and return home.
    /// The battle itself starts in CombatEnemy when the two bodies touch.
    /// All the numbers (speeds, radii) come from the EnemyData of the CombatEnemy.
    ///
    /// Walls: any solid collider without a moving Rigidbody2D (Tilemap colliders, rocks...) blocks the sight and the movement.
    /// </summary>
    [RequireComponent(typeof(CombatEnemy), typeof(Rigidbody2D))]
    public class EnemyAI : MonoBehaviour
    {
        enum State { Patrol, Chase, Return }

        [Tooltip("Optional: points it walks between in order. Empty = random points around home.")]
        [SerializeField] Transform[] patrolPoints;
        [Tooltip("Empty = the PlayerCombatant of the scene.")]
        [SerializeField] Transform target;
        [Tooltip("Seconds it ignores the player after a battle or a respawn.")]
        [SerializeField, Min(0f)] float calmTimeAfterBattle = 2f;
        [Tooltip("Flip the sprite to look where it walks (art drawn facing right).")]
        [SerializeField] bool flipSprite = true;

        CombatEnemy enemy;
        Rigidbody2D body;
        EnemyData data;
        State state = State.Patrol;
        Vector2 patrolTarget;
        int patrolIndex;
        float waitTimer;
        float calmTimer;
        bool wasBusy;
        readonly RaycastHit2D[] hits = new RaycastHit2D[8];

        public bool IsChasing => state == State.Chase;

        void Awake()
        {
            enemy = GetComponent<CombatEnemy>();
            body = GetComponent<Rigidbody2D>();
            // Kinematic: it moves by script and is never pushed by the player.
            body.bodyType = RigidbodyType2D.Kinematic;
            body.useFullKinematicContacts = false;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        void Start()
        {
            data = enemy.Data;
            if (target == null)
            {
                var player = FindAnyObjectByType<PlayerCombatant>();
                if (player != null) target = player.transform;
            }
            patrolTarget = enemy.HomePosition;
            waitTimer = data != null ? data.patrolWait : 1f;
        }

        void FixedUpdate()
        {
            if (data == null || target == null) return;

            // Freeze while there is a battle, a pause or a cutscene.
            if (!GameState.IsExploring || enemy.InCombat)
            {
                wasBusy = true;
                return;
            }
            if (wasBusy)
            {
                wasBusy = false;
                calmTimer = calmTimeAfterBattle;
                state = State.Return;
            }
            if (calmTimer > 0f) calmTimer -= Time.fixedDeltaTime;

            switch (state)
            {
                case State.Patrol: Patrol(); break;
                case State.Chase: Chase(); break;
                case State.Return: Return(); break;
            }
        }

        // ---------------- States ----------------

        void Patrol()
        {
            if (CanSeePlayer())
            {
                StartChase();
                return;
            }

            if (Vector2.Distance(body.position, patrolTarget) < 0.1f)
            {
                waitTimer -= Time.fixedDeltaTime;
                if (waitTimer <= 0f)
                {
                    patrolTarget = NextPatrolPoint();
                    waitTimer = data.patrolWait;
                }
                return;
            }

            // If a wall is in the way, pick another point.
            // Blocked: stop and rest a moment before choosing another point (no back-and-forth shaking).
            if (!MoveTowards(patrolTarget, data.patrolSpeed)) { patrolTarget = body.position; waitTimer = data.patrolWait; }
        }

        void Chase()
        {
            float toPlayer = Vector2.Distance(body.position, target.position);
            float fromHome = Vector2.Distance(body.position, enemy.HomePosition);
            if (toPlayer > data.loseRadius || fromHome > data.leashRadius)
            {
                state = State.Return;
                return;
            }
            MoveTowards(target.position, data.chaseSpeed);
        }

        void Return()
        {
            if (calmTimer <= 0f && CanSeePlayer() && Vector2.Distance(body.position, enemy.HomePosition) < data.leashRadius * 0.5f)
            {
                StartChase();
                return;
            }
            if (Vector2.Distance(body.position, enemy.HomePosition) < 0.1f)
            {
                state = State.Patrol;
                patrolTarget = enemy.HomePosition;
                return;
            }
            // Blocked on the way back: jump home (rare, keeps the enemy from getting stuck forever).
            if (!MoveTowards(enemy.HomePosition, data.patrolSpeed * 1.5f)) body.position = enemy.HomePosition;
        }

        void StartChase()
        {
            state = State.Chase;
            if (data.alertAlliesRadius > 0f) AlertAllies();
            AlertEncounter();
        }

        /// <summary>A camp fights as one: when one of its members sees Tlahuelli, the others come too.</summary>
        void AlertEncounter()
        {
            if (string.IsNullOrEmpty(enemy.Encounter)) return;
            foreach (var ally in FindObjectsByType<EnemyAI>())
                if (ally != this && ally.state != State.Chase && ally.enemy.Encounter == enemy.Encounter) ally.Alert();
        }

        /// <summary>Wolf howl: nearby enemies that are calm also start chasing.</summary>
        void AlertAllies()
        {
            foreach (var ally in FindObjectsByType<EnemyAI>())
            {
                if (ally == this || ally.state == State.Chase || ally.enemy.InCombat) continue;
                if (Vector2.Distance(ally.transform.position, transform.position) <= data.alertAlliesRadius)
                    ally.state = State.Chase;
            }
        }

        /// <summary>Called by others (howl, story) to make this enemy chase now.</summary>
        public void Alert()
        {
            if (enemy.IsAvailable) state = State.Chase;
        }

        // ---------------- Helpers ----------------

        bool CanSeePlayer()
        {
            if (calmTimer > 0f) return false;
            Vector2 from = body.position;
            Vector2 to = target.position;
            if (Vector2.Distance(from, to) > data.detectRadius) return false;

            // A wall between the enemy and the player blocks the sight.
            foreach (var hit in Physics2D.LinecastAll(from, to))
                if (IsWall(hit.collider)) return false;
            return true;
        }

        /// <summary>Moves one physics step. Slides along walls. Returns false if it could not move at all.</summary>
        bool MoveTowards(Vector2 destination, float speed)
        {
            Vector2 delta = destination - body.position;
            float step = Mathf.Min(speed * Time.fixedDeltaTime, delta.magnitude);
            if (step <= 0.0001f) return true;
            Vector2 dir = delta.normalized;

            // Try straight, then only horizontal, then only vertical.
            Vector2[] options = { dir, new Vector2(Mathf.Sign(dir.x), 0f), new Vector2(0f, Mathf.Sign(dir.y)) };
            foreach (var option in options)
            {
                if (option.sqrMagnitude < 0.01f || IsBlocked(option, step)) continue;
                body.MovePosition(body.position + option * step);
                Face(dir.x);
                return true;
            }
            return false;
        }

        bool IsBlocked(Vector2 direction, float distance)
        {
            int count = body.Cast(direction, hits, distance + 0.02f);
            for (int i = 0; i < count; i++)
                if (IsWall(hits[i].collider)) return true;
            return false;
        }

        static bool IsWall(Collider2D c) =>
            c != null && !c.isTrigger && (c.attachedRigidbody == null || c.attachedRigidbody.bodyType == RigidbodyType2D.Static);

        Vector2 NextPatrolPoint()
        {
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                if (patrolPoints[patrolIndex] != null) return patrolPoints[patrolIndex].position;
            }
            return (Vector2)enemy.HomePosition + Random.insideUnitCircle * data.patrolRadius;
        }

        // Turning around waits a little: without this, an enemy sliding along a wall or standing on its target
        // point turned left-right-left every frame and its picture flickered.
        float turnTimer;

        void Face(float x)
        {
            if (!flipSprite || enemy.Sprite == null) return;
            bool wantsLeft = x < 0f;
            if (Mathf.Abs(x) < 0.3f || wantsLeft == enemy.Sprite.flipX) { turnTimer = 0f; return; }
            turnTimer += Time.fixedDeltaTime;
            if (turnTimer >= 0.25f) { enemy.Sprite.flipX = wantsLeft; turnTimer = 0f; }
        }

        void OnDrawGizmosSelected()
        {
            var e = GetComponent<CombatEnemy>();
            if (e == null || e.Data == null) return;
            Vector3 home = Application.isPlaying ? e.HomePosition : transform.position;
            Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, e.Data.detectRadius);
            Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, e.Data.loseRadius);
            Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(home, e.Data.leashRadius);
        }
    }
}
```
### EnemyTurn.cs

Ruta: `Assets/TD_Game/Scripts/Combat/EnemyTurn.cs` · 192 líneas

El turno del enemigo en combate: telegrafía el golpe, espera la tecla de esquivar (ActionCommand), aplica daño o contraataque. **Se conecta con:** CombatManager (lo llama por cada enemigo), CombatContext (daño), CombatFeed (avisos para la UI), CombatMotion (animación), GameAudio.

```csharp
using System.Collections;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// One enemy's turn: red warning, then ONE button (Dodge: Space / B) pressed right before the impact
    /// (in the spirit of Clair Obscur: Expedition 33). In time = no damage and an automatic counter-attack;
    /// PERFECT = a stronger counter, +stamina and +Tonalli. Too early or too late = full damage, and pressing out of
    /// time locks the button (anti-spam). Spells use the same button to INTERRUPT. Then the attack's unique effect.
    /// </summary>
    public class EnemyTurn : MonoBehaviour
    {
        [SerializeField] CombatInput input;
        [SerializeField] Color telegraphColor = MexicaPalette.Blood;
        [Tooltip("The last part of the window (this fraction) counts as PERFECT.")]
        [SerializeField, Range(0.1f, 0.9f)] float perfectWindowFraction = 0.35f;
        [Tooltip("Pressing out of time locks the button for this many seconds (x1.5 for every extra press: mashing never works).")]
        [SerializeField, Min(0f)] float earlyPressLockout = 0.3f;
        [SerializeField, Min(0f)] float dodgeStepDistance = 1f;
        [SerializeField, Min(0.05f)] float enemyLungeTime = 0.18f;
        [Tooltip("Chance that an enemy attack simply misses (0.1 = 1 in 10).")]
        [SerializeField, Range(0f, 0.5f)] float missChance = 0.05f;
        [Tooltip("Every enemy's dodge window is multiplied by this (smaller = harder).")]
        [SerializeField, Range(0.3f, 1.5f)] float counterWindowScale = 0.75f;
        [Tooltip("Extra shrink for the boss's window.")]
        [SerializeField, Range(0.3f, 1f)] float bossWindowScale = 0.75f;
        [Tooltip("The counter-attack of a normal (not PERFECT) dodge hits this fraction of the PERFECT one.")]
        [SerializeField, Range(0.1f, 1f)] float goodCounterFraction = 0.6f;

        public IEnumerator Run(CombatContext context, CombatEnemy enemy)
        {
            CombatFeed.SetTurnLabel(enemy.Data.displayName.ToUpperInvariant() + " ATTACKS");
            enemy.BeginTurn();
            yield return new WaitForSeconds(0.5f);

            var attack = enemy.PickAttack();
            CombatFeed.ShowBanner(attack.attackName, 1f);

            for (int i = 0; i < attack.hits && !context.PlayerDead && !enemy.Health.IsDead; i++)
            {
                yield return Hit(context, enemy, attack);
                if (context.HasPendingLastStand) yield return context.ResolveLastStand();
                if (i < attack.hits - 1) yield return new WaitForSeconds(attack.timeBetweenHits);
            }

            yield return new WaitForSeconds(0.4f);
            CombatFeed.SetTurnLabel("");
        }

        IEnumerator Hit(CombatContext context, CombatEnemy enemy, EnemyAttack attack)
        {
            float windUp = attack.windUpTime;
            context.Run(CombatMotion.Telegraph(enemy.Sprite, windUp, telegraphColor));
            context.Run(CombatMotion.ChargeUp(enemy.transform, windUp));
            if (!attack.lungeAtTarget) Vfx.Wisps(enemy.transform.position, 6, telegraphColor, 1.2f, 0.4f);   // a spell gathering

            var result = ActionCommand.Result.Miss;
            if (attack.counter == CounterType.Unavoidable) yield return new WaitForSeconds(windUp);
            else
            {
                bool interrupt = attack.counter == CounterType.Interrupt;
                // One button for every defence: "Press [SPACE] to DODGE!" right before the impact.
                float window = attack.counterWindow * counterWindowScale * (enemy.Data.isBoss ? bossWindowScale : 1f);
                yield return ActionCommand.Run(input.Dodge, interrupt ? "INTERRUPT" : "DODGE", windUp, window,
                    perfectWindowFraction, earlyPressLockout, context.Player.transform, r => result = r);
            }

            bool success = result != ActionCommand.Result.Miss;
            // Sometimes the enemy just misses (only real hits, not howls or guards).
            bool missed = !success && attack.damageMultiplier > 0f && Random.value < missChance;
            if (success) yield return Countered(context, enemy, attack, result == ActionCommand.Result.Perfect);
            else if (missed)
            {
                CombatFeed.ShowPopup(context.Player.transform.position + Vector3.up * 1.3f, "MISS", MexicaPalette.Bone);
                CombatFeed.ShowBanner(enemy.Data.displayName + " missed!", 0.8f);
                yield return new WaitForSeconds(0.5f);
            }
            else yield return Land(context, enemy, attack);

            if ((!success && !missed) || attack.effectIgnoresCounter) ApplyEffect(context, enemy, attack);
        }

        IEnumerator Countered(CombatContext context, CombatEnemy enemy, EnemyAttack attack, bool perfect)
        {
            var player = context.Player;
            bool interrupt = attack.counter == CounterType.Interrupt;
            CombatFeed.ShowBanner(perfect ? "PERFECT!" : interrupt ? "Interrupted!" : "Dodged!", 0.9f);   // GameplayAudio plays the sound of each banner
            CombatFeed.RequestShake(perfect ? 0.08f : 0.04f, 0.15f);
            if (interrupt)
            {
                // The spell breaks: a flash on the caster, who staggers.
                Vfx.Ring(enemy.transform.position + Vector3.up * 0.5f, MexicaPalette.Bone, 0.2f, 1.2f, 0.3f);
                Vfx.Burst(enemy.transform.position + Vector3.up * 0.5f, 10, MexicaPalette.Bone, new Vector2(2f, 4f), 8f, 1.2f, 0.4f, toGround: false, upward: 0.2f);
                CombatMotion.HitReaction(enemy.transform, player.transform.position, 0.8f);
            }
            Vfx.Ring(player.transform.position + Vector3.up * 0.4f, perfect ? MexicaPalette.Turquoise : MexicaPalette.Bone, 0.2f, perfect ? 1.3f : 0.9f, 0.35f);
            if (!interrupt) Vfx.Afterimage(context.PlayerSprite, perfect ? MexicaPalette.Turquoise : MexicaPalette.Bone, 0.3f);

            if (!interrupt)
                yield return CombatMotion.Step(player.transform, new Vector3(-dodgeStepDistance, 0.4f, 0f), 0.12f);
            else
                yield return new WaitForSeconds(0.2f);

            if (enemy.Health.IsDead) yield break;

            // Every successful defence answers with an automatic counter-attack; PERFECT hits harder and also
            // gives stamina and Tonalli.
            if (perfect)
            {
                player.AddStamina(player.Stats.perfectCounterStaminaBonus);
                player.AddConcentration(player.Stats.concentrationPerPerfect);
            }
            float damage = player.AttackDamage(player.Stats.counterAttackMultiplier * (perfect ? 1f : goodCounterFraction));
            GameAudio.PlaySfx("swing");
            var weapon = MacuahuitlRig.Active(player);
            if (weapon != null) context.Run(weapon.RaiseTo(MacuahuitlTechnique.Backhand.windAngle, 0.08f));
            yield return CombatMotion.Lunge(player.transform, enemy.transform.position, context.StopDistance(enemy), 0.12f,
                () =>
                {
                    Vfx.Slash(enemy.transform.position + Vector3.up * 0.5f, 1f, perfect ? MexicaPalette.Turquoise : MexicaPalette.Bone, perfect ? 1.3f : 1f, MacuahuitlTechnique.Backhand.slashAngle);
                    context.DamageEnemy(enemy, damage, critical: perfect, label: "COUNTER");
                },
                weapon != null ? impact => weapon.Strike(MacuahuitlTechnique.Backhand, impact) : null);
        }

        IEnumerator Land(CombatContext context, CombatEnemy enemy, EnemyAttack attack)
        {
            float damage = Mathf.Round(enemy.AttackPower * attack.damageMultiplier);
            if (damage <= 0f)
            {
                // Pure support ability (howl, guard...): no hit, only its effect.
                yield return new WaitForSeconds(0.4f);
            }
            else if (attack.lungeAtTarget)
            {
                yield return CombatMotion.Lunge(enemy.transform, context.Player.transform.position,
                    context.StopDistance(enemy), enemyLungeTime, () => context.DamagePlayer(damage, enemy.Data));
            }
            else
            {
                // Ranged attack or spell: a projectile flies to the player.
                yield return Vfx.Projectile(enemy.transform.position + Vector3.up * 0.5f,
                    context.Player.transform.position + Vector3.up * 0.45f, telegraphColor, 0.3f);
                context.DamagePlayer(damage, enemy.Data);
                yield return new WaitForSeconds(0.3f);
            }
        }

        static void ApplyEffect(CombatContext context, CombatEnemy enemy, EnemyAttack attack)
        {
            var player = context.Player;
            Vector3 above = Vector3.up * 1.3f;
            switch (attack.effect)
            {
                case AttackEffect.DrainStamina:
                    player.AddStamina(-attack.effectAmount);
                    CombatFeed.ShowPopup(player.transform.position + above, "-STAMINA", MexicaPalette.Ochre);
                    break;
                case AttackEffect.HealSelf:
                    if (enemy.Health.IsDead) break;
                    enemy.HealFraction(attack.effectAmount);
                    CombatFeed.ShowPopup(enemy.transform.position + above, "HEAL", MexicaPalette.Jade);
                    break;
                case AttackEffect.Weaken:
                    player.Weaken(attack.effectAmount);
                    CombatFeed.ShowPopup(player.transform.position + above, "WEAKENED", MexicaPalette.Ash);
                    break;
                case AttackEffect.EmpowerSelf:
                    if (enemy.Health.IsDead) break;
                    enemy.Empower(attack.effectAmount);
                    CombatFeed.ShowPopup(enemy.transform.position + above, "POWER UP", MexicaPalette.Cinnabar);
                    break;
                case AttackEffect.LifeSteal:
                    if (enemy.Health.IsDead) break;
                    float stolen = Mathf.Round(enemy.AttackPower * attack.damageMultiplier * attack.effectAmount);
                    enemy.Health.Heal(stolen);
                    CombatFeed.ShowPopup(enemy.transform.position + above, "+" + stolen, MexicaPalette.Jade);
                    break;
                case AttackEffect.Stun:
                    player.Stun();
                    CombatFeed.ShowPopup(player.transform.position + above, "STUNNED", MexicaPalette.AgedGold);
                    break;
                case AttackEffect.Guard:
                    if (enemy.Health.IsDead) break;
                    enemy.SetGuard(attack.effectAmount);
                    CombatFeed.ShowPopup(enemy.transform.position + above, "GUARD", MexicaPalette.Bone);
                    break;
            }
        }
    }
}
```

---

## Menús y pantallas

### MenuWidgets.cs

Ruta: `Assets/TD_Game/Scripts/UI/MenuWidgets.cs` · 176 líneas

Caja de herramientas para construir menús por código: canvas 1920x1080, botones con marco azteca, etiquetas, paneles y lectura de teclas (cualquier tecla, cancelar, pausa). **Se conecta con:** lo usan TODOS los menús de esta lista, además de la tienda, el mapa y los diálogos. AztecFrames da el estilo y MenuSelectSound el sonido.

```csharp
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// Small helpers to build the menus by code (adapted from Mictlán v5's UIKit), with the Mexica palette.
    /// Used by BootScreens, TitleMenu and PauseScreen.
    /// </summary>
    public static class MenuWidgets
    {
        static Font font;
        static bool aztecFrames;
        public static Font Font => font != null ? font : (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        /// <summary>Menu style: the artistic font (Pirata One) and the Aztec frames of Mictlán v5. Call before building.</summary>
        public static void Use(Font menuFont, bool useAztecFrames)
        {
            if (menuFont != null) font = menuFont;
            aztecFrames = useAztecFrames;
        }

        /// <summary>A full-screen canvas (1920x1080 reference) under the parent.</summary>
        public static RectTransform Screen(string name, int sortingOrder, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            EnsureEventSystem();
            return (RectTransform)go.transform;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            return r;
        }

        public static void Center(RectTransform r, Vector2 position, Vector2 size)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = position;
            r.sizeDelta = size;
        }

        public static Image Image(string name, Transform parent, Color color)
        {
            var image = Rect(name, parent).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static RawImage Picture(string name, Transform parent, Texture texture)
        {
            var raw = Rect(name, parent).gameObject.AddComponent<RawImage>();
            raw.texture = texture;
            raw.raycastTarget = false;
            return raw;
        }

        public static Text Label(string name, Transform parent, string text, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var t = Rect(name, parent).gameObject.AddComponent<Text>();
            t.font = Font; t.text = text; t.fontSize = size; t.color = color; t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Dark stone plate with a gold edge; red when selected. Plays the menu sounds.</summary>
        public static Button Button(Transform parent, string text, Vector2 position, Vector2 size, UnityAction onClick, int fontSize = 30)
        {
            var r = Rect("Button_" + text, parent);
            Center(r, position, size);
            var image = r.gameObject.AddComponent<Image>();
            var button = r.gameObject.AddComponent<Button>();
            if (aztecFrames)
            {
                // Aztec frame of Mictlán v5: one picture per state (normal, highlight, selected, pressed, disabled).
                AztecFrames.StyleButton(button, image, size.x, size.y);
            }
            else
            {
                image.color = Color.white;
                var outline = r.gameObject.AddComponent<Outline>();
                outline.effectColor = MexicaPalette.AgedGold;
                outline.effectDistance = new Vector2(2f, -2f);
                button.targetGraphic = image;
                var colors = button.colors;
                colors.normalColor = MexicaPalette.ObsidianPlate;
                colors.highlightedColor = MexicaPalette.Cinnabar;
                colors.selectedColor = MexicaPalette.Cinnabar;
                colors.pressedColor = MexicaPalette.Tezontle;
                colors.disabledColor = MexicaPalette.WithAlpha(MexicaPalette.ObsidianPlate, 0.5f);
                button.colors = colors;
            }
            button.onClick.AddListener(() => GameAudio.PlaySfx("ui_select", 1f, 0f));
            button.onClick.AddListener(onClick);
            r.gameObject.AddComponent<MenuSelectSound>();

            var label = Label("Label", r, text, fontSize, MexicaPalette.Bone);
            Stretch(label.rectTransform);
            return button;
        }

        /// <summary>A panel with the Aztec frame (credits, shop, prologue captions).</summary>
        public static Image Panel(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var image = Image(name, parent, Color.white);
            Center(image.rectTransform, position, size);
            AztecFrames.StylePanel(image, size.x, size.y);
            return image;
        }

        public static void SetLabel(Button button, string text) => button.GetComponentInChildren<Text>().text = text;

        public static void Select(GameObject go)
        {
            if (EventSystem.current == null || go == null) return;
            MenuSelectSound.MuteFor(0.1f);
            EventSystem.current.SetSelectedGameObject(go);
        }

        /// <summary>Any key, click or gamepad button this frame.</summary>
        public static bool AnyPress()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            var pad = Gamepad.current;
            return (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
                || (mouse != null && mouse.leftButton.wasPressedThisFrame)
                || (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame));
        }

        public static bool CancelPressed()
        {
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            return (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || (pad != null && pad.buttonEast.wasPressedThisFrame);
        }

        public static bool PausePressed()
        {
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            return (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || (pad != null && pad.startButton.wasPressedThisFrame);
        }
    }
}
```
### SceneFader.cs

Ruta: `Assets/TD_Game/Scripts/Managers/SceneFader.cs` · 90 líneas

Cambia de escena con un fundido a negro y guarda los nombres de las escenas (SceneIds). **Se conecta con:** BootScreens → menú, TitleMenu → prólogo / mapa, DefeatScreen y CreditsScreen → menú.

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>Names of the scenes of the new game (they must be in File > Build Profiles > Scene List).</summary>
    public static class SceneIds
    {
        // "TD_" so they never get confused with the Boot / MainMenu scenes of the previous game.
        public const string Boot = "TD_Boot";
        public const string MainMenu = "TD_MainMenu";
        public const string Prologue = "TD_Prologue";
        public const string MainMap = "MainMap_CSV";   // the team's CSV map (2026-11-24). Alex's map is "MainMap".
    }

    /// <summary>
    /// Changes scene with a fade to black (adapted from Mictlán v5's SceneLoader). Usage: SceneFader.Load(SceneIds.MainMap);
    /// It creates itself the first time and survives scene changes.
    /// </summary>
    public class SceneFader : MonoBehaviour
    {
        const float FadeSeconds = 0.4f;

        static SceneFader instance;
        CanvasGroup fade;
        bool busy;

        public static void Load(string sceneName)
        {
            if (instance == null) Create();
            if (!instance.busy) instance.StartCoroutine(instance.Go(sceneName));
        }

        static void Create()
        {
            var go = new GameObject("SceneFader");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SceneFader>();

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            instance.fade = go.AddComponent<CanvasGroup>();
            instance.fade.alpha = 0f;
            instance.fade.blocksRaycasts = false;

            var black = new GameObject("Black", typeof(RectTransform)).AddComponent<Image>();
            black.transform.SetParent(go.transform, false);
            black.color = Color.black;
            var r = black.rectTransform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        IEnumerator Go(string sceneName)
        {
            busy = true;
            fade.blocksRaycasts = true;
            yield return Fade(1f);

            Time.timeScale = 1f;
            GameState.SetMode(GameMode.Exploration);
            var op = SceneManager.LoadSceneAsync(sceneName);
            if (op == null) Debug.LogError($"[{nameof(SceneFader)}] Cannot load '{sceneName}'. Is it in the Build Profiles scene list?");
            else while (!op.isDone) yield return null;
            yield return null;

            yield return Fade(0f);
            fade.blocksRaycasts = false;
            busy = false;
        }

        IEnumerator Fade(float target)
        {
            float start = fade.alpha;
            for (float t = 0f; t < FadeSeconds; t += Time.unscaledDeltaTime)
            {
                fade.alpha = Mathf.Lerp(start, target, t / FadeSeconds);
                yield return null;
            }
            fade.alpha = target;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;
    }
}
```
### BootScreens.cs

Ruta: `Assets/TD_Game/Scripts/UI/BootScreens.cs` · 191 líneas

Primeras pantallas: logo del estudio, clasificación Rated M y descripción del juego. **Se conecta con:** MenuWidgets y SceneFader (al terminar carga TD_MainMenu); GameAudio para el sonido del logo.

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// First screens of the game (adapted from Mictlán v5's BootSplash):
    /// studio logo -> age rating -> description of the game -> main menu.
    /// Any key, click or gamepad button skips the current screen (after half a second).
    /// Everything (logo, rating image, texts, times) is edited in the Inspector of the TD_Boot scene.
    /// </summary>
    public class BootScreens : MonoBehaviour
    {
        [SerializeField] Font menuFont;          // Pirata One

        [Header("1) Studio logo")]
        [SerializeField] Texture2D studioLogo;
        [SerializeField] string studioName = "MICTA FORGE";
        [SerializeField, Min(0.5f)] float logoSeconds = 3f;

        [Header("2) Age rating (optional: assign the official image)")]
        [SerializeField] Texture2D ratingImage;
        [SerializeField] string ratingLetter = "M";
        [SerializeField] string ratingLabel = "MATURE 17+";
        [SerializeField, TextArea(2, 6)] string contentDescriptors = "Blood and Gore\nIntense Violence\nMature Themes";
        [SerializeField, Min(0.5f)] float ratingSeconds = 3.5f;

        [Header("3) Description of the game")]
        [SerializeField] string descriptionTitle = "MICTLÁN";
        [SerializeField, TextArea(4, 10)] string description =
            "Tlahuelli, a Mexica warrior, falls into Mictlán, the land of the dead.\n" +
            "Cross its dark regions, defeat wolves, witches and giants in turn-based battles\n" +
            "and face Quetzalcóatl to find the way back.\n\n" +
            "Mictlán is an original work inspired by Mexica mythology.\n" +
            "Characters and events shown here are fictional.";
        [SerializeField, Min(0.5f)] float descriptionSeconds = 6f;

        [Header("Next scene")]
        [SerializeField] string nextScene = SceneIds.MainMenu;

        class Slide { public CanvasGroup group; public float seconds; }
        readonly List<Slide> slides = new List<Slide>();

        void Start()
        {
            Time.timeScale = 1f;
            MenuWidgets.Use(menuFont, false);
            var root = MenuWidgets.Screen("BootCanvas", 10, transform);
            MenuWidgets.Stretch(MenuWidgets.Image("Black", root, Color.black).rectTransform);
            BuildLogo(root);
            BuildRating(root);
            BuildDescription(root);
            StartCoroutine(Run());
        }

        CanvasGroup NewSlide(RectTransform parent, string name, float seconds)
        {
            var r = MenuWidgets.Stretch(MenuWidgets.Rect(name, parent));
            var group = r.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            slides.Add(new Slide { group = group, seconds = seconds });
            return group;
        }

        void BuildLogo(RectTransform parent)
        {
            var slide = NewSlide(parent, "Slide_Logo", logoSeconds).transform;
            if (studioLogo != null)
            {
                var logo = MenuWidgets.Picture("Logo", slide, studioLogo);
                float h = 560f;
                MenuWidgets.Center(logo.rectTransform, Vector2.zero, new Vector2(h * studioLogo.width / Mathf.Max(1, studioLogo.height), h));
            }
            else
            {
                var name = MenuWidgets.Label("StudioName", slide, studioName, 90, MexicaPalette.Cinnabar);
                name.fontStyle = FontStyle.Bold;
                MenuWidgets.Center(name.rectTransform, Vector2.zero, new Vector2(1200, 140));
            }
        }

        void BuildRating(RectTransform parent)
        {
            var slide = NewSlide(parent, "Slide_Rating", ratingSeconds).transform;
            if (ratingImage != null)
            {
                var image = MenuWidgets.Picture("Rating", slide, ratingImage);
                float h = 420f;
                MenuWidgets.Center(image.rectTransform, Vector2.zero, new Vector2(h * ratingImage.width / Mathf.Max(1, ratingImage.height), h));
                return;
            }
            // Rating label like the one printed on the back of a game box: a white frame with the big letter,
            // the age bar, and next to it a framed list of content descriptors. Plain bold font (like the real ones).
            var plain = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var label = MenuWidgets.Rect("RatingLabel", slide);
            // Every text has its own box INSIDE its frame (with a margin), and shrinks by itself if it does not fit.
            MenuWidgets.Center(label, new Vector2(0, 40), new Vector2(660, 380));

            var box = Frame("LetterBox", label, new Vector2(-180, 0), new Vector2(260, 370), 8);
            var letter = Plain(MenuWidgets.Label("Letter", box, ratingLetter, 210, Color.white), plain);
            MenuWidgets.Center(letter.rectTransform, new Vector2(0, 60), new Vector2(230, 220));
            var bar = MenuWidgets.Image("AgeBar", box, Color.white);
            MenuWidgets.Center(bar.rectTransform, new Vector2(0, -92), new Vector2(226, 58));
            var age = Plain(MenuWidgets.Label("Age", bar.transform, ratingLabel, 30, Color.black), plain);
            MenuWidgets.Stretch(age.rectTransform);
            age.rectTransform.offsetMin = new Vector2(8, 4);
            age.rectTransform.offsetMax = new Vector2(-8, -4);
            var side = Plain(MenuWidgets.Label("Side", box, "CONTENT RATED", 16, Color.white), plain);
            MenuWidgets.Center(side.rectTransform, new Vector2(0, -145), new Vector2(226, 26));

            var info = Frame("Descriptors", label, new Vector2(140, 0), new Vector2(360, 370), 4);
            var descriptors = Plain(MenuWidgets.Label("Text", info, contentDescriptors, 28, Color.white, TextAnchor.MiddleLeft), plain);
            MenuWidgets.Stretch(descriptors.rectTransform);
            descriptors.rectTransform.offsetMin = new Vector2(24, 24);
            descriptors.rectTransform.offsetMax = new Vector2(-20, -24);
            descriptors.lineSpacing = 1.15f;

            var note = Fit(MenuWidgets.Label("Note", slide, "This game contains intense violence, blood and gore.", 26, MexicaPalette.Ash));
            MenuWidgets.Center(note.rectTransform, new Vector2(0, -230), new Vector2(1300, 60));
        }

        /// <summary>A white frame with a black inside (returns the inside).</summary>
        static RectTransform Frame(string name, RectTransform parent, Vector2 position, Vector2 size, float border)
        {
            var outer = MenuWidgets.Image(name, parent, Color.white);
            MenuWidgets.Center(outer.rectTransform, position, size);
            var inner = MenuWidgets.Image("Inside", outer.transform, Color.black);
            MenuWidgets.Stretch(inner.rectTransform);
            inner.rectTransform.offsetMin = new Vector2(border, border);
            inner.rectTransform.offsetMax = new Vector2(-border, -border);
            return inner.rectTransform;
        }

        static Text Plain(Text text, Font font)
        {
            text.font = font;
            text.fontStyle = FontStyle.Bold;
            return Fit(text);
        }

        /// <summary>Best Fit (Unity UI): the text shrinks by itself if it does not fit in its box, on any screen size.</summary>
        static Text Fit(Text text)
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 8;
            text.resizeTextMaxSize = text.fontSize;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        void BuildDescription(RectTransform parent)
        {
            var slide = NewSlide(parent, "Slide_Description", descriptionSeconds).transform;
            var title = MenuWidgets.Label("Title", slide, descriptionTitle, 80, MexicaPalette.AgedGold);
            title.fontStyle = FontStyle.Bold;
            MenuWidgets.Center(title.rectTransform, new Vector2(0, 200), new Vector2(1200, 110));
            var body = Fit(MenuWidgets.Label("Body", slide, description, 32, MexicaPalette.Bone));
            MenuWidgets.Center(body.rectTransform, new Vector2(0, -40), new Vector2(1500, 360));
        }

        IEnumerator Run()
        {
            yield return null;
            GameAudio.PlaySfx("logo", 1f, 0f);
            foreach (var slide in slides)
            {
                yield return Fade(slide.group, 1f, 0.6f);
                for (float t = 0f; t < slide.seconds; t += Time.unscaledDeltaTime)
                {
                    if (t > 0.5f && MenuWidgets.AnyPress()) break;
                    yield return null;
                }
                yield return Fade(slide.group, 0f, 0.5f);
            }
            SceneFader.Load(nextScene);
        }

        static IEnumerator Fade(CanvasGroup group, float target, float seconds)
        {
            float start = group.alpha;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(start, target, t / seconds);
                yield return null;
            }
            group.alpha = target;
        }
    }
}
```
### TitleMenu.cs

Ruta: `Assets/TD_Game/Scripts/UI/TitleMenu.cs` · 343 líneas

Menú principal: PRESS START, Start Game, Continue, Options (volumen, iconos de botones), Credits y Exit. Muestra la versión v0.9. **Se conecta con:** GameSave (NewGame / Continue), ContextTutorial (borra pistas al empezar), GameAudio (volúmenes), InputPrompts (opción de iconos), SceneFader.

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// Title screen and main menu (adapted from Mictlán v5's MainMenu), with the same background and music.
    /// Stage 1: "PRESS ENTER / START".  Stage 2: Start Game, Continue, Options, Credits, Exit.
    /// Keyboard (arrows + Enter), mouse and gamepad. The song comes from GameAudio (Menu track).
    /// </summary>
    public class TitleMenu : MonoBehaviour
    {
        [Header("Style")]
        [SerializeField] Font menuFont;          // Pirata One
        [Tooltip("Soft round light used for the torches, the moon and the fog of the background.")]
        [SerializeField] Sprite glow;

        [Header("Art")]
        [SerializeField] Texture2D background;
        [Tooltip("Optional logo image; empty = the title is written with text.")]
        [SerializeField] Texture2D gameLogo;
        [SerializeField] string title = "MICTLÁN";
        [SerializeField] string studio = "Micta Forge";
        [SerializeField] string version = "v0.9";

        [Header("Scenes")]
        [SerializeField] string gameScene = SceneIds.MainMap;
        [Tooltip("Story comic shown before a NEW game.")]
        [SerializeField] string prologueScene = SceneIds.Prologue;

        [Header("Credits")]
        // Fixed in code (not in the scene) so an old saved text can never replace it.
        const string TeamCredits =
            "MICTLÁN\nA game by Micta Forge\n\n" +
            "Antonio Enríquez Velasco\n" +
            "Alejandro Cornejo Pérez\n" +
            "Adrián Gallardo Medina\n" +
            "Jetzuvely del Carmen González Gutiérrez\n\n" +
            "<size=26>With the help of Claude, an AI assistant by Anthropic</size>";

        enum Stage { Title, Menu, Options, Credits }
        Stage stage = Stage.Title;

        GameObject menuPanel, optionsPanel, creditsPanel;
        Text pressStart;
        RawImage backgroundImage;
        Button startButton, continueButton, masterButton, musicButton, sfxButton, fullscreenButton, goreButton, controllerButton;

        void Start()
        {
            Time.timeScale = 1f;
            GameState.SetMode(GameMode.Exploration);
            GameAudio.PlayMusic(MusicTrack.Menu);
            MenuWidgets.Use(menuFont, true);
            Build();
        }

        void Update()
        {
            if (backgroundImage != null)
            {
                // The background breathes and drifts slowly, like a camera floating over Teotihuacan.
                float t = Time.unscaledTime;
                backgroundImage.rectTransform.localScale = Vector3.one * (1.08f + 0.04f * Mathf.Sin(t * 0.25f));
                backgroundImage.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(t * 0.13f) * 40f, Mathf.Cos(t * 0.17f) * 20f);
            }
            MoveEmbers();
            AnimateBackground();

            switch (stage)
            {
                case Stage.Title:
                    pressStart.color = MexicaPalette.WithAlpha(Color.white, 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.2f)));
                    if (MenuWidgets.AnyPress()) { GameAudio.PlaySfx("ui_select", 1f, 0f); ShowMenu(); }
                    break;
                case Stage.Options:
                case Stage.Credits:
                    if (MenuWidgets.CancelPressed()) { GameAudio.PlaySfx("ui_back", 1f, 0f); ShowMenu(); }
                    break;
            }
        }

        // ---------------- Building ----------------

        void Build()
        {
            var root = MenuWidgets.Screen("MenuCanvas", 10, transform);
            MenuWidgets.Stretch(MenuWidgets.Image("Black", root, MexicaPalette.Obsidian).rectTransform);
            if (background != null)
            {
                backgroundImage = MenuWidgets.Picture("Background", root, background);
                MenuWidgets.Stretch(backgroundImage.rectTransform);
                MakeBackgroundAlive(backgroundImage.rectTransform);
            }
            MenuWidgets.Stretch(MenuWidgets.Image("Shade", root, new Color(0f, 0f, 0f, background != null ? 0.25f : 0.5f)).rectTransform);
            MakeEmbers(root);

            if (gameLogo != null)
            {
                var logo = MenuWidgets.Picture("Logo", root, gameLogo);
                float h = 260f;
                MenuWidgets.Center(logo.rectTransform, new Vector2(0, 230), new Vector2(h * gameLogo.width / Mathf.Max(1, gameLogo.height), h));
            }
            else
            {
                var titleText = MenuWidgets.Label("Title", root, title, 150, MexicaPalette.Cinnabar);
                titleText.fontStyle = FontStyle.Bold;
                var outline = titleText.gameObject.AddComponent<Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(4f, -4f);
                MenuWidgets.Center(titleText.rectTransform, new Vector2(0, 230), new Vector2(1400, 200));
            }

            var studioText = MenuWidgets.Label("Studio", root, studio, 24, MexicaPalette.WithAlpha(Color.white, 0.6f), TextAnchor.LowerLeft);
            Corner(studioText.rectTransform, new Vector2(0, 0), new Vector2(24, 16));
            var versionText = MenuWidgets.Label("Version", root, version, 24, MexicaPalette.WithAlpha(Color.white, 0.6f), TextAnchor.LowerRight);
            Corner(versionText.rectTransform, new Vector2(1, 0), new Vector2(-24, 16));

            pressStart = MenuWidgets.Label("PressStart", root, "PRESS ENTER / START", 40, Color.white);
            MenuWidgets.Center(pressStart.rectTransform, new Vector2(0, -230), new Vector2(1000, 60));

            BuildMenu(root);
            BuildOptions(root);
            BuildCredits(root);
            menuPanel.SetActive(false);
            optionsPanel.SetActive(false);
            creditsPanel.SetActive(false);
        }

        static void Corner(RectTransform r, Vector2 anchor, Vector2 offset)
        {
            r.anchorMin = r.anchorMax = r.pivot = anchor;
            r.anchoredPosition = offset;
            r.sizeDelta = new Vector2(500, 40);
        }

        void BuildMenu(RectTransform root)
        {
            menuPanel = MenuWidgets.Stretch(MenuWidgets.Rect("MenuPanel", root)).gameObject;
            var t = menuPanel.transform;
            Vector2 size = new Vector2(440, 64);
            float y = 20f, step = -80f;
            startButton = MenuWidgets.Button(t, "Start Game", new Vector2(0, y), size, OnStartGame);
            continueButton = MenuWidgets.Button(t, "Continue", new Vector2(0, y + step), size, OnContinue);
            MenuWidgets.Button(t, "Options", new Vector2(0, y + step * 2), size, () => ShowPanel(Stage.Options));
            MenuWidgets.Button(t, "Credits", new Vector2(0, y + step * 3), size, () => ShowPanel(Stage.Credits));
            MenuWidgets.Button(t, "Exit Game", new Vector2(0, y + step * 4), size, OnQuit);
            continueButton.interactable = GameSave.HasSave;
        }

        void BuildOptions(RectTransform root)
        {
            optionsPanel = MenuWidgets.Stretch(MenuWidgets.Rect("OptionsPanel", root)).gameObject;
            var t = optionsPanel.transform;
            var head = MenuWidgets.Label("Head", t, "OPTIONS", 52, MexicaPalette.AgedGold);
            MenuWidgets.Center(head.rectTransform, new Vector2(0, 60), new Vector2(700, 70));
            Vector2 size = new Vector2(480, 58);
            masterButton = MenuWidgets.Button(t, "", new Vector2(0, -20), size, () => { GameAudio.MasterVolume = Cycle(GameAudio.MasterVolume); Refresh(); }, 28);
            musicButton = MenuWidgets.Button(t, "", new Vector2(0, -90), size, () => { GameAudio.MusicVolume = Cycle(GameAudio.MusicVolume); Refresh(); }, 28);
            sfxButton = MenuWidgets.Button(t, "", new Vector2(0, -160), size, () => { GameAudio.SfxVolume = Cycle(GameAudio.SfxVolume); Refresh(); }, 28);
            fullscreenButton = MenuWidgets.Button(t, "", new Vector2(0, -230), size, ToggleFullscreen, 28);
            goreButton = MenuWidgets.Button(t, "", new Vector2(0, -300), size, () => { GoreEffects.Enabled = !GoreEffects.Enabled; Refresh(); }, 28);
            controllerButton = MenuWidgets.Button(t, "", new Vector2(0, -370), size, () => { InputPrompts.CycleMode(); Refresh(); }, 28);
            MenuWidgets.Button(t, "Back", new Vector2(0, -440), size, ShowMenu, 28);
            Refresh();
        }

        void BuildCredits(RectTransform root)
        {
            creditsPanel = MenuWidgets.Stretch(MenuWidgets.Rect("CreditsPanel", root)).gameObject;
            MenuWidgets.Panel("Plate", creditsPanel.transform, new Vector2(0, -20), new Vector2(1100, 480));
            var body = MenuWidgets.Label("Body", creditsPanel.transform, TeamCredits, 34, MexicaPalette.Bone);
            MenuWidgets.Center(body.rectTransform, new Vector2(0, 10), new Vector2(1050, 380));
            MenuWidgets.Button(creditsPanel.transform, "Back", new Vector2(0, -300), new Vector2(440, 58), ShowMenu, 28);
        }

        // ---------------- A living background: torches, moon, stars and fog ----------------
        // Everything is a child of the background picture, so it moves with it. Positions are 0..1 of the picture.

        Image[] torchLights, stars;
        Image moonHalo;
        RectTransform[] fog;

        Image Light(RectTransform parent, string name, Vector2 where, float size, Color color)
        {
            var image = MenuWidgets.Image(name, parent, color);
            image.sprite = glow;
            var r = image.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(where.x, 1f - where.y);
            r.sizeDelta = Vector2.one * size;
            return image;
        }

        void MakeBackgroundAlive(RectTransform bg)
        {
            if (glow == null) return;
            torchLights = new[]
            {
                Light(bg, "TorchLight", new Vector2(0.347f, 0.36f), 90f, new Color(1f, 0.6f, 0.2f, 0.7f)),
                Light(bg, "TorchLight", new Vector2(0.370f, 0.36f), 90f, new Color(1f, 0.6f, 0.2f, 0.7f)),
            };
            moonHalo = Light(bg, "MoonHalo", new Vector2(0.743f, 0.216f), 380f, new Color(1f, 0.95f, 0.8f, 0.25f));
            stars = new Image[30];
            for (int i = 0; i < stars.Length; i++)
                stars[i] = Light(bg, "Star", new Vector2(Random.value, Random.Range(0.02f, 0.4f)), Random.Range(10f, 18f), Color.white);
            fog = new RectTransform[3];
            for (int i = 0; i < fog.Length; i++)
            {
                var band = Light(bg, "Fog", new Vector2(i / 3f, 0.82f + i * 0.05f), 1f, new Color(0.7f, 0.6f, 0.75f, 0.12f));
                band.rectTransform.sizeDelta = new Vector2(1400f, 160f);
                fog[i] = band.rectTransform;
            }
        }

        void AnimateBackground()
        {
            if (torchLights == null) return;
            float t = Time.unscaledTime;
            for (int i = 0; i < torchLights.Length; i++)   // fire flickers fast and irregularly
            {
                float flicker = 0.75f + 0.15f * Mathf.Sin(t * 13f + i * 2f) + 0.1f * Mathf.Sin(t * 29f + i);
                torchLights[i].rectTransform.localScale = Vector3.one * flicker;
            }
            moonHalo.rectTransform.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(t * 0.8f));   // the moon breathes
            for (int i = 0; i < stars.Length; i++)   // stars twinkle
                stars[i].color = new Color(1f, 1f, 1f, 0.15f + 0.6f * Mathf.Abs(Mathf.Sin(t * (0.7f + i % 5 * 0.3f) + i)));
            for (int i = 0; i < fog.Length; i++)    // fog drifts slowly sideways and wraps around
            {
                float x = Mathf.Repeat(t * (12f + i * 6f) + i * 600f, 2800f) - 1400f;
                fog[i].anchoredPosition = new Vector2(x, 0f);
            }
        }

        // ---------------- Floating embers (simple: small squares that rise and come back to the bottom) ----------------

        RectTransform[] embers;

        void MakeEmbers(RectTransform root)
        {
            embers = new RectTransform[40];
            for (int i = 0; i < embers.Length; i++)
            {
                var ember = MenuWidgets.Image("Ember", root, new Color(1f, Random.Range(0.35f, 0.65f), 0.15f, Random.Range(0.4f, 0.9f)));
                MenuWidgets.Center(ember.rectTransform, new Vector2(Random.Range(-960f, 960f), Random.Range(-540f, 540f)), Vector2.one * Random.Range(4f, 9f));
                embers[i] = ember.rectTransform;
            }
        }

        void MoveEmbers()
        {
            if (embers == null) return;
            for (int i = 0; i < embers.Length; i++)
            {
                Vector2 p = embers[i].anchoredPosition;
                p.y += (30f + i % 5 * 12f) * Time.unscaledDeltaTime;                 // rise
                p.x += Mathf.Sin(Time.unscaledTime * 1.3f + i) * 20f * Time.unscaledDeltaTime;   // sway
                if (p.y > 560f) p = new Vector2(Random.Range(-960f, 960f), -560f);   // back to the bottom
                embers[i].anchoredPosition = p;
            }
        }

        // ---------------- Navigation ----------------

        void ShowMenu()
        {
            stage = Stage.Menu;
            pressStart.gameObject.SetActive(false);
            optionsPanel.SetActive(false);
            creditsPanel.SetActive(false);
            menuPanel.SetActive(true);
            StartCoroutine(SelectNextFrame(startButton.gameObject));
        }

        void ShowPanel(Stage panelStage)
        {
            stage = panelStage;
            menuPanel.SetActive(false);
            var panel = panelStage == Stage.Options ? optionsPanel : creditsPanel;
            panel.SetActive(true);
            StartCoroutine(SelectNextFrame(panel.GetComponentInChildren<Button>().gameObject));
        }

        static IEnumerator SelectNextFrame(GameObject go)
        {
            yield return null;
            MenuWidgets.Select(go);
        }

        // ---------------- Actions ----------------

        void OnStartGame()
        {
            GameSave.NewGame();
            ContextTutorial.ResetAll();
            // New game: first the story of Tlahuelli, then the map.
            SceneFader.Load(Application.CanStreamedLevelBeLoaded(prologueScene) ? prologueScene : gameScene);
        }

        void OnContinue()
        {
            GameSave.Continue();
            var data = GameSave.Read();
            string scene = data != null && !string.IsNullOrEmpty(data.scene) && Application.CanStreamedLevelBeLoaded(data.scene)
                ? data.scene : gameScene;
            SceneFader.Load(scene);
        }

        static void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void ToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
            StartCoroutine(RefreshNextFrame());
        }

        IEnumerator RefreshNextFrame()
        {
            yield return null;
            Refresh();
        }

        /// <summary>0 -> 25 -> 50 -> 75 -> 100 -> 0 %.</summary>
        static float Cycle(float value) => value >= 0.99f ? 0f : Mathf.Min(1f, Mathf.Round(value * 4f + 1f) / 4f);

        void Refresh()
        {
            MenuWidgets.SetLabel(masterButton, $"Master Volume: {Mathf.RoundToInt(GameAudio.MasterVolume * 100f)}%");
            MenuWidgets.SetLabel(musicButton, $"Music Volume: {Mathf.RoundToInt(GameAudio.MusicVolume * 100f)}%");
            MenuWidgets.SetLabel(sfxButton, $"Effects Volume: {Mathf.RoundToInt(GameAudio.SfxVolume * 100f)}%");
            MenuWidgets.SetLabel(fullscreenButton, "Fullscreen: " + (Screen.fullScreen ? "On" : "Off"));
            MenuWidgets.SetLabel(goreButton, "Gore (17+): " + (GoreEffects.Enabled ? "On" : "Off"));
            MenuWidgets.SetLabel(controllerButton, "Button icons: " + InputPrompts.ModeLabel);   // Auto follows the last device used
        }
    }
}
```
### PauseScreen.cs

Ruta: `Assets/TD_Game/Scripts/UI/PauseScreen.cs` · 98 líneas

Pausa con ESC / Start dentro del juego: reanudar, volumen, salir al menú. **Se conecta con:** GameState (modo Paused), GameAudio, SceneFader, MenuWidgets.

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// Pause menu (adapted from Mictlán v5's PauseMenu). ESC / Start during exploration.
    /// Resume, music and effects volume, Main Menu (the game is saved after every victory).
    /// Put one in each level scene.
    /// </summary>
    public class PauseScreen : MonoBehaviour
    {
        [SerializeField] string mainMenuScene = SceneIds.MainMenu;
        [SerializeField] Font menuFont;          // Pirata One

        GameObject panel;
        Button resumeButton, musicButton, sfxButton, controllerButton;
        bool paused;

        void Start()
        {
            MenuWidgets.Use(menuFont, true);
            var root = MenuWidgets.Screen("PauseCanvas", 50, transform);
            panel = MenuWidgets.Stretch(MenuWidgets.Rect("PausePanel", root)).gameObject;
            var t = panel.transform;
            MenuWidgets.Stretch(MenuWidgets.Image("Shade", t, new Color(0f, 0f, 0f, 0.7f)).rectTransform);
            var head = MenuWidgets.Label("Head", t, "PAUSED", 70, MexicaPalette.AgedGold);
            head.fontStyle = FontStyle.Bold;
            MenuWidgets.Center(head.rectTransform, new Vector2(0, 200), new Vector2(800, 100));

            Vector2 size = new Vector2(460, 60);
            resumeButton = MenuWidgets.Button(t, "Resume", new Vector2(0, 70), size, Resume);
            musicButton = MenuWidgets.Button(t, "", new Vector2(0, -10), size, () => { GameAudio.MusicVolume = Cycle(GameAudio.MusicVolume); Refresh(); }, 28);
            sfxButton = MenuWidgets.Button(t, "", new Vector2(0, -90), size, () => { GameAudio.SfxVolume = Cycle(GameAudio.SfxVolume); Refresh(); }, 28);
            controllerButton = MenuWidgets.Button(t, "", new Vector2(0, -170), size, () => { InputPrompts.CycleMode(); Refresh(); }, 26);
            MenuWidgets.Button(t, "Main Menu", new Vector2(0, -250), size, ToMainMenu);
            var note = MenuWidgets.Label("Note", t, "Your progress is saved after every victory.", 24, MexicaPalette.Ash);
            MenuWidgets.Center(note.rectTransform, new Vector2(0, -330), new Vector2(900, 40));
            Refresh();
            panel.SetActive(false);
        }

        void Update()
        {
            if (!MenuWidgets.PausePressed()) return;
            if (paused) Resume();
            else if (GameState.IsExploring) Pause();
        }

        void Pause()
        {
            paused = true;
            GameState.SetMode(GameMode.Paused);
            Time.timeScale = 0f;
            panel.SetActive(true);
            GameAudio.PlaySfx("ui_select", 1f, 0f);
            StartCoroutine(SelectNextFrame());
        }

        void Resume()
        {
            if (!paused) return;
            paused = false;
            Time.timeScale = 1f;
            panel.SetActive(false);
            GameState.SetMode(GameMode.Exploration);
            GameAudio.PlaySfx("ui_back", 1f, 0f);
        }

        void ToMainMenu()
        {
            paused = false;
            Time.timeScale = 1f;
            SceneFader.Load(mainMenuScene);
        }

        IEnumerator SelectNextFrame()
        {
            yield return null;
            MenuWidgets.Select(resumeButton.gameObject);
        }

        static float Cycle(float value) => value >= 0.99f ? 0f : Mathf.Min(1f, Mathf.Round(value * 4f + 1f) / 4f);

        void Refresh()
        {
            MenuWidgets.SetLabel(musicButton, $"Music Volume: {Mathf.RoundToInt(GameAudio.MusicVolume * 100f)}%");
            MenuWidgets.SetLabel(sfxButton, $"Effects Volume: {Mathf.RoundToInt(GameAudio.SfxVolume * 100f)}%");
            MenuWidgets.SetLabel(controllerButton, "Button icons: " + InputPrompts.ModeLabel);   // Auto follows the last device used
        }

        void OnDestroy()
        {
            if (paused) Time.timeScale = 1f;
        }
    }
}
```
### DefeatScreen.cs

Ruta: `Assets/TD_Game/Scripts/UI/DefeatScreen.cs` · 113 líneas

Pantalla de derrota con Retry / Main Menu. **Se conecta con:** CombatRewards.Defeat (espera a que el jugador elija), GameAudio (canción de Game Over), SceneFader.

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// Full Game Over screen (task 4.18). CombatRewards.Defeat waits for it:
    /// the player chooses "Retry" (back to the checkpoint) or "Main Menu".
    /// Keys: ENTER / SPACE / gamepad A = retry, ESC / gamepad B = main menu.
    /// </summary>
    public class DefeatScreen : MonoBehaviour
    {
        public static DefeatScreen Instance { get; private set; }

        [SerializeField] CanvasGroup panel;
        [SerializeField] Text tipText;
        [SerializeField] Button retryButton;
        [SerializeField] Button menuButton;
        [Tooltip("Scene loaded by Main Menu. If it is not in Build Settings, the button retries instead.")]
        [SerializeField] string mainMenuScene = SceneIds.MainMenu;
        [SerializeField, Min(0.01f)] float fadeTime = 0.6f;
        [Tooltip("Seconds before the choice is accepted (avoids skipping it by accident while mashing keys).")]
        [SerializeField, Min(0f)] float inputDelay = 1f;
        [SerializeField, TextArea] string[] tips =
        {
            "Wait for the last moment to dodge: a PERFECT dodge gives stamina, Tonalli and a counter-attack.",
            "Spells cannot be dodged. Interrupt them with 1.",
            "Ending your turn with stamina left is fine: you recover part of it every turn.",
            "Jump onto an enemy before the battle to AMBUSH it and act first.",
            "Green orbs heal you. Collect them before the next fight.",
            "The Giant's quake cannot be dodged. Keep your health high before it charges.",
            "Quetzalcóatl changes form at half health. Save your Tonalli Burst for that moment."
        };

        enum Choice { None, Retry, Menu }
        Choice choice;

        void Awake()
        {
            Instance = this;
            Hide();
            if (retryButton != null) retryButton.onClick.AddListener(() => choice = Choice.Retry);
            if (menuButton != null) menuButton.onClick.AddListener(() => choice = Choice.Menu);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Shows the screen and waits until the player chooses. Called during the defeat.</summary>
        public IEnumerator Show()
        {
            choice = Choice.None;
            // The keys follow the device in use: [ENTER] / [A] / [CROSS] and [ESC] / [B] / [CIRCLE].
            SetLabel(retryButton, "RETRY FROM THE BEGINNING   {Confirm}");   // a defeat sends you back to the start of the map
            SetLabel(menuButton, "MAIN MENU   {Back}");
            GameAudio.PlaySfx("defeat");
            GameAudio.PlayMusic(MusicTrack.GameOver);
            if (tipText != null && tips.Length > 0) tipText.text = "TIP: " + tips[Random.Range(0, tips.Length)];
            panel.blocksRaycasts = true;
            panel.interactable = true;
            for (float a = 0f; a < 1f; a += Time.unscaledDeltaTime / fadeTime)
            {
                panel.alpha = a;
                yield return null;
            }
            panel.alpha = 1f;

            yield return new WaitForSecondsRealtime(inputDelay);
            while (choice == Choice.None)
            {
                ReadKeys();
                yield return null;
            }

            GameAudio.PlaySfx(choice == Choice.Menu ? "ui_back" : "ui_select");
            if (choice == Choice.Menu && Application.CanStreamedLevelBeLoaded(mainMenuScene))
            {
                SceneFader.Load(mainMenuScene);
                yield break;
            }
            Hide();
        }

        void ReadKeys()
        {
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            if ((keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                || (pad != null && pad.buttonSouth.wasPressedThisFrame))
                choice = Choice.Retry;
            else if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || (pad != null && pad.buttonEast.wasPressedThisFrame))
                choice = Choice.Menu;
        }

        static void SetLabel(Button button, string text)
        {
            var label = button != null ? button.GetComponentInChildren<Text>() : null;
            if (label != null) label.text = InputPrompts.Localize(text);
        }

        void Hide()
        {
            if (panel == null) return;
            panel.alpha = 0f;
            panel.blocksRaycasts = false;
            panel.interactable = false;
        }
    }
}
```
### CreditsScreen.cs

Ruta: `Assets/TD_Game/Scripts/UI/CreditsScreen.cs` · 272 líneas

Final del juego: epílogo y créditos que suben (incluye a Claude). **Se conecta con:** se muestra al vencer al jefe; GameSave (borra la partida), SceneFader (vuelve al menú).

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// Ending of the game (task 4.7), after winning against the boss (EnemyData.isBoss). The save is deleted and:
    /// 1. epilogue cards appear one by one over a slowly moving picture with embers rising,
    /// 2. the credits scroll up between two gold Aztec friezes (Pirata One, coloured headers),
    /// 3. "THANK YOU FOR PLAYING" beats like a heart, then the main menu loads.
    /// ENTER / SPACE / gamepad A skips the current part.
    /// </summary>
    public class CreditsScreen : MonoBehaviour
    {
        [SerializeField] CanvasGroup panel;
        [Tooltip("The text that scrolls up (child of a mask or of the panel).")]
        [SerializeField] RectTransform scrollingText;
        [SerializeField] Text creditsText;
        [SerializeField, Min(1f)] float scrollSeconds = 30f;
        [SerializeField, Min(0f)] float delayAfterVictory = 1.5f;
        [Tooltip("Loaded at the end. If it is not in Build Settings, the current scene restarts.")]
        [SerializeField] string mainMenuScene = SceneIds.MainMenu;

        [Header("Look (set by the builder)")]
        [SerializeField] Font menuFont;
        [Tooltip("Picture behind the credits (it moves slowly).")]
        [SerializeField] Texture background;
        [Tooltip("Aztec frieze tiled at the top and bottom.")]
        [SerializeField] Sprite greca;
        [SerializeField, Min(0)] int emberCount = 60;

        static readonly string[] Epilogue =
        {
            "Quetzalcóatl has fallen.",
            "<color=#C0392B>The gods finally answered.</color>",
            "But no prayer brings the dead back.",
            "Tlahuelli walked on, deeper into Mictlán...",
        };

        // Fixed in code (not in the scene) so an old saved text can never replace it.
        const string Gold = "#C8963C", Jade = "#3FA37A", Red = "#C0392B", Ash = "#8C8579";
        static readonly string EndCredits =
            $"<size=110><color={Gold}>MICTLÁN</color></size>\n" +
            $"<color={Ash}>the journey of Tlahuelli</color>\n\n\n\n" +
            $"<size=44><color={Red}>A GAME BY MICTA FORGE</color></size>\n\n\n" +
            $"<color={Gold}>Antonio Enríquez Velasco</color>\n<size=26><color={Ash}>combat · animation · effects</color></size>\n\n" +
            $"<color={Gold}>Alejandro Cornejo Pérez</color>\n<size=26><color={Ash}>enemies · menus · audio · saving</color></size>\n\n" +
            $"<color={Gold}>Adrián Gallardo Medina</color>\n<size=26><color={Ash}>map · level design</color></size>\n\n" +
            $"<color={Gold}>Jetzuvely del Carmen González Gutiérrez</color>\n<size=26><color={Ash}>art · sprites</color></size>\n\n" +
            $"<color={Gold}>Claude</color>\n<size=26><color={Ash}>AI assistant by Anthropic · programming · procedural art · sound</color></size>\n\n\n\n" +
            $"<size=40><color={Jade}>IN MEMORY OF THE MEXICA WORLD</color></size>\n" +
            $"<size=26><color={Ash}>Mictlantecuhtli · Quetzalcóatl · Tezcatlipoca · Coatlicue\nEhécatl · Tlazoltéotl · Xólotl</color></size>\n\n\n\n" +
            $"<size=34><color={Red}>MADE WITH</color></size>\n" +
            $"Unity 6\n<size=26><color={Ash}>Font: Pirata One (SIL Open Font License)</color></size>\n\n\n\n\n" +
            $"<size=26><color={Ash}>\"The gods forgot you. They forgot me too.\"</color></size>";

        bool playing;
        RectTransform backgroundRect;
        Text card;
        CanvasGroup cardGroup;   // fades the card (rich-text colours ignore the Text alpha)
        readonly List<(RectTransform rect, float speed, float sway)> embers = new List<(RectTransform, float, float)>();

        void Awake()
        {
            if (panel != null) { panel.alpha = 0f; panel.blocksRaycasts = false; }
        }

        void OnEnable() => CombatManager.CombatWon += OnCombatWon;
        void OnDisable() => CombatManager.CombatWon -= OnCombatWon;

        void OnCombatWon(IReadOnlyList<EnemyData> defeated)
        {
            if (playing) return;
            foreach (var enemy in defeated)
                if (enemy != null && enemy.isBoss)
                {
                    StartCoroutine(Roll());
                    return;
                }
        }

        /// <summary>Can also be called directly (tests, a cutscene).</summary>
        public IEnumerator Roll()
        {
            playing = true;
            GameSave.Delete();   // the game is finished: no Continue afterwards, only New Game
            yield return new WaitForSecondsRealtime(delayAfterVictory);
            GameState.SetMode(GameMode.Cutscene);
            GameAudio.PlayMusic(MusicTrack.Menu);
            BuildLook();
            StartCoroutine(Animate());

            panel.blocksRaycasts = true;
            yield return FadePanel(1f, 1.5f);

            // 1. Epilogue cards.
            foreach (string line in Epilogue)
            {
                card.text = line;
                yield return ShowCard(3.2f);
            }

            // 2. The credits scroll up.
            var area = (RectTransform)scrollingText.parent;
            float start = -area.rect.height * 0.5f - scrollingText.rect.height * 0.5f;
            float end = area.rect.height * 0.5f + scrollingText.rect.height * 0.5f;
            for (float t = 0f; t < scrollSeconds; t += Time.unscaledDeltaTime)
            {
                scrollingText.anchoredPosition = new Vector2(0f, Mathf.Lerp(start, end, t / scrollSeconds));
                if (SkipPressed()) break;
                yield return null;
            }
            scrollingText.gameObject.SetActive(false);

            // 3. Thank you, beating like a heart.
            card.text = $"<size=90><color={Gold}>THANK YOU</color></size>\n<size=50>FOR PLAYING</size>";
            GameAudio.PlaySfx("victory", 0.7f, 0f);
            float shown = 0f;
            cardGroup.alpha = 0f;
            while (shown < 6f && !(shown > 1f && SkipPressed()))
            {
                shown += Time.unscaledDeltaTime;
                float beat = 1f + Mathf.Max(0f, Mathf.Sin(shown * 5f)) * 0.04f;
                card.rectTransform.localScale = Vector3.one * beat;
                cardGroup.alpha = Mathf.Clamp01(shown);
                yield return null;
            }

            if (Application.CanStreamedLevelBeLoaded(mainMenuScene)) SceneFader.Load(mainMenuScene);
            else SceneFader.Load(SceneManager.GetActiveScene().name);
        }

        /// <summary>A card grows in from a little bigger, stays, and fades away.</summary>
        IEnumerator ShowCard(float seconds)
        {
            var rect = card.rectTransform;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float a = Mathf.Min(Mathf.Clamp01(t / 0.8f), Mathf.Clamp01((seconds - t) / 0.7f));
                cardGroup.alpha = a;
                rect.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f, Mathf.SmoothStep(0f, 1f, t / 0.8f));
                if (t > 0.5f && SkipPressed()) break;
                yield return null;
            }
            cardGroup.alpha = 0f;
        }

        IEnumerator FadePanel(float to, float seconds)
        {
            float from = panel.alpha;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                panel.alpha = Mathf.Lerp(from, to, t / seconds);
                yield return null;
            }
            panel.alpha = to;
        }

        /// <summary>The picture drifts slowly (Ken Burns) and the embers rise and sway.</summary>
        IEnumerator Animate()
        {
            float t = 0f;
            var size = ((RectTransform)panel.transform).rect.size;
            while (true)
            {
                t += Time.unscaledDeltaTime;
                if (backgroundRect != null)
                {
                    backgroundRect.localScale = Vector3.one * (1.12f + Mathf.Sin(t * 0.05f) * 0.05f);
                    backgroundRect.anchoredPosition = new Vector2(Mathf.Sin(t * 0.07f) * 40f, Mathf.Cos(t * 0.05f) * 20f);
                }
                foreach (var (rect, speed, sway) in embers)
                {
                    var p = rect.anchoredPosition;
                    p.y += speed * Time.unscaledDeltaTime;
                    p.x += Mathf.Sin(t * 1.5f + sway) * 12f * Time.unscaledDeltaTime;
                    if (p.y > size.y * 0.5f + 20f) p = new Vector2(Random.Range(-size.x, size.x) * 0.5f, -size.y * 0.5f - 20f);
                    rect.anchoredPosition = p;
                }
                yield return null;
            }
        }

        // ---------------- Everything on screen is created here, so the builder only needs the references ----------------

        void BuildLook()
        {
            var root = (RectTransform)panel.transform;
            var font = menuFont != null ? menuFont : MenuWidgets.Font;

            if (background != null)
            {
                var picture = MenuWidgets.Picture("Background", root, background);
                backgroundRect = (RectTransform)picture.transform;
                backgroundRect.anchorMin = backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
                float aspect = background.width / (float)background.height;
                float width = Mathf.Max(root.rect.width, root.rect.height * aspect);
                backgroundRect.sizeDelta = new Vector2(width, width / aspect);
                picture.color = new Color(0.45f, 0.4f, 0.42f);
                backgroundRect.SetSiblingIndex(0);
                var shade = MenuWidgets.Stretch(MenuWidgets.Image("Shade", root, new Color(0f, 0f, 0f, 0.45f)).rectTransform);
                shade.SetSiblingIndex(1);
            }

            var ember = MenuWidgets.Rect("Embers", root);
            MenuWidgets.Stretch(ember);
            for (int i = 0; i < emberCount; i++)
            {
                var e = MenuWidgets.Image("Ember", ember, Random.value < 0.6f ? new Color(1f, 0.5f, 0.15f, 0.8f) : new Color(1f, 0.8f, 0.3f, 0.6f));
                var r = e.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.sizeDelta = Vector2.one * Random.Range(3f, 7f);
                r.anchoredPosition = new Vector2(Random.Range(-0.5f, 0.5f) * root.rect.width, Random.Range(-0.5f, 0.5f) * root.rect.height);
                embers.Add((r, Random.Range(25f, 70f), Random.Range(0f, 6f)));
            }

            if (greca != null)
                foreach (bool top in new[] { true, false })
                {
                    var band = MenuWidgets.Image(top ? "GrecaTop" : "GrecaBottom", root, new Color(0.78f, 0.59f, 0.24f));
                    band.sprite = greca;
                    band.type = Image.Type.Tiled;
                    var r = band.rectTransform;
                    r.anchorMin = new Vector2(0f, top ? 1f : 0f);
                    r.anchorMax = new Vector2(1f, top ? 1f : 0f);
                    r.pivot = new Vector2(0.5f, top ? 1f : 0f);
                    r.sizeDelta = new Vector2(0f, 36f);
                    r.anchoredPosition = Vector2.zero;
                    if (!top) r.localScale = new Vector3(1f, -1f, 1f);
                }

            creditsText.font = font;
            creditsText.supportRichText = true;
            creditsText.fontSize = 36;
            creditsText.lineSpacing = 1.1f;
            creditsText.verticalOverflow = VerticalWrapMode.Overflow;
            creditsText.color = MexicaPalette.Bone;
            creditsText.text = EndCredits;
            Shadow(creditsText);
            scrollingText.sizeDelta = new Vector2(1100f, creditsText.preferredHeight + 200f);
            scrollingText.anchoredPosition = new Vector2(0f, -5000f);   // hidden below until the scroll starts
            scrollingText.SetAsLastSibling();

            card = MenuWidgets.Label("Card", root, "", 58, MexicaPalette.Bone, TextAnchor.MiddleCenter);
            card.font = font;
            card.supportRichText = true;
            MenuWidgets.Center(card.rectTransform, Vector2.zero, new Vector2(1400f, 400f));
            cardGroup = card.gameObject.AddComponent<CanvasGroup>();
            cardGroup.alpha = 0f;
            Shadow(card);
        }

        static void Shadow(Text text)
        {
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            shadow.effectDistance = new Vector2(3f, -3f);
        }

        static bool SkipPressed()
        {
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            return (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                || (pad != null && pad.buttonSouth.wasPressedThisFrame);
        }
    }
}
```

---

## Audio

### AudioLibrary.cs

Ruta: `Assets/TD_Game/Scripts/Audio/AudioLibrary.cs` · 65 líneas

Asset con todas las canciones y efectos por nombre (con variantes al azar). **Se conecta con:** GameAudio lo lee; el asset está en Audio/AudioLibrary.asset y los sonidos nuevos en Audio/SFX/Generated.

```csharp
using UnityEngine;

namespace TD_Game
{
    /// <summary>The songs of the game.</summary>
    public enum MusicTrack { None, Menu, Explore, Battle, Boss, GameOver }

    /// <summary>
    /// Every song and sound effect of the game in one asset (adapted from Mictlán v5's Resources/Audio).
    /// Sound effects are found by name: GameAudio.PlaySfx("hurt").
    /// To replace a sound, drag another clip into its slot here. Create with: Create > TD_Game > Audio Library.
    /// </summary>
    [CreateAssetMenu(menuName = "TD_Game/Audio Library", fileName = "AudioLibrary")]
    public class AudioLibrary : ScriptableObject
    {
        [System.Serializable]
        public class NamedClip
        {
            public string name;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
            [Tooltip("Other versions of the same sound: one is picked at random each time, so repeated sounds do not tire the ear.")]
            public AudioClip[] variants;

            public AudioClip Pick()
            {
                int count = variants == null ? 0 : variants.Length;
                int i = Random.Range(-1, count);
                var chosen = i < 0 ? clip : variants[i];
                return chosen != null ? chosen : clip;
            }
        }

        [Header("Music")]
        public AudioClip menu;
        public AudioClip explore;
        public AudioClip battle;
        public AudioClip boss;
        public AudioClip gameOver;

        [Header("Sound effects (found by name)")]
        public NamedClip[] sfx;

        public AudioClip GetMusic(MusicTrack track)
        {
            switch (track)
            {
                case MusicTrack.Menu: return menu;
                case MusicTrack.Explore: return explore;
                case MusicTrack.Battle: return battle;
                case MusicTrack.Boss: return boss;
                case MusicTrack.GameOver: return gameOver;
                default: return null;
            }
        }

        public NamedClip GetSfx(string clipName)
        {
            if (sfx == null) return null;
            foreach (var s in sfx)
                if (s != null && s.name == clipName) return s;
            return null;
        }
    }
}
```
### GameAudio.cs

Ruta: `Assets/TD_Game/Scripts/Audio/GameAudio.cs` · 194 líneas

El reproductor de todo el juego: música con fundidos, efectos (sin encimar el mismo sonido), canal de voz del comerciante y fuentes en bucle para el ambiente. Sobrevive entre escenas. Uso: GameAudio.PlaySfx(\"hurt\"). **Se conecta con:** AudioLibrary, el menú de opciones (volúmenes en PlayerPrefs), y lo llaman casi todos los scripts.

```csharp
using System.Collections;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Music and sound effects for the whole game (adapted from Mictlán v5's AudioManager).
    /// Put one in EVERY scene with the AudioLibrary and the song of that scene. The first one survives scene changes;
    /// the ones in later scenes only tell it which song to play and then remove themselves.
    /// Usage from code: GameAudio.PlaySfx("hurt");  GameAudio.PlayMusic(MusicTrack.Battle);
    /// Volumes (master, music, effects) are saved in PlayerPrefs and changed in the Options menu.
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        const string MasterKey = "td_master_volume", MusicKey = "td_music_volume", SfxKey = "td_sfx_volume";

        [SerializeField] AudioLibrary library;
        [Tooltip("Song that starts when this scene loads.")]
        [SerializeField] MusicTrack sceneMusic = MusicTrack.Explore;
        [SerializeField, Min(0f)] float crossFadeSeconds = 0.8f;
        [SerializeField, Range(1, 24)] int sfxVoices = 10;
        [Tooltip("The same effect is not repeated closer than this (seconds): stacked copies only sound louder and harsher.")]
        [SerializeField, Range(0f, 0.2f)] float minRepeatSeconds = 0.06f;
        [Tooltip("Spoken lines (the merchant), relative to the effects volume.")]
        [SerializeField, Range(0f, 1f)] float voiceVolume = 0.95f;

        public static GameAudio Instance { get; private set; }

        AudioSource musicA, musicB;    // two sources so songs can cross-fade
        AudioSource[] voices;
        AudioSource voiceSource;
        readonly System.Collections.Generic.Dictionary<string, float> lastPlayed = new System.Collections.Generic.Dictionary<string, float>();
        int nextVoice;
        MusicTrack current = MusicTrack.None;
        Coroutine fade;

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(MasterKey, 0.6f);   // 60% by default (100% was too loud)
            set { PlayerPrefs.SetFloat(MasterKey, Mathf.Clamp01(value)); AudioListener.volume = Mathf.Clamp01(value); }
        }

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicKey, 0.45f);
            set { PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value)); if (Instance != null) Instance.ApplyMusicVolume(); }
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxKey, 1f);   // effects louder than the music so hits are heard
            set => PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value));
        }

        public static MusicTrack CurrentTrack => Instance != null ? Instance.current : MusicTrack.None;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // A new scene: the surviving GameAudio plays this scene's song.
                Instance.PlayTrack(sceneMusic);
                Destroy(this);   // only this component: GameplayAudio on the same object must keep working

                return;
            }
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            AudioListener.volume = MasterVolume;

            musicA = NewSource(true);
            musicB = NewSource(true);
            voices = new AudioSource[sfxVoices];
            for (int i = 0; i < voices.Length; i++) voices[i] = NewSource(false);
            voiceSource = NewSource(false);
        }

        void Start()
        {
            if (Instance == this) PlayTrack(sceneMusic);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        AudioSource NewSource(bool loop)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.spatialBlend = 0f;
            return s;
        }

        // ---------------- Static shortcuts (safe without a GameAudio in the scene) ----------------

        public static void PlaySfx(string clipName, float volume = 1f, float pitchJitter = 0.05f)
        {
            if (Instance != null) Instance.Sfx(clipName, volume, pitchJitter);
        }

        public static void PlayMusic(MusicTrack track)
        {
            if (Instance != null) Instance.PlayTrack(track);
        }

        // ---------------- Implementation ----------------

        void Sfx(string clipName, float volume, float pitchJitter)
        {
            var entry = library != null ? library.GetSfx(clipName) : null;
            if (entry == null || entry.clip == null) return;

            // The same sound fired again in the same instant (several enemies hit, many orbs) is played once.
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(clipName, out float last) && now - last < minRepeatSeconds) return;
            lastPlayed[clipName] = now;

            var voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            voice.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            voice.PlayOneShot(entry.Pick(), entry.volume * volume * SfxVolume);
        }

        /// <summary>A spoken line (the merchant). Only one voice at a time: a new line cuts the previous one.</summary>
        public static void PlayVoice(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (Instance == null || clip == null) return;
            var source = Instance.voiceSource;
            source.Stop();
            source.clip = clip;
            source.pitch = pitch;
            source.volume = Mathf.Clamp01(volume * Instance.voiceVolume * SfxVolume);
            source.Play();
        }

        /// <summary>A new looping source for an ambient sound (wind, river, forest). The caller fades its volume.</summary>
        public static AudioSource LoopSource(AudioClip clip)
        {
            if (Instance == null || clip == null) return null;
            var source = Instance.NewSource(true);
            source.clip = clip;
            source.volume = 0f;
            source.Play();
            return source;
        }

        public void PlayTrack(MusicTrack track)
        {
            if (track == current) return;
            current = track;
            var clip = library != null ? library.GetMusic(track) : null;
            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(CrossFade(clip));
        }

        IEnumerator CrossFade(AudioClip clip)
        {
            // The new song goes in the silent source; the playing one fades out.
            var from = musicA.isPlaying ? musicA : musicB;
            var to = from == musicA ? musicB : musicA;
            float fromStart = from.volume;
            if (clip != null)
            {
                to.clip = clip;
                to.volume = 0f;
                to.Play();
            }
            for (float t = 0f; t < crossFadeSeconds; t += Time.unscaledDeltaTime)
            {
                float k = t / crossFadeSeconds;
                from.volume = Mathf.Lerp(fromStart, 0f, k);
                if (clip != null) to.volume = Mathf.Lerp(0f, MusicVolume, k);
                yield return null;
            }
            from.Stop();
            if (clip != null) to.volume = MusicVolume;
            fade = null;
        }

        void ApplyMusicVolume()
        {
            if (fade != null) return;
            if (musicA.isPlaying) musicA.volume = MusicVolume;
            if (musicB.isPlaying) musicB.volume = MusicVolume;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;
    }
}
```
### GameplayAudio.cs

Ruta: `Assets/TD_Game/Scripts/Audio/GameplayAudio.cs` · 156 líneas

Escucha los eventos del combate y del mapa y pone el sonido correcto (inicio de pelea, golpes, muerte, esquiva, orbes, salto). El combate nunca toca el audio directamente. **Se conecta con:** CombatManager, CombatEnemy.Defeated, CombatFeed, OrbSpawner, Health, PlayerJump → GameAudio.

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Connects the game events to the music and the sound effects of Mictlán v5.
    /// The combat code never plays sounds itself: this component listens to its events (like the HUD does).
    /// Put one in each level scene.
    /// </summary>
    public class GameplayAudio : MonoBehaviour
    {
        [SerializeField] PlayerCombatant player;

        readonly List<CombatEnemy> enemies = new List<CombatEnemy>();
        PlayerJump jump;
        bool wasAirborne;
        CombatAbility lastAction;

        /// <summary>
        /// Sounds that come from what the player is doing (same as Mictlán v5's combat):
        /// swinging the Macuahuitl, Seismic Jump, jumping and landing.
        /// </summary>
        void Update()
        {
            var combat = CombatManager.Instance;
            var action = combat != null && combat.InCombat ? combat.PlayerTurn.Performing : null;
            if (action != null && action != lastAction)
            {
                if (action is SeismicAbility) GameAudio.PlaySfx("seismic");
                else if (!(action is PotionAbility)) GameAudio.PlaySfx("swing", 0.8f, 0.1f);
            }
            lastAction = action;

            if (jump != null)
            {
                if (jump.IsAirborne && !wasAirborne) GameAudio.PlaySfx("jump", 0.6f, 0.05f);
                if (!jump.IsAirborne && wasAirborne) GameAudio.PlaySfx("land", 0.5f, 0.05f);
                wasAirborne = jump.IsAirborne;
            }
        }

        void Start()
        {
            if (player == null) player = FindAnyObjectByType<PlayerCombatant>();
            if (player != null) jump = player.GetComponent<PlayerJump>();
            if (player != null)
            {
                player.Health.Damaged += OnPlayerDamaged;
                player.Health.Healed += OnPlayerHealed;
            }
            foreach (var enemy in FindObjectsByType<CombatEnemy>(FindObjectsInactive.Include))
            {
                if (enemy.Health == null) continue;
                enemy.Health.Damaged += OnEnemyDamaged;
                enemies.Add(enemy);
            }
        }

        void OnEnable()
        {
            CombatManager.CombatStarted += OnCombatStarted;
            CombatManager.CombatWon += OnCombatWon;
            CombatManager.CombatLost += OnCombatLost;
            CombatEnemy.Defeated += OnEnemyDefeated;
            CombatFeed.Banner += OnBanner;
            CombatFeed.CounterPrompt += OnCounterPrompt;
            CombatFeed.TurnLabel += OnTurnLabel;
            OrbSpawner.Collected += OnOrbCollected;
        }

        void OnDisable()
        {
            CombatManager.CombatStarted -= OnCombatStarted;
            CombatManager.CombatWon -= OnCombatWon;
            CombatManager.CombatLost -= OnCombatLost;
            CombatEnemy.Defeated -= OnEnemyDefeated;
            CombatFeed.Banner -= OnBanner;
            CombatFeed.CounterPrompt -= OnCounterPrompt;
            CombatFeed.TurnLabel -= OnTurnLabel;
            OrbSpawner.Collected -= OnOrbCollected;
        }

        void OnDestroy()
        {
            if (player != null)
            {
                player.Health.Damaged -= OnPlayerDamaged;
                player.Health.Healed -= OnPlayerHealed;
            }
            foreach (var enemy in enemies)
                if (enemy != null) enemy.Health.Damaged -= OnEnemyDamaged;
        }

        // ---------------- Battle ----------------

        void OnCombatStarted()
        {
            bool boss = false;
            var context = CombatManager.Instance != null ? CombatManager.Instance.Context : null;
            if (context != null)
                foreach (var e in context.Enemies)
                    if (e != null && e.Data.isBoss) boss = true;
            GameAudio.PlayMusic(boss ? MusicTrack.Boss : MusicTrack.Battle);
            GameAudio.PlaySfx(boss ? "roar" : "alert");
        }

        void OnCombatWon(IReadOnlyList<EnemyData> _)
        {
            GameAudio.PlaySfx("victory");
            GameAudio.PlayMusic(MusicTrack.Explore);
        }

        // The defeat sound and the Game Over song start in DefeatScreen; after the retry the map song returns.
        void OnCombatLost() => GameAudio.PlayMusic(MusicTrack.Explore);

        void OnEnemyDefeated(CombatEnemy _) => GameAudio.PlaySfx("enemy_die");

        void OnBanner(string text, float _)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (text.StartsWith("PERFECT")) GameAudio.PlaySfx("perfect");
            else if (text.StartsWith("Dodged")) GameAudio.PlaySfx("dodge");
            else if (text.StartsWith("Interrupted")) GameAudio.PlaySfx("interrupt");
            else if (text.StartsWith("Last Stand")) GameAudio.PlaySfx("fury");
            else if (text.Contains("transforms")) GameAudio.PlaySfx("roar");
            else if (text.StartsWith("Stunned")) GameAudio.PlaySfx("land");
            else if (text.StartsWith("Not enough") || text.StartsWith("Out of stamina") || text.StartsWith("You have not")
                     || text.Contains("already used")) GameAudio.PlaySfx("error");
            else if (text.StartsWith("New skill")) GameAudio.PlaySfx("key");
        }

        void OnCounterPrompt(string prompt)
        {
            if (!string.IsNullOrEmpty(prompt)) GameAudio.PlaySfx("telegraph", 0.8f);
        }

        void OnTurnLabel(string label)
        {
            if (label == "YOUR TURN") GameAudio.PlaySfx("turn_start", 0.7f);
        }

        // ---------------- Hits and pickups ----------------

        void OnPlayerDamaged(float _) => GameAudio.PlaySfx("hurt", 1f, 0.06f);

        void OnPlayerHealed(float amount)
        {
            if (amount > 0.5f && GameState.Mode == GameMode.Combat) GameAudio.PlaySfx("potion");
        }

        void OnEnemyDamaged(float _) => GameAudio.PlaySfx("enemy_hit", 1f, 0.08f);

        void OnOrbCollected(SoulOrb orb, PlayerCombatant who) => GameAudio.PlaySfx("key", 0.35f, 0.15f);
    }
}
```
### AmbientSoundscape.cs

Ruta: `Assets/TD_Game/Scripts/Audio/AmbientSoundscape.cs` · 113 líneas

Sonido ambiente según dónde está el jugador: bosque, río, viento del desierto o interior. **Se conecta con:** GameAudio.LoopSource, los Tilemaps Ground y Water, HouseDoor.InInterior.

```csharp
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TD_Game
{
    /// <summary>
    /// The sound of each place, under the music: leaves and birds in the forests, the river when water is near,
    /// the dry wind of the desert of the dead and a low hum inside houses. Each sound loops on its own source and
    /// fades in and out with where Tlahuelli walks (it looks at the ground and the water around him).
    /// Quieter during battles. Its volume follows the effects volume of the Options menu.
    /// </summary>
    public class AmbientSoundscape : MonoBehaviour
    {
        [SerializeField] AudioClip forest;
        [SerializeField] AudioClip river;
        [SerializeField] AudioClip desertWind;
        [SerializeField] AudioClip interior;
        [SerializeField, Range(0f, 1f)] float volume = 0.45f;
        [SerializeField, Range(0f, 1f)] float battleVolume = 0.35f;
        [Tooltip("Tiles around the player that are checked for water.")]
        [SerializeField, Min(1f)] float waterHearing = 6f;

        Transform player;
        Tilemap ground, water;
        AudioSource forestSource, riverSource, windSource, interiorSource;
        float forestTarget, riverTarget, windTarget, interiorTarget, nextSample;

        void Start()
        {
            var p = FindAnyObjectByType<PlayerCombatant>();
            if (p != null) player = p.transform;
            foreach (var map in FindObjectsByType<Tilemap>())
            {
                if (map.name == "Ground") ground = map;
                else if (map.name == "Water") water = map;
            }
            forestSource = GameAudio.LoopSource(forest);
            riverSource = GameAudio.LoopSource(river);
            windSource = GameAudio.LoopSource(desertWind);
            interiorSource = GameAudio.LoopSource(interior);
            // Start the loops at different points so they never sound in step.
            foreach (var s in new[] { forestSource, riverSource, windSource, interiorSource })
                if (s != null && s.clip != null) s.time = Random.Range(0f, s.clip.length * 0.9f);
        }

        void OnDestroy()
        {
            foreach (var s in new[] { forestSource, riverSource, windSource, interiorSource })
                if (s != null) Destroy(s);
        }

        void Update()
        {
            if (player == null) return;
            if (Time.unscaledTime >= nextSample)
            {
                nextSample = Time.unscaledTime + 0.3f;
                Sample();
            }
            float master = volume * GameAudio.SfxVolume * (GameState.Mode == GameMode.Combat ? battleVolume : 1f);
            Fade(forestSource, forestTarget * master);
            Fade(riverSource, riverTarget * master);
            Fade(windSource, windTarget * master);
            Fade(interiorSource, interiorTarget * master);
        }

        static void Fade(AudioSource source, float target)
        {
            if (source != null) source.volume = Mathf.MoveTowards(source.volume, target, Time.unscaledDeltaTime * 0.35f);
        }

        void Sample()
        {
            if (HouseDoor.InInterior)
            {
                forestTarget = riverTarget = windTarget = 0f;
                interiorTarget = 1f;
                return;
            }
            interiorTarget = 0f;

            // Ground around the player: grass = forest, sand = desert.
            int grass = 0, sand = 0, total = 0;
            Vector3 p = player.position;
            for (int dx = -3; dx <= 3; dx++)
                for (int dy = -3; dy <= 3; dy++)
                {
                    if (ground == null) break;
                    var tile = ground.GetTile(ground.WorldToCell(p + new Vector3(dx * 2f, dy * 2f)));
                    if (tile == null) continue;
                    total++;
                    if (tile.name.StartsWith("Sand")) sand++;
                    else if (tile.name.Contains("rass") || tile.name.Contains("lowers")) grass++;
                }
            float desert = total > 0 ? sand / (float)total : 0f;
            forestTarget = total > 0 ? Mathf.Clamp01(grass / (float)total * 1.2f) : 0f;
            windTarget = Mathf.Lerp(0.25f, 1f, desert);   // there is always a little wind

            // The closest water within hearing.
            float nearest = float.MaxValue;
            if (water != null)
                for (int i = 0; i < 24; i++)
                {
                    float angle = i * Mathf.PI * 2f / 8f;
                    float radius = waterHearing * (1 + i / 8) / 3f;
                    var probe = p + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    if (water.HasTile(water.WorldToCell(probe))) nearest = Mathf.Min(nearest, radius);
                }
            if (water != null && water.HasTile(water.WorldToCell(p))) nearest = 0f;
            riverTarget = nearest == float.MaxValue ? 0f : Mathf.Clamp01(1.15f - nearest / waterHearing);
        }
    }
}
```

---

## Guardado

### GameSave.cs

Ruta: `Assets/TD_Game/Scripts/Managers/GameSave.cs` · 75 líneas

El archivo de guardado (JSON en persistentDataPath): qué se guarda (SaveData) y cómo se lee, escribe y borra. **Se conecta con:** TitleMenu (Continue / New Game) y SaveController.

```csharp
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TD_Game
{
    /// <summary>Everything the save file stores (task 4.19). JsonUtility turns it into text.</summary>
    [System.Serializable]
    public class SaveData
    {
        public string scene;
        public float checkpointX, checkpointY;
        public float bonusMaxHealth, bonusMaxStamina;
        public int victories;
        public int potions;
        public int gold;
        public bool hasKey;
        public float concentration;
        public List<string> abilities = new List<string>();
        public List<string> defeatedEnemies = new List<string>();
        public List<string> seenTutorials = new List<string>();
        public List<string> ownedSkins = new List<string>();
        public string equippedSkin;
    }

    /// <summary>
    /// Reads and writes the save file in Application.persistentDataPath (one slot).
    /// Called "GameSave" so it does not get confused with the SaveSystem of the previous game.
    /// The main menu uses HasSave, NewGame() and Continue(); SaveController does the rest in the level.
    /// </summary>
    public static class GameSave
    {
        const string FileName = "td_game_save.json";
        static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>Set by the main menu: true = load the save when the level starts.</summary>
        public static bool ContinueRequested { get; set; }

        public static bool HasSave => File.Exists(FilePath);

        public static void NewGame()
        {
            Delete();
            ContinueRequested = false;
        }

        public static void Continue() => ContinueRequested = HasSave;

        public static void Write(SaveData data)
        {
            try { File.WriteAllText(FilePath, JsonUtility.ToJson(data, true)); }
            catch (System.Exception e) { Debug.LogError($"[{nameof(GameSave)}] Could not save: {e.Message}"); }
        }

        /// <summary>Null if there is no save or it cannot be read.</summary>
        public static SaveData Read()
        {
            if (!HasSave) return null;
            try { return JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)); }
            catch (System.Exception e)
            {
                Debug.LogError($"[{nameof(GameSave)}] The save file is damaged: {e.Message}");
                return null;
            }
        }

        public static void Delete()
        {
            if (HasSave) File.Delete(FilePath);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => ContinueRequested = false;
    }
}
```
### SaveController.cs

Ruta: `Assets/TD_Game/Scripts/Managers/SaveController.cs` · 140 líneas

En el mapa: guarda solo después de cada victoria (y al salir de la tienda o terminar la misión) y carga al empezar si elegiste Continue: checkpoint, oro, pociones, habilidades, enemigos derrotados, pistas vistas, skins y misiones. **Se conecta con:** GameSave, PlayerCombatant/PlayerProgression, CombatEnemy.SaveId, ContextTutorial, PlayerSkin, PlayerHazards, CameraFollow.

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Save and continue in the level (task 4.19). Put ONE in each level scene.
    /// - Saves automatically after every victory (the checkpoint is set there) and when Save() is called.
    /// - Loads when the level starts if the menu chose "Continue" (GameSave.ContinueRequested).
    /// Saves: checkpoint, growth, victories, potions, Tonalli, unlocked abilities, defeated enemies and seen tutorials.
    /// Health is not saved: the player always continues with full health.
    /// </summary>
    public class SaveController : MonoBehaviour
    {
        [SerializeField] PlayerCombatant player;
        [Tooltip("EVERY ability that can be unlocked, so the save can find them by name.")]
        [SerializeField] CombatAbility[] allAbilities;
        [Tooltip("For testing in the editor: load the save even without pressing Continue in the menu.")]
        [SerializeField] bool alwaysLoadInEditor = false;

        readonly HashSet<string> defeated = new HashSet<string>();

        public static SaveController Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            if (player == null) player = FindAnyObjectByType<PlayerCombatant>();
        }

        void OnEnable()
        {
            CombatEnemy.Defeated += OnEnemyDefeated;
            CombatManager.CombatWon += OnCombatWon;
        }

        void OnDisable()
        {
            CombatEnemy.Defeated -= OnEnemyDefeated;
            CombatManager.CombatWon -= OnCombatWon;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            bool load = GameSave.ContinueRequested || (Application.isEditor && alwaysLoadInEditor);
            GameSave.ContinueRequested = false;
            if (load) Load();
        }

        void OnEnemyDefeated(CombatEnemy enemy)
        {
            // The boss is not remembered: after the credits the save is deleted anyway.
            if (!enemy.Data.isBoss) defeated.Add(enemy.SaveId);
        }

        void OnCombatWon(IReadOnlyList<EnemyData> _) => Save();

        public void Save()
        {
            if (player == null) return;
            var progression = player.Progression;
            var data = new SaveData
            {
                scene = gameObject.scene.name,
                checkpointX = progression.Checkpoint.x,
                checkpointY = progression.Checkpoint.y,
                bonusMaxHealth = progression.BonusMaxHealth,
                bonusMaxStamina = progression.BonusMaxStamina,
                victories = progression.Victories,
                potions = player.Potions,
                gold = progression.Gold,
                hasKey = progression.HasKey,
                concentration = player.Concentration,
                defeatedEnemies = new List<string>(defeated),
                seenTutorials = new List<string>(ContextTutorial.Seen)
            };
            foreach (var ability in progression.UnlockedAbilities)
                if (ability != null) data.abilities.Add(ability.name);
            var skin = player.GetComponent<PlayerSkin>();
            if (skin != null)
            {
                data.ownedSkins = new List<string>(skin.Owned);
                data.equippedSkin = skin.Equipped;
            }
            GameSave.Write(data);
        }

        void Load()
        {
            var data = GameSave.Read();
            if (data == null || player == null) return;
            if (!string.IsNullOrEmpty(data.scene) && data.scene != gameObject.scene.name)
                Debug.LogWarning($"[{nameof(SaveController)}] The save is from scene {data.scene}; loading it here anyway.", this);

            var abilities = new List<CombatAbility>();
            foreach (string abilityName in data.abilities)
            {
                var ability = FindAbility(abilityName);
                if (ability != null) abilities.Add(ability);
                else Debug.LogWarning($"[{nameof(SaveController)}] Ability '{abilityName}' is not in All Abilities.", this);
            }

            var checkpoint = new Vector3(data.checkpointX, data.checkpointY, player.transform.position.z);
            player.Progression.LoadSaved(data.bonusMaxHealth, data.bonusMaxStamina, data.victories, checkpoint, abilities);
            player.FullRestore();
            player.AddPotions(data.potions - player.Potions);
            player.Progression.LoadGold(data.gold);
            if (data.hasKey) player.Progression.GiveKey();
            player.ClearConcentration();
            player.AddConcentration(data.concentration);
            player.Teleport(checkpoint);

            defeated.Clear();
            foreach (string id in data.defeatedEnemies) defeated.Add(id);
            foreach (var enemy in FindObjectsByType<CombatEnemy>())
                if (defeated.Contains(enemy.SaveId)) enemy.gameObject.SetActive(false);

            ContextTutorial.MarkSeen(data.seenTutorials);
            var skin = player.GetComponent<PlayerSkin>();
            if (skin != null) skin.Load(data.ownedSkins, data.equippedSkin);
            var hazards = player.GetComponent<PlayerHazards>();
            if (hazards != null) hazards.ResetSafeSpot();
            var follow = FindAnyObjectByType<CameraFollow>();
            if (follow != null) follow.SnapToTarget();
        }

        CombatAbility FindAbility(string abilityName)
        {
            if (allAbilities == null) return null;
            foreach (var a in allAbilities)
                if (a != null && a.name == abilityName) return a;
            return null;
        }
    }
}
```
