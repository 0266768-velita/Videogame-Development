using UnityEngine;

/// <summary>
/// Gives a character its pixel-art body: picks the sprite sheet, sizes/positions the "Model" child so the feet
/// sit at the right place, adds a ground shadow and animates frames (walk / idle) in code.
/// Works on the player (3 rows: down, up, side) and on every enemy (1 row).
/// Runs before FakeHeight so the jump/squash code sees the final sprite.
/// </summary>
[DefaultExecutionOrder(-100)]
public class CharacterVisuals : MonoBehaviour
{
    [Header("Used when there is no EnemyController (the player)")]
    public string sheet = "tlahuelli";
    public int cellWidth = 18;
    public int cellHeight = 26;
    public int frames = 4;
    public float feetOffset = 0.4f;
    public bool directional = true;     // rows: 0 down, 1 up, 2 side (faces right)
    public bool walkAnimated = true;

    Transform model;
    SpriteRenderer sr;
    Sprite[][] rowFrames;
    Rigidbody2D rb;
    Player2D player;
    EnemyController enemy;
    ActorFeedback fx;
    float timer;
    int frame;

    void Awake()
    {
        enemy = GetComponent<EnemyController>();
        player = GetComponent<Player2D>();
        rb = GetComponent<Rigidbody2D>();
        fx = GetComponent<ActorFeedback>();

        model = transform.Find("Model");
        if (model == null)
        {
            model = new GameObject("Model").transform;
            model.SetParent(transform, false);
        }
        sr = model.GetComponent<SpriteRenderer>();
        if (sr == null) sr = model.gameObject.AddComponent<SpriteRenderer>();

        float shadowW = 1f;
        if (enemy != null && enemy.data != null && !string.IsNullOrEmpty(enemy.data.spriteSheet))
        {
            var d = enemy.data;
            sheet = d.spriteSheet; cellWidth = d.frameWidth; cellHeight = d.frameHeight;
            feetOffset = d.feetOffset; directional = false;
            walkAnimated = d.archetype == EnemyArchetype.Giant || d.archetype == EnemyArchetype.Hound;
            frames = 4;
        }

        int rowCount = directional ? 3 : 1;
        rowFrames = new Sprite[rowCount][];
        for (int r = 0; r < rowCount; r++) rowFrames[r] = SpriteLib.Row(sheet, cellWidth, cellHeight, r, frames);

        sr.sprite = rowFrames[0][0];
        sr.color = Color.white;
        float h = cellHeight / SpriteLib.PPU, w = cellWidth / SpriteLib.PPU;
        model.localScale = Vector3.one;
        model.localPosition = new Vector3(0f, -feetOffset + h * 0.5f, 0f);
        shadowW = Mathf.Max(0.8f, w * 0.75f);

        // FakeHeight is created here (not lazily) so every character has a shadow and a correct feet anchor.
        var fh = GetComponent<FakeHeight>();
        if (fh == null) fh = gameObject.AddComponent<FakeHeight>();
        fh.shadowOffset = new Vector2(0f, -feetOffset);
        fh.shadowWidth = shadowW;
    }

    void Update()
    {
        if (sr == null || rowFrames == null) return;

        bool combat = GameState.Mode == GameMode.Combat;
        float speed = rb != null && rb.simulated ? rb.linearVelocity.magnitude : 0f;
        bool moving = speed > 0.15f && GameState.Mode == GameMode.Exploration;

        int row = 0;
        if (directional && player != null)
        {
            Vector2 f = combat ? Vector2.right : player.Facing;
            if (Mathf.Abs(f.x) > 0.5f) { row = 2; }
            else row = f.y > 0f ? 1 : 0;
            if (row != 2) sr.flipX = false;
            else if (combat) sr.flipX = false;
        }

        var list = rowFrames[Mathf.Min(row, rowFrames.Length - 1)];
        float frameTime = directional ? Mathf.Lerp(0.16f, 0.09f, Mathf.Clamp01(speed / 6f)) : (moving ? 0.14f : 0.45f);

        timer += Time.deltaTime;
        if (timer >= frameTime)
        {
            timer = 0f;
            frame++;
        }

        int idx;
        if (directional) idx = moving ? frame % list.Length : 0;
        else if (walkAnimated) idx = moving ? frame % list.Length : (frame % 2);      // breathe between 2 frames
        else idx = frame % list.Length;                                                // floaters/bosses: always loop
        sr.sprite = list[Mathf.Clamp(idx, 0, list.Length - 1)];
    }
}
