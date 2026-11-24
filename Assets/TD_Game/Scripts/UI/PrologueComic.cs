using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// The backstory of Tlahuelli as a comic (GDD section 3: Prologue + Mictlantecuhtli's Offer).
    /// The panels appear one by one. NEXT (Space / Enter / click, A or CROSS) shows the next one; holding SKIP
    /// (ESC, B or CIRCLE) skips everything. Pressing NEXT while a panel is still fading in only finishes the fade,
    /// so one press never jumps two panels. The button help is not on the art all the time: it fades in at the
    /// start and whenever any key or button is pressed, and fades out after a few idle seconds. Its icons follow the
    /// device in use (ButtonGlyph). At the end the game starts.
    /// Every page, picture and text is edited in the Inspector, so the team can replace the pictures with the
    /// drawn comic art later without touching code.
    /// </summary>
    public class PrologueComic : MonoBehaviour
    {
        [System.Serializable]
        public class ComicPanel
        {
            public Texture picture;
            [Tooltip("Part of the picture to show (x, y, width, height from 0 to 1). (0,0,1,1) = all of it.")]
            public Rect crop = new Rect(0, 0, 1, 1);
            public Color tint = Color.white;
            [TextArea(2, 4)] public string caption;
        }

        [System.Serializable]
        public class ComicPage
        {
            public List<ComicPanel> panels = new List<ComicPanel>();
        }

        [SerializeField] Font menuFont;
        [SerializeField] List<ComicPage> pages = new List<ComicPage>();
        [SerializeField] string nextScene = SceneIds.MainMap;

        [Header("Button help")]
        [Tooltip("Seconds without input before the help fades out.")]
        [SerializeField, Min(0.5f)] float hintIdleSeconds = 3f;
        [Tooltip("How long SKIP must be held.")]
        [SerializeField, Min(0.1f)] float skipHoldSeconds = 1f;
        [SerializeField, Min(0.05f)] float panelFadeSeconds = 0.5f;

        RectTransform root;
        bool skip;
        int usedFrame = -1;   // the frame whose NEXT press was already used

        // The help in the corner.
        CanvasGroup hint;
        RectTransform hintPlate;
        ButtonGlyph nextGlyph, skipGlyph;
        Text nextText, skipText;
        Image skipFill;
        float hintShownUntil, held;

        void Start()
        {
            MenuWidgets.Use(menuFont, true);
            root = MenuWidgets.Screen("ComicCanvas", 10, transform);
            MenuWidgets.Stretch(MenuWidgets.Image("Black", root, Color.black).rectTransform);
            BuildHint();
            ShowHint();
            StartCoroutine(Play());
        }

        void OnEnable() => InputPrompts.Changed += LayoutHint;
        void OnDisable() => InputPrompts.Changed -= LayoutHint;

        void Update()
        {
            if (AnyInput()) ShowHint();

            // Hold to skip: the gold line under HOLD TO SKIP fills up.
            if (SkipHeld() && !skip)
            {
                held += Time.unscaledDeltaTime;
                ShowHint();
                if (held >= skipHoldSeconds) skip = true;
            }
            else held = 0f;
            if (skipFill != null) skipFill.fillAmount = Mathf.Clamp01(held / skipHoldSeconds);

            if (hint != null)
            {
                float target = Time.unscaledTime < hintShownUntil ? 1f : 0f;
                hint.alpha = Mathf.MoveTowards(hint.alpha, target, Time.unscaledDeltaTime / 0.25f);
            }
        }

        IEnumerator Play()
        {
            yield return null;
            foreach (var page in pages)
            {
                if (skip) break;
                var pageRoot = MenuWidgets.Stretch(MenuWidgets.Rect("Page", root));
                pageRoot.SetSiblingIndex(1);   // above the black background, under the help
                int count = page.panels.Count;
                float width = Mathf.Min(560f, 1700f / count);
                for (int i = 0; i < count; i++)
                {
                    if (skip) break;
                    float x = (i - (count - 1) / 2f) * (width + 30f);   // panels side by side, centred
                    var group = BuildPanel(pageRoot, page.panels[i], new Vector2(x, 20f), new Vector2(width, 700f));
                    yield return FadeIn(group);
                    yield return WaitForNext();
                }
                if (!skip) yield return Fade(pageRoot.gameObject.AddComponent<CanvasGroup>(), 0f, 0.4f);
                Destroy(pageRoot.gameObject);
            }
            SceneFader.Load(nextScene);
        }

        CanvasGroup BuildPanel(RectTransform parent, ComicPanel panel, Vector2 position, Vector2 size)
        {
            var frame = MenuWidgets.Panel("Panel", parent, position, size);   // Aztec border of the comic panel
            var group = frame.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            if (panel.picture != null)
            {
                // The area for the picture, and the picture inside it keeping its proportions.
                var area = MenuWidgets.Rect("PictureArea", frame.transform);
                MenuWidgets.Center(area, new Vector2(0, 70), new Vector2(size.x - 40, size.y - 190));
                var picture = MenuWidgets.Picture("Picture", area, panel.picture);
                picture.uvRect = panel.crop;
                picture.color = panel.tint;
                var aspect = picture.gameObject.AddComponent<AspectRatioFitter>();
                aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                aspect.aspectRatio = panel.picture.width * panel.crop.width / (panel.picture.height * panel.crop.height);
            }

            if (!string.IsNullOrEmpty(panel.caption))
            {
                var caption = MenuWidgets.Label("Caption", frame.transform, panel.caption, 30, MexicaPalette.Bone);
                MenuWidgets.Center(caption.rectTransform, new Vector2(0, -size.y / 2f + 75f), new Vector2(size.x - 50, 130));
            }
            return group;
        }

        /// <summary>The panel fades in; a NEXT press during the fade just finishes it (and is used up).</summary>
        IEnumerator FadeIn(CanvasGroup group)
        {
            for (float t = 0f; t < panelFadeSeconds && !skip; t += Time.unscaledDeltaTime)
            {
                if (TakeNext()) break;
                group.alpha = Mathf.SmoothStep(0f, 1f, t / panelFadeSeconds);
                yield return null;
            }
            group.alpha = 1f;
        }

        IEnumerator WaitForNext()
        {
            yield return null;   // never the same frame as the press that finished the fade
            while (!skip && !TakeNext()) yield return null;
        }

        /// <summary>True once per press: the frame is remembered so the same press is never used twice.</summary>
        bool TakeNext()
        {
            if (Time.frameCount == usedFrame || !NextPressed()) return false;
            usedFrame = Time.frameCount;
            return true;
        }

        static IEnumerator Fade(CanvasGroup group, float target, float seconds)
        {
            float start = group.alpha;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(start, target, t / seconds);
                yield return null;
            }
            group.alpha = target;
        }

        // ---------------- input ----------------

        static bool NextPressed()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame
                                     || keyboard.numpadEnterKey.wasPressedThisFrame)) return true;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;
            foreach (var pad in Gamepad.all) if (pad.buttonSouth.wasPressedThisFrame) return true;
            return false;
        }

        static bool SkipHeld()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.isPressed) return true;
            foreach (var pad in Gamepad.all) if (pad.buttonEast.isPressed) return true;
            return false;
        }

        static bool AnyInput()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame) return true;
            if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)) return true;
            foreach (var pad in Gamepad.all)
                if (pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame
                    || pad.buttonNorth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame
                    || pad.leftStick.ReadValue().magnitude > 0.5f || pad.dpad.ReadValue().magnitude > 0.5f) return true;
            return false;
        }

        // ---------------- the help in the corner ----------------

        void ShowHint() => hintShownUntil = Time.unscaledTime + hintIdleSeconds;

        /// <summary>A dark plate with a gold edge, bottom right: [NEXT icon] NEXT    [SKIP icon] HOLD TO SKIP.</summary>
        void BuildHint()
        {
            hintPlate = MenuWidgets.Rect("ButtonHelp", root);
            hintPlate.anchorMin = hintPlate.anchorMax = hintPlate.pivot = new Vector2(1f, 0f);
            hintPlate.anchoredPosition = new Vector2(-36f, 30f);
            var plate = hintPlate.gameObject.AddComponent<Image>();
            plate.color = MexicaPalette.WithAlpha(MexicaPalette.ObsidianPlate, 0.88f);
            plate.raycastTarget = false;
            hint = hintPlate.gameObject.AddComponent<CanvasGroup>();
            hint.alpha = 0f;
            hint.blocksRaycasts = hint.interactable = false;

            var edge = MenuWidgets.Image("GoldEdge", hintPlate, MexicaPalette.AgedGold);
            edge.rectTransform.anchorMin = new Vector2(0f, 1f);
            edge.rectTransform.anchorMax = Vector2.one;
            edge.rectTransform.pivot = new Vector2(0.5f, 1f);
            edge.rectTransform.sizeDelta = new Vector2(0f, 3f);

            nextGlyph = ButtonGlyph.Create(hintPlate, "Next", 40f);
            nextText = MenuWidgets.Label("NextText", hintPlate, "NEXT", 26, MexicaPalette.Bone, TextAnchor.MiddleLeft);
            skipGlyph = ButtonGlyph.Create(hintPlate, "Skip", 40f);
            skipText = MenuWidgets.Label("SkipText", hintPlate, "HOLD TO SKIP", 26, MexicaPalette.Bone, TextAnchor.MiddleLeft);
            skipFill = MenuWidgets.Image("SkipFill", hintPlate, MexicaPalette.AgedGold);
            skipFill.sprite = ButtonGlyphs.Pill();   // Filled needs a sprite
            skipFill.type = Image.Type.Filled;
            skipFill.fillMethod = Image.FillMethod.Horizontal;
            skipFill.fillAmount = 0f;
            LayoutHint();
        }

        /// <summary>Places the icons and words left to right; called again when the icons change size.</summary>
        void LayoutHint()
        {
            if (hintPlate == null) return;
            nextGlyph.Refresh();
            skipGlyph.Refresh();
            const float pad = 22f, gap = 12f, space = 40f, height = 64f;
            float x = pad;
            Place((RectTransform)nextGlyph.transform, ref x, nextGlyph.Width, gap);
            Place(nextText.rectTransform, ref x, nextText.preferredWidth, space);
            Place((RectTransform)skipGlyph.transform, ref x, skipGlyph.Width, gap);
            float skipStart = x;
            Place(skipText.rectTransform, ref x, skipText.preferredWidth, 0f);
            hintPlate.sizeDelta = new Vector2(x + pad, height);

            var fill = skipFill.rectTransform;   // a thin line under the words HOLD TO SKIP
            fill.anchorMin = fill.anchorMax = fill.pivot = new Vector2(0f, 0.5f);
            fill.anchoredPosition = new Vector2(skipStart, -19f);
            fill.sizeDelta = new Vector2(skipText.preferredWidth, 4f);
        }

        static void Place(RectTransform r, ref float x, float width, float after)
        {
            float h = r.sizeDelta.y > 0f ? r.sizeDelta.y : 40f;
            r.anchorMin = r.anchorMax = new Vector2(0f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            r.anchoredPosition = new Vector2(x, 0f);
            r.sizeDelta = new Vector2(width, h);
            x += width + after;
        }
    }
}
