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
        [Header("1) Studio logo")]
        [SerializeField] Texture2D studioLogo;
        [SerializeField] string studioName = "MICTA FORGE";
        [SerializeField, Min(0.5f)] float logoSeconds = 3f;

        [Header("2) Age rating (optional: assign the official image)")]
        [SerializeField] Texture2D ratingImage;
        [SerializeField] string ratingLetter = "M";
        [SerializeField] string ratingLabel = "MATURE 17+";
        [SerializeField, TextArea(2, 4)] string ratingDescriptors = "Blood and Gore\nViolence";
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
            // Rating box drawn with UI (white frame, black inside, big letter), like the classic rating labels.
            var box = MenuWidgets.Image("Box", slide, Color.white);
            MenuWidgets.Center(box.rectTransform, new Vector2(-170, 20), new Vector2(240, 310));
            var inner = MenuWidgets.Image("Inner", box.transform, Color.black);
            MenuWidgets.Stretch(inner.rectTransform);
            inner.rectTransform.offsetMin = new Vector2(10, 10);
            inner.rectTransform.offsetMax = new Vector2(-10, -10);
            var letter = MenuWidgets.Label("Letter", inner.transform, ratingLetter, 200, Color.white);
            letter.fontStyle = FontStyle.Bold;
            MenuWidgets.Center(letter.rectTransform, new Vector2(0, 25), new Vector2(220, 230));
            var label = MenuWidgets.Label("Label", inner.transform, ratingLabel, 30, Color.white);
            MenuWidgets.Center(label.rectTransform, new Vector2(0, -110), new Vector2(220, 40));

            var descriptors = MenuWidgets.Label("Descriptors", slide, ratingDescriptors, 34, Color.white, TextAnchor.MiddleLeft);
            MenuWidgets.Center(descriptors.rectTransform, new Vector2(170, 20), new Vector2(420, 240));
            var note = MenuWidgets.Label("Note", slide, "Rating: " + ratingLabel, 24, MexicaPalette.Ash);
            MenuWidgets.Center(note.rectTransform, new Vector2(0, -200), new Vector2(900, 40));
        }

        void BuildDescription(RectTransform parent)
        {
            var slide = NewSlide(parent, "Slide_Description", descriptionSeconds).transform;
            var title = MenuWidgets.Label("Title", slide, descriptionTitle, 80, MexicaPalette.AgedGold);
            title.fontStyle = FontStyle.Bold;
            MenuWidgets.Center(title.rectTransform, new Vector2(0, 200), new Vector2(1200, 110));
            var body = MenuWidgets.Label("Body", slide, description, 32, MexicaPalette.Bone);
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
