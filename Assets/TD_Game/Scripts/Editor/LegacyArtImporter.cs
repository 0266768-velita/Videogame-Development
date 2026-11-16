using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// Import settings for the character and menu art reused from Mictlán v5 (Art/Characters/Legacy, Art/UI/Menu).
    /// The characters were drawn at 16 px per tile, so they use Pixels Per Unit 16.
    /// Character sheets are cut in a grid of frames with the pivot at the feet. (The old map is NOT used.)
    /// </summary>
    public static class LegacyArtImporter
    {
        public const string CharacterFolder = "Assets/TD_Game/Art/Characters/Legacy";
        public const string MenuFolder = "Assets/TD_Game/Art/UI/Menu";
        public const int LegacyPPU = 16;

        /// <summary>name -> (frame width, frame height) of each character sheet.</summary>
        public static readonly Dictionary<string, Vector2Int> Sheets = new Dictionary<string, Vector2Int>
        {
            { "tlahuelli", new Vector2Int(18, 26) },
            { "dog", new Vector2Int(26, 18) },
            { "witch", new Vector2Int(22, 30) },
            { "giant", new Vector2Int(50, 58) },
            { "quetzal", new Vector2Int(98, 82) },
        };

        [MenuItem("TD_Game/Import Character And Menu Art (Mictlan v5)")]
        public static void ImportAll()
        {
            foreach (string path in Pngs(CharacterFolder))
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (Sheets.TryGetValue(name, out var cell)) ConfigureSheet(path, cell);
            }
            foreach (string path in Pngs(MenuFolder))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        static IEnumerable<string> Pngs(string folder) =>
            AssetDatabase.FindAssets("t:Texture2D", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith(".png"));

        static TextureImporter Base(string path, int ppu)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            return importer;
        }

        /// <summary>Cuts the sheet in a grid. Frames are named name_row_column (row 0 = top row).</summary>
        static void ConfigureSheet(string path, Vector2Int cell)
        {
            var importer = Base(path, LegacyPPU);
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.SaveAndReimport();

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            int columns = texture.width / cell.x, rows = Mathf.Max(1, texture.height / cell.y);

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var existing = provider.GetSpriteRects();
            var rects = new List<SpriteRect>();
            for (int row = 0; row < rows; row++)
                for (int col = 0; col < columns; col++)
                {
                    string frameName = $"{name}_{row}_{col}";
                    var old = existing.FirstOrDefault(r => r.name == frameName);
                    rects.Add(new SpriteRect
                    {
                        name = frameName,
                        spriteID = old != null ? old.spriteID : GUID.Generate(),
                        rect = new Rect(col * cell.x, texture.height - (row + 1) * cell.y, cell.x, cell.y),
                        alignment = SpriteAlignment.Custom,
                        pivot = new Vector2(0.5f, 0.05f)
                    });
                }
            provider.SetSpriteRects(rects.ToArray());
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (names != null) names.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }

        /// <summary>All frames of a row of a character sheet, in order.</summary>
        public static Sprite[] Frames(string sheet, int row = 0)
        {
            string path = $"{CharacterFolder}/{sheet}.png";
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                .Where(s => s.name.StartsWith($"{sheet}_{row}_"))
                .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1)))
                .ToArray();
        }

    }
}
