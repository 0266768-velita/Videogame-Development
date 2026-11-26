using UnityEngine;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// The Pyramid of the Feathered Serpent, the great landmark of the map (40 x 26 tiles, 16 px per tile like the
    /// rest of the world, seen from the same high angle as the trees and houses). Five stepped tiers of old limestone
    /// with talud-tablero faces: a frieze of skulls at the base, serpent heads sticking out of the third tier (like the
    /// temple of Quetzalcóatl in Teotihuacán), stepped frets with traces of red paint. A wide staircase climbs the front
    /// between two balustrades that end in feathered serpent heads; on the summit, a shrine whose door is the open mouth
    /// of a serpent, crowned with merlons and a sun disc. Cracks, moss and hanging vines make it ancient.
    /// Layout constants are public so the builder can put the colliders exactly under the picture.
    /// </summary>
    public static partial class PropArt
    {
        public const int PyramidW = 640, PyramidH = 416;
        public const int StairLeft = 272, StairRight = 368;           // walkable staircase (pixels)
        public const int SummitLeft = 168, SummitRight = 472;
        public const int SummitFront = 198, ShrineFront = 150;         // summit walkable band: ShrineFront..SummitFront (pixels from the top)

        static int TierL(int i) => i * 42;
        static int TierR(int i) => PyramidW - i * 42;
        static int TierBack(int i) => 40 + i * 18;
        static int TierFront(int i) => PyramidH - i * 46;
        const int FaceH = 34;

        public static Sprite PyramidSprite()
        {
            var c = new PixelCanvas(PyramidW, PyramidH);
            var top = C(156, 144, 122);
            for (int i = 0; i < 5; i++) Tier(c, i, top);
            Summit(c);
            Staircase(c);
            Shrine(c);
            Weather(c);
            c.Outline(C(28, 22, 18));
            return c.Save($"{Props}/v09_pyramid.png", 16, new Vector2(0.5f, 0f));
        }

        static void Tier(PixelCanvas c, int i, Color32 top)
        {
            int l = TierL(i), r = TierR(i), back = TierBack(i), front = TierFront(i), faceTop = front - FaceH;
            int nl = TierL(i + 1), nr = TierR(i + 1), nback = TierBack(i + 1);

            // The visible ring of this tier: lit west slope, shaded east slope, the back and the ledge in front.
            for (int x = l; x < r; x++)
                for (int y = back; y < faceTop; y++)
                {
                    float n = PixelCanvas.Hash(x, y, 20 + i);
                    Color32 col;
                    if (x < nl) col = Tone(top, 1.08f - 0.1f * (x - l) / (float)(nl - l));                  // west: in the light
                    else if (x >= nr) col = Tone(top, 0.7f + 0.08f * (r - x) / (float)(r - nr));            // east: shadow
                    else if (y < nback) col = Tone(top, 0.86f);                                              // north
                    else col = top;                                                                          // ledge
                    if (n < 0.07f) col = Tone(col, 0.9f); else if (n > 0.95f) col = Tone(col, 1.08f);
                    // Courses of stone blocks.
                    if ((x < nl || x >= nr) && (y - back) % 7 == 0) col = Tone(col, 0.82f);
                    if (y >= nback && x >= nl && x < nr && (x + (y / 7) * 9) % 18 == 0) col = Tone(col, 0.86f);
                    c.Set(x, y, col);
                }
            // Cast shadow of the tier above onto this ledge (light from the west).
            if (i < 4)
                for (int y = nback; y < TierFront(i + 1); y++)
                    for (int x = nr; x < Mathf.Min(r, nr + 10); x++) c.Set(x, y, Tone(c.Get(x, y), 0.8f));

            // Front face: sloped talud below, framed tablero above.
            for (int x = l; x < r; x++)
                for (int y = faceTop; y < front; y++)
                {
                    bool talud = y >= front - 12;
                    var col = talud ? C(134, 122, 104) : C(118, 106, 90);
                    if (talud && (x + y) % 9 == 0) col = Tone(col, 0.88f);
                    float east = Mathf.Clamp01((x - (r - 70)) / 70f);
                    col = Tone(col, 1f - 0.28f * east);                         // the east end is in shadow
                    if (x < l + 3) col = Tone(col, 1.15f);                       // lit corner
                    c.Set(x, y, col);
                }
            // Tablero: a raised frame with a recessed panel.
            int py0 = faceTop + 3, py1 = front - 14;
            for (int x = l + 4; x < r - 4; x++)
            {
                c.Set(x, faceTop + 1, C(170, 158, 136));
                c.Set(x, py1 + 1, C(84, 74, 64));
                for (int y = py0; y < py1; y++) c.Set(x, y, Tone(C(86, 76, 66), 1f - 0.25f * Mathf.Clamp01((x - (r - 70)) / 70f)));
            }
            int midY = (py0 + py1) / 2;
            if (i == 0) SkullFrieze(c, l + 8, r - 8, midY);
            else if (i == 2) SerpentFrieze(c, l + 20, r - 20, midY);
            else Greca(c, l + 6, r - 6, py0 + 2, i);
        }

        /// <summary>Stepped fret (xicalcoliuhqui) in ochre with traces of cinnabar.</summary>
        static void Greca(PixelCanvas c, int x0, int x1, int y, int seed)
        {
            for (int x = x0; x < x1; x++)
            {
                int k = (x - x0) % 16;
                int h = k < 4 ? 0 : k < 8 ? 3 : k < 12 ? 6 : 3;
                c.Set(x, y + 9 - h, C(176, 140, 60));
                if (k == 4 || k == 8 || k == 12) for (int j = 0; j <= 3; j++) c.Set(x, y + 9 - h + j, C(176, 140, 60));
                if (PixelCanvas.Hash(x, seed, 33) < 0.5f) c.Set(x, y + 11, C(140, 40, 32));
            }
        }

        /// <summary>A row of carved skulls (tzompantli) along the base.</summary>
        static void SkullFrieze(PixelCanvas c, int x0, int x1, int y)
        {
            for (int x = x0 + 6; x < x1 - 6; x += 15)
            {
                Ball(c, x, y, 5f, 4.5f, C(206, 196, 172));
                c.Rect(x - 3, y - 1, 2, 2, C(30, 24, 22)); c.Rect(x + 1, y - 1, 2, 2, C(30, 24, 22));
                c.Set(x, y + 2, C(60, 50, 44));
                for (int t = -2; t <= 2; t += 2) c.Set(x + t, y + 4, C(90, 80, 70));
            }
        }

        /// <summary>Serpent heads sticking out of the wall, alternating with rain-god masks.</summary>
        static void SerpentFrieze(PixelCanvas c, int x0, int x1, int y)
        {
            int n = 0;
            for (int x = x0 + 12; x < x1 - 12; x += 34, n++)
            {
                if (n % 2 == 0)
                {
                    for (int f = -2; f <= 2; f++) Ball(c, x + f * 3, y - 6, 2f, 3f, f % 2 == 0 ? C(60, 130, 100) : C(46, 104, 84));   // feathers
                    Ball(c, x, y, 7f, 5.5f, C(70, 128, 98));
                    c.Rect(x - 4, y - 2, 2, 2, C(240, 210, 80)); c.Rect(x + 2, y - 2, 2, 2, C(240, 210, 80));                     // eyes
                    c.Rect(x - 4, y + 3, 9, 2, C(30, 20, 20));                                                                      // mouth
                    c.Set(x - 3, y + 3, C(230, 222, 200)); c.Set(x + 3, y + 3, C(230, 222, 200));                                    // fangs
                }
                else
                {
                    Ball(c, x, y, 6f, 5f, C(110, 120, 130));                                                                         // goggle-eyed mask
                    c.Circle(x - 2.5f, y - 1, 2f, C(30, 30, 40)); c.Circle(x + 2.5f, y - 1, 2f, C(30, 30, 40));
                    c.Rect(x - 3, y + 3, 7, 1, C(160, 50, 40));
                }
            }
        }

        static void Summit(PixelCanvas c)
        {
            // The flat top of the last tier: paving stones.
            int l = TierL(4) + 2, r = TierR(4) - 2, back = TierBack(4), front = TierFront(4) - FaceH;
            for (int x = l; x < r; x++)
                for (int y = back; y < front; y++)
                {
                    var col = C(150, 138, 116);
                    if (x % 12 == 0 || y % 8 == 0) col = C(120, 110, 94);
                    if (PixelCanvas.Hash(x, y, 50) < 0.05f) col = Tone(col, 0.9f);
                    c.Set(x, y, col);
                }
        }

        static void Staircase(PixelCanvas c)
        {
            int top = SummitFront, bottom = PyramidH;
            // Balustrades (alfardas) on both sides, with a line of jade inlay.
            for (int y = top - 6; y < bottom; y++)
            {
                for (int x = StairLeft - 12; x < StairLeft; x++) c.Set(x, y, x < StairLeft - 9 ? C(176, 164, 140) : C(126, 114, 98));
                for (int x = StairRight; x < StairRight + 12; x++) c.Set(x, y, x > StairRight + 8 ? C(92, 82, 70) : C(120, 108, 92));
                c.Set(StairLeft - 6, y, C(60, 130, 100)); c.Set(StairRight + 6, y, C(50, 110, 86));
            }
            // Steps: a lit tread and a dark riser every 4 px, a worn red runner in the middle.
            for (int y = top; y < bottom; y++)
                for (int x = StairLeft; x < StairRight; x++)
                {
                    int k = (bottom - y) % 4;
                    var col = k == 0 ? C(96, 86, 74) : k == 1 ? C(178, 166, 142) : C(150, 138, 118);
                    float east = (x - StairLeft) / (float)(StairRight - StairLeft);
                    col = Tone(col, 1.04f - 0.14f * east);
                    if (Mathf.Abs(x - (StairLeft + StairRight) / 2) < 10 && k != 0) col = Mix(col, C(140, 46, 36), 0.35f);
                    if (PixelCanvas.Hash(x, y, 61) < 0.03f) col = Tone(col, 0.85f);
                    c.Set(x, y, col);
                }
            // Feathered serpent heads at the foot of the balustrades, jaws open towards the plaza.
            SerpentHeadBig(c, StairLeft - 6, bottom - 14);
            SerpentHeadBig(c, StairRight + 6, bottom - 14);
        }

        static void SerpentHeadBig(PixelCanvas c, int x, int y)
        {
            for (int f = -3; f <= 3; f++) Tube(c, x + f * 4, y - 6, x + f * 6, y - 18, 2.4f, 1.2f, f % 2 == 0 ? C(56, 136, 100) : C(196, 160, 60));   // feather crest
            Ball(c, x, y, 15f, 11f, C(70, 132, 100));                     // the head
            Ball(c, x, y + 6, 12f, 6f, C(56, 108, 84));                   // lower jaw
            c.Rect(x - 9, y + 3, 19, 5, C(26, 14, 14));                   // open mouth
            Poly(c, C(170, 40, 36), V(x - 3, y + 4), V(x + 3, y + 4), V(x + 2, y + 13), V(x, y + 10), V(x - 2, y + 13));   // forked tongue
            for (int f = -8; f <= 8; f += 4) { c.Set(x + f, y + 3, C(236, 230, 210)); c.Set(x + f, y + 4, C(236, 230, 210)); }   // fangs
            Ball(c, x - 6, y - 4, 3f, 2.5f, C(236, 200, 70)); Ball(c, x + 6, y - 4, 3f, 2.5f, C(236, 200, 70));               // eyes
            c.Set(x - 6, y - 4, C(30, 20, 20)); c.Set(x + 6, y - 4, C(30, 20, 20));
            for (int s = -10; s <= 10; s += 5) c.Set(x + s, y - 8, C(40, 96, 72));                                           // scales
        }

        static void Shrine(PixelCanvas c)
        {
            int l = 236, r = 404, wallTop = 64, wallBottom = 140;
            // Step under the shrine and the floor in front of the door.
            c.Rect(l - 6, wallBottom, r - l + 12, ShrineFront - wallBottom, C(120, 108, 92));
            for (int x = l - 6; x < r + 6; x++) c.Set(x, wallBottom, C(170, 158, 136));
            // Stucco walls with a red band and a jade line.
            for (int x = l; x < r; x++)
                for (int y = wallTop; y < wallBottom; y++)
                {
                    var col = y < wallTop + 12 ? C(150, 42, 34) : C(200, 186, 162);
                    if (y == wallTop + 12 || y == wallTop + 13) col = C(56, 130, 100);
                    float east = (x - l) / (float)(r - l);
                    col = Tone(col, 1.05f - 0.25f * east);
                    if (PixelCanvas.Hash(x, y, 71) < 0.04f) col = Tone(col, 0.88f);
                    c.Set(x, y, col);
                }
            // Painted glyphs on the walls: a sun on the left, a skull on the right.
            Glyph(c, l + 26, 104, true);
            Glyph(c, r - 26, 104, false);

            // The door: the open mouth of a serpent.
            int dl = 296, dr = 344, dt = 90;
            c.Rect(dl, dt, dr - dl, wallBottom - dt, C(10, 8, 12));
            for (int x = dl; x < dr; x++) { c.Set(x, dt - 1, C(56, 130, 100)); c.Set(x, dt - 2, C(70, 150, 112)); }
            for (int f = dl + 3; f < dr - 2; f += 6)
            {
                Poly(c, C(236, 230, 210), V(f - 2, dt), V(f + 2, dt), V(f, dt + 7));                  // upper fangs
                Poly(c, C(220, 214, 196), V(f - 1, wallBottom), V(f + 3, wallBottom), V(f + 1, wallBottom - 6));   // lower fangs
            }
            c.Rect(dl + 8, wallBottom, dr - dl - 16, ShrineFront - wallBottom, C(150, 38, 32));        // tongue out over the step
            Ball(c, dl - 4, dt - 6, 5f, 4f, C(236, 200, 70)); Ball(c, dr + 4, dt - 6, 5f, 4f, C(236, 200, 70));   // serpent eyes over the door
            c.Rect(dl - 5, dt - 7, 2, 2, C(20, 14, 14)); c.Rect(dr + 3, dt - 7, 2, 2, C(20, 14, 14));
            // Pillars painted as serpents.
            foreach (int px in new[] { dl - 10, dr + 2 })
                for (int y = dt; y < wallBottom; y++)
                    for (int x = px; x < px + 8; x++)
                        c.Set(x, y, ((y / 5) % 2 == 0) ? C(56, 130, 100) : C(160, 44, 36));

            // Roof: an overhanging lintel, merlons shaped like stepped clouds and a sun disc.
            c.Rect(l - 8, wallTop - 10, r - l + 16, 10, C(84, 64, 50));
            for (int x = l - 8; x < r + 8; x++) c.Set(x, wallTop - 10, C(124, 96, 72));
            for (int m = l - 4; m < r; m += 18)
            {
                c.Rect(m, wallTop - 22, 14, 12, C(196, 182, 156));
                c.Rect(m + 3, wallTop - 30, 8, 8, C(196, 182, 156));
                c.Set(m + 6, wallTop - 26, C(150, 42, 34)); c.Set(m + 7, wallTop - 26, C(150, 42, 34));
            }
            int sx = (l + r) / 2, sy = wallTop - 40;
            for (int a = 0; a < 16; a++)
            {
                float ang = a * Mathf.PI / 8f;
                c.Line(sx, sy, sx + Mathf.RoundToInt(Mathf.Cos(ang) * 18), sy + Mathf.RoundToInt(Mathf.Sin(ang) * 18), C(220, 170, 60));
            }
            Ball(c, sx, sy, 12f, 12f, C(230, 186, 70));
            c.Circle(sx, sy, 5f, C(150, 42, 34));
            c.Rect(sx - 2, sy - 2, 4, 4, C(240, 220, 140));
        }

        static void Glyph(PixelCanvas c, int x, int y, bool sun)
        {
            if (sun)
            {
                c.Circle(x, y, 9f, C(150, 42, 34));
                c.Circle(x, y, 6f, C(214, 170, 64));
                c.Circle(x, y, 2.5f, C(150, 42, 34));
            }
            else
            {
                Ball(c, x, y, 8f, 7f, C(226, 218, 196));
                c.Rect(x - 5, y - 2, 3, 3, C(30, 24, 22)); c.Rect(x + 2, y - 2, 3, 3, C(30, 24, 22));
                for (int t = -4; t <= 4; t += 2) c.Set(x + t, y + 5, C(60, 50, 44));
            }
        }

        /// <summary>Age: cracks, moss in the shade, vines hanging over the faces and a few fallen blocks.</summary>
        static void Weather(PixelCanvas c)
        {
            var random = new System.Random(5);
            for (int k = 0; k < 46; k++)   // cracks
            {
                int x = random.Next(10, PyramidW - 10), y = random.Next(60, PyramidH - 4);
                if (c.IsEmpty(x, y) || (x > StairLeft - 14 && x < StairRight + 14 && y > SummitFront)) continue;
                for (int s = 0; s < 6 + random.Next(8); s++)
                {
                    c.Set(x, y, Tone(c.Get(x, y), 0.6f));
                    x += random.Next(-1, 2); y += random.Next(0, 2);
                }
            }
            for (int x = 0; x < PyramidW; x++)   // moss, more on the shaded east side and near the ground
                for (int y = 40; y < PyramidH; y++)
                {
                    if (c.IsEmpty(x, y)) continue;
                    float chance = 0.02f + 0.1f * Mathf.Clamp01((y - 300) / 116f) + 0.06f * Mathf.Clamp01((x - 420) / 220f);
                    if (PixelCanvas.Hash(x / 2, y / 2, 88) < chance) c.Set(x, y, Mix(c.Get(x, y), C(54, 92, 40), 0.65f));
                }
            for (int v = 0; v < 22; v++)   // vines from the ledges
            {
                int x = random.Next(20, PyramidW - 20);
                if (x > StairLeft - 14 && x < StairRight + 14) continue;
                int tier = random.Next(0, 4);
                int y = TierFront(tier + 1);
                int length = 10 + random.Next(26);
                for (int s = 0; s < length && y < PyramidH; s++, y++)
                {
                    int wx = x + (int)(Mathf.Sin(s * 0.5f) * 1.5f);
                    if (c.IsEmpty(wx, y)) break;
                    c.Set(wx, y, s % 5 == 0 ? C(80, 130, 50) : C(40, 82, 32));
                    if (s % 5 == 0) c.Set(wx + 1, y, C(60, 110, 40));
                }
            }
        }
    }
}
