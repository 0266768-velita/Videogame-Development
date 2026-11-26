using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// Builds MainMap_CSV: a copy of Alex's MainMap (same player, combat, HUD, shop, puzzle, boss, key and
    /// darkness) whose world is painted from the team's CSV map, and whose objects are moved to where the CSV
    /// puts them: the player on S, enemies on E, chests on K, the boss inside the pyramid (M / I) with a sealed
    /// obsidian door on every entrance, and the shrine puzzle next to the house H.
    /// MainMap is never touched. It never overwrites MainMap_CSV: delete it to rebuild.
    /// Menu: TD_Game > Build CSV Map (MainMap_CSV).
    /// </summary>
    public static class CsvMapBuilder
    {
        public const string ScenePath = "Assets/TD_Game/Scenes/Levels/MainMap_CSV.unity";
        public const string CsvPath = "Assets/TD_Game/Settings/Maps/mapa_juego_150x70.csv";
        const string PrefabFolder = "Assets/TD_Game/Prefabs/Characters";

        static CsvMap map;
        static CsvMapPainter.Ground[,] plan;

        [MenuItem("TD_Game/Build CSV Map (MainMap_CSV)")]
        public static void Build()
        {
            if (File.Exists(ScenePath)) { Debug.Log("[TD_Game] MainMap_CSV already exists (delete it to rebuild)."); return; }
            if (!File.Exists(MainMapBuilder.MainMapPath)) { Debug.LogError("[TD_Game] Build MainMap first: MainMap_CSV is a copy of it."); return; }
            map = CsvMap.Load(CsvPath);
            if (map == null) return;

            AssetDatabase.CopyAsset(MainMapBuilder.MainMapPath, ScenePath);
            var scene = EditorSceneManager.OpenScene(ScenePath);

            plan = CsvMapPainter.Plan(map);
            CsvMapPainter.Paint(map, plan);
            var start = map.Find('S');
            var distance = map.Distances(start, Walkable);

            PlacePlayer(map.World(start));
            PlaceEnemies(distance);
            var entrances = PlaceTemple();
            PlaceShop(start);
            PlaceHouseAndPuzzle();
            PlaceChests(distance);
            PlaceTerritories();
            AddBorder();
            AddToBuildSettings();
            Report(start, entrances);

            AssetDatabase.SaveAssets();   // the new sand tiles
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[TD_Game] MainMap_CSV built from " + CsvPath);
        }

        static bool Walkable(Vector2Int cell) => map.Inside(cell.x, cell.y) && CsvMapPainter.IsWalkable(plan[cell.x, cell.y]);

        static Vector2Int CellAt(Vector2 world) =>
            new Vector2Int(Mathf.FloorToInt(world.x / CsvMap.TilesPerCell), map.Height - 1 - Mathf.FloorToInt(world.y / CsvMap.TilesPerCell));

        // ---------------- Player ----------------

        static void PlacePlayer(Vector2 position)
        {
            var player = Object.FindAnyObjectByType<PlayerCombatant>();
            player.transform.position = position;
            var camera = Object.FindAnyObjectByType<Camera>();
            camera.transform.position = new Vector3(position.x, position.y, camera.transform.position.z);
        }

        // ---------------- Enemies (E) ----------------

        /// <summary>One enemy per E group (two in the big groups). Wolves in the north-west forest, witches in the
        /// south-west forest, giants, witches and wolves in the desert. The farther from S, the stronger.</summary>
        static void PlaceEnemies(int[,] distance)
        {
            var root = GameObject.Find("Enemies").transform;
            for (int i = root.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.GetChild(i).gameObject);

            var groups = map.Groups('E').OrderBy(g => Steps(g, distance)).ToList();
            string[] desert = { "Enemy_Giant", "Enemy_Witch", "Enemy_Wolf" };
            int desertCount = 0, n = 0;
            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                var first = group[0];
                string prefab = first.x < 64 ? (first.y < 36 ? "Enemy_Wolf" : "Enemy_Witch") : desert[desertCount++ % desert.Length];
                float strength = 1.2f + 0.8f * i / Mathf.Max(1, groups.Count - 1);
                var spots = group.Count >= 4 ? new[] { map.World(group[0]), map.World(group[group.Count - 1]) } : new[] { map.Center(group) };
                foreach (var spot in spots) Spawn(prefab, spot, root, $"{prefab}_{++n}", strength);
            }
        }

        static int Steps(List<Vector2Int> group, int[,] distance)
        {
            int best = int.MaxValue;
            foreach (var cell in group)
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        if (map.Inside(cell.x + dx, cell.y + dy) && distance[cell.x + dx, cell.y + dy] >= 0)
                            best = Mathf.Min(best, distance[cell.x + dx, cell.y + dy]);
            return best;
        }

        static CombatEnemy Spawn(string prefabName, Vector2 position, Transform parent, string name, float strength)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{prefabName}.prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.position = position;
            var enemy = go.GetComponent<CombatEnemy>();
            var so = new SerializedObject(enemy);
            var power = so.FindProperty("strength");
            if (power != null) { power.floatValue = strength; so.ApplyModifiedPropertiesWithoutUndo(); }
            return enemy;
        }

        // ---------------- Pyramid: boss, doors and gates ----------------

        /// <summary>Puts Quetzalcóatl in the most open spot of the great hall, a sealed obsidian door (KeyDoor) on
        /// every entrance of the pyramid and a boss gate behind each one. Returns the entrances (world rectangles).</summary>
        static List<Rect> PlaceTemple()
        {
            var outdoors = map.Outdoors();
            bool Pyramid(Vector2Int c) => map[c] == 'I' && !map.IsCave(c);
            var entrances = map.Groups(c => Pyramid(c) && CsvMap.Neighbours4(c).Any(o => map.Inside(o.x, o.y) && outdoors[o.x, o.y] && Walkable(o)))
                .Select(g => map.Bounds(g)).ToList();

            // The great hall is the upper part of the pyramid: the boss stands where it is widest.
            var hall = new List<Vector2Int>();
            for (int r = 0; r <= 15; r++) for (int c = 0; c < map.Width; c++) if (Pyramid(new Vector2Int(c, r))) hall.Add(new Vector2Int(c, r));
            Vector2 middle = map.Center(hall);
            var bossCell = hall.OrderByDescending(Openness).ThenBy(c => Vector2.Distance(map.World(c), middle)).First();
            Vector2 bossPosition = map.World(bossCell);

            var zone = GameObject.Find("BossZone").transform;
            zone.GetComponentInChildren<CombatEnemy>(true).transform.position = bossPosition;
            var arena = zone.GetComponentInChildren<BossArena>(true);
            arena.transform.position = bossPosition;
            FitBossArena(arena, hall);

            var gateTemplate = zone.Find("BossGate").gameObject;
            var doorTemplate = GameObject.Find("ObsidianDoor");
            var gates = new List<GameObject>();
            for (int i = 0; i < entrances.Count; i++)
            {
                var door = i == 0 ? doorTemplate : Object.Instantiate(doorTemplate);
                door.name = i == 0 ? "ObsidianDoor" : $"ObsidianDoor_{i + 1}";
                Block(door, entrances[i]);
                var gate = i == 0 ? gateTemplate : Object.Instantiate(gateTemplate, zone);
                gate.name = i == 0 ? "BossGate" : $"BossGate_{i + 1}";
                Block(gate, entrances[i]);
                gates.Add(gate);
            }
            var so = new SerializedObject(arena);
            var list = so.FindProperty("gates");
            list.arraySize = gates.Count;
            for (int i = 0; i < gates.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = gates[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            DecorateTemple(entrances, bossPosition);
            return entrances;
        }

        /// <summary>
        /// The arena trigger covers the whole great hall, 3 cells back from its edges: the gates (in the doorways) close
        /// well behind the player, and always before Quetzalcoatl can see them (his sight is wider than a small box).
        /// </summary>
        static void FitBossArena(BossArena arena, List<Vector2Int> hall)
        {
            var area = map.Bounds(hall);
            const float back = 3f * CsvMap.TilesPerCell;
            var trigger = arena.GetComponent<BoxCollider2D>();
            trigger.offset = area.center - (Vector2)arena.transform.position;
            trigger.size = new Vector2(Mathf.Max(4f, area.width - 2f * back), Mathf.Max(4f, area.height - 2f * back));
        }

        /// <summary>Re-fits the arena of the open MainMap_CSV (used once to update the existing scene).</summary>
        public static void RefitBossArenaInOpenScene()
        {
            map = CsvMap.Load(CsvPath);
            var hall = new List<Vector2Int>();
            for (int r = 0; r <= 15; r++) for (int c = 0; c < map.Width; c++) if (map[c, r] == 'I' && !map.IsCave(new Vector2Int(c, r))) hall.Add(new Vector2Int(c, r));
            FitBossArena(Object.FindAnyObjectByType<BossArena>(), hall);
        }

        /// <summary>How many rings of floor surround a cell (0 = touches a wall).</summary>
        static int Openness(Vector2Int cell)
        {
            for (int k = 1; k <= 6; k++)
                for (int dx = -k; dx <= k; dx++)
                    for (int dy = -k; dy <= k; dy++)
                        if (!Walkable(new Vector2Int(cell.x + dx, cell.y + dy))) return k - 1;
            return 6;
        }

        /// <summary>Fills an entrance with a door or gate: the 1 x 2 gate picture drawn at x2 (2 x 4 tiles = 1 x 2
        /// cells) and a collider over the whole entrance.</summary>
        static void Block(GameObject go, Rect area)
        {
            const float scale = 2f;
            go.transform.position = new Vector3(area.center.x, area.yMin, 0f);
            go.transform.localScale = Vector3.one * scale;
            var box = go.GetComponent<BoxCollider2D>();
            box.size = area.size / scale;
            box.offset = new Vector2(0f, area.height / scale / 2f);
        }

        static void DecorateTemple(List<Rect> entrances, Vector2 boss)
        {
            foreach (var name in new[] { "TempleEntrance", "TempleDecor" })
            {
                var old = GameObject.Find(name);
                if (old != null) Object.DestroyImmediate(old);
            }
            var parent = new GameObject("TempleDecor_CSV").transform;

            // Braziers on both sides of every door, on the outside.
            foreach (var door in entrances)
                foreach (var side in new[] { new Vector2(-2.5f, 0f), new Vector2(2.5f, 0f), new Vector2(0f, -2.5f), new Vector2(0f, 2.5f) })
                {
                    var spot = door.center + side * (door.width > door.height ? new Vector2(1.4f, 1f) : new Vector2(1f, 1.4f));
                    if (Walkable(CellAt(spot)) && !door.Contains(spot)) Brazier(parent, spot);
                }

            // The great hall: braziers around Quetzalcóatl and the idol of Coatlicue behind him.
            foreach (var corner in new[] { new Vector2(-5f, 3.5f), new Vector2(5f, 3.5f), new Vector2(-5f, -3.5f), new Vector2(5f, -3.5f) })
                if (Walkable(CellAt(boss + corner))) Brazier(parent, boss + corner);
            var idolSpot = boss + new Vector2(0f, 5f);
            if (Walkable(CellAt(idolSpot)))
            {
                var idol = Prop(parent, "Idol_Coatlicue", PropArt.Sprite("temple_idol"), idolSpot, false);
                Glow(idol.transform, new Vector3(0f, 2.4f, 0f), 2f, new Color(0.3f, 0.9f, 0.6f, 0.25f));
            }
        }

        static void Brazier(Transform parent, Vector2 position)
        {
            var brazier = Prop(parent, "Brazier", PropArt.Sprite("brazier_0"), position, true);
            brazier.gameObject.AddComponent<SpriteFlipbook>().Setup(new[] { PropArt.Sprite("brazier_0"), PropArt.Sprite("brazier_1") }, 0.2f);
            Glow(brazier.transform, new Vector3(0f, 0.8f, 0f), 2.6f, new Color(1f, 0.55f, 0.2f, 0.4f));
        }

        static SpriteRenderer Prop(Transform parent, string name, Sprite sprite, Vector2 position, bool small)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.spriteSortPoint = SpriteSortPoint.Pivot;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = small ? new Vector2(0.6f, 0.4f) : new Vector2(1.2f, 0.6f);
            box.offset = new Vector2(0f, box.size.y / 2f);
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
            sr.sortingOrder = 501;   // above the darkness of the map
            glow.AddComponent<SpriteFlipbook>().Setup(null, 1f, 0.06f);
        }

        // ---------------- Shop, house H and the shrine puzzle ----------------

        /// <summary>The merchant's tent on clear grass near the start (it never sits on the path).</summary>
        static void PlaceShop(Vector2Int start)
        {
            var shop = GameObject.Find("Merchant (Shop)");
            if (shop == null) return;
            var spot = ClearSpot(start + new Vector2Int(8, 4), 3, 3);
            shop.transform.position = new Vector2(spot.center.x, spot.yMin + 1.5f);
        }

        static void PlaceHouseAndPuzzle()
        {
            var houseCells = map.Groups('H').SelectMany(g => g).ToList();
            if (houseCells.Count == 0) return;
            var area = map.Bounds(houseCells);

            var house = new GameObject("House of the Sword (H)");
            house.transform.position = new Vector2(area.center.x, area.center.y - 2.75f);   // the 7 x 5.5 picture centred on the H
            var sr = house.AddComponent<SpriteRenderer>();
            sr.sprite = CsvMapPainter.HouseSprite();
            sr.spriteSortPoint = SpriteSortPoint.Pivot;
            var solid = house.AddComponent<BoxCollider2D>();
            solid.size = new Vector2(6.4f, 2.4f);
            solid.offset = new Vector2(0f, 1.3f);

            var hint = new GameObject("HouseHint");
            hint.transform.SetParent(house.transform, false);
            hint.transform.localPosition = new Vector3(0f, -0.8f, 0f);
            var box = hint.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(4f, 1.6f);
            var tip = new SerializedObject(hint.AddComponent<TutorialZone>());
            tip.FindProperty("id").stringValue = "house_sword";
            tip.FindProperty("message").stringValue =
                "THE HOUSE OF THE SWORD\nThe old warrior sealed his treasure with four fires. Light the braziers of the shrine ({Interact}) in the order of the stela.";
            tip.ApplyModifiedPropertiesWithoutUndo();

            // Alex's shrine of the four fires moves next to the house, on clear grass.
            var shrine = GameObject.Find("Puzzle (Four Fires)");
            if (shrine == null) return;
            var renderers = shrine.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            var spot = ClearSpot(CellAt(new Vector2(area.center.x, area.yMin)) + new Vector2Int(-3, 3), 8, 4);
            shrine.transform.position += (Vector3)(spot.center - (Vector2)bounds.center);
        }

        /// <summary>The nearest rectangle of w x h cells (spiral search from a cell) that is all grass.</summary>
        static Rect ClearSpot(Vector2Int near, int w, int h)
        {
            bool Clear(int c0, int r0)
            {
                for (int c = c0; c < c0 + w; c++)
                    for (int r = r0; r < r0 + h; r++)
                    {
                        if (!map.Inside(c, r) || plan[c, r] != CsvMapPainter.Ground.Grass) return false;
                        char s = map[c, r];
                        if (s == 'E' || s == 'K' || s == 'H' || s == 'S') return false;
                    }
                return true;
            }
            for (int k = 0; k < 40; k++)
                for (int dx = -k; dx <= k; dx++)
                    for (int dy = -k; dy <= k; dy++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != k) continue;
                        int c0 = near.x + dx, r0 = near.y + dy;
                        if (Clear(c0, r0))
                            return map.Bounds(new List<Vector2Int> { new Vector2Int(c0, r0), new Vector2Int(c0 + w - 1, r0 + h - 1) });
                    }
            Debug.LogWarning("[TD_Game] No clear spot near " + near);
            return map.Bounds(new List<Vector2Int> { near });
        }

        // ---------------- Chests (K) ----------------

        static void PlaceChests(int[,] distance)
        {
            var parent = new GameObject("Chests").transform;
            var groups = map.Groups('K').OrderBy(g => Steps(g, distance)).ToList();
            for (int i = 0; i < groups.Count; i++)
            {
                bool cave = map.IsCave(groups[i][0]);
                var go = new GameObject($"Chest_{i + 1}");
                go.transform.SetParent(parent, false);
                go.transform.position = map.Center(groups[i]);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = PropArt.Sprite("chest");
                sr.spriteSortPoint = SpriteSortPoint.Pivot;
                var box = go.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(1.4f, 1.2f);
                var so = new SerializedObject(go.AddComponent<LootChest>());
                so.FindProperty("sprite").objectReferenceValue = sr;
                so.FindProperty("gold").intValue = cave ? 100 : 20 + 15 * i;
                so.FindProperty("potions").intValue = 1;
                so.FindProperty("heal").floatValue = 20f + 5f * i;
                so.FindProperty("title").stringValue = cave ? "THE TREASURE OF THE CAVE" : "AN OFFERING FOR THE DEAD";
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ---------------- Territories, border, build settings ----------------

        static void PlaceTerritories()
        {
            var root = GameObject.Find("Territories");
            if (root == null) return;
            var pyramid = new List<Vector2Int>();
            for (int r = 0; r < map.Height; r++)
                for (int c = 0; c < map.Width; c++)
                    if (CsvMap.IsBuilding(map[c, r]) && !map.IsCave(new Vector2Int(c, r))) pyramid.Add(new Vector2Int(c, r));

            foreach (var zone in root.GetComponentsInChildren<ZoneArea>())
            {
                string name = zone.name.ToUpperInvariant();
                Rect area = name.Contains("TEMPLE") ? map.Bounds(pyramid)
                    : name.Contains("TLAZ") ? CellRect(0, 36, 66, 34)
                    : name.Contains("SERP") ? CellRect(64, 0, 86, 70)
                    : CellRect(0, 0, 64, 36);   // Territories of Tláloc: the start
                zone.transform.position = area.center;
                var box = zone.GetComponent<BoxCollider2D>();
                box.offset = Vector2.zero;
                box.size = area.size - Vector2.one * 2f;
            }
        }

        static Rect CellRect(int column, int row, int width, int height) =>
            map.Bounds(new List<Vector2Int> { new Vector2Int(column, row), new Vector2Int(column + width - 1, row + height - 1) });

        /// <summary>Invisible walls around the map: the CSV edge is open land, nobody may walk off it.</summary>
        static void AddBorder()
        {
            float w = CsvMap.PlayableColumns * CsvMap.TilesPerCell, h = map.Height * CsvMap.TilesPerCell;
            var border = new GameObject("MapBorder");
            foreach (var (offset, size) in new[]
                     {
                         (new Vector2(-1f, h / 2f), new Vector2(2f, h + 4f)), (new Vector2(w + 1f, h / 2f), new Vector2(2f, h + 4f)),
                         (new Vector2(w / 2f, -1f), new Vector2(w + 4f, 2f)), (new Vector2(w / 2f, h + 1f), new Vector2(w + 4f, 2f)),
                     })
            {
                var box = border.AddComponent<BoxCollider2D>();
                box.offset = offset;
                box.size = size;
            }
        }

        static void AddToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == ScenePath)) return;
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>Checks that everything can be reached from S, and that the boss can only be reached through a door.</summary>
        static void Report(Vector2Int start, List<Rect> entrances)
        {
            var open = map.Distances(start, Walkable);
            var sealedMap = map.Distances(start, c => Walkable(c) && !entrances.Any(e => e.Contains(map.World(c))));
            int Reached(char symbol, int[,] d) => map.Groups(symbol).Count(g => Steps(g, d) < int.MaxValue);
            var boss = CellAt(Object.FindAnyObjectByType<BossArena>().transform.position);
            Debug.Log($"[TD_Game] CSV map {map.Width}x{map.Height} cells ({map.Width * 2}x{map.Height * 2} tiles). " +
                      $"From S: E groups {Reached('E', open)}/{map.Groups('E').Count}, K groups {Reached('K', open)}/{map.Groups('K').Count}, " +
                      $"house {Reached('H', open)}/{map.Groups('H').Count}. Pyramid entrances {entrances.Count}. " +
                      $"Boss reachable with doors open: {open[boss.x, boss.y] >= 0}, with doors sealed: {sealedMap[boss.x, boss.y] >= 0}.");
        }
    }
}
