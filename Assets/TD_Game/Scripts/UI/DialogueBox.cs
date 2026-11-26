using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// The conversation box at the bottom of the screen (villagers, the prisoner of the witches).
    /// A portrait on the left, the name of who is speaking in gold and the text, written letter by letter.
    /// {Next} (Space / Enter / E / click / A) shows the whole line or goes to the next one; {Back} (ESC / B) ends the talk.
    /// While it is open the world stops (GameMode.Cutscene). One per level, built by code with the menu style.
    /// Usage: yield return DialogueBox.Play(lines);
    /// </summary>
    public class DialogueBox : MonoBehaviour
    {
        public struct Line
        {
            public string speaker;
            public string text;
            public Sprite portrait;
            public Line(string speaker, string text, Sprite portrait = null) { this.speaker = speaker; this.text = text; this.portrait = portrait; }
        }

        [SerializeField] Font font;
        [SerializeField, Min(5f)] float lettersPerSecond = 55f;

        static DialogueBox instance;
        public static bool IsOpen { get; private set; }

        GameObject root;
        Text speakerText, bodyText, hintText;
        Image portraitImage;
        GameObject portraitFrame;

        void Awake() => instance = this;

        void OnDestroy()
        {
            if (instance == this) instance = null;
            if (IsOpen) IsOpen = false;
        }

        public static IEnumerator Play(IList<Line> lines)
        {
            if (instance == null || lines == null || lines.Count == 0) yield break;
            yield return instance.Run(lines);
        }

        IEnumerator Run(IList<Line> lines)
        {
            if (root == null) Build();
            IsOpen = true;
            var previous = GameState.Mode;
            GameState.SetMode(GameMode.Cutscene);
            root.SetActive(true);
            GameAudio.PlaySfx("ui_select", 0.6f, 0f);
            yield return null;   // the button that opened the talk must not also skip the first line

            foreach (var line in lines)
            {
                speakerText.text = line.speaker ?? "";
                portraitFrame.SetActive(line.portrait != null);
                if (line.portrait != null) portraitImage.sprite = line.portrait;
                bodyText.rectTransform.offsetMin = new Vector2(line.portrait != null ? 250f : 50f, bodyText.rectTransform.offsetMin.y);
                string full = InputPrompts.Localize(line.text ?? "");

                // Letter by letter (a press shows the whole line at once).
                float shown = 0f;
                bool cancelled = false;
                while (shown < full.Length)
                {
                    if (MenuWidgets.CancelPressed()) { cancelled = true; break; }
                    if (NextPressed()) break;
                    shown += Time.unscaledDeltaTime * lettersPerSecond;
                    bodyText.text = full.Substring(0, Mathf.Min(full.Length, (int)shown));
                    if ((int)shown % 6 == 0) GameAudio.PlaySfx("ui_move", 0.12f, 0.2f);
                    yield return null;
                }
                bodyText.text = full;
                if (cancelled) break;
                yield return null;
                hintText.enabled = true;
                hintText.text = InputPrompts.Localize("{Next}  next");
                while (!NextPressed())
                {
                    if (MenuWidgets.CancelPressed()) { cancelled = true; break; }
                    yield return null;
                }
                hintText.enabled = false;
                if (cancelled) break;
                GameAudio.PlaySfx("ui_move", 0.5f, 0.05f);
                yield return null;
            }

            root.SetActive(false);
            yield return null;   // the ESC that closed it must not open the pause menu
            GameState.SetMode(previous == GameMode.Cutscene ? GameMode.Exploration : previous);
            IsOpen = false;
        }

        static bool NextPressed()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            var pad = Gamepad.current;
            return (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame))
                   || (mouse != null && mouse.leftButton.wasPressedThisFrame)
                   || (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame));
        }

        void Build()
        {
            MenuWidgets.Use(font, true);
            var canvas = MenuWidgets.Screen("DialogueCanvas", 45, transform);
            root = canvas.gameObject;

            var panel = MenuWidgets.Panel("Box", canvas, new Vector2(0, -360), new Vector2(1500, 300));
            var plate = MenuWidgets.Image("Plate", panel.transform, MexicaPalette.WithAlpha(MexicaPalette.Obsidian, 0.92f));
            MenuWidgets.Stretch(plate.rectTransform);
            plate.rectTransform.offsetMin = new Vector2(18, 18);
            plate.rectTransform.offsetMax = new Vector2(-18, -18);

            portraitFrame = MenuWidgets.Image("PortraitFrame", panel.transform, MexicaPalette.AgedGold).gameObject;
            var frameRect = (RectTransform)portraitFrame.transform;
            frameRect.anchorMin = frameRect.anchorMax = frameRect.pivot = new Vector2(0f, 0.5f);
            frameRect.anchoredPosition = new Vector2(44, 0);
            frameRect.sizeDelta = new Vector2(184, 214);
            var inside = MenuWidgets.Image("Inside", portraitFrame.transform, new Color(0.08f, 0.06f, 0.07f));
            MenuWidgets.Stretch(inside.rectTransform);
            inside.rectTransform.offsetMin = new Vector2(5, 5);
            inside.rectTransform.offsetMax = new Vector2(-5, -5);
            portraitImage = MenuWidgets.Image("Portrait", inside.transform, Color.white);
            MenuWidgets.Stretch(portraitImage.rectTransform);
            portraitImage.rectTransform.offsetMin = new Vector2(12, 8);
            portraitImage.rectTransform.offsetMax = new Vector2(-12, -8);
            portraitImage.preserveAspect = true;

            speakerText = MenuWidgets.Label("Speaker", panel.transform, "", 40, MexicaPalette.AgedGold, TextAnchor.UpperLeft);
            var s = speakerText.rectTransform;
            s.anchorMin = new Vector2(0, 1); s.anchorMax = new Vector2(1, 1); s.pivot = new Vector2(0, 1);
            s.offsetMin = new Vector2(250, -86); s.offsetMax = new Vector2(-40, -30);

            bodyText = MenuWidgets.Label("Text", panel.transform, "", 32, MexicaPalette.Bone, TextAnchor.UpperLeft);
            var b = bodyText.rectTransform;
            b.anchorMin = Vector2.zero; b.anchorMax = Vector2.one;
            b.offsetMin = new Vector2(250, 40); b.offsetMax = new Vector2(-50, -92);
            bodyText.lineSpacing = 1.1f;

            hintText = MenuWidgets.Label("Next", panel.transform, "", 26, MexicaPalette.Ash, TextAnchor.LowerRight);
            var h = hintText.rectTransform;
            h.anchorMin = h.anchorMax = h.pivot = new Vector2(1, 0);
            h.anchoredPosition = new Vector2(-40, 30);
            h.sizeDelta = new Vector2(300, 40);
            hintText.enabled = false;
            root.SetActive(false);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
            IsOpen = false;
        }
    }
}
