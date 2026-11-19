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
