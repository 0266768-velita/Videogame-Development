using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// First screens of the game (adapted from Mictlán v5's BootSplash):
    /// studio logo -> age rating -> description of the game -> main menu.
    /// Any key, click or gamepad button skips the current screen (after half a second).
    /// Everything (logo, rating image, texts, times) is edited in the Inspector of the TD_Boot scene.
    /// </summary>
    public class BootScreens : MonoBehaviour
    {
        [SerializeField] Font menuFont;          // Pirata One

        [Header("1) Studio logo")]
        [SerializeField] Texture2D studioLogo;
        [SerializeField] string studioName = "MICTA FORGE";
        [SerializeField, Min(0.5f)] float logoSeconds = 3f;

        [Header("2) Age rating (optional: assign the official image)")]
        [SerializeField] Texture2D ratingImage;
        [SerializeField] string ratingLetter = "M";
        [SerializeField] string ratingLabel = "MATURE 17+";
        [SerializeField, TextArea(2, 6)] string contentDescriptors = "Blood and Gore\nIntense Violence\nMature Themes";
        [SerializeField, Min(0.5f)] float ratingSeconds = 3.5f;

        [Header("3) Description of the game")]
        [SerializeField] string descriptionTitle = "MICTLÁN";
        [SerializeField, TextArea(4, 10)] string description =
            "Tlahuelli, a Mexica warrior, falls into Mictlán, the land of the dead.\n" +
            "Cross its dark regions, defeat wolves, witches and giants in turn-based battles\n" +
            "and face Quetzalcóatl to find the way back.\n\n" +
            "Mictlán is an original work inspired by Mexica mythology.\n" +
            "Characters and events shown here are fictional.";
        [SerializeField, Min(0.5f)] float descriptionSeconds = 6f;

        [Header("Next scene")]
        [SerializeField] string nextScene = SceneIds.MainMenu;

        class Slide { public CanvasGroup group; public float seconds; }
        readonly List<Slide> slides = new List<Slide>();

        void Start()
        {
            Time.timeScale = 1f;
            MenuWidgets.Use(menuFont, false);
            var root = MenuWidgets.Screen("BootCanvas", 10, transform);
            MenuWidgets.Stretch(MenuWidgets.Image("Black", root, Color.black).rectTransform);
            BuildLogo(root);
            BuildRating(root);
            BuildDescription(root);
            StartCoroutine(Run());
        }

        CanvasGroup NewSlide(RectTransform parent, string name, float seconds)
        {
            var r = MenuWidgets.Stretch(MenuWidgets.Rect(name, parent));
            var group = r.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            slides.Add(new Slide { group = group, seconds = seconds });
            return group;
        }

        void BuildLogo(RectTransform parent)
        {
            var slide = NewSlide(parent, "Slide_Logo", logoSeconds).transform;
            if (studioLogo != null)
            {
                var logo = MenuWidgets.Picture("Logo", slide, studioLogo);
                float h = 560f;
                MenuWidgets.Center(logo.rectTransform, Vector2.zero, new Vector2(h * studioLogo.width / Mathf.Max(1, studioLogo.height), h));
            }
            else
            {
                var name = MenuWidgets.Label("StudioName", slide, studioName, 90, MexicaPalette.Cinnabar);
                name.fontStyle = FontStyle.Bold;
                MenuWidgets.Center(name.rectTransform, Vector2.zero, new Vector2(1200, 140));
            }
        }

        void BuildRating(RectTransform parent)
        {
            var slide = NewSlide(parent, "Slide_Rating", ratingSeconds).transform;
            if (ratingImage != null)
            {
                var image = MenuWidgets.Picture("Rating", slide, ratingImage);
                float h = 420f;
                MenuWidgets.Center(image.rectTransform, Vector2.zero, new Vector2(h * ratingImage.width / Mathf.Max(1, ratingImage.height), h));
                return;
            }
            // Rating label like the one printed on the back of a game box: a white frame with the big letter,
            // the age bar, and next to it a framed list of content descriptors. Plain bold font (like the real ones).
            var plain = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var label = MenuWidgets.Rect("RatingLabel", slide);
            MenuWidgets.Center(label, new Vector2(0, 30), new Vector2(620, 340));

            var box = Frame("LetterBox", label, new Vector2(-170, 0), new Vector2(240, 330), 8);
            var letter = Plain(MenuWidgets.Label("Letter", box, ratingLetter, 230, Color.white), plain);
            MenuWidgets.Center(letter.rectTransform, new Vector2(0, 40), new Vector2(220, 240));
            var bar = MenuWidgets.Image("AgeBar", box, Color.white);
            MenuWidgets.Center(bar.rectTransform, new Vector2(0, -118), new Vector2(214, 62));
            var age = Plain(MenuWidgets.Label("Age", bar.transform, ratingLabel, 30, Color.black), plain);
            MenuWidgets.Stretch(age.rectTransform);
            var side = Plain(MenuWidgets.Label("Side", box, "CONTENT RATED", 13, Color.white), plain);
            MenuWidgets.Center(side.rectTransform, new Vector2(0, -158), new Vector2(220, 18));

            var info = Frame("Descriptors", label, new Vector2(130, 0), new Vector2(330, 330), 4);
            var descriptors = Plain(MenuWidgets.Label("Text", info, contentDescriptors, 30, Color.white, TextAnchor.UpperLeft), plain);
            MenuWidgets.Stretch(descriptors.rectTransform);
            descriptors.rectTransform.offsetMin = new Vector2(22, 22);
            descriptors.rectTransform.offsetMax = new Vector2(-18, -22);

            var note = Fit(MenuWidgets.Label("Note", slide, "This game contains intense violence, blood and gore.", 26, MexicaPalette.Ash));
            MenuWidgets.Center(note.rectTransform, new Vector2(0, -230), new Vector2(1100, 40));
        }

        /// <summary>A white frame with a black inside (returns the inside).</summary>
        static RectTransform Frame(string name, RectTransform parent, Vector2 position, Vector2 size, float border)
        {
            var outer = MenuWidgets.Image(name, parent, Color.white);
            MenuWidgets.Center(outer.rectTransform, position, size);
            var inner = MenuWidgets.Image("Inside", outer.transform, Color.black);
            MenuWidgets.Stretch(inner.rectTransform);
            inner.rectTransform.offsetMin = new Vector2(border, border);
            inner.rectTransform.offsetMax = new Vector2(-border, -border);
            return inner.rectTransform;
        }

        static Text Plain(Text text, Font font)
        {
            text.font = font;
            text.fontStyle = FontStyle.Bold;
            return Fit(text);
        }

        /// <summary>Best Fit (Unity UI): the text shrinks by itself if it does not fit in its box, on any screen size.</summary>
        static Text Fit(Text text)
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 8;
            text.resizeTextMaxSize = text.fontSize;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        void BuildDescription(RectTransform parent)
        {
            var slide = NewSlide(parent, "Slide_Description", descriptionSeconds).transform;
            var title = MenuWidgets.Label("Title", slide, descriptionTitle, 80, MexicaPalette.AgedGold);
            title.fontStyle = FontStyle.Bold;
            MenuWidgets.Center(title.rectTransform, new Vector2(0, 200), new Vector2(1200, 110));
            var body = Fit(MenuWidgets.Label("Body", slide, description, 32, MexicaPalette.Bone));
            MenuWidgets.Center(body.rectTransform, new Vector2(0, -40), new Vector2(1500, 360));
        }

        IEnumerator Run()
        {
            yield return null;
            GameAudio.PlaySfx("logo", 1f, 0f);
            foreach (var slide in slides)
            {
                yield return Fade(slide.group, 1f, 0.6f);
                for (float t = 0f; t < slide.seconds; t += Time.unscaledDeltaTime)
                {
                    if (t > 0.5f && MenuWidgets.AnyPress()) break;
                    yield return null;
                }
                yield return Fade(slide.group, 0f, 0.5f);
            }
            SceneFader.Load(nextScene);
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
    }
}
