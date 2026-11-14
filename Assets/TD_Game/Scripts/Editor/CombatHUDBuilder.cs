using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// Menu TD_Game > Build Combat HUD: creates the "CombatHUD" Canvas in the open scene and wires it
    /// to the CombatManager and the player. The result is a normal scene object: edit it in the Inspector.
    /// It does nothing if a CombatHUD already exists (delete it first to rebuild).
    ///
    /// Style: a diagonal command list on the right with round key badges and big italic names on
    /// opaque dark plates with thin gold lines; a turn order strip at the top; every message on a plate.
    /// </summary>
    public static class CombatHUDBuilder
    {
        const string SquareSprite = "Assets/TD_Game/Art/Tilesets/Placeholder/Textures/Square_64.png";

        static readonly Color Obsidian = MexicaPalette.Obsidian;        // opaque: the gold Outline must not show through
        static readonly Color PlateColor = MexicaPalette.ObsidianPlate;
        static readonly Color Gold = MexicaPalette.AgedGold;
        static readonly Color Bone = MexicaPalette.Bone;

        static Font font;
        static Sprite square, circle, greca, vignette;

        const string GrecaPath = "Assets/TD_Game/Art/UI/Greca_Almenas.png";
        const string VignettePath = "Assets/TD_Game/Art/UI/Vignette.png";

        [MenuItem("TD_Game/Build Combat HUD")]
        public static void Build()
        {
            if (GameObject.Find("CombatHUD") != null) { Debug.LogWarning("CombatHUD already exists. Delete it to rebuild."); return; }
            var combat = Object.FindAnyObjectByType<CombatManager>();
            var player = Object.FindAnyObjectByType<PlayerCombatant>();
            if (combat == null || player == null) { Debug.LogError("The scene needs a CombatManager and a PlayerCombatant."); return; }

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            square = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSprite);
            circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            greca = EnsureGreca();
            vignette = EnsureVignette();

            var canvasGo = new GameObject("CombatHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGo, "Build Combat HUD");
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 540);
            scaler.matchWidthOrHeight = 1f;
            var root = (RectTransform)canvasGo.transform;
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

            BuildStatus(root, player);
            var combatGroup = Stretch(New("CombatGroup", root)).gameObject.AddComponent<CanvasGroup>();
            combatGroup.alpha = 0f;
            var groupRect = (RectTransform)combatGroup.transform;

            // The world darkens towards the edges when a battle starts: the fight feels like a ritual arena.
            var shade = Stretch(New("CombatVignette", groupRect)).gameObject.AddComponent<Image>();
            shade.sprite = vignette; shade.color = Color.black; shade.raycastTarget = false;

            BuildEnemyBars(groupRect, combat);
            BuildTurnOrder(groupRect, combat);
            BuildMenu(groupRect, combat);
            BuildMessages(root, player);

            var hud = canvasGo.AddComponent<CombatHUD>();
            Set(hud, "combat", combat);
            Set(hud, "combatGroup", combatGroup);
            EditorUtility.SetDirty(canvasGo);
        }

        // ---------------- Sections ----------------

        static void BuildStatus(RectTransform root, PlayerCombatant player)
        {
            var panel = Panel("PlayerStatus", root, new Vector2(0, 1), new Vector2(12, -12), new Vector2(270, 124));
            Greca(panel, bottom: true, height: 12f);
            Label("Title", panel, "◆ TLAHUELLI", 14, Gold, TextAnchor.UpperLeft, new Vector2(10, -6), new Vector2(250, 18), FontStyle.BoldAndItalic);
            var hp = Bar("Health", panel, new Vector2(10, -26), new Vector2(250, 16), MexicaPalette.Cinnabar);
            var st = Bar("Stamina", panel, new Vector2(10, -46), new Vector2(250, 16), MexicaPalette.Ochre);
            var tn = Bar("Tonalli", panel, new Vector2(10, -66), new Vector2(250, 16), MexicaPalette.Turquoise);
            var info = Label("Info", panel, "", 12, Bone, TextAnchor.UpperLeft, new Vector2(10, -86), new Vector2(250, 18));
            var ready = Label("TonalliReady", panel, "TONALLI READY  [5]", 12, MexicaPalette.Turquoise,
                TextAnchor.UpperRight, new Vector2(10, -6), new Vector2(250, 18), FontStyle.Bold);

            var status = panel.gameObject.AddComponent<PlayerStatusPanel>();
            Set(status, "player", player);
            Set(status, "health", hp); Set(status, "stamina", st); Set(status, "tonalli", tn);
            Set(status, "info", info); Set(status, "tonalliReady", ready);
        }

        static void BuildMenu(RectTransform parent, CombatManager combat)
        {
            // Command list: bottom-right, rows stepping to the left as they go up (diagonal).
            var menu = New("CommandMenu", parent);
            menu.anchorMin = menu.anchorMax = menu.pivot = new Vector2(1f, 0f);
            menu.anchoredPosition = new Vector2(-16, 16);
            menu.sizeDelta = new Vector2(300, 300);
            var menuGroup = menu.gameObject.AddComponent<CanvasGroup>();

            const float rowHeight = 38f, gap = 6f, rowWidth = 250f, step = 12f;
            string[] names = { "Action1", "Action2", "Action3", "Action4", "Action5", "EndTurn" };
            var rows = new ActionButton[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                int fromBottom = names.Length - 1 - i;    // the first command is on top
                var pos = new Vector2(-fromBottom * step, fromBottom * (rowHeight + gap));
                rows[i] = CommandRow(menu, names[i], pos, new Vector2(rowWidth, rowHeight), i == names.Length - 1);
            }

            // Stepped-merlon frieze above the first command, like the top of a temple.
            var frieze = New("Frieze", menu);
            frieze.anchorMin = frieze.anchorMax = frieze.pivot = new Vector2(1f, 0f);
            frieze.anchoredPosition = new Vector2(-(names.Length - 1) * step, names.Length * (rowHeight + gap) - 2);
            frieze.sizeDelta = new Vector2(rowWidth, 14);
            TiledGreca(frieze.gameObject, frieze.sizeDelta.y);

            // Target / combo / action description: left of the bottom rows, slides with the menu.
            var hintPlate = Plate("HintPlate", menu, new Vector2(1f, 0f), Vector2.zero, 13, out var hint);
            hintPlate.pivot = new Vector2(1f, 0f);
            hintPlate.anchoredPosition = new Vector2(-(rowWidth + 6 * step + 14), 4);
            hint.alignment = TextAnchor.MiddleLeft;
            hintPlate.gameObject.SetActive(true);

            // What to do during the enemy's turn (replaces the menu).
            var enemyBar = Plate("EnemyTurnBar", parent, new Vector2(0.5f, 0f), new Vector2(0, 22), 16, out var enemyText);
            enemyText.text = "◆  DEFEND  ◆        SPACE / B   dodge        ·        1 / A   interrupt spells        ·        late press = PERFECT";
            enemyText.color = Bone;
            enemyBar.gameObject.SetActive(true);
            var enemyGroup = enemyBar.gameObject.AddComponent<CanvasGroup>();
            enemyGroup.alpha = 0f;
            enemyGroup.blocksRaycasts = false;

            var actionMenu = menu.gameObject.AddComponent<CombatActionMenu>();
            Set(actionMenu, "combat", combat);
            Set(actionMenu, "endTurnButton", rows[names.Length - 1]);
            Set(actionMenu, "hint", hint);
            Set(actionMenu, "menuGroup", menuGroup);
            Set(actionMenu, "enemyTurnBar", enemyGroup);
            var so = new SerializedObject(actionMenu);
            var list = so.FindProperty("buttons");
            list.arraySize = 5;
            for (int i = 0; i < 5; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = rows[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static ActionButton CommandRow(RectTransform menu, string name, Vector2 position, Vector2 size, bool small)
        {
            var row = New(name, menu);
            row.anchorMin = row.anchorMax = row.pivot = new Vector2(1f, 0f);
            row.anchoredPosition = position;
            row.sizeDelta = size;
            var bg = row.gameObject.AddComponent<Image>();
            bg.sprite = square; bg.color = PlateColor;
            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.None;   // ActionButton paints the states itself

            // Key badge: a stone diamond (square turned 45°), like a carved glyph. The key text is not rotated.
            float badgeSize = size.y - 15;
            float badgeCenter = size.y * 0.5f + 2;
            var badge = New("KeyBadge", row);
            badge.anchorMin = badge.anchorMax = new Vector2(0f, 0.5f);
            badge.pivot = new Vector2(0.5f, 0.5f);
            badge.anchoredPosition = new Vector2(badgeCenter, 0);
            badge.sizeDelta = new Vector2(badgeSize, badgeSize);
            badge.localRotation = Quaternion.Euler(0, 0, 45);
            var badgeImage = badge.gameObject.AddComponent<Image>();
            badgeImage.sprite = square; badgeImage.color = Gold; badgeImage.raycastTarget = false;
            var key = Label("Key", row, "", 15, MexicaPalette.Obsidian, TextAnchor.MiddleCenter,
                Vector2.zero, new Vector2(size.y, size.y), FontStyle.Bold);
            key.rectTransform.anchorMin = key.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            key.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            key.rectTransform.anchoredPosition = new Vector2(badgeCenter, 0);

            float textX = size.y + 4;
            var title = Label("Name", row, "", small ? 14 : 17, Bone, TextAnchor.MiddleLeft,
                new Vector2(textX, 0), new Vector2(size.x - textX - 70, size.y), FontStyle.BoldAndItalic);
            Middle(title.rectTransform);
            title.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.8f);
            var cost = Label("Cost", row, "", 12, Gold, TextAnchor.MiddleRight,
                new Vector2(size.x - 72, 0), new Vector2(64, size.y), FontStyle.Bold);
            Middle(cost.rectTransform);

            var line = New("Underline", row);
            line.anchorMin = new Vector2(0f, 0f); line.anchorMax = new Vector2(1f, 0f);
            line.pivot = new Vector2(0.5f, 0f);
            line.offsetMin = new Vector2(textX, 3); line.offsetMax = new Vector2(-6, 5);
            var lineImage = line.gameObject.AddComponent<Image>();
            lineImage.sprite = square; lineImage.color = Gold; lineImage.raycastTarget = false;

            var view = row.gameObject.AddComponent<ActionButton>();
            Set(view, "button", button); Set(view, "background", bg); Set(view, "keyBadge", badgeImage);
            Set(view, "underline", lineImage); Set(view, "keyLabel", key); Set(view, "nameLabel", title); Set(view, "costLabel", cost);
            return view;
        }

        static void BuildTurnOrder(RectTransform parent, CombatManager combat)
        {
            var strip = New("TurnOrder", parent);
            strip.anchorMin = strip.anchorMax = strip.pivot = new Vector2(0.5f, 1f);
            strip.anchoredPosition = new Vector2(0, -58);
            strip.sizeDelta = new Vector2(600, 26);
            var layout = strip.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8; layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            strip.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var entry = New("EntryTemplate", strip);
            var image = entry.gameObject.AddComponent<Image>();
            image.sprite = square; image.color = PlateColor;
            var entryLayout = entry.gameObject.AddComponent<HorizontalLayoutGroup>();
            entry.sizeDelta = new Vector2(100, 24);
            entryLayout.padding = new RectOffset(12, 12, 0, 0);
            entryLayout.childAlignment = TextAnchor.MiddleCenter;
            entryLayout.childControlWidth = true;
            entryLayout.childControlHeight = false;
            entryLayout.childForceExpandWidth = entryLayout.childForceExpandHeight = false;
            var text = Label("Text", entry, "", 12, Bone, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(80, 18), FontStyle.BoldAndItalic);
            entry.gameObject.SetActive(false);

            var bar = strip.gameObject.AddComponent<TurnOrderBar>();
            Set(bar, "combat", combat);
            Set(bar, "template", entry);
        }

        static void BuildEnemyBars(RectTransform parent, CombatManager combat)
        {
            var area = Stretch(New("EnemyBars", parent));
            var template = New("EnemyBarTemplate", area);
            template.sizeDelta = new Vector2(110, 34);
            var name = Label("Name", template, "", 11, Bone, TextAnchor.LowerCenter, new Vector2(0, 0), new Vector2(110, 16), FontStyle.BoldAndItalic);
            name.gameObject.AddComponent<Outline>().effectColor = Color.black;
            var bar = Bar("Bar", template, new Vector2(0, -16), new Vector2(110, 10), MexicaPalette.Cinnabar);
            var marker = Label("TargetMarker", template, "▼", 16, Gold, TextAnchor.LowerCenter, new Vector2(0, 16), new Vector2(110, 18));
            marker.gameObject.AddComponent<Outline>().effectColor = Color.black;
            foreach (var t in new[] { name.rectTransform, bar.transform as RectTransform, marker.rectTransform })
            { t.anchorMin = t.anchorMax = new Vector2(0.5f, 1f); t.pivot = new Vector2(0.5f, 1f); t.anchoredPosition = new Vector2(0, t.anchoredPosition.y); }
            bar.GetComponentInChildren<Text>().fontSize = 9;
            template.gameObject.SetActive(false);

            var bars = area.gameObject.AddComponent<EnemyHealthBars>();
            Set(bars, "combat", combat);
            Set(bars, "template", template);
        }

        static void BuildMessages(RectTransform root, PlayerCombatant player)
        {
            var area = Stretch(New("Messages", root));
            Plate("TurnPlate", area, new Vector2(0.5f, 1f), new Vector2(0, -14), 20, out var turn);
            turn.color = Gold;
            var bannerPlate = Plate("BannerPlate", area, new Vector2(0.5f, 0.5f), new Vector2(0, 95), 26, out var banner);
            bannerPlate.gameObject.AddComponent<CanvasGroup>();
            var promptPlate = Plate("PromptPlate", area, new Vector2(0.5f, 0.5f), new Vector2(0, 15), 34, out var prompt);
            promptPlate.GetComponent<Image>().color = MexicaPalette.Tezontle;   // urgent, but not a bright cartoon red
            prompt.color = MexicaPalette.Bone;
            var combo = Big("Combo", area, 24, MexicaPalette.Ochre, new Vector2(1f, 0.5f), new Vector2(-150, 120));
            var popup = Big("PopupTemplate", area, 22, MexicaPalette.Bone, new Vector2(0.5f, 0.5f), Vector2.zero);
            popup.rectTransform.sizeDelta = new Vector2(220, 40);

            var messages = area.gameObject.AddComponent<CombatMessages>();
            Set(messages, "player", player);
            Set(messages, "banner", banner); Set(messages, "turnLabel", turn); Set(messages, "prompt", prompt);
            Set(messages, "combo", combo); Set(messages, "popupTemplate", popup);
        }

        // ---------------- Helpers ----------------

        static RectTransform New(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static RectTransform Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        static void Middle(RectTransform rect)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, 0f);
        }

        /// <summary>Dark panel with a gold border, anchored and pivoted at 'corner'.</summary>
        static RectTransform Panel(string name, Transform parent, Vector2 corner, Vector2 position, Vector2 size)
        {
            var rect = New(name, parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = corner;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = square; image.color = Obsidian;
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.8f);
            outline.effectDistance = new Vector2(2, -2);
            return rect;
        }

        /// <summary>
        /// A dark plate that resizes to its text (with a thin gold rule below), so a message is
        /// always readable over the map. The plate is the Text's parent; it is hidden when empty.
        /// </summary>
        static RectTransform Plate(string name, Transform parent, Vector2 anchor, Vector2 position, int fontSize, out Text text)
        {
            var plate = New(name, parent);
            plate.anchorMin = plate.anchorMax = plate.pivot = anchor;
            plate.pivot = new Vector2(0.5f, anchor.y >= 1f ? 1f : anchor.y <= 0f ? 0f : 0.5f);
            plate.anchoredPosition = position;
            var image = plate.gameObject.AddComponent<Image>();
            image.sprite = square; image.color = PlateColor; image.raycastTarget = false;
            var outline = plate.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.55f);
            outline.effectDistance = new Vector2(0, -2);
            var layout = plate.gameObject.AddComponent<HorizontalLayoutGroup>();
            // Width follows the text; the height is fixed (legacy Text reports unreliable preferred heights).
            layout.padding = new RectOffset(18, 18, 0, 0);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var fitter = plate.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            plate.sizeDelta = new Vector2(200, fontSize + 16);

            text = Label("Text", plate, "", fontSize, Bone, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(100, fontSize + 8), FontStyle.BoldAndItalic);
            text.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.9f);
            // A thin merlon frieze hanging under the plate (outside the layout).
            var frieze = New("Frieze", plate);
            frieze.anchorMin = new Vector2(0f, 0f); frieze.anchorMax = new Vector2(1f, 0f);
            frieze.pivot = new Vector2(0.5f, 1f);
            frieze.anchoredPosition = new Vector2(0, -1);
            frieze.sizeDelta = new Vector2(0, 9);
            frieze.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            TiledGreca(frieze.gameObject, frieze.sizeDelta.y);
            plate.gameObject.SetActive(false);
            return plate;
        }

        static Text Label(string name, Transform parent, string text, int size, Color color, TextAnchor anchor,
                          Vector2 position, Vector2 boxSize, FontStyle style = FontStyle.Normal)
        {
            var rect = New(name, parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = boxSize;
            var t = rect.gameObject.AddComponent<Text>();
            t.font = font; t.fontSize = size; t.color = color; t.alignment = anchor; t.text = text; t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        static Text Big(string name, Transform parent, int size, Color color, Vector2 anchor, Vector2 position)
        {
            var rect = New(name, parent);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(820, size * 2.6f);
            var t = rect.gameObject.AddComponent<Text>();
            t.font = font; t.fontSize = size; t.fontStyle = FontStyle.BoldAndItalic; t.color = color;
            t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black; outline.effectDistance = new Vector2(2, -2);
            return t;
        }

        static UIBar Bar(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var rect = New(name, parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var back = rect.gameObject.AddComponent<Image>();
            back.sprite = square; back.color = new Color(0f, 0f, 0f, 0.7f);

            var trail = Filled("Trail", rect, MexicaPalette.WithAlpha(MexicaPalette.Bone, 0.7f));
            var fill = Filled("Fill", rect, color);
            var label = Label("Label", rect, "", 11, MexicaPalette.Bone, TextAnchor.MiddleLeft, new Vector2(5, 0), size);
            label.gameObject.AddComponent<Outline>().effectColor = Color.black;

            var bar = rect.gameObject.AddComponent<UIBar>();
            Set(bar, "fill", fill); Set(bar, "trail", trail); Set(bar, "label", label);
            return bar;
        }

        static Image Filled(string name, Transform parent, Color color)
        {
            var rect = Stretch(New(name, parent));
            rect.offsetMin = new Vector2(2, 2); rect.offsetMax = new Vector2(-2, -2);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = square; image.color = color; image.raycastTarget = false;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = 0;
            return image;
        }

        // ---------------- Mexica ornaments (textures generated inside Unity) ----------------

        /// <summary>Strip of greca along the top or bottom edge, inside a panel.</summary>
        static void Greca(RectTransform panel, bool bottom, float height)
        {
            var strip = New("Frieze", panel);
            float y = bottom ? 0f : 1f;
            strip.anchorMin = new Vector2(0f, y); strip.anchorMax = new Vector2(1f, y);
            strip.pivot = new Vector2(0.5f, y);
            strip.offsetMin = new Vector2(6, bottom ? 4 : -4 - height);
            strip.offsetMax = new Vector2(-6, bottom ? 4 + height : -4);
            TiledGreca(strip.gameObject, height);
        }

        static void TiledGreca(GameObject go, float height)
        {
            var image = go.AddComponent<Image>();
            image.sprite = greca;
            image.type = Image.Type.Tiled;
            image.pixelsPerUnitMultiplier = 14f / Mathf.Max(1f, height);   // one row of merlons fills the strip height
            image.color = MexicaPalette.DarkGold;
            image.raycastTarget = false;
        }

        /// <summary>
        /// Stepped merlons (almenas), the crest of the Templo Mayor: a stepped pyramid repeated along a base line.
        /// 12 × 7 px, point-filtered, tinted by the Image colour.
        /// </summary>
        static Sprite EnsureGreca() => EnsureTexture(GrecaPath, 12, 7, FilterMode.Point, TextureWrapMode.Repeat, 50, (x, y) =>
        {
            if (y == 0) return true;                                   // base line
            int tier = Mathf.FloorToInt(Mathf.Abs(x - 5.5f) / 2f);     // 0 centre, 1, 2 edges
            return y < 7 - 2 * tier && y > 1;                          // stepped pyramid with a gap above the base
        }, solidAlpha: 1f);

        /// <summary>Transparent centre, dark edges.</summary>
        static Sprite EnsureVignette() => EnsureTexture(VignettePath, 128, 72, FilterMode.Bilinear, TextureWrapMode.Clamp, 100, null, solidAlpha: 0f);

        static Sprite EnsureTexture(string path, int w, int h, FilterMode filter, TextureWrapMode wrap, float ppu,
                                    System.Func<int, int, bool> pattern, float solidAlpha)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float a;
                    if (pattern != null) a = pattern(x, y) ? solidAlpha : 0f;
                    else
                    {
                        // Vignette: elliptical distance from the centre.
                        float dx = (x + 0.5f) / w * 2f - 1f, dy = (y + 0.5f) / h * 2f - 1f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.4142f;
                        a = 0.3f + Mathf.SmoothStep(0f, 0.6f, Mathf.InverseLerp(0.25f, 1f, d));   // the whole arena dims, edges much more
                    }
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = filter;
            importer.wrapMode = wrap;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
