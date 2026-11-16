using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// Creates in the open scene the canvas "GameFlowUI" (hints, Game Over and credits) and the SaveController
    /// (tasks 4.7, 4.18, 4.19). Menu: TD_Game > Build Game Flow UI. After that, everything is edited in the Inspector.
    /// To rebuild it, delete GameFlowUI and run the menu again.
    /// </summary>
    public static class GameFlowUIBuilder
    {
        static Font font;

        [MenuItem("TD_Game/Build Game Flow UI")]
        public static void Build()
        {
            EnsureSaveController();
            if (GameObject.Find("GameFlowUI") != null) { Debug.LogWarning("GameFlowUI already exists. Delete it to rebuild."); return; }
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("GameFlowUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGo, "Build Game Flow UI");
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;   // above the combat HUD
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 540);
            scaler.matchWidthOrHeight = 1f;
            var root = (RectTransform)canvasGo.transform;
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

            BuildHints(root);
            BuildDefeat(root);
            BuildCredits(root);
            EditorSceneManager.MarkSceneDirty(canvasGo.scene);
        }

        static void EnsureSaveController()
        {
            if (Object.FindAnyObjectByType<SaveController>() != null) return;
            var go = new GameObject("SaveController");
            Undo.RegisterCreatedObjectUndo(go, "Add SaveController");
            var save = go.AddComponent<SaveController>();
            var so = new SerializedObject(save);
            so.FindProperty("player").objectReferenceValue = Object.FindAnyObjectByType<PlayerCombatant>();
            var list = so.FindProperty("allAbilities");
            string[] guids = AssetDatabase.FindAssets("t:CombatAbility", new[] { "Assets/TD_Game" });
            list.arraySize = guids.Length;
            for (int i = 0; i < guids.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<CombatAbility>(AssetDatabase.GUIDToAssetPath(guids[i]));
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Hint box at the top centre.
        static void BuildHints(RectTransform root)
        {
            var box = New("TutorialHint", root);
            box.anchorMin = box.anchorMax = new Vector2(0.5f, 1f);
            box.pivot = new Vector2(0.5f, 1f);
            box.anchoredPosition = new Vector2(0f, -70f);
            box.sizeDelta = new Vector2(640f, 64f);
            Plate(box, MexicaPalette.WithAlpha(MexicaPalette.ObsidianPlate, 0.92f));
            var group = box.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f; group.blocksRaycasts = false;
            var text = Label("Text", box, "", 13, MexicaPalette.Bone);
            Inset((RectTransform)text.transform, 12f);

            var tutorial = box.gameObject.AddComponent<ContextTutorial>();
            var so = new SerializedObject(tutorial);
            so.FindProperty("panel").objectReferenceValue = group;
            so.FindProperty("text").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BuildDefeat(RectTransform root)
        {
            var screen = Stretch(New("GameOverScreen", root));
            Plate(screen, MexicaPalette.WithAlpha(MexicaPalette.Obsidian, 0.9f));
            var group = screen.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f; group.blocksRaycasts = false;

            Place(Label("Title", screen, "YOU HAVE FALLEN", 44, MexicaPalette.Cinnabar), 0f, 110f, 800f, 60f);
            Place(Label("Subtitle", screen, "Mictlán claims another soul... but Tlahuelli rises again.", 16, MexicaPalette.Ash), 0f, 60f, 800f, 30f);
            var tip = Label("Tip", screen, "TIP", 14, MexicaPalette.Bone);
            Place(tip, 0f, 5f, 700f, 50f);

            var retry = MakeButton("RetryButton", screen, "RETRY FROM CHECKPOINT   [ENTER]", 0f, -70f);
            var menu = MakeButton("MenuButton", screen, "MAIN MENU   [ESC]", 0f, -125f);

            var defeat = screen.gameObject.AddComponent<DefeatScreen>();
            var so = new SerializedObject(defeat);
            so.FindProperty("panel").objectReferenceValue = group;
            so.FindProperty("tipText").objectReferenceValue = tip;
            so.FindProperty("retryButton").objectReferenceValue = retry;
            so.FindProperty("menuButton").objectReferenceValue = menu;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BuildCredits(RectTransform root)
        {
            var screen = Stretch(New("CreditsScreen", root));
            Plate(screen, Color.black);
            var group = screen.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f; group.blocksRaycasts = false;

            var area = Stretch(New("ScrollArea", screen));
            area.gameObject.AddComponent<RectMask2D>();
            var text = Label("CreditsText", area, "", 20, MexicaPalette.Bone);
            var rect = (RectTransform)text.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(700f, 1100f);
            rect.anchoredPosition = new Vector2(0f, -1000f);
            text.alignment = TextAnchor.MiddleCenter;

            var credits = screen.gameObject.AddComponent<CreditsScreen>();
            var so = new SerializedObject(credits);
            so.FindProperty("panel").objectReferenceValue = group;
            so.FindProperty("scrollingText").objectReferenceValue = rect;
            so.FindProperty("creditsText").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------- Small helpers ----------------

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

        static void Inset(RectTransform rect, float padding)
        {
            Stretch(rect);
            rect.offsetMin = new Vector2(padding, padding * 0.5f);
            rect.offsetMax = new Vector2(-padding, -padding * 0.5f);
        }

        static Image Plate(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        static Text Label(string name, RectTransform parent, string value, int size, Color color)
        {
            var text = New(name, parent).gameObject.AddComponent<Text>();
            text.font = font; text.fontSize = size; text.color = color; text.text = value;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        static void Place(Graphic graphic, float x, float y, float width, float height)
        {
            var rect = (RectTransform)graphic.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        static Button MakeButton(string name, RectTransform parent, string label, float x, float y)
        {
            var rect = New(name, parent);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(360f, 42f);
            var image = Plate(rect, MexicaPalette.ObsidianPlate);
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = MexicaPalette.AgedGold;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = MexicaPalette.Tezontle;
            colors.pressedColor = MexicaPalette.Cinnabar;
            button.colors = colors;
            Inset((RectTransform)Label("Label", rect, label, 15, MexicaPalette.Bone).transform, 6f);
            return button;
        }
    }
}
