using System.IO;
using UnityEngine;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// v0.9 nature and landmarks:
    ///   - a low wall of the map's old rocks (the team's rock and trees are used as drawn: see PropArt.V09Team);
    ///   - fallen logs and bushes (you can jump over them);
    ///   - landmarks to find your way: the sacred ceiba, the giant skull rock, carved stelae at crossroads, the head of a
    ///     fallen god in the sand, the statue of Xólotl by the lake;
    ///   - details for each region: cempasúchil, mushrooms and ferns in the forest; cacti, agaves, skulls and broken
    ///     pots in the desert; standing torches.
    /// </summary>
    public static partial class PropArt
    {
        public static void V09Nature()
        {
            SetLight(-0.55f, -0.6f, 0.58f);
            var oldRock = LoadPixels($"{TerrainArt.Folder}/Rock.png");
            var mossy = LoadPixels($"{TerrainArt.Folder}/Rock_Mossy.png");
            RockGroup("rock_wall", 44, 18, (oldRock, 1, 0, 2, false), (mossy, 1, 13, 1, false), (oldRock, 1, 26, 2, true));
            Log(false); Log(true); Bush();
            Ceiba(); SkullRock(); Stela(); FallenGodHead(); XoloStatue();
            Mushrooms(); Fern(); Cactus(); Agave(); OxSkull(); Pottery(); StandingTorch(0); StandingTorch(1);
        }

        // ---------------- Pixel helpers ----------------

        /// <summary>The smallest canvas that holds the drawn pixels of an area.</summary>
        static PixelCanvas Crop(PixelCanvas src, int x0, int y0, int w, int h)
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
            for (int x = x0; x < x0 + w; x++)
                for (int y = y0; y < y0 + h; y++)
                    if (!src.IsEmpty(x, y)) { minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y); maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y); }
            if (maxX < 0) return new PixelCanvas(1, 1);
            var c = new PixelCanvas(maxX - minX + 1, maxY - minY + 1);
            for (int x = minX; x <= maxX; x++) for (int y = minY; y <= maxY; y++) c.Set(x - minX, y - minY, src.Get(x, y));
            return c;
        }

        static PixelCanvas LoadPixels(string path)
        {
            if (!File.Exists(path)) return null;
            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(path));
            var c = new PixelCanvas(tex.width, tex.height);
            for (int x = 0; x < tex.width; x++)
                for (int y = 0; y < tex.height; y++)
                {
                    var p = tex.GetPixel(x, tex.height - 1 - y);
                    if (p.a > 0.004f) c.Set(x, y, p);   // keep faint pixels too (the team's fog is almost transparent)
                }
            Object.DestroyImmediate(tex);
            return c;
        }

        /// <summary>Several rocks drawn together, back to front: (picture, integer scale, x, y, mirrored).</summary>
        static void RockGroup(string name, int w, int h, params (PixelCanvas pic, int scale, int x, int y, bool flip)[] parts)
        {
            var c = new PixelCanvas(w, h);
            foreach (var (pic, scale, px, py, flip) in parts)
            {
                if (pic == null) continue;
                for (int x = 0; x < pic.W * scale; x++)
                    for (int y = 0; y < pic.H * scale; y++)
                    {
                        int sx = flip ? pic.W - 1 - x / scale : x / scale;
                        var col = pic.Get(sx, y / scale);
                        if (col.a > 0) c.Set(px + x, py + y, col);
                    }
            }
            c.Save($"{Props}/{name}.png", 16, Feet);
        }

        // ---------------- Low obstacles you can jump ----------------

        static void Log(bool vertical)
        {
            var bark = C(92, 62, 40);
            PixelCanvas c;
            if (!vertical)
            {
                c = new PixelCanvas(48, 18);
                Tube(c, 5, 10, 43, 10, 5.5f, 5f, bark);
                Ball(c, 44, 10, 3.5f, 5f, C(170, 130, 86)); c.Circle(44, 10, 1.6f, C(130, 96, 60));   // cut end with rings
                for (int x = 8; x < 40; x += 5) c.Set(x, 7 + (x % 3), Tone(bark, 0.7f));
                Ball(c, 20, 6, 3f, 1.4f, C(60, 100, 50)); c.Set(31, 14, C(200, 190, 150));             // moss, a mushroom
            }
            else
            {
                c = new PixelCanvas(18, 46);
                Tube(c, 9, 6, 9, 40, 5.5f, 5f, bark);
                Ball(c, 9, 41, 5f, 3.5f, C(170, 130, 86)); c.Circle(9, 41, 1.6f, C(130, 96, 60));
                for (int y = 9; y < 36; y += 5) c.Set(7 + (y % 3), y, Tone(bark, 0.7f));
                Ball(c, 6, 18, 1.4f, 3f, C(60, 100, 50));
            }
            c.Outline(Ink);
            c.Save($"{Props}/{(vertical ? "log_vertical" : "log_horizontal")}.png", 16, Feet);
        }

        static void Bush()
        {
            var c = new PixelCanvas(22, 16);
            Ball(c, 7, 9, 6f, 5.5f, C(36, 74, 30)); Ball(c, 15, 9, 6f, 5.5f, C(32, 66, 28)); Ball(c, 11, 6, 6f, 5f, C(44, 86, 34));
            c.Set(8, 5, C(200, 60, 60)); c.Set(14, 8, C(200, 60, 60)); c.Set(11, 11, C(200, 60, 60));   // red berries
            c.Outline(Ink);
            c.Save($"{Props}/bush.png", 16, Feet);
        }

        // ---------------- Landmarks ----------------

        /// <summary>The sacred ceiba of the forest: a huge pale trunk with buttress roots and a dark crown.</summary>
        static void Ceiba()
        {
            var c = new PixelCanvas(88, 116);
            var trunk = C(150, 140, 120);
            Tube(c, 44, 112, 44, 50, 10f, 7f, trunk);
            Tube(c, 44, 104, 26, 114, 5f, 2f, trunk); Tube(c, 44, 104, 62, 114, 5f, 2f, trunk);   // buttress roots
            Tube(c, 44, 70, 22, 46, 4f, 2f, trunk); Tube(c, 44, 64, 68, 40, 4f, 2f, trunk);
            for (int t = 60; t < 108; t += 6) c.Set(40 + (t % 7), t, C(110, 100, 86));          // thorns
            var leaf = C(30, 66, 30);
            Ball(c, 44, 30, 40f, 24f, Tone(leaf, 0.8f));
            Ball(c, 26, 40, 20f, 14f, leaf); Ball(c, 62, 38, 22f, 14f, Tone(leaf, 0.9f)); Ball(c, 44, 22, 26f, 16f, Tone(leaf, 1.15f));
            c.Speckle(31, Tone(leaf, 0.7f), Tone(leaf, 1.35f), 0.05f);
            for (int s = 0; s < 6; s++) Ball(c, 20 + s * 10, 52 + (s % 2) * 3, 2f, 3f, C(220, 210, 200));   // white flowers hanging
            c.Outline(Ink);
            c.Save($"{Props}/ceiba.png", 16, Feet);
        }

        /// <summary>A rock in the desert shaped like a giant skull, with dark eye holes you see from far away.</summary>
        static void SkullRock()
        {
            var c = new PixelCanvas(80, 64);
            var stone = C(176, 164, 140);
            Ball(c, 40, 28, 34f, 26f, stone);
            Ball(c, 40, 52, 22f, 10f, Tone(stone, 0.9f));
            Ball(c, 27, 28, 8f, 9f, C(20, 16, 18)); Ball(c, 53, 28, 8f, 9f, C(20, 16, 18));     // eye holes
            Poly(c, C(26, 20, 22), V(37, 38), V(43, 38), V(40, 45));                           // nose
            for (int t = 24; t < 58; t += 6) c.Rect(t, 54, 4, 6, Tone(stone, 1.1f));           // teeth
            c.Speckle(44, Tone(stone, 0.82f), Tone(stone, 1.1f), 0.06f);
            c.Rect(8, 60, 64, 4, C(128, 102, 68));                                              // sand at its foot
            c.Outline(Ink);
            c.Save($"{Props}/skull_rock.png", 16, Feet);
        }

        /// <summary>A carved stela with a glyph and a pointing hand: it stands at crossroads.</summary>
        static void Stela()
        {
            var c = new PixelCanvas(16, 46);
            var stone = C(118, 112, 100);
            c.Rect(2, 4, 12, 40, stone);
            Ball(c, 8, 5, 6f, 3f, stone);
            c.Rect(0, 42, 16, 4, Tone(stone, 0.8f));
            for (int y = 10; y < 40; y += 8) { c.Rect(4, y, 8, 6, Tone(stone, 0.8f)); c.Set(6, y + 2, C(170, 50, 40)); c.Set(9, y + 3, C(60, 130, 100)); }
            c.Rect(3, 4, 1, 38, Tone(stone, 1.2f));
            c.Outline(Ink);
            c.Save($"{Props}/stela_cross.png", 16, Feet);
        }

        /// <summary>The broken head of a stone god half buried in the sand (Mictlantecuhtli, lord of the dead).</summary>
        static void FallenGodHead()
        {
            var c = new PixelCanvas(68, 46);
            var stone = C(140, 132, 118);
            Ball(c, 34, 26, 28f, 20f, stone);
            Ball(c, 22, 22, 7f, 7f, C(30, 24, 26)); Ball(c, 46, 22, 7f, 7f, C(30, 24, 26));
            c.Set(22, 22, C(220, 60, 50)); c.Set(46, 22, C(220, 60, 50));                     // something still glows inside
            for (int t = 18; t < 52; t += 5) c.Rect(t, 34, 3, 5, Tone(stone, 1.15f));
            for (int f = 0; f < 7; f++) Tube(c, 10 + f * 8, 8, 6 + f * 9, -2, 2f, 1f, f % 2 == 0 ? C(60, 120, 100) : C(150, 40, 34));   // broken crown
            c.Line(30, 6, 36, 30, Tone(stone, 0.6f)); c.Line(36, 30, 33, 44, Tone(stone, 0.6f));   // a long crack
            for (int x = 0; x < 68; x++) for (int y = 38; y < 46; y++) if (PixelCanvas.Hash(x, y, 9) < 0.7f - (y - 38) * 0.05f) c.Set(x, y, C(128, 102, 68));   // sand over it
            c.Outline(Ink);
            c.Save($"{Props}/fallen_god.png", 16, Feet);
        }

        /// <summary>Xólotl, the dog god who guides the dead: a stone statue by the water.</summary>
        static void XoloStatue()
        {
            var c = new PixelCanvas(26, 34);
            var stone = C(96, 100, 104);
            c.Rect(2, 26, 22, 8, Tone(stone, 0.8f));
            Tube(c, 13, 24, 13, 12, 5f, 4f, stone);
            Ball(c, 13, 9, 6f, 5f, stone);
            Poly(c, stone, V(8, 6), V(7, -1), V(11, 4)); Poly(c, stone, V(18, 6), V(19, -1), V(15, 4));
            c.Set(11, 9, C(240, 200, 80)); c.Set(15, 9, C(240, 200, 80));
            c.Rect(8, 17, 10, 2, C(150, 40, 34));
            c.Outline(Ink);
            c.Save($"{Props}/xolo_statue.png", 16, Feet);
        }

        // ---------------- Region details ----------------

        static void Mushrooms()
        {
            var c = new PixelCanvas(12, 9);
            foreach (var (x, h) in new[] { (3, 5), (8, 7), (10, 4) })
            {
                c.Rect(x, 9 - h + 2, 1, h - 2, C(220, 210, 190));
                Ball(c, x + 0.5f, 9 - h + 1, 2.4f, 1.5f, C(180, 50, 40));
                c.Set(x, 9 - h, C(240, 230, 220));
            }
            c.Outline(Ink);
            c.Save($"{Props}/mushrooms.png", 16, Feet);
        }

        static void Fern()
        {
            var c = new PixelCanvas(18, 12);
            for (int f = 0; f < 5; f++)
            {
                float a = Mathf.Lerp(-1.1f, 1.1f, f / 4f);
                int ex = 9 + Mathf.RoundToInt(Mathf.Sin(a) * 8), ey = 11 - Mathf.RoundToInt(Mathf.Cos(a) * 10);
                c.Line(9, 11, ex, ey, f % 2 == 0 ? C(50, 100, 40) : C(40, 84, 32));
            }
            c.Save($"{Props}/fern.png", 16, Feet);
        }

        static void Cactus()
        {
            var c = new PixelCanvas(16, 28);
            var green = C(70, 110, 70);
            Tube(c, 8, 27, 8, 5, 3.5f, 3f, green);
            Tube(c, 8, 16, 3, 12, 1.8f, 1.6f, green); Tube(c, 3, 12, 3, 7, 1.6f, 1.4f, green);
            Tube(c, 8, 13, 13, 10, 1.8f, 1.6f, green); Tube(c, 13, 10, 13, 5, 1.6f, 1.4f, green);
            for (int y = 6; y < 26; y += 3) c.Set(8, y, C(210, 210, 170));
            c.Set(8, 3, C(230, 120, 160));   // a flower
            c.Outline(Ink);
            c.Save($"{Props}/cactus.png", 16, Feet);
        }

        static void Agave()
        {
            var c = new PixelCanvas(22, 14);
            for (int f = 0; f < 7; f++)
            {
                float a = Mathf.Lerp(-1.3f, 1.3f, f / 6f);
                Tube(c, 11, 13, 11 + Mathf.Sin(a) * 10, 13 - Mathf.Cos(a) * 12, 1.8f, 0.5f, f % 2 == 0 ? C(96, 130, 110) : C(80, 112, 96));
            }
            c.Outline(Ink);
            c.Save($"{Props}/agave.png", 16, Feet);
        }

        static void OxSkull()
        {
            var c = new PixelCanvas(16, 10);
            Ball(c, 8, 6, 4f, 3.4f, C(220, 212, 192));
            Tube(c, 5, 4, 1, 1, 1.2f, 0.6f, C(220, 212, 192)); Tube(c, 11, 4, 15, 1, 1.2f, 0.6f, C(220, 212, 192));
            c.Set(6, 5, Ink); c.Set(10, 5, Ink); c.Rect(7, 8, 3, 1, C(120, 110, 100));
            c.Outline(Ink);
            c.Save($"{Props}/ox_skull.png", 16, Feet);
        }

        static void Pottery()
        {
            var c = new PixelCanvas(16, 9);
            Ball(c, 5, 5, 4f, 3.5f, C(170, 90, 56)); c.Rect(3, 3, 5, 1, C(40, 30, 26));
            Poly(c, C(160, 84, 52), V(10, 8), V(15, 7), V(13, 4));
            c.Set(5, 6, C(230, 200, 120)); c.Set(13, 6, C(230, 200, 120));
            c.Outline(Ink);
            c.Save($"{Props}/pottery.png", 16, Feet);
        }

        static void StandingTorch(int frame)
        {
            var c = new PixelCanvas(10, 30);
            c.Rect(4, 10, 2, 20, C(80, 56, 38));
            c.Rect(2, 8, 6, 3, C(60, 50, 44));
            int lean = frame == 0 ? 0 : 1;
            Ball(c, 5 + lean * 0.5f, 5, 2.6f, 4f, C(230, 110, 30));
            Ball(c, 5 + lean, 4, 1.6f, 2.6f, C(255, 200, 80));
            c.Set(5 + lean, 1, C(255, 240, 180));
            c.Outline(Ink);
            c.Save($"{Props}/torch_{frame}.png", 16, Feet);
        }
    }
}
