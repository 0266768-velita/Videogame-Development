using UnityEngine;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// Decoration inside the Celestial Temple (16 PPU, pivot at the feet): carved serpent columns,
    /// a stone idol of Coatlicue, a tzompantli (skull rack), a chacmool altar, hanging banners and offerings.
    /// </summary>
    public static partial class PropArt
    {
        static readonly Color32 Stone = new Color32(124, 116, 104, 255);

        static void TempleDecor()
        {
            SetLight(-0.55f, -0.6f, 0.58f);
            TempleColumn(); TempleIdol(); Tzompantli(); Chacmool(); TempleBanner(); Offering();
        }

        /// <summary>A column with a feathered serpent coiled around it (1 x 3.5 tiles).</summary>
        static void TempleColumn()
        {
            var c = new PixelCanvas(18, 58);
            c.Rect(1, 52, 16, 6, Tone(Stone, 0.8f)); c.Rect(1, 52, 16, 1, Tone(Stone, 1.15f));   // base
            Tube(c, 9, 8, 9, 52, 6f, 6f, Stone);
            c.Rect(0, 2, 18, 6, Tone(Stone, 0.9f)); c.Rect(0, 2, 18, 1, Tone(Stone, 1.2f));       // capital
            for (int x = 1; x < 17; x += 4) c.Rect(x, 4, 2, 2, OldGold);
            for (int y = 10; y < 50; y += 8)                                                       // the coiled serpent
            {
                Tube(c, 3, y, 15, y + 5, 2f, 2f, JadeStone);
                for (int x = 4; x < 15; x += 3) c.Set(x, y + (x - 3) * 5 / 12, Tone(JadeStone, 1.35f));
            }
            Ball(c, 9, 52, 4, 3, JadeStone);                                                       // its head at the base
            c.Set(8, 51, OldGold); c.Set(10, 51, OldGold);
            c.Outline(Ink);
            c.Save($"{Props}/temple_column.png", 16, new Vector2(0.5f, 0f));
        }

        /// <summary>Coatlicue: skirt of serpents, a necklace of hands and hearts and two serpent heads for a face.</summary>
        static void TempleIdol()
        {
            var c = new PixelCanvas(30, 46);
            c.Rect(3, 40, 24, 6, Tone(Stone, 0.75f)); c.Rect(3, 40, 24, 1, Tone(Stone, 1.1f));     // pedestal
            Tube(c, 15, 24, 15, 40, 10f, 11f, Tone(Stone, 0.95f));                                  // skirt
            for (int y = 26; y < 40; y += 3)
                for (int x = 6; x < 25; x += 4) Ball(c, x + (y / 3 % 2) * 2, y, 2f, 1.3f, Tone(JadeStone, 0.75f));   // woven serpents
            Ball(c, 15, 18, 10, 8, Stone);                                                         // torso
            for (int i = 0; i < 5; i++) Ball(c, 8 + i * 3.5f, 20 + Mathf.Abs(i - 2), 1.6f, 1.6f, i % 2 == 0 ? C(150, 40, 34) : Tone(Bone, 0.9f));   // hearts and hands
            Ball(c, 15, 25, 3, 3, Tone(Bone, 0.95f)); c.Set(14, 25, Ink); c.Set(16, 25, Ink);       // skull buckle
            Tube(c, 5, 16, 3, 26, 2.4f, 2f, Stone); Tube(c, 25, 16, 27, 26, 2.4f, 2f, Stone);       // arms
            Ball(c, 2.5f, 27, 2.4f, 2.4f, Tone(Stone, 1.05f)); Ball(c, 27.5f, 27, 2.4f, 2.4f, Tone(Stone, 1.05f));
            // Two serpent heads facing each other make her face.
            Ball(c, 11, 7, 5, 5, Stone); Ball(c, 19, 7, 5, 5, Stone);
            c.Set(9, 5, OldGold); c.Set(21, 5, OldGold);
            c.Rect(13, 9, 4, 2, Ink); c.Set(14, 11, Bone); c.Set(15, 11, Bone);                    // fangs
            c.Rect(14, 11, 2, 3, C(150, 40, 34));                                                   // forked tongue
            c.Speckle(21, Tone(Stone, 0.7f), Tone(Stone, 1.15f), 0.05f);
            c.Outline(Ink);
            c.Save($"{Props}/temple_idol.png", 16, new Vector2(0.5f, 0f));
        }

        /// <summary>A tzompantli: a wooden rack with rows of skulls.</summary>
        static void Tzompantli()
        {
            var c = new PixelCanvas(42, 32);
            var wood = C(96, 62, 36);
            Tube(c, 3, 2, 3, 31, 1.6f, 1.6f, wood); Tube(c, 38, 2, 38, 31, 1.6f, 1.6f, wood);
            for (int row = 0; row < 3; row++)
            {
                int y = 6 + row * 9;
                Tube(c, 1, y, 41, y, 1f, 1f, Tone(wood, 1.1f));
                for (int x = 7; x < 37; x += 6)
                {
                    Ball(c, x, y + 3, 2.6f, 2.8f, Bone);
                    c.Set(x - 1, y + 3, Ink); c.Set(x + 1, y + 3, Ink); c.Set(x, y + 5, Tone(Bone, 0.6f));
                }
            }
            c.Outline(Ink);
            c.Save($"{Props}/tzompantli.png", 16, new Vector2(0.5f, 0f));
        }

        /// <summary>A chacmool: a reclining stone figure holding an offering bowl on its belly.</summary>
        static void Chacmool()
        {
            var c = new PixelCanvas(32, 20);
            c.Rect(1, 15, 30, 5, Tone(Stone, 0.75f)); c.Rect(1, 15, 30, 1, Tone(Stone, 1.1f));
            Tube(c, 6, 12, 24, 12, 3.5f, 3f, Stone);            // body
            Tube(c, 24, 12, 28, 6, 2.6f, 2.2f, Stone);          // raised knees
            Tube(c, 28, 6, 29, 14, 2f, 1.8f, Stone);
            Ball(c, 6, 7, 4, 4, Stone);                          // head turned to us
            c.Set(5, 7, Ink); c.Set(7, 7, Ink); c.Rect(5, 9, 3, 1, Tone(Stone, 0.6f));
            Ball(c, 15, 8, 4.5f, 2f, Tone(Stone, 0.85f));        // the bowl
            Ball(c, 15, 7.5f, 3.5f, 1.2f, C(150, 30, 26));       // offering
            c.Outline(Ink);
            c.Save($"{Props}/chacmool.png", 16, new Vector2(0.5f, 0f));
        }

        /// <summary>A long banner of red cotton with gold steps and a sun glyph.</summary>
        static void TempleBanner()
        {
            var c = new PixelCanvas(12, 34);
            Tube(c, 0, 1, 12, 1, 1f, 1f, C(96, 62, 36));
            Poly(c, C(140, 30, 26), V(1, 2), V(11, 2), V(11, 31), V(6, 27), V(1, 31));
            for (int y = 4; y < 26; y += 6) { c.Rect(2, y, 8, 1, OldGold); c.Rect(3, y + 1, 1, 1, OldGold); c.Rect(8, y + 1, 1, 1, OldGold); }
            Ball(c, 6, 14, 2.6f, 2.6f, OldGold); c.Set(6, 14, C(140, 30, 26));
            for (int y = 2; y < 30; y++) c.Set(1, y, Tone(C(140, 30, 26), 1.25f));
            c.Outline(Ink);
            c.Save($"{Props}/temple_banner.png", 16, new Vector2(0.5f, 0f));
        }

        /// <summary>Offerings on the floor: a clay bowl with copal smoke, corn and cacao.</summary>
        static void Offering()
        {
            var c = new PixelCanvas(18, 12);
            Ball(c, 9, 9, 7, 3, C(150, 80, 48));
            Ball(c, 9, 7, 5, 1.4f, C(60, 40, 30));
            Ball(c, 4, 9, 2.4f, 1.2f, C(230, 190, 60)); Ball(c, 14, 9, 2.2f, 1.2f, C(110, 60, 36));
            Ball(c, 9, 3, 2, 2, PixelCanvas.C(200, 200, 200, 180)); Ball(c, 10, 0.5f, 1.5f, 1.5f, PixelCanvas.C(200, 200, 200, 120));
            c.Outline(Ink);
            c.Save($"{Props}/offering.png", 16, new Vector2(0.5f, 0f));
        }
    }
}
