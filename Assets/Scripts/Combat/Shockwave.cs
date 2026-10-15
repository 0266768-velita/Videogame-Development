using UnityEngine;

/// <summary>
/// Expanding ground ring (flattened to look top-down). Used by the Seismic Jump.
/// Shockwave.Spawn(position, radius, duration, color) creates it and it destroys itself.
/// </summary>
public class Shockwave : MonoBehaviour
{
    const int Segments = 56;
    LineRenderer lr;
    float radius, duration, t;
    Color color;

    public static Shockwave Spawn(Vector3 position, float radius, float duration, Color color)
    {
        var go = new GameObject("Shockwave");
        go.transform.position = new Vector3(position.x, position.y, 0f);
        var s = go.AddComponent<Shockwave>();
        s.radius = radius; s.duration = Mathf.Max(0.05f, duration); s.color = color;
        s.Build();
        return s;
    }

    void Build()
    {
        lr = gameObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = Segments;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = -30000;   // above the ground, below characters and props
        Apply(0f);
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / duration);
        Apply(k);
        if (k >= 1f) { Destroy(lr.material); Destroy(gameObject); }
    }

    void Apply(float k)
    {
        float r = radius * (1f - Mathf.Pow(1f - k, 3f));   // fast at first, slows down
        for (int i = 0; i < Segments; i++)
        {
            float a = i / (float)Segments * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r * 0.55f, 0f));  // 0.55 = top-down squash
        }
        float alpha = 1f - k;
        var c = new Color(color.r, color.g, color.b, alpha);
        lr.startColor = c; lr.endColor = c;
        lr.widthMultiplier = Mathf.Lerp(0.5f, 0.08f, k);
    }
}
