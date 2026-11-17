using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// Small helpers to build the menus by code (adapted from Mictlán v5's UIKit), with the Mexica palette.
    /// Used by BootScreens, TitleMenu and PauseScreen.
    /// </summary>
    public static class MenuWidgets
    {
        static Font font;
        static bool aztecFrames;
        public static Font Font => font != null ? font : (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        /// <summary>Menu style: the artistic font (Pirata One) and the Aztec frames of Mictlán v5. Call before building.</summary>
        public static void Use(Font menuFont, bool useAztecFrames)
        {
            if (menuFont != null) font = menuFont;
            aztecFrames = useAztecFrames;
        }

        /// <summary>A full-screen canvas (1920x1080 reference) under the parent.</summary>
        public static RectTransform Screen(string name, int sortingOrder, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            EnsureEventSystem();
            return (RectTransform)go.transform;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            return r;
        }

        public static void Center(RectTransform r, Vector2 position, Vector2 size)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = position;
            r.sizeDelta = size;
        }

        public static Image Image(string name, Transform parent, Color color)
        {
            var image = Rect(name, parent).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static RawImage Picture(string name, Transform parent, Texture texture)
        {
            var raw = Rect(name, parent).gameObject.AddComponent<RawImage>();
            raw.texture = texture;
            raw.raycastTarget = false;
            return raw;
        }

        public static Text Label(string name, Transform parent, string text, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var t = Rect(name, parent).gameObject.AddComponent<Text>();
            t.font = Font; t.text = text; t.fontSize = size; t.color = color; t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Dark stone plate with a gold edge; red when selected. Plays the menu sounds.</summary>
        public static Button Button(Transform parent, string text, Vector2 position, Vector2 size, UnityAction onClick, int fontSize = 30)
        {
            var r = Rect("Button_" + text, parent);
            Center(r, position, size);
            var image = r.gameObject.AddComponent<Image>();
            var button = r.gameObject.AddComponent<Button>();
            if (aztecFrames)
            {
                // Aztec frame of Mictlán v5: one picture per state (normal, highlight, selected, pressed, disabled).
                AztecFrames.StyleButton(button, image, size.x, size.y);
            }
            else
            {
                image.color = Color.white;
                var outline = r.gameObject.AddComponent<Outline>();
                outline.effectColor = MexicaPalette.AgedGold;
                outline.effectDistance = new Vector2(2f, -2f);
                button.targetGraphic = image;
                var colors = button.colors;
                colors.normalColor = MexicaPalette.ObsidianPlate;
                colors.highlightedColor = MexicaPalette.Cinnabar;
                colors.selectedColor = MexicaPalette.Cinnabar;
                colors.pressedColor = MexicaPalette.Tezontle;
                colors.disabledColor = MexicaPalette.WithAlpha(MexicaPalette.ObsidianPlate, 0.5f);
                button.colors = colors;
            }
            button.onClick.AddListener(() => GameAudio.PlaySfx("ui_select", 1f, 0f));
            button.onClick.AddListener(onClick);
            r.gameObject.AddComponent<MenuSelectSound>();

            var label = Label("Label", r, text, fontSize, MexicaPalette.Bone);
            Stretch(label.rectTransform);
            return button;
        }

        /// <summary>A panel with the Aztec frame (credits, shop, prologue captions).</summary>
        public static Image Panel(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var image = Image(name, parent, Color.white);
            Center(image.rectTransform, position, size);
            AztecFrames.StylePanel(image, size.x, size.y);
            return image;
        }

        public static void SetLabel(Button button, string text) => button.GetComponentInChildren<Text>().text = text;

        public static void Select(GameObject go)
        {
            if (EventSystem.current == null || go == null) return;
            MenuSelectSound.MuteFor(0.1f);
            EventSystem.current.SetSelectedGameObject(go);
        }

        /// <summary>Any key, click or gamepad button this frame.</summary>
        public static bool AnyPress()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            var pad = Gamepad.current;
            return (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
                || (mouse != null && mouse.leftButton.wasPressedThisFrame)
                || (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame));
        }

        public static bool CancelPressed()
        {
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            return (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || (pad != null && pad.buttonEast.wasPressedThisFrame);
        }

        public static bool PausePressed()
        {
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            return (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || (pad != null && pad.startButton.wasPressedThisFrame);
        }
    }
}
