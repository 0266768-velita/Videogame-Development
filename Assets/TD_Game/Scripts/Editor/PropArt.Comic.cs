using UnityEngine;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// The comic of the prologue (GDD section 3): 144 x 96 pictures painted with light and shadow
    /// (see PropArt.Paint). The team can replace any picture with their own PNG of the same name.
    /// </summary>
    public static partial class PropArt
    {
        const int PW = 144, PH = 96;
        static readonly Color32 Hair = new Color32(26, 22, 30, 255);
        static readonly Color32 Cotton = new Color32(205, 195, 165, 255);
        static readonly Color32 Blue = new Color32(40, 90, 190, 255);
        static readonly Color32 Bone = new Color32(225, 218, 196, 255);
        static readonly Color32 JadeStone = new Color32(40, 150, 110, 255);
        static readonly Color32 OldGold = new Color32(200, 150, 50, 255);

        static void SaveComic(PixelCanvas c, string name) => c.Save($"{Comic}/{name}.png", 16, new Vector2(0.5f, 0.5f));

        /// <summary>"His mother was dying": night in the adobe house, lit by one candle.</summary>
        static void ComicHut()
        {
            var c = new PixelCanvas(PW, PH);
            SetLight(-0.8f, -0.2f, 0.55f);
            Sky(c, 0, 0, PW, 64, C(36, 22, 16), C(92, 58, 36), 5);   // adobe wall
            for (int y = 8; y < 64; y += 8)
            {
                c.Rect(0, y, PW, 1, C(56, 36, 24));
                for (int x = (y / 8) % 2 * 6; x < PW; x += 12) c.Rect(x, y - 7, 1, 7, C(60, 38, 26));
            }
            Tube(c, -4, 4, PW + 4, 4, 3.5f, 3.5f, C(90, 60, 36));   // roof beam
            Tube(c, 8, 0, 8, 64, 3.5f, 3.5f, C(84, 56, 34));
            // Window with the moon.
            Sky(c, 98, 12, 26, 20, C(8, 12, 36), C(30, 40, 84), 4);
            Ball(c, 116, 20, 4, 4, C(226, 224, 205));
            c.Rect(96, 10, 30, 2, C(80, 52, 30)); c.Rect(96, 32, 30, 2, C(80, 52, 30));
            c.Rect(96, 10, 2, 24, C(80, 52, 30)); c.Rect(124, 10, 2, 24, C(80, 52, 30)); c.Rect(110, 12, 1, 20, C(70, 46, 28));
            // Floor and reed mat.
            Sky(c, 0, 64, PW, 32, C(76, 52, 32), C(36, 24, 16), 4);
            Poly(c, C(170, 138, 76), V(30, 70), V(130, 70), V(138, 88), V(22, 88));
            for (int x = 18; x < 140; x += 3) c.Line(x, 70, x + 8, 88, C(140, 110, 58));
            // The mother under her rebozo, head on the right.
            Ball(c, 88, 77, 30, 7, C(150, 60, 50));
            for (int x = 62; x < 116; x += 6) c.Line(x, 71, x + 2, 83, C(118, 44, 38));
            Ball(c, 122, 74, 7, 5, Hair);
            Ball(c, 119, 75, 5.5f, 5f, C(150, 104, 80));
            c.Set(116, 74, C(40, 26, 20)); c.Set(117, 74, C(40, 26, 20));   // closed eyes
            Tube(c, 66, 80, 54, 83, 1.6f, 1.4f, C(150, 104, 80));            // her hand reaches him
            // Tlahuelli kneeling beside her.
            Figure(c, 40, 90, 36, Pose.Kneeling, Cotton, Blue);
            // Clay pots and the candle.
            Ball(c, 132, 64, 6, 6, C(150, 80, 48)); Ball(c, 140, 66, 4, 4, C(130, 70, 44));
            c.Rect(14, 66, 3, 8, C(232, 216, 172));
            Ball(c, 15.5f, 63, 1.6f, 2.6f, C(255, 214, 96)); c.Set(15, 63, C(255, 250, 220));
            Glow(c, 16, 63, 80, C(255, 168, 70), 0.6f);
            Glow(c, 112, 24, 36, C(120, 140, 230), 0.3f);
            Vignette(c, 1.3f);
            SaveComic(c, "hut");
        }

        /// <summary>"Tlahuelli could only watch over her": a close-up of his worried face with a tear.</summary>
        static void ComicFace()
        {
            var c = new PixelCanvas(PW, PH);
            SetLight(-0.8f, -0.3f, 0.5f);
            Sky(c, 0, 0, PW, PH, C(14, 10, 20), C(54, 30, 28), 6);
            Ball(c, 72, 108, 50, 22, Cotton);                          // shoulders
            Tube(c, 72, 70, 72, 98, 11, 12, Tone(Skin, 0.85f));        // neck
            Ball(c, 47, 50, 4, 7, Skin); Ball(c, 97, 50, 4, 7, Skin);  // ears
            Ball(c, 72, 48, 24, 30, Skin);                             // face
            Ball(c, 72, 21, 27, 13, Hair);
            c.Rect(45, 18, 4, 30, Hair); c.Rect(95, 18, 4, 30, Hair);
            c.Rect(46, 27, 52, 5, Blue); c.Rect(46, 27, 52, 1, Tone(Blue, 1.4f));   // headband
            Ball(c, 72, 29.5f, 3, 3, OldGold);
            Ball(c, 46, 59, 3, 3, JadeStone); Ball(c, 98, 59, 3, 3, JadeStone);    // jade ear spools
            // Worried brows (the inner ends go up).
            for (int t = 0; t < 2; t++) { c.Line(54, 39 + t, 66, 35 + t, Hair); c.Line(90, 39 + t, 78, 35 + t, Hair); }
            foreach (int ex in new[] { 61, 83 })
            {
                Ball(c, ex, 44, 6, 3.2f, C(232, 222, 206));
                Ball(c, ex + 0.5f, 44.5f, 2.6f, 2.8f, C(66, 38, 24));
                c.Set(ex, 44, C(10, 6, 6)); c.Set(ex - 1, 43, C(255, 255, 255));
                c.Line(ex - 6, 41, ex + 6, 41, C(40, 22, 18));             // eyelid
            }
            Tube(c, 72, 44, 73, 58, 2.5f, 4f, Skin);                       // nose
            c.Set(70, 60, C(70, 36, 28)); c.Set(75, 60, C(70, 36, 28));
            c.Line(62, 69, 66, 67, C(100, 46, 38)); c.Line(66, 67, 78, 67, C(100, 46, 38)); c.Line(78, 67, 82, 69, C(100, 46, 38));   // a sad mouth
            c.Line(66, 69, 78, 69, Tone(Skin, 1.15f));
            c.Rect(53, 50, 14, 2, Blue); c.Rect(77, 50, 14, 2, Blue);     // face paint
            c.Line(86, 48, 89, 63, C(150, 196, 240)); Ball(c, 89, 65, 1.6f, 2.2f, C(180, 220, 255));   // the tear
            Glow(c, 10, 60, 100, C(255, 160, 70), 0.45f);
            Glow(c, 140, 36, 56, C(90, 110, 210), 0.3f);
            Vignette(c, 1.6f);
            SaveComic(c, "face");
        }

        /// <summary>"He danced for the gods": a storm over the pyramid, a fire on the altar.</summary>
        static void ComicAltar()
        {
            var c = new PixelCanvas(PW, PH);
            SetLight(0.6f, -0.5f, 0.6f);
            Sky(c, 0, 0, PW, 72, C(6, 8, 22), C(44, 46, 74), 6);
            for (int i = 0; i < 9; i++) Ball(c, 8 + i * 17, 10 + (i % 3) * 5, 16, 7, C(32, 34, 52));   // clouds
            // Lightning.
            int lx = 30, ly = 0;
            var random = new System.Random(3);
            while (ly < 40) { int nx = lx + random.Next(-5, 6), ny = ly + 4 + random.Next(4); c.Line(lx, ly, nx, ny, C(230, 230, 255)); lx = nx; ly = ny; }
            Glow(c, 30, 10, 40, C(170, 180, 255), 0.35f);
            // The pyramid with its stairs and the temple on top.
            for (int i = 0; i < 5; i++)
            {
                int w = 84 - i * 14, y = 66 - i * 9;
                c.Rect(118 - w / 2, y, w, 9, C(26, 26, 40)); c.Rect(118 - w / 2, y, w, 1, C(50, 50, 70));
            }
            c.Rect(110, 12, 16, 9, C(40, 24, 30)); c.Rect(108, 10, 20, 2, C(90, 30, 26));
            c.Rect(114, 21, 8, 54, C(36, 36, 52));
            for (int y = 21; y < 75; y += 2) c.Rect(114, y, 8, 1, C(48, 48, 66));
            // Plaza, puddles and the altar with the fire.
            Sky(c, 0, 72, PW, 24, C(46, 46, 52), C(20, 20, 26), 4);
            Ball(c, 30, 88, 14, 2, C(70, 80, 110)); Ball(c, 96, 86, 10, 1.5f, C(70, 80, 110));
            c.Rect(66, 72, 26, 12, C(86, 80, 74)); c.Rect(66, 72, 26, 2, C(120, 112, 100));
            Ball(c, 79, 70, 8, 3, C(110, 104, 96));
            Ball(c, 79, 64, 5, 6, C(236, 110, 40)); Ball(c, 79, 66, 3, 4, C(255, 214, 96));
            Glow(c, 79, 64, 60, C(255, 150, 60), 0.55f);
            Figure(c, 44, 90, 46, Pose.ArmsUp, Cotton, Blue);
            c.Rain(4, C(150, 170, 220), 360);
            Vignette(c, 1.3f);
            SaveComic(c, "altar_rain");
        }

        /// <summary>"They did not answer": the hall of the gods, three empty thrones under shafts of light.</summary>
        static void ComicThrones()
        {
            var c = new PixelCanvas(PW, PH);
            SetLight(0f, -1f, 0.45f);
            Sky(c, 0, 0, PW, PH, C(70, 12, 10), C(12, 4, 6), 6);
            for (int x = 0; x < PW; x += 8)   // stepped greca on the back wall
            {
                c.Rect(x, 14, 4, 2, C(150, 100, 40)); c.Rect(x + 2, 12, 4, 2, C(150, 100, 40)); c.Rect(x + 4, 10, 4, 2, C(150, 100, 40));
            }
            foreach (int x in new[] { 12, 40, 104, 132 })
            {
                Tube(c, x, 6, x, 70, 6, 6, C(116, 42, 32));
                c.Rect(x - 8, 4, 16, 4, C(150, 120, 80)); c.Rect(x - 8, 68, 16, 4, C(150, 120, 80));
            }
            for (int i = 0; i < 3; i++)   // the dais
            {
                int w = 100 + i * 16, y = 66 + i * 8;
                c.Rect(72 - w / 2, y, w, 8, C(90, 70, 56)); c.Rect(72 - w / 2, y, w, 1, C(140, 112, 86));
            }
            foreach (int x in new[] { 44, 72, 100 })
            {
                c.Rect(x - 8, 36, 16, 30, C(130, 110, 80));
                Ball(c, x, 36, 8, 6, C(130, 110, 80));
                c.Rect(x - 10, 56, 20, 10, C(110, 92, 66));
                Ball(c, x, 40, 3.5f, 3.5f, OldGold);
                c.Rect(x - 7, 58, 14, 3, C(190, 150, 70));   // a jaguar pelt on the seat
                for (int s = 0; s < 4; s++) c.Set(x - 5 + s * 3, 59, C(40, 26, 18));
            }
            // Shafts of light falling on the empty seats.
            foreach (int x in new[] { 34, 62, 90 })
                for (int y = 0; y < 66; y++)
                    for (int w = 0; w < 14; w++) c.Blend(x + w + y / 4, y, C(255, 210, 160), 0.12f);
            var random = new System.Random(8);
            for (int i = 0; i < 40; i++) c.Set(random.Next(30, 120), random.Next(0, 66), C(255, 220, 180));   // dust
            c.Line(0, 0, 10, 10, C(120, 110, 110)); c.Line(0, 6, 8, 2, C(120, 110, 110));   // cobwebs
            c.Line(143, 0, 133, 10, C(120, 110, 110)); c.Line(143, 6, 135, 2, C(120, 110, 110));
            Vignette(c, 1.7f);
            SaveComic(c, "empty_thrones");
        }

        /// <summary>"That night, grief became something else": a fresh grave with marigolds under the moon.</summary>
        static void ComicGrave()
        {
            var c = new PixelCanvas(PW, PH);
            SetLight(0.7f, -0.5f, 0.5f);
            Sky(c, 0, 0, PW, 70, C(4, 6, 18), C(26, 32, 56), 6);
            Stars(c, 11, 60, 50);
            Ball(c, 114, 20, 10, 10, C(222, 222, 206)); Ball(c, 110, 17, 2, 2, C(196, 196, 184)); Ball(c, 118, 24, 1.5f, 1.5f, C(196, 196, 184));
            Glow(c, 114, 20, 40, C(170, 180, 230), 0.35f);
            for (int i = 0; i < 6; i++) Ball(c, i * 30, 70, 26, 12, C(16, 20, 30));   // hills
            Sky(c, 0, 66, PW, 30, C(22, 30, 24), C(10, 14, 12), 4);
            Ball(c, 92, 80, 28, 7, C(84, 60, 40));                               // the grave
            c.Rect(98, 52, 12, 24, C(116, 110, 100)); Ball(c, 104, 52, 6, 5, C(116, 110, 100));
            c.Rect(102, 58, 4, 4, C(70, 66, 60)); c.Rect(100, 64, 8, 1, C(70, 66, 60));   // carved glyph
            var random = new System.Random(5);
            for (int i = 0; i < 26; i++)                                         // cempasúchil
            {
                float a = i / 26f * Mathf.PI * 2f;
                int fx = 92 + Mathf.RoundToInt(Mathf.Cos(a) * 30), fy = 80 + Mathf.RoundToInt(Mathf.Sin(a) * 8);
                c.Line(fx, fy, fx, fy + 3, C(40, 90, 40));
                Ball(c, fx, fy, 1.8f, 1.6f, random.Next(3) == 0 ? C(250, 200, 50) : C(240, 130, 30));
            }
            foreach (int x in new[] { 70, 120 })
            {
                c.Rect(x, 74, 2, 5, C(232, 216, 172)); c.Set(x, 72, C(255, 214, 96)); c.Set(x, 73, C(255, 180, 70));
                Glow(c, x, 72, 18, C(255, 170, 70), 0.6f);
            }
            Figure(c, 42, 90, 40, Pose.Kneeling, Tone(Cotton, 0.8f), Blue);
            c.Rain(11, C(140, 160, 210), 260);
            Vignette(c, 1.4f);
            SaveComic(c, "grave");
        }

        /// <summary>"I will make them answer": Tlahuelli's raised fist against a red sky.</summary>
        static void ComicFist()
        {
            var c = new PixelCanvas(PW, PH);
            SetLight(-0.45f, -0.7f, 0.55f);
            Sky(c, 0, 0, PW, PH, C(26, 4, 6), C(160, 30, 18), 7);
            for (int x = 0; x < PW; x++)   // rays behind the fist
                for (int y = 0; y < PH; y++)
                {
                    float a = Mathf.Atan2(y - 44f, x - 72f);
                    if (Mathf.FloorToInt((a + Mathf.PI) / (Mathf.PI / 10f)) % 2 == 0) c.Blend(x, y, C(0, 0, 0), 0.22f);
                }
            // A temple far away and a lightning bolt.
            for (int i = 0; i < 4; i++) c.Rect(6 + i * 4, 70 + i * -6, 40 - i * 8, 30, C(22, 4, 6));
            int lx = 122, ly = 0;
            var random = new System.Random(12);
            while (ly < 50) { int nx = lx + random.Next(-4, 5), ny = ly + 3 + random.Next(4); c.Line(lx, ly, nx, ny, C(255, 210, 190)); lx = nx; ly = ny; }
            c.Rain(5, C(255, 130, 100), 160);

            // The hand is painted on its own layer so it gets a strong ink outline.
            var h = new PixelCanvas(PW, PH);
            Tube(h, 74, 110, 72, 70, 15, 13, Skin);                                                   // forearm
            for (int i = 0; i < 3; i++) h.Line(66 + i * 5, 104, 68 + i * 4, 82, Tone(Skin, 0.75f));   // tendons
            h.Rect(58, 65, 29, 8, OldGold);                                                           // gold cuff
            h.Rect(58, 65, 29, 1, Tone(OldGold, 1.3f)); h.Rect(58, 72, 29, 1, Tone(OldGold, 0.6f));
            h.Rect(80, 66, 7, 6, Tone(OldGold, 0.72f));
            for (int x = 61; x <= 83; x += 4) Ball(h, x, 69, 1.6f, 1.6f, JadeStone);
            Ball(h, 72, 50, 20, 15, Skin);                                                            // back of the hand
            float[] fingers = { 57.5f, 67.5f, 77.5f, 87f };
            foreach (float fx in fingers)
            {
                Tube(h, fx, 34, fx, 50, 5.2f, 4.8f, Skin);                                            // the curled finger, facing us
                Ball(h, fx, 33, 5.2f, 4f, Tone(Skin, 1.08f));                                         // knuckle
                h.Line(Mathf.RoundToInt(fx) - 3, 42, Mathf.RoundToInt(fx) + 3, 42, Tone(Skin, 0.55f)); // joint crease
                h.Rect(Mathf.RoundToInt(fx) - 2, 50, 4, 2, C(214, 156, 132));                         // nail tucked in
            }
            for (int i = 0; i < 3; i++)                                                               // deep gaps between fingers
            {
                int gx = Mathf.RoundToInt((fingers[i] + fingers[i + 1]) / 2f);
                h.Line(gx, 31, gx, 53, Ink); h.Line(gx + 1, 34, gx + 1, 52, Tone(Skin, 0.45f));
            }
            h.Line(52, 54, 92, 54, Tone(Skin, 0.5f));                                                 // under the fingers
            Tube(h, 50, 62, 77, 54, 6.5f, 5f, Tone(Skin, 1.04f));                                     // thumb across the fingers
            h.Line(48, 68, 76, 60, Ink);
            Ball(h, 78, 54, 3.4f, 2.8f, C(214, 156, 132));
            for (int i = 0; i < 3; i++) { h.Line(57 + i * 10, 46, 59 + i * 10, 38, Blue); h.Line(58 + i * 10, 46, 60 + i * 10, 38, Blue); }   // war paint
            h.Outline(Ink);
            Stamp(c, h);
            c.Line(68, 55, 69, 78, C(140, 8, 8)); Ball(c, 69, 80, 1.6f, 2.2f, C(170, 12, 12));        // blood
            c.Line(86, 53, 86, 63, C(140, 8, 8));
            Glow(c, 120, 70, 60, C(255, 70, 40), 0.35f);
            Glow(c, 72, 40, 46, C(255, 210, 160), 0.2f);
            Vignette(c, 1.4f);
            SaveComic(c, "fist");
        }

        /// <summary>Mictlantecuhtli speaks: the lord of the dead, lit from below by the green fire of Mictlán.</summary>
        static void ComicMictlantecuhtli()
        {
            var c = new PixelCanvas(PW, PH);
            SetLight(0f, 0.65f, 0.65f);
            Sky(c, 0, 0, PW, PH, C(2, 8, 6), C(12, 44, 30), 6);
            var random = new System.Random(2);
            for (int i = 0; i < 18; i++) Ball(c, random.Next(PW), random.Next(40, PH), random.Next(6, 14), random.Next(3, 6), C(18, 54, 38));   // smoke
            // The great headdress of feathers.
            for (int i = -7; i <= 7; i++)
            {
                float a = -Mathf.PI / 2f + i * 0.17f;
                Tube(c, 72, 34, 72 + Mathf.Cos(a) * 40, 34 + Mathf.Sin(a) * 34, 2.4f, 3.6f, i % 2 == 0 ? C(40, 130, 90) : C(190, 150, 60));
            }
            // Robe and bony shoulders.
            Poly(c, C(30, 18, 24), V(36, PH), V(52, 58), V(92, 58), V(108, PH));
            Poly(c, C(140, 30, 26), V(50, 62), V(94, 62), V(92, 66), V(52, 66));
            Ball(c, 54, 60, 7, 5, Bone); Ball(c, 90, 60, 7, 5, Bone);
            // Ribs and spine.
            Tube(c, 72, 58, 72, 92, 2, 2, Tone(Bone, 0.9f));
            for (int i = 0; i < 5; i++) { float y = 68 + i * 5; Tube(c, 60 + i, y, 71, y - 2, 1.4f, 1.4f, Bone); Tube(c, 84 - i, y, 73, y - 2, 1.4f, 1.4f, Bone); }
            // Necklace of eyes.
            for (int i = 0; i < 7; i++)
            {
                float a = Mathf.PI * (0.15f + i * 0.7f / 6f);
                float ex = 72 - Mathf.Cos(a) * 18, ey = 58 + Mathf.Sin(a) * 6;
                Ball(c, ex, ey, 2.2f, 2.2f, C(236, 230, 214)); c.Set(Mathf.RoundToInt(ex), Mathf.RoundToInt(ey), C(20, 60, 40));
            }
            // Arms: one raised, one pointing at us.
            Tube(c, 54, 60, 30, 46, 2, 1.6f, Bone); Tube(c, 30, 46, 24, 24, 1.6f, 1.4f, Bone);
            for (int f = 0; f < 4; f++) Tube(c, 24, 24, 18 + f * 4, 14, 0.9f, 0.7f, Bone);
            Tube(c, 90, 60, 112, 66, 2, 1.6f, Bone); Tube(c, 112, 66, 124, 56, 1.6f, 1.4f, Bone);
            Tube(c, 124, 56, 132, 48, 0.9f, 0.7f, Bone);
            // The skull.
            Ball(c, 72, 36, 15, 16, Bone);
            Ball(c, 72, 50, 10, 6, Tone(Bone, 0.92f));
            Ball(c, 65, 35, 4.4f, 4.6f, C(10, 8, 10)); Ball(c, 79, 35, 4.4f, 4.6f, C(10, 8, 10));
            Ball(c, 65, 35, 1.4f, 1.4f, C(150, 255, 190)); Ball(c, 79, 35, 1.4f, 1.4f, C(150, 255, 190));
            Poly(c, C(10, 8, 10), V(70, 44), V(74, 44), V(72, 40));
            c.Rect(62, 49, 20, 3, C(10, 8, 10));
            for (int x = 63; x < 82; x += 2) { c.Set(x, 49, Bone); c.Set(x, 51, Bone); }
            Ball(c, 55, 42, 3, 3, JadeStone); Ball(c, 89, 42, 3, 3, JadeStone);
            Glow(c, 65, 35, 12, C(120, 255, 170), 0.8f); Glow(c, 79, 35, 12, C(120, 255, 170), 0.8f);
            Glow(c, 72, PH, 90, C(60, 200, 120), 0.4f);
            Vignette(c, 1.4f);
            SaveComic(c, "mictlantecuhtli");
        }

        /// <summary>"Take this Macuahuitl": a skeletal hand offers the obsidian club.</summary>
        static void ComicGift()
        {
            var c = new PixelCanvas(PW, PH);
            SetLight(-0.3f, -0.8f, 0.5f);
            Sky(c, 0, 0, PW, PH, C(4, 14, 10), C(14, 40, 28), 6);
            var random = new System.Random(6);
            for (int i = 0; i < 14; i++) Ball(c, random.Next(PW), random.Next(PH), random.Next(8, 16), random.Next(3, 6), C(18, 50, 36));
            // The wooden blade, slightly tilted, with obsidian teeth on both edges.
            Vector2 a = V(14, 58), b = V(100, 40);
            Vector2 dir = (b - a).normalized, n = V(-dir.y, dir.x);
            Poly(c, C(112, 72, 40), a + n * 7, b + n * 9, b - n * 9, a - n * 7);
            for (int i = 0; i < 6; i++) c.Line(Mathf.RoundToInt(a.x + dir.x * i * 15), Mathf.RoundToInt(a.y + dir.y * i * 15) + 2, Mathf.RoundToInt(a.x + dir.x * (i * 15 + 10)), Mathf.RoundToInt(a.y + dir.y * (i * 15 + 10)) + 2, C(88, 54, 30));
            for (float t = 0.04f; t < 0.98f; t += 0.08f)
            {
                Vector2 p = Vector2.Lerp(a, b, t);
                float w = Mathf.Lerp(7, 9, t);
                foreach (float side in new[] { 1f, -1f })
                {
                    Vector2 root = p + n * w * side, tip = p + n * (w + 6f) * side;
                    Poly(c, C(28, 22, 40), root - dir * 3f, tip, root + dir * 3f);
                    c.Set(Mathf.RoundToInt(tip.x - dir.x), Mathf.RoundToInt(tip.y - n.y * side), C(170, 160, 220));
                }
            }
            // Handle and red tassel.
            Tube(c, b.x, b.y, 122, 32, 4, 3.4f, C(90, 56, 30));
            for (int i = 0; i < 5; i++) Tube(c, 124, 32, 126 + i, 46 + i * 2, 1, 0.8f, C(170, 36, 30));
            // The bony hand around the handle.
            Ball(c, 112, 34, 7, 6, Bone);
            for (int f = 0; f < 4; f++)
            {
                float fx = 106 + f * 4;
                Tube(c, fx, 28, fx - 2, 40, 1.4f, 1.2f, Bone);
                Ball(c, fx - 1, 34, 1.6f, 1.6f, Tone(Bone, 1.08f));
            }
            Tube(c, 118, 36, 132, 54, 3, 2.4f, Bone); Tube(c, 132, 54, 144, 64, 2.4f, 2.4f, Bone);   // wrist and arm
            for (float t = 0.05f; t < 1f; t += 0.11f)
            {
                Vector2 p = Vector2.Lerp(a, b, t);
                Glow(c, p.x, p.y, 14, C(150, 120, 255), 0.2f);
            }
            Glow(c, 60, 48, 70, C(70, 210, 140), 0.25f);
            Vignette(c, 1.5f);
            SaveComic(c, "gift");
        }

        /// <summary>"And so his journey began": Tlahuelli and a xoloitzcuintle walk towards the land of the dead.</summary>
        static void ComicJourney()
        {
            var c = new PixelCanvas(PW, PH);
            SetLight(0.3f, -0.6f, 0.4f);
            Sky(c, 0, 0, PW, 58, C(34, 12, 40), C(210, 96, 50), 7);
            Ball(c, 72, 56, 14, 14, C(255, 200, 110)); Glow(c, 72, 54, 60, C(255, 160, 80), 0.4f);
            // Three layers of mountains, the far ones paler.
            Color32[] layer = { C(120, 60, 70), C(70, 34, 50), C(32, 18, 30) };
            for (int l = 0; l < 3; l++)
                for (int x = 0; x < PW; x++)
                {
                    float h = 18 - l * 4 + Mathf.Sin(x * (0.06f + l * 0.03f) + l * 2f) * (6 + l * 2) + Mathf.Sin(x * 0.17f + l) * 2;
                    int top = Mathf.RoundToInt(58 - h + l * 6);
                    c.Rect(x, top, 1, PH - top, layer[l]);
                }
            Sky(c, 0, 70, PW, 26, C(40, 34, 30), C(18, 14, 14), 4);
            for (int y = 62; y < PH; y++)   // the path towards the horizon
            {
                int w = 2 + (y - 62) * 2;
                c.Rect(72 - w / 2, y, w, 1, Mix(C(110, 80, 56), C(60, 44, 34), (y - 62) / 34f));
            }
            Poly(c, C(40, 70, 110), V(0, 76), V(50, 78), V(50, 82), V(0, 84));      // the river Apanohuaya
            c.Line(4, 79, 40, 80, C(90, 130, 180));
            // Tlahuelli seen from behind, the macuahuitl on his back.
            Figure(c, 66, 92, 46, Pose.FromBehind, Cotton, Blue);
            Tube(c, 56, 78, 78, 50, 2.6f, 3f, C(100, 64, 36));
            for (float t = 0.1f; t < 1f; t += 0.15f) { var p = Vector2.Lerp(V(56, 78), V(78, 50), t); c.Set(Mathf.RoundToInt(p.x) + 3, Mathf.RoundToInt(p.y) + 2, C(28, 22, 40)); }
            // The xolo dog guide.
            Ball(c, 88, 86, 6, 3.5f, C(50, 40, 44)); Tube(c, 84, 88, 84, 92, 1, 1, C(50, 40, 44)); Tube(c, 92, 88, 92, 92, 1, 1, C(50, 40, 44));
            Ball(c, 94, 82, 3, 2.6f, C(56, 44, 48));
            Poly(c, C(56, 44, 48), V(92, 80), V(93, 75), V(95, 80)); Poly(c, C(56, 44, 48), V(95, 80), V(97, 75), V(98, 81));
            Tube(c, 82, 85, 78, 82, 0.8f, 0.6f, C(50, 40, 44));
            Vignette(c, 1.3f);
            SaveComic(c, "journey");
        }
    }
}
