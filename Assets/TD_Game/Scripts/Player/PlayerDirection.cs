using UnityEngine;

namespace TD_Game
{
    /// <summary>The eight screen-relative directions (up = up on the screen).</summary>
    public enum Direction8 { Down, DownRight, Right, UpRight, Up, UpLeft, Left, DownLeft }

    /// <summary>
    /// Keeps the player's current direction and the last valid one (used for idle animations
    /// and for facing in combat). It only reads PlayerMovement; it does not move anything.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerDirection : MonoBehaviour
    {
        [Tooltip("Direction the player faces before moving for the first time.")]
        [SerializeField] Direction8 initialFacing = Direction8.Down;

        [Tooltip("Input shorter than this is treated as 'not moving' (stick dead zone).")]
        [SerializeField, Range(0f, 0.5f)] float deadZone = 0.1f;

        PlayerMovement movement;

        /// <summary>Movement direction this frame (zero when standing still).</summary>
        public Vector2 Current { get; private set; }
        /// <summary>Last non-zero direction, normalized. Never zero.</summary>
        public Vector2 LastValid { get; private set; }
        /// <summary>LastValid snapped to one of the eight directions.</summary>
        public Direction8 Facing { get; private set; }
        public bool IsMoving => Current != Vector2.zero;

        void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            Facing = initialFacing;
            LastValid = ToVector(initialFacing);
        }

        void Update()
        {
            Vector2 dir = movement.MoveDirection;
            Current = dir.sqrMagnitude > deadZone * deadZone ? dir : Vector2.zero;
            if (Current != Vector2.zero) SetFacing(Current);
        }

        /// <summary>Turns the player towards a direction without moving (combat, cutscenes).</summary>
        public void Face(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.0001f) SetFacing(direction);
        }

        void SetFacing(Vector2 direction)
        {
            LastValid = direction.normalized;
            Facing = ToDirection8(LastValid);
        }

        public static Direction8 ToDirection8(Vector2 direction)
        {
            // 0° = down, counter-clockwise in steps of 45°, matching the enum order.
            float angle = Mathf.Atan2(direction.x, -direction.y) * Mathf.Rad2Deg;
            int index = Mathf.RoundToInt(angle / 45f);
            return (Direction8)((index % 8 + 8) % 8);
        }

        public static Vector2 ToVector(Direction8 direction)
        {
            float angle = (int)direction * 45f * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle));
        }
    }
}
