using UnityEditor;
using UnityEngine;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// Pixel art made with code inside Unity for the merchant's tent, the hooded merchant, the temple of
    /// Quetzalcóatl and the comic of the prologue. Saved in Art/Props and Art/Comic.
    /// Menu: TD_Game > Generate Props And Comic Art.
    /// </summary>
    public static partial class PropArt
    {
        public const string Props = "Assets/TD_Game/Art/Props";
        public const string Comic = "Assets/TD_Game/Art/Comic";
        static readonly Color32 Ink = PixelCanvas.C(16, 12, 14);
        static Color32 C(int r, int g, int b) => PixelCanvas.C(r, g, b);

        [MenuItem("TD_Game/Generate Props And Comic Art")]
        public static void GenerateAll()
        {
            MapArrow(); Quetzalcoatl(); OrbsAndPuzzle();
            TentBack(); TentFront(); Merchant(); SerpentHead(); Brazier(0); Brazier(1); TempleGate(true); TempleGate(false);
            TempleDecor();
            ComicHut(); ComicFace(); ComicAltar(); ComicThrones(); ComicGrave(); ComicFist(); ComicMictlantecuhtli(); ComicGift(); ComicJourney();
        }

        public static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Props}/{name}.png");
        public static Texture2D Picture(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{Comic}/{name}.png");
        static readonly Vector2 Bottom = new Vector2(0.5f, 0f);

        // ================= ORBS, PUZZLE AND TREASURE =================

        /// <summary>Gold coin (shiny, with a square hole like old coins) and bright gems for the orbs and the puzzle.</summary>
        static void OrbsAndPuzzle()
        {
            var coin = new PixelCanvas(11, 11);
            coin.Circle(5.5f, 5.5f, 5.3f, C(200, 150, 30));
            coin.Circle(5.5f, 5.5f, 4.2f, C(255, 210, 70));
            coin.Circle(4.2f, 4f, 1.6f, C(255, 250, 200));                 // shine
            coin.Rect(5, 5, 2, 2, C(150, 100, 20));                         // square hole
            coin.Outline(C(90, 60, 10));
            coin.Save($"{Props}/orb_coin.png", 16, new Vector2(0.5f, 0.5f));

            Gem("orb_jade", C(40, 220, 110), C(190, 255, 210));
            Gem("orb_tonalli", C(40, 200, 240), C(200, 245, 255));
            Gem("gem_red", C(220, 40, 40), C(255, 180, 170));
            Gem("gem_jade", C(40, 200, 110), C(190, 255, 210));
            Gem("gem_gold", C(240, 190, 40), C(255, 245, 190));
            Gem("gem_blue", C(50, 110, 230), C(190, 210, 255));

            // Stela: the order of the colours, top to bottom (red, gold, jade, blue).
            var stela = new PixelCanvas(14, 26, C(110, 104, 94));
            stela.Rect(0, 0, 14, 2, C(140, 132, 120));
            Color32[] order = { C(220, 40, 40), C(240, 190, 40), C(40, 200, 110), C(50, 110, 230) };
            for (int i = 0; i < 4; i++) { stela.Circle(7, 5 + i * 5.5f, 2.2f, order[i]); stela.Set(7, 4 + (int)(i * 5.5f), C(255, 255, 230)); }
            stela.Speckle(3, C(96, 90, 80), C(126, 120, 108), 0.08f);
            stela.Outline(Ink);
            stela.Save($"{Props}/stela.png", 16, new Vector2(0.5f, 0.04f));

            // Unlit brazier (cold stone bowl with ashes).
            var cold = new PixelCanvas(12, 18);
            cold.Rect(4, 12, 4, 6, C(96, 88, 78));
            cold.Ellipse(6, 11, 6, 2.5f, C(120, 112, 100));
            cold.Ellipse(6, 10, 4, 1.2f, C(60, 56, 54));
            cold.Outline(Ink);
            cold.Save($"{Props}/brazier_cold.png", 16, new Vector2(0.5f, 0f));

            // Treasure chest.
            var chest = new PixelCanvas(16, 13);
            chest.Rect(1, 4, 14, 9, C(120, 74, 36));
            chest.Rect(1, 1, 14, 4, C(150, 96, 46));
            chest.Rect(1, 4, 14, 1, C(200, 160, 50));
            chest.Rect(7, 4, 2, 4, C(230, 190, 70));
            chest.Rect(1, 1, 1, 12, C(200, 160, 50)); chest.Rect(14, 1, 1, 12, C(200, 160, 50));
            chest.Outline(Ink);
            chest.Save($"{Props}/chest.png", 16, new Vector2(0.5f, 0f));
        }

        static void Gem(string name, Color32 body, Color32 shine)
        {
            var c = new PixelCanvas(9, 11);
            for (int y = 0; y < 11; y++)   // a diamond shape
            {
                int half = y < 5 ? y : 10 - y;
                c.Rect(4 - half, y, half * 2 + 1, 1, body);
            }
            c.Set(3, 3, shine); c.Set(3, 4, shine); c.Set(4, 3, shine);
            c.Outline(C(255, 255, 255));   // white outline: it glows in the dark
            c.Save($"{Props}/{name}.png", 16, new Vector2(0.5f, 0.5f));
        }

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
    }
}
