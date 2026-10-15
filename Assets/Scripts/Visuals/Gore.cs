using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Blood and dismemberment (the game is rated 17+). Everything is made in code from pixel sprites:
///  - Gore.Splatter: blood spray + stains when something is hit.
///  - Gore.Dismember: when an enemy dies, its real sprite is cut into pieces (with bloody cut edges) that fly
///    apart, bounce and stay on the floor together with gibs, bones and a blood pool.
/// Turn it off in Options (Gore: Off) and enemies simply fade away.
/// </summary>
public static class Gore
{
    const string Pref = "mictlan_gore";
    public static bool Enabled
    {
        get => PlayerPrefs.GetInt(Pref, 1) == 1;
        set { PlayerPrefs.SetInt(Pref, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    /// <summary>Blood spray at a world position, mostly in direction 'dir'.</summary>
    public static void Splatter(Vector3 worldPos, Vector2 dir, int droplets, float power = 1f)
    {
        if (!Enabled || droplets <= 0) return;
        var run = GoreRunner.Get();
        Vector2 ground = new Vector2(worldPos.x, worldPos.y - 0.6f);
        float h0 = 0.6f;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;
        dir.Normalize();
        for (int i = 0; i < droplets; i++)
        {
            Vector2 v = (dir + Random.insideUnitCircle * 0.9f).normalized * Random.Range(1.5f, 6f) * power;
            run.SpawnDroplet(ground, h0 + Random.Range(-0.2f, 0.4f), v, Random.Range(1.5f, 5f) * power);
        }
        int stains = Mathf.Clamp(droplets / 5, 1, 4);
        for (int i = 0; i < stains; i++)
            run.SpawnDecal(SpriteLib.Single("blood_" + Random.Range(0, 3)), ground + dir * Random.Range(0.4f, 1.8f) + Random.insideUnitCircle * 0.5f,
                Random.Range(0.6f, 1.1f), Random.value * 360f);
    }

    /// <summary>The enemy explodes into pieces. The enemy GameObject is hidden (the caller destroys it later).</summary>
    public static void Dismember(EnemyController e)
    {
        var d = e.data;
        var sr = e.transform.Find("Model") != null ? e.transform.Find("Model").GetComponent<SpriteRenderer>() : null;
        Vector3 center = sr != null ? sr.bounds.center : e.transform.position;
        float groundY = e.transform.position.y - d.feetOffset;
        var run = GoreRunner.Get();
        float amount = Mathf.Max(0.3f, d.goreAmount);

        // 1) Pieces of the real sprite
        if (sr != null && sr.sprite != null)
        {
            var src = sr.sprite;
            int cols = Mathf.Max(1, d.gorePieceCols), rows = Mathf.Max(1, d.gorePieceRows);
            Rect rect = src.rect;
            int rw = Mathf.RoundToInt(rect.width), rh = Mathf.RoundToInt(rect.height);
            Color[] px = null;
            if (src.texture != null && src.texture.isReadable)
                px = src.texture.GetPixels(Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y), rw, rh);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int x0 = c * rw / cols, x1 = (c + 1) * rw / cols;
                    int yTop = rh - r * rh / rows, yBot = rh - (r + 1) * rh / rows;   // y grows upward
                    int pw = x1 - x0, ph = yTop - yBot;
                    if (pw < 2 || ph < 2) continue;

                    Sprite piece;
                    if (px != null)
                    {
                        piece = BuildPiece(src, px, rw, rh, x0, yBot, pw, ph, out bool any);
                        if (!any) continue;
                    }
                    else piece = Sprite.Create(src.texture, new Rect(rect.x + x0, rect.y + yBot, pw, ph), new Vector2(0.5f, 0.5f), SpriteLib.PPU);

                    Vector2 off = new Vector2((x0 + pw * 0.5f - rw * 0.5f), (yBot + ph * 0.5f - rh * 0.5f)) / SpriteLib.PPU;
                    if (sr.flipX) off.x = -off.x;
                    Vector2 wp = (Vector2)center + off;
                    float h = Mathf.Max(0.3f, wp.y - groundY);
                    Vector2 outward = off.sqrMagnitude > 0.01f ? off.normalized : Random.insideUnitCircle.normalized;
                    Vector2 v = (outward + Random.insideUnitCircle * 0.5f).normalized * Random.Range(2.5f, 5.5f);
                    run.SpawnPiece(piece, sr.flipX, new Vector2(wp.x, groundY), h, v, Random.Range(3f, 7f), Random.Range(-420f, 420f), null, false, px != null);
                }
            }
        }

        Vector2 groundPos = new Vector2(center.x, groundY);
        // 2) Gibs (flesh, bones, organs, cloth scraps)
        int gibs = Mathf.RoundToInt(Random.Range(9, 15) * amount);
        Color cloth = d.archetype == EnemyArchetype.Witch ? new Color(0.55f, 0.25f, 0.7f)
                    : d.archetype == EnemyArchetype.Giant ? new Color(0.7f, 0.15f, 0.15f)
                    : d.archetype == EnemyArchetype.Boss ? new Color(0.2f, 0.75f, 0.55f) : new Color(0.8f, 0.8f, 0.75f);
        for (int i = 0; i < gibs; i++)
        {
            int kind = Random.Range(0, 4);
            var spr = SpriteLib.Single($"gib_{kind}_{Random.Range(0, 3)}");
            Vector2 v = Random.insideUnitCircle.normalized * Random.Range(1.5f, 7f);
            run.SpawnPiece(spr, Random.value < 0.5f, groundPos + Random.insideUnitCircle * 0.4f, Random.Range(0.4f, 1.6f), v,
                Random.Range(4f, 9f), Random.Range(-720f, 720f), kind == 3 ? cloth : Color.white, true);
        }

        // 3) Blood: burst of droplets, stains around and a pool under the body
        int drops = Mathf.RoundToInt(Random.Range(45, 70) * amount);
        for (int i = 0; i < drops; i++)
        {
            Vector2 v = Random.insideUnitCircle.normalized * Random.Range(1f, 7.5f);
            run.SpawnDroplet(groundPos, Random.Range(0.3f, 1.8f), v, Random.Range(2f, 8f));
        }
        for (int i = 0; i < Mathf.RoundToInt(8 * amount); i++)
            run.SpawnDecal(SpriteLib.Single("blood_" + Random.Range(0, 6)), groundPos + Random.insideUnitCircle * (1.2f + amount), Random.Range(0.7f, 1.4f), Random.value * 360f);
        run.SpawnDecal(SpriteLib.Single("pool_" + Random.Range(0, 3)), groundPos, Mathf.Clamp(0.9f + amount * 0.5f, 1f, 2.4f), Random.Range(-10f, 10f), true);

        Audio.Play("gib", 1f, 0.05f);
        foreach (var s in e.GetComponentsInChildren<SpriteRenderer>(true)) s.enabled = false;
    }

