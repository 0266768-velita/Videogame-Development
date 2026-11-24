using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// Creates MainMap in the style of Adrián's map (same tiles: grass, dirt, stone, water, pillars) but bigger
    /// (88 x 40 tiles, 5 rooms) and darker, and places everything on it:
    ///   room A (start, zone 1: wolves) -> room B (zone 2: witches, the potion SHOP) -> room C and room D
    ///   (zone 3: giants) -> room E (Quetzalcóatl; the corridor closes behind you).
    /// Also adds the minimap, the gold counter, gore, Tlahuelli's torch and the darkness around the light.
    /// The tiles are painted once here and then the scene is a normal Tilemap scene: Adrián can repaint it
    /// with the Tile Palette. It never overwrites MainMap: delete it to rebuild.
    /// Menu: TD_Game > Build Main Map (big, Adrian's style).
    /// </summary>
    public static class MainMapBuilder
    {
        public const string MainMapPath = "Assets/TD_Game/Scenes/Levels/MainMap.unity";
        const string TestLevelPath = "Assets/TD_Game/Scenes/Testing/TestLevel.unity";
        const string PrefabFolder = "Assets/TD_Game/Prefabs/Characters";
        const string TileFolder = "Assets/TD_Game/Art/Tilesets/Placeholder/Tiles";
        const string Textures = "Assets/TD_Game/Art/Tilesets/Placeholder/Textures";

        // ---------------- Layout (cells; 1 tile = 1 unit) ----------------
        static readonly RectInt MapArea = new RectInt(0, 0, 88, 40);
        static readonly RectInt[] Floors =
        {
            new RectInt(1, 1, 24, 18),    // A  start, zone 1 (Territories of Tlaloc)
            new RectInt(1, 21, 24, 18),   // B  zone 2 + merchant (Valley of Tlazolteotl)
            new RectInt(27, 21, 30, 18),  // C  zone 3 (Valley of Serpents)
            new RectInt(27, 1, 30, 18),   // D  zone 3 (Valley of Serpents)
        };
        static readonly RectInt Temple = new RectInt(59, 5, 28, 30);   // E  boss (Celestial Temple)
        // The path of the player: start -> bridge A/B -> bridge B/C -> bridge C/D -> temple door.
        static readonly Vector2Int[] PathPoints =
        {
            new Vector2Int(3, 3), new Vector2Int(11, 3), new Vector2Int(11, 30), new Vector2Int(41, 30),
            new Vector2Int(41, 9), new Vector2Int(58, 9),
        };
        static readonly (Vector2 center, float radius)[] Ponds =
        {
            (new Vector2(6.5f, 11.5f), 2.3f), (new Vector2(18f, 27f), 2.6f), (new Vector2(48f, 34f), 2.4f), (new Vector2(33f, 7f), 2.2f),
        };
        static readonly Vector2Int[] Pits = { new Vector2Int(19, 6), new Vector2Int(4, 34), new Vector2Int(51, 11), new Vector2Int(36, 25) };
        static readonly RectInt ShopArea = new RectInt(9, 31, 10, 8);
        static readonly RectInt PuzzleArea = new RectInt(29, 32, 12, 6);   // shrine of the four fires (room C)

        static readonly Vector2 PlayerStart = new Vector2(3.5f, 3.5f);
        static readonly (string prefab, Vector2 position)[] Enemies =
        {
            ("Enemy_Wolf", new Vector2(9.5f, 8.5f)), ("Enemy_Wolf", new Vector2(18.5f, 13.5f)), ("Enemy_Wolf", new Vector2(5.5f, 16.5f)),   // zone 1
            ("Enemy_Witch", new Vector2(5.5f, 26.5f)), ("Enemy_Witch", new Vector2(21.5f, 34.5f)), ("Enemy_Wolf", new Vector2(14.5f, 23.5f)), // zone 2
            ("Enemy_Witch", new Vector2(32.5f, 33.5f)), ("Enemy_Giant", new Vector2(52.5f, 29.5f)), ("Enemy_Witch", new Vector2(49.5f, 23.5f)), // zone 3
            ("Enemy_Giant", new Vector2(33.5f, 13.5f)), ("Enemy_Giant", new Vector2(47.5f, 5.5f)), ("Enemy_Wolf", new Vector2(53.5f, 15.5f)),  // zone 3
        };
        static readonly Vector2 ShopPosition = new Vector2(13.5f, 34.6f);   // tent against the forest at the north of room B
        static readonly Vector2 KeyDoorPosition = new Vector2(58.5f, 10f);   // in the west wall of the temple
        static readonly Vector2 BossPosition = new Vector2(78.5f, 20f);
        static readonly Vector2 GatePosition = new Vector2(59.5f, 10f);      // closes behind you during the boss fight
        static readonly Vector2 ArenaTrigger = new Vector2(62.5f, 20f);
        static readonly Vector2 ArenaTriggerSize = new Vector2(1f, 28f);
        const float ExplorationZoom = 6.75f;   // camera size: Tlahuelli small on screen, like the old Zelda / Pokemon
        const float CombatZoom = 4.21875f;
        static readonly (string title, string subtitle, RectInt area)[] Territories =   // names from the GDD
        {
            ("TERRITORIES OF TLÁLOC", "The rainforest of the rain god", new RectInt(1, 1, 24, 18)),
            ("VALLEY OF TLAZOLTÉOTL", "Where the witches sing", new RectInt(1, 21, 24, 18)),
            ("VALLEY OF SERPENTS", "Land of the Quinametzin giants", new RectInt(27, 1, 30, 38)),
            ("CELESTIAL TEMPLE", "The house of Quetzalcóatl", new RectInt(59, 5, 28, 30)),
        };

        enum Kind { Forest, Floor, Path, Water, BridgeV, BridgeH, Rock, Pit, TempleFloor, TempleWall }

        [MenuItem("TD_Game/Build Main Map (big, Adrian's style)")]
        public static void BuildFromMenu() => Build(FrontEndBuilder.EnsureAudioLibrary());

        public static void Build(AudioLibrary library)
        {
            if (System.IO.File.Exists(MainMapPath)) { Debug.Log("[TD_Game] MainMap already exists (delete it to rebuild)."); return; }

            FrontEndBuilder.EnsureFolder("Assets/TD_Game/Scenes/Levels");
            AssetDatabase.CopyAsset(TestLevelPath, MainMapPath);   // the systems (player, combat, HUD...) come from TestLevel
            var scene = EditorSceneManager.OpenScene(MainMapPath);

            foreach (var enemy in Object.FindObjectsByType<CombatEnemy>(FindObjectsInactive.Include)) Object.DestroyImmediate(enemy.gameObject);
            foreach (var orb in Object.FindObjectsByType<SoulOrb>(FindObjectsInactive.Include)) Object.DestroyImmediate(orb.gameObject);

            PaintMap();
            var player = PlacePlayer();
            var parent = new GameObject("Enemies").transform;
            int i = 0;
            foreach (var (prefab, position) in Enemies)
            {
                var enemy = Spawn(prefab, position, parent, $"{prefab}_{++i}");
                // Progressive difficulty: the deeper the zone, the stronger (more health and damage).
                float strength = i <= 3 ? 1.2f : i <= 6 ? 1.6f : 2f;
                var e = new SerializedObject(enemy);
                e.FindProperty("strength").floatValue = strength;
                e.ApplyModifiedPropertiesWithoutUndo();
            }
            BuildBossZone();
            BuildTempleEntrance();
            BuildTempleDecor();
            BuildKeyAndDoor();
            var mapUI = BuildMapUI(player);
            BuildShop(mapUI.shopPrompt);
            BuildTerritoryHints(mapUI.zoneTitle);
            BuildPuzzle(mapUI.shopPrompt);
            new GameObject("ClickToTarget").AddComponent<ClickToTarget>();
            AddGore();
            AddDarkness();
            SetZoomAndCombat();
            FrontEndBuilder.AddLevelServicesToOpenScene(library, MusicTrack.Explore);

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[TD_Game] MainMap built (natural terrain).");
        }

        // ---------------- Tiles: natural barriers (forest, rivers with bridges, rocks, pits) ----------------

        static void PaintMap()
        {
            Tilemap ground = null, walls = null, deco = null;
            foreach (var map in Object.FindObjectsByType<Tilemap>())
            {
                if (map.name == "Ground") ground = map;
                else if (map.name == "Walls") walls = map;
                else if (map.name == "Decoration") deco = map;
            }
            ground.ClearAllTiles(); walls.ClearAllTiles(); deco.ClearAllTiles();
            ground.color = Color.white; walls.color = Color.white; deco.color = Color.white;
            var art = TerrainArt.Generate();
            var k = Plan();

            for (int x = 0; x < MapArea.width; x++)
                for (int y = 0; y < MapArea.height; y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    float n = PixelCanvas.Hash(x, y, 99);
                    TileBase grass = n < 0.06f ? art.flowers
                        : (art.velyGrass != null && PixelCanvas.Hash(x, y, 123) < 0.5f) ? art.velyGrass   // half the grass is Vely's
                        : art.grass[(int)(n * 1000) % art.grass.Length];
                    switch (k[x, y])
                    {
                        case Kind.Floor: ground.SetTile(cell, grass); break;
                        case Kind.Path: ground.SetTile(cell, art.dirt); break;
                        case Kind.BridgeV: ground.SetTile(cell, art.bridgeVertical); break;
                        case Kind.BridgeH: ground.SetTile(cell, art.bridgeHorizontal); break;
                        case Kind.TempleFloor: ground.SetTile(cell, art.templeFloor); break;
                        case Kind.TempleWall: ground.SetTile(cell, art.templeFloor); walls.SetTile(cell, art.templeWall); break;
                        case Kind.Water: ground.SetTile(cell, grass); walls.SetTile(cell, art.water[LandMask(k, x, y, Kind.Water)]); break;
                        case Kind.Pit: ground.SetTile(cell, grass); walls.SetTile(cell, art.pits[LandMask(k, x, y, Kind.Pit)]); break;
                        case Kind.Rock: ground.SetTile(cell, grass); walls.SetTile(cell, art.rocks[(int)(n * 100) % art.rocks.Length]); break;
                        default:   // forest: trees (dead trees in the Valley of Serpents, the land of Mictlan)
                            ground.SetTile(cell, grass);
                            bool deadLand = x > 26 && n < 0.35f;
                            bool velyTree = art.velyTrees != null && art.velyTrees[0] != null && PixelCanvas.Hash(x, y, 55) < 0.35f;   // a third of the trees are Vely's
                            walls.SetTile(cell, deadLand ? art.trees[2] : velyTree ? art.velyTrees[(int)(n * 1000) % 2] : art.trees[(int)(n * 1000) % 2]);
                            break;
                    }
                }
            if (walls.GetComponent<TilemapCollider2D>() == null) walls.gameObject.AddComponent<TilemapCollider2D>();
        }

        /// <summary>Which sides of this water (or pit) cell touch land: decides the shape of the shore.</summary>
        static int LandMask(Kind[,] k, int x, int y, Kind kind)
        {
            bool Land(int cx, int cy)
            {
                if (cx < 0 || cy < 0 || cx >= MapArea.width || cy >= MapArea.height) return false;
                var other = k[cx, cy];
                if (kind == Kind.Water && (other == Kind.BridgeV || other == Kind.BridgeH)) return false;   // the bridge is over water
                return other != kind;
            }
            // North in the picture = up on the screen = y + 1 on the map.
            return TerrainArt.Mask(Land(x, y + 1), Land(x + 1, y), Land(x, y - 1), Land(x - 1, y));
        }

        /// <summary>Decides what every cell of the map is. Simple rules, no randomness that can block the path.</summary>
        static Kind[,] Plan()
        {
            int w = MapArea.width, h = MapArea.height;
            var k = new Kind[w, h];   // everything starts as forest

            // 1. Clearings (the 4 zones) with irregular forest edges.
            foreach (var room in Floors)
                foreach (var c in room.allPositionsWithin)
                {
                    int edge = Mathf.Min(Mathf.Min(c.x - room.xMin, room.xMax - 1 - c.x), Mathf.Min(c.y - room.yMin, room.yMax - 1 - c.y));
                    if (edge > 0 || PixelCanvas.Hash(c.x, c.y, 5) > 0.45f) k[c.x, c.y] = Kind.Floor;
                }

            // 2. Rivers between the zones, with a bridge where the path crosses.
            for (int x = 1; x < 57; x++)
                for (int y = 19; y <= 20; y++)
                    if (x < 25 || x > 26) k[x, y] = Kind.Water;                                 // river A|B and C|D
            for (int y = 21; y < 39; y++) for (int x = 25; x <= 26; x++) k[x, y] = Kind.Water;   // river B|C
            for (int x = 1; x < 57; x++)                                                         // the river meanders a little
                if (x < 24 || x > 27)
                {
                    if (PixelCanvas.Hash(x, 0, 21) > 0.65f && k[x, 18] == Kind.Floor) k[x, 18] = Kind.Water;
                    if (PixelCanvas.Hash(x, 1, 21) > 0.65f && k[x, 21] == Kind.Floor) k[x, 21] = Kind.Water;
                }
            for (int y = 18; y <= 21; y++) { k[11, y] = Kind.BridgeV; k[12, y] = Kind.BridgeV; k[41, y] = Kind.BridgeV; k[42, y] = Kind.BridgeV; }
            for (int x = 24; x <= 27; x++) { k[x, 30] = Kind.BridgeH; k[x, 31] = Kind.BridgeH; }

            // 3. A ridge of rocks between A and D (you cannot skip the zones).
            for (int y = 1; y < 19; y++) for (int x = 25; x <= 26; x++) k[x, y] = PixelCanvas.Hash(x, y, 8) < 0.6f ? Kind.Rock : Kind.Forest;

            // 4. The temple: stone floor surrounded by temple walls, with a door on the west side.
            foreach (var c in Temple.allPositionsWithin) k[c.x, c.y] = Kind.TempleFloor;
            for (int x = Temple.xMin - 1; x <= Temple.xMax; x++) { k[x, Temple.yMin - 1] = Kind.TempleWall; k[x, Temple.yMax] = Kind.TempleWall; }
            for (int y = Temple.yMin - 1; y <= Temple.yMax; y++) { k[Temple.xMin - 1, y] = Kind.TempleWall; if (Temple.xMax < w) k[Temple.xMax, y] = Kind.TempleWall; }
            for (int y = 9; y <= 10; y++) { k[57, y] = Kind.Path; k[58, y] = Kind.TempleFloor; }

            // 5. Ponds (round) and pits.
            foreach (var (center, radius) in Ponds)
                for (int x = (int)(center.x - radius - 1); x <= center.x + radius + 1; x++)
                    for (int y = (int)(center.y - radius - 1); y <= center.y + radius + 1; y++)
                        if (Inside(x, y) && k[x, y] == Kind.Floor && Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) <= radius)
                            k[x, y] = Kind.Water;
            foreach (var p in Pits)
                for (int x = p.x; x < p.x + 3; x++)
                    for (int y = p.y; y < p.y + 2; y++)
                        if (Inside(x, y) && k[x, y] == Kind.Floor && !(x == p.x + 2 && y == p.y + 1)) k[x, y] = Kind.Pit;

            // 6. The dirt path (always free) and the protected places (enemies, shop, start).
            var free = new bool[w, h];
            for (int i = 0; i + 1 < PathPoints.Length; i++)
            {
                var a = PathPoints[i]; var b = PathPoints[i + 1];
                for (int x = Mathf.Min(a.x, b.x); x <= Mathf.Max(a.x, b.x) + 1; x++)
                    for (int y = Mathf.Min(a.y, b.y); y <= Mathf.Max(a.y, b.y) + 1; y++)
                    {
                        if (!Inside(x, y)) continue;
                        if (k[x, y] == Kind.Floor || k[x, y] == Kind.Forest || k[x, y] == Kind.Pit) k[x, y] = Kind.Path;
                        for (int dx = -2; dx <= 2; dx++) for (int dy = -2; dy <= 2; dy++) if (Inside(x + dx, y + dy)) free[x + dx, y + dy] = true;
                    }
            }
            foreach (var c in ShopArea.allPositionsWithin) { k[c.x, c.y] = Kind.Floor; free[c.x, c.y] = true; }
            foreach (var c in PuzzleArea.allPositionsWithin) { k[c.x, c.y] = Kind.Floor; free[c.x, c.y] = true; }
            foreach (var (_, position) in Enemies) Protect(free, position, 2);
            Protect(free, PlayerStart, 2);

            // 7. Lonely trees and rocks inside the clearings (never on the path or next to an enemy).
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    if (k[x, y] != Kind.Floor || free[x, y]) continue;
                    float n = PixelCanvas.Hash(x, y, 77);
                    if (n < 0.045f) k[x, y] = Kind.Forest;
                    else if (n < 0.065f) k[x, y] = Kind.Rock;
                }
            return k;
        }

        static bool Inside(int x, int y) => x >= 0 && y >= 0 && x < MapArea.width && y < MapArea.height;

        static void Protect(bool[,] free, Vector2 position, int radius)
        {
            for (int dx = -radius; dx <= radius; dx++)
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int x = (int)position.x + dx, y = (int)position.y + dy;
                    if (Inside(x, y)) free[x, y] = true;
                }
        }

        // ---------------- Player with torch ----------------

        static PlayerCombatant PlacePlayer()
        {
            var player = Object.FindAnyObjectByType<PlayerCombatant>();
            player.transform.position = PlayerStart;
            SetupPlayerVisual(player.gameObject);
            var camera = Object.FindAnyObjectByType<Camera>();
            camera.transform.position = new Vector3(PlayerStart.x, PlayerStart.y, camera.transform.position.z);
            camera.backgroundColor = Color.black;
            return player;
        }

        /// <summary>
        /// Tlahuelli with the pixel art of Mictlán v5, animated in 4 directions (PlayerSpriteAnimator),
        /// a torch in the left hand and the Macuahuitl in the right hand (it swings when attacking).
        /// Used for the player of MainMap and for Player.prefab.
        /// </summary>
        public static void SetupPlayerVisual(GameObject player)
        {
            var visual = player.transform.Find("Visual");
            if (visual == null) return;
            var sr = visual.GetComponent<SpriteRenderer>();
            var down = LegacyArtImporter.Frames("tlahuelli", 0);
            if (down.Length == 0) return;
            sr.sprite = down[0];
            sr.color = Color.white;

            var torch = AddTorch(visual);
            var weapon = AddMacuahuitl(visual);

            var animator = player.GetComponent<PlayerSpriteAnimator>();
            if (animator == null) animator = player.AddComponent<PlayerSpriteAnimator>();
            var so = new SerializedObject(animator);
            so.FindProperty("sprite").objectReferenceValue = sr;
            SetArray(so.FindProperty("walkDown"), down);
            SetArray(so.FindProperty("walkUp"), LegacyArtImporter.Frames("tlahuelli", 1));
            SetArray(so.FindProperty("walkSide"), LegacyArtImporter.Frames("tlahuelli", 2));
            so.FindProperty("torch").objectReferenceValue = torch;
            so.FindProperty("torchFlame").objectReferenceValue = torch.GetComponent<SpriteRenderer>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(SerializedProperty list, Object[] items)
        {
            list.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        /// <summary>A burning torch in the LEFT hand (x below 0): two flame frames + a warm glow.</summary>
        public static Transform AddTorch(Transform holder, float x = -0.42f)
        {
            var old = holder.Find("Torch");
            if (old != null) Object.DestroyImmediate(old.gameObject, true);   // rebuild it with the current settings
            var torch = new GameObject("Torch");
            torch.transform.SetParent(holder, false);
            torch.transform.localPosition = new Vector3(x, 0.3f, 0f);
            var flame = torch.AddComponent<SpriteRenderer>();
            flame.sprite = ArtGenerator.Effect("torch_0");
            flame.sortingOrder = 1;
            torch.AddComponent<SpriteFlipbook>().Setup(new[] { ArtGenerator.Effect("torch_0"), ArtGenerator.Effect("torch_1") }, 0.15f);

            var glow = new GameObject("Glow");
            glow.transform.SetParent(torch.transform, false);
            glow.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            glow.transform.localScale = Vector3.one * 1.6f;
            var light = glow.AddComponent<SpriteRenderer>();
            light.sprite = ArtGenerator.Effect("glow");
            light.color = new Color(1f, 0.55f, 0.2f, 0.35f);
            light.sortingOrder = 501;   // above the darkness: warm light around the flame
            glow.AddComponent<SpriteFlipbook>().Setup(null, 1f, 0.06f);
            return torch.transform;
        }

        /// <summary>The Macuahuitl in the RIGHT hand, resting on the shoulder; MacuahuitlSwing animates the attack.</summary>
        static Transform AddMacuahuitl(Transform visual)
        {
            var old = visual.Find("Macuahuitl");
            if (old != null) Object.DestroyImmediate(old.gameObject, true);
            var weapon = new GameObject("Macuahuitl");
            weapon.transform.SetParent(visual, false);
            weapon.transform.localPosition = new Vector3(0.15f, 0.45f, 0f);   // the front hand (battle: looking right)
            var sr = weapon.AddComponent<SpriteRenderer>();
            sr.sprite = ArtGenerator.Effect("macuahuitl");
            sr.sortingOrder = 2;
            weapon.AddComponent<MacuahuitlSwing>();
            return weapon.transform;
        }

        /// <summary>The night: a big black sprite with a soft hole, glued to the camera.</summary>
        static void AddDarkness()
        {
            var camera = Object.FindAnyObjectByType<Camera>();
            var dark = new GameObject("Darkness");
            dark.layer = LayerMask.NameToLayer("TransparentFX");   // the minimap does not draw it
            dark.transform.SetParent(camera.transform, false);
            dark.transform.localPosition = new Vector3(0f, 0f, 10f);
            dark.transform.localScale = Vector3.one * 2.6f;
            var sr = dark.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtGenerator.DarknessPath);
            sr.sortingOrder = 500;
        }

        // ---------------- Enemies and boss ----------------

        static CombatEnemy Spawn(string prefabName, Vector2 position, Transform parent, string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{prefabName}.prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.position = position;
            return go.GetComponent<CombatEnemy>();
        }

        static void BuildBossZone()
        {
            var zone = new GameObject("BossZone").transform;
            var boss = Spawn("Boss_Quetzalcoatl", BossPosition, zone, "Boss_Quetzalcoatl");

            var gate = new GameObject("BossGate");
            gate.transform.SetParent(zone, false);
            gate.transform.position = GatePosition + new Vector2(0f, -1f);   // the sprite stands on its bottom
            var sprite = gate.AddComponent<SpriteRenderer>();
            sprite.sprite = PropArt.Sprite("temple_gate_closed");
            sprite.spriteSortPoint = SpriteSortPoint.Pivot;
            var gateBox = gate.AddComponent<BoxCollider2D>();
            gateBox.size = new Vector2(1f, 2f);
            gateBox.offset = new Vector2(0f, 1f);

            var arena = new GameObject("BossArena");
            arena.transform.SetParent(zone, false);
            arena.transform.position = ArenaTrigger;
            var box = arena.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = ArenaTriggerSize;
            var so = new SerializedObject(arena.AddComponent<BossArena>());
            so.FindProperty("boss").objectReferenceValue = boss;
            var gates = so.FindProperty("gates");
            gates.arraySize = 1;
            gates.GetArrayElementAtIndex(0).objectReferenceValue = gate;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------- Shop ----------------

        /// <summary>The merchant's tent against the forest: back cloth and shelves, the hooded merchant, and the counter.</summary>
        static void BuildShop(Text prompt)
        {
            var shop = new GameObject("Merchant (Shop)");
            shop.transform.position = ShopPosition;

            // The three layers of the tent are ordered inside a SortingGroup: back, merchant, front.
            var tent = new GameObject("Tent");
            tent.transform.SetParent(shop.transform, false);
            tent.AddComponent<UnityEngine.Rendering.SortingGroup>();
            Layer(tent.transform, "TentBack", PropArt.Sprite("tent_back"), Vector3.zero, 0);
            var merchant = Layer(tent.transform, "Merchant", PropArt.Sprite("merchant"), new Vector3(0f, 1.3f, 0f), 1);
            merchant.gameObject.AddComponent<SpriteFlipbook>().Setup(null, 1f, 0.02f);   // breathes slowly
            Layer(tent.transform, "TentFront", PropArt.Sprite("tent_front"), Vector3.zero, 2);

            // Solid: the whole tent (crates, barrel, counter and cloth). Nobody walks through it, player or enemy.
            var solid = shop.AddComponent<BoxCollider2D>();
            solid.offset = new Vector2(0f, 2f);
            solid.size = new Vector2(5f, 3.6f);

            // Warm light of the two lanterns (outside the SortingGroup, above the darkness).
            foreach (float x in new[] { -1.4f, 1.4f }) Glow(shop.transform, new Vector3(x, 2.2f, 0f), 2.2f, new Color(1f, 0.7f, 0.3f, 0.4f));

            // Where the player stands to talk (in front of the counter).
            var front = new GameObject("TalkArea");
            front.transform.SetParent(shop.transform, false);
            front.transform.localPosition = new Vector3(0f, -0.3f, 0f);
            var area = front.AddComponent<BoxCollider2D>();
            area.isTrigger = true;
            area.size = new Vector2(3.6f, 1.4f);
            var so = new SerializedObject(front.AddComponent<MerchantShop>());
            so.FindProperty("promptText").objectReferenceValue = prompt;
            so.FindProperty("menuFont").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>(ArtGenerator.FontPath);
            so.FindProperty("artifact").objectReferenceValue = FrontEndBuilder.EnsureArtifact();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static SpriteRenderer Layer(Transform parent, string name, Sprite sprite, Vector3 position, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        static void Glow(Transform parent, Vector3 position, float size, Color color)
        {
            var glow = new GameObject("Glow");
            glow.transform.SetParent(parent, false);
            glow.transform.localPosition = position;
            glow.transform.localScale = Vector3.one * size;
            var sr = glow.AddComponent<SpriteRenderer>();
            sr.sprite = ArtGenerator.Effect("glow");
            sr.color = color;
            sr.sortingOrder = 501;   // above the darkness
            glow.AddComponent<SpriteFlipbook>().Setup(null, 1f, 0.06f);
        }

        // ---------------- Entrance of the Celestial Temple ----------------

        /// <summary>Feathered-serpent heads guard the obsidian door, with braziers burning on both sides.</summary>
        static void BuildTempleEntrance()
        {
            var parent = new GameObject("TempleEntrance").transform;
            var door = KeyDoorPosition;
            Prop(parent, "SerpentHead_North", PropArt.Sprite("serpent_head"), new Vector2(door.x - 1.6f, door.y + 1.4f), false);
            Prop(parent, "SerpentHead_South", PropArt.Sprite("serpent_head"), new Vector2(door.x - 1.6f, door.y - 2.6f), false);
            foreach (var p in new[]
                     {
                         new Vector2(door.x - 3.2f, door.y + 1.6f), new Vector2(door.x - 3.2f, door.y - 2.4f),   // outside the door
                         new Vector2(64f, 12.5f), new Vector2(64f, 7f), new Vector2(72f, 12.5f), new Vector2(72f, 7f),   // the aisle inside
                         new Vector2(75f, 26f), new Vector2(82f, 26f), new Vector2(75f, 14f), new Vector2(82f, 14f),     // around the boss
                     })
            {
                var brazier = Prop(parent, "Brazier", PropArt.Sprite("brazier_0"), p, true);
                brazier.gameObject.AddComponent<SpriteFlipbook>().Setup(new[] { PropArt.Sprite("brazier_0"), PropArt.Sprite("brazier_1") }, 0.2f);
                Glow(brazier.transform, new Vector3(0f, 0.8f, 0f), 2.6f, new Color(1f, 0.55f, 0.2f, 0.4f));
            }
        }

        /// <summary>
        /// The inside of the Celestial Temple: serpent columns along the aisle and the great hall, the idol of
        /// Coatlicue behind Quetzalcoatl with a chacmool in front, tzompantli racks, banners and offerings.
        /// Nothing is placed on the aisle (rows 8-12) or in the fighting area around the boss.
        /// </summary>
        static void BuildTempleDecor()
        {
            var parent = new GameObject("TempleDecor").transform;
            foreach (var p in new[]
                     {
                         new Vector2(61.5f, 13.6f), new Vector2(67.5f, 13.6f), new Vector2(61.5f, 5.6f), new Vector2(67.5f, 5.6f),   // the aisle
                         new Vector2(66.5f, 18f), new Vector2(66.5f, 24f), new Vector2(66.5f, 30f),                                   // the great hall
                         new Vector2(86f, 30f), new Vector2(86f, 16f), new Vector2(72f, 32f), new Vector2(85f, 6f),
                     })
                Prop(parent, "SerpentColumn", PropArt.Sprite("temple_column"), p, true);

            var idol = Prop(parent, "Idol_Coatlicue", PropArt.Sprite("temple_idol"), new Vector2(78.5f, 30.5f), false);
            Glow(idol.transform, new Vector3(0f, 2.4f, 0f), 2f, new Color(0.3f, 0.9f, 0.6f, 0.25f));
            Prop(parent, "Chacmool", PropArt.Sprite("chacmool"), new Vector2(78.5f, 28.2f), false);
            Prop(parent, "Tzompantli", PropArt.Sprite("tzompantli"), new Vector2(61.8f, 28f), false);
            Prop(parent, "Tzompantli", PropArt.Sprite("tzompantli"), new Vector2(61.8f, 20f), false);

            foreach (float x in new[] { 64f, 70f, 74f, 83f })
                Decor(parent, "Banner", PropArt.Sprite("temple_banner"), new Vector2(x, 32.7f));
            foreach (float x in new[] { 63f, 70f })
                Decor(parent, "Banner", PropArt.Sprite("temple_banner"), new Vector2(x, 4.2f));
            foreach (var p in new[] { new Vector2(75.5f, 28.5f), new Vector2(81.5f, 28.5f), new Vector2(60.5f, 6.2f), new Vector2(60.5f, 13.4f), new Vector2(84.5f, 24f) })
                Decor(parent, "Offering", PropArt.Sprite("offering"), p);
        }

        /// <summary>Decoration you can walk past (no collider).</summary>
        static void Decor(Transform parent, string name, Sprite sprite, Vector2 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.spriteSortPoint = SpriteSortPoint.Pivot;
        }

        static SpriteRenderer Prop(Transform parent, string name, Sprite sprite, Vector2 position, bool solid)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.spriteSortPoint = SpriteSortPoint.Pivot;
            var box = go.AddComponent<BoxCollider2D>();   // statues and braziers cannot be walked through
            box.size = solid ? new Vector2(0.6f, 0.4f) : new Vector2(1.2f, 0.6f);
            box.offset = new Vector2(0f, box.size.y / 2f);
            return sr;
        }

        // ---------------- Obsidian key and the sealed door ----------------

        static void BuildKeyAndDoor()
        {
            var key = new GameObject("ObsidianKey");
            var keySprite = key.AddComponent<SpriteRenderer>();
            keySprite.sprite = ArtGenerator.Effect("key");
            keySprite.sortingOrder = 3;
            var pickup = key.AddComponent<CircleCollider2D>();
            pickup.isTrigger = true;
            pickup.radius = 0.6f;
            var k = new SerializedObject(key.AddComponent<ObsidianKey>());
            k.FindProperty("sprite").objectReferenceValue = keySprite;
            k.FindProperty("pickupArea").objectReferenceValue = pickup;
            k.ApplyModifiedPropertiesWithoutUndo();

            var door = new GameObject("ObsidianDoor");
            door.transform.position = KeyDoorPosition + new Vector2(0f, -1f);   // the sprite stands on its bottom
            var doorSprite = door.AddComponent<SpriteRenderer>();
            doorSprite.sprite = PropArt.Sprite("temple_gate_closed");
            doorSprite.spriteSortPoint = SpriteSortPoint.Pivot;
            var blocker = door.AddComponent<BoxCollider2D>();
            blocker.size = new Vector2(1f, 2f);
            blocker.offset = new Vector2(0f, 1f);
            var d = new SerializedObject(door.AddComponent<KeyDoor>());
            d.FindProperty("sprite").objectReferenceValue = doorSprite;
            d.FindProperty("closedSprite").objectReferenceValue = PropArt.Sprite("temple_gate_closed");
            d.FindProperty("openSprite").objectReferenceValue = PropArt.Sprite("temple_gate_open");
            d.FindProperty("blocker").objectReferenceValue = blocker;
            d.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------- Territory names (GDD) ----------------

        static void BuildTerritoryHints(ZoneTitleCard card)
        {
            var parent = new GameObject("Territories").transform;
            foreach (var (title, subtitle, area) in Territories)
            {
                var zone = new GameObject("Zone_" + title);
                zone.transform.SetParent(parent, false);
                zone.transform.position = new Vector3(area.x + area.width / 2f, area.y + area.height / 2f, 0f);
                var box = zone.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(area.width - 2, area.height - 2);
                var so = new SerializedObject(zone.AddComponent<ZoneArea>());
                so.FindProperty("zoneName").stringValue = title;
                so.FindProperty("subtitle").stringValue = subtitle;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ---------------- Puzzle: the shrine of the four fires (room C) ----------------

        static void BuildPuzzle(Text prompt)
        {
            var shrine = new GameObject("Puzzle (Four Fires)");
            var puzzle = shrine.AddComponent<BrazierPuzzle>();

            // Stela with the order (red, gold, jade, blue) and four braziers placed in a DIFFERENT order.
            var stela = Prop(shrine.transform, "Stela", PropArt.Sprite("stela"), new Vector2(39.5f, 36f), true);
            string[] gemsLeftToRight = { "gem_jade", "gem_red", "gem_blue", "gem_gold" };
            var braziers = new PuzzleBrazier[4];
            for (int i = 0; i < 4; i++)
            {
                var spot = new Vector2(30.5f + i * 2f, 36f);
                var bowl = Prop(shrine.transform, "Brazier_" + gemsLeftToRight[i], PropArt.Sprite("brazier_cold"), spot, true);
                bowl.gameObject.AddComponent<SpriteFlipbook>().Setup(new[] { PropArt.Sprite("brazier_0"), PropArt.Sprite("brazier_1") }, 0.2f);
                var gem = new GameObject("Gem");
                gem.transform.SetParent(bowl.transform, false);
                gem.transform.localPosition = new Vector3(0f, -0.25f, 0f);
                var gemSprite = gem.AddComponent<SpriteRenderer>();
                gemSprite.sprite = PropArt.Sprite(gemsLeftToRight[i]);
                gemSprite.sortingOrder = 502;   // bright in the dark
                var glowHolder = new GameObject("Light");
                glowHolder.transform.SetParent(bowl.transform, false);
                Glow(glowHolder.transform, new Vector3(0f, 0.8f, 0f), 2.6f, new Color(1f, 0.55f, 0.2f, 0.45f));
                var talk = new GameObject("LightArea");
                talk.transform.SetParent(bowl.transform, false);
                talk.transform.localPosition = new Vector3(0f, -0.8f, 0f);
                var area = talk.AddComponent<BoxCollider2D>();
                area.isTrigger = true;
                area.size = new Vector2(1.6f, 1.2f);
                var b = talk.AddComponent<PuzzleBrazier>();
                var bs = new SerializedObject(b);
                bs.FindProperty("bowl").objectReferenceValue = bowl;
                bs.FindProperty("unlitSprite").objectReferenceValue = PropArt.Sprite("brazier_cold");
                var frames = bs.FindProperty("litFrames");
                frames.arraySize = 2;
                frames.GetArrayElementAtIndex(0).objectReferenceValue = PropArt.Sprite("brazier_0");
                frames.GetArrayElementAtIndex(1).objectReferenceValue = PropArt.Sprite("brazier_1");
                bs.FindProperty("lightGlow").objectReferenceValue = glowHolder;
                bs.ApplyModifiedPropertiesWithoutUndo();
                braziers[i] = b;
            }

            // The treasure that appears when the puzzle is solved.
            var chest = Prop(shrine.transform, "Treasure", PropArt.Sprite("chest"), new Vector2(34.5f, 33.6f), false);
            var chestBox = chest.GetComponent<BoxCollider2D>();
            chestBox.isTrigger = true;
            var c = new SerializedObject(chest.gameObject.AddComponent<TreasureChest>());
            c.FindProperty("puzzle").objectReferenceValue = puzzle;
            c.ApplyModifiedPropertiesWithoutUndo();

            // Correct order = the stela: red, gold, jade, blue.
            var p = new SerializedObject(puzzle);
            var order = p.FindProperty("correctOrder");
            order.arraySize = 4;
            order.GetArrayElementAtIndex(0).objectReferenceValue = braziers[1];   // red
            order.GetArrayElementAtIndex(1).objectReferenceValue = braziers[3];   // gold
            order.GetArrayElementAtIndex(2).objectReferenceValue = braziers[0];   // jade
            order.GetArrayElementAtIndex(3).objectReferenceValue = braziers[2];   // blue
            p.FindProperty("treasure").objectReferenceValue = chest.gameObject;
            p.FindProperty("promptText").objectReferenceValue = prompt;
            p.ApplyModifiedPropertiesWithoutUndo();

            // A hint stone next to the stela.
            var hint = new GameObject("PuzzleHint");
            hint.transform.SetParent(shrine.transform, false);
            hint.transform.position = new Vector2(35f, 34.5f);
            var box = hint.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(11f, 4f);
            var h = new SerializedObject(hint.AddComponent<TutorialZone>());
            h.FindProperty("id").stringValue = "puzzle_hint";
            h.FindProperty("message").stringValue = "THE SHRINE OF THE FOUR FIRES\nThe stela shows four colours. Light the braziers ({Interact}) in that order.";
            h.ApplyModifiedPropertiesWithoutUndo();
            _ = stela;
        }

        // ---------------- Quick time event box ("Press [A] to DODGE!") ----------------

        static void BuildQuickTime(Transform root, Font font)
        {
            var go = new GameObject("QuickTimePrompt", typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)go.transform;
            rect.SetParent(root, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 70f);
            rect.sizeDelta = new Vector2(420f, 70f);
            var group = go.AddComponent<CanvasGroup>();
            var plate = go.AddComponent<Image>();
            plate.color = new Color(0f, 0f, 0f, 0.75f);

            Text Part(string name, float x, float width, int size)
            {
                var t = new GameObject(name, typeof(RectTransform)).AddComponent<Text>();
                t.transform.SetParent(rect, false);
                var r = t.rectTransform;
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = new Vector2(x, 0f);
                r.sizeDelta = new Vector2(width, 60f);
                t.font = font; t.fontSize = size; t.color = Color.white; t.alignment = TextAnchor.MiddleCenter;
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.gameObject.AddComponent<Outline>().effectColor = Color.black;
                return t;
            }
            var before = Part("Press", -130f, 120f, 30);
            var after = Part("ToVerb", 110f, 220f, 30);

            var keyGo = new GameObject("KeyBox", typeof(RectTransform));
            var keyRect = (RectTransform)keyGo.transform;
            keyRect.SetParent(rect, false);
            keyRect.anchorMin = keyRect.anchorMax = keyRect.pivot = new Vector2(0.5f, 0.5f);
            keyRect.anchoredPosition = new Vector2(-45f, 0f);
            keyRect.sizeDelta = new Vector2(54f, 54f);
            var keyImage = keyGo.AddComponent<Image>();
            var border = keyGo.AddComponent<Outline>();
            border.effectColor = Color.white;
            border.effectDistance = new Vector2(3f, -3f);
            var keyLabel = new GameObject("Key", typeof(RectTransform)).AddComponent<Text>();
            keyLabel.transform.SetParent(keyRect, false);
            keyLabel.rectTransform.anchorMin = Vector2.zero; keyLabel.rectTransform.anchorMax = Vector2.one;
            keyLabel.rectTransform.offsetMin = keyLabel.rectTransform.offsetMax = Vector2.zero;
            keyLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            keyLabel.fontSize = 36; keyLabel.fontStyle = FontStyle.Bold; keyLabel.color = Color.black;
            keyLabel.alignment = TextAnchor.MiddleCenter;

            var q = new SerializedObject(go.AddComponent<QuickTimePrompt>());
            q.FindProperty("group").objectReferenceValue = group;
            q.FindProperty("before").objectReferenceValue = before;
            q.FindProperty("keyBox").objectReferenceValue = keyRect;
            q.FindProperty("keyBoxImage").objectReferenceValue = keyImage;
            q.FindProperty("keyLabel").objectReferenceValue = keyLabel;
            q.FindProperty("after").objectReferenceValue = after;
            q.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------- Camera: less zoom; combat HUD with the artifact row ----------------

        static void SetZoomAndCombat()
        {
            var camera = Object.FindAnyObjectByType<Camera>();
            camera.orthographicSize = ExplorationZoom;
            var a = new SerializedObject(Object.FindAnyObjectByType<CombatArena>());
            a.FindProperty("combatOrthographicSize").floatValue = CombatZoom;
            a.ApplyModifiedPropertiesWithoutUndo();

            // Artifact slot (key 6) and every ability known by the save.
            var t = new SerializedObject(Object.FindAnyObjectByType<PlayerTurn>());
            t.FindProperty("artifact").objectReferenceValue = FrontEndBuilder.EnsureArtifact();
            t.ApplyModifiedPropertiesWithoutUndo();
            FrontEndBuilder.RefreshSaveAbilities();

            // The tips box goes at the top centre, between the life bars (left) and the minimap (right).
            var hint = GameObject.Find("TutorialHint");
            if (hint != null)
            {
                var rect = (RectTransform)hint.transform;
                rect.anchoredPosition = new Vector2(0f, -10f);
                rect.sizeDelta = new Vector2(460f, 70f);
            }

            // Rebuild the combat HUD so it has the 6th command row.
            var hud = GameObject.Find("CombatHUD");
            if (hud != null) Object.DestroyImmediate(hud);
            CombatHUDBuilder.Build();
        }

        // ---------------- Minimap, gold and shop text ----------------

        static (Text shopPrompt, ZoneTitleCard zoneTitle) BuildMapUI(PlayerCombatant player)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(ArtGenerator.FontPath);
            var frame = AssetDatabase.LoadAssetAtPath<Sprite>(ArtGenerator.FramePath);
            var canvasGo = new GameObject("MapUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 540);
            scaler.matchWidthOrHeight = 1f;
            var root = canvasGo.transform;

            // Minimap (top left, under the life bars)
            var mapFrame = Box("Minimap", root, frame, new Vector2(1, 1), new Vector2(-10, -50), new Vector2(208, 100));
            var group = mapFrame.gameObject.AddComponent<CanvasGroup>();
            var image = new GameObject("Map", typeof(RectTransform)).AddComponent<RawImage>();
            image.transform.SetParent(mapFrame, false);
            image.rectTransform.sizeDelta = new Vector2(192, 87);
            var dot = new GameObject("PlayerArrow", typeof(RectTransform)).AddComponent<Image>();
            dot.transform.SetParent(image.transform, false);
            dot.rectTransform.sizeDelta = new Vector2(13, 13);
            dot.sprite = PropArt.Sprite("map_arrow");   // turns with the direction Tlahuelli is looking
            var minimap = mapFrame.gameObject.AddComponent<Minimap>();
            var mm = new SerializedObject(minimap);
            mm.FindProperty("player").objectReferenceValue = player.transform;
            mm.FindProperty("mapImage").objectReferenceValue = image;
            mm.FindProperty("playerDot").objectReferenceValue = dot.rectTransform;
            mm.FindProperty("group").objectReferenceValue = group;
            mm.ApplyModifiedPropertiesWithoutUndo();

            // Gold counter (under the minimap)
            var goldBox = Box("Gold", root, frame, new Vector2(1, 1), new Vector2(-10, -10), new Vector2(208, 34));
            var goldText = Label(goldBox, font, "GOLD  0", 16, MexicaPalette.AgedGold);
            var gold = goldBox.gameObject.AddComponent<GoldCounter>();
            var g = new SerializedObject(gold);
            g.FindProperty("player").objectReferenceValue = player.GetComponent<PlayerProgression>();
            g.FindProperty("label").objectReferenceValue = goldText;
            g.ApplyModifiedPropertiesWithoutUndo();

            // Shop message (bottom centre), hidden until you stand at the shop
            var shopBox = Box("ShopPrompt", root, frame, new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(460, 34));
            var shopText = Label(shopBox, font, "", 17, MexicaPalette.Bone);

            // Zone title (big, in the upper middle of the screen, like GTA)
            var titleRoot = new GameObject("ZoneTitle", typeof(RectTransform));
            titleRoot.layer = LayerMask.NameToLayer("UI");
            var titleRect = (RectTransform)titleRoot.transform;
            titleRect.SetParent(root, false);
            titleRect.anchorMin = titleRect.anchorMax = titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 120f);
            titleRect.sizeDelta = new Vector2(700f, 110f);
            var titleGroup = titleRoot.AddComponent<CanvasGroup>();
            var big = Label(titleRect, font, "", 46, MexicaPalette.Bone);
            big.rectTransform.offsetMin = new Vector2(0f, 35f);
            var shadow = big.gameObject.AddComponent<Outline>();
            shadow.effectColor = Color.black;
            shadow.effectDistance = new Vector2(2f, -2f);
            var small = Label(titleRect, font, "", 20, MexicaPalette.AgedGold);
            small.rectTransform.offsetMax = new Vector2(0f, -70f);
            small.gameObject.AddComponent<Outline>().effectColor = Color.black;
            var card = titleRoot.AddComponent<ZoneTitleCard>();
            var z = new SerializedObject(card);
            z.FindProperty("group").objectReferenceValue = titleGroup;
            z.FindProperty("title").objectReferenceValue = big;
            z.FindProperty("subtitle").objectReferenceValue = small;
            z.ApplyModifiedPropertiesWithoutUndo();
            BuildQuickTime(root, font);
            return (shopText, card);
        }

        static RectTransform Box(string name, Transform parent, Sprite frame, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(anchor.x, anchor.y == 0 ? 0 : 1);
            if (anchor.x == 0.5f) rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.sprite = frame;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 3f;   // thin border for small boxes
            image.raycastTarget = false;
            return rect;
        }

        static Text Label(RectTransform parent, Font font, string text, int size, Color color)
        {
            var t = new GameObject("Text", typeof(RectTransform)).AddComponent<Text>();
            t.transform.SetParent(parent, false);
            t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
            t.font = font; t.fontSize = size; t.color = color; t.text = text;
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            return t;
        }

        // ---------------- Gore ----------------

        static void AddGore()
        {
            var gore = new GameObject("GoreEffects").AddComponent<GoreEffects>();
            var so = new SerializedObject(gore);
            var blood = so.FindProperty("bloodStains");
            blood.arraySize = 6;
            for (int i = 0; i < 6; i++) blood.GetArrayElementAtIndex(i).objectReferenceValue = ArtGenerator.Effect($"blood_{i}");
            var gibs = so.FindProperty("gibs");
            gibs.arraySize = 12;
            int n = 0;
            for (int a = 0; a < 4; a++)
                for (int b = 0; b < 3; b++)
                    gibs.GetArrayElementAtIndex(n++).objectReferenceValue = ArtGenerator.Effect($"gib_{a}_{b}");
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
