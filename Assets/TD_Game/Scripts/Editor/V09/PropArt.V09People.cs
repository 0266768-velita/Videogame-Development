using UnityEngine;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// v0.9 characters and small icons, at the size of Tlahuelli (18 x 26 px, 16 PPU, feet at the bottom):
    /// the villagers of Mictlán (each with its own clothes and something in its hands), the xoloitzcuintle that guides
    /// souls across the river, Painal chained and free, the "!" / "..." signs over their heads and the icons
    /// of the full-screen map.
    /// </summary>
    public static partial class PropArt
    {
        static readonly Vector2 Feet = new Vector2(0.5f, 0f);

        public static void V09People()
        {
            SetLight(-0.55f, -0.6f, 0.58f);
            Villager("npc_boatwoman", skin: C(150, 100, 68), robe: C(112, 70, 44), trim: C(170, 50, 40), hair: C(200, 200, 196), hunched: true, extra: StaffAndBun);
            Villager("npc_eagle_ghost", skin: C(150, 100, 68), robe: C(120, 96, 60), trim: C(200, 170, 70), hair: C(30, 24, 22), extra: EagleHelmet, ghost: true);
            Villager("npc_herb_girl", skin: C(166, 112, 76), robe: C(220, 212, 196), trim: C(190, 60, 90), hair: C(26, 20, 22), small: true, extra: BraidsAndBasket);
            Villager("npc_owl_priest", skin: C(140, 92, 62), robe: C(40, 34, 44), trim: C(120, 40, 120), hair: C(26, 20, 22), extra: OwlHeaddress);
            Villager("npc_bone_collector", skin: C(150, 146, 136), robe: C(84, 70, 56), trim: C(140, 130, 110), hair: C(70, 66, 60), hunched: true, extra: BoneSack);
            Villager("npc_fisherman", skin: C(150, 100, 68), robe: C(70, 96, 110), trim: C(200, 190, 160), hair: C(26, 20, 22), extra: HatAndNet);
            Villager("npc_pilgrim", skin: C(150, 100, 68), robe: C(96, 86, 70), trim: C(60, 120, 100), hair: C(26, 20, 22), extra: HoodAndStaff);
            Villager("npc_widow", skin: C(156, 106, 72), robe: C(30, 26, 30), trim: C(200, 120, 30), hair: C(20, 16, 18), extra: VeilAndCandle);
            Xolo();
            Prisoner(true);
            Prisoner(false);
            Signs();
            MapIcons();
        }

        delegate void Extra(PixelCanvas c);

        static void Villager(string name, Color32 skin, Color32 robe, Color32 trim, Color32 hair, Extra extra,
            bool hunched = false, bool small = false, bool ghost = false)
        {
            var c = new PixelCanvas(18, 26);
            int top = small ? 3 : hunched ? 2 : 0;          // a child is shorter; a hunched back lowers the head
            int hx = hunched ? 8 : 9;
            // Legs and sandals.
            c.Rect(6, 22, 2, 3, Tone(skin, 0.8f)); c.Rect(10, 22, 2, 3, Tone(skin, 0.8f));
            c.Rect(5, 25, 3, 1, C(70, 46, 30)); c.Rect(10, 25, 3, 1, C(70, 46, 30));
            // Robe (huipil / tilmatli), shaded, with a band of colour.
            for (int y = 11 + top; y < 23; y++)
            {
                int half = 4 + (y - 11 - top) / 3;
                for (int x = 9 - half; x <= 9 + half; x++)
                {
                    var col = x < 9 - half / 2 ? Tone(robe, 1.12f) : x > 9 + half / 2 ? Tone(robe, 0.78f) : robe;
                    if (y == 20 || y == 21) col = trim;
                    if (y == 15 + top && (x + y) % 2 == 0) col = Tone(trim, 0.9f);
                    c.Set(x, y, col);
                }
            }
            // Arms.
            Tube(c, 5, 12 + top, 3.5f, 18 + top, 1.4f, 1.2f, Tone(robe, 1.05f));
            Tube(c, 13, 12 + top, 14.5f, 18 + top, 1.4f, 1.2f, Tone(robe, 0.85f));
            Ball(c, 3.5f, 19 + top, 1.3f, 1.3f, skin); Ball(c, 14.5f, 19 + top, 1.3f, 1.3f, skin);
            // Head.
            Ball(c, hx, 7 + top, 3.7f, 3.9f, skin);
            Ball(c, hx, 4.6f + top, 3.9f, 2.4f, hair);
            c.Set(hx - 1, 7 + top, C(20, 14, 14)); c.Set(hx + 2, 7 + top, C(20, 14, 14));
            c.Set(hx, 9 + top, Tone(skin, 0.7f));
            extra(c);
            if (ghost)
                for (int x = 0; x < c.W; x++)
                    for (int y = 0; y < c.H; y++)
                    {
                        if (c.IsEmpty(x, y)) continue;
                        var p = Mix(c.Get(x, y), C(150, 220, 230), 0.55f);
                        p.a = (byte)(y > 20 ? 110 : 190);   // its feet fade away
                        c.Set(x, y, p);
                    }
            c.Outline(ghost ? PixelCanvas.C(60, 120, 140, 160) : Ink);
            c.Save($"{Props}/{name}.png", 16, Feet);
        }

        static void StaffAndBun(PixelCanvas c)
        {
            Ball(c, 7, 2, 2f, 1.8f, C(200, 200, 196));
            c.Line(2, 9, 2, 25, C(110, 80, 50)); c.Set(2, 8, C(140, 100, 60));
        }

        static void EagleHelmet(PixelCanvas c)
        {
            Ball(c, 9, 4, 4.5f, 3.5f, C(120, 84, 50));
            Poly(c, C(230, 190, 70), V(9, 4), V(15, 6), V(10, 8));          // the beak over the face
            for (int f = 0; f < 5; f++) c.Line(5 + f * 2, 1, 4 + f * 2, -2, C(230, 230, 220));
            Ball(c, 15, 16, 3f, 4f, C(160, 50, 40)); c.Circle(15, 16, 1.4f, C(230, 200, 80));   // round shield
        }

        static void BraidsAndBasket(PixelCanvas c)
        {
            c.Line(5, 7, 5, 14, C(26, 20, 22)); c.Line(13, 7, 13, 14, C(26, 20, 22));
            c.Set(5, 15, C(190, 60, 90)); c.Set(13, 15, C(190, 60, 90));
            Ball(c, 14, 20, 3f, 2.4f, C(170, 130, 70));
            c.Set(13, 18, C(240, 140, 20)); c.Set(15, 18, C(220, 200, 60));   // flowers in the basket
        }

        static void OwlHeaddress(PixelCanvas c)
        {
            Ball(c, 9, 3, 5f, 3f, C(90, 72, 56));
            c.Circle(7, 3, 1.4f, C(240, 210, 90)); c.Circle(11, 3, 1.4f, C(240, 210, 90));
            c.Set(7, 3, C(20, 14, 14)); c.Set(11, 3, C(20, 14, 14));
            Poly(c, C(90, 72, 56), V(4, 2), V(5, -1), V(7, 1)); Poly(c, C(90, 72, 56), V(14, 2), V(13, -1), V(11, 1));
            Ball(c, 3, 20, 2f, 1.6f, C(120, 110, 100));                   // copal burner
            c.Set(3, 17, C(200, 200, 200)); c.Set(2, 15, C(180, 180, 180));
        }

        static void BoneSack(PixelCanvas c)
        {
            Ball(c, 14, 13, 4f, 5f, C(140, 120, 90));
            c.Rect(12, 9, 5, 1, C(220, 210, 190)); c.Set(16, 8, C(220, 210, 190));
        }

        static void HatAndNet(PixelCanvas c)
        {
            c.Rect(3, 4, 13, 2, C(200, 170, 100)); Ball(c, 9, 3, 3.5f, 2f, C(210, 180, 110));
            for (int y = 16; y < 24; y += 2) for (int x = 13; x < 18; x += 2) c.Set(x, y, C(200, 190, 160));
        }

        static void HoodAndStaff(PixelCanvas c)
        {
            Ball(c, 9, 5, 4.6f, 4f, C(86, 76, 62));
            c.Line(15, 6, 15, 25, C(110, 80, 50)); Ball(c, 15, 5, 1.5f, 1.5f, C(60, 120, 100));
        }

        static void VeilAndCandle(PixelCanvas c)
        {
            for (int y = 2; y < 16; y++) { c.Set(4 + y / 6, y, C(40, 34, 40)); c.Set(14 - y / 6, y, C(40, 34, 40)); }
            Ball(c, 9, 3, 4.4f, 2.4f, C(40, 34, 40));
            c.Rect(14, 15, 2, 4, C(230, 220, 190)); c.Set(14, 14, C(255, 200, 80)); c.Set(15, 13, C(255, 240, 160));
        }

        /// <summary>A xoloitzcuintle, the hairless dog that guides the souls across the river of Mictlán.</summary>
        static void Xolo()
        {
            var c = new PixelCanvas(20, 14);
            var dog = C(52, 48, 56);
            Tube(c, 5, 7, 14, 7, 3f, 3f, dog);                     // body
            for (int i = 0; i < 4; i++) c.Rect(5 + i * 3, 9, 2, 4, Tone(dog, 0.85f));   // legs
            Tube(c, 4, 6, 1, 3, 0.9f, 0.6f, dog);                  // tail up
            Ball(c, 16, 4, 3f, 2.6f, dog);                         // head
            Poly(c, dog, V(14, 2), V(15, -1), V(16, 2)); Poly(c, dog, V(17, 2), V(19, -1), V(19, 3));   // big ears
            c.Set(17, 4, C(240, 200, 80));                         // a gold eye that glows a little
            c.Rect(7, 5, 5, 1, C(150, 40, 34));                    // a red cloth
            c.Outline(Ink);
            c.Save($"{Props}/npc_xolo.png", 16, Feet);
        }

        /// <summary>Painal, the runner of the gods: hanging by the wrists from chains, or standing free.</summary>
        static void Prisoner(bool chained)
        {
            var c = new PixelCanvas(26, 36);
            var skin = C(150, 100, 68);
            int lift = chained ? 0 : 6;
            if (chained)
            {
                // Arms up to the shackles, body hanging, feet off the floor.
                Tube(c, 9, 13, 5, 3, 1.5f, 1.3f, skin); Tube(c, 17, 13, 21, 3, 1.5f, 1.3f, Tone(skin, 0.85f));
                c.Rect(3, 1, 5, 3, C(110, 110, 120)); c.Rect(19, 1, 5, 3, C(90, 90, 100));
            }
            else
            {
                Tube(c, 9, 14 + lift, 6, 22 + lift, 1.5f, 1.3f, skin); Tube(c, 17, 14 + lift, 20, 22 + lift, 1.5f, 1.3f, Tone(skin, 0.85f));
            }
            Ball(c, 13, 17 + lift, 4.5f, 6f, skin);                                           // chest
            c.Rect(9, 21 + lift, 9, 4, C(200, 190, 160)); c.Rect(12, 25 + lift, 3, 3, C(170, 50, 40));   // loincloth
            Tube(c, 11, 25 + lift, 10, 32 + (chained ? -2 : 0), 1.5f, 1.3f, skin);
            Tube(c, 15, 25 + lift, 16, 32 + (chained ? -2 : 0), 1.5f, 1.3f, Tone(skin, 0.85f));
            int feetY = chained ? 31 : 33;
            c.Rect(8, feetY, 4, 1, C(80, 140, 110)); c.Rect(14, feetY, 4, 1, C(80, 140, 110));  // feathered anklets
            Ball(c, 13, 8 + lift, 3.8f, 4f, skin);                                            // head
            Ball(c, 13, 5.5f + lift, 4f, 2.4f, C(26, 20, 22));
            c.Rect(9, 6 + lift, 9, 1, C(60, 140, 110));                                      // headband
            if (chained) { c.Set(12, 9, C(20, 14, 14)); c.Set(14, 9, C(20, 14, 14)); c.Rect(12, 11, 3, 1, Tone(skin, 0.6f)); c.Set(10, 18, C(120, 50, 60)); }
            else { c.Set(12, 8 + lift, C(20, 14, 14)); c.Set(14, 8 + lift, C(20, 14, 14)); c.Set(13, 10 + lift, C(120, 60, 50)); }
            c.Outline(Ink);
            c.Save($"{Props}/{(chained ? "prisoner_chained" : "prisoner_free")}.png", 16, Feet);
        }

        static void Signs()
        {
            var bubble = C(236, 228, 210);
            var news = new PixelCanvas(11, 13);
            news.Ellipse(5.5f, 5.5f, 5.3f, 5.3f, bubble);
            Poly(news, bubble, V(3, 9), V(6, 9), V(4, 12));
            news.Rect(5, 2, 2, 5, C(200, 140, 30)); news.Rect(5, 8, 2, 2, C(200, 140, 30));
            news.Outline(Ink);
            news.Save($"{Props}/sign_new.png", 16, new Vector2(0.5f, 0f));

            var talked = new PixelCanvas(11, 13);
            talked.Ellipse(5.5f, 5.5f, 5.3f, 5.3f, C(170, 164, 150));
            Poly(talked, C(170, 164, 150), V(3, 9), V(6, 9), V(4, 12));
            for (int i = 0; i < 3; i++) talked.Set(3 + i * 2, 6, C(60, 54, 50));
            talked.Outline(Ink);
            talked.Save($"{Props}/sign_talked.png", 16, new Vector2(0.5f, 0f));
        }

        static void MapIcons()
        {
            var enemy = new PixelCanvas(9, 9);
            for (int y = 0; y < 9; y++) { int h = y < 5 ? y : 8 - y; enemy.Rect(4 - h, y, h * 2 + 1, 1, C(220, 50, 40)); }
            enemy.Set(4, 3, C(255, 180, 160));
            enemy.Outline(C(30, 10, 10));
            enemy.Save($"{Props}/icon_enemy.png", 16, Mid);

            var boss = new PixelCanvas(15, 15);
            Ball(boss, 7.5f, 6.5f, 6.5f, 6f, C(230, 220, 200));
            boss.Rect(3, 5, 3, 3, C(180, 20, 20)); boss.Rect(9, 5, 3, 3, C(180, 20, 20));
            boss.Rect(4, 11, 7, 3, C(230, 220, 200));
            for (int t = 4; t < 11; t += 2) boss.Set(t, 12, C(40, 30, 30));
            boss.Outline(C(120, 10, 10));
            boss.Save($"{Props}/icon_boss.png", 16, Mid);

            var villager = new PixelCanvas(9, 11);
            villager.Circle(4.5f, 2.5f, 2.2f, C(236, 228, 210));
            Poly(villager, C(236, 228, 210), V(1, 11), V(4.5f, 4.5f), V(8, 11));
            villager.Outline(C(40, 34, 30));
            villager.Save($"{Props}/icon_villager.png", 16, Mid);

            var quest = new PixelCanvas(13, 15);
            quest.Ellipse(6.5f, 7.5f, 6.3f, 7.2f, C(150, 60, 190));
            quest.Rect(4, 3, 5, 2, C(250, 240, 255)); quest.Rect(8, 5, 2, 2, C(250, 240, 255));
            quest.Rect(6, 7, 2, 2, C(250, 240, 255)); quest.Rect(6, 11, 2, 2, C(250, 240, 255));
            quest.Outline(C(40, 10, 60));
            quest.Save($"{Props}/icon_quest.png", 16, Mid);

            var landmark = new PixelCanvas(15, 12);
            for (int t = 0; t < 4; t++) landmark.Rect(1 + t * 2, 9 - t * 3, 13 - t * 4, 3, Tone(C(220, 180, 80), 1f - t * 0.08f));
            landmark.Rect(6, 6, 3, 6, C(150, 110, 40));
            landmark.Outline(C(60, 40, 10));
            landmark.Save($"{Props}/icon_landmark.png", 16, Mid);

            var door = new PixelCanvas(9, 12);
            door.Rect(1, 2, 7, 10, C(150, 100, 60));
            door.Ellipse(4.5f, 2.5f, 3.5f, 2.5f, C(150, 100, 60));
            door.Rect(3, 4, 3, 8, C(40, 26, 20));
            door.Outline(C(30, 20, 14));
            door.Save($"{Props}/icon_door.png", 16, Mid);
        }
    }
}
