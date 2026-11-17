using System.IO;
using UnityEditor;
using UnityEngine;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// A tiny drawing board to make pixel art inside Unity with code: rectangles, circles, lines, noise and outlines.
    /// (0,0) is the TOP-LEFT pixel, like in a drawing program. Save() writes a PNG and sets its import (Point, no blur).
    /// </summary>
    public class PixelCanvas
    {
        public readonly int W, H;
        readonly Color32[] px;
        static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        public PixelCanvas(int width, int height) : this(width, height, Clear) { }

        public PixelCanvas(int width, int height, Color32 fill)
        {
            W = width; H = height;
            px = new Color32[W * H];
            for (int i = 0; i < px.Length; i++) px[i] = fill;
        }

        public static Color32 C(int r, int g, int b, int a = 255) => new Color32((byte)r, (byte)g, (byte)b, (byte)a);

        public bool Inside(int x, int y) => x >= 0 && y >= 0 && x < W && y < H;
        public Color32 Get(int x, int y) => Inside(x, y) ? px[y * W + x] : Clear;
        public bool IsEmpty(int x, int y) => Get(x, y).a == 0;

        public void Set(int x, int y, Color32 c)
        {
            if (Inside(x, y)) px[y * W + x] = c;
        }

        /// <summary>Paints c on top with some transparency (0..1).</summary>
        public void Blend(int x, int y, Color32 c, float amount)
        {
            if (!Inside(x, y)) return;
            var old = Get(x, y);
            Set(x, y, Color32.Lerp(old, new Color32(c.r, c.g, c.b, old.a == 0 ? c.a : old.a), amount));
        }

        public void Rect(int x, int y, int w, int h, Color32 c)
        {
            for (int i = x; i < x + w; i++)
                for (int j = y; j < y + h; j++) Set(i, j, c);
        }

        public void Circle(float cx, float cy, float r, Color32 c) => Ellipse(cx, cy, r, r, c);

        public void Ellipse(float cx, float cy, float rx, float ry, Color32 c)
        {
            for (int x = Mathf.FloorToInt(cx - rx); x <= Mathf.CeilToInt(cx + rx); x++)
                for (int y = Mathf.FloorToInt(cy - ry); y <= Mathf.CeilToInt(cy + ry); y++)
                {
                    float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                    if (dx * dx + dy * dy <= 1f) Set(x, y, c);
                }
        }

        public void Line(int x0, int y0, int x1, int y1, Color32 c)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), 1);
            for (int i = 0; i <= steps; i++)
                Set(Mathf.RoundToInt(Mathf.Lerp(x0, x1, i / (float)steps)), Mathf.RoundToInt(Mathf.Lerp(y0, y1, i / (float)steps)), c);
        }

        /// <summary>Vertical colour gradient from top to bottom.</summary>
        public void Gradient(int x, int y, int w, int h, Color32 top, Color32 bottom)
        {
            for (int j = 0; j < h; j++)
            {
                var c = Color32.Lerp(top, bottom, h <= 1 ? 0f : j / (float)(h - 1));
                for (int i = 0; i < w; i++) Set(x + i, y + j, c);
            }
        }

        /// <summary>Sprinkles darker and lighter pixels so flat colours look like a texture.</summary>
        public void Speckle(int seed, Color32 dark, Color32 light, float amount)
        {
            for (int x = 0; x < W; x++)
                for (int y = 0; y < H; y++)
                {
                    if (IsEmpty(x, y)) continue;
                    float n = Hash(x, y, seed);
                    if (n < amount) Set(x, y, dark);
                    else if (n > 1f - amount) Set(x, y, light);
                }
        }

        /// <summary>Draws a 1-pixel dark line around everything that is not transparent (makes sprites readable).</summary>
        public void Outline(Color32 c)
        {
            var copy = (Color32[])px.Clone();
            for (int x = 0; x < W; x++)
                for (int y = 0; y < H; y++)
                {
                    if (copy[y * W + x].a != 0) continue;
                    bool touches = false;
                    for (int dx = -1; dx <= 1 && !touches; dx++)
                        for (int dy = -1; dy <= 1 && !touches; dy++)
                            if ((dx == 0) != (dy == 0))
                            {
                                int nx = x + dx, ny = y + dy;
                                if (Inside(nx, ny) && copy[ny * W + nx].a != 0) touches = true;
                            }
                    if (touches) Set(x, y, c);
                }
        }

        /// <summary>Rain: thin diagonal light streaks.</summary>
        public void Rain(int seed, Color32 c, int drops)
        {
            var random = new System.Random(seed);
            for (int i = 0; i < drops; i++)
            {
                int x = random.Next(W), y = random.Next(H), length = 3 + random.Next(4);
                for (int k = 0; k < length; k++) Blend(x - k / 2, y + k, c, 0.6f);
            }
        }

        public static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 2147483;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        /// <summary>Saves as PNG and sets the import: Sprite, Point filter, no compression, the given PPU and pivot.</summary>
        public Sprite Save(string path, float pixelsPerUnit, Vector2 pivot, Vector4 border = default)
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var flipped = new Color32[px.Length];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++) flipped[(H - 1 - y) * W + x] = px[y * W + x];   // textures start at the bottom
            tex.SetPixels32(flipped);
            tex.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteBorder = border;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
