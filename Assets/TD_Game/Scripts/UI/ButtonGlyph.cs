using UnityEngine;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// One button icon in the UI, for an ACTION ("Dodge", "Next", "Interact"...) or a raw key ("1", "Q").
    /// It shows the keyboard key, the Xbox button or the PlayStation symbol of the device the player is using,
    /// and changes by itself when they switch (InputPrompts.Changed). Its width fits the label (SPACE is wider).
    /// Create it from code with ButtonGlyph.Create, or add it to an empty UI object and set Action in the Inspector.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ButtonGlyph : MonoBehaviour
    {
        [Tooltip("Action (Dodge, Attack, Interact, Next, Skip...) or keyboard key (1, Q, E).")]
        [SerializeField] string action = "Dodge";
        [SerializeField, Min(8f)] float height = 40f;

        Image image;
        Text label;
        RectTransform rect;

        public string Action => action;
        public float Width => rect != null ? rect.sizeDelta.x : height;
        public float Height => height;

        public static ButtonGlyph Create(Transform parent, string action, float height)
        {
            var go = new GameObject("Glyph_" + action, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var glyph = go.AddComponent<ButtonGlyph>();
            glyph.action = action;
            glyph.height = height;
            glyph.Refresh();
            return glyph;
        }

        public void SetAction(string newAction)
        {
            if (newAction == action && image != null) return;
            action = newAction;
            Refresh();
        }

        public void SetHeight(float newHeight)
        {
            height = newHeight;
            Refresh();
        }

        void OnEnable()
        {
            InputPrompts.Changed += Refresh;
            Refresh();
        }

        void OnDisable() => InputPrompts.Changed -= Refresh;

        void Build()
        {
            rect = (RectTransform)transform;
            image = GetComponent<Image>();
            if (image == null) image = gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.layer = gameObject.layer;
            textGo.transform.SetParent(transform, false);
            label = textGo.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            var r = label.rectTransform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(0f, height * 0.06f); r.offsetMax = Vector2.zero;   // a bit up: the bottom band is the key's depth
            label.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        }

        public void Refresh()
        {
            if (image == null) Build();
            string text = InputPrompts.Label(action);
            float scale = Mathf.Max(1f, Mathf.Round(height / ButtonGlyphs.Size));   // integer pixel scale
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.pixelsPerUnitMultiplier = 1f;
            label.text = "";
            label.color = MexicaPalette.Bone;
            float width = height;

            if (ButtonGlyphs.IsPlayStationShape(text))
                image.sprite = ButtonGlyphs.PlayStation(text);
            else if (InputPrompts.Scheme == InputScheme.Xbox && ButtonGlyphs.IsFaceButton(text))
            {
                image.sprite = ButtonGlyphs.Face(InputPrompts.BoxColor(text, MexicaPalette.AgedGold));
                label.text = text;
                label.fontSize = Mathf.RoundToInt(height * 0.55f);
            }
            else if (text == "CLICK")
            {
                image.sprite = ButtonGlyphs.Mouse();
                width = height * 0.7f;
            }
            else
            {
                // Keyboard key, or a controller button with a name (LB, R1, START, D-PAD...): a plate that fits the text.
                image.sprite = InputPrompts.UseGamepad ? ButtonGlyphs.Pill() : ButtonGlyphs.Keycap();
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1f / scale;
                if (text.StartsWith("D-PAD ")) text = text.Substring(6);   // on a stone plate the arrow is enough: compact
                label.text = text;
                label.fontSize = Mathf.RoundToInt(height * (text.Length <= 2 ? 0.5f : 0.36f));
                width = Mathf.Max(height, label.preferredWidth + height * 0.55f);
            }
            rect.sizeDelta = new Vector2(Mathf.Round(width), height);
        }
    }
}
