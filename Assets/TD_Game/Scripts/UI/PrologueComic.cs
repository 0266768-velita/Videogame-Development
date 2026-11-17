using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// The backstory of Tlahuelli as a comic (GDD section 3: Prologue + Mictlantecuhtli's Offer).
    /// The panels appear one by one; any key shows the next one, ESC skips everything. At the end the game starts.
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

        RectTransform root;
        bool skip;

        void Start()
        {
            MenuWidgets.Use(menuFont, true);
            root = MenuWidgets.Screen("ComicCanvas", 10, transform);
            MenuWidgets.Stretch(MenuWidgets.Image("Black", root, Color.black).rectTransform);
            var hint = MenuWidgets.Label("Hint", root, "Any key: next      ESC: skip", 22, MexicaPalette.Ash, TextAnchor.LowerRight);
            hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = hint.rectTransform.pivot = new Vector2(1, 0);
            hint.rectTransform.anchoredPosition = new Vector2(-30, 20);
            hint.rectTransform.sizeDelta = new Vector2(600, 40);
            StartCoroutine(Play());
        }

        void Update()
        {
            if (MenuWidgets.CancelPressed()) skip = true;
        }

        IEnumerator Play()
        {
            yield return null;
            foreach (var page in pages)
            {
                if (skip) break;
                var pageRoot = MenuWidgets.Stretch(MenuWidgets.Rect("Page", root));
                int count = page.panels.Count;
                float width = Mathf.Min(560f, 1700f / count);
                for (int i = 0; i < count; i++)
                {
                    if (skip) break;
                    float x = (i - (count - 1) / 2f) * (width + 30f);   // panels side by side, centred
                    var group = BuildPanel(pageRoot, page.panels[i], new Vector2(x, 20f), new Vector2(width, 700f));
                    yield return Fade(group, 1f, 0.5f);
                    yield return WaitForKey();
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

        IEnumerator WaitForKey()
        {
            for (float t = 0f; !skip; t += Time.unscaledDeltaTime)
            {
                if (t > 0.4f && MenuWidgets.AnyPress()) yield break;
                yield return null;
            }
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
