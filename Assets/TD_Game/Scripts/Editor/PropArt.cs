using UnityEditor;
using UnityEngine;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// Pixel art made with code inside Unity for the merchant's tent, the hooded merchant, the temple of
    /// Quetzalcóatl and the comic of the prologue. Saved in Art/Props and Art/Comic.
    /// Menu: TD_Game > Generate Props And Comic Art.
    /// </summary>
    public static class PropArt
    {
        public const string Props = "Assets/TD_Game/Art/Props";
        public const string Comic = "Assets/TD_Game/Art/Comic";
        static readonly Color32 Ink = PixelCanvas.C(16, 12, 14);
        static Color32 C(int r, int g, int b) => PixelCanvas.C(r, g, b);

        [MenuItem("TD_Game/Generate Props And Comic Art")]
        public static void GenerateAll()
        {
            MapArrow(); Quetzalcoatl();
            TentBack(); TentFront(); Merchant(); SerpentHead(); Brazier(0); Brazier(1); TempleGate(true); TempleGate(false);
            ComicHut(); ComicAltar(); ComicThrones(); ComicGrave(); ComicFist(); ComicFace(); ComicMictlantecuhtli(); ComicJourney();
        }

        public static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Props}/{name}.png");
        public static Texture2D Picture(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{Comic}/{name}.png");
        static readonly Vector2 Bottom = new Vector2(0.5f, 0f);

        // ================= QUETZALCOATL (boss) =================

        /// <summary>
        /// The feathered serpent, darker and meaner than in Mictlán v5: obsidian-jade scales, black spikes on the back,
        /// a crown of sharp feathers, glowing red eyes, angry brows and long fangs. 4 frames: the body sways.
        /// </summary>
        public static Sprite[] Quetzalcoatl()
        {
            var frames = new Sprite[4];
            for (int f = 0; f < 4; f++) frames[f] = QuetzalFrame(f);
            return frames;
        }

        public static Sprite[] QuetzalcoatlFrames()
        {
            var frames = new Sprite[4];
            for (int f = 0; f < 4; f++) frames[f] = AssetDatabase.LoadAssetAtPath<Sprite>($"{Props}/quetzalcoatl_{f}.png");
            return frames;
        }

        static Sprite QuetzalFrame(int frame)
        {
            var c = new PixelCanvas(112, 96);
            float sway = Mathf.Sin(frame * Mathf.PI / 2f) * 2f;
            var scale = C(22, 74, 58); var scaleLight = C(34, 104, 80); var belly = C(120, 140, 96);
            var spot = C(160, 120, 40); var spike = C(20, 16, 26);

            // Body: circles along a coil, from the tail (bottom right) to the neck (top middle).
            Vector2[] path = { new Vector2(104, 86), new Vector2(70, 88), new Vector2(30, 86), new Vector2(10, 70), new Vector2(18, 52), new Vector2(42, 46), new Vector2(54, 36) };
            int steps = 60;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector2 p = Along(path, t);
                p.x += Mathf.Sin(t * 9f + frame * 1.5f) * sway;
                float r = Mathf.Lerp(3f, 12f, Mathf.SmoothStep(0f, 1f, t));
                c.Circle(p.x, p.y, r, scale);
                c.Circle(p.x - r * 0.2f, p.y - r * 0.35f, r * 0.55f, scaleLight);
                c.Ellipse(p.x, p.y + r * 0.55f, r * 0.7f, r * 0.3f, belly);
                if (i % 6 == 3) c.Circle(p.x, p.y - r * 0.2f, r * 0.3f, spot);
                if (i % 4 == 0 && i > 4)   // black spikes on the back
                    for (int k = 0; k < 4; k++) c.Rect((int)p.x - 1 + k / 2, (int)(p.y - r - k), 2 - k / 2, 1, spike);
            }

            // Crown of sharp feathers around the head (they flutter).
            Color32[] feathers = { C(30, 130, 90), C(170, 30, 30), C(200, 160, 50), C(20, 16, 26) };
            for (int i = 0; i < 11; i++)
            {
                float a = Mathf.Lerp(200f, 340f, i / 10f) * Mathf.Deg2Rad;
                float length = 20f + (i % 2) * 6f + (frame % 2 == i % 2 ? 2f : 0f);
                var tip = new Vector2(56 + Mathf.Cos(a) * length, 24 + Mathf.Sin(a) * length);
                c.Line(56, 24, (int)tip.x, (int)tip.y, feathers[i % feathers.Length]);
                c.Line(57, 24, (int)tip.x + 1, (int)tip.y, feathers[i % feathers.Length]);
                c.Circle(tip.x, tip.y, 1.6f, feathers[(i + 1) % feathers.Length]);
            }

            // Head.
            c.Ellipse(56, 28, 17, 12, scale);
            c.Ellipse(55, 25, 13, 7, scaleLight);
            c.Rect(42, 18, 7, 2, spike); c.Rect(63, 18, 7, 2, spike);          // angry brows
            c.Line(41, 17, 49, 21, spike); c.Line(71, 17, 63, 21, spike);
            foreach (int ex in new[] { 46, 66 })                              // glowing red eyes with a black slit
            {
                c.Ellipse(ex, 23, 4, 3, C(230, 40, 30));
                c.Ellipse(ex, 23, 2.5f, 1.8f, frame % 2 == 0 ? C(255, 140, 90) : C(255, 90, 60));
                c.Rect(ex, 21, 1, 5, C(10, 0, 0));
            }
            c.Rect(42, 31, 29, 9, C(70, 10, 14));                              // open jaws
            c.Rect(44, 33, 25, 5, C(120, 20, 24));
            foreach (int fx in new[] { 44, 50, 62, 68 })                       // long fangs
            { c.Rect(fx, 31, 2, 6, C(236, 228, 210)); c.Set(fx, 37, C(236, 228, 210)); }
            c.Rect(55, 37, 3, 6, C(190, 30, 50)); c.Set(54, 43, C(190, 30, 50)); c.Set(58, 43, C(190, 30, 50));   // forked tongue
            c.Set(51, 41, C(120, 200, 120)); c.Set(51, 42, C(120, 200, 120));   // poison drips

            c.Outline(Ink);
            return c.Save($"{Props}/quetzalcoatl_{frame}.png", 16, new Vector2(0.5f, 0.06f));
        }

        static Vector2 Along(Vector2[] points, float t)
        {
            float f = t * (points.Length - 1);
            int i = Mathf.Min(points.Length - 2, (int)f);
            float k = f - i;
            Vector2 p0 = points[Mathf.Max(0, i - 1)], p1 = points[i], p2 = points[i + 1], p3 = points[Mathf.Min(points.Length - 1, i + 2)];
            // Catmull-Rom curve: a smooth line through the points
            return 0.5f * ((2f * p1) + (-p0 + p2) * k + (2f * p0 - 5f * p1 + 4f * p2 - p3) * k * k + (-p0 + 3f * p1 - 3f * p2 + p3) * k * k * k);
        }

        /// <summary>Arrow of the minimap (points up; the Minimap turns it).</summary>
        static void MapArrow()
        {
            var c = new PixelCanvas(11, 11);
            for (int y = 0; y < 7; y++) c.Rect(5 - y / 2 - (y > 0 ? 1 : 0), y + 1, (y / 2 + (y > 0 ? 1 : 0)) * 2 + 1, 1, C(220, 50, 40));   // the head
            c.Rect(4, 8, 3, 2, C(220, 50, 40));                           // the tail
            c.Set(5, 2, C(255, 200, 120));
            c.Outline(C(255, 240, 220));
            c.Save($"{Props}/map_arrow.png", 16, new Vector2(0.5f, 0.5f));
        }

        // ================= MERCHANT TENT (back layer / merchant / front layer) =================

        static void TentBack()
        {
            var c = new PixelCanvas(80, 64);
            c.Rect(8, 14, 64, 40, C(64, 26, 28));                       // back cloth
            for (int x = 8; x < 72; x += 6) c.Rect(x, 14, 1, 40, C(52, 20, 22));   // folds
            for (int y = 4; y < 19; y++)                                  // striped canopy, wider at the bottom
            {
                int half = 28 + (y - 4) * 12 / 15;
                for (int x = 40 - half; x < 40 + half; x++)
                    c.Set(x, y, ((x - 40 + 64) / 6) % 2 == 0 ? C(150, 40, 34) : C(206, 186, 146));
            }
            for (int x = 0; x < 80; x += 8) c.Circle(x + 4, 19, 4, C(150, 40, 34));   // scalloped edge
            c.Rect(6, 6, 3, 58, C(96, 64, 38)); c.Rect(71, 6, 3, 58, C(96, 64, 38));   // poles
            c.Circle(40, 3, 3, C(200, 160, 60));                          // gold ornament on top
            foreach (int x in new[] { 18, 62 })                           // hanging lanterns
            {
                c.Line(x, 20, x, 24, C(40, 30, 24));
                c.Rect(x - 2, 25, 5, 6, C(60, 40, 26));
                c.Rect(x - 1, 26, 3, 4, C(250, 200, 90));
            }
            // shelves with goods on the back wall
            c.Rect(14, 32, 52, 2, C(96, 64, 38));
            foreach (int x in new[] { 18, 24, 30 }) { c.Rect(x, 27, 3, 5, C(170, 30, 36)); c.Set(x + 1, 26, C(210, 200, 180)); }
            c.Circle(42, 29, 3, C(220, 210, 190)); c.Set(41, 28, Ink); c.Set(43, 28, Ink);   // a skull
            c.Rect(52, 26, 2, 6, C(40, 120, 100)); c.Rect(57, 27, 4, 5, C(200, 160, 60));    // jade + gold
            c.Outline(Ink);
            c.Save($"{Props}/tent_back.png", 16, Bottom);
        }

        static void TentFront()
        {
            var c = new PixelCanvas(80, 64);
            c.Rect(14, 44, 52, 4, C(140, 98, 58));                        // counter top
            c.Rect(14, 48, 52, 10, C(96, 62, 36));                        // counter front
            for (int x = 14; x < 66; x += 7) c.Rect(x, 48, 1, 10, C(66, 42, 24));
            for (int x = 16; x < 64; x++) c.Set(x, 52, (x / 2) % 2 == 0 ? C(200, 160, 60) : C(130, 40, 34));   // greca
            foreach (int x in new[] { 20, 26 }) { c.Rect(x, 39, 4, 5, C(176, 30, 36)); c.Rect(x + 1, 37, 2, 2, C(210, 200, 180)); }  // potions
            c.Ellipse(46, 42, 6, 2.5f, C(222, 182, 60)); c.Ellipse(46, 41, 4, 1.5f, C(250, 220, 110));                               // gold
            c.Line(58, 30, 62, 44, C(110, 70, 40)); c.Set(57, 31, Ink); c.Set(59, 33, Ink); c.Set(60, 36, Ink);                       // macuahuitl
            c.Rect(0, 46, 12, 12, C(110, 76, 44)); c.Rect(2, 48, 8, 8, C(90, 60, 34)); c.Line(0, 46, 11, 57, C(70, 46, 26));            // crate
            c.Ellipse(74, 53, 6, 8, C(100, 66, 38)); c.Rect(68, 50, 12, 1, C(60, 60, 64)); c.Rect(68, 57, 12, 1, C(60, 60, 64));        // barrel
            for (int x = 22; x < 58; x++)                                                                                              // rug
                for (int y = 58; y < 64; y++)
                    c.Set(x, y, (x + y) % 6 < 3 ? C(120, 34, 30) : C(176, 140, 60));
            c.Outline(Ink);
            c.Save($"{Props}/tent_front.png", 16, Bottom);
        }

        /// <summary>A hooded merchant: a dark cloak, a shadow for a face and two glowing eyes.</summary>
        static void Merchant()
        {
            var c = new PixelCanvas(18, 26);
            c.Ellipse(9, 7, 6, 6, C(52, 34, 62));                         // hood
            c.Ellipse(9, 8, 4, 4, C(14, 10, 16));                         // shadow of the face
            c.Set(7, 8, C(250, 220, 90)); c.Set(11, 8, C(250, 220, 90));  // glowing eyes
            c.Rect(3, 12, 12, 13, C(58, 38, 68));                         // cloak
            c.Rect(2, 18, 14, 7, C(48, 30, 58));
            c.Line(4, 13, 13, 22, C(200, 160, 60));                       // gold bandolier
            c.Rect(1, 16, 2, 3, C(150, 104, 74)); c.Rect(15, 16, 2, 3, C(150, 104, 74));   // hands
            c.Outline(Ink);
            c.Save($"{Props}/merchant.png", 16, Bottom);
        }

        // ================= TEMPLE OF QUETZALCOATL =================

        static void SerpentHead()
        {
            var c = new PixelCanvas(24, 22);
            for (int i = 0; i < 6; i++) c.Ellipse(4 + i * 3, 5, 2, 5, i % 2 == 0 ? C(40, 140, 100) : C(30, 100, 80));   // jade feathers
            c.Ellipse(12, 13, 10, 7, C(120, 112, 100));                   // stone head
            c.Ellipse(11, 11, 7, 4, C(146, 138, 124));
            c.Rect(5, 17, 14, 3, C(30, 20, 20));                          // open mouth
            for (int x = 6; x < 18; x += 2) c.Set(x, 17, C(230, 220, 200));   // fangs
            c.Rect(10, 19, 4, 3, C(170, 40, 40));                         // tongue
            c.Circle(8, 11, 1.5f, C(200, 160, 60)); c.Circle(16, 11, 1.5f, C(200, 160, 60));   // gold eyes
            c.Outline(Ink);
            c.Save($"{Props}/serpent_head.png", 16, Bottom);
        }

        static void Brazier(int frame)
        {
            var c = new PixelCanvas(12, 18);
            c.Rect(4, 12, 4, 6, C(96, 88, 78));                           // stone foot
            c.Ellipse(6, 11, 6, 2.5f, C(120, 112, 100));                  // bowl
            float h = frame == 0 ? 6f : 7.5f;
            c.Ellipse(6, 10 - h / 2, 4, h / 2 + 1, C(230, 110, 40));       // flame
            c.Ellipse(6, 10 - h / 3, 2.5f, h / 3, C(250, 210, 90));
            c.Outline(Ink);
            c.Save($"{Props}/brazier_{frame}.png", 16, Bottom);
        }

        /// <summary>Obsidian gate of the temple (2 tiles tall). Open = only the frame.</summary>
        static void TempleGate(bool closed)
        {
            var c = new PixelCanvas(16, 34);
            c.Rect(0, 0, 16, 3, C(200, 160, 60)); c.Rect(0, 0, 3, 34, C(120, 110, 96)); c.Rect(13, 0, 3, 34, C(120, 110, 96));
            if (closed)
                for (int x = 4; x < 13; x += 3) c.Rect(x, 3, 2, 31, C(30, 24, 40));   // obsidian bars
            c.Set(8, 1, C(150, 40, 34));
            c.Outline(Ink);
            c.Save($"{Props}/temple_gate_{(closed ? "closed" : "open")}.png", 16, new Vector2(0.5f, 1f / 34f));
        }

        // ================= COMIC OF THE PROLOGUE (96 x 64 pictures) =================

        static void Person(PixelCanvas c, int x, int feet, Color32 cloth, bool armsUp = false, bool kneeling = false)
        {
            var skin = C(150, 100, 70);
            int top = kneeling ? feet - 11 : feet - 16;
            c.Circle(x, top + 2, 2.5f, skin);
            c.Rect(x - 3, top - 1, 7, 2, C(20, 20, 30));                  // hair
            c.Rect(x - 2, top + 5, 5, kneeling ? 4 : 7, cloth);
            if (kneeling) c.Rect(x - 2, feet - 3, 6, 3, cloth);
            else { c.Rect(x - 2, top + 12, 2, 4, skin); c.Rect(x + 1, top + 12, 2, 4, skin); }
            if (armsUp) { c.Line(x - 3, top + 6, x - 6, top, skin); c.Line(x + 3, top + 6, x + 6, top, skin); }
            else { c.Line(x - 3, top + 6, x - 4, top + 10, skin); c.Line(x + 3, top + 6, x + 4, top + 10, skin); }
        }

        static void SaveComic(PixelCanvas c, string name) => c.Save($"{Comic}/{name}.png", 16, new Vector2(0.5f, 0.5f));

        static void ComicHut()
        {
            var c = new PixelCanvas(96, 64, C(60, 40, 28));
            c.Gradient(0, 0, 96, 40, C(70, 46, 30), C(96, 66, 42));       // adobe walls
            c.Rect(0, 40, 96, 24, C(84, 62, 40));                         // floor
            c.Rect(60, 10, 14, 12, C(30, 30, 52)); c.Circle(67, 14, 2, C(200, 200, 210));   // window with the moon
            c.Rect(14, 44, 46, 8, C(160, 130, 70));                       // reed mat
            for (int x = 14; x < 60; x += 3) c.Rect(x, 44, 1, 8, C(130, 104, 56));
            c.Ellipse(36, 46, 14, 3.5f, C(150, 140, 120));               // the mother under a blanket
            c.Circle(20, 45, 3, C(140, 96, 70)); c.Rect(17, 42, 6, 2, C(30, 30, 40));   // her face and hair
            Person(c, 50, 54, C(40, 70, 150), kneeling: true);            // Tlahuelli kneeling beside her
            c.Rect(80, 40, 2, 6, C(220, 200, 160)); c.Circle(81, 38, 1.5f, C(250, 200, 90));   // candle
            SaveComic(c, "hut");
        }

        static void ComicAltar()
        {
            var c = new PixelCanvas(96, 64);
            c.Gradient(0, 0, 96, 64, C(10, 12, 30), C(36, 40, 70));
            for (int i = 0; i < 4; i++) c.Rect(56 + i * 4, 18 + i * 8, 40 - i * 8, 8, C(24, 24, 40));   // pyramid silhouette
            c.Rect(0, 52, 96, 12, C(28, 30, 34));
            c.Rect(36, 44, 22, 8, C(90, 84, 78)); c.Rect(38, 42, 18, 2, C(110, 104, 96));               // stone altar
            Person(c, 26, 56, C(40, 70, 150), armsUp: true);                                            // dancing, praying
            c.Rain(4, C(150, 170, 220), 140);
            SaveComic(c, "altar_rain");
        }

        static void ComicThrones()
        {
            var c = new PixelCanvas(96, 64);
            c.Gradient(0, 0, 96, 64, C(90, 16, 14), C(20, 6, 8));
            for (int x = 6; x < 96; x += 22) c.Rect(x, 6, 6, 58, C(60, 20, 18));                       // columns
            foreach (int x in new[] { 22, 48, 74 })                                                     // empty thrones, turned away
            {
                c.Rect(x - 7, 26, 14, 20, C(120, 104, 80)); c.Rect(x - 8, 44, 16, 6, C(100, 86, 66));
                c.Rect(x - 5, 22, 10, 4, C(200, 160, 60));
            }
            SaveComic(c, "empty_thrones");
        }

        static void ComicGrave()
        {
            var c = new PixelCanvas(96, 64);
            c.Gradient(0, 0, 96, 64, C(6, 8, 18), C(24, 28, 40));
            c.Circle(78, 12, 5, C(170, 170, 180));
            c.Rect(0, 48, 96, 16, C(26, 32, 26));
            c.Ellipse(50, 50, 16, 4, C(70, 52, 36));                      // fresh grave
            c.Rect(60, 34, 6, 14, C(110, 104, 96)); c.Rect(59, 33, 8, 2, C(130, 124, 114));   // stone marker
            Person(c, 32, 54, C(30, 50, 110), kneeling: true);
            c.Rain(11, C(140, 160, 210), 160);
            SaveComic(c, "grave");
        }

        static void ComicFist()
        {
            var c = new PixelCanvas(96, 64);
            c.Gradient(0, 0, 96, 64, C(120, 20, 16), C(30, 6, 6));
            c.Ellipse(48, 34, 18, 16, C(150, 100, 70));                   // fist
            for (int i = 0; i < 4; i++) c.Rect(34 + i * 8, 22, 7, 10, C(164, 112, 80));   // fingers
            for (int i = 0; i < 4; i++) c.Rect(34 + i * 8, 31, 7, 1, C(110, 70, 50));
            c.Rect(36, 48, 24, 16, C(40, 70, 150));                       // sleeve
            c.Outline(Ink);
            SaveComic(c, "fist");
        }

        static void ComicFace()
        {
            var c = new PixelCanvas(96, 64);
            c.Gradient(0, 0, 96, 64, C(30, 26, 40), C(70, 50, 50));
            c.Ellipse(48, 36, 18, 22, C(150, 100, 70));                   // face
            c.Rect(28, 12, 40, 10, C(20, 20, 30));                        // hair
            c.Rect(30, 20, 36, 4, C(40, 80, 170));                        // blue headband
            c.Rect(36, 32, 8, 3, C(30, 30, 50)); c.Rect(52, 32, 8, 3, C(30, 30, 50));   // worried eyes
            c.Line(34, 28, 44, 30, C(40, 30, 30)); c.Line(62, 28, 52, 30, C(40, 30, 30));
            c.Rect(36, 36, 6, 2, C(60, 120, 200)); c.Rect(54, 36, 6, 2, C(60, 120, 200));   // face paint
            c.Rect(42, 48, 12, 2, C(90, 50, 40));                         // mouth
            c.Outline(Ink);
            SaveComic(c, "face");
        }

        static void ComicMictlantecuhtli()
        {
            var c = new PixelCanvas(96, 64);
            c.Gradient(0, 0, 96, 64, C(4, 10, 8), C(16, 30, 24));
            for (int i = 0; i < 7; i++) c.Ellipse(36 + i * 4, 10, 2, 8, i % 2 == 0 ? C(40, 120, 90) : C(180, 150, 60));   // headdress
            c.Circle(48, 22, 9, C(220, 214, 196));                        // skull
            c.Circle(44, 21, 2.5f, Ink); c.Circle(52, 21, 2.5f, Ink);
            c.Set(44, 21, C(120, 255, 180)); c.Set(52, 21, C(120, 255, 180));   // glowing eyes
            c.Rect(44, 27, 8, 2, Ink); for (int x = 44; x < 52; x += 2) c.Set(x, 27, C(220, 214, 196));
            for (int y = 34; y < 50; y += 3) c.Rect(40, y, 16, 1, C(200, 194, 176));   // ribs
            c.Rect(47, 32, 2, 22, C(200, 194, 176));                      // spine
            c.Line(56, 36, 72, 30, C(200, 194, 176)); c.Line(40, 36, 28, 46, C(200, 194, 176));   // arms
            c.Rect(36, 50, 24, 14, C(30, 20, 24));                        // dark robe
            SaveComic(c, "mictlantecuhtli");
        }

        static void ComicJourney()
        {
            var c = new PixelCanvas(96, 64);
            c.Gradient(0, 0, 96, 40, C(60, 20, 40), C(160, 70, 50));      // red dusk
            for (int i = 0; i < 5; i++) c.Ellipse(10 + i * 20, 40, 16, 12, C(30, 20, 34));   // dark mountains
            c.Rect(0, 40, 96, 24, C(30, 40, 30));
            for (int y = 40; y < 64; y++) { int w = 2 + (y - 40) / 2; c.Rect(48 - w / 2 + (y - 40) / 4, y, w, 1, C(80, 60, 40)); }   // path into the distance
            c.Rect(0, 50, 30, 3, C(30, 60, 100));                         // river
            Person(c, 54, 62, C(40, 70, 150));
            SaveComic(c, "journey");
        }
    }
}
