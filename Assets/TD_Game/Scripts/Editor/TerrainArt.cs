using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// Makes the tiles of the map inside Unity (pixel art, 16 px per tile like Tlahuelli): textured grass and dirt,
    /// water and pits with rounded shores, bridges, trees and rocks with a dark outline, and the temple stones.
    /// Each picture is saved as PNG + a Tile asset in Art/Tilesets/Mictlan, so they can also be painted by hand
    /// with the Tile Palette. Menu: TD_Game > Generate Map Tiles.
    /// </summary>
    public static class TerrainArt
    {
        public const string Folder = "Assets/TD_Game/Art/Tilesets/Mictlan";
        const int T = 16;   // pixels per tile
        static readonly Color32 Ink = PixelCanvas.C(18, 22, 16);   // outline colour

        public class Set
        {
            public TileBase[] grass, water, pits, trees, rocks;
            public TileBase flowers, dirt, bridgeVertical, bridgeHorizontal, templeFloor, templeWall;
            public TileBase velyGrass;          // Vely's grass (mixed with the generated one)
            public TileBase[] velyTrees;        // Vely's trees
        }

        [MenuItem("TD_Game/Generate Map Tiles")]
        public static void GenerateFromMenu() => Generate();

        public static Set Generate()
        {
            Directory.CreateDirectory(Folder);
            var set = new Set
            {
                grass = new[] { Grass(1, false), Grass(2, false), Grass(3, false) },
                flowers = Grass(4, true),
                dirt = Dirt(),
                water = new TileBase[16],
                pits = new TileBase[16],
                bridgeVertical = Bridge(true),
                bridgeHorizontal = Bridge(false),
                trees = new[] { Tree(1, false), Tree(2, false), Tree(3, true) },
                rocks = new[] { Rock(1, false), Rock(2, true) },
                templeFloor = TempleFloor(),
                templeWall = TempleWall(),
                velyGrass = VelyTile("grass", 64, new Vector2(0.5f, 0.5f), false),
                velyTrees = new[] { VelyTile("tree", 32, new Vector2(0.5f, 0.28f), true), VelyTile("tree2", 32, new Vector2(0.5f, 0.12f), true) },
            };
            for (int mask = 0; mask < 16; mask++)
            {
                set.water[mask] = Water(mask);
                set.pits[mask] = Pit(mask);
            }
            AssetDatabase.SaveAssets();
            return set;
        }

        // ---------------- helpers ----------------

        static TileBase MakeTile(string name, PixelCanvas canvas, Vector2 pivot, bool solid)
        {
            var sprite = canvas.Save($"{Folder}/{name}.png", T, pivot);
            string path = $"{Folder}/{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
            tile.sprite = sprite;
            tile.colliderType = solid ? Tile.ColliderType.Grid : Tile.ColliderType.None;   // solid = blocks the player
            EditorUtility.SetDirty(tile);
            return tile;
        }

        static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        // Neighbour mask: 1 = land to the North, 2 = East, 4 = South, 8 = West.
        public static int Mask(bool north, bool east, bool south, bool west) =>
            (north ? 1 : 0) | (east ? 2 : 0) | (south ? 4 : 0) | (west ? 8 : 0);

        /// <summary>Distance (pixels) from a pixel to the land sides of the mask, with rounded corners.</summary>
        static float ShoreDistance(int x, int y, int mask)
        {
            float px = x + 0.5f, py = y + 0.5f;
            float dn = (mask & 1) != 0 ? py : 99f;
            float de = (mask & 2) != 0 ? T - px : 99f;
            float ds = (mask & 4) != 0 ? T - py : 99f;
            float dw = (mask & 8) != 0 ? px : 99f;
            float d = Mathf.Min(Mathf.Min(dn, de), Mathf.Min(ds, dw));
            const float r = 5f;   // round the corners where two land sides meet
            if ((mask & 9) == 9 && px < r && py < r) d = Mathf.Min(d, r - Vector2.Distance(new Vector2(px, py), new Vector2(r, r)));
            if ((mask & 3) == 3 && px > T - r && py < r) d = Mathf.Min(d, r - Vector2.Distance(new Vector2(px, py), new Vector2(T - r, r)));
            if ((mask & 6) == 6 && px > T - r && py > T - r) d = Mathf.Min(d, r - Vector2.Distance(new Vector2(px, py), new Vector2(T - r, T - r)));
            if ((mask & 12) == 12 && px < r && py > T - r) d = Mathf.Min(d, r - Vector2.Distance(new Vector2(px, py), new Vector2(r, T - r)));
            return d;
        }

        // ---------------- Vely's art (Art/Tilesets/Vely, 64 x 64 pictures) ----------------

        public const string VelyFolder = "Assets/TD_Game/Art/Tilesets/Vely";

        /// <summary>Imports one of Vely's pictures (Point filter, no blur) and makes its Tile.
        /// Grass: 64 px per tile. Trees: 32 px per tile so they are 2 tiles big, like the forest.</summary>
        static TileBase VelyTile(string name, int pixelsPerUnit, Vector2 pivot, bool solid)
        {
            string png = $"{VelyFolder}/{name}.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(png);
            if (importer == null) { Debug.LogWarning("[TD_Game] Missing Vely sprite " + png); return null; }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            string path = $"{VelyFolder}/{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
            tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(png);
            tile.colliderType = solid ? Tile.ColliderType.Grid : Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        // ---------------- ground ----------------

        static TileBase Grass(int seed, bool flowers)
        {
            var c = new PixelCanvas(T, T, PixelCanvas.C(44, 72, 40));
            c.Speckle(seed, PixelCanvas.C(34, 58, 32), PixelCanvas.C(58, 92, 50), 0.16f);
            var random = new System.Random(seed * 31);
            for (int i = 0; i < 5; i++)   // small blades of grass
            {
                int x = random.Next(T), y = random.Next(2, T);
                c.Set(x, y, PixelCanvas.C(72, 110, 58));
                c.Set(x, y - 1, PixelCanvas.C(64, 100, 54));
            }
            if (flowers)
            {
                Color32[] colors = { PixelCanvas.C(190, 60, 50), PixelCanvas.C(220, 190, 80), PixelCanvas.C(200, 200, 220) };
                for (int i = 0; i < 4; i++)
                {
                    int x = 2 + random.Next(T - 4), y = 2 + random.Next(T - 4);
                    c.Set(x, y, colors[i % colors.Length]);
                    c.Set(x, y + 1, PixelCanvas.C(40, 80, 36));
                }
            }
            return MakeTile(flowers ? "Grass_Flowers" : $"Grass_{seed}", c, Center, false);
        }

        static TileBase Dirt()
        {
            var c = new PixelCanvas(T, T, PixelCanvas.C(88, 66, 44));
            c.Speckle(7, PixelCanvas.C(70, 52, 36), PixelCanvas.C(108, 84, 58), 0.18f);
            c.Set(4, 9, PixelCanvas.C(120, 112, 100)); c.Set(11, 3, PixelCanvas.C(120, 112, 100));   // pebbles
            return MakeTile("Dirt_Path", c, Center, false);
        }

        // ---------------- water and pits (16 versions each, one per combination of land sides) ----------------

        static TileBase Water(int mask)
        {
            var c = new PixelCanvas(T, T, PixelCanvas.C(28, 56, 92));
            for (int x = 0; x < T; x++)
                for (int y = 0; y < T; y++)
                {
                    if (((x + y * 3 + (y / 4) * 5) % 9) == 0) c.Set(x, y, PixelCanvas.C(52, 92, 132));   // ripples
                    float d = ShoreDistance(x, y, mask);
                    if (d < 2f) c.Set(x, y, PixelCanvas.C(140, 122, 86));          // sand
                    else if (d < 3f) c.Set(x, y, PixelCanvas.C(176, 196, 206));    // foam
                    else if (d < 5f) c.Blend(x, y, PixelCanvas.C(60, 110, 140), 0.5f);   // shallow water
                }
            return MakeTile($"Water_{mask:00}", c, Center, true);
        }

        static TileBase Pit(int mask)
        {
            var c = new PixelCanvas(T, T, PixelCanvas.C(10, 8, 10));
            for (int x = 0; x < T; x++)
                for (int y = 0; y < T; y++)
                {
                    float d = ShoreDistance(x, y, mask);
                    if (d < 1.5f) c.Set(x, y, PixelCanvas.C(84, 62, 42));                  // earth rim
                    else if (d < 3f) c.Set(x, y, PixelCanvas.C(46, 34, 26));               // wall of the hole
                    else if (d < 6f) c.Set(x, y, PixelCanvas.C(24, 18, 18));
                }
            return MakeTile($"Pit_{mask:00}", c, Center, true);
        }

        static TileBase Bridge(bool vertical)
        {
            var c = new PixelCanvas(T, T, PixelCanvas.C(28, 56, 92));
            for (int i = 0; i < T; i++)
                for (int j = 2; j < T - 2; j++)
                {
                    // i runs along the bridge: a dark gap every 4 pixels separates the planks
                    var plank = i % 4 == 3 ? PixelCanvas.C(70, 46, 28) : PixelCanvas.C(128, 90, 54);
                    if (vertical) c.Set(j, i, plank); else c.Set(i, j, plank);
                }
            for (int i = 0; i < T; i++)   // rails
            {
                if (vertical) { c.Set(2, i, PixelCanvas.C(60, 40, 24)); c.Set(T - 3, i, PixelCanvas.C(60, 40, 24)); }
                else { c.Set(i, 2, PixelCanvas.C(60, 40, 24)); c.Set(i, T - 3, PixelCanvas.C(60, 40, 24)); }
            }
            return MakeTile(vertical ? "Bridge_Vertical" : "Bridge_Horizontal", c, Center, false);
        }

        // ---------------- trees and rocks (taller than a tile, outlined) ----------------

        static TileBase Tree(int seed, bool dead)
        {
            var c = new PixelCanvas(24, 32);
            var bark = PixelCanvas.C(70, 46, 30);
            c.Rect(10, 20, 4, 11, bark);                         // trunk
            c.Set(9, 30, bark); c.Set(14, 30, bark);             // roots
            if (dead)
            {
                // a dead tree of Mictlan: bare twisted branches
                c.Line(12, 20, 5, 9, bark); c.Line(12, 20, 19, 8, bark); c.Line(12, 18, 12, 4, bark);
                c.Line(7, 12, 3, 10, bark); c.Line(17, 11, 21, 12, bark);
            }
            else
            {
                var dark = PixelCanvas.C(30, 64, 34);
                var mid = PixelCanvas.C(44, 88, 44);
                var light = PixelCanvas.C(70, 118, 58);
                float r = seed == 1 ? 10f : 9f;
                c.Circle(12, 12, r, dark);
                c.Circle(11, 11, r - 2, mid);
                c.Circle(9, 8, r - 6, light);
                if (seed == 2) { c.Circle(6, 15, 5, mid); c.Circle(18, 15, 5, mid); }
                c.Speckle(seed, dark, light, 0.07f);
            }
            c.Outline(Ink);
            // pivot: the bottom of the trunk is the bottom of the cell
            return MakeTile(dead ? "Tree_Dead" : $"Tree_{seed}", c, new Vector2(0.5f, 8f / 32f), true);
        }

        static TileBase Rock(int seed, bool mossy)
        {
            var c = new PixelCanvas(T, T);
            c.Ellipse(8, 9, 7, 6, PixelCanvas.C(92, 88, 86));
            c.Ellipse(7, 7, 5, 4, PixelCanvas.C(120, 116, 112));
            c.Ellipse(6, 6, 2, 1.5f, PixelCanvas.C(150, 146, 140));
            if (mossy) c.Ellipse(9, 4, 4, 2, PixelCanvas.C(60, 100, 50));
            c.Speckle(seed, PixelCanvas.C(76, 72, 70), PixelCanvas.C(130, 126, 120), 0.08f);
            c.Outline(Ink);
            return MakeTile(mossy ? "Rock_Mossy" : "Rock", c, Center, true);
        }

        // ---------------- temple of Quetzalcóatl ----------------

        static TileBase TempleFloor()
        {
            var c = new PixelCanvas(T, T, PixelCanvas.C(96, 88, 78));
            c.Speckle(5, PixelCanvas.C(84, 76, 68), PixelCanvas.C(110, 102, 92), 0.12f);
            for (int i = 0; i < T; i++) { c.Set(i, 0, PixelCanvas.C(64, 58, 52)); c.Set(0, i, PixelCanvas.C(64, 58, 52)); c.Set(i, 8, PixelCanvas.C(70, 64, 56)); }
            c.Set(8, 2, PixelCanvas.C(140, 40, 34)); c.Set(8, 12, PixelCanvas.C(40, 120, 100));   // red and jade inlays
            return MakeTile("Temple_Floor", c, Center, false);
        }

        static TileBase TempleWall()
        {
            var c = new PixelCanvas(T, 24, PixelCanvas.C(86, 78, 70));
            for (int y = 0; y < 24; y += 4)   // stone blocks
                for (int x = 0; x < T; x++)
                {
                    c.Set(x, y, PixelCanvas.C(58, 52, 46));
                    if ((x + (y / 4) * 4) % 8 == 0) for (int k = 0; k < 4; k++) c.Set(x, y + k, PixelCanvas.C(58, 52, 46));
                }
            for (int x = 0; x < T; x++)   // stepped fret band in gold and red (talud-tablero)
            {
                bool up = (x / 2) % 2 == 0;
                c.Set(x, up ? 9 : 11, PixelCanvas.C(200, 160, 60));
                c.Set(x, 10, PixelCanvas.C(150, 40, 32));
            }
            c.Speckle(9, PixelCanvas.C(72, 66, 58), PixelCanvas.C(104, 96, 86), 0.06f);
            return MakeTile("Temple_Wall", c, new Vector2(0.5f, 8f / 24f), true);
        }
    }
}
