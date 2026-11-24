using UnityEngine;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// Quick time event prompt, like in many PlayStation games: "Press [A] to DODGE!".
    /// The key sits in a coloured square that pulses so it stands out. It listens to CombatFeed.QuickTime.
    /// </summary>
    public class QuickTimePrompt : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] Text before;      // "Press"
        [SerializeField] RectTransform keyBox;
        [SerializeField] Image keyBoxImage;
        [SerializeField] Text keyLabel;    // "A"
        [SerializeField] Text after;       // "to DODGE!"

        /// <summary>True while there is one in the scene (the old text prompt then stays hidden).</summary>
        public static bool Present { get; private set; }

        ButtonGlyph glyph;

        void OnEnable()
        {
            Present = true;
            CombatFeed.QuickTime += Show;
            InputPrompts.Changed += Refit;
            Show(null, null);
        }

        void OnDisable()
        {
            Present = false;
            CombatFeed.QuickTime -= Show;
            InputPrompts.Changed -= Refit;
        }

        /// <summary>key = the ACTION ("Dodge", "Attack"): the box shows its keyboard key, Xbox or PlayStation button.</summary>
        void Show(string verb, string key)
        {
            bool visible = !string.IsNullOrEmpty(verb);
            group.alpha = visible ? 1f : 0f;
            if (!visible) return;
            before.text = "Press";
            after.text = "to " + verb + "!";
            if (glyph == null)
            {
                glyph = ButtonGlyph.Create(keyBox, key, Mathf.Round(keyBox.sizeDelta.y - 12f));
                keyLabel.enabled = false;
            }
            glyph.SetAction(key);
            // The icon sits in a coloured frame: gold = dodge, turquoise = interrupt, red = strike.
            keyBoxImage.color = verb == "INTERRUPT" ? new Color(0.2f, 0.75f, 0.85f)
                              : verb == "STRIKE" ? new Color(0.9f, 0.3f, 0.25f) : new Color(0.95f, 0.75f, 0.2f);
            FitKey();
        }

        // The player switched device while the prompt is up: the icon changed size.
        void Refit()
        {
            if (glyph == null || group.alpha <= 0f) return;
            glyph.Refresh();
            FitKey();
        }

        // The box was made for one letter: a wide icon ("SPACE") gets a wider box, and "to DODGE!" moves right.
        bool measured;
        float keyLeft, keyHeight, gap, plateHeight;

        void FitKey()
        {
            var after = this.after.rectTransform;
            var plate = (RectTransform)transform;
            if (!measured)
            {
                measured = true;
                keyLeft = keyBox.anchoredPosition.x - keyBox.sizeDelta.x / 2f;
                keyHeight = keyBox.sizeDelta.y;
                gap = after.anchoredPosition.x - after.sizeDelta.x / 2f - (keyBox.anchoredPosition.x + keyBox.sizeDelta.x / 2f);
                plateHeight = plate.sizeDelta.y;
            }
            float width = Mathf.Max(keyHeight, glyph.Width + 12f);
            keyBox.sizeDelta = new Vector2(width, keyHeight);
            keyBox.anchoredPosition = new Vector2(keyLeft + width / 2f, keyBox.anchoredPosition.y);
            this.after.alignment = TextAnchor.MiddleLeft;    // the words start right after the icon
            before.alignment = TextAnchor.MiddleRight;
            float afterX = keyLeft + width + gap + after.sizeDelta.x / 2f;
            after.anchoredPosition = new Vector2(afterX, after.anchoredPosition.y);
            plate.sizeDelta = new Vector2(Mathf.Max(420f, 2f * (afterX + after.sizeDelta.x / 2f)), plateHeight);
        }

        void Update()
        {
            if (group.alpha <= 0f) return;
            float pulse = 1f + 0.15f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 12f));
            keyBox.localScale = Vector3.one * pulse;
        }
    }
}
