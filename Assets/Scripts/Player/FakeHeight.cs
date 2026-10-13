using UnityEngine;

/// <summary>
/// Fake "height" for a top-down 2D character: lifts the visual (child "Model") up, squashes/stretches it,
/// and shrinks a blob shadow that stays on the ground. The root, collider and physics never move,
/// so jumping does not break collisions or YSort.
///
/// Added automatically at runtime by Player2D / ActorFeedback, so existing prefabs do not need editing.
/// If you later animate the Model child's position/scale with an Animator, put the sprite one level
/// deeper (Model/Sprite) and drag the "Model" object into the Visual field so the two do not fight.
/// </summary>
[DisallowMultipleComponent]
public class FakeHeight : MonoBehaviour
{
    [Tooltip("Object that is lifted/squashed. Empty = child named 'Model', or the first child.")]
    public Transform visual;

    [Header("Shadow")]
    public bool createShadow = true;
    public Vector2 shadowOffset = new Vector2(0f, -0.4f);
    public float shadowWidth = 0.9f;
    [Range(0f, 1f)] public float shadowAlpha = 0.35f;
    [Tooltip("At this height (tiles) the shadow is at its smallest/faintest.")]
    public float shadowFadeHeight = 2f;

    /// <summary>Height above the ground in tiles.</summary>
    public float Height { get; set; }
    /// <summary>Scale multiplier (x = width, y = height). (1,1) = normal.</summary>
    public Vector2 Squash { get; set; } = Vector2.one;
    /// <summary>Extra small vertical offset (walking bob).</summary>
    public float Bob { get; set; }

    Vector3 baseLocalPos, baseLocalScale;
    float halfSpriteHeight;
    bool hasVisual;
    Transform shadow;
    SpriteRenderer shadowSr, visualSr;
    static Sprite circle;

    void Awake()
    {
        if (visual == null)
        {
            visual = transform.Find("Model");
            if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
        }
        if (visual == null) return;

        hasVisual = true;
        baseLocalPos = visual.localPosition;
        baseLocalScale = visual.localScale;
        visualSr = visual.GetComponentInChildren<SpriteRenderer>();
        if (visualSr != null && visualSr.sprite != null)
            halfSpriteHeight = visualSr.sprite.bounds.size.y * baseLocalScale.y * 0.5f;
    }

    // Shadow is created in Start (after YSort.Awake) so YSort does not take it over.
    void Start()
    {
        if (!createShadow) return;
        var go = new GameObject("Shadow");
        shadow = go.transform;
        shadow.SetParent(transform, false);
        shadow.localPosition = shadowOffset;
        shadowSr = go.AddComponent<SpriteRenderer>();
        shadowSr.sprite = GetCircle();
        shadowSr.color = new Color(0f, 0f, 0f, shadowAlpha);
        ApplyShadow();
    }

    void LateUpdate()
    {
        if (hasVisual)
        {
            // Anchor at the feet: when squashing around the sprite's center, push it down/up so the feet stay put.
            float feetFix = (Squash.y - 1f) * halfSpriteHeight;
            visual.localPosition = baseLocalPos + new Vector3(0f, Height + Bob + feetFix, 0f);
            visual.localScale = new Vector3(baseLocalScale.x * Squash.x, baseLocalScale.y * Squash.y, baseLocalScale.z);
        }
        ApplyShadow();
    }

    void ApplyShadow()
    {
        if (shadow == null) return;
        float k = Mathf.Clamp01(Height / Mathf.Max(0.01f, shadowFadeHeight));
        float w = shadowWidth * Mathf.Lerp(1f, 0.55f, k) * Mathf.Lerp(1f, Squash.x, 0.5f);
        shadow.localScale = new Vector3(w, w * 0.45f, 1f);
        var c = shadowSr.color; c.a = shadowAlpha * Mathf.Lerp(1f, 0.4f, k); shadowSr.color = c;
        if (visualSr != null) shadowSr.sortingOrder = visualSr.sortingOrder - 1;
    }

    public void ResetVisual()
    {
        Height = 0f; Bob = 0f; Squash = Vector2.one;
    }

    static Sprite GetCircle()
    {
        if (circle != null) return circle;
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                float a = Mathf.Clamp01((1f - d) * 4f);   // soft edge
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        return circle;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { circle = null; }
}