    // ---------------------------------------------------------------------
    /// <summary>Copies a rectangle of the sprite and paints a raw red cut edge where it was joined to another piece.</summary>
    static Sprite BuildPiece(Sprite src, Color[] px, int rw, int rh, int x0, int y0, int pw, int ph, out bool any)
    {
        var tex = new Texture2D(pw, ph, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var outPx = new Color[pw * ph];
        int opaque = 0;
        Color cutA = new Color(0.62f, 0.04f, 0.08f, 1f), cutB = new Color(0.85f, 0.14f, 0.18f, 1f);
        for (int y = 0; y < ph; y++)
        {
            for (int x = 0; x < pw; x++)
            {
                int sx = x0 + x, sy = y0 + y;
                Color c = px[sy * rw + sx];
                if (c.a > 0.5f)
                {
                    opaque++;
                    bool cut =
                        (x == 0 && sx > 0 && px[sy * rw + sx - 1].a > 0.5f) ||
                        (x == pw - 1 && sx < rw - 1 && px[sy * rw + sx + 1].a > 0.5f) ||
                        (y == 0 && sy > 0 && px[(sy - 1) * rw + sx].a > 0.5f) ||
                        (y == ph - 1 && sy < rh - 1 && px[(sy + 1) * rw + sx].a > 0.5f);
                    if (cut) c = ((x + y) & 1) == 0 ? cutA : cutB;
                }
                outPx[y * pw + x] = c;
            }
        }
        any = opaque > 8;
        tex.SetPixels(outPx);
        tex.Apply(false, false);
        return Sprite.Create(tex, new Rect(0, 0, pw, ph), new Vector2(0.5f, 0.5f), SpriteLib.PPU);
    }
}

/// <summary>Scene object that owns all the blood: spawns, caps the number of stains, runs the tiny physics.</summary>
public class GoreRunner : MonoBehaviour
{
    static GoreRunner instance;
    public static GoreRunner Get()
    {
        if (instance == null) instance = new GameObject("Gore").AddComponent<GoreRunner>();
        return instance;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { instance = null; }

    const int MaxPersistent = 420;
    const int DecalOrder = -31400;
    readonly Queue<GameObject> persistent = new Queue<GameObject>();
    int orderCounter;

    void Awake() { instance = this; }

    void Remember(GameObject go)
    {
        persistent.Enqueue(go);
        while (persistent.Count > MaxPersistent)
        {
            var old = persistent.Dequeue();
            if (old != null) Destroy(old);
        }
    }

    int NextOrder() { orderCounter = (orderCounter + 1) % 900; return DecalOrder + orderCounter; }

    public void SpawnDecal(Sprite sprite, Vector2 pos, float scale, float rot, bool under = false)
    {
        if (Hazards.KindAt(pos) != TerrainKind.Ground) return;
        var go = new GameObject("Stain");
        go.transform.SetParent(transform, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0, 0, rot);
        go.transform.localScale = Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = under ? DecalOrder - 50 : NextOrder();
        Remember(go);
    }

    public void SpawnDroplet(Vector2 ground, float height, Vector2 vel, float vh)
    {
        var go = new GameObject("Drop");
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteLib.White();
        float size = Random.Range(0.06f, 0.16f);
        go.transform.localScale = Vector3.one * size;
        float k = Random.Range(0.55f, 1f);
        sr.color = new Color(0.55f * k + 0.2f, 0.03f, 0.06f, 1f);
        sr.sortingOrder = 6000;
        var bit = go.AddComponent<GoreBit>();
        bit.Init(this, ground, height, vel, vh, 0f, GoreBit.Kind.Droplet, 0);
    }

    public void SpawnPiece(Sprite sprite, bool flipX, Vector2 ground, float height, Vector2 vel, float vh, float spin, Color? tint = null, bool gib = false, bool ownsTexture = false)
    {
        var go = new GameObject(gib ? "Gib" : "Piece");
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.flipX = flipX;
        sr.color = tint ?? Color.white;
        sr.sortingOrder = 6000;
        var bit = go.AddComponent<GoreBit>();
        bit.Init(this, ground, height, vel, vh, spin, gib ? GoreBit.Kind.Gib : GoreBit.Kind.Piece, gib ? 1 : 2);
        bit.ownedTexture = ownsTexture ? sprite.texture : null;
    }

    public void Settle(GoreBit bit)
    {
        var sr = bit.GetComponent<SpriteRenderer>();
        sr.sortingOrder = DecalOrder + 900 + (orderCounter++ % 90);
        Remember(bit.gameObject);
    }
}

/// <summary>One flying drop / gib / body piece. Fake height (z) over a ground position, like FakeHeight.</summary>
public class GoreBit : MonoBehaviour
{
    public enum Kind { Droplet, Gib, Piece }
    public Texture2D ownedTexture;

