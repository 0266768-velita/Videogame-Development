using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// The pictures drawn by the team (Art/Environment/Team/*_original.png), used exactly as drawn:
    ///   - tree_mist and tree_glow: two trees (120 px drawings of 48 px art, so 40 px per tile keeps the 16 px look);
    ///   - rock_team: the rock and its pebble, cut apart so they can be placed separately;
    ///   - fog_strip: 12 frames of drifting mist (an animation), cut into frames;
    ///   - sand_team: a 64 x 64 sand tile.
    /// Only the import is set (Point filter, no compression, pivot at the base); the pixels are never repainted.
    /// </summary>
    public static partial class PropArt
    {
        public const string TeamFolder = "Assets/TD_Game/Art/Environment/Team";
        const float TeamPPU = 40f;

        public class TeamArt
        {
            public Sprite treeMist, treeGlow, rock, pebble, shadow;
            public Sprite[] fog;
            public TileBase sand;
        }

        public static TeamArt V09Team()
        {
            var art = new TeamArt
            {
                treeMist = ImportTeam("tree_mist_original", new Vector2(0.5f, 0.23f)),
                treeGlow = ImportTeam("tree_glow_original", new Vector2(0.47f, 0.04f)),
            };
            // The rock picture holds the rock (lower left) and a pebble (upper right): cut them apart, same pixels.
            var whole = LoadPixels($"{TeamFolder}/rock_team_original.png");
            if (whole != null)
            {
                art.rock = Crop(whole, 0, whole.H / 2, whole.W / 2 + 8, whole.H / 2).Save($"{TeamFolder}/rock_team.png", TeamPPU, Feet);
                art.pebble = Crop(whole, whole.W / 2 + 8, 0, whole.W / 2 - 8, whole.H / 2).Save($"{TeamFolder}/pebble_team.png", TeamPPU, Feet);
            }
            // The fog strip: 12 frames of 120 x 120.
            var strip = LoadPixels($"{TeamFolder}/fog_strip_original.png");
            if (strip != null)
            {
                int frames = strip.W / strip.H;
                art.fog = new Sprite[frames];
                for (int f = 0; f < frames; f++)
                {
                    var frame = new PixelCanvas(strip.H, strip.H);
                    for (int x = 0; x < strip.H; x++) for (int y = 0; y < strip.H; y++) frame.Set(x, y, strip.Get(f * strip.H + x, y));
                    art.fog[f] = frame.Save($"{TeamFolder}/Fog/fog_{f:00}.png", TeamPPU, Mid);
                }
            }
            art.sand = TeamSandTile();
            art.shadow = SoftShadow();
            return art;
        }

        static Sprite ImportTeam(string name, Vector2 pivot)
        {
            string path = $"{TeamFolder}/{name}.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) { Debug.LogWarning("[TD_Game] Missing team art " + path); return null; }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = TeamPPU;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>The team's sand as a Tile (64 px = 1 tile).</summary>
        static TileBase TeamSandTile()
        {
            string png = $"{TeamFolder}/sand_team_original.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(png);
            if (importer == null) return null;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            string path = $"{TeamFolder}/sand_team.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
            tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(png);
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        /// <summary>A soft dark ellipse that sits under rocks and trees so they rest ON the ground.</summary>
        static Sprite SoftShadow()
        {
            var c = new PixelCanvas(24, 10);
            for (int x = 0; x < 24; x++)
                for (int y = 0; y < 10; y++)
                {
                    float dx = (x + 0.5f - 12f) / 12f, dy = (y + 0.5f - 5f) / 5f, d = dx * dx + dy * dy;
                    if (d < 1f) c.Set(x, y, PixelCanvas.C(0, 0, 0, d < 0.5f ? 90 : 55));
                }
            return c.Save($"{Props}/soft_shadow.png", 16, Mid);
        }
    }
}
