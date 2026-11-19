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

        void OnEnable()
        {
            Present = true;
            CombatFeed.QuickTime += Show;
            Show(null, null);
        }

        void OnDisable()
        {
            Present = false;
            CombatFeed.QuickTime -= Show;
        }

        void Show(string verb, string key)
        {
            bool visible = !string.IsNullOrEmpty(verb);
            group.alpha = visible ? 1f : 0f;
            if (!visible) return;
            before.text = "Press";
            keyLabel.text = key;
            after.text = "to " + verb + "!";
            // Dodge = gold box, interrupt = turquoise box.
            Color keyboardColor = verb == "INTERRUPT" ? new Color(0.2f, 0.75f, 0.85f)
                                : verb == "STRIKE" ? new Color(0.9f, 0.3f, 0.25f) : new Color(0.95f, 0.75f, 0.2f);
            keyBoxImage.color = InputPrompts.BoxColor(key, keyboardColor);   // Xbox colours when using a controller
        }

        void Update()
        {
            if (group.alpha <= 0f) return;
            float pulse = 1f + 0.15f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 12f));
            keyBox.localScale = Vector3.one * pulse;
        }
    }
}
