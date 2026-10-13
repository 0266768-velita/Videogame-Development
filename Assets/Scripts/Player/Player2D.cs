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

    [Header("Hop (visual only, you can still move and collide normally)")]
    public bool canJump = true;
    public float jumpHeight = 0.9f;        // tiles
    public float jumpDuration = 0.45f;     // seconds in the air
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

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        fx = GetComponent<ActorFeedback>();
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

        if (exploring && canJump && jumpAction.WasPressedThisFrame()) lastJumpPress = Time.time;
        UpdateJump(exploring);
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

    public void EnterBattle()
    {
        input = Vector2.zero;
        jumping = false;
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
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        rb.position = position;
        rb.linearVelocity = Vector2.zero;
    }
}
