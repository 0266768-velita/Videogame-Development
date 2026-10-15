using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Enemy AI in 2D EXPLORATION (no NavMesh): patrols, detects Tlahuelli (view cone + hearing + line of sight),
/// chases him around simple obstacles and, once it reaches him, starts the turn-based battle (BattleManager).
/// Every number comes from the assigned EnemyData. Works for Giants, Witches and anything else.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Health))]
public class EnemyController : MonoBehaviour
{
    public enum State { Patrol, Chase, Return, InBattle, Defeated }

    public EnemyData data;

    [Header("Events (hook up sounds / particles here)")]
    public UnityEvent onAlert;
    public UnityEvent onDefeated;

    public Health Health { get; private set; }
    public ActorFeedback Fx { get; private set; }
    public State CurrentState => state;
    public bool IsAvailableForBattle => state != State.InBattle && state != State.Defeated;
    public string UniqueId { get; private set; }
    public bool Phase2Announced { get; set; }
    public bool InPhase2 => data != null && data.isBoss && Health.Normalized <= data.phase2HealthFraction;

    /// <summary>Every enemy currently alive in the level (used for the "all enemies defeated" key drop).</summary>
    public static readonly List<EnemyController> Active = new List<EnemyController>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Active.Clear(); }

    Rigidbody2D rb;
    Transform player;
    State state = State.Patrol;
    Vector2 home;
    Vector2 facing = Vector2.down;
    Vector2 patrolTarget;
    float waitTimer = 0.5f;
    float patrolTimeout;
    bool waiting = true;
    float lostTimer;
    int lastAttackIndex = -1;

    ContactFilter2D filter;
    static readonly List<RaycastHit2D> hitBuffer = new List<RaycastHit2D>();
    static readonly float[] steerAngles = { 45f, -45f, 90f, -90f, 135f, -135f };

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        Health = GetComponent<Health>();
        Fx = GetComponent<ActorFeedback>();
        if (Fx == null) Fx = gameObject.AddComponent<ActorFeedback>();

        filter = new ContactFilter2D();
        filter.NoFilter();
        filter.useTriggers = false;

        if (data == null)
        {
            Debug.LogError($"[{name}] EnemyController has no EnemyData assigned.", this);
            enabled = false;
            return;
        }
        Health.Initialize(data.maxHealth);
        home = transform.position;
        UniqueId = $"{SceneManager.GetActiveScene().name}_{name}_{Mathf.RoundToInt(home.x)}_{Mathf.RoundToInt(home.y)}";
    }

    void Start()
    {
        if (!enabled) return;
        if (SaveSystem.IsDefeated(UniqueId)) { Destroy(gameObject); return; }
        Active.Add(this);
        EnterPatrol();
    }

    void Update()
    {
        if (data == null || state == State.Defeated || state == State.InBattle) return;

        if (GameState.Mode != GameMode.Exploration)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (player == null)
        {
            var p = FindFirstObjectByType<Player2D>();
            if (p != null) player = p.transform;
            else return;
        }

        float dist = Vector2.Distance(rb.position, player.position);

        switch (state)
        {
            case State.Patrol:
                if (CanSeePlayer(dist)) EnterChase(); else PatrolTick();
                break;
            case State.Chase:
                ChaseTick(dist);
                break;
            case State.Return:
                if (CanSeePlayer(dist)) EnterChase(); else ReturnTick();
                break;
        }

        float speed = rb.linearVelocity.magnitude;
        Fx.SetFloat("Speed", speed);
        Fx.SetFloat("MoveX", facing.x);
        Fx.SetFloat("MoveY", facing.y);
        Fx.Face(facing.x);
    }

    // ---------------- States ----------------
    void EnterPatrol()
    {
        state = State.Patrol;
        rb.linearVelocity = Vector2.zero;
        waiting = true;
        waitTimer = Random.Range(1f, 2.5f);
    }

    void EnterChase()
    {
        state = State.Chase;
        lostTimer = 0f;
        Fx.Trigger("Alert");
        Audio.Play("alert", 0.6f);
        onAlert?.Invoke();
    }

    void EnterReturn()
    {
        state = State.Return;
    }

    void PatrolTick()
    {
        if (data.patrolRadius <= 0.1f) { rb.linearVelocity = Vector2.zero; return; }

        if (waiting)
        {
            rb.linearVelocity = Vector2.zero;
            waitTimer -= Time.deltaTime;
            if (waitTimer > 0f) return;

            patrolTarget = home + Random.insideUnitCircle * data.patrolRadius;
            patrolTimeout = 5f;
            waiting = false;
            return;
        }

        patrolTimeout -= Time.deltaTime;
        if (Vector2.Distance(rb.position, patrolTarget) < 0.3f || patrolTimeout <= 0f)
        {
            waiting = true;
            waitTimer = Random.Range(1.5f, 4f);
            rb.linearVelocity = Vector2.zero;
            return;
        }
        MoveToward(patrolTarget, data.patrolSpeed);
    }

    void ChaseTick(float dist)
    {
        if (dist <= data.engageDistance)
        {
            TriggerBattle();
            return;
        }

        MoveToward(player.position, data.chaseSpeed);

        bool sees = CanSeePlayer(dist);
        lostTimer = sees ? 0f : lostTimer + Time.deltaTime;
        if (lostTimer >= data.loseTargetAfterSeconds || dist > data.loseSightRadius) EnterReturn();
    }

    void ReturnTick()
    {
        if (Vector2.Distance(rb.position, home) < 0.3f)
        {
            Health.HealFull();
            EnterPatrol();
            return;
        }
        MoveToward(home, data.patrolSpeed * 1.5f);
    }

    void TriggerBattle()
    {
        rb.linearVelocity = Vector2.zero;
        state = State.InBattle; // prevents a double trigger in the same frame
        if (!BattleManager.Instance.StartBattle(this)) state = State.Chase;
    }

    // ---------------- Movement ----------------
    void MoveToward(Vector2 target, float speed)
    {
        Vector2 to = target - rb.position;
        float d = to.magnitude;
        if (d < 0.05f) { rb.linearVelocity = Vector2.zero; return; }

        Vector2 dir = Steer(to / d);
        rb.linearVelocity = dir * speed;
        facing = dir;
    }

    /// <summary>If something blocks the way, tries alternative directions (simple avoidance).</summary>
    Vector2 Steer(Vector2 dir)
    {
        if (!Blocked(dir)) return dir;
        foreach (float a in steerAngles)
        {
            Vector2 alt = Quaternion.Euler(0f, 0f, a) * dir;
            if (!Blocked(alt)) return alt;
        }
        return dir;
    }

    bool Blocked(Vector2 dir)
    {
        hitBuffer.Clear();
        Physics2D.CircleCast(rb.position, Mathf.Max(0.2f, data.bodyRadius * 0.8f), dir, filter, hitBuffer, data.bodyRadius + 0.6f);
        foreach (var h in hitBuffer) if (IsObstacle(h.collider)) return true;
        return false;
    }

    bool IsObstacle(Collider2D c)
    {
        if (c == null) return false;
        if (c.attachedRigidbody == rb) return false;                         // myself
        if (player != null && c.transform.root == player.root) return false; // the player is not an obstacle
        if (c.isTrigger) return false;
        return true;
    }

    // ---------------- Detection ----------------
    bool CanSeePlayer(float dist)
    {
        if (dist <= data.hearingRadius) return true;
        if (dist > data.detectionRadius) return false;

        Vector2 to = (Vector2)player.position - rb.position;
        if (Vector2.Angle(facing, to) > data.fieldOfView * 0.5f) return false;

        hitBuffer.Clear();
        Physics2D.Linecast(rb.position, player.position, filter, hitBuffer);
        foreach (var h in hitBuffer) if (IsObstacle(h.collider) && h.collider.GetComponent<HazardCollider>() == null) return false;   // water and pits do not block sight
        return true;
    }

    // ---------------- API for BattleManager ----------------
    public void EnterBattle()
    {
        state = State.InBattle;
        rb.linearVelocity = Vector2.zero;
        Fx.SetFloat("Speed", 0f);
        Fx.SetPhysics(false); // in combat the BattleManager moves the transform directly
    }

    /// <summary>Picks a weighted random attack, avoiding a repeat of the previous one.</summary>
    public EnemyAttackData PickAttack()
    {
        var list = (InPhase2 && data.phase2Attacks != null && data.phase2Attacks.Length > 0) ? data.phase2Attacks : data.attacks;
        if (list == null || list.Length == 0) return new EnemyAttackData();
        if (lastAttackIndex >= list.Length) lastAttackIndex = -1;

        float total = 0f;
        for (int i = 0; i < list.Length; i++)
            if (list.Length == 1 || i != lastAttackIndex) total += list[i].weight;

        float roll = Random.value * total;
        for (int i = 0; i < list.Length; i++)
        {
            if (list.Length > 1 && i == lastAttackIndex) continue;
            roll -= list[i].weight;
            if (roll <= 0f) { lastAttackIndex = i; return list[i]; }
        }
        lastAttackIndex = 0;
        return list[0];
    }

    public void OnDefeated()
    {
        if (state == State.Defeated) return;
        state = State.Defeated;
        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        rb.simulated = false;
        Active.Remove(this);
        SaveSystem.MarkDefeated(UniqueId);
        GameProgress.EnemiesDefeated++;
        onDefeated?.Invoke();
        if (Gore.Enabled)
        {
            Gore.Dismember(this);
            Destroy(gameObject, 1.5f);
        }
        else StartCoroutine(Fx.DeathRoutine(0.4f, 1.0f, true));
    }

    /// <summary>If the player loses, the enemy goes back to its spot with full health.</summary>
    public void ResetAfterPlayerDefeat()
    {
        Health.HealFull();
        transform.position = new Vector3(home.x, home.y, transform.position.z);
        Fx.SetPhysics(true);
        rb.position = home;
        EnterPatrol();
    }

    void OnDestroy() { Active.Remove(this); }

    void OnDrawGizmosSelected()
    {
        if (data == null) return;
        Vector3 c = Application.isPlaying ? (Vector3)home : transform.position;
        Gizmos.color = Color.green;  Gizmos.DrawWireSphere(c, data.patrolRadius);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, data.detectionRadius);
        Gizmos.color = new Color(1f, 0.5f, 0f); Gizmos.DrawWireSphere(transform.position, data.hearingRadius);
        Gizmos.color = Color.red;    Gizmos.DrawWireSphere(transform.position, data.engageDistance);
        Gizmos.color = Color.gray;   Gizmos.DrawWireSphere(transform.position, data.loseSightRadius);
    }
}
