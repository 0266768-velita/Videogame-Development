using UnityEngine;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// Painting tools for the detailed art (comic and temple): shapes with light and shadow (spheres and
    /// tapered tubes), filled polygons, glows, dithered skies and a vignette. The light comes from LightDir.
    /// </summary>
    public static partial class PropArt
    {
        /// <summary>Where the light comes from (x right, y DOWN, z towards the viewer). Change it per picture.</summary>
        static Vector3 LightDir = new Vector3(-0.55f, -0.6f, 0.58f).normalized;

        static void SetLight(float x, float y, float z) => LightDir = new Vector3(x, y, z).normalized;

        static Color32 Tone(Color32 c, float k) =>
            new Color32((byte)Mathf.Clamp(c.r * k, 0, 255), (byte)Mathf.Clamp(c.g * k, 0, 255), (byte)Mathf.Clamp(c.b * k, 0, 255), c.a);

        static Color32 Mix(Color32 a, Color32 b, float t) => Color32.Lerp(a, b, t);

        /// <summary>Four tones (deep shadow, shadow, base, highlight) from how much a surface faces the light.</summary>
        static Color32 Lit(Color32 baseColor, Vector3 normal, int x, int y)
        {
            float k = Vector3.Dot(normal, LightDir) + (PixelCanvas.Hash(x, y, 7) - 0.5f) * 0.1f;   // dither the edges
            if (k < 0.05f) return Tone(baseColor, 0.5f);
            if (k < 0.4f) return Tone(baseColor, 0.72f);
            if (k < 0.82f) return baseColor;
            return Mix(baseColor, new Color32(255, 246, 225, 255), 0.3f);
        }

        /// <summary>An ellipse shaded like a ball (heads, shoulders, pots, knuckles).</summary>
        static void Ball(PixelCanvas c, float cx, float cy, float rx, float ry, Color32 color)
        {
            for (int x = Mathf.FloorToInt(cx - rx); x <= Mathf.CeilToInt(cx + rx); x++)
                for (int y = Mathf.FloorToInt(cy - ry); y <= Mathf.CeilToInt(cy + ry); y++)
                {
                    float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry, d = dx * dx + dy * dy;
                    if (d > 1f) continue;
                    c.Set(x, y, Lit(color, new Vector3(dx, dy, Mathf.Sqrt(1f - d)), x, y));
                }
        }

        /// <summary>A tube from (x0,y0) to (x1,y1) whose radius goes from r0 to r1, shaded round (limbs, fingers, columns).</summary>
        static void Tube(PixelCanvas c, float x0, float y0, float x1, float y1, float r0, float r1, Color32 color)
        {
            var a = new Vector2(x0, y0); var b = new Vector2(x1, y1);
            Vector2 ab = b - a; float len2 = Mathf.Max(ab.sqrMagnitude, 0.0001f);
            float r = Mathf.Max(r0, r1);
            for (int x = Mathf.FloorToInt(Mathf.Min(x0, x1) - r); x <= Mathf.CeilToInt(Mathf.Max(x0, x1) + r); x++)
                for (int y = Mathf.FloorToInt(Mathf.Min(y0, y1) - r); y <= Mathf.CeilToInt(Mathf.Max(y0, y1) + r); y++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
                    Vector2 off = p - (a + ab * t);
                    float radius = Mathf.Lerp(r0, r1, t);
                    float d = off.magnitude / radius;
                    if (d > 1f) continue;
                    Vector2 n2 = off / radius;
                    c.Set(x, y, Lit(color, new Vector3(n2.x, n2.y, Mathf.Sqrt(Mathf.Max(0f, 1f - d * d))), x, y));
                }
        }

        /// <summary>Fills a polygon (points in order).</summary>
        static void Poly(PixelCanvas c, Color32 color, params Vector2[] pts)
        {
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            foreach (var p in pts) { minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x); minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y); }
            for (int x = Mathf.FloorToInt(minX); x <= Mathf.CeilToInt(maxX); x++)
                for (int y = Mathf.FloorToInt(minY); y <= Mathf.CeilToInt(maxY); y++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    bool inside = false;
                    for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
                        if ((pts[i].y > py) != (pts[j].y > py) &&
                            px < (pts[j].x - pts[i].x) * (py - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x)
                            inside = !inside;
                    if (inside) c.Set(x, y, color);
                }
        }

        static Vector2 V(float x, float y) => new Vector2(x, y);

        /// <summary>Soft light painted on top (fire, moon, magic).</summary>
        static void Glow(PixelCanvas c, float cx, float cy, float radius, Color32 color, float strength)
        {
            for (int x = Mathf.FloorToInt(cx - radius); x <= Mathf.CeilToInt(cx + radius); x++)
                for (int y = Mathf.FloorToInt(cy - radius); y <= Mathf.CeilToInt(cy + radius); y++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)) / radius;
                    if (d >= 1f || c.IsEmpty(x, y)) continue;
                    float k = (1f - d) * (1f - d) * strength;
                    // Stepped, so it still looks like pixel art.
                    k = Mathf.Floor(k * 5f + PixelCanvas.Hash(x, y, 3) * 0.6f) / 5f;
                    if (k > 0f) c.Blend(x, y, color, Mathf.Min(1f, k));
                }
        }

        static readonly float[] Bayer = { 0f, 8f, 2f, 10f, 12f, 4f, 14f, 6f, 3f, 11f, 1f, 9f, 15f, 7f, 13f, 5f };

        /// <summary>A sky or wall gradient in a few colour bands mixed with an ordered dither (classic pixel art).</summary>
        static void Sky(PixelCanvas c, int x, int y, int w, int h, Color32 top, Color32 bottom, int bands = 6)
        {
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                {
                    float t = h <= 1 ? 0f : j / (float)(h - 1) * bands;
                    float threshold = Bayer[(j & 3) * 4 + (i & 3)] / 16f;
                    int band = Mathf.Min(bands, Mathf.FloorToInt(t) + (t - Mathf.Floor(t) > threshold ? 1 : 0));
                    c.Set(x + i, y + j, Color32.Lerp(top, bottom, band / (float)bands));
                }
        }

        /// <summary>Copies every painted pixel of a layer on top of the picture.</summary>
        static void Stamp(PixelCanvas target, PixelCanvas layer)
        {
            for (int x = 0; x < layer.W; x++)
                for (int y = 0; y < layer.H; y++)
                    if (!layer.IsEmpty(x, y)) target.Set(x, y, layer.Get(x, y));
        }

        /// <summary>Darkens the corners so the eye goes to the middle.</summary>
        static void Vignette(PixelCanvas c, float strength)
        {
            var dark = C(6, 4, 6);
            for (int x = 0; x < c.W; x++)
                for (int y = 0; y < c.H; y++)
                {
                    float dx = (x - c.W / 2f) / (c.W / 2f), dy = (y - c.H / 2f) / (c.H / 2f);
                    float k = Mathf.Clamp01((dx * dx + dy * dy - 0.45f) * strength);
                    k = Mathf.Floor(k * 4f + PixelCanvas.Hash(x, y, 9) * 0.5f) / 4f;
                    if (k > 0f) c.Blend(x, y, dark, Mathf.Min(0.85f, k));
                }
        }

        /// <summary>Stars: single bright pixels and a few crosses.</summary>
        static void Stars(PixelCanvas c, int seed, int count, int maxY)
        {
            var random = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                int x = random.Next(c.W), y = random.Next(maxY);
                var star = random.Next(4) == 0 ? C(255, 240, 200) : C(170, 180, 220);
                c.Set(x, y, star);
                if (random.Next(8) == 0) { c.Set(x - 1, y, Tone(star, 0.6f)); c.Set(x + 1, y, Tone(star, 0.6f)); c.Set(x, y - 1, Tone(star, 0.6f)); c.Set(x, y + 1, Tone(star, 0.6f)); }
            }
        }

        // ---------------- A person with volume (Tlahuelli and others) ----------------

        enum Pose { Standing, Kneeling, ArmsUp, FromBehind }

        static readonly Color32 Skin = new Color32(160, 104, 70, 255);

        /// <summary>A shaded person about 'height' pixels tall, feet on 'feet'.</summary>
        static void Figure(PixelCanvas c, float x, float feet, float height, Pose pose, Color32 cloth, Color32 sash)
        {
            float s = height / 40f;
            float top = pose == Pose.Kneeling ? feet - 30f * s : feet - 40f * s;
            float shoulder = top + 10f * s, hip = top + 22f * s;

            // Legs.
            if (pose == Pose.Kneeling)
            {
                Tube(c, x - 1f * s, hip, x + 6f * s, feet - 2f * s, 2.6f * s, 2.3f * s, Skin);   // thigh forward
                Tube(c, x + 6f * s, feet - 2f * s, x - 4f * s, feet - 1f * s, 2.2f * s, 1.8f * s, Skin);   // shin back on the ground
            }
            else
            {
                Tube(c, x - 2f * s, hip, x - 2.5f * s, feet - 1f, 2.4f * s, 1.8f * s, Skin);
                Tube(c, x + 2f * s, hip, x + 2.5f * s, feet - 1f, 2.4f * s, 1.8f * s, Skin);
                Ball(c, x - 2.8f * s, feet - 0.5f, 2.2f * s, 1.1f * s, Tone(Skin, 0.8f));
                Ball(c, x + 2.8f * s, feet - 0.5f, 2.2f * s, 1.1f * s, Tone(Skin, 0.8f));
            }

            // Arms behind the body when seen from behind are hidden by the cloak.
            void Arms()
            {
                if (pose == Pose.ArmsUp)
                {
                    Tube(c, x - 4f * s, shoulder + 1f, x - 9f * s, top - 2f * s, 2f * s, 1.6f * s, Skin);
                    Tube(c, x + 4f * s, shoulder + 1f, x + 9f * s, top - 2f * s, 2f * s, 1.6f * s, Skin);
                    Ball(c, x - 9.5f * s, top - 3f * s, 1.8f * s, 1.8f * s, Skin);
                    Ball(c, x + 9.5f * s, top - 3f * s, 1.8f * s, 1.8f * s, Skin);
                }
                else
                {
                    Tube(c, x - 5f * s, shoulder + 1f, x - 6f * s, hip + 2f * s, 2f * s, 1.6f * s, Skin);
                    Tube(c, x + 5f * s, shoulder + 1f, x + 6f * s, hip + 2f * s, 2f * s, 1.6f * s, Skin);
                }
            }

            // Torso with a cotton tunic and a blue sash.
            Tube(c, x, shoulder + 2f * s, x, hip, 5.2f * s, 4.4f * s, cloth);
            Ball(c, x, shoulder + 1.5f * s, 6f * s, 3f * s, cloth);
            c.Rect(Mathf.RoundToInt(x - 4.5f * s), Mathf.RoundToInt(hip - 2f * s), Mathf.RoundToInt(9f * s), Mathf.Max(1, Mathf.RoundToInt(2f * s)), sash);
            Poly(c, sash, V(x - 1.5f * s, hip), V(x + 1.5f * s, hip), V(x + 1f * s, hip + 7f * s), V(x - 1f * s, hip + 7f * s));   // the loincloth flap
            if (pose != Pose.FromBehind) Arms();
            else
            {
                Poly(c, Tone(cloth, 0.8f), V(x - 6f * s, shoulder), V(x + 6f * s, shoulder), V(x + 7f * s, hip + 4f * s), V(x - 7f * s, hip + 4f * s));   // a cloak
                for (int i = -1; i <= 1; i++) c.Line(Mathf.RoundToInt(x + i * 3f * s), Mathf.RoundToInt(shoulder + 2), Mathf.RoundToInt(x + i * 3.6f * s), Mathf.RoundToInt(hip + 3f * s), Tone(cloth, 0.62f));
            }

            // Head, hair and headband.
            float headY = top + 4.5f * s;
            Tube(c, x, headY + 3f * s, x, shoulder, 1.6f * s, 1.8f * s, Tone(Skin, 0.85f));   // neck
            Ball(c, x, headY, 4.2f * s, 4.6f * s, pose == Pose.FromBehind ? C(26, 22, 30) : Skin);
            Ball(c, x, headY - 2f * s, 4.4f * s, 2.8f * s, C(26, 22, 30));   // black hair
            if (pose == Pose.FromBehind) Tube(c, x, headY, x + 0.5f * s, headY + 6f * s, 1.6f * s, 1f * s, C(26, 22, 30));   // tied hair
            c.Rect(Mathf.RoundToInt(x - 4.4f * s), Mathf.RoundToInt(headY - 1.5f * s), Mathf.RoundToInt(8.8f * s), Mathf.Max(1, Mathf.RoundToInt(1.4f * s)), C(40, 90, 190));
        }
    }
}
