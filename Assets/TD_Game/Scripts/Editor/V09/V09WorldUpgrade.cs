using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// Mictlán v0.9: upgrades the existing MainMap_CSV in place (the map of the team is kept: same rivers, paths,
    /// forests, desert, pyramid site and temple). It repaints the ground with the new tiles, turns water and ravines
    /// into hazards you can fall into (and jump over where they are narrow), adds bridges and stepping stones where they
    /// save the most walking, replaces the loose temple blocks with natural obstacles, builds the new pyramid, spreads
    /// rocks, landmarks and region details, places the new enemy camps, the merchants, the villagers and the house of
    /// the four witches, and adds the map (M), the Enemies Left counter and the dialogue box.
    /// Everything it creates lives under objects named "V09_..." and is rebuilt (not duplicated) when run again.
    /// Menu: TD_Game > v0.9 > Upgrade MainMap_CSV. Batch: -executeMethod TD_Game.EditorTools.V09WorldUpgrade.Run
    /// </summary>
    public static partial class V09WorldUpgrade
    {
        enum K { Grass, Dirt, Sand, Water, Bridge, BridgeV, Tree, TempleWall, TempleFloor, Rock, Pit, Plaza, PlazaEdge, Stone, Void }

        /// <summary>The pyramid site in CSV cells (columns 72-93, rows 7-20).</summary>
        static readonly RectInt PyramidCells = new RectInt(72, 7, 22, 14);

        static CsvMap map;
        static K[,] plan;
        static int W, H;
        static PropArt.V09Tiles tiles;
        static PropArt.TeamArt team;
        static TerrainArt.Set art;
        static readonly List<(Vector2 p, float r)> occupied = new List<(Vector2, float)>();
        static readonly List<Vector2Int> removedBlocks = new List<Vector2Int>();
        static readonly List<(Vector2Int a, Vector2Int b, bool horizontal)> bridges = new List<(Vector2Int, Vector2Int, bool)>();
        static readonly List<(Vector2Int a, Vector2Int b)> steppingStones = new List<(Vector2Int, Vector2Int)>();
        static System.Random random;

        [MenuItem("TD_Game/v0.9/Upgrade MainMap_CSV")]
        public static void Run()
        {
            random = new System.Random(909);
            occupied.Clear(); removedBlocks.Clear(); bridges.Clear(); steppingStones.Clear();
            EnsureLayers();

            // 1. Art and sounds.
            art = TerrainArt.Generate();
            tiles = PropArt.V09Terrain();
            PropArt.V09People(); PropArt.V09Nature(); PropArt.V09Witch(); PropArt.PyramidSprite();
            team = PropArt.V09Team();
            SfxSynth.GenerateAll();
            AssetDatabase.Refresh();
            ImportAudio();
            var library = UpdateAudioLibrary();
            var catalog = SkinCatalogAsset();
            SetEnemyGold();
            MexicaNamesAndDifficulty();

            // 2. The map.
            var scene = EditorSceneManager.OpenScene(CsvMapBuilder.ScenePath);
            map = CsvMap.Load(CsvMapBuilder.CsvPath);
            W = map.Width; H = map.Height;
            BuildPlan();
            CaptureInterior();          // the inside of the pyramid leaves the map (V09WorldUpgrade.Layout.cs)
            AddBridgesAndStones();
            Paint();

            foreach (var root in scene.GetRootGameObjects().Where(g => g.name.StartsWith("V09_")).ToList()) Object.DestroyImmediate(root);
            ApplyLayoutToScene();       // map edges, the house of the sword goes away, the boss moves with the inside
            ReserveExisting();
            BuildPyramid();
            ReplaceBlocks();
            PlaceLandmarks();
            PlaceRocksAndDetails();
            PlaceEnemies();
            PlaceMerchants(library);
            PlaceVillagers();
            BuildWitchHouse();
            BuildPyramidInterior();
            SetupPlayer(catalog);
            SetupSystems(library);
            BuildUI();
            RemoveOutsideMap();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            UpdateMenuScenes();
            AssetDatabase.SaveAssets();
            Debug.Log($"[TD_Game] v0.9 upgrade done: {bridges.Count} new bridges, {steppingStones.Count} stepping-stone crossings, " +
                      $"{removedBlocks.Count} loose blocks replaced, {Object.FindObjectsByType<CombatEnemy>().Length} enemies in the scene.");
        }

        // ---------------- Layers ----------------

        static void EnsureLayers()
        {
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tags.FindProperty("layers");
            void Set(int index, string name)
            {
                var p = layers.GetArrayElementAtIndex(index);
                if (string.IsNullOrEmpty(p.stringValue)) p.stringValue = name;
            }
            Set(8, PlayerJump.PlayerLayer); Set(9, PlayerJump.HazardLayer); Set(10, PlayerJump.LowObstacleLayer);
            tags.ApplyModifiedPropertiesWithoutUndo();
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer(PlayerJump.PlayerLayer), LayerMask.NameToLayer(PlayerJump.HazardLayer), true);
            var physics = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Physics2DSettings.asset")[0]);
            physics.Update();
            AssetDatabase.SaveAssets();
        }

        // ---------------- The plan of every cell ----------------

        static Vector2Int Cell(Vector2 world) =>
            new Vector2Int(Mathf.FloorToInt(world.x / CsvMap.TilesPerCell), map.Height - 1 - Mathf.FloorToInt(world.y / CsvMap.TilesPerCell));

        static Vector2 World(Vector2Int cell) => map.World(cell);
        static bool Inside(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < W && c.y < H;
        static K At(Vector2Int c) => Inside(c) ? plan[c.x, c.y] : K.Tree;

        static bool Walkable(K k) => k == K.Grass || k == K.Dirt || k == K.Sand || k == K.Bridge || k == K.BridgeV || k == K.TempleFloor
                                     || k == K.Plaza || k == K.PlazaEdge || k == K.Stone;
        static bool Walkable(Vector2Int c) => Inside(c) && Walkable(plan[c.x, c.y]);
        static bool Open(K k) => k == K.Grass || k == K.Sand || k == K.Dirt || k == K.Plaza || k == K.PlazaEdge;

        static void BuildPlan()
        {
            var basePlan = CsvMapPainter.Plan(map);
            plan = new K[W, H];
            for (int c = 0; c < W; c++)
                for (int r = 0; r < H; r++)
                {
                    plan[c, r] = basePlan[c, r] switch
                    {
                        CsvMapPainter.Ground.Grass => K.Grass,
                        CsvMapPainter.Ground.Dirt => K.Dirt,
                        CsvMapPainter.Ground.Sand => K.Sand,
                        CsvMapPainter.Ground.Water => K.Water,
                        CsvMapPainter.Ground.Bridge => K.Bridge,
                        CsvMapPainter.Ground.Tree => K.Tree,
                        CsvMapPainter.Ground.TempleWall => K.TempleWall,
                        CsvMapPainter.Ground.TempleFloor => K.TempleFloor,
                        CsvMapPainter.Ground.Rock => K.Rock,
                        CsvMapPainter.Ground.Void => K.Void,
                        _ => K.Pit,
                    };
                }

            // The pyramid site becomes a stone plaza (the pyramid itself is one big picture with colliders),
            // with a ring of paving overgrown with grass around it.
            for (int c = PyramidCells.xMin - 1; c <= PyramidCells.xMax; c++)
                for (int r = PyramidCells.yMin - 1; r <= PyramidCells.yMax; r++)
                {
                    var cell = new Vector2Int(c, r);
                    if (!Inside(cell)) continue;
                    bool core = PyramidCells.Contains(cell);
                    if (core) plan[c, r] = K.Plaza;
                    else if (plan[c, r] != K.Tree && plan[c, r] != K.Water) plan[c, r] = K.PlazaEdge;
                }

            // The dashed lines of the design (single path cells with no path next to them) looked like loose squares of
            // dirt: they become an old trail of overgrown paving stones.
            for (int c = 0; c < W; c++)
                for (int r = 0; r < H; r++)
                {
                    if (plan[c, r] != K.Dirt) continue;
                    var cell = new Vector2Int(c, r);
                    if (CsvMap.Neighbours4(cell).Any(n => Inside(n) && plan[n.x, n.y] == K.Dirt)) continue;
                    plan[c, r] = K.PlazaEdge;
                }

            // Loose temple blocks (P outside the pyramid) were walls in the middle of the forest: they become grass,
            // and ReplaceBlocks() puts logs, rocks and bushes there instead.
            for (int c = 0; c < W; c++)
                for (int r = 0; r < H; r++)
                {
                    if (map[c, r] != 'P') continue;
                    var cell = new Vector2Int(c, r);
                    var grown = new RectInt(PyramidCells.xMin - 3, PyramidCells.yMin - 3, PyramidCells.width + 6, PyramidCells.height + 6);
                    if (grown.Contains(cell)) { if (!PyramidCells.Contains(cell)) plan[c, r] = K.PlazaEdge; continue; }
                    plan[c, r] = K.Grass;
                    removedBlocks.Add(cell);
                }
        }

        // ---------------- Bridges and stepping stones ----------------

        struct Crossing
        {
            public Vector2Int a, b;    // land on both ends
            public List<Vector2Int> water;
            public bool horizontal;
            public int saving;
        }

        /// <summary>
        /// Every straight run of water (2-5 cells) with walkable land on both ends is a possible crossing. The ones that
        /// save the most walking (and touch a path) get a bridge; the next ones get stepping stones you have to jump.
        /// </summary>
        static void AddBridgesAndStones()
        {
            var placed = new List<Vector2Int>();
            for (int c = 0; c < W; c++) for (int r = 0; r < H; r++) if (plan[c, r] == K.Bridge) placed.Add(new Vector2Int(c, r));

            for (int round = 0; round < 9; round++)
            {
                bool wantBridge = round < 5;
                var best = new Crossing { saving = 0 };
                foreach (var crossing in Crossings())
                {
                    if (placed.Any(p => Vector2Int.Distance(p, crossing.water[0]) < (wantBridge ? 16 : 10))) continue;
                    int saving = Saving(crossing);
                    if (TouchesPath(crossing)) saving += 25;
                    if (saving > best.saving) { best = crossing; best.saving = saving; }
                }
                if (best.water == null || best.saving < (wantBridge ? 35 : 22)) continue;
                if (wantBridge)
                {
                    foreach (var w in best.water) plan[w.x, w.y] = best.horizontal ? K.Bridge : K.BridgeV;
                    bridges.Add((best.a, best.b, best.horizontal));
                }
                else
                {
                    // Stones with one cell (2 tiles) of water before each: W S W, W S, W S W S, W S W S W.
                    int count = best.water.Count;
                    for (int i = 0; i < count; i++)
                        if (count % 2 == 1 ? i % 2 == 1 : (count - 1 - i) % 2 == 0) plan[best.water[i].x, best.water[i].y] = K.Stone;
                    steppingStones.Add((best.a, best.b));
                }
                placed.Add(best.water[0]);
            }
        }

        static IEnumerable<Crossing> Crossings()
        {
            for (int c = 0; c < W; c++)
                for (int r = 0; r < H; r++)
                {
                    var start = new Vector2Int(c, r);
                    if (!Walkable(start)) continue;
                    foreach (var dir in new[] { Vector2Int.right, Vector2Int.down })
                    {
                        var run = new List<Vector2Int>();
                        var p = start + dir;
                        while (Inside(p) && plan[p.x, p.y] == K.Water && run.Count < 6) { run.Add(p); p += dir; }
                        if (run.Count < 2 || run.Count > 5 || !Walkable(p) || plan[p.x, p.y] == K.Stone) continue;
                        // Both sides of the run must be water too (a real river, not a corner of a pond).
                        var side = new Vector2Int(dir.y, dir.x);
                        if (run.Any(w => At(w + side) != K.Water && At(w - side) != K.Water)) continue;
                        yield return new Crossing { a = start, b = p, water = run, horizontal = dir == Vector2Int.right };
                    }
                }
        }

        static bool TouchesPath(Crossing crossing)
        {
            foreach (var end in new[] { crossing.a, crossing.b })
                foreach (var n in CsvMap.Neighbours4(end).Append(end))
                    if (map[n] == 'C') return true;
            return false;
        }

        /// <summary>How many cells of walking the crossing saves (walking around now minus walking across).</summary>
        static int Saving(Crossing crossing)
        {
            var d = map.Distances(crossing.a, Walkable);
            int around = d[crossing.b.x, crossing.b.y];
            if (around < 0) around = 400;
            return around - (crossing.water.Count + 1);
        }

        // ---------------- Painting the tilemaps ----------------

        static Tilemap Tilemap(string name, int order, TilemapRenderer.Mode mode, int layer, bool collider)
        {
            var grid = GameObject.Find("Grid").transform;
            var t = grid.Find(name);
            if (t == null)
            {
                var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
                go.transform.SetParent(grid, false);
                t = go.transform;
            }
            t.gameObject.layer = layer;
            var renderer = t.GetComponent<TilemapRenderer>();
            renderer.sortingOrder = order;
            renderer.mode = mode;
            if (collider && t.GetComponent<TilemapCollider2D>() == null) t.gameObject.AddComponent<TilemapCollider2D>();
            return t.GetComponent<Tilemap>();
        }

        /// <summary>Smooth value noise (0..1): big soft patches instead of a checkerboard.</summary>
        static float Noise(float x, float y, float scale, int seed)
        {
            x /= scale; y /= scale;
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            float a = PixelCanvas.Hash(x0, y0, seed), b = PixelCanvas.Hash(x0 + 1, y0, seed);
            float c = PixelCanvas.Hash(x0, y0 + 1, seed), d = PixelCanvas.Hash(x0 + 1, y0 + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        static void Paint()
        {
            Tilemap ground = null, walls = null, deco = null;
            foreach (var t in Object.FindObjectsByType<Tilemap>())
            {
                if (t.name == "Ground") ground = t;
                else if (t.name == "Walls") walls = t;
                else if (t.name == "Decoration") deco = t;
            }
            int hazard = LayerMask.NameToLayer(PlayerJump.HazardLayer);
            var water = Tilemap("Water", -9, TilemapRenderer.Mode.Chunk, hazard, true);
            var pits = Tilemap("Pits", -9, TilemapRenderer.Mode.Chunk, hazard, true);
            ground.ClearAllTiles(); walls.ClearAllTiles(); deco.ClearAllTiles(); water.ClearAllTiles(); pits.ClearAllTiles();

            int w = W * CsvMap.TilesPerCell, h = H * CsvMap.TilesPerCell;
            K KAt(int x, int y)
            {
                if (x < 0 || y < 0 || x >= w || y >= h) return (K)(-1);
                var cell = map.CellOfTile(x, y);
                return plan[cell.x, cell.y];
            }
            int Mask(int x, int y, System.Func<K, bool> same)
            {
                bool Land(int cx, int cy) { var g = KAt(cx, cy); return (int)g >= 0 && !same(g); }
                return TerrainArt.Mask(Land(x, y + 1), Land(x + 1, y), Land(x, y - 1), Land(x - 1, y));
            }
            bool IsWaterish(K g) => g == K.Water || g == K.Bridge || g == K.BridgeV || g == K.Stone;

            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    var tile = new Vector3Int(x, y, 0);
                    float n = PixelCanvas.Hash(x, y, 99);
                    var cell = map.CellOfTile(x, y);
                    TileBase grass = GrassTile(x, y, cell);
                    switch (KAt(x, y))
                    {
                        case K.Grass: ground.SetTile(tile, grass); break;
                        case K.Dirt: ground.SetTile(tile, art.dirt); break;
                        case K.Sand: ground.SetTile(tile, SandTile(x, y)); break;
                        case K.Plaza: ground.SetTile(tile, tiles.plaza); break;
                        case K.PlazaEdge: ground.SetTile(tile, n < 0.6f ? tiles.plazaEdge : grass); break;
                        case K.Bridge: ground.SetTile(tile, art.bridgeHorizontal); break;
                        case K.BridgeV: ground.SetTile(tile, art.bridgeVertical); break;
                        case K.Stone:
                            ground.SetTile(tile, tiles.steppingStone);
                            break;
                        case K.TempleFloor: ground.SetTile(tile, art.templeFloor); break;
                        case K.TempleWall: ground.SetTile(tile, art.templeFloor); walls.SetTile(tile, art.templeWall); break;
                        case K.Rock: ground.SetTile(tile, art.dirt); walls.SetTile(tile, art.rocks[(int)(n * 100) % art.rocks.Length]); break;
                        case K.Void: break;   // out of the map (X, or where the inside of the pyramid was)
                        case K.Water:
                            ground.SetTile(tile, grass);
                            water.SetTile(tile, art.water[Mask(x, y, IsWaterish)]);
                            break;
                        case K.Pit:
                            ground.SetTile(tile, SandTile(x, y));
                            pits.SetTile(tile, tiles.pits[Mask(x, y, g => g == K.Pit)]);
                            break;
                        default:   // trees: Alex's and Vely's trees, dead trees towards the desert
                            ground.SetTile(tile, grass);
                            bool dead = cell.x >= 56 && n < 0.35f;
                            bool vely = art.velyTrees != null && art.velyTrees[0] != null && PixelCanvas.Hash(x, y, 55) < 0.35f;
                            walls.SetTile(tile, dead ? art.trees[2] : vely ? art.velyTrees[(int)(n * 1000) % 2] : art.trees[(int)(n * 1000) % 2]);
                            break;
                    }
                }
            ground.CompressBounds(); walls.CompressBounds(); water.CompressBounds(); pits.CompressBounds();
            if (walls.GetComponent<TilemapCollider2D>() == null) walls.gameObject.AddComponent<TilemapCollider2D>();
        }

        /// <summary>Vely's grass and the new zigzag grass in big soft patches; marigolds near the start of the journey.</summary>
        static TileBase GrassTile(int x, int y, Vector2Int cell)
        {
            float patch = Noise(x, y, 7f, 3);
            float n = PixelCanvas.Hash(x, y, 99);
            bool tlaloc = cell.x < 64 && cell.y < 36;
            if (tlaloc && Noise(x, y, 5f, 21) > 0.8f && n < 0.55f) return tiles.cempasuchil;
            if (patch < 0.42f && art.velyGrass != null) return art.velyGrass;
            if (n < 0.03f) return art.flowers;
            return tiles.grass[(int)(n * 1000) % tiles.grass.Length];
        }

        /// <summary>Rippled dunes with flat, windswept patches.</summary>
        static TileBase SandTile(int x, int y)
        {
            float n = PixelCanvas.Hash(x, y, 77);
            if (team.sand != null && Noise(x, y, 6f, 44) > 0.66f) return team.sand;   // the team's sand, in patches
            if (Noise(x, y, 9f, 8) < 0.33f) return n < 0.08f ? tiles.sand[2] : tiles.sandFlat;
            return n < 0.06f ? tiles.sand[2] : n < 0.5f ? tiles.sand[0] : tiles.sand[1];
        }
    }
}
