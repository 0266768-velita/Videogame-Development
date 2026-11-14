using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Moves the player with physics (Rigidbody2D) using the direction from PlayerInputReader.
    /// Movement is relative to the screen: up on the keyboard = up on the screen.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerMovement : MonoBehaviour
    {
        [Tooltip("Speed in tiles per second (1 tile = 1 unit = 64 px).")]
        [SerializeField, Min(0f)] float moveSpeed = 4f;

        [Tooltip("Off = 4 directions (the dominant axis wins). On = 8 directions.")]
        [SerializeField] bool allowDiagonals = false;

        [Header("Sprint (hold the Sprint button)")]
        [Tooltip("Walking speed is multiplied by this while sprinting.")]
        [SerializeField, Min(1f)] float sprintMultiplier = 1.75f;
        [Tooltip("How fast the speed changes between walking and sprinting (tiles/s per second), so it does not snap.")]
        [SerializeField, Min(0.1f)] float speedChangeRate = 20f;

        Rigidbody2D body;
        PlayerInputReader input;
        float currentSpeed;

        /// <summary>Direction used in the last physics step (length 0..1).</summary>
        public Vector2 MoveDirection { get; private set; }

        /// <summary>True while the player is moving with the sprint button held (animations, dust...).</summary>
        public bool IsSprinting { get; private set; }

        /// <summary>Speed actually used in the last physics step (walk or sprint).</summary>
        public float CurrentSpeed => currentSpeed;

        /// <summary>Current velocity in units per second.</summary>
        public Vector2 Velocity => body.linearVelocity;

        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0f, value);
        }

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            input = GetComponent<PlayerInputReader>();

            // Top-down: no gravity and no spinning when bumping into things.
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            currentSpeed = moveSpeed;
        }

        void FixedUpdate()
        {
            // The player only walks during exploration (combat, pause and cutscenes keep it still).
            MoveDirection = GameState.IsExploring ? ToMoveDirection(input.Move) : Vector2.zero;

            IsSprinting = input.SprintHeld && MoveDirection != Vector2.zero;
            float targetSpeed = moveSpeed * (IsSprinting ? sprintMultiplier : 1f);
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, speedChangeRate * Time.fixedDeltaTime);

            body.linearVelocity = MoveDirection * currentSpeed;
        }

        void OnDisable()
        {
            MoveDirection = Vector2.zero;
            IsSprinting = false;
            if (body != null) body.linearVelocity = Vector2.zero;
        }

        Vector2 ToMoveDirection(Vector2 raw)
        {
            if (!allowDiagonals)
            {
                // Keep only the dominant axis so the player moves in 4 directions.
                raw = Mathf.Abs(raw.x) >= Mathf.Abs(raw.y) ? new Vector2(raw.x, 0f) : new Vector2(0f, raw.y);
            }

            // Normalize only when longer than 1: diagonals are not faster, and a half-tilted stick still walks slowly.
            return raw.sqrMagnitude > 1f ? raw.normalized : raw;
        }
    }
}
