using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Animates Tlahuelli with the sprite sheet of Mictlán v5 (3 rows of 4 frames):
    ///   row 0 = walking DOWN (front), row 1 = walking UP (back), row 2 = walking to the SIDE (faces right; flipped for left).
    /// Standing still shows frame 0. Walking changes frames; sprinting changes them faster.
    /// The torch goes in the free hand: a fixed position for each direction (the Macuahuitl is already drawn in the sprite).
    /// </summary>
    public class PlayerSpriteAnimator : MonoBehaviour
    {
        [SerializeField] SpriteRenderer sprite;
        [SerializeField] Sprite[] walkDown = new Sprite[4];
        [SerializeField] Sprite[] walkUp = new Sprite[4];
        [SerializeField] Sprite[] walkSide = new Sprite[4];
        [SerializeField] float framesPerSecond = 8f;
        [SerializeField] float sprintSpeedUp = 1.7f;   // sprinting animates this many times faster

        [Header("Torch in the free hand")]
        [SerializeField] Transform torch;
        [SerializeField] SpriteRenderer torchFlame;
        [SerializeField] Vector2 torchFront = new Vector2(-0.45f, 0.42f);   // looking down: left hand, in front of the body
        [SerializeField] Vector2 torchBack = new Vector2(-0.45f, 0.42f);    // looking up: behind the body
        [SerializeField] Vector2 torchSide = new Vector2(-0.22f, 0.5f);     // looking right: back hand (mirrored for left)

        PlayerMovement movement;
        PlayerDirection direction;
        float frameTimer;

        void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            direction = GetComponent<PlayerDirection>();
        }

        void Update()
        {
            // 1. Which way is Tlahuelli looking? (in battle always to the right, towards the enemies)
            Vector2 look = GameState.Mode == GameMode.Combat ? Vector2.right : direction.LastValid;
            Sprite[] row;
            Vector2 torchPosition;
            int torchOrder;
            bool faceLeft = false;
            if (Mathf.Abs(look.x) > Mathf.Abs(look.y))
            {
                row = walkSide;
                faceLeft = look.x < 0f;
                torchPosition = new Vector2(faceLeft ? -torchSide.x : torchSide.x, torchSide.y);
                torchOrder = -1;   // the back hand is behind the body
            }
            else if (look.y > 0f) { row = walkUp; torchPosition = torchBack; torchOrder = -1; }
            else { row = walkDown; torchPosition = torchFront; torchOrder = 1; }

            // 2. Which frame? Moving = advance frames; still = frame 0.
            bool moving = movement.Velocity.sqrMagnitude > 0.05f && GameState.IsExploring;
            int frame = 0;
            if (moving)
            {
                float speed = movement.IsSprinting ? framesPerSecond * sprintSpeedUp : framesPerSecond;
                frameTimer += Time.deltaTime * speed;
                frame = (int)frameTimer % row.Length;
            }
            else frameTimer = 0f;

            sprite.sprite = row[frame];
            sprite.flipX = faceLeft;

            // 3. The torch follows the hand (it bobs a little with the steps).
            if (torch != null)
            {
                float bob = moving && frame % 2 == 1 ? 0.04f : 0f;
                torch.localPosition = new Vector3(torchPosition.x, torchPosition.y + bob, 0f);
                if (torchFlame != null) torchFlame.sortingOrder = torchOrder;
            }
        }
    }
}
