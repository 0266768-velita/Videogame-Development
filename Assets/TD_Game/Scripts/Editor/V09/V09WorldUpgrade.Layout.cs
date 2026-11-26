using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// Layout of the map after the team's review (26 nov):
    ///   - the map ends at the green line: CSV columns 0-124 (the cave and the east desert are X in the CSV);
    ///   - the inside of the pyramid (CSV columns 102-143, rows 0-33) leaves the map and becomes a room of its own,
    ///     like the house of the witches: you enter through the serpent's mouth on the summit of the pyramid, arrive in a
    ///     small antechamber and open the obsidian gate there (KeyDoor) to reach the labyrinth and Quetzalcóatl;
    ///     its east entrance is walled up; where the inside was, the map shows the edge of the forest;
    ///   - the house of the witches takes the place of the house of the sword (H in the CSV);
    ///   - the borders (trees T and rocks R) are drawn in the CSV.
    /// Everything here can run again: what was already moved is not moved twice.
    /// </summary>
    public static partial class V09WorldUpgrade
    {
        /// <summary>The inside of the pyramid in CSV cells (columns 102-143, rows 0-33).</summary>
        static readonly RectInt InteriorCells = new RectInt(102, 0, 42, 34);
        /// <summary>The south entrance of the labyrinth, where the obsidian gate stays.</summary>
        static readonly RectInt EntranceCells = new RectInt(122, 32, 2, 2);
        /// <summary>The antechamber in front of the obsidian gate (floor) and its walls.</summary>
        static readonly RectInt AntechamberCells = new RectInt(120, 34, 6, 3);
        static readonly RectInt AntechamberRing = new RectInt(119, 33, 8, 5);
        /// <summary>Where the inside of the pyramid is moved (tiles): far west of the map, away from the witches' room.</summary>
        static readonly Vector2Int InteriorOffset = new Vector2Int(-464, -64);

        static bool[,] interiorFloor;        // [column, row] of the CSV: true = floor inside the pyramid
        static Vector2 pyramidShrineDoor, pyramidSummitArrival;

        static bool InInteriorRegion(Vector2Int c) => InteriorCells.Contains(c) || AntechamberRing.Contains(c);

        /// <summary>World rectangle of the old inside of the pyramid (and its antechamber), before moving it.</summary>
        static Rect InteriorWorld()
        {
            var a = map.World(new Vector2Int(InteriorCells.xMin, InteriorCells.yMin));
            var b = map.World(new Vector2Int(InteriorCells.xMax - 1, AntechamberRing.yMax - 1));
            float h = CsvMap.TilesPerCell / 2f;
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x) - h, Mathf.Min(a.y, b.y) - h, Mathf.Max(a.x, b.x) + h, Mathf.Max(a.y, b.y) + h);
        }

        static Vector2 EntranceWorld() => map.Bounds(new List<Vector2Int> { new Vector2Int(EntranceCells.xMin, EntranceCells.yMin),
                                                                             new Vector2Int(EntranceCells.xMax - 1, EntranceCells.yMax - 1) }).center;

        // ---------------- 1. The plan: keep the inside, cut the map ----------------

        /// <summary>Remembers the floor of the inside of the pyramid, then turns that part of the map into forest edge
        /// (columns up to 124) or nothing (beyond the map).</summary>
        static void CaptureInterior()
        {
            var outdoors = map.Outdoors();
            interiorFloor = new bool[W, H];
            for (int c = 0; c < W; c++)
                for (int r = 0; r < H; r++)
                {
                    var cell = new Vector2Int(c, r);
                    if (!InInteriorRegion(cell)) continue;
                    bool labyrinth = InteriorCells.Contains(cell) && !outdoors[c, r] && plan[c, r] == K.TempleFloor;
                    interiorFloor[c, r] = labyrinth || EntranceCells.Contains(cell) || AntechamberCells.Contains(cell);
                }

            for (int c = 0; c < W; c++)
                for (int r = 0; r < H; r++)
                {
                    if (c >= CsvMap.PlayableColumns) plan[c, r] = K.Void;
                    else if (InteriorCells.Contains(new Vector2Int(c, r))) plan[c, r] = K.Tree;
                }
        }

        // ---------------- 2. Objects made by Build CSV Map ----------------

        static void ApplyLayoutToScene()
        {
            // The house of the sword gives its place to the house of the witches.
            var sword = GameObject.Find("House of the Sword (H)");
            if (sword != null) Object.DestroyImmediate(sword);

            var region = InteriorWorld();
            var entrance = EntranceWorld();
            var offset = (Vector3)(Vector2)InteriorOffset;
            bool Near(Transform t) => Vector2.Distance(t.position, new Vector2(entrance.x, entrance.y - CsvMap.TilesPerCell)) < 3f;

            // Only the obsidian gate of the south entrance stays (the east one is walled up).
            foreach (var door in Object.FindObjectsByType<KeyDoor>(FindObjectsInactive.Include))
                if (region.Contains(door.transform.position))
                {
                    if (Near(door.transform)) door.transform.position += offset;
                    else Object.DestroyImmediate(door.gameObject);
                }

            var arena = Object.FindAnyObjectByType<BossArena>(FindObjectsInactive.Include);
            if (arena != null)
            {
                var so = new SerializedObject(arena);
                var gates = so.FindProperty("gates");
                var kept = new List<GameObject>();
                for (int i = 0; i < gates.arraySize; i++)
                {
                    var gate = gates.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
                    if (gate == null) continue;
                    if (!region.Contains(gate.transform.position)) { kept.Add(gate); continue; }   // already moved
                    if (Near(gate.transform)) { gate.transform.position += offset; kept.Add(gate); }
                    else Object.DestroyImmediate(gate);
                }
                gates.arraySize = kept.Count;
                for (int i = 0; i < kept.Count; i++) gates.GetArrayElementAtIndex(i).objectReferenceValue = kept[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // The boss, its arena, the braziers and the idol of the temple move with the inside.
            foreach (var rootName in new[] { "BossZone", "TempleDecor_CSV" })
            {
                var root = GameObject.Find(rootName);
                if (root == null) continue;
                foreach (Transform child in root.transform)
                    if (region.Contains(child.position)) child.position += offset;
            }

            // Region names: the temple goes inside with the pyramid; the Valley of Serpents is the desert east of the
            // valley, up to the edge of the map (it used to reach the old east part).
            var territories = GameObject.Find("Territories");
            if (territories != null)
                foreach (var zone in territories.GetComponentsInChildren<ZoneArea>(true))
                {
                    string zoneName = zone.name.ToUpperInvariant();
                    if (zoneName.Contains("TEMPLE"))
                    {
                        if (region.Contains(zone.transform.position)) zone.transform.position += offset;
                    }
                    else if (zoneName.Contains("SERP"))
                    {
                        var desert = map.Bounds(new List<Vector2Int> { new Vector2Int(64, 34), new Vector2Int(CsvMap.PlayableColumns - 1, H - 1) });
                        zone.transform.position = desert.center;
                        var box = zone.GetComponent<BoxCollider2D>();
                        box.offset = Vector2.zero;
                        box.size = desert.size - Vector2.one * 2f;
                    }
                }

            // Chests that were in the cave or beyond the map.
            var chests = GameObject.Find("Chests");
            if (chests != null)
                for (int i = chests.transform.childCount - 1; i >= 0; i--)
                {
                    var chest = chests.transform.GetChild(i);
                    if (OutsideMap(chest.position)) Object.DestroyImmediate(chest.gameObject);
                }

            // Invisible walls around the playable map (250 x 140 tiles).
            var border = GameObject.Find("MapBorder");
            if (border != null)
            {
                foreach (var box in border.GetComponents<BoxCollider2D>()) Object.DestroyImmediate(box);
                float w = CsvMap.PlayableColumns * CsvMap.TilesPerCell, h = H * CsvMap.TilesPerCell;
                foreach (var (o, s) in new[]
                         {
                             (new Vector2(-1f, h / 2f), new Vector2(2f, h + 4f)), (new Vector2(w + 1f, h / 2f), new Vector2(2f, h + 4f)),
                             (new Vector2(w / 2f, -1f), new Vector2(w + 4f, 2f)), (new Vector2(w / 2f, h + 1f), new Vector2(w + 4f, 2f)),
                         })
                {
                    var box = border.AddComponent<BoxCollider2D>();
                    box.offset = o;
                    box.size = s;
                }
            }
        }

        /// <summary>True for a point of the main map that is no longer walkable land: beyond the map or where the inside
        /// of the pyramid was. Points of the rooms (far from the map) are never "outside".</summary>
        static bool OutsideMap(Vector2 p)
        {
            if (p.x < 0f || p.y < 0f || p.x >= W * CsvMap.TilesPerCell || p.y >= H * CsvMap.TilesPerCell) return false;
            var cell = Cell(p);
            return plan[cell.x, cell.y] == K.Void || InteriorCells.Contains(cell);
        }

        // ---------------- 3. The inside of the pyramid, as a room ----------------

        static void BuildPyramidInterior()
        {
            var grid = new GameObject("V09_PyramidInterior", typeof(Grid)).transform;
            var floor = InteriorTilemap(grid, "PyramidFloor", -10, false);
            var walls = InteriorTilemap(grid, "PyramidWalls", 0, true);
            for (int c = 0; c < W; c++)
                for (int r = 0; r < H; r++)
                {
                    var cell = new Vector2Int(c, r);
                    if (!InInteriorRegion(cell)) continue;
                    int x0 = c * CsvMap.TilesPerCell + InteriorOffset.x, y0 = (H - 1 - r) * CsvMap.TilesPerCell + InteriorOffset.y;
                    for (int i = 0; i < CsvMap.TilesPerCell; i++)
                        for (int j = 0; j < CsvMap.TilesPerCell; j++)
                        {
                            var t = new Vector3Int(x0 + i, y0 + j, 0);
                            floor.SetTile(t, art.templeFloor);
                            if (!interiorFloor[c, r]) walls.SetTile(t, art.templeWall);
                        }
                }
            floor.CompressBounds(); walls.CompressBounds();

            // The antechamber: the obsidian gate to the north, the way out to the south, two torches.
            var offset = (Vector2)InteriorOffset;
            var room = map.Bounds(new List<Vector2Int> { new Vector2Int(AntechamberCells.xMin, AntechamberCells.yMin),
                                                        new Vector2Int(AntechamberCells.xMax - 1, AntechamberCells.yMax - 1) });
            Vector2 arrival = new Vector2(room.center.x, room.yMin + 3.6f) + offset;
            Vector2 exit = new Vector2(room.center.x, room.yMin + 0.6f) + offset;
            Torch(grid, new Vector2(room.xMin + 1.2f, room.yMax - 1.2f) + offset);
            Torch(grid, new Vector2(room.xMax - 1.2f, room.yMax - 1.2f) + offset);

            // No map position: the world map shows the inside of the pyramid at this door (the summit), so the temple's
            // name and Quetzalcóatl do not cover the name of the pyramid at its foot.
            Door(grid, "Door (pyramid, outside)", pyramidShrineDoor, new Vector2(1.8f, 0.6f), arrival, floor, true, null,
                "pyramid", "THE HOUSE OF THE WIND\nThe obsidian gate waits in the dark...");
            Door(grid, "Door (pyramid, inside)", exit, new Vector2(3f, 0.8f), pyramidSummitArrival, GroundTilemap(), false, null, "pyramid");

            // Braziers of the old east entrance now stand inside a wall: they go.
            var decor = GameObject.Find("TempleDecor_CSV");
            if (decor != null)
                for (int i = decor.transform.childCount - 1; i >= 0; i--)
                {
                    var t = decor.transform.GetChild(i);
                    if (t.position.x < 0f && walls.HasTile(walls.WorldToCell(t.position))) Object.DestroyImmediate(t.gameObject);
                }
        }

        // ---------------- 4. Nothing outside the map ----------------

        /// <summary>Removes anything v0.9 placed on the land that is no longer part of the map (rocks, details, enemies...).</summary>
        static void RemoveOutsideMap()
        {
            int removed = 0;
            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()
                .Where(g => (g.name.StartsWith("V09_") && g.GetComponent<Grid>() == null && g.name != "V09_WitchHouseInside") || g.name == "Enemies" || g.name == "Chests");
            foreach (var root in roots.ToList())
                for (int i = root.transform.childCount - 1; i >= 0; i--)
                {
                    var child = root.transform.GetChild(i);
                    if (child.GetComponent<HouseDoor>() != null || !OutsideMap(child.position)) continue;
                    Object.DestroyImmediate(child.gameObject);
                    removed++;
                }
            Debug.Log($"[TD_Game] Map layout: {removed} objects removed outside the map; inside of the pyramid moved by {InteriorOffset}.");
        }

        /// <summary>Centre of the H of the CSV (the straw house of the valley). Falls back to the old spot of the witches.</summary>
        static Vector2 WitchHouseSpot()
        {
            var cells = map.Groups('H').SelectMany(g => g).ToList();
            return cells.Count > 0 ? map.Bounds(cells).center : World(new Vector2Int(3, 56));
        }
    }
}
