using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Small white shapes for the combat effects, generated once in code (no external tools) and
    /// tinted per effect: soft circle (dust, glow), ring (shockwaves), pixel (debris, sparks),
    /// crescent (slashes), crack (ground impact) and diamond (soul wisps).
    /// Replace them with Aseprite art later by assigning sprites to Vfx.Sprites.
    /// </summary>
    public static class VfxSprites
    {
        const float PixelsPerUnit = 64f;
        static Sprite soft, ring, pixel, crescent, crack, diamond;

        public static Sprite Soft => soft ??= Make(32, FilterMode.Bilinear, (x, y, r, a) => Mathf.Clamp01(1f - r) * Mathf.Clamp01(1f - r));
        public static Sprite Ring => ring ??= Make(64, FilterMode.Bilinear, (x, y, r, a) => Mathf.Clamp01(1f - Mathf.Abs(r - 0.88f) / 0.1f));
        public static Sprite Pixel => pixel ??= Make(4, FilterMode.Point, (x, y, r, a) => 1f);
        public static Sprite Diamond => diamond ??= Make(9, FilterMode.Point, (x, y, r, a) =>
            Mathf.Abs(x - 4) + Mathf.Abs(y - 4) <= 4 ? 1f : 0f);

        /// <summary>Crescent slash: a thick arc on the right half, thin at the tips.</summary>
        public static Sprite Crescent => crescent ??= Make(64, FilterMode.Point, (x, y, r, a) =>
        {
            if (x < 32) return 0f;
            float t = Mathf.Abs(a) / (Mathf.PI * 0.5f);          // 0 at the middle of the arc, 1 at the tips
            float thickness = Mathf.Lerp(0.22f, 0.02f, t);
            return r > 0.95f - thickness && r < 0.95f ? 1f : 0f;
        });

        /// <summary>Star of cracks for the ground impact.</summary>
        public static Sprite Crack => crack ??= Make(96, FilterMode.Point, (x, y, r, a) =>
        {
            if (r > 0.98f) return 0f;
            // 7 jagged rays: a pixel belongs to a ray if its angle is close to the ray (wobbling with the radius).
            for (int i = 0; i < 7; i++)
            {
                float ray = i * Mathf.PI * 2f / 7f + 0.35f * Mathf.Sin(i * 3.1f);
                float wobble = 0.12f * Mathf.Sin(r * 18f + i);
                float diff = Mathf.DeltaAngle(a * Mathf.Rad2Deg, (ray + wobble) * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                float width = Mathf.Lerp(0.09f, 0.015f, r);
                if (Mathf.Abs(diff) * Mathf.Max(r, 0.15f) < width * 0.5f) return 1f;
            }
            return r < 0.12f ? 1f : 0f;
        });

        delegate float Shape(int x, int y, float radius01, float angle);

        static Sprite Make(int size, FilterMode filter, Shape shape)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = filter, wrapMode = TextureWrapMode.Clamp };
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - c) / (size * 0.5f), dy = (y - c) / (size * 0.5f);
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(shape(x, y, r, Mathf.Atan2(dy, dx)));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            tex.Apply();
            tex.hideFlags = HideFlags.DontSave;
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { soft = ring = pixel = crescent = crack = diamond = null; }
    }
}
