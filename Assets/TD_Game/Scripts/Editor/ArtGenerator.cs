using System.IO;
using UnityEditor;
using UnityEngine;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// Makes two small textures inside Unity (no external tools) and sets the import of the reused art:
    ///   - AztecFrame.png: button frame with a stepped-fret (greca) border, cut as 9-slice so it stretches nicely.
    ///   - Darkness.png: black with a soft hole in the middle (the light of Tlahuelli's torch).
    ///   - Effects (torch, glow, blood, gibs) and the Pirata One font.
    /// Menu: TD_Game > Generate Menu Frame And Darkness.
    /// </summary>
    public static class ArtGenerator
    {
        public const string FramePath = "Assets/TD_Game/Art/UI/AztecFrame.png";
        public const string DarknessPath = "Assets/TD_Game/Art/Effects/Darkness.png";
        public const string EffectsFolder = "Assets/TD_Game/Art/Effects";
        public const string FontPath = "Assets/TD_Game/Art/Fonts/PirataOne-Regular.ttf";
        const int FrameSize = 48, Border = 14;

        [MenuItem("TD_Game/Generate Menu Frame And Darkness")]
        public static void GenerateAll()
        {
            MakeFrame();
            MakeDarkness();
            MakePixelArt();
            ImportEffects();
        }

        // ---------------- Merchant, stall and Macuahuitl (pixel art written as text: one letter = one colour) ----------------

        static readonly string[] Merchant =
        {
            "......GGGG......",
            ".....GJJJJG.....",
            "....GJJJJJJG....",
            ".....HHHHHH.....",
            ".....SSSSSS.....",
            ".....SESSES.....",
            ".....SSSSSS.....",
            "......SMMS......",
            "....CCCCCCCC....",
            "...CCCCHHCCCC...",
            "..CCCCCHHCCCCC..",
            "..CCCCCCCCCCCC..",
            "..SCCCCCCCCCCS..",
            "..SCCCCCCCCCCS..",
            "...CCCCCCCCCC...",
            "...CCCCCCCCCC...",
            "...CCCHHHHCCC...",
            "...CCCCCCCCCC...",
            "...CCCCCCCCCC...",
            "....DDD..DDD....",
            "....DDD..DDD....",
            "...DDDD..DDDD...",
        };

        static readonly string[] Macuahuitl =
        {
            "..OWO..",
            ".OWWWO.",
            "OWWWWWO",
            ".OWWWO.",
            "OWWWWWO",
            ".OWWWO.",
            "OWWWWWO",
            ".OWWWO.",
            "OWWWWWO",
            ".OWWWO.",
            "OWWWWWO",
            ".OWWWO.",
            "OWWWWWO",
            ".OWWWO.",
            "..WWW..",
            "..HHH..",
            "...B...",
            "...B...",
            "...B...",
            "...B...",
            "...B...",
            "..HBH..",
        };

        static Color32 Palette(char c)
        {
            switch (c)
            {
                case 'G': return new Color32(70, 150, 95, 255);    // jade feathers
                case 'J': return new Color32(30, 95, 65, 255);
                case 'H': return new Color32(200, 160, 60, 255);   // gold
                case 'S': return new Color32(150, 100, 70, 255);   // skin
                case 'E': return new Color32(20, 10, 10, 255);
                case 'M': return new Color32(90, 40, 30, 255);
                case 'C': return new Color32(120, 30, 25, 255);    // cinnabar cloak
                case 'D': return new Color32(40, 25, 20, 255);
                case 'O': return new Color32(25, 20, 35, 255);     // obsidian teeth
                case 'W': return new Color32(125, 78, 42, 255);    // carved wood
                case 'B': return new Color32(85, 50, 28, 255);     // handle
                default: return new Color32(0, 0, 0, 0);
            }
        }

        static Texture2D FromText(string[] rows)
        {
            int h = rows.Length, w = rows[0].Length;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, h - 1 - y, Palette(rows[y][x]));   // text goes top-down, textures bottom-up
            return tex;
        }

        static void MakePixelArt()
        {
            Save(FromText(Merchant), $"{EffectsFolder}/merchant.png");
            Save(FromText(Macuahuitl), $"{EffectsFolder}/macuahuitl.png");
            Save(MakeStall(), $"{EffectsFolder}/stall.png");
        }

        /// <summary>Wooden counter with red potions and gold coins on top, and a gold greca on the front.</summary>
        static Texture2D MakeStall()
        {
            const int w = 40, h = 22;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var clear = new Color32(0, 0, 0, 0);
            for (int x = 0; x < w; x++)
                for (int yTop = 0; yTop < h; yTop++)
                {
                    Color32 c = clear;
                    if (yTop >= 7 && yTop <= 9) c = new Color32(155, 105, 60, 255);                       // counter top
                    else if (yTop > 9) c = x % 7 == 0 ? new Color32(60, 35, 20, 255) : new Color32(95, 58, 32, 255);   // planks
                    if (yTop == 13 || yTop == 14) c = (x / 2 + yTop) % 2 == 0 ? new Color32(200, 160, 60, 255) : new Color32(110, 80, 30, 255);
                    // three red potions
                    foreach (int px in new[] { 4, 10, 16 })
                    {
                        if (x >= px && x < px + 3 && yTop >= 3 && yTop <= 6) c = new Color32(170, 25, 30, 255);
                        if (x == px + 1 && yTop >= 1 && yTop <= 2) c = new Color32(200, 190, 170, 255);
                    }
                    // a pile of gold coins
                    if (x >= 25 && x <= 35 && yTop >= 4 && yTop <= 6 && (x + yTop) % 3 != 0) c = new Color32(220, 180, 60, 255);
                    tex.SetPixel(x, h - 1 - yTop, c);
                }
            return tex;
        }

        // ---------------- Aztec frame ----------------

        static void MakeFrame()
        {
            var tex = new Texture2D(FrameSize, FrameSize, TextureFormat.RGBA32, false);
            Color32 edge = MexicaPalette.Obsidian;
            Color32 gold = MexicaPalette.AgedGold;
            Color32 darkGold = MexicaPalette.DarkGold;
            Color32 fill = MexicaPalette.ObsidianPlate;

            for (int x = 0; x < FrameSize; x++)
                for (int y = 0; y < FrameSize; y++)
                {
                    int d = Mathf.Min(Mathf.Min(x, y), Mathf.Min(FrameSize - 1 - x, FrameSize - 1 - y));   // distance to the edge
                    Color32 c;
                    if (d == 0) c = edge;                                  // dark outline
                    else if (d == 1 || d == Border - 2) c = gold;          // two gold lines
                    else if (d < Border - 2) c = Fret(x, y, d) ? gold : darkGold;   // stepped fret band
                    else if (d == Border - 1) c = edge;
                    else c = fill;                                         // obsidian inside
                    tex.SetPixel(x, y, c);
                }
            Save(tex, FramePath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(FramePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.spriteBorder = new Vector4(Border, Border, Border, Border);   // 9-slice
            importer.SaveAndReimport();
        }

        // Stepped pattern (like the almenas of the Templo Mayor) repeating every 6 pixels along the border.
        static bool Fret(int x, int y, int depth)
        {
            int along = (x + y) % 6;
            int step = depth - 2;          // 0..9 across the band
            return (along < 3 && step % 4 < 2) || (along >= 3 && step % 4 >= 2);
        }

        // ---------------- Darkness with a hole of light ----------------

        static void MakeDarkness()
        {
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                    // 0.15 dark in the centre (still a bit dim), almost black at the edges.
                    float alpha = Mathf.Lerp(0.15f, 0.92f, Mathf.SmoothStep(0.12f, 0.75f, distance));
                    tex.SetPixel(x, y, new Color(0f, 0f, 0.02f, alpha));
                }
            Save(tex, DarknessPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(DarknessPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16;     // 256 px = 16 tiles wide
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        static void Save(Texture2D tex, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
        }

        // ---------------- Effects from Mictlán v5 (16 px per tile) ----------------

        static void ImportEffects()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { EffectsFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path == DarknessPath) continue;
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 16;
                importer.filterMode = path.EndsWith("glow.png") ? FilterMode.Bilinear : FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                bool standsOnFloor = path.Contains("torch") || path.Contains("merchant") || path.Contains("stall")
                    || path.Contains("macuahuitl") || path.Contains("door");
                settings.spriteAlignment = (int)(standsOnFloor ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }

        public static Sprite Effect(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{EffectsFolder}/{name}.png");
    }
}
