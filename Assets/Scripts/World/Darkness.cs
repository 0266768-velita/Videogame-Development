using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dark atmosphere: a vignette that stays dark at the edges and around the world, with a soft "light"
/// that follows Tlahuelli. Drawn under the HUD and the minimap.
/// </summary>
public class Darkness : MonoBehaviour
{
    [Range(0f, 1f)] public float ambient = 0.30f;     // darkness everywhere
    [Range(0f, 1f)] public float edge = 0.80f;        // darkness far from Tlahuelli
    public float lightRadius = 0.22f;                 // clear radius (fraction of the gradient texture)

    RawImage img;
    Texture2D tex;
    Camera cam;
    Player2D player;

    void Awake()
    {
        var content = UIKit.CreateScreen("DarknessCanvas", 2, transform);
        img = UIKit.NewRawImage("Vignette", content, null);
        UIKit.Stretch(img.rectTransform);
        img.raycastTarget = false;

        const int n = 128;
        tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                float k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(lightRadius, 1f, d));
                float a = Mathf.Lerp(ambient, edge, k);
                tex.SetPixel(x, y, new Color(0.02f, 0.01f, 0.06f, a));
            }
        tex.Apply();
        img.texture = tex;
        img.color = Color.white;
    }

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (player == null) player = FindFirstObjectByType<Player2D>();
        if (cam == null || player == null) return;

        // Center the light on the player (it is not always in the middle of the screen near map edges).
        Vector3 vp = cam.WorldToViewportPoint(player.transform.position);
        const float scale = 2.2f;   // the gradient texture covers 2.2 screens, so the light radius is generous
        img.uvRect = new Rect(0.5f - vp.x * scale, 0.5f - vp.y * scale, scale, scale);
    }

    void OnDestroy() { if (tex != null) Destroy(tex); }
}
