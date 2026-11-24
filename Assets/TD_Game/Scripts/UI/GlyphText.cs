using UnityEngine;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// A text prompt that starts with a button: "{Interact}  Talk to the merchant" (or the old "[E]  Talk...").
    /// The button becomes a ButtonGlyph drawn just left of the words, and follows the device in use.
    /// Other {Action} tokens inside the sentence become plain [BUTTON] text. Use: GlyphText.Set(promptText, "...").
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class GlyphText : MonoBehaviour
    {
        Text text;
        ButtonGlyph glyph;
        string message = "";
        Vector2 home;
        bool measured;

        public static void Set(Text target, string message)
        {
            if (target == null) return;
            var g = target.GetComponent<GlyphText>();
            if (g == null) g = target.gameObject.AddComponent<GlyphText>();
            g.Show(message ?? "");
        }

        void OnEnable() => InputPrompts.Changed += Layout;
        void OnDisable() => InputPrompts.Changed -= Layout;

        public void Show(string newMessage)
        {
            message = newMessage;
            Layout();
        }

        void Layout()
        {
            if (text == null) text = GetComponent<Text>();
            if (!measured) { measured = true; home = text.rectTransform.anchoredPosition; }

            string action = null, rest = message;
            string trimmed = message.TrimStart();
            char open = trimmed.Length > 0 ? trimmed[0] : ' ';
            char close = open == '{' ? '}' : open == '[' ? ']' : ' ';
            int end = close != ' ' ? trimmed.IndexOf(close) : -1;
            if (end > 1)
            {
                action = trimmed.Substring(1, end - 1);
                rest = trimmed.Substring(end + 1).TrimStart();
            }

            text.text = InputPrompts.Localize(rest);
            if (action == null)
            {
                if (glyph != null) glyph.gameObject.SetActive(false);
                text.rectTransform.anchoredPosition = home;
                return;
            }

            if (glyph == null)
            {
                glyph = ButtonGlyph.Create(text.transform.parent, action, Mathf.Round(text.fontSize * 1.5f));
                var r = (RectTransform)glyph.transform;
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            }
            glyph.gameObject.SetActive(true);
            glyph.SetAction(action);
            glyph.Refresh();

            // The words and the icon, centred together where the text used to be.
            const float gap = 10f;
            float words = text.preferredWidth;
            float total = glyph.Width + gap + words;
            var g = (RectTransform)glyph.transform;
            g.anchoredPosition = home + new Vector2(-total / 2f + glyph.Width / 2f, 0f);
            text.rectTransform.anchoredPosition = home + new Vector2((glyph.Width + gap) / 2f, 0f);
        }
    }
}