    GoreRunner runner;
    Vector2 ground, vel;
    float height, vh, spin, trail;
    Kind kind;
    int bounces;
    bool settled;

    public void Init(GoreRunner r, Vector2 g, float h, Vector2 v, float vUp, float spinDeg, Kind k, int bounceCount)
    {
        runner = r; ground = g; height = h; vel = v; vh = vUp; spin = spinDeg; kind = k; bounces = bounceCount;
        Apply();
    }

    void Update()
    {
        if (settled) return;
        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        ground += vel * dt;
        height += vh * dt;
        vh -= 26f * dt;
        transform.Rotate(0, 0, spin * dt);

        if (kind == Kind.Piece)
        {
            trail -= dt;
            if (trail <= 0f && height > 0.2f) { trail = 0.05f; runner.SpawnDroplet(ground, height, vel * 0.2f + Random.insideUnitCircle * 0.8f, 0.5f); }
        }

        if (height <= 0f)
        {
            height = 0f;
            var tk = Hazards.KindAt(ground);
            if (tk != TerrainKind.Ground && kind != Kind.Droplet) { Audio.Play("splash", 0.4f, 0.1f); Destroy(gameObject); return; }

            if (kind == Kind.Droplet)
            {
                if (tk == TerrainKind.Ground && Random.value < 0.6f)
                    runner.SpawnDecal(SpriteLib.Single("blood_" + Random.Range(0, 3)), ground, Random.Range(0.18f, 0.4f), Random.value * 360f);
                Destroy(gameObject);
                return;
            }
            if (bounces > 0 && vh < -2.5f)
            {
                bounces--;
                vh = -vh * 0.38f;
                vel *= 0.55f;
                spin *= 0.5f;
                for (int i = 0; i < 3; i++) runner.SpawnDroplet(ground, 0.1f, Random.insideUnitCircle * 2f, 2f);
                runner.SpawnDecal(SpriteLib.Single("blood_" + Random.Range(0, 3)), ground, Random.Range(0.5f, 0.9f), Random.value * 360f);
            }
            else
            {
                settled = true;
                vel = Vector2.zero;
                runner.Settle(this);
            }
        }
        Apply();
    }

    void Apply()
    {
        transform.position = new Vector3(ground.x, ground.y + height, 0f);
    }

    void OnDestroy()
    {
        if (ownedTexture != null) Destroy(ownedTexture);
    }
}
