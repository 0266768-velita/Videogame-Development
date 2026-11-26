using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// v0.9 ground tiles (16 px per tile, like the rest of the map):
    ///   - grass with zigzag blades, in the same dark green as Vely's grass so both mix in soft patches;
    ///   - desert sand with continuous wind ripples (they join across tiles), flat sand and sand with grains;
    ///   - ravines with depth: a lit wall of layered earth under the far rim, darkness going down, a lip on the near rim;
    ///   - stepping stones over the river (you jump from one to the next) and a stone plaza for the pyramid.
    /// </summary>
    public static partial class PropArt
    {
        public class V09Tiles
        {
            public TileBase[] grass, sand, pits;
            public TileBase sandFlat, steppingStone, plaza, plazaEdge, interiorFloor, interiorWall, cempasuchil;
        }

        public static V09Tiles V09Terrain()
        {
            var set = new V09Tiles
            {
                grass = new[] { ZigGrass(1), ZigGrass(2), ZigGrass(3), ZigGrass(4) },
                sand = new[] { WavySand(1), WavySand(2), WavySand(3) },
                sandFlat = FlatSand(),
                pits = new TileBase[16],
                steppingStone = SteppingStone(),
                plaza = Plaza(false),
                plazaEdge = Plaza(true),
                interiorFloor = InteriorFloor(),
                interiorWall = InteriorWall(),
                cempasuchil = Cempasuchil(),
            };
            for (int mask = 0; mask < 16; mask++) set.pits[mask] = DeepPit(mask);
            AssetDatabase.SaveAssets();
            return set;
        }

        static TileBase SaveTile(string name, PixelCanvas canvas, Vector2 pivot, bool solid)
        {
            var sprite = canvas.Save($"{TerrainArt.Folder}/{name}.png", 16, pivot);
            string path = $"{TerrainArt.Folder}/{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
            tile.sprite = sprite;
            tile.colliderType = solid ? Tile.ColliderType.Grid : Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        static readonly Vector2 Mid = new Vector2(0.5f, 0.5f);

        // ---------------- Grass ----------------

        /// <summary>Dark grass with little zigzag blades ("^v^"), in rows that shift from tile to tile.</summary>
        static TileBase ZigGrass(int seed)
        {
            var baseColor = C(24, 50, 14);
            var c = new PixelCanvas(16, 16, baseColor);
            c.Speckle(seed * 31, C(19, 42, 10), C(30, 60, 18), 0.18f);
            var dark = C(14, 32, 7);
            var light = C(46, 84, 26);
            var tip = C(66, 108, 36);
            int rows = seed == 4 ? 2 : 3;
            for (int r = 0; r < rows; r++)
            {
                int y0 = 3 + r * (16 / rows) + (seed * 3 + r * 5) % 3;
                int x0 = (seed * 7 + r * 5) % 16;
                int length = 5 + (seed + r) % 4;
                for (int i = 0; i < length; i++)
                {
                    int x = (x0 + i) % 16;
                    int y = y0 + (i % 2 == 0 ? 0 : -1);           // the zigzag
                    c.Set(x, y, i % 2 == 0 ? light : tip);
                    c.Set(x, y + 1, dark);                         // shadow under the blades
                }
            }
            if (seed == 3) { c.Set(4, 12, C(160, 150, 70)); c.Set(11, 5, C(140, 70, 110)); }   // a tiny flower or two
            return SaveTile($"V09_Grass_{seed}", c, Mid, false);
        }

        /// <summary>Cempasúchil, the flower of the dead: orange marigolds on the dark grass (zone colour near the start).</summary>
        static TileBase Cempasuchil()
        {
            var c = new PixelCanvas(16, 16, C(24, 50, 14));
            c.Speckle(77, C(19, 42, 10), C(30, 60, 18), 0.18f);
            int[,] flowers = { { 3, 4 }, { 10, 3 }, { 6, 10 }, { 13, 11 }, { 1, 13 } };
            for (int i = 0; i < flowers.GetLength(0); i++)
            {
                int x = flowers[i, 0], y = flowers[i, 1];
                c.Set(x, y + 1, C(20, 60, 16)); c.Set(x, y + 2, C(20, 60, 16));
                c.Set(x, y, C(240, 140, 20)); c.Set(x - 1, y, C(220, 110, 10)); c.Set(x + 1, y, C(220, 110, 10)); c.Set(x, y - 1, C(255, 190, 60));
            }
            return SaveTile("V09_Cempasuchil", c, Mid, false);
        }

        // ---------------- Sand ----------------

        static readonly Color32 SandBase = C(128, 102, 68);

        /// <summary>Wind ripples: two wavy lines per tile with a period of 16 px, so they continue from tile to tile.</summary>
        static TileBase WavySand(int seed)
        {
            var c = new PixelCanvas(16, 16, SandBase);
            c.Speckle(seed * 17, C(116, 92, 60), C(140, 112, 76), 0.12f);
            for (int line = 0; line < 2; line++)
                for (int x = 0; x < 16; x++)
                {
                    int y = Mathf.RoundToInt(4 + line * 8 + 1.6f * Mathf.Sin(2f * Mathf.PI * x / 16f));
                    c.Set(x, y, C(150, 122, 84));                  // lit crest
                    c.Set(x, (y + 1) % 16, C(104, 80, 52));        // shade behind it
                }
            if (seed == 2) { c.Set(3, 9, C(92, 70, 48)); c.Set(12, 14, C(90, 70, 50)); c.Set(7, 2, C(160, 140, 110)); }
            if (seed == 3) { c.Rect(9, 9, 3, 1, C(196, 186, 160)); c.Set(8, 10, C(196, 186, 160)); c.Set(12, 8, C(196, 186, 160)); }   // a little bone
            return SaveTile($"V09_Sand_{seed}", c, Mid, false);
        }

        static TileBase FlatSand()
        {
            var c = new PixelCanvas(16, 16, SandBase);
            c.Speckle(5, C(114, 90, 60), C(142, 114, 78), 0.15f);
            return SaveTile("V09_Sand_Flat", c, Mid, false);
        }

        // ---------------- Ravines with depth ----------------

        /// <summary>
        /// A ravine seen from above with the camera to the south: the far (north) wall is visible as bands of earth
        /// that darken as they go down; the sides are thin dark walls; the near (south) rim is a bright lip.
        /// The middle is almost black with a cold blue tint and a few rocks catching light far below.
        /// </summary>
        static TileBase DeepPit(int mask)
        {
            const int T = 16;
            var c = new PixelCanvas(T, T);
            Color32[] strata = { C(150, 118, 78), C(124, 94, 62), C(104, 76, 50), C(84, 60, 42), C(64, 46, 34), C(46, 34, 28), C(32, 24, 22) };
            for (int x = 0; x < T; x++)
                for (int y = 0; y < T; y++)
                {
                    // Distances to each land side (99 = no land there).
                    float dn = (mask & 1) != 0 ? y + 0.5f : 99f;
                    float de = (mask & 2) != 0 ? T - x - 0.5f : 99f;
                    float ds = (mask & 4) != 0 ? T - y - 0.5f : 99f;
                    float dw = (mask & 8) != 0 ? x + 0.5f : 99f;
                    float wobble = (PixelCanvas.Hash(x, y, mask) - 0.5f) * 0.8f;

                    // Round the corners where two land sides meet, so a hole is a hole and not a black square.
                    // Outside the rounded rim the pixel stays empty and the sand under the tile shows.
                    const float R = 7f;
                    float px = x + 0.5f, py = y + 0.5f;
                    bool cut = false;
                    void Corner(bool on, float cx, float cy, ref float vertical, ref float side, bool nearSide)
                    {
                        if (!on) return;
                        float d = R - Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                        if (d < 0f) { cut = true; return; }
                        vertical = Mathf.Min(vertical, d);
                        if (nearSide) side = Mathf.Min(side, d);
                    }
                    if (px < R && py < R) Corner((mask & 9) == 9, R, R, ref dn, ref dw, px < py);
                    if (px > T - R && py < R) Corner((mask & 3) == 3, T - R, R, ref dn, ref de, T - px < py);
                    if (px < R && py > T - R) Corner((mask & 12) == 12, R, T - R, ref ds, ref dw, px < T - py);
                    if (px > T - R && py > T - R) Corner((mask & 6) == 6, T - R, T - R, ref ds, ref de, T - px < T - py);
                    if (cut) continue;

                    // Deep inside: almost black, a little blue.
                    float depth = Mathf.Min(dn * 0.6f, Mathf.Min(de, Mathf.Min(ds, dw)));
                    Color32 col = depth > 9f ? C(8, 8, 14) : Color32.Lerp(C(22, 18, 24), C(8, 8, 14), depth / 9f);

                    if (dn < 1.2f + wobble) col = C(176, 148, 100);                          // far rim (sand edge)
                    else if (dn < 10f)                                                       // far wall: layered earth
                    {
                        int band = Mathf.Clamp((int)((dn - 1.2f + wobble) / 1.25f), 0, strata.Length - 1);
                        col = strata[band];
                        if (PixelCanvas.Hash(x, y, 91) < 0.06f && band < 4) col = Tone(col, 1.25f);   // pebbles in the wall
                    }
                    if (dw < 2.2f + wobble || de < 2.2f + wobble) col = Color32.Lerp(C(70, 52, 38), C(30, 24, 22), Mathf.Min(dw, de) / 2.2f);   // side walls
                    if (dw < 0.9f || de < 0.9f) col = C(150, 122, 84);                       // side rims
                    if (ds < 1.4f + wobble * 0.5f) col = C(160, 132, 90);                     // near lip, lit from above
                    else if (ds < 2.4f) col = C(20, 16, 18);                                  // shadow just under the lip
                    c.Set(x, y, col);
                }
            if (mask == 0 && PixelCanvas.Hash(mask, 3, 5) < 2f) { c.Set(6, 9, C(40, 40, 52)); c.Set(11, 5, C(34, 34, 46)); }   // rocks far below
            return SaveTile($"V09_Pit_{mask:00}", c, Mid, true);   // solid for enemies (Hazard layer: the player falls instead)
        }

        // ---------------- River crossing and the pyramid plaza ----------------

        /// <summary>A flat stone sticking out of the river: walkable, you jump from one to the next.</summary>
        static TileBase SteppingStone()
        {
            var c = new PixelCanvas(16, 16, C(28, 56, 92));
            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                    if (((x + y * 3 + (y / 4) * 5) % 9) == 0) c.Set(x, y, C(52, 92, 132));
            c.Ellipse(8, 9, 7.5f, 6.5f, C(176, 196, 206));      // foam around it
            c.Ellipse(8, 9, 6.5f, 5.5f, C(70, 66, 64));         // its wet side
            c.Ellipse(8, 7.5f, 6f, 4.8f, C(118, 112, 104));     // flat top
            c.Ellipse(6, 6, 2.5f, 1.5f, C(146, 140, 128));      // light
            c.Set(10, 9, C(60, 100, 50)); c.Set(11, 9, C(60, 100, 50));   // moss
            return SaveTile("V09_SteppingStone", c, Mid, false);
        }

        /// <summary>Old stone paving around the pyramid; the edge version has grass growing between the stones.</summary>
        static TileBase Plaza(bool edge)
        {
            var c = new PixelCanvas(16, 16, C(108, 98, 84));
            c.Speckle(edge ? 41 : 40, C(94, 86, 74), C(122, 112, 98), 0.12f);
            for (int i = 0; i < 16; i++) { c.Set(i, 0, C(70, 64, 56)); c.Set(i, 8, C(76, 70, 60)); }
            for (int j = 0; j < 8; j++) { c.Set(0, j, C(70, 64, 56)); c.Set(8, 8 + j, C(70, 64, 56)); }
            if (edge)
                for (int x = 0; x < 16; x++)
                    for (int y = 0; y < 16; y++)
                        if (PixelCanvas.Hash(x, y, 4) < 0.22f) c.Set(x, y, PixelCanvas.Hash(x, y, 5) < 0.5f ? C(30, 60, 18) : C(44, 80, 26));
            c.Set(4, 3, C(140, 40, 34));   // a trace of red paint
            return SaveTile(edge ? "V09_Plaza_Edge" : "V09_Plaza", c, Mid, false);
        }

        // ---------------- Inside the witches' house ----------------

        static TileBase InteriorFloor()
        {
            var c = new PixelCanvas(16, 16, C(58, 40, 30));
            for (int y = 0; y < 16; y += 4)
                for (int x = 0; x < 16; x++)
                {
                    c.Set(x, y, C(36, 24, 20));
                    if ((x + y * 3) % 11 == 0) c.Set(x, y + 2, C(44, 30, 24));   // knots
                }
            c.Speckle(12, C(50, 34, 26), C(68, 48, 36), 0.1f);
            return SaveTile("V09_Interior_Floor", c, Mid, false);
        }

        /// <summary>Dark adobe wall, one and a half tiles tall (it rises above its cell, like the temple wall).</summary>
        static TileBase InteriorWall()
        {
            var c = new PixelCanvas(16, 24, C(70, 52, 44));
            c.Speckle(13, C(60, 44, 38), C(84, 64, 54), 0.14f);
            for (int x = 0; x < 16; x++) { c.Set(x, 0, C(40, 30, 26)); c.Set(x, 16, C(44, 32, 28)); c.Set(x, 17, C(36, 26, 22)); }
            for (int y = 18; y < 24; y++) for (int x = 0; x < 16; x++) c.Set(x, y, C(48, 36, 30));   // the foot of the wall
            c.Line(3, 4, 5, 9, C(46, 34, 30)); c.Line(5, 9, 4, 12, C(46, 34, 30));                    // a crack
            return SaveTile("V09_Interior_Wall", c, new Vector2(0.5f, 8f / 24f), true);
        }
    }
}
