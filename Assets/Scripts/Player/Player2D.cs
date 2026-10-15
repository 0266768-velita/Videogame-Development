using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Tlahuelli in 2D top-down exploration (8 directions, Zelda style).
/// Reads its own input in code (WASD / arrows / left stick / D-pad), so it does not depend
/// on a PlayerInput component or an .inputactions asset.
///
/// Feel: speed ramps up and down (acceleration / deceleration) instead of snapping, turning is snappier
/// than starting, and there is a small hop (Space / A button) built with FakeHeight (visual only).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Player2D : MonoBehaviour
{
    [Header("Movement (tiles per second)")]
    public float speed = 4f;
    public float sprintSpeed = 6f;
    [Tooltip("Tiles/s^2 to reach full speed. Lower = floatier, higher = tighter.")]
    public float acceleration = 35f;
    [Tooltip("Tiles/s^2 when you let go. Lower = slides more.")]
    public float deceleration = 45f;
    [Tooltip("Used when you reverse direction, so turning around does not feel heavy.")]
    public float turnAcceleration = 60f;

    [Header("Jump (crosses rivers and pits: water/pit colliders are ignored while you are in the air)")]
    public bool canJump = true;
    public float jumpHeight = 1.0f;        // tiles
    public float jumpDuration = 0.55f;     // seconds in the air (sprint + jump = about 3.3 tiles: enough for a 2-tile river)
    [Tooltip("Press jump slightly before landing and it still counts.")]
    public float jumpBuffer = 0.12f;

    [Header("Walk feel (turn off if your real animations already bob)")]
    public bool proceduralBob = true;
    public float bobHeight = 0.05f;
    public float bobFrequency = 11f;

    /// <summary>Last facing direction in 4 directions (for animations).</summary>
    public Vector2 Facing { get; private set; } = Vector2.down;
    public Vector2 Position => rb != null ? rb.position : (Vector2)transform.position;
    public bool IsJumping => jumping;

    Rigidbody2D rb;
    ActorFeedback fx;
    FakeHeight fh;
    InputAction moveAction, sprintAction, jumpAction;
    Vector2 input;

    bool jumping;
    float jumpTime;
    float landTimer;
    float lastJumpPress = -10f;
    float bobPhase;
    const float LandSquashTime = 0.12f;

    // hazards (water / pits)
    Collider2D body;
    bool ignoringHazards;
    Vector2 lastSafe;
    float safeTimer;
    bool falling;
    public bool IsFalling => falling;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        fx = GetComponent<ActorFeedback>();
        body = GetComponent<Collider2D>();
        lastSafe = rb.position;
        fh = GetComponent<FakeHeight>();
        if (fh == null) fh = gameObject.AddComponent<FakeHeight>();

        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");
        moveAction.AddBinding("<Gamepad>/dpad");

        sprintAction = new InputAction("Sprint", InputActionType.Button);
        sprintAction.AddBinding("<Keyboard>/leftShift");
        sprintAction.AddBinding("<Gamepad>/leftStickPress");

        jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");
    }

    void OnEnable() { moveAction.Enable(); sprintAction.Enable(); jumpAction.Enable(); }
    void OnDisable() { moveAction.Disable(); sprintAction.Disable(); jumpAction.Disable(); }
    void OnDestroy() { moveAction?.Dispose(); sprintAction?.Dispose(); jumpAction?.Dispose(); }

    void Update()
    {
        bool exploring = GameState.Mode == GameMode.Exploration;

        input = exploring ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        if (input.sqrMagnitude > 1f) input.Normalize();

        if (input.sqrMagnitude > 0.01f)
        {
            // Hysteresis: keep the current axis unless the other one clearly wins (no flicker on diagonals).
            float ax = Mathf.Abs(input.x), ay = Mathf.Abs(input.y);
            bool horizontal = Facing.x != 0f ? ax >= ay * 0.8f : ax > ay * 1.25f;
            Facing = horizontal ? new Vector2(Mathf.Sign(input.x), 0f) : new Vector2(0f, Mathf.Sign(input.y));
            if (fx != null) fx.Face(Facing.x);
        }

        if (exploring && canJump && !falling && jumpAction.WasPressedThisFrame()) lastJumpPress = Time.time;
        UpdateJump(exploring && !falling);
        UpdateHazards(exploring);
        UpdateBob();

        if (fx != null)
        {
            fx.SetFloat("Speed", Mathf.Clamp01(rb.linearVelocity.magnitude / Mathf.Max(0.01f, speed)));
            fx.SetFloat("MoveX", Facing.x);
            fx.SetFloat("MoveY", Facing.y);
            fx.SetBool("Jumping", jumping);
        }
    }

    void UpdateJump(bool exploring)
    {
        if (!jumping)
        {
            if (exploring && canJump && Time.time - lastJumpPress <= jumpBuffer)
            {
                jumping = true;
                jumpTime = 0f;
                lastJumpPress = -10f;
                landTimer = 0f;
                if (fx != null) fx.Trigger("Jump");
                Audio.Play("jump", 0.6f, 0.05f);
                SetHazardCollision(true);
            }
        }

        if (jumping)
        {
            jumpTime += Time.deltaTime;
            float k = Mathf.Clamp01(jumpTime / Mathf.Max(0.05f, jumpDuration));
            float arc = 4f * k * (1f - k);                    // parabola: 0 -> 1 -> 0
            fh.Height = arc * jumpHeight;
            // Stretch while rising/falling fast, relaxed at the top.
            float stretch = Mathf.Abs(1f - 2f * k);           // 1 at takeoff/landing, 0 at the peak
            fh.Squash = new Vector2(1f - 0.10f * stretch, 1f + 0.16f * stretch);

            if (k >= 1f)
            {
                jumping = false;
                fh.Height = 0f;
                landTimer = LandSquashTime;
                Audio.Play("land", 0.5f, 0.05f);
                OnLanded();
            }
        }
        else if (landTimer > 0f)
        {
            landTimer -= Time.deltaTime;
            float k = Mathf.Clamp01(landTimer / LandSquashTime);   // 1 -> 0
            fh.Squash = new Vector2(1f + 0.22f * k, 1f - 0.22f * k);
            if (landTimer <= 0f) fh.Squash = Vector2.one;
        }
        else if (fh.Squash != Vector2.one)
        {
            fh.Squash = Vector2.one;
        }
    }

    void UpdateBob()
    {
        if (!proceduralBob || jumping || landTimer > 0f) { if (!jumping) fh.Bob = 0f; return; }
        float speedNorm = Mathf.Clamp01(rb.linearVelocity.magnitude / Mathf.Max(0.01f, speed));
        if (speedNorm > 0.05f)
        {
            bobPhase += Time.deltaTime * bobFrequency * Mathf.Lerp(0.8f, 1.3f, speedNorm);
            fh.Bob = Mathf.Abs(Mathf.Sin(bobPhase)) * bobHeight * speedNorm;
        }
        else
        {
            bobPhase = 0f;
            fh.Bob = Mathf.MoveTowards(fh.Bob, 0f, Time.deltaTime * 0.5f);
        }
    }

    void FixedUpdate()
    {
        float s = sprintAction.IsPressed() ? sprintSpeed : speed;
        Vector2 target = input * s;
        Vector2 v = rb.linearVelocity;

        float rate;
        if (target.sqrMagnitude < 0.0001f) rate = deceleration;
        else if (v.sqrMagnitude > 0.25f && Vector2.Dot(v.normalized, target.normalized) < 0.3f) rate = turnAcceleration;
        else rate = acceleration;

        rb.linearVelocity = Vector2.MoveTowards(v, target, rate * Time.fixedDeltaTime);
    }

    // ------------------------------------------------------------------ water / pits
    /// <summary>While jumping, water and pit colliders do not block Tlahuelli (that is how he crosses them).</summary>
    void SetHazardCollision(bool ignore)
    {
        if (body == null || ignoringHazards == ignore) return;
        ignoringHazards = ignore;
        foreach (var c in Hazards.Blockers)
            if (c != null) Physics2D.IgnoreCollision(body, c, ignore);
    }

    void UpdateHazards(bool exploring)
    {
        if (!exploring || jumping || falling) return;

        // Remember the last solid place we stood on (a respawn spot after falling)
        safeTimer -= Time.deltaTime;
        if (safeTimer <= 0f)
        {
            safeTimer = 0.2f;
            if (Hazards.IsSafe(rb.position, 0.9f)) lastSafe = rb.position;
        }

        // First time near a river/pit: explain how to cross it
        if (!TutorialTips.Seen("jump_hint") && Hazards.KindAt(rb.position + Facing * 1.6f) != TerrainKind.Ground)
            TutorialTips.Instance.Toast("jump_hint", "Water and pits block your way. RUN (hold SHIFT) and JUMP (SPACE) to cross them. Land in the water or a pit and you fall.", 9f);
    }

    void OnLanded()
    {
        var kind = Hazards.KindAt(rb.position);
        if (kind == TerrainKind.Ground) { SetHazardCollision(false); return; }
        StartCoroutine(FallRoutine(kind));
    }

    System.Collections.IEnumerator FallRoutine(TerrainKind kind)
    {
        falling = true;
        GameState.SetMode(GameMode.Cutscene);
        rb.linearVelocity = Vector2.zero;
        Audio.Play(kind == TerrainKind.Water ? "splash" : "fall");

        // shrink into the water / hole
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            float k = 1f - t / 0.4f;
            fh.Squash = new Vector2(k, k);
            yield return null;
        }
        yield return ScreenFade.Out(0.25f);

        var health = GetComponent<Health>();
        if (health != null) health.TakeDamage(Mathf.Min(health.maxHealth * 0.10f, Mathf.Max(0f, health.Current - 1f)));

        SetHazardCollision(false);
        Teleport(lastSafe);
        fh.ResetVisual();
        var cam = FindFirstObjectByType<CameraFollow2D>();
        if (cam != null) cam.SnapToTarget();
        yield return ScreenFade.In(0.25f);

        GameState.SetMode(GameMode.Exploration);
        falling = false;
        TutorialTips.Instance.Toast("fell", "You fell! Start your jump closer to the edge and run (SHIFT) to cross wider gaps. Falling costs a little health.", 7f);
    }

    public void EnterBattle()
    {
        input = Vector2.zero;
        jumping = false;
        SetHazardCollision(false);
        landTimer = 0f;
        rb.linearVelocity = Vector2.zero;
        if (fh != null) fh.ResetVisual();
        if (fx != null) fx.SetPhysics(false);
    }

    public void ExitBattle()
    {
        if (fx != null) fx.SetPhysics(true);
    }

    /// <summary>Moves the player instantly (battle placement, respawn, loading a save).</summary>
    public void Teleport(Vector2 position)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        rb.position = position;
        rb.linearVelocity = Vector2.zero;
    }
}
