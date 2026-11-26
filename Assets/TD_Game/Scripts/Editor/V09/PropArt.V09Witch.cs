using UnityEditor;
using UnityEngine;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// The house of the four witches and everything inside it: the crooked hut outside (bones, purple candle light,
    /// a crow on the roof), and inside a ritual circle painted on the floor, candles, a cauldron, shelves of jars and
    /// skulls, hanging herbs, the chains on the wall and the painting of the side quest — torn on the wall, whole when
    /// the puzzle is solved, and the big picture used by the puzzle screen (an amate codex: Painal running along the
    /// road of footprints while the four Cihuateteo close in from the corners, under the sun).
    /// </summary>
    public static partial class PropArt
    {
        public const string PaintingPath = Props + "/painting_codex.png";

        public static void V09Witch()
        {
            SetLight(-0.55f, -0.6f, 0.58f);
            WitchHouse();
            RitualCircle(); Cauldron(); Shelf(); Herbs(); Candle(); WallChains(); Crow(); DeadTree(); SkullPile(); Bloodstain();
            var picture = CodexPainting();
            WallPainting(picture, false);
            WallPainting(picture, true);
        }

        static void WitchHouse()
        {
            var c = new PixelCanvas(112, 96);
            var wood = C(58, 44, 40);
            // Walls of dark planks, a little crooked.
            for (int x = 14; x < 98; x++)
                for (int y = 46; y < 92; y++)
                {
                    var col = (x / 6) % 2 == 0 ? wood : Tone(wood, 0.85f);
                    if ((x * 7 + y) % 23 == 0) col = Tone(col, 0.7f);
                    c.Set(x + (y < 60 ? (y - 60) / 8 : 0), y, Tone(col, 1f - 0.25f * Mathf.Clamp01((x - 60) / 40f)));
                }
            c.Rect(12, 88, 88, 6, C(46, 40, 38));                                       // stone base
            // Sagging thatched roof, darker and torn.
            for (int y = 6; y <= 50; y++)
            {
                int half = 18 + (y - 6) * 36 / 44;
                int sag = (int)(Mathf.Sin((y - 6) / 44f * Mathf.PI) * 3f);
                for (int x = 56 - half - sag; x <= 56 + half; x++)
                {
                    if (y > 44 && PixelCanvas.Hash(x, y, 8) < 0.35f) continue;       // ragged edge
                    var col = (x + y / 3) % 4 == 0 ? C(66, 54, 40) : C(90, 74, 52);
                    c.Set(x, y, Tone(col, 1.05f - 0.3f * Mathf.Clamp01((x - 56) / 50f)));
                }
            }
            c.Line(56, 0, 56, 8, C(50, 40, 32)); c.Line(52, 2, 60, 2, C(50, 40, 32));   // crooked pole on top
            // Door and windows with a purple candle light inside.
            c.Rect(46, 62, 20, 30, C(24, 16, 20));
            Ball(c, 56, 62, 10f, 6f, C(24, 16, 20));
            c.Rect(48, 66, 16, 24, C(40, 26, 34));
            Glow(c, 56, 80, 10f, C(170, 90, 220), 0.6f);
            foreach (int wx in new[] { 22, 78 })
            {
                c.Rect(wx, 60, 12, 10, C(30, 18, 30));
                c.Rect(wx + 2, 62, 8, 6, C(150, 80, 200));
                c.Set(wx + 5, 61, C(220, 180, 255)); c.Line(wx + 6, 60, wx + 6, 70, C(40, 26, 34));
            }
            // Bones and a skull over the door, herbs hanging from the roof edge.
            Ball(c, 56, 54, 4f, 3.6f, C(214, 206, 186)); c.Rect(54, 53, 1, 2, Ink); c.Rect(57, 53, 1, 2, Ink);
            c.Line(48, 50, 64, 58, C(214, 206, 186)); c.Line(48, 58, 64, 50, C(214, 206, 186));
            for (int h = 18; h < 96; h += 13) { c.Line(h, 50, h, 55, C(70, 90, 40)); c.Set(h, 56, C(90, 120, 50)); }
            // A crow on the roof.
            Ball(c, 76, 16, 3f, 2.4f, C(20, 18, 24)); Ball(c, 79, 13, 1.8f, 1.6f, C(20, 18, 24)); c.Set(81, 13, C(200, 160, 60)); c.Set(79, 12, C(220, 60, 60));
            c.Outline(Ink);
            c.Save($"{Props}/witch_house.png", 16, new Vector2(0.5f, 0f));
        }

        /// <summary>Chalk circle on the floor with the four directions, symbols and old blood (a floor decal).</summary>
        static void RitualCircle()
        {
            var c = new PixelCanvas(96, 64);
            var chalk = PixelCanvas.C(220, 210, 200, 200);
            var blood = PixelCanvas.C(110, 20, 24, 200);
            for (int a = 0; a < 360; a++)
            {
                float r = a * Mathf.Deg2Rad;
                c.Set(48 + Mathf.RoundToInt(Mathf.Cos(r) * 44), 32 + Mathf.RoundToInt(Mathf.Sin(r) * 29), chalk);
                c.Set(48 + Mathf.RoundToInt(Mathf.Cos(r) * 36), 32 + Mathf.RoundToInt(Mathf.Sin(r) * 23), blood);
            }
            // The four directions (a cross) and a star of four points.
            c.Line(48, 4, 48, 60, chalk); c.Line(6, 32, 90, 32, chalk);
            c.Line(20, 12, 76, 52, blood); c.Line(76, 12, 20, 52, blood);
            for (int s = 0; s < 8; s++)
            {
                float r = s * Mathf.PI / 4f + 0.4f;
                int sx = 48 + Mathf.RoundToInt(Mathf.Cos(r) * 40), sy = 32 + Mathf.RoundToInt(Mathf.Sin(r) * 26);
                c.Rect(sx - 1, sy - 1, 3, 3, chalk); c.Set(sx, sy, blood);
            }
            c.Ellipse(48, 32, 7, 5, PixelCanvas.C(90, 14, 20, 210));   // old blood in the middle
            c.Save($"{Props}/ritual_circle.png", 16, Mid);
        }

        static void Cauldron()
        {
            var c = new PixelCanvas(26, 24);
            c.Line(6, 23, 9, 16, C(40, 34, 30)); c.Line(20, 23, 17, 16, C(40, 34, 30));
            Ball(c, 13, 13, 10f, 8f, C(48, 46, 52));
            c.Ellipse(13, 7, 9, 3, C(30, 28, 34));
            c.Ellipse(13, 7, 8, 2.2f, C(90, 170, 60));                     // green brew
            c.Set(10, 6, C(180, 240, 120)); c.Set(15, 7, C(180, 240, 120));
            Ball(c, 9, 3, 2f, 2f, PixelCanvas.C(150, 200, 120, 160)); Ball(c, 15, 1, 1.5f, 1.5f, PixelCanvas.C(150, 200, 120, 120));
            c.Rect(8, 21, 10, 3, C(200, 90, 30)); c.Set(12, 20, C(255, 200, 80));   // embers under it
            c.Outline(Ink);
            c.Save($"{Props}/cauldron.png", 16, new Vector2(0.5f, 0f));
        }

        static void Shelf()
        {
            var c = new PixelCanvas(36, 34);
            var wood = C(80, 56, 40);
            c.Rect(1, 0, 3, 34, wood); c.Rect(32, 0, 3, 34, wood);
            for (int s = 0; s < 3; s++) c.Rect(1, 9 + s * 11, 34, 2, Tone(wood, 1.1f));
            Ball(c, 9, 5, 3.5f, 3.4f, C(214, 206, 186)); c.Set(8, 5, Ink); c.Set(10, 5, Ink);
            c.Rect(16, 3, 5, 6, PixelCanvas.C(90, 160, 120, 220)); c.Rect(24, 4, 5, 5, PixelCanvas.C(160, 60, 80, 220));
            c.Rect(6, 15, 4, 5, PixelCanvas.C(200, 180, 60, 220)); Ball(c, 18, 17, 4f, 3f, C(120, 80, 50)); c.Rect(25, 13, 4, 7, PixelCanvas.C(80, 80, 160, 220));
            Ball(c, 8, 27, 3f, 3f, C(214, 206, 186)); Ball(c, 15, 28, 3f, 2.6f, C(200, 192, 172)); c.Rect(22, 25, 8, 4, C(120, 40, 40));
            c.Outline(Ink);
            c.Save($"{Props}/shelf.png", 16, new Vector2(0.5f, 0f));
        }

        static void Herbs()
        {
            var c = new PixelCanvas(12, 16);
            c.Line(0, 0, 11, 0, C(80, 56, 40));
            for (int i = 0; i < 3; i++)
            {
                int x = 2 + i * 4;
                c.Line(x, 1, x, 4, C(140, 120, 80));
                Ball(c, x, 8 + i, 1.8f, 4f, i == 1 ? C(110, 80, 120) : C(70, 100, 50));
            }
            c.Outline(Ink);
            c.Save($"{Props}/herbs.png", 16, new Vector2(0.5f, 1f));
        }

        static void Candle()
        {
            var c = new PixelCanvas(5, 11);
            c.Rect(1, 4, 3, 7, C(150, 130, 170)); c.Set(1, 5, C(190, 170, 210));
            c.Rect(0, 10, 5, 1, C(120, 100, 140));
            c.Set(2, 3, C(40, 30, 30));
            c.Set(2, 2, C(255, 190, 90)); c.Set(2, 1, C(255, 240, 180)); c.Set(2, 0, C(255, 220, 140));
            c.Save($"{Props}/candle.png", 16, new Vector2(0.5f, 0f));
        }

        /// <summary>Two chains hanging from rings in the wall (the shackles are on Painal).</summary>
        static void WallChains()
        {
            var c = new PixelCanvas(28, 14);
            foreach (int x in new[] { 5, 22 })
            {
                c.Circle(x, 2, 2f, C(130, 130, 140)); c.Set(x, 2, C(40, 40, 50));
                for (int y = 4; y < 14; y += 2) { c.Set(x, y, C(150, 150, 160)); c.Set(x + (y % 4 == 0 ? 1 : -1), y + 1, C(110, 110, 120)); }
            }
            c.Save($"{Props}/wall_chains.png", 16, new Vector2(0.5f, 0f));
        }

        static void Crow()
        {
            var c = new PixelCanvas(10, 8);
            Ball(c, 4, 5, 3.5f, 2.6f, C(22, 20, 26)); Ball(c, 7.5f, 3, 1.8f, 1.7f, C(22, 20, 26));
            c.Set(9, 3, C(200, 160, 60)); c.Set(8, 2, C(220, 60, 60));
            c.Line(2, 7, 2, 7, C(60, 50, 40)); c.Set(5, 7, C(60, 50, 40));
            c.Outline(Ink);
            c.Save($"{Props}/crow.png", 16, new Vector2(0.5f, 0f));
        }

        static void DeadTree()
        {
            var c = new PixelCanvas(34, 46);
            var bark = C(66, 52, 46);
            Tube(c, 17, 45, 16, 18, 3.5f, 2f, bark);
            Tube(c, 16, 26, 6, 12, 1.6f, 0.8f, bark); Tube(c, 16, 22, 27, 8, 1.6f, 0.8f, bark); Tube(c, 16, 18, 15, 2, 1.4f, 0.6f, bark);
            Tube(c, 9, 15, 3, 13, 0.9f, 0.5f, bark); Tube(c, 24, 11, 31, 12, 0.9f, 0.5f, bark);
            c.Rect(13, 44, 9, 2, Tone(bark, 0.8f));
            Ball(c, 27, 7, 1.8f, 1.5f, C(22, 20, 26));   // a crow on a branch
            c.Outline(Ink);
            c.Save($"{Props}/dead_tree.png", 16, new Vector2(0.5f, 0f));
        }

        static void SkullPile()
        {
            var c = new PixelCanvas(18, 12);
            foreach (var (x, y) in new[] { (4, 8), (10, 8), (15, 9), (7, 4), (12, 4) })
            {
                Ball(c, x, y, 3f, 2.7f, C(214, 206, 186));
                c.Set(x - 1, y, Ink); c.Set(x + 1, y, Ink);
            }
            c.Outline(Ink);
            c.Save($"{Props}/skull_pile.png", 16, new Vector2(0.5f, 0f));
        }

        static void Bloodstain()
        {
            var c = new PixelCanvas(20, 12);
            c.Ellipse(9, 6, 8, 4, PixelCanvas.C(90, 14, 20, 200));
            c.Ellipse(16, 8, 3, 2, PixelCanvas.C(90, 14, 20, 200));
            c.Ellipse(4, 3, 2, 1.5f, PixelCanvas.C(90, 14, 20, 200));
            c.Save($"{Props}/bloodstain.png", 16, Mid);
        }

        // ---------------- The painting ----------------

        /// <summary>The 96 x 96 codex painting. Flat colours with thick outlines, like the Mexica books of amate paper.</summary>
        static PixelCanvas CodexPainting()
        {
            const int S = 96;
            var c = new PixelCanvas(S, S, C(214, 190, 140));
            c.Speckle(9, C(200, 176, 126), C(226, 204, 156), 0.1f);
            for (int y = 0; y < S; y += 3) for (int x = 0; x < S; x++) if (PixelCanvas.Hash(x, y, 4) < 0.15f) c.Set(x, y, C(204, 180, 130));   // paper fibres
            // Red border with yellow corners.
            for (int i = 0; i < S; i++) for (int k = 0; k < 4; k++) { c.Set(i, k, C(170, 40, 34)); c.Set(i, S - 1 - k, C(170, 40, 34)); c.Set(k, i, C(170, 40, 34)); c.Set(S - 1 - k, i, C(170, 40, 34)); }
            // The sun at the top.
            for (int a = 0; a < 12; a++)
            {
                float r = a * Mathf.PI / 6f;
                c.Line(48, 18, 48 + Mathf.RoundToInt(Mathf.Cos(r) * 12), 18 + Mathf.RoundToInt(Mathf.Sin(r) * 12), C(220, 150, 40));
            }
            c.Circle(48, 18, 7f, C(20, 16, 14)); c.Circle(48, 18, 6f, C(240, 190, 60)); c.Circle(48, 18, 2.5f, C(170, 40, 34));

            // The road of footprints crossing the page (and a crossroads at the bottom).
            for (int x = 8; x < 88; x += 7) { c.Rect(x, 60 + (x / 7 % 2) * 3, 3, 2, C(30, 24, 20)); c.Set(x + 3, 60 + (x / 7 % 2) * 3, C(30, 24, 20)); }
            c.Rect(8, 84, 80, 3, C(90, 70, 50)); c.Rect(46, 70, 3, 18, C(90, 70, 50));

            // Painal running, in the middle: blue body paint, feathered anklets, a white loincloth.
            CodexRunner(c, 48, 52);

            // The four Cihuateteo in the corners, black skirts and skull faces, claws out.
            CodexWitch(c, 16, 30, false); CodexWitch(c, 80, 30, true); CodexWitch(c, 16, 76, false); CodexWitch(c, 80, 76, true);
            c.Save(PaintingPath, 16, Mid);
            return c;
        }

        static void CodexRunner(PixelCanvas c, int x, int y)
        {
            var ink = C(20, 16, 14);
            var body = C(60, 110, 180);
            Tube(c, x - 1, y - 12, x + 1, y + 2, 4f, 3.5f, body);                     // torso
            Tube(c, x, y + 2, x - 8, y + 12, 2f, 2f, body); Tube(c, x, y + 2, x + 9, y + 8, 2f, 2f, body);   // running legs
            Tube(c, x, y - 8, x - 9, y - 4, 1.6f, 1.6f, body); Tube(c, x, y - 8, x + 9, y - 14, 1.6f, 1.6f, body);   // arms
            c.Rect(x - 4, y - 1, 8, 4, C(236, 230, 214));                             // loincloth
            Ball(c, x + 1, y - 18, 4.5f, 4.5f, C(170, 120, 80));                      // head
            c.Set(x + 3, y - 19, ink);
            for (int f = 0; f < 4; f++) c.Line(x - 2 + f, y - 22, x - 6 + f * 2, y - 28, f % 2 == 0 ? C(60, 150, 110) : C(220, 170, 50));   // feathers
            c.Rect(x - 10, y + 12, 4, 2, C(60, 150, 110)); c.Rect(x + 8, y + 8, 4, 2, C(60, 150, 110));         // anklets
            c.Outline(ink);
        }

        static void CodexWitch(PixelCanvas c, int x, int y, bool facingLeft)
        {
            var ink = C(20, 16, 14);
            int dir = facingLeft ? -1 : 1;
            Poly(c, C(36, 30, 40), V(x - 8, y + 10), V(x + 8, y + 10), V(x + 4, y - 4), V(x - 4, y - 4));   // black skirt
            Ball(c, x, y - 9, 5f, 5f, C(226, 218, 196));                              // skull face
            c.Rect(x - 3, y - 10, 2, 2, ink); c.Rect(x + 1, y - 10, 2, 2, ink);
            c.Line(x - 2, y - 6, x + 2, y - 6, ink);
            c.Line(x - 5, y - 13, x - 7, y - 4, C(40, 30, 40)); c.Line(x + 5, y - 13, x + 7, y - 4, C(40, 30, 40));   // hair
            Tube(c, x, y - 2, x + dir * 12, y - 8, 1.3f, 1f, C(226, 218, 196));       // reaching arm
            for (int k = 0; k < 3; k++) c.Line(x + dir * 12, y - 8, x + dir * (14 + k), y - 11 + k * 2, ink);   // claws
            c.Set(x + dir * 2, y + 2, C(170, 40, 34));
        }

        /// <summary>The painting on the wall (a small copy), whole or torn: pieces out of place, a rip and a crooked frame.</summary>
        static void WallPainting(PixelCanvas picture, bool torn)
        {
            const int W = 35, H = 35;   // fits on the two-row north wall of the room
            var c = new PixelCanvas(W, H);
            var frame = C(92, 62, 38);
            c.Rect(0, 0, W, H, frame);
            for (int i = 0; i < W; i++) { c.Set(i, 0, Tone(frame, 1.25f)); c.Set(i, H - 1, Tone(frame, 0.7f)); }
            // A 27 x 27 copy of the 96 x 96 picture.
            for (int x = 0; x < 27; x++)
                for (int y = 0; y < 27; y++)
                {
                    int sx = x * 96 / 27, sy = y * 96 / 27;
                    if (torn)
                    {
                        // Swap the 3 x 3 blocks around (the same mess the puzzle starts with) and leave gaps.
                        int bx = x / 9, by = y / 9;
                        int[] mapTo = { 4, 8, 1, 6, 0, 3, 2, 7, 5 };
                        int piece = mapTo[by * 3 + bx];
                        sx = (piece % 3) * 32 + (x % 9) * 32 / 9;
                        sy = (piece / 3) * 32 + (y % 9) * 32 / 9;
                        if (x % 9 == 0 || y % 9 == 0) { c.Set(4 + x, 4 + y, C(30, 20, 18)); continue; }
                    }
                    c.Set(4 + x, 4 + y, picture.Get(sx, sy));
                }
            if (torn)
            {
                c.Line(5, 22, 19, 31, C(30, 20, 18)); c.Line(6, 22, 20, 31, C(160, 140, 100));   // a rip
                c.Rect(22, 4, 9, 9, C(40, 30, 26));                                           // a missing corner
            }
            c.Outline(Ink);
            c.Save($"{Props}/{(torn ? "painting_torn" : "painting_whole")}.png", 16, new Vector2(0.5f, 0f));
        }

        public static Texture2D PaintingTexture() => AssetDatabase.LoadAssetAtPath<Texture2D>(PaintingPath);
    }
}
