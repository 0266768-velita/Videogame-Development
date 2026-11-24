using System.Collections.Generic;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Pixel-art pictures of the buttons, made in code once and cached (no files to import):
    ///   - keycap: an obsidian key with an old-gold rim (keyboard keys, also stretched for SPACE / SHIFT);
    ///   - pill: a stone plate with a gold rim (LB, RT, L1, START, D-PAD...);
    ///   - face: a round button in a muted palette colour (Xbox A B X Y; the letter is a Text on top);
    ///   - PlayStation: an obsidian round button with the cross, circle, square or triangle drawn in colour;
    ///   - mouse: a mouse with the left button lit.
    /// They are 20 px and drawn at an integer scale, so they stay sharp next to the rest of the pixel art.
    /// </summary>
    public static class ButtonGlyphs
    {
        public const int Size = 20;
        const int Border = 6;   // 9-slice border of the keycap and the pill

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        static Color Gold => Color.Lerp(MexicaPalette.AgedGold, MexicaPalette.Bone, 0.25f);
        static readonly Color Ink = new Color(0.02f, 0.015f, 0.015f, 1f);

        public static Sprite Keycap() => Get("keycap", () => Plate(MexicaPalette.Obsidian, new Color(0.13f, 0.1f, 0.09f)));
        public static Sprite Pill() => Get("pill", () => Plate(new Color(0.16f, 0.14f, 0.13f), new Color(0.24f, 0.21f, 0.19f)));

        /// <summary>Round button filled with a colour (the letter goes on top as text).</summary>
        public static Sprite Face(Color color) => Get("face" + ColorUtility.ToHtmlStringRGB(color), () => Round(Brighten(color, 0.15f), null));

        /// <summary>PlayStation button: "CROSS", "CIRCLE", "SQUARE" or "TRIANGLE" drawn on obsidian.</summary>
        public static Sprite PlayStation(string shape) => Get("ps" + shape, () => Round(MexicaPalette.Obsidian, shape));

        public static Sprite Mouse() => Get("mouse", MouseSprite);

        public static bool IsPlayStationShape(string label) =>
            label == "CROSS" || label == "CIRCLE" || label == "SQUARE" || label == "TRIANGLE";

        public static bool IsFaceButton(string label) => label == "A" || label == "B" || label == "X" || label == "Y";

        // ---------------- drawing ----------------

        static Sprite Get(string id, System.Func<Sprite> make)
        {
            if (!cache.TryGetValue(id, out var sprite) || sprite == null) cache[id] = sprite = make();
            return sprite;
        }

        static Color Brighten(Color c, float amount) => Color.Lerp(c, Color.white, amount);

        static Texture2D NewTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var clear = new Color32[w * h];
            tex.SetPixels32(clear);
            return tex;
        }

        static Sprite Finish(Texture2D tex, Vector4 border)
        {
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        /// <summary>A key / plate: gold rim, face, a darker band at the bottom (the depth of the key), a lit top edge.</summary>
        static Sprite Plate(Color face, Color light)
        {
            var tex = NewTexture(Size, Size);
            Color depth = Color.Lerp(face, Color.black, 0.5f);
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                {
                    bool corner = (x == 0 || x == Size - 1) && (y == 0 || y == Size - 1);
                    if (corner) continue;
                    bool rim = x == 0 || y == 0 || x == Size - 1 || y == Size - 1;
                    Color c = rim ? Gold : y <= 3 ? depth : y == Size - 2 ? light : face;
                    if (!rim && (x == 1 || x == Size - 2) && y > 3) c = Color.Lerp(c, light, 0.4f);
                    tex.SetPixel(x, y, c);
                }
            tex.SetPixel(1, 1, Gold); tex.SetPixel(Size - 2, 1, Gold); tex.SetPixel(1, Size - 2, Gold); tex.SetPixel(Size - 2, Size - 2, Gold);
            return Finish(tex, new Vector4(Border, Border, Border, Border));
        }

        static Sprite Round(Color face, string shape)
        {
            var tex = NewTexture(Size, Size);
            Vector2 c = new Vector2(Size / 2f, Size / 2f);
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                    if (d > Size / 2f) continue;
                    Color col = d > Size / 2f - 1.2f ? Ink : d > Size / 2f - 2.4f ? Gold : face;
                    if (d <= Size / 2f - 2.4f && y > Size * 0.62f) col = Brighten(col, 0.12f);   // light from above
                    if (d <= Size / 2f - 2.4f && y < Size * 0.3f) col = Color.Lerp(col, Color.black, 0.25f);
                    tex.SetPixel(x, y, col);
                }
            if (shape != null) DrawShape(tex, shape);
            return Finish(tex, Vector4.zero);
        }

        static void DrawShape(Texture2D tex, string shape)
        {
            Color color = InputPrompts.BoxColor(shape, MexicaPalette.Bone);
            color = Brighten(color, 0.35f);
            switch (shape)
            {
                case "CROSS":
                    for (int i = 5; i <= 14; i++)
                    {
                        tex.SetPixel(i, i, color); tex.SetPixel(i + 1, i, color);
                        tex.SetPixel(i, 19 - i, color); tex.SetPixel(i + 1, 19 - i, color);
                    }
                    break;
                case "CIRCLE":
                    for (int x = 0; x < Size; x++)
                        for (int y = 0; y < Size; y++)
                        {
                            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(10f, 10f));
                            if (d >= 3.6f && d <= 5.6f) tex.SetPixel(x, y, color);
                        }
                    break;
                case "SQUARE":
                    for (int i = 5; i <= 14; i++)
                    {
                        tex.SetPixel(i, 5, color); tex.SetPixel(i, 6, color); tex.SetPixel(i, 13, color); tex.SetPixel(i, 14, color);
                        tex.SetPixel(5, i, color); tex.SetPixel(6, i, color); tex.SetPixel(13, i, color); tex.SetPixel(14, i, color);
                    }
                    break;
                case "TRIANGLE":
                    for (int y = 5; y <= 14; y++)
                    {
                        float half = (14 - y) * 0.55f;   // apex at the top
                        int left = Mathf.RoundToInt(10f - half - 0.5f), right = Mathf.RoundToInt(9f + half + 0.5f);
                        tex.SetPixel(left, y, color); tex.SetPixel(right, y, color);
                        if (y <= 6) for (int x = left; x <= right; x++) tex.SetPixel(x, y, color);   // the base
                    }
                    break;
            }
        }

        static Sprite MouseSprite()
        {
            var tex = NewTexture(14, Size);
            for (int x = 0; x < 14; x++)
                for (int y = 0; y < Size; y++)
                {
                    float dx = (x + 0.5f - 7f) / 7f, dy = (y + 0.5f - 10f) / 10f;
                    float d = dx * dx + dy * dy;
                    if (d > 1f) continue;
                    Color col = d > 0.78f ? Gold : MexicaPalette.Obsidian;
                    if (d <= 0.78f && y >= 11 && x <= 6) col = Brighten(MexicaPalette.AgedGold, 0.2f);   // left button lit
                    if (d <= 0.78f && (x == 7 && y >= 10 || y == 10)) col = Gold;                        // seams
                    tex.SetPixel(x, y, col);
                }
            return Finish(tex, Vector4.zero);
        }
    }
}
