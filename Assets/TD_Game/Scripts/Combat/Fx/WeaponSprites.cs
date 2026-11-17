using System.Collections.Generic;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Pixel art for the Macuahuitl rig, generated in code in the same 16 px style as Tlahuelli's sprite:
    /// a wooden paddle edged with obsidian blades, a wrapped handle and a red tassel.
    /// Also builds "unarmed" copies of the character frames (the club drawn in the sprite removed), so
    /// the animated weapon is never shown twice. Replace with Aseprite art by assigning a sprite to the rig.
    /// </summary>
    public static class WeaponSprites
    {
        // Colours taken from Tlahuelli's own sprite, so the weapon matches it.
        static readonly Color32 Outline = new Color32(14, 8, 18, 255);
        static readonly Color32 Wood = new Color32(120, 76, 44, 255);
        static readonly Color32 WoodDark = new Color32(86, 52, 30, 255);
        static readonly Color32 Obsidian = new Color32(34, 32, 50, 255);
        static readonly Color32 ObsidianShine = new Color32(110, 110, 150, 255);
        static readonly Color32 Wrap = new Color32(150, 100, 55, 255);
        static readonly Color32 Tassel = new Color32(200, 50, 50, 255);

        /// <summary>Colours of the club already drawn in Tlahuelli's sprite (wood, obsidian, obsidian shine).</summary>
        static readonly Color32[] DrawnClub =
        {
            new Color32(120, 76, 44, 255), new Color32(34, 32, 50, 255), new Color32(110, 110, 150, 255)
        };

        const int GripRow = 13;   // row (from the top) of the hand on the handle
        static Sprite macuahuitl;
        static readonly Dictionary<Sprite, Sprite> unarmed = new Dictionary<Sprite, Sprite>();

        /// <summary>7 × 17 px at 16 px per tile, pivot on the grip. The blade points up.</summary>
        public static Sprite Macuahuitl => macuahuitl ??= BuildMacuahuitl();

        static Sprite BuildMacuahuitl()
        {
            const int w = 7, h = 17;
            var px = new Color32[w * h];
            void Set(int x, int yTop, Color32 c) { if (x >= 0 && x < w && yTop >= 0 && yTop < h) px[(h - 1 - yTop) * w + x] = c; }

            // Paddle (rows 0-10): wood, 3 px wide, rounded tip, with obsidian teeth on both edges.
            for (int y = 0; y <= 10; y++)
            {
                int half = y == 0 ? 0 : 1;
                for (int x = 3 - half; x <= 3 + half; x++) Set(x, y, x == 3 && y > 0 ? WoodDark : Wood);
                if (y >= 1 && y <= 10)
                {
                    bool shine = y % 2 == 1;   // alternating glints along each row of blades
                    Set(1, y, shine ? ObsidianShine : Obsidian);
                    Set(5, y, shine ? Obsidian : ObsidianShine);
                }
            }
            // Handle (rows 11-15): 1 px wood with a leather wrap, red tassel at the end.
            for (int y = 11; y <= 15; y++) Set(3, y, y % 2 == 0 ? Wrap : WoodDark);
            Set(2, 15, Tassel); Set(4, 15, Tassel); Set(2, 16, Tassel); Set(4, 16, Tassel);

            AddOutline(px, w, h);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px);
            tex.Apply();
            tex.hideFlags = HideFlags.DontSave;
            var pivot = new Vector2(3.5f / w, (h - GripRow - 0.5f) / h);
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), pivot, 16f);
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        /// <summary>
        /// The same frame without the club drawn in it. Only works if the texture is Read/Write enabled;
        /// otherwise (or if the frame has no club) the original sprite is returned.
        /// </summary>
        public static Sprite Unarmed(Sprite source)
        {
            if (source == null) return null;
            if (unarmed.TryGetValue(source, out var cached)) return cached;
            var result = source.texture != null && source.texture.isReadable ? RemoveClub(source) : source;
            unarmed[source] = result;
            return result;
        }

        static Sprite RemoveClub(Sprite source)
        {
            var r = source.rect;
            int w = (int)r.width, h = (int)r.height;
            var px = source.texture.GetPixels32();
            var frame = new Color32[w * h];
            int texW = source.texture.width;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    frame[y * w + x] = px[((int)r.y + y) * texW + (int)r.x + x];

            // 1. Remove the club pixels.
            var removed = new bool[w * h];
            bool any = false;
            for (int i = 0; i < frame.Length; i++)
                if (frame[i].a > 0 && IsClub(frame[i])) { removed[i] = true; frame[i] = default; any = true; }
            if (!any) return source;

            // 2. Removed pixels enclosed by the body (club held in front of it) are filled with the
            //    neighbouring colours; the ones connected to the outside stay transparent.
            var outside = FloodOutside(frame, w, h);
            for (int pass = 0; pass < 4; pass++)
                for (int i = 0; i < frame.Length; i++)
                    if (removed[i] && !outside[i] && frame[i].a == 0) frame[i] = NeighbourColour(frame, w, h, i % w, i / w);

            // 3. Outline pixels left floating (they surrounded the club) go away; the body gets its outline back.
            for (int i = 0; i < frame.Length; i++)
                if (Same(frame[i], Outline) && !TouchesBody(frame, w, h, i % w, i / w)) frame[i] = default;
            AddOutline(frame, w, h);

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(frame);
            tex.Apply();
            tex.hideFlags = HideFlags.DontSave;
            var pivot = new Vector2(source.pivot.x / w, source.pivot.y / h);
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), pivot, source.pixelsPerUnit);
            sprite.name = source.name + "_unarmed";
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        static bool IsClub(Color32 c)
        {
            foreach (var k in DrawnClub) if (Same(c, k)) return true;
            return false;
        }

        static bool Same(Color32 a, Color32 b) => a.a > 0 && a.r == b.r && a.g == b.g && a.b == b.b;

        static bool Opaque(Color32[] p, int w, int h, int x, int y) => x >= 0 && y >= 0 && x < w && y < h && p[y * w + x].a > 0;

        static bool TouchesBody(Color32[] p, int w, int h, int x, int y)
        {
            foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                if (Opaque(p, w, h, x + dx, y + dy) && !Same(p[(y + dy) * w + x + dx], Outline)) return true;
            return false;
        }

        static Color32 NeighbourColour(Color32[] p, int w, int h, int x, int y)
        {
            foreach (var (dx, dy) in new[] { (-1, 0), (1, 0), (0, 1), (0, -1) })
                if (Opaque(p, w, h, x + dx, y + dy) && !Same(p[(y + dy) * w + x + dx], Outline)) return p[(y + dy) * w + x + dx];
            return default;
        }

        static bool[] FloodOutside(Color32[] p, int w, int h)
        {
            var seen = new bool[w * h];
            var stack = new Stack<int>();
            for (int x = 0; x < w; x++) { stack.Push(x); stack.Push((h - 1) * w + x); }
            for (int y = 0; y < h; y++) { stack.Push(y * w); stack.Push(y * w + w - 1); }
            while (stack.Count > 0)
            {
                int i = stack.Pop();
                if (seen[i] || p[i].a > 0) continue;
                seen[i] = true;
                int x = i % w, y = i / w;
                if (x > 0) stack.Push(i - 1);
                if (x < w - 1) stack.Push(i + 1);
                if (y > 0) stack.Push(i - w);
                if (y < h - 1) stack.Push(i + w);
            }
            return seen;
        }

        static void AddOutline(Color32[] p, int w, int h)
        {
            var add = new List<int>();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (p[y * w + x].a == 0 && TouchesBody(p, w, h, x, y)) add.Add(y * w + x);
            foreach (int i in add) p[i] = Outline;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { macuahuitl = null; unarmed.Clear(); }
    }
}
