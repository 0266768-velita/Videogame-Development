using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// The design map of the team (Settings/Maps/mapa_juego_150x70.csv): one character per cell, row 1 = north.
    /// Legend: S start, C dirt path, . grass, T tree, W water, B bridge, H house, P pyramid (outside),
    /// M wall / I corridor (inside the pyramid), D desert sand, V ravine, E enemy, K chest / key object,
    /// R rock (a border you cannot pass), X out of the map (nothing is painted).
    /// One CSV cell = 2 x 2 tiles of the Tilemap (1 tile = 1 unit), so the world is 300 x 140 tiles.
    /// Only reads and answers questions: CsvMapPainter paints it and CsvMapBuilder places things on it.
    /// </summary>
    public class CsvMap
    {
        public const int TilesPerCell = 2;

        /// <summary>Columns of the playable map (the rest of the CSV is X or the inside of the pyramid, moved away by v0.9).</summary>
        public const int PlayableColumns = 125;

        public readonly int Width, Height;   // in cells
        readonly char[,] cells;              // [column, row], row 0 = the first line of the CSV (north)

        CsvMap(char[,] cells)
        {
            this.cells = cells;
            Width = cells.GetLength(0);
            Height = cells.GetLength(1);
        }

        public static CsvMap Load(string assetPath)
        {
            var text = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            if (text == null) { Debug.LogError("[TD_Game] Missing map CSV " + assetPath); return null; }
            var rows = new List<string[]>();
            foreach (var line in text.text.Split('\n'))
            {
                string clean = line.Trim();
                if (clean.Length > 0) rows.Add(clean.Split(','));
            }
            int width = rows[0].Length;
            var grid = new char[width, rows.Count];
            for (int r = 0; r < rows.Count; r++)
                for (int c = 0; c < width; c++)
                    grid[c, r] = c < rows[r].Length && rows[r][c].Length > 0 ? rows[r][c].Trim()[0] : '.';
            return new CsvMap(grid);
        }

        public bool Inside(int c, int r) => c >= 0 && r >= 0 && c < Width && r < Height;
        public char this[int c, int r] => Inside(c, r) ? cells[c, r] : '\0';
        public char this[Vector2Int cell] => this[cell.x, cell.y];

        /// <summary>Centre of a cell in world units (the 2 x 2 tiles of the cell).</summary>
        public Vector2 World(Vector2Int cell) =>
            new Vector2(cell.x * TilesPerCell + TilesPerCell * 0.5f, (Height - 1 - cell.y) * TilesPerCell + TilesPerCell * 0.5f);

        /// <summary>The cell that contains a tile of the Tilemap.</summary>
        public Vector2Int CellOfTile(int x, int y) => new Vector2Int(x / TilesPerCell, Height - 1 - y / TilesPerCell);

        public Vector2 Center(List<Vector2Int> group)
        {
            Vector2 sum = Vector2.zero;
            foreach (var cell in group) sum += World(cell);
            return sum / group.Count;
        }

        /// <summary>World rectangle that covers a group of cells.</summary>
        public Rect Bounds(List<Vector2Int> group)
        {
            Vector2 min = World(group[0]), max = min;
            foreach (var cell in group) { min = Vector2.Min(min, World(cell)); max = Vector2.Max(max, World(cell)); }
            float half = TilesPerCell * 0.5f;
            return Rect.MinMaxRect(min.x - half, min.y - half, max.x + half, max.y + half);
        }

        public Vector2Int Find(char symbol)
        {
            for (int r = 0; r < Height; r++)
                for (int c = 0; c < Width; c++)
                    if (cells[c, r] == symbol) return new Vector2Int(c, r);
            return new Vector2Int(-1, -1);
        }

        /// <summary>Cells with this symbol that touch each other (also diagonally) form one group.</summary>
        public List<List<Vector2Int>> Groups(char symbol) => Groups(cell => this[cell] == symbol);

        public List<List<Vector2Int>> Groups(System.Func<Vector2Int, bool> belongs)
        {
            var seen = new bool[Width, Height];
            var groups = new List<List<Vector2Int>>();
            for (int r = 0; r < Height; r++)
                for (int c = 0; c < Width; c++)
                {
                    if (seen[c, r] || !belongs(new Vector2Int(c, r))) continue;
                    var group = new List<Vector2Int>();
                    var open = new Stack<Vector2Int>();
                    open.Push(new Vector2Int(c, r));
                    seen[c, r] = true;
                    while (open.Count > 0)
                    {
                        var cell = open.Pop();
                        group.Add(cell);
                        for (int dx = -1; dx <= 1; dx++)
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                var next = new Vector2Int(cell.x + dx, cell.y + dy);
                                if (!Inside(next.x, next.y) || seen[next.x, next.y] || !belongs(next)) continue;
                                seen[next.x, next.y] = true;
                                open.Push(next);
                            }
                    }
                    groups.Add(group);
                }
            return groups;
        }

        /// <summary>
        /// True for every cell that can be reached from the edge of the map without going through
        /// the walls or corridors of a building (M / I). The rest (gaps inside the pyramid) is indoors.
        /// </summary>
        public bool[,] Outdoors()
        {
            var outdoors = new bool[Width, Height];
            var open = new Queue<Vector2Int>();
            for (int c = 0; c < Width; c++) { Seed(c, 0); Seed(c, Height - 1); }
            for (int r = 0; r < Height; r++) { Seed(0, r); Seed(Width - 1, r); }
            while (open.Count > 0)
            {
                var cell = open.Dequeue();
                foreach (var next in Neighbours4(cell)) Seed(next.x, next.y);
            }
            return outdoors;

            void Seed(int c, int r)
            {
                if (!Inside(c, r) || outdoors[c, r] || IsBuilding(cells[c, r])) return;
                outdoors[c, r] = true;
                open.Enqueue(new Vector2Int(c, r));
            }
        }

        public static bool IsBuilding(char symbol) => symbol == 'M' || symbol == 'I';

        /// <summary>The south-east cave: its M are rock and its floor is earth (the rest of M / I is the pyramid).</summary>
        public bool IsCave(Vector2Int cell) => cell.y >= 35 && cell.x >= 125;

        public static IEnumerable<Vector2Int> Neighbours4(Vector2Int cell)
        {
            yield return cell + Vector2Int.up;
            yield return cell + Vector2Int.right;
            yield return cell + Vector2Int.down;
            yield return cell + Vector2Int.left;
        }

        /// <summary>Steps (in cells) from a cell to every cell reachable through walkable cells. -1 = unreachable.</summary>
        public int[,] Distances(Vector2Int from, System.Func<Vector2Int, bool> walkable)
        {
            var dist = new int[Width, Height];
            for (int c = 0; c < Width; c++) for (int r = 0; r < Height; r++) dist[c, r] = -1;
            var open = new Queue<Vector2Int>();
            dist[from.x, from.y] = 0;
            open.Enqueue(from);
            while (open.Count > 0)
            {
                var cell = open.Dequeue();
                foreach (var next in Neighbours4(cell))
                {
                    if (!Inside(next.x, next.y) || dist[next.x, next.y] >= 0 || !walkable(next)) continue;
                    dist[next.x, next.y] = dist[cell.x, cell.y] + 1;
                    open.Enqueue(next);
                }
            }
            return dist;
        }
    }
}
