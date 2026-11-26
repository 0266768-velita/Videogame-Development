using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace TD_Game.EditorTools
{
    /// <summary>v0.9 upgrade: enemy camps, merchants, villagers, the witches' house, the player, the UI and the audio.</summary>
    public static partial class V09WorldUpgrade
    {
        const string Characters = "Assets/TD_Game/Prefabs/Characters";
        const string Wolf = "Enemy_Wolf", Witch = "Enemy_Witch", Giant = "Enemy_Giant";

        // ---------------- Enemy camps ----------------

        /// <summary>
        /// 20 encounters spread over the whole map so there is always something close: some lone hunters, and many camps of
        /// two or three mixed enemies (a witch heals and curses while a wolf or a giant hits). Each camp shares an encounter
        /// id, so the whole camp joins the battle. The farther from the start, the stronger.
        /// </summary>
        static void PlaceEnemies()
        {
            var root = GameObject.Find("Enemies").transform;
            for (int i = root.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.GetChild(i).gameObject);

            var camps = new (Vector2Int cell, string[] who)[]
            {
                (new Vector2Int(9, 9), new[] { Wolf }),                          // first fight, close to the start
                (new Vector2Int(24, 11), new[] { Wolf, Wolf }),
                (new Vector2Int(42, 10), new[] { Wolf, Witch }),
                (new Vector2Int(12, 27), new[] { Witch }),
                (new Vector2Int(33, 31), new[] { Wolf, Wolf, Witch }),
                (new Vector2Int(50, 31), new[] { Witch, Wolf }),
                (new Vector2Int(13, 51), new[] { Witch, Wolf }),
                (new Vector2Int(28, 47), new[] { Witch, Witch }),
                (new Vector2Int(40, 58), new[] { Wolf, Witch, Witch }),
                (new Vector2Int(54, 50), new[] { Giant }),                       // the first giant fights alone
                (new Vector2Int(75, 23), new[] { Giant, Wolf }),                 // guards of the pyramid
                (new Vector2Int(90, 23), new[] { Witch, Giant }),
                (new Vector2Int(90, 32), new[] { Witch, Wolf }),
                (new Vector2Int(86, 48), new[] { Giant, Witch, Wolf }),
                (new Vector2Int(100, 40), new[] { Giant }),
                (new Vector2Int(106, 55), new[] { Giant, Witch }),
                (new Vector2Int(120, 49), new[] { Wolf, Wolf, Wolf }),           // a pack
                (new Vector2Int(138, 57), new[] { Giant, Witch, Wolf }),
                (new Vector2Int(146, 52), new[] { Wolf }),                       // ambush in the east corridor
                (new Vector2Int(80, 63), new[] { Witch, Wolf }),
            };

            var start = map.Find('S');
            var distance = map.Distances(start, Walkable);
            var steps = camps.Select(camp => Steps(camp.cell, distance)).ToList();
            int maxSteps = Mathf.Max(1, steps.Max());
            Vector2[] offsets = { Vector2.zero, new Vector2(1.6f, 0.9f), new Vector2(-1.4f, 1.1f) };
            int n = 0;
            for (int i = 0; i < camps.Length; i++)
            {
                var (cell, who) = camps[i];
                float radius = who.Length == 1 ? 1f : 2.2f;
                var spot = FindSpot(World(cell), radius, g => g == K.Grass || g == K.Sand || g == K.Dirt || g == K.PlazaEdge, 16f);
                if (spot == null) { Debug.LogWarning("[TD_Game] No room for camp " + cell); continue; }
                occupied.Add((spot.Value, radius + 1.5f));
                // Strength grows with the distance from the start; camps are a little weaker per enemy.
                // (v0.9b: harder — every fight hits and lasts more, especially far from the start.)
                float strength = Mathf.Lerp(1.35f, 2.4f, steps[i] / (float)maxSteps) * (who.Length == 1 ? 1f : who.Length == 2 ? 0.95f : 0.88f);
                string encounter = who.Length > 1 ? $"camp_{i + 1}" : "";
                for (int k = 0; k < who.Length; k++)
                    Spawn(who[k], spot.Value + offsets[k], root, $"{who[k]}_{++n}", strength, encounter, optional: false);
            }
        }

        static int Steps(Vector2Int cell, int[,] distance)
        {
            int best = int.MaxValue;
            for (int dx = -3; dx <= 3; dx++)
                for (int dy = -3; dy <= 3; dy++)
                {
                    var c = new Vector2Int(cell.x + dx, cell.y + dy);
                    if (Inside(c) && distance[c.x, c.y] >= 0) best = Mathf.Min(best, distance[c.x, c.y]);
                }
            return best == int.MaxValue ? 0 : best;
        }

        static CombatEnemy Spawn(string prefabName, Vector2 position, Transform parent, string name, float strength, string encounter, bool optional, EnemyData data = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Characters}/{prefabName}.prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.position = position;
            var enemy = go.GetComponent<CombatEnemy>();
            var so = new SerializedObject(enemy);
            so.FindProperty("strength").floatValue = strength;
            so.FindProperty("encounter").stringValue = encounter;
            so.FindProperty("optional").boolValue = optional;
            if (data != null) so.FindProperty("data").objectReferenceValue = data;
            so.ApplyModifiedPropertiesWithoutUndo();
            return enemy;
        }

        // ---------------- Merchants ----------------

        static readonly (string id, string text)[] Greet =
        {
            ("merchant_greet_1", "What do you want, stranger?"), ("merchant_greet_2", "Ahh... a living soul. Come closer, stranger."),
            ("merchant_greet_3", "Got some fine things for sale, warrior. Take a look."), ("merchant_greet_4", "Welcome, welcome! Mind the bones on the floor."),
        };
        static readonly (string id, string text)[] Buy =
        {
            ("merchant_buy_1", "Heh heh... a fine choice."), ("merchant_buy_2", "Pleasure doing business, stranger."),
            ("merchant_buy_3", "That will serve you well down there."), ("merchant_buy_4", "Sold! Try not to die before you use it."),
        };
        static readonly (string id, string text)[] Win =
        {
            ("merchant_win_1", "Ohh! The gods smile on you today, stranger!"), ("merchant_win_2", "Lucky bones... Take your gold."),
            ("merchant_win_3", "Bah! Fortune is a fickle thing. You win."),
        };
        static readonly (string id, string text)[] Lose =
        {
            ("merchant_lose_1", "Heh heh heh... the bones have spoken."), ("merchant_lose_2", "The house of the dead always wins, stranger."),
            ("merchant_lose_3", "Better luck in your next life."),
        };
        static readonly (string id, string text)[] Bye =
        {
            ("merchant_bye_1", "Come back anytime... if you're still breathing."), ("merchant_bye_2", "Watch your back out there, stranger."),
        };

        /// <summary>The merchant of the start is copied to every region (like the travelling merchant of the classic horror
        /// games: the same hooded trader, always a step ahead of you). All of them get the voice, prices and skins.</summary>
        static void PlaceMerchants(AudioLibrary library)
        {
            var original = GameObject.Find("Merchant (Shop)");
            if (original == null) { Debug.LogWarning("[TD_Game] No merchant in the scene."); return; }
            var places = new (Vector2Int cell, string region, System.Func<K, bool> ground)[]
            {
                (new Vector2Int(21, 55), "Valley of Tlazolteotl", g => g == K.Grass),
                (new Vector2Int(96, 39), "Valley of Serpents", g => g == K.Sand),
                (new Vector2Int(118, 34), "Before the Temple", g => g == K.Sand),
            };
            Configure(original, "Merchant");
            foreach (var (cell, region, ground) in places)
            {
                var spot = FindSpot(World(cell), 2.6f, ground, 20f, awayFromPaths: true);
                if (spot == null) { Debug.LogWarning("[TD_Game] No room for the merchant of " + region); continue; }
                var copy = Object.Instantiate(original);
                copy.name = "V09_Merchant (" + region + ")";
                copy.transform.position = spot.Value;
                occupied.Add((spot.Value, 3f));
                Configure(copy, "Merchant");
            }
        }

        static void Configure(GameObject merchant, string label)
        {
            Marker(merchant, MapMarker.Kind.Merchant, label);
            var shop = merchant.GetComponentInChildren<MerchantShop>(true);
            var so = new SerializedObject(shop);
            so.FindProperty("potionPrice").intValue = 40;
            so.FindProperty("smallBet").intValue = 15;
            so.FindProperty("bigBet").intValue = 40;
            so.FindProperty("winChance").floatValue = 0.4f;
            so.FindProperty("artifactPrice").intValue = 200;
            so.FindProperty("voicePitch").floatValue = 0.94f;
            Voice(so.FindProperty("greetLines"), Greet);
            Voice(so.FindProperty("buyLines"), Buy);
            Voice(so.FindProperty("winLines"), Win);
            Voice(so.FindProperty("loseLines"), Lose);
            Voice(so.FindProperty("goodbyeLines"), Bye);
            var frames = so.FindProperty("previewFrames");
            var walkDown = WalkDownFrames();
            frames.arraySize = walkDown.Length;
            for (int i = 0; i < walkDown.Length; i++) frames.GetArrayElementAtIndex(i).objectReferenceValue = walkDown[i];
            so.FindProperty("menuFont").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>(ArtGenerator.FontPath);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Voice(SerializedProperty array, (string id, string text)[] lines)
        {
            array.arraySize = lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                var e = array.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("text").stringValue = lines[i].text;
                e.FindPropertyRelative("clip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SfxSynth.VoiceOut}/{lines[i].id}.wav");
            }
        }

        static Sprite[] WalkDownFrames()
        {
            var animator = Object.FindAnyObjectByType<PlayerSpriteAnimator>();
            if (animator == null) return new Sprite[0];
            var p = new SerializedObject(animator).FindProperty("walkDown");
            var list = new List<Sprite>();
            for (int i = 0; i < p.arraySize; i++) list.Add((Sprite)p.GetArrayElementAtIndex(i).objectReferenceValue);
            return list.ToArray();
        }

        // ---------------- Villagers ----------------

        static Sprite PlayerPortrait() => WalkDownFrames().FirstOrDefault();

        static DialogueNPC Npc(Transform root, string id, string npcName, string sprite, Vector2 position, string[] first, string[] later, bool solid = true)
        {
            var go = new GameObject("NPC_" + npcName);
            go.transform.SetParent(root, false);
            go.transform.position = position;
            occupied.Add((position, 1.5f));
            var body = new GameObject("Body").AddComponent<SpriteRenderer>();
            body.transform.SetParent(go.transform, false);
            body.sprite = S(sprite);
            body.spriteSortPoint = SpriteSortPoint.Pivot;
            var sign = new GameObject("Sign").AddComponent<SpriteRenderer>();
            sign.transform.SetParent(go.transform, false);
            sign.transform.localPosition = new Vector3(0f, body.sprite != null ? body.sprite.bounds.size.y + 0.3f : 2f, 0f);
            sign.sortingOrder = 505;   // seen even in the darkness of the night
            if (solid)
            {
                var box = go.AddComponent<BoxCollider2D>();
                box.size = new Vector2(0.7f, 0.35f);
                box.offset = new Vector2(0f, 0.17f);
            }
            var talk = go.AddComponent<CircleCollider2D>();
            talk.isTrigger = true;
            talk.radius = 1.35f;
            talk.offset = new Vector2(0f, 0.4f);
            var npc = go.AddComponent<DialogueNPC>();
            var so = new SerializedObject(npc);
            so.FindProperty("npcName").stringValue = npcName;
            so.FindProperty("id").stringValue = id;
            SetStrings(so.FindProperty("firstTalk"), first);
            SetStrings(so.FindProperty("laterTalk"), later ?? new string[0]);
            so.FindProperty("body").objectReferenceValue = body;
            so.FindProperty("playerPortrait").objectReferenceValue = PlayerPortrait();
            so.FindProperty("sign").objectReferenceValue = sign;
            so.FindProperty("newSign").objectReferenceValue = S("sign_new");
            so.FindProperty("talkedSign").objectReferenceValue = S("sign_talked");
            so.ApplyModifiedPropertiesWithoutUndo();
            Marker(go, MapMarker.Kind.Villager, npcName);
            return npc;
        }

        static Vector2 NpcSpot(Vector2Int cell, System.Func<K, bool> ground = null)
        {
            ground ??= g => g == K.Grass || g == K.Sand || g == K.PlazaEdge || g == K.Plaza;
            var spot = FindSpot(World(cell), 1.2f, ground, 16f);
            return spot ?? World(cell);
        }

        /// <summary>
        /// Villagers of Mictlán, each with a reason to be where they are: some teach (jumping, combat), some tell the story
        /// of the place, some warn about dangers and some point, without saying it plainly, to the side quest and secrets.
        /// </summary>
        static void PlaceVillagers()
        {
            var root = Root("V09_Villagers");
            Npc(root, "xochitl", "Xochitl, the boatwoman", "npc_boatwoman", NpcSpot(new Vector2Int(14, 23)),
                new[]
                {
                    "Careful by the water, young one. The river of Mictlán does not carry boats any more, only the careless.",
                    "My boat sank long ago. Now I watch the crossings. Where the river is narrow, the brave JUMP. Sprint first, then leap.",
                    "If you fall in, the river spits you back where you last stood on firm ground. It takes a little of your strength as payment.",
                    "There are bridges too. Look for the planks, or for flat stones in a line across the water.",
                },
                new[] { "Narrow water: sprint, then jump. Wide water: find a bridge. That is all the wisdom an old boatwoman has." });

            Npc(root, "cuauhtli", "Cuauhtli, ghost of an eagle warrior", "npc_eagle_ghost", NpcSpot(new Vector2Int(19, 9)),
                new[]
                {
                    "Halt, living one. I fell at the war of flowers, and I still guard this road out of habit.",
                    "Listen well. When an enemy glows red, wait... and press {Dodge} at the very last moment. Too soon and your guard locks.",
                    "In your turn, strike again when the blow lands ({Attack}): a critical hit, and your Tonalli grows.",
                    "The servants of the gods hunt in packs now. Kill the witch first: she heals the others and curses you.",
                    "And if you can, fall on them from a jump. A warrior who strikes first often strikes last.",
                },
                new[] { "Witches first. Dodge late. Jump on them when they do not see you. Go." });

            Npc(root, "itzel", "Itzel, the herb girl", "npc_herb_girl", NpcSpot(new Vector2Int(19, 42)),
                new[]
                {
                    "Oh! You are not a ghost... You have colour in your cheeks.",
                    "I gather cempasúchil for the dead. Their smell shows the souls the way home.",
                    "But I do not go west any more. Past the dead trees there is a crooked house with four purple lights.",
                    "At night a man screams in there. My grandmother says the Cihuateteo keep him... the women of the west.",
                    "> Four lights in the west. I will remember.",
                },
                new[] { "Go to the far west of the valley, behind the dead trees. Follow the purple light... if you dare." });

            Npc(root, "tecolotl", "Tecolotl, the owl priest", "npc_owl_priest", new Vector2(160f, 95.6f),
                new[]
                {
                    "The owl sees in the dark, warrior. So do I.",
                    "This is the Pyramid of the Feathered Serpent. Quetzalcóatl came down here to steal the bones of the dead and give life to people.",
                    "Climb the stairs. On the summit, his shrine still speaks to those who read its glyphs... and the faithful leave offerings there.",
                    "But Quetzalcóatl himself waits in the House of the Wind, far to the east. Corrupted, they say. Hungry.",
                },
                new[] { "Read the glyphs on the summit, warrior. The stairs are in the middle of the south face." });

            Npc(root, "tzontli", "Tzontli, the bone collector", "npc_bone_collector", NpcSpot(new Vector2Int(95, 45), g => g == K.Sand),
                new[]
                {
                    "Bones, bones, bones... Mictlán is generous with bones.",
                    "You want advice? Do not fight the giants with an empty belly. They hit like falling mountains. Bring potions.",
                    "And watch the ground. The ravines of this desert are deep. Some are narrow enough to jump; I have seen fast warriors cross them to cut the way.",
                    "If you get lost, look for the skull of stone. It looks south, towards the lake of Xólotl.",
                },
                new[] { "Narrow ravines can be jumped. Wide ones... well. More bones for me." });

            Npc(root, "atl", "Atl, the fisherman", "npc_fisherman", NpcSpot(new Vector2Int(103, 58), g => g == K.Sand),
                new[]
                {
                    "Nothing bites in this lake. Nothing alive, anyway.",
                    "This water comes from the Apanohuaya, the river every soul must cross to reach Mictlán.",
                    "The dead cross it on the back of a dog. A xoloitzcuintle. That is why the statue of Xólotl stands here: the god-dog keeps the crossing.",
                    "Be kind to the dogs you meet. One of them may carry you one day.",
                },
                new[] { "A dog by the river of the forest watches the stones in the water. Strange animal." });

            Npc(root, "xolo", "A xoloitzcuintle", "npc_xolo", NpcSpot(new Vector2Int(25, 25)),
                new[]
                {
                    "Woof!",
                    "> The dog looks at the river, then at me... then at the river again.",
                    "> There are flat stones in the water. A line of them. Maybe I could jump from one to the next.",
                },
                new[] { "Woof! Woof!", "> He wags his tail at the stones in the river." }, solid: false);

            Npc(root, "yaotl", "Yaotl, the pilgrim", "npc_pilgrim", NpcSpot(new Vector2Int(128, 34), g => g == K.Sand),
                new[]
                {
                    "I walked here from the world above to bring my brother an offering. I am still walking.",
                    "To the south-east there is a cave whose walls are all rock. Pilgrims hid their treasures there, to pay their way through Mictlán.",
                    "North of here stands the black temple, the House of the Wind. Its obsidian gates will not open... not without the key.",
                    "They say the key appears when every servant of the gods has fallen. Count them, warrior.",
                },
                new[] { "The cave is to the south-east, the temple to the north. May the road be short." });

            Npc(root, "citlali", "Citlali, the widow", "npc_widow", NpcSpot(new Vector2Int(31, 62)),
                new[]
                {
                    "I light candles for my husband. The witches took him to the crooked house in the west.",
                    "They are the Cihuateteo, women who died giving life. Mictlán took their names, and now they steal from those who still have one.",
                    "Four of them. Four lights. They never leave him alone... you would have to face all four at once.",
                    "If you go... look at the walls. They always paint what they steal.",
                },
                new[] { "Four lights in the west. They paint what they steal. Remember that." });
        }

        // ---------------- The witches' house (side quest) ----------------

        static void BuildWitchHouse()
        {
            var root = Root("V09_WitchHouse");
            var house = FindSpot(World(new Vector2Int(3, 56)), 3.2f, g => g == K.Grass, 12f) ?? World(new Vector2Int(3, 56));
            occupied.Add((house, 4.5f));

            // Outside: the crooked hut, dead trees, crows, skulls and purple candles.
            var hut = Prop(root, "Crooked House", S("witch_house"), house, new Vector2(6.2f, 2.3f), reserve: 0f);
            hut.GetComponent<BoxCollider2D>().offset = new Vector2(0f, 1.45f);
            Glow(hut.transform, new Vector3(0f, 1.2f, 0f), 3f, new Color(0.6f, 0.3f, 0.9f, 0.35f));
            var marker = hut.gameObject.AddComponent<MapMarker>();
            marker.Set(MapMarker.Kind.SideQuest, "The Prisoner of the Four Witches");
            foreach (var (offset, sprite) in new[] { (new Vector2(-4.6f, 0.4f), "dead_tree"), (new Vector2(4.7f, 0.8f), "dead_tree"), (new Vector2(-3.4f, -1.8f), "skull_pile"),
                                                    (new Vector2(3.2f, -2.2f), "crow"), (new Vector2(-5.5f, -1.2f), "crow") })
                Prop(root, "Decor", S(sprite), house + offset, sprite == "dead_tree" ? new Vector2(0.6f, 0.4f) : (Vector2?)null, reserve: 0.5f, flip: offset.x > 0);
            for (int i = 0; i < 4; i++)
            {
                var candle = Prop(root, "PurpleCandle", S("candle"), house + new Vector2(-2.6f + i * 1.75f, -2.7f), reserve: 0.2f);
                Glow(candle.transform, new Vector3(0f, 0.6f, 0f), 1.4f, new Color(0.65f, 0.35f, 1f, 0.45f));
            }
            // The team's mist creeps around the house.
            if (team.fog != null)
            {
                var mist = Root("V09_WitchMist");
                foreach (var o in new[] { new Vector2(-4f, -1.5f), new Vector2(3.8f, -1.2f), new Vector2(-1.5f, -3.4f), new Vector2(2f, -3.6f), new Vector2(-5.5f, 1.5f), new Vector2(5.2f, 1.8f) })
                    Mist(mist, house + o);
            }

            // Inside: a room far from the map (its own grid), dark floor, adobe walls, an exit gap in the south wall.
            var interiorGrid = new GameObject("V09_Interiors", typeof(Grid)).transform;
            var floor = InteriorTilemap(interiorGrid, "WitchFloor", -10, false);
            var walls = InteriorTilemap(interiorGrid, "WitchWalls", 0, true);
            const int x0 = -118, x1 = -103, y0 = 2, y1 = 13;   // the north wall is two rows tall: things hang ON it
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                {
                    var t = new Vector3Int(x, y, 0);
                    floor.SetTile(t, tiles.interiorFloor);
                    bool edge = x == x0 || x == x1 || y == y0 || y >= y1 - 1;
                    bool door = y == y0 && (x == -111 || x == -110);
                    if (edge && !door) walls.SetTile(t, tiles.interiorWall);
                }
            for (int x = x0; x <= x1; x++) floor.SetTile(new Vector3Int(x, y0 - 1, 0), tiles.interiorFloor);   // so the camera shows the doorway
            floor.CompressBounds(); walls.CompressBounds();

            Vector2 entry = new Vector2(-110f, 4.2f);
            Vector2 outside = house + new Vector2(0f, -1.9f);
            Door(root, "Door (outside)", house + new Vector2(0f, -0.35f), new Vector2(1.4f, 0.7f), entry, floor, true, hut.transform);
            Door(interiorGrid, "Door (inside)", new Vector2(-110f, 2.35f), new Vector2(2.2f, 0.7f), outside, GroundTilemap(), false, null);

            var inside = Root("V09_WitchHouseInside");
            Prop(inside, "RitualCircle", S("ritual_circle"), new Vector2(-110f, 6.9f), order: -9, reserve: 0f).spriteSortPoint = SpriteSortPoint.Center;
            Prop(inside, "Blood", S("bloodstain"), new Vector2(-106.2f, 4.3f), order: -9, reserve: 0f);
            Prop(inside, "Cauldron", S("cauldron"), new Vector2(-116.2f, 4f), new Vector2(1.3f, 0.5f), reserve: 0f);
            Prop(inside, "Shelf", S("shelf"), new Vector2(-116f, 11.05f), new Vector2(2f, 0.5f), reserve: 0f);
            Prop(inside, "Shelf", S("shelf"), new Vector2(-104.9f, 11.05f), new Vector2(2f, 0.5f), reserve: 0f, flip: true);
            Prop(inside, "Skulls", S("skull_pile"), new Vector2(-104.8f, 3.4f), new Vector2(0.9f, 0.3f), reserve: 0f);
            foreach (float hx in new[] { -114.6f, -112.5f })
            {
                var herbs = Prop(inside, "Herbs", S("herbs"), new Vector2(hx, 14.45f), reserve: 0f, order: 1);
                herbs.spriteSortPoint = SpriteSortPoint.Center;
            }
            // Candles at the four points of the circle.
            foreach (var c in new[] { new Vector2(-114.6f, 6.9f), new Vector2(-105.4f, 6.9f), new Vector2(-110f, 9.7f), new Vector2(-110f, 4.1f) })
            {
                var candle = Prop(inside, "Candle", S("candle"), c, new Vector2(0.3f, 0.2f), reserve: 0f);
                Glow(candle.transform, new Vector3(0f, 0.6f, 0f), 2f, new Color(0.7f, 0.4f, 1f, 0.5f));
            }

            // The prisoner hanging from the north wall, the chains and the torn painting.
            var prisoner = Npc(inside, "painal", "Painal", "prisoner_chained", new Vector2(-110f, 11.35f), new[] { "..." }, null, solid: true);
            var prisonerBody = prisoner.transform.Find("Body").GetComponent<SpriteRenderer>();
            var npcSo = new SerializedObject(prisoner);
            npcSo.FindProperty("idleBob").boolValue = false;
            npcSo.FindProperty("portrait").objectReferenceValue = S("prisoner_free");
            npcSo.ApplyModifiedPropertiesWithoutUndo();
            Object.DestroyImmediate(prisoner.GetComponent<MapMarker>());
            var chains = Prop(inside, "Chains", S("wall_chains"), new Vector2(-110f, 12.9f), reserve: 0f, order: 1);
            var freed = new GameObject("FreedSpot").transform;
            freed.SetParent(inside, false);
            freed.position = new Vector2(-110f, 9.8f);

            var painting = Prop(inside, "Painting", S("painting_torn"), new Vector2(-107.4f, 12.2f), reserve: 0f, order: 1);
            var paintingSpot = new GameObject("PaintingSpot");
            paintingSpot.transform.SetParent(inside, false);
            paintingSpot.transform.position = new Vector2(-107.4f, 11.5f);
            var area = paintingSpot.AddComponent<BoxCollider2D>();
            area.isTrigger = true;
            area.size = new Vector2(2.2f, 1.0f);
            var spotSo = new SerializedObject(paintingSpot.AddComponent<InteractSpot>());
            spotSo.FindProperty("prompt").stringValue = "{Interact}  Look at the torn painting";
            spotSo.ApplyModifiedPropertiesWithoutUndo();

            // The four witches around the circle: one battle against all of them.
            var witches = new List<CombatEnemy>();
            var witchRoot = new GameObject("Witches").transform;
            witchRoot.SetParent(inside, false);
            Vector2[] places = { new Vector2(-114f, 5.4f), new Vector2(-106f, 5.4f), new Vector2(-113.6f, 8.6f), new Vector2(-106.4f, 8.6f) };
            for (int i = 0; i < 4; i++) witches.Add(Spawn(Witch, places[i], witchRoot, $"Cihuateotl_{i + 1}", 1.7f, "witch_house", optional: true, data: CihuateotlData()));

            var puzzle = inside.gameObject.AddComponent<PaintingPuzzle>();
            var pz = new SerializedObject(puzzle);
            pz.FindProperty("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>(ArtGenerator.FontPath);
            pz.FindProperty("picture").objectReferenceValue = PropArt.PaintingTexture();
            pz.FindProperty("title").stringValue = "THE TORN CODEX";
            pz.FindProperty("caption").stringValue = "The witches tore the painting of Painal's capture. Put it back together to break their spell.";
            pz.ApplyModifiedPropertiesWithoutUndo();

            var quest = inside.gameObject.AddComponent<WitchHouseQuest>();
            var q = new SerializedObject(quest);
            var list = q.FindProperty("witches");
            list.arraySize = witches.Count;
            for (int i = 0; i < witches.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = witches[i];
            q.FindProperty("prisoner").objectReferenceValue = prisoner;
            q.FindProperty("prisonerSprite").objectReferenceValue = prisonerBody;
            q.FindProperty("chainedSprite").objectReferenceValue = S("prisoner_chained");
            q.FindProperty("freedSprite").objectReferenceValue = S("prisoner_free");
            q.FindProperty("chains").objectReferenceValue = chains.gameObject;
            q.FindProperty("paintingSpot").objectReferenceValue = paintingSpot.GetComponent<InteractSpot>();
            q.FindProperty("paintingSprite").objectReferenceValue = painting;
            q.FindProperty("tornPainting").objectReferenceValue = S("painting_torn");
            q.FindProperty("wholePainting").objectReferenceValue = S("painting_whole");
            q.FindProperty("puzzle").objectReferenceValue = puzzle;
            q.FindProperty("marker").objectReferenceValue = marker;
            q.FindProperty("freedSpot").objectReferenceValue = freed;
            q.ApplyModifiedPropertiesWithoutUndo();
        }

        static Tilemap InteriorTilemap(Transform grid, string name, int order, bool collider)
        {
            var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(grid, false);
            var r = go.GetComponent<TilemapRenderer>();
            r.sortingOrder = order;
            r.mode = collider ? TilemapRenderer.Mode.Individual : TilemapRenderer.Mode.Chunk;
            if (collider) go.AddComponent<TilemapCollider2D>();
            return go.GetComponent<Tilemap>();
        }

        static Tilemap GroundTilemap() => Object.FindObjectsByType<Tilemap>().First(t => t.name == "Ground");

        static void Door(Transform parent, string name, Vector2 position, Vector2 size, Vector2 destination, Tilemap bounds, bool inside, Transform mapPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = size;
            var arrival = new GameObject("Arrival").transform;
            arrival.SetParent(go.transform, false);
            arrival.position = destination;
            var door = new SerializedObject(go.AddComponent<HouseDoor>());
            door.FindProperty("destination").objectReferenceValue = arrival;
            door.FindProperty("destinationBounds").objectReferenceValue = bounds;
            door.FindProperty("leadsInside").boolValue = inside;
            door.FindProperty("mapPosition").objectReferenceValue = mapPosition;
            door.FindProperty("id").stringValue = "witch_house";
            door.FindProperty("firstTimeBanner").stringValue = inside ? "THE HOUSE OF THE FOUR LIGHTS\nSomething breathes in the dark..." : "";
            door.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------- The player ----------------

        static void SetupPlayer(SkinCatalog catalog)
        {
            var player = Object.FindAnyObjectByType<PlayerCombatant>();
            var go = player.gameObject;
            go.layer = LayerMask.NameToLayer(PlayerJump.PlayerLayer);
            var visual = go.transform.Find("Visual");
            var shadow = go.transform.Find("Shadow");

            var jump = new SerializedObject(go.GetComponent<PlayerJump>());
            jump.FindProperty("airTime").floatValue = 0.55f;
            jump.ApplyModifiedPropertiesWithoutUndo();

            var hazards = go.GetComponent<PlayerHazards>() ?? go.AddComponent<PlayerHazards>();
            var h = new SerializedObject(hazards);
            h.FindProperty("visual").objectReferenceValue = visual;
            h.FindProperty("shadow").objectReferenceValue = shadow;
            foreach (var t in Object.FindObjectsByType<Tilemap>())
            {
                if (t.name == "Water") h.FindProperty("water").objectReferenceValue = t;
                if (t.name == "Pits") h.FindProperty("pits").objectReferenceValue = t;
            }
            h.ApplyModifiedPropertiesWithoutUndo();

            // One sorting unit for Tlahuelli, his torch and his shadow: the torch can no longer poke through a roof when he
            // walks behind a house, and while jumping the whole group is drawn above logs and rocks (PlayerJump).
            if (go.GetComponent<UnityEngine.Rendering.SortingGroup>() == null) go.AddComponent<UnityEngine.Rendering.SortingGroup>();

            var skin = go.GetComponent<PlayerSkin>() ?? go.AddComponent<PlayerSkin>();
            var s = new SerializedObject(skin);
            s.FindProperty("catalog").objectReferenceValue = catalog;
            s.FindProperty("target").objectReferenceValue = visual.GetComponent<SpriteRenderer>();
            s.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------- Systems: combat, tips, chests, ambience ----------------

        static void SetupSystems(AudioLibrary library)
        {
            var combat = Object.FindAnyObjectByType<CombatManager>();
            var cm = new SerializedObject(combat);
            cm.FindProperty("maxEnemies").intValue = 4;   // the four witches fight together
            var orbs = new SerializedObject(combat.GetComponent<OrbSpawner>());
            orbs.FindProperty("maxOrbsPerType").intValue = 4;   // fewer, richer orbs: dozens at once made the game stutter on every kill
            orbs.ApplyModifiedPropertiesWithoutUndo();
            SolidPuzzleBraziers();
            cm.ApplyModifiedPropertiesWithoutUndo();

            var tutorial = Object.FindAnyObjectByType<ContextTutorial>();
            if (tutorial != null)
            {
                var t = new SerializedObject(tutorial);
                SetStrings(t.FindProperty("explorationTips"),
                    "Press {Map} to open the map of Mictlán: enemies, merchants, villagers and side quests are marked on it.",
                    "Villagers with a gold ! over their heads have something new to tell. Talk to them with {Interact}.",
                    "Rivers and ravines are dangerous. Jump ({Jump}) over the narrow parts; sprint ({Sprint}) first to jump farther.",
                    "Fallen logs, bushes and low rocks can be jumped over. Look for shortcuts.",
                    "A merchant waits in every region. Gold is scarce: choose well.",
                    "Enemies Left (bottom right) counts the servants of the gods still standing. When it reaches 0, the obsidian key appears.",
                    "Press {Pause} to pause the game and change the volume.");
                t.ApplyModifiedPropertiesWithoutUndo();
            }

            // Chests: less gold (gold must feel like a reward).
            var chests = GameObject.Find("Chests");
            if (chests != null)
            {
                int i = 0;
                foreach (var chest in chests.GetComponentsInChildren<LootChest>())
                {
                    var so = new SerializedObject(chest);
                    bool cave = so.FindProperty("title").stringValue.Contains("CAVE");
                    so.FindProperty("gold").intValue = cave ? 30 : 8 + 4 * i++;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            var ambience = new GameObject("V09_Ambience").AddComponent<AmbientSoundscape>();
            var a = new SerializedObject(ambience);
            a.FindProperty("forest").objectReferenceValue = Clip("amb_forest");
            a.FindProperty("river").objectReferenceValue = Clip("amb_river");
            a.FindProperty("desertWind").objectReferenceValue = Clip("amb_wind");
            a.FindProperty("interior").objectReferenceValue = Clip("amb_interior");
            a.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------- UI ----------------

        static void BuildUI()
        {
            var mapUI = GameObject.Find("MapUI");
            var font = AssetDatabase.LoadAssetAtPath<Font>(ArtGenerator.FontPath);
            var frame = AssetDatabase.LoadAssetAtPath<Sprite>(ArtGenerator.FramePath);
            var root = mapUI.transform;
            foreach (string old in new[] { "EnemiesLeft", "WorldMap", "Dialogue" })
            {
                var t = root.Find(old);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }

            // Enemies Left, bottom right.
            var box = new GameObject("EnemiesLeft", typeof(RectTransform)).GetComponent<RectTransform>();
            box.gameObject.layer = LayerMask.NameToLayer("UI");
            box.SetParent(root, false);
            box.anchorMin = box.anchorMax = box.pivot = new Vector2(1f, 0f);
            box.anchoredPosition = new Vector2(-10f, 10f);
            box.sizeDelta = new Vector2(300f, 34f);
            var image = box.gameObject.AddComponent<Image>();
            image.sprite = frame;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 3f;
            image.raycastTarget = false;
            var group = box.gameObject.AddComponent<CanvasGroup>();
            var label = new GameObject("Text", typeof(RectTransform)).AddComponent<Text>();
            label.transform.SetParent(box, false);
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.font = font; label.fontSize = 17; label.color = MexicaPalette.Bone; label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 9;
            label.resizeTextMaxSize = 17;
            label.text = "Enemies Left: 0";
            var counter = new SerializedObject(box.gameObject.AddComponent<EnemiesLeftCounter>());
            counter.FindProperty("label").objectReferenceValue = label;
            counter.FindProperty("group").objectReferenceValue = group;
            counter.FindProperty("allDefeated").stringValue = "Enemies Left: 0  -  the obsidian key awaits";
            counter.ApplyModifiedPropertiesWithoutUndo();

            // The shared interact prompt uses the text of the shop prompt.
            var prompt = mapUI.GetComponent<InteractPrompt>() ?? mapUI.AddComponent<InteractPrompt>();
            var shopPrompt = root.Find("ShopPrompt");
            var p = new SerializedObject(prompt);
            p.FindProperty("text").objectReferenceValue = shopPrompt != null ? shopPrompt.GetComponentInChildren<Text>(true) : null;
            p.ApplyModifiedPropertiesWithoutUndo();

            // Full-screen map and dialogue box (they build their own canvases when first used).
            // Root objects: inside the MapUI canvas their canvases would take its scale (960 x 540) and its drawing order.
            var worldMap = new GameObject("V09_WorldMap").AddComponent<WorldMapScreen>();
            var wm = new SerializedObject(worldMap);
            wm.FindProperty("font").objectReferenceValue = font;
            wm.FindProperty("ground").objectReferenceValue = GroundTilemap();
            wm.FindProperty("player").objectReferenceValue = Object.FindAnyObjectByType<PlayerCombatant>().transform;
            wm.FindProperty("playerIcon").objectReferenceValue = S("map_arrow");
            wm.FindProperty("enemyIcon").objectReferenceValue = S("icon_enemy");
            wm.FindProperty("bossIcon").objectReferenceValue = S("icon_boss");
            wm.FindProperty("merchantIcon").objectReferenceValue = S("orb_coin");
            wm.FindProperty("villagerIcon").objectReferenceValue = S("icon_villager");
            wm.FindProperty("questIcon").objectReferenceValue = S("icon_quest");
            wm.FindProperty("landmarkIcon").objectReferenceValue = S("icon_landmark");
            wm.FindProperty("doorIcon").objectReferenceValue = S("icon_door");
            wm.ApplyModifiedPropertiesWithoutUndo();

            var dialogue = new GameObject("V09_Dialogue").AddComponent<DialogueBox>();
            var d = new SerializedObject(dialogue);
            d.FindProperty("font").objectReferenceValue = font;
            d.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------- Audio ----------------

        static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{SfxSynth.Folder}/{name}.wav");

        static void ImportAudio()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { SfxSynth.Folder, SfxSynth.VoiceOut }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                importer.forceToMono = true;
                var settings = importer.defaultSampleSettings;
                settings.loadType = path.Contains("amb_") ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.7f;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
        }

        /// <summary>
        /// Softer, varied combat sounds: the enemy warning and the hit on Tlahuelli are replaced (they were shrill and tiring),
        /// the weapon swings and hits on enemies keep their old sound plus new versions. New sounds for falls, chains and the
        /// puzzle. Volumes balanced so effects, music and the merchant's voice sit together.
        /// </summary>
        static AudioLibrary UpdateAudioLibrary()
        {
            var library = AssetDatabase.LoadAssetAtPath<AudioLibrary>("Assets/TD_Game/Audio/AudioLibrary.asset");
            var list = library.sfx.ToList();
            AudioLibrary.NamedClip Entry(string name)
            {
                var e = list.FirstOrDefault(x => x.name == name);
                if (e == null) { e = new AudioLibrary.NamedClip { name = name }; list.Add(e); }
                return e;
            }
            void Set(string name, AudioClip main, float volume, params string[] variants)
            {
                var e = Entry(name);
                if (main != null) e.clip = main;
                e.volume = volume;
                e.variants = variants.Select(Clip).Where(c => c != null).ToArray();
            }
            Set("telegraph", Clip("telegraph_1"), 0.5f, "telegraph_2", "telegraph_3");
            Set("hurt", Clip("hurt_1"), 0.85f, "hurt_2", "hurt_3");
            Set("enemy_hit", Clip("enemy_hit_1"), 0.85f, "enemy_hit_2", "enemy_hit_3");
            Set("swing", null, 0.6f, "swing_1", "swing_2", "swing_3");
            Set("splash", Clip("splash"), 0.85f);
            Set("fall_pit", Clip("fall_pit"), 0.85f);
            Set("chains_break", Clip("chains_break"), 0.9f);
            Set("puzzle_place", Clip("puzzle_place"), 0.8f);
            Set("puzzle_done", Clip("puzzle_done"), 0.85f);
            Entry("alert").volume = 0.6f;
            Entry("roar").volume = 0.8f;
            Entry("victory").volume = 0.75f;
            Entry("defeat").volume = 0.75f;
            Entry("ui_move").volume = 0.6f;
            library.sfx = list.ToArray();
            EditorUtility.SetDirty(library);
            return library;
        }

        // ---------------- Skins, gold, menus ----------------

        static SkinCatalog SkinCatalogAsset()
        {
            const string materialPath = "Assets/TD_Game/Materials/SpriteSkin.mat";
            const string catalogPath = "Assets/TD_Game/ScriptableObjects/Items/SkinCatalog.asset";
            System.IO.Directory.CreateDirectory("Assets/TD_Game/Materials");
            System.IO.Directory.CreateDirectory("Assets/TD_Game/ScriptableObjects/Items");
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("TD_Game/SpriteSkin"));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            var catalog = AssetDatabase.LoadAssetAtPath<SkinCatalog>(catalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<SkinCatalog>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }
            catalog.material = material;
            catalog.skins = new[]
            {
                new SkinCatalog.Skin { id = "default", displayName = "Warrior of Mictlán", description = "The blue of the night sky over Tenochtitlan.", price = 0 },
                new SkinCatalog.Skin { id = "jaguar", displayName = "Jaguar Knight", description = "Gold and ochre of the jaguar warriors.", price = 120, hueShift = -0.52f, saturation = 0.95f, brightness = 1.05f },
                new SkinCatalog.Skin { id = "quetzal", displayName = "Quetzal Jade", description = "Green as the feathers of the quetzal.", price = 150, hueShift = -0.19f, saturation = 0.9f, brightness = 0.95f },
                new SkinCatalog.Skin { id = "tezontle", displayName = "Tezontle Red", description = "Dyed with cochineal: the colour of sacrifice.", price = 180, hueShift = 0.39f, saturation = 0.85f, brightness = 0.9f },
                new SkinCatalog.Skin { id = "eagle", displayName = "Eagle Bone", description = "White of eagle down and old bone.", price = 220, hueShift = 0f, saturation = 0.12f, brightness = 1.45f },
                new SkinCatalog.Skin { id = "obsidian", displayName = "Obsidian Night", description = "Black glass and smoke, like Tezcatlipoca.", price = 300, hueShift = 0.12f, saturation = 0.5f, brightness = 0.45f },
            };
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        /// <summary>The four witches of the house are Cihuateteo: same powers as the witches of the valley, their own name.</summary>
        static EnemyData CihuateotlData()
        {
            const string folder = "Assets/TD_Game/ScriptableObjects/Enemies";
            string path = $"{folder}/EnemyData_Cihuateotl.asset";
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (data == null)
            {
                AssetDatabase.CopyAsset($"{folder}/EnemyData_Witch.asset", path);
                data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            }
            data.displayName = "Cihuateotl";
            data.goldOnDefeat = 6;
            EditorUtility.SetDirty(data);
            return data;
        }

        /// <summary>Names from the Mexica world instead of generic ones, and a harder game (no free potion per victory).</summary>
        static void MexicaNamesAndDifficulty()
        {
            const string folder = "Assets/TD_Game/ScriptableObjects/Enemies";
            foreach (var (asset, name) in new[] { ("EnemyData_Wolf", "Cuetlachtli"), ("EnemyData_Witch", "Tlahuelpuchi"), ("EnemyData_Giant", "Quinametzin") })
            {
                var data = AssetDatabase.LoadAssetAtPath<EnemyData>($"{folder}/{asset}.asset");
                if (data == null) continue;
                data.displayName = name;
                EditorUtility.SetDirty(data);
            }
            var stats = AssetDatabase.LoadAssetAtPath<PlayerStats>("Assets/TD_Game/ScriptableObjects/Characters/PlayerStats_Tlahuelli.asset");
            if (stats != null)
            {
                stats.potionsOnVictory = 0;   // potions come from chests, the merchants and the green orbs heal
                EditorUtility.SetDirty(stats);
            }
        }

        /// <summary>The braziers (with their gems) and the treasure of the Four Fires puzzle: you walk around them, not through.
        /// Their big trigger (to light them) stays; a small solid base is added.</summary>
        static void SolidPuzzleBraziers()
        {
            var shrine = GameObject.Find("Puzzle (Four Fires)");
            if (shrine == null) return;
            foreach (Transform child in shrine.transform)
            {
                bool brazier = child.GetComponent<PuzzleBrazier>() != null, treasure = child.name == "Treasure";
                if (!brazier && !treasure) continue;
                var old = child.Find("SolidBase");
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var solid = new GameObject("SolidBase").AddComponent<BoxCollider2D>();
                solid.transform.SetParent(child, false);
                solid.size = treasure ? new Vector2(0.9f, 0.4f) : new Vector2(0.6f, 0.35f);
                solid.offset = new Vector2(0f, solid.size.y / 2f);
            }
        }

        /// <summary>Less gold per enemy: there are many more of them now, and gold must feel valuable.</summary>
        static void SetEnemyGold()
        {
            foreach (var (asset, gold) in new[] { ("EnemyData_Wolf", 2), ("EnemyData_Witch", 4), ("EnemyData_Giant", 8) })
            {
                var data = AssetDatabase.LoadAssetAtPath<EnemyData>($"Assets/TD_Game/ScriptableObjects/Enemies/{asset}.asset");
                if (data == null) continue;
                data.goldOnDefeat = gold;
                EditorUtility.SetDirty(data);
            }
        }

        /// <summary>The version number on the title screen.</summary>
        static void UpdateMenuScenes()
        {
            PlayerSettings.bundleVersion = "0.9.0";
            const string menu = "Assets/TD_Game/Scenes/MainMenu/TD_MainMenu.unity";
            var scene = EditorSceneManager.OpenScene(menu);
            var title = Object.FindAnyObjectByType<TitleMenu>();
            if (title != null)
            {
                var so = new SerializedObject(title);
                so.FindProperty("version").stringValue = "v0.9";
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
    }
}
