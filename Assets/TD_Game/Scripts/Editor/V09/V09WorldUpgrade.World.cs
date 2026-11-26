using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TD_Game.EditorTools
{
    /// <summary>v0.9 upgrade: the pyramid, natural obstacles, landmarks, rocks and the details of each region.</summary>
    public static partial class V09WorldUpgrade
    {
        static Sprite S(string name) => PropArt.Sprite(name);
        static Sprite Tile(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{TerrainArt.Folder}/{name}.png");

        // ---------------- Placement helpers ----------------

        static void ReserveExisting()
        {
            foreach (string name in new[] { "Chests", "House of the Sword (H)", "Puzzle (Four Fires)", "Merchant (Shop)", "TempleDecor_CSV", "BossZone", "ObsidianDoor", "ObsidianDoor_2" })
            {
                var go = GameObject.Find(name);
                if (go == null) continue;
                foreach (var t in go.GetComponentsInChildren<Transform>()) occupied.Add((t.position, 1.6f));
            }
            var player = Object.FindAnyObjectByType<PlayerCombatant>();
            if (player != null) occupied.Add((player.transform.position, 4f));
        }

        /// <summary>True if a circle of this radius (tiles) is on open ground of the given kind and touches nothing placed.</summary>
        static bool Free(Vector2 p, float radius, System.Func<K, bool> ground, bool awayFromPaths = false)
        {
            foreach (var (q, r) in occupied) if (Vector2.Distance(p, q) < radius + r) return false;
            for (float dx = -radius; dx <= radius + 0.01f; dx += 0.5f)
                for (float dy = -radius; dy <= radius + 0.01f; dy += 0.5f)
                {
                    var cell = Cell(p + new Vector2(dx, dy));
                    if (!Inside(cell) || !ground(plan[cell.x, cell.y])) return false;
                    char s = map[cell];
                    if (s == 'E' || s == 'K' || s == 'H' || s == 'S') return false;
                }
            if (awayFromPaths)
                foreach (var n in new[] { Vector2.zero, Vector2.left * 2, Vector2.right * 2, Vector2.up * 2, Vector2.down * 2 })
                {
                    var cell = Cell(p + n);
                    if (Inside(cell) && (map[cell] == 'C' || plan[cell.x, cell.y] == K.Dirt || plan[cell.x, cell.y] == K.Bridge
                                         || plan[cell.x, cell.y] == K.BridgeV || plan[cell.x, cell.y] == K.Stone)) return false;
                }
            return true;
        }

        /// <summary>The nearest free spot to a cell (spiral search in half tiles), or null.</summary>
        static Vector2? FindSpot(Vector2 near, float radius, System.Func<K, bool> ground, float maxDistance = 24f, bool awayFromPaths = false)
        {
            for (float k = 0f; k <= maxDistance; k += 0.5f)
                for (float dx = -k; dx <= k + 0.01f; dx += 0.5f)
                    for (float dy = -k; dy <= k + 0.01f; dy += 0.5f)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) < k - 0.01f) continue;
                        var p = near + new Vector2(dx, dy);
                        if (Free(p, radius, ground, awayFromPaths)) return p;
                    }
            return null;
        }

        static Transform Root(string name) => new GameObject(name).transform;

        /// <summary>A sprite standing on the map (pivot at its feet, sorted with the characters).</summary>
        static SpriteRenderer Prop(Transform parent, string name, Sprite sprite, Vector2 position, Vector2? box = null,
            string layer = null, bool flip = false, float reserve = 1f, int order = 0)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.flipX = flip;
            sr.sortingOrder = order;
            sr.spriteSortPoint = SpriteSortPoint.Pivot;
            if (box.HasValue)
            {
                var b = go.AddComponent<BoxCollider2D>();
                b.size = box.Value;
                b.offset = new Vector2(0f, box.Value.y / 2f);
            }
            if (layer != null) go.layer = LayerMask.NameToLayer(layer);
            if (reserve > 0f) occupied.Add((position, reserve));
            return sr;
        }

        static void Glow(Transform parent, Vector3 local, float size, Color color)
        {
            var glow = new GameObject("Glow");
            glow.transform.SetParent(parent, false);
            glow.transform.localPosition = local;
            glow.transform.localScale = Vector3.one * size;
            var sr = glow.AddComponent<SpriteRenderer>();
            sr.sprite = ArtGenerator.Effect("glow");
            sr.color = color;
            sr.sortingOrder = 501;   // above the darkness of the night
            glow.AddComponent<SpriteFlipbook>().Setup(null, 1f, 0.06f);
        }

        static void Brazier(Transform parent, Vector2 position)
        {
            var b = Prop(parent, "Brazier", S("brazier_0"), position, new Vector2(0.6f, 0.4f), reserve: 0.8f);
            b.gameObject.AddComponent<SpriteFlipbook>().Setup(new[] { S("brazier_0"), S("brazier_1") }, 0.2f);
            Glow(b.transform, new Vector3(0f, 0.8f, 0f), 2.6f, new Color(1f, 0.55f, 0.2f, 0.4f));
        }

        static void Torch(Transform parent, Vector2 position)
        {
            var t = Prop(parent, "Torch", S("torch_0"), position, new Vector2(0.3f, 0.25f), reserve: 0.6f);
            t.gameObject.AddComponent<SpriteFlipbook>().Setup(new[] { S("torch_0"), S("torch_1") }, 0.18f);
            Glow(t.transform, new Vector3(0f, 1.6f, 0f), 2.2f, new Color(1f, 0.6f, 0.25f, 0.38f));
        }

        static void Marker(GameObject go, MapMarker.Kind kind, string label)
        {
            var m = go.GetComponent<MapMarker>();
            if (m == null) m = go.AddComponent<MapMarker>();
            m.Set(kind, label);
        }

        // ---------------- The pyramid ----------------

        /// <summary>
        /// The pyramid picture on the plaza, colliders that leave the staircase and the summit walkable, and around it
        /// braziers, torches, serpent columns, a chacmool, a tzompantli, banners and offerings. On the summit, the door of the
        /// shrine (carved glyphs you can read) and an offering chest.
        /// </summary>
        static void BuildPyramid()
        {
            var root = Root("V09_Pyramid");
            // The site in world units: x 144..188, y 98..126. The picture (40 x 26 tiles) sits on its centre line.
            Vector2 foot = new Vector2(166f, 99f);
            float left = foot.x - PropArt.PyramidW / 32f;     // 146
            var go = new GameObject("Pyramid of the Feathered Serpent");
            go.transform.SetParent(root, false);
            go.transform.position = foot;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = S("v09_pyramid");
            sr.sortingOrder = -6;   // under everyone: the player climbs on it
            Marker(go, MapMarker.Kind.Landmark, "Pyramid of the Feathered Serpent");

            float X(int px) => left + px / 16f;
            float Y(int py) => foot.y + (PropArt.PyramidH - py) / 16f;
            float stairsL = X(PropArt.StairLeft), stairsR = X(PropArt.StairRight);
            float summitL = X(PropArt.SummitLeft), summitR = X(PropArt.SummitRight);
            float summitFront = Y(PropArt.SummitFront), shrineFront = Y(PropArt.ShrineFront);
            float right = X(PropArt.PyramidW), top = Y(0);
            void Block(float x0, float y0, float x1, float y1)
            {
                var b = go.AddComponent<BoxCollider2D>();
                b.offset = new Vector2((x0 + x1) / 2f - foot.x, (y0 + y1) / 2f - foot.y);
                b.size = new Vector2(x1 - x0, y1 - y0);
            }
            Block(left, foot.y, stairsL, summitFront);                 // west of the stairs
            Block(stairsR, foot.y, right, summitFront);                // east of the stairs
            Block(left, summitFront, summitL, shrineFront);            // beside the summit
            Block(summitR, summitFront, right, shrineFront);
            Block(left, shrineFront, right, top);                      // the shrine and the back of the pyramid
            Block(stairsL - 1.3f, foot.y, stairsL + 0.6f, foot.y + 1.4f);    // the serpent heads at the foot of the stairs
            Block(stairsR - 0.6f, foot.y, stairsR + 1.3f, foot.y + 1.4f);
            occupied.Add((new Vector2(166f, 112f), 24f));

            var decor = Root("V09_PyramidDecor");
            // At the foot of the staircase.
            Brazier(decor, new Vector2(stairsL - 2.2f, 97.2f));
            Brazier(decor, new Vector2(stairsR + 2.2f, 97.2f));
            Prop(decor, "SerpentColumn", S("temple_column"), new Vector2(157f, 96.6f), new Vector2(1f, 0.5f));
            Prop(decor, "SerpentColumn", S("temple_column"), new Vector2(175f, 96.6f), new Vector2(1f, 0.5f));
            Prop(decor, "Chacmool", S("chacmool"), new Vector2(152.5f, 97f), new Vector2(1.8f, 0.5f));
            Prop(decor, "Offering", S("offering"), new Vector2(154.6f, 96.4f));
            Prop(decor, "Offering", S("offering"), new Vector2(178f, 96.5f));
            Prop(decor, "Tzompantli", S("tzompantli"), new Vector2(181f, 96.8f), new Vector2(2.4f, 0.5f));
            foreach (float x in new[] { 147f, 151f, 181f, 185f }) Torch(decor, new Vector2(x, 98.3f));
            // Banners and serpent heads on the corners of the plaza.
            Prop(decor, "Banner", S("temple_banner"), new Vector2(143f, 100f), new Vector2(0.5f, 0.3f));
            Prop(decor, "Banner", S("temple_banner"), new Vector2(189f, 100f), new Vector2(0.5f, 0.3f));
            Prop(decor, "SerpentHead", S("serpent_head"), new Vector2(143f, 124.5f), new Vector2(1.2f, 0.5f));
            Prop(decor, "SerpentHead", S("serpent_head"), new Vector2(189f, 124.5f), new Vector2(1.2f, 0.5f), flip: true);
            Prop(decor, "Stela", S("stela_cross"), new Vector2(143f, 112f), new Vector2(0.7f, 0.4f));
            Prop(decor, "Stela", S("stela_cross"), new Vector2(189f, 112f), new Vector2(0.7f, 0.4f));

            // The summit: braziers on both sides of the shrine door, the readable glyphs and an offering chest.
            Brazier(decor, new Vector2(161.5f, shrineFront - 0.6f));
            Brazier(decor, new Vector2(170.5f, shrineFront - 0.6f));
            // The shrine door (the serpent's mouth) is the way into the pyramid: BuildPyramidInterior puts a door there.
            pyramidShrineDoor = new Vector2(166f, shrineFront - 0.3f);
            pyramidSummitArrival = new Vector2(166f, summitFront + 1.1f);
            var glyphs = new GameObject("ShrineGlyphs");
            glyphs.transform.SetParent(decor, false);
            glyphs.transform.position = new Vector2(163.4f, shrineFront - 0.45f);   // beside the door, not in front of it
            var trigger = glyphs.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1.6f, 1.1f);
            var spot = new SerializedObject(glyphs.AddComponent<InteractSpot>());
            spot.FindProperty("prompt").stringValue = "{Interact}  Read the glyphs of the shrine";
            spot.FindProperty("speaker").stringValue = "Glyphs of the shrine";
            SetStrings(spot.FindProperty("lines"),
                "Here the Feathered Serpent came down to Mictlán to steal the bones of the dead and make the people of this age.",
                "The serpent's mouth leads down into the House of the Wind, where Quetzalcóatl sleeps behind a gate of obsidian.",
                "Only the obsidian key opens it. It is born when every servant of the gods has fallen.",
                "> So I go in through the serpent's mouth... once the last of them is gone.");
            spot.ApplyModifiedPropertiesWithoutUndo();

            var chest = new GameObject("SummitOffering");
            chest.transform.SetParent(decor, false);
            chest.transform.position = new Vector2(174f, summitFront + 0.6f);
            var chestSprite = chest.AddComponent<SpriteRenderer>();
            chestSprite.sprite = S("chest");
            chestSprite.spriteSortPoint = SpriteSortPoint.Pivot;
            var box = chest.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.4f, 1.2f);
            var loot = new SerializedObject(chest.AddComponent<LootChest>());
            loot.FindProperty("sprite").objectReferenceValue = chestSprite;
            loot.FindProperty("gold").intValue = 12;
            loot.FindProperty("potions").intValue = 1;
            loot.FindProperty("heal").floatValue = 30f;
            loot.FindProperty("title").stringValue = "THE OFFERING OF THE SUMMIT";
            loot.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetStrings(SerializedProperty array, params string[] values)
        {
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).stringValue = values[i];
        }

        // ---------------- Natural obstacles instead of the loose temple blocks ----------------

        /// <summary>
        /// Where a loose block cut a path there is now a pile of fallen logs you jump over (layer LowObstacle);
        /// where it stood in the grass, rocks (the team's rock mixed with the old ones) and bushes.
        /// </summary>
        static void ReplaceBlocks()
        {
            var root = Root("V09_Obstacles");
            bool hinted = false;
            foreach (var cell in removedBlocks)
            {
                Vector2 p = World(cell);
                bool pathEW = IsPath(cell + Vector2Int.left) || IsPath(cell + Vector2Int.right) || IsPath(cell + Vector2Int.left * 2) && IsPath(cell + Vector2Int.right * 2);
                bool pathNS = IsPath(cell + Vector2Int.up) || IsPath(cell + Vector2Int.down);
                if (pathEW || pathNS)
                {
                    bool vertical = pathEW;   // a path running east-west is blocked by a log lying north-south
                    var log = Prop(root, "FallenLog", S(vertical ? "log_vertical" : "log_horizontal"), p + (vertical ? Vector2.down * 1.3f : Vector2.zero),
                        vertical ? new Vector2(0.9f, 2.6f) : new Vector2(2.8f, 0.8f), PlayerJump.LowObstacleLayer, reserve: 1.4f);
                    if (!hinted)
                    {
                        hinted = true;
                        var hint = new GameObject("JumpHint");
                        hint.transform.SetParent(root, false);
                        hint.transform.position = p;
                        var area = hint.AddComponent<BoxCollider2D>();
                        area.isTrigger = true;
                        area.size = new Vector2(8f, 6f);
                        var tip = new SerializedObject(hint.AddComponent<TutorialZone>());
                        tip.FindProperty("id").stringValue = "log_jump";
                        tip.FindProperty("message").stringValue = "Fallen logs and low rocks block the way. JUMP over them with {Jump} (sprint with {Sprint} to jump farther).";
                        tip.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
                else
                {
                    RockCluster(root, p + new Vector2(0f, -0.6f), cell.x * 31 + cell.y);
                    if ((cell.x + cell.y) % 3 == 0) Prop(root, "Bush", S("bush"), p + new Vector2(1.3f, 0.5f), new Vector2(1f, 0.4f), PlayerJump.LowObstacleLayer, reserve: 0.6f);
                }
            }
        }

        static bool IsPath(Vector2Int c) => Inside(c) && (map[c] == 'C' || plan[c.x, c.y] == K.Dirt);

        // ---------------- Landmarks ----------------

        static void PlaceLandmarks()
        {
            var root = Root("V09_Landmarks");
            Landmark(root, "Sacred Ceiba", "ceiba", new Vector2Int(33, 29), new Vector2(1.8f, 0.8f), 3f, g => g == K.Grass);
            Landmark(root, "Skull Rock", "skull_rock", new Vector2Int(104, 44), new Vector2(4.2f, 1.4f), 3f, g => g == K.Sand);
            Landmark(root, "Head of the Fallen God", "fallen_god", new Vector2Int(124, 59), new Vector2(3.6f, 1.2f), 2.5f, g => g == K.Sand);
            Landmark(root, "Statue of Xólotl", "xolo_statue", new Vector2Int(110, 57), new Vector2(1.2f, 0.5f), 1.2f, g => g == K.Sand);

            // Existing places worth finding on the map.
            var house = GameObject.Find("House of the Sword (H)");
            if (house != null) Marker(house, MapMarker.Kind.Landmark, "House of the Sword");
            // (The cave of offerings is no longer part of the map.)
            // The obsidian gate is now inside the pyramid, in its own room: the map shows the pyramid instead, not the gate.
            foreach (var door in Object.FindObjectsByType<KeyDoor>())
            {
                var oldMarker = door.GetComponent<MapMarker>();
                if (door.transform.position.x >= 0f) Marker(door.gameObject, MapMarker.Kind.Door, "Obsidian Gate");
                else if (oldMarker != null) Object.DestroyImmediate(oldMarker);
            }

            // Stelae at the crossroads of the paths, so the forks can be told apart.
            var junctions = new List<Vector2>();
            for (int c = 1; c < W - 1; c++)
                for (int r = 1; r < H - 1; r++)
                {
                    var cell = new Vector2Int(c, r);
                    if (map[cell] != 'C') continue;
                    int around = CsvMap.Neighbours4(cell).Count(n => map[n] == 'C');
                    if (around < 3) continue;
                    var p = World(cell);
                    if (junctions.Any(j => Vector2.Distance(j, p) < 36f)) continue;
                    var spot = FindSpot(p + new Vector2(2.5f, 2.5f), 0.6f, g => g == K.Grass || g == K.Sand, 6f);
                    if (spot == null) continue;
                    junctions.Add(p);
                    Prop(root, "CrossroadsStela", S("stela_cross"), spot.Value, new Vector2(0.7f, 0.4f));
                    Torch(root, spot.Value + new Vector2(1f, -0.2f));
                }
        }

        static void Landmark(Transform root, string label, string sprite, Vector2Int cell, Vector2 box, float radius, System.Func<K, bool> ground)
        {
            var spot = FindSpot(World(cell), radius, ground, 20f, awayFromPaths: true);
            if (spot == null) { Debug.LogWarning("[TD_Game] No room for landmark " + label); return; }
            var sr = Prop(root, label, S(sprite), spot.Value, box, reserve: radius + 1f);
            Marker(sr.gameObject, MapMarker.Kind.Landmark, label);
        }

        // ---------------- Rocks and region details ----------------

        enum Region { Tlaloc, Tlazolteotl, Desert, Other }

        static Region RegionOf(Vector2Int cell)
        {
            if (map.IsCave(cell) || CsvMap.IsBuilding(map[cell])) return Region.Other;
            if (cell.x < 64 && cell.y < 36) return Region.Tlaloc;
            if (cell.x < 66) return Region.Tlazolteotl;
            return Region.Desert;
        }

        /// <summary>How many cells around a cell (up to 2 away) are trees, water, ravines or walls: rocks belong there.</summary>
        static int Edges(Vector2Int cell)
        {
            int n = 0;
            for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                {
                    var k = At(new Vector2Int(cell.x + dx, cell.y + dy));
                    if (k == K.Tree || k == K.Water || k == K.Pit || k == K.TempleWall || k == K.Rock) n++;
                }
            return n;
        }

        /// <summary>
        /// Rocks, the team's trees, mist and the small things that give each region its own face.
        /// Rocks are not sprinkled at random any more: they gather where rocks really are â€” under tree lines, on river banks,
        /// at the lips of ravines and at the foot of walls â€” in small families that mix the team's rock (as drawn) with the old
        /// rocks of the map, each with a shadow and a solid base (you walk around them; the small ones can be jumped).
        /// The team's two trees grow along the edges of the forests; their mist drifts by the water and the witches' valley.
        /// Nothing is placed on paths, bridges, next to enemies or chests, or where it could close a passage.
        /// </summary>
        static void PlaceRocksAndDetails()
        {
            var root = Root("V09_Nature");
            var fogRoot = Root("V09_Mist");
            var rockSpots = new List<Vector2>();
            var treeSpots = new List<Vector2>();
            int rocks = 0, details = 0, trees = 0, fog = 0;
            System.Func<K, bool> ground = g => g == K.Grass || g == K.Sand || g == K.PlazaEdge;
            for (int c = 1; c < W - 1; c++)
                for (int r = 1; r < H - 1; r++)
                {
                    var cell = new Vector2Int(c, r);
                    var region = RegionOf(cell);
                    if (region == Region.Other || !Open(plan[c, r]) || plan[c, r] == K.Dirt) continue;
                    int edges = Edges(cell);
                    Vector2 p = World(cell) + new Vector2((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f);

                    // Team trees along the edges of the forests (grass next to trees).
                    bool treeLine = region != Region.Desert && edges >= 3 && CsvMap.Neighbours4(cell).Any(n => At(n) == K.Tree);
                    if (treeLine && random.NextDouble() < 0.3 && !treeSpots.Any(t => Vector2.Distance(t, p) < 4.5f) && Free(p, 1.3f, ground, awayFromPaths: true))
                    {
                        bool glow = region == Region.Tlazolteotl ? random.NextDouble() < 0.45 : random.NextDouble() < 0.15;
                        TeamTree(root, glow ? team.treeGlow : team.treeMist, p, random.Next(2) == 0);
                        treeSpots.Add(p);
                        trees++;
                        continue;
                    }

                    // Rock families where rocks belong; almost none in the middle of open ground.
                    double rockChance = edges >= 2 ? 0.1 : 0.004;
                    if (random.NextDouble() < rockChance && !rockSpots.Any(t => Vector2.Distance(t, p) < 7f) && Free(p, 1.4f, ground, awayFromPaths: true))
                    {
                        RockCluster(root, p, random.Next());
                        rockSpots.Add(p);
                        rocks++;
                        continue;
                    }

                    // Mist by the water and in the witches' valley.
                    bool nearWater = Enumerable.Range(-2, 5).Any(dx => Enumerable.Range(-2, 5).Any(dy => At(new Vector2Int(c + dx, r + dy)) == K.Water));
                    double mistChance = nearWater ? 0.025 : region == Region.Tlazolteotl ? 0.008 : 0.0;
                    if (team.fog != null && random.NextDouble() < mistChance)
                    {
                        Mist(fogRoot, p);
                        fog++;
                        continue;
                    }

                    if (random.NextDouble() < 0.045 && Free(p, 0.8f, ground, awayFromPaths: true))
                    {
                        Detail(root, region, p);
                        details++;
                    }
                }
            Debug.Log($"[TD_Game] v0.9 nature: {rocks} rock families, {trees} team trees, {fog} mist patches, {details} details.");
        }

        /// <summary>A family of 2-4 rocks: the team's rock (as drawn) and the old rocks of the map, with pebbles.</summary>
        static void RockCluster(Transform root, Vector2 p, int seed)
        {
            var rng = new System.Random(seed);
            Sprite oldRock = Tile("Rock"), oldMossy = Tile("Rock_Mossy");
            bool teamFirst = rng.NextDouble() < 0.6;
            Rock(root, teamFirst ? team.rock : (rng.Next(2) == 0 ? oldRock : oldMossy), p, rng.Next(2) == 0, solid: true);
            int extra = 1 + rng.Next(3);
            for (int i = 0; i < extra; i++)
            {
                var offset = new Vector2((float)(rng.NextDouble() * 2.6 - 1.3), (float)(rng.NextDouble() * 1.2 - 0.7));
                if (offset.magnitude < 0.8f) offset = offset.normalized * 0.9f + Vector2.right * 0.2f;
                int pick = rng.Next(4);
                Sprite s = pick == 0 ? team.rock : pick == 1 ? oldRock : pick == 2 ? oldMossy : team.pebble;
                Rock(root, s, p + offset, rng.Next(2) == 0, solid: pick != 3);
            }
            occupied.Add((p, 2f));
        }

        /// <summary>One rock with a soft shadow; solid rocks block, pebbles can be jumped over (LowObstacle).</summary>
        static void Rock(Transform root, Sprite sprite, Vector2 p, bool flip, bool solid)
        {
            if (sprite == null) return;
            var sr = Prop(root, solid ? "Rock" : "Pebble", sprite, p, null, solid ? null : PlayerJump.LowObstacleLayer, flip, reserve: 0f);
            var b = sprite.bounds;
            var box = sr.gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(Mathf.Max(0.35f, b.size.x * 0.8f), Mathf.Clamp(b.size.y * 0.4f, 0.25f, 0.6f));
            box.offset = new Vector2(b.center.x * (flip ? -1f : 1f), b.min.y + box.size.y / 2f);
            Shadow(sr.transform, b);
        }

        static void Shadow(Transform parent, Bounds b)
        {
            if (team.shadow == null) return;
            var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>();
            shadow.transform.SetParent(parent, false);
            shadow.transform.localPosition = new Vector3(b.center.x, b.min.y + 0.05f, 0f);
            shadow.transform.localScale = new Vector3(Mathf.Max(0.6f, b.size.x / 1.4f), Mathf.Max(0.6f, b.size.x / 2.2f), 1f);
            shadow.sprite = team.shadow;
            shadow.sortingOrder = -4;
        }

        static void TeamTree(Transform root, Sprite sprite, Vector2 p, bool flip)
        {
            if (sprite == null) return;
            var sr = Prop(root, "TeamTree", sprite, p, new Vector2(0.7f, 0.4f), flip: flip, reserve: 1.8f);
            Shadow(sr.transform, new Bounds(Vector3.zero, new Vector3(2.4f, 0.6f, 0f)));
            if (sprite == team.treeGlow) Glow(sr.transform, new Vector3(0.3f, 2.2f, 0f), 2.2f, new Color(0.3f, 1f, 0.5f, 0.28f));
        }

        /// <summary>The team's drifting mist (12 frames), low over the ground.</summary>
        static void Mist(Transform root, Vector2 p)
        {
            var sr = new GameObject("Mist").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(root, false);
            sr.transform.position = p;
            sr.transform.localScale = Vector3.one * (1.2f + (float)random.NextDouble() * 0.8f);
            sr.sprite = team.fog[0];
            sr.flipX = random.Next(2) == 0;
            sr.sortingOrder = -4;   // over the ground, under people and props
            sr.gameObject.AddComponent<SpriteFlipbook>().Setup(team.fog, 0.14f + (float)random.NextDouble() * 0.06f);
        }

        static void Detail(Transform root, Region region, Vector2 p)
        {
            int roll = random.Next(10);
            switch (region)
            {
                case Region.Tlaloc:
                    if (roll < 3) Prop(root, "Mushrooms", S("mushrooms"), p, reserve: 0.5f);
                    else if (roll < 6) Prop(root, "Fern", S("fern"), p, reserve: 0.5f, flip: roll % 2 == 0);
                    else Prop(root, "Bush", S("bush"), p, new Vector2(1f, 0.4f), PlayerJump.LowObstacleLayer, reserve: 0.8f);
                    break;
                case Region.Tlazolteotl:
                    if (roll < 3) Prop(root, "DeadTree", S("dead_tree"), p, new Vector2(0.6f, 0.4f), reserve: 1.2f, flip: roll % 2 == 0);
                    else if (roll < 5) Prop(root, "Skulls", S("skull_pile"), p, reserve: 0.6f);
                    else if (roll < 7) Prop(root, "Crow", S("crow"), p, reserve: 0.4f, flip: roll % 2 == 0);
                    else Prop(root, "Fern", S("fern"), p, reserve: 0.5f);
                    break;
                default:
                    if (roll < 3) Prop(root, "Cactus", S("cactus"), p, new Vector2(0.5f, 0.3f), reserve: 0.7f, flip: roll % 2 == 0);
                    else if (roll < 5) Prop(root, "Agave", S("agave"), p, reserve: 0.6f);
                    else if (roll < 7) Prop(root, "OxSkull", S("ox_skull"), p, reserve: 0.5f, flip: roll % 2 == 0);
                    else Prop(root, "Pottery", S("pottery"), p, reserve: 0.5f);
                    break;
            }
        }
    }
}
