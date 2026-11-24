using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// Paints the CSV map on the Ground / Walls Tilemaps with Alex's tiles (TerrainArt) plus the desert sand,
    /// which TerrainArt does not have. Every CSV cell becomes 2 x 2 tiles, exactly where the CSV says:
    /// paths and borders are NOT smoothed or straightened. Water and ravines only round their shore
    /// pixels (the tile picture), never the shape of the map.
    /// </summary>
    public static class CsvMapPainter
    {
        public enum Ground { Grass, Dirt, Sand, Water, Bridge, Tree, TempleWall, TempleFloor, Rock, Pit }

        /// <summary>The CSV has no B: the dirt path of row 22 crosses the western river at columns 16-18.</summary>
        static readonly RectInt BridgeCells = new RectInt(15, 21, 3, 1);   // 0-based column, row

        public static bool IsWalkable(Ground g) =>
            g == Ground.Grass || g == Ground.Dirt || g == Ground.Sand || g == Ground.Bridge || g == Ground.TempleFloor;

        /// <summary>What every cell is. S, E, K and H take the ground of their neighbours.</summary>
        public static Ground[,] Plan(CsvMap map)
        {
            var outdoors = map.Outdoors();
            var plan = new Ground[map.Width, map.Height];
            for (int r = 0; r < map.Height; r++)
                for (int c = 0; c < map.Width; c++)
                {
                    var cell = new Vector2Int(c, r);
                    char s = map[cell];
                    if (s == 'S' || s == 'E' || s == 'K' || s == 'H') s = GroundAround(map, cell);
                    bool indoors = !outdoors[c, r];
                    bool cave = map.IsCave(cell);
                    plan[c, r] = s switch
                    {
                        'C' => indoors ? Ground.TempleFloor : Ground.Dirt,
                        'D' => Ground.Sand,
                        'W' => BridgeCells.Contains(cell) ? Ground.Bridge : Ground.Water,
                        'B' => Ground.Bridge,
                        'T' => indoors ? Ground.TempleWall : Ground.Tree,   // a "tree" inside the pyramid is a pillar
                        'P' => Ground.TempleWall,
                        'M' => cave ? Ground.Rock : Ground.TempleWall,
                        'I' => cave ? Ground.Dirt : Ground.TempleFloor,
                        'V' => Ground.Pit,
                        _ => indoors ? Ground.TempleFloor : cave ? Ground.Dirt : Ground.Grass,
                    };
                }
            return plan;
        }

        /// <summary>The most common walkable ground around a cell (grass if there is none).</summary>
        static char GroundAround(CsvMap map, Vector2Int cell)
        {
            const string kinds = ".CDI";
            var count = new int[kinds.Length];
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    int i = kinds.IndexOf(map[cell.x + dx, cell.y + dy]);
                    if (i >= 0) count[i]++;
                }
            int best = 0;
            for (int i = 1; i < kinds.Length; i++) if (count[i] > count[best]) best = i;
            return count[best] > 0 ? kinds[best] : '.';
        }

        public static void Paint(CsvMap map, Ground[,] plan)
        {
            Tilemap ground = null, walls = null, deco = null;
            foreach (var tilemap in Object.FindObjectsByType<Tilemap>())
            {
                if (tilemap.name == "Ground") ground = tilemap;
                else if (tilemap.name == "Walls") walls = tilemap;
                else if (tilemap.name == "Decoration") deco = tilemap;
            }
            ground.ClearAllTiles(); walls.ClearAllTiles(); deco.ClearAllTiles();
            var art = TerrainArt.Generate();
            var sand = new[] { SandTile(1), SandTile(2) };

            int w = map.Width * CsvMap.TilesPerCell, h = map.Height * CsvMap.TilesPerCell;
            Ground At(int x, int y)
            {
                if (x < 0 || y < 0 || x >= w || y >= h) return (Ground)(-1);
                var cell = map.CellOfTile(x, y);
                return plan[cell.x, cell.y];
            }

            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    var tile = new Vector3Int(x, y, 0);
                    float n = PixelCanvas.Hash(x, y, 99);
                    TileBase grass = n < 0.06f ? art.flowers
                        : art.velyGrass != null && PixelCanvas.Hash(x, y, 123) < 0.5f ? art.velyGrass
                        : art.grass[(int)(n * 1000) % art.grass.Length];
                    var kind = At(x, y);
                    switch (kind)
                    {
                        case Ground.Grass: ground.SetTile(tile, grass); break;
                        case Ground.Dirt: ground.SetTile(tile, art.dirt); break;
                        case Ground.Sand: ground.SetTile(tile, sand[(int)(n * 1000) % sand.Length]); break;
                        case Ground.Bridge: ground.SetTile(tile, art.bridgeHorizontal); break;
                        case Ground.TempleFloor: ground.SetTile(tile, art.templeFloor); break;
                        case Ground.TempleWall: ground.SetTile(tile, art.templeFloor); walls.SetTile(tile, art.templeWall); break;
                        case Ground.Rock: ground.SetTile(tile, art.dirt); walls.SetTile(tile, art.rocks[(int)(n * 100) % art.rocks.Length]); break;
                        case Ground.Water:
                            ground.SetTile(tile, grass);
                            walls.SetTile(tile, art.water[Shore(At, x, y, g => g == Ground.Water || g == Ground.Bridge)]);
                            break;
                        case Ground.Pit:
                            ground.SetTile(tile, sand[0]);
                            walls.SetTile(tile, art.pits[Shore(At, x, y, g => g == Ground.Pit)]);
                            break;
                        default:   // tree: Alex's and Vely's trees, dead trees towards the desert (the land of Mictlan)
                            ground.SetTile(tile, grass);
                            bool dead = map.CellOfTile(x, y).x >= 56 && n < 0.35f;
                            bool vely = art.velyTrees != null && art.velyTrees[0] != null && PixelCanvas.Hash(x, y, 55) < 0.35f;
                            walls.SetTile(tile, dead ? art.trees[2] : vely ? art.velyTrees[(int)(n * 1000) % 2] : art.trees[(int)(n * 1000) % 2]);
                            break;
                    }
                }
            ground.CompressBounds(); walls.CompressBounds();
            if (walls.GetComponent<TilemapCollider2D>() == null) walls.gameObject.AddComponent<TilemapCollider2D>();
        }

        /// <summary>Which sides of a water / ravine tile touch land. Outside the map counts as more water.</summary>
        static int Shore(System.Func<int, int, Ground> at, int x, int y, System.Func<Ground, bool> same)
        {
            bool Land(int cx, int cy)
            {
                var g = at(cx, cy);
                return (int)g >= 0 && !same(g);
            }
            return TerrainArt.Mask(Land(x, y + 1), Land(x + 1, y), Land(x, y - 1), Land(x - 1, y));
        }

        // ---------------- Art that TerrainArt does not have ----------------

        /// <summary>Desert sand of the land of the dead: dull ochre with dark grains and wind ripples.</summary>
        static TileBase SandTile(int seed)
        {
            var c = new PixelCanvas(16, 16, PixelCanvas.C(128, 102, 68));
            c.Speckle(seed * 13, PixelCanvas.C(108, 84, 56), PixelCanvas.C(150, 122, 84), 0.2f);
            int ripple = 3 + seed * 5;
            for (int x = 0; x < 16; x++) c.Set(x, (ripple + x / 5) % 16, PixelCanvas.C(112, 88, 58));
            if (seed == 2) { c.Set(4, 11, PixelCanvas.C(170, 160, 140)); c.Set(12, 4, PixelCanvas.C(90, 70, 50)); }   // a bone, a stone
            return SaveTile($"Sand_{seed}", c, false);
        }

        static TileBase SaveTile(string name, PixelCanvas canvas, bool solid)
        {
            var sprite = canvas.Save($"{TerrainArt.Folder}/{name}.png", 16, new Vector2(0.5f, 0.5f));
            string path = $"{TerrainArt.Folder}/{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
            tile.sprite = sprite;
            tile.colliderType = solid ? Tile.ColliderType.Grid : Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        /// <summary>
        /// The "house of the sword" (H): an adobe house with a thatched roof, a red tezontle door frame,
        /// a stepped fret and a macuahuitl over the door. 112 x 88 px at 16 PPU, pivot at the bottom.
        /// </summary>
        public static Sprite HouseSprite()
        {
            const int W = 112, H = 88;
            var c = new PixelCanvas(W, H);
            var ink = PixelCanvas.C(18, 14, 14);
            c.Rect(12, 42, 88, 42, PixelCanvas.C(140, 100, 66));                       // adobe walls
            c.Speckle(3, PixelCanvas.C(120, 84, 56), PixelCanvas.C(160, 118, 78), 0.1f);
            c.Rect(12, 78, 88, 6, PixelCanvas.C(84, 70, 60));                          // stone base
            for (int y = 8; y <= 44; y++)                                              // thatched roof
            {
                int half = 22 + (y - 8) * 30 / 36;   // wider at the bottom
                for (int x = 56 - half; x <= 56 + half; x++)
                    c.Set(x, y, (x + y / 3) % 4 == 0 ? PixelCanvas.C(104, 78, 42) : PixelCanvas.C(138, 106, 58));
            }
            for (int x = 6; x < W - 6; x++) c.Set(x, 44, PixelCanvas.C(84, 62, 34));    // roof edge
            for (int x = 14; x < 98; x++)                                              // stepped fret (gold and red)
            {
                bool up = (x / 3) % 2 == 0;
                c.Set(x, up ? 48 : 50, PixelCanvas.C(196, 156, 64));
                c.Set(x, 49, PixelCanvas.C(140, 40, 32));
            }
            c.Rect(44, 54, 24, 30, PixelCanvas.C(140, 40, 32));                        // tezontle frame
            c.Rect(47, 57, 18, 27, PixelCanvas.C(40, 28, 20));                         // dark doorway
            c.Rect(22, 60, 10, 8, PixelCanvas.C(30, 22, 18));                          // windows
            c.Rect(80, 60, 10, 8, PixelCanvas.C(30, 22, 18));
            c.Line(56, 30, 56, 44, PixelCanvas.C(96, 64, 36));                         // the macuahuitl over the door
            for (int y = 31; y < 43; y += 2) { c.Set(54, y, PixelCanvas.C(26, 24, 34)); c.Set(58, y, PixelCanvas.C(26, 24, 34)); }
            c.Set(56, 45, PixelCanvas.C(150, 36, 30)); c.Set(56, 46, PixelCanvas.C(150, 36, 30));   // red tassel
            c.Outline(ink);
            return c.Save($"{PropArt.Props}/house_sword.png", 16, new Vector2(0.5f, 0f));
        }
    }
}
