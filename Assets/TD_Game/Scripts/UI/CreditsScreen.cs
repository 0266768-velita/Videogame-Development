using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// Ending of the game (task 4.7), after winning against the boss (EnemyData.isBoss). The save is deleted and:
    /// 1. epilogue cards appear one by one over a slowly moving picture with embers rising,
    /// 2. the credits scroll up between two gold Aztec friezes (Pirata One, coloured headers),
    /// 3. "THANK YOU FOR PLAYING" beats like a heart, then the main menu loads.
    /// ENTER / SPACE / gamepad A skips the current part.
    /// </summary>
    public class CreditsScreen : MonoBehaviour
    {
        [SerializeField] CanvasGroup panel;
        [Tooltip("The text that scrolls up (child of a mask or of the panel).")]
        [SerializeField] RectTransform scrollingText;
        [SerializeField] Text creditsText;
        [SerializeField, Min(1f)] float scrollSeconds = 30f;
        [SerializeField, Min(0f)] float delayAfterVictory = 1.5f;
        [Tooltip("Loaded at the end. If it is not in Build Settings, the current scene restarts.")]
        [SerializeField] string mainMenuScene = SceneIds.MainMenu;

        [Header("Look (set by the builder)")]
        [SerializeField] Font menuFont;
        [Tooltip("Picture behind the credits (it moves slowly).")]
        [SerializeField] Texture background;
        [Tooltip("Aztec frieze tiled at the top and bottom.")]
        [SerializeField] Sprite greca;
        [SerializeField, Min(0)] int emberCount = 60;

        static readonly string[] Epilogue =
        {
            "Quetzalcóatl has fallen.",
            "<color=#C0392B>The gods finally answered.</color>",
            "But no prayer brings the dead back.",
            "Tlahuelli walked on, deeper into Mictlán...",
        };

        // Fixed in code (not in the scene) so an old saved text can never replace it.
        const string Gold = "#C8963C", Jade = "#3FA37A", Red = "#C0392B", Ash = "#8C8579";
        static readonly string EndCredits =
            $"<size=110><color={Gold}>MICTLÁN</color></size>\n" +
            $"<color={Ash}>the journey of Tlahuelli</color>\n\n\n\n" +
            $"<size=44><color={Red}>A GAME BY MICTA FORGE</color></size>\n\n\n" +
            $"<color={Gold}>Antonio Enríquez Velasco</color>\n<size=26><color={Ash}>combat · animation · effects</color></size>\n\n" +
            $"<color={Gold}>Alejandro Cornejo Pérez</color>\n<size=26><color={Ash}>enemies · menus · audio · saving</color></size>\n\n" +
            $"<color={Gold}>Adrián Gallardo Medina</color>\n<size=26><color={Ash}>map · level design</color></size>\n\n" +
            $"<color={Gold}>Jetzuvely del Carmen González Gutiérrez</color>\n<size=26><color={Ash}>art · sprites</color></size>\n\n" +
            $"<color={Gold}>Claude</color>\n<size=26><color={Ash}>AI assistant by Anthropic · programming · procedural art · sound</color></size>\n\n\n\n" +
            $"<size=40><color={Jade}>IN MEMORY OF THE MEXICA WORLD</color></size>\n" +
            $"<size=26><color={Ash}>Mictlantecuhtli · Quetzalcóatl · Tezcatlipoca · Coatlicue\nEhécatl · Tlazoltéotl · Xólotl</color></size>\n\n\n\n" +
            $"<size=34><color={Red}>MADE WITH</color></size>\n" +
            $"Unity 6\n<size=26><color={Ash}>Font: Pirata One (SIL Open Font License)</color></size>\n\n\n\n\n" +
            $"<size=26><color={Ash}>\"The gods forgot you. They forgot me too.\"</color></size>";

        bool playing;
        RectTransform backgroundRect;
        Text card;
        CanvasGroup cardGroup;   // fades the card (rich-text colours ignore the Text alpha)
        readonly List<(RectTransform rect, float speed, float sway)> embers = new List<(RectTransform, float, float)>();

        void Awake()
        {
            if (panel != null) { panel.alpha = 0f; panel.blocksRaycasts = false; }
        }

        void OnEnable() => CombatManager.CombatWon += OnCombatWon;
        void OnDisable() => CombatManager.CombatWon -= OnCombatWon;

        void OnCombatWon(IReadOnlyList<EnemyData> defeated)
        {
            if (playing) return;
            foreach (var enemy in defeated)
                if (enemy != null && enemy.isBoss)
                {
                    StartCoroutine(Roll());
                    return;
                }
        }

        /// <summary>Can also be called directly (tests, a cutscene).</summary>
        public IEnumerator Roll()
        {
            playing = true;
            GameSave.Delete();   // the game is finished: no Continue afterwards, only New Game
            yield return new WaitForSecondsRealtime(delayAfterVictory);
            GameState.SetMode(GameMode.Cutscene);
            GameAudio.PlayMusic(MusicTrack.Menu);
            BuildLook();
            StartCoroutine(Animate());

            panel.blocksRaycasts = true;
            yield return FadePanel(1f, 1.5f);

            // 1. Epilogue cards.
            foreach (string line in Epilogue)
            {
                card.text = line;
                yield return ShowCard(3.2f);
            }

            // 2. The credits scroll up.
            var area = (RectTransform)scrollingText.parent;
            float start = -area.rect.height * 0.5f - scrollingText.rect.height * 0.5f;
            float end = area.rect.height * 0.5f + scrollingText.rect.height * 0.5f;
            for (float t = 0f; t < scrollSeconds; t += Time.unscaledDeltaTime)
            {
                scrollingText.anchoredPosition = new Vector2(0f, Mathf.Lerp(start, end, t / scrollSeconds));
                if (SkipPressed()) break;
                yield return null;
            }
            scrollingText.gameObject.SetActive(false);

            // 3. Thank you, beating like a heart.
            card.text = $"<size=90><color={Gold}>THANK YOU</color></size>\n<size=50>FOR PLAYING</size>";
            GameAudio.PlaySfx("victory", 0.7f, 0f);
            float shown = 0f;
            cardGroup.alpha = 0f;
            while (shown < 6f && !(shown > 1f && SkipPressed()))
            {
                shown += Time.unscaledDeltaTime;
                float beat = 1f + Mathf.Max(0f, Mathf.Sin(shown * 5f)) * 0.04f;
                card.rectTransform.localScale = Vector3.one * beat;
                cardGroup.alpha = Mathf.Clamp01(shown);
                yield return null;
            }

            if (Application.CanStreamedLevelBeLoaded(mainMenuScene)) SceneFader.Load(mainMenuScene);
            else SceneFader.Load(SceneManager.GetActiveScene().name);
        }

        /// <summary>A card grows in from a little bigger, stays, and fades away.</summary>
        IEnumerator ShowCard(float seconds)
        {
            var rect = card.rectTransform;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float a = Mathf.Min(Mathf.Clamp01(t / 0.8f), Mathf.Clamp01((seconds - t) / 0.7f));
                cardGroup.alpha = a;
                rect.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f, Mathf.SmoothStep(0f, 1f, t / 0.8f));
                if (t > 0.5f && SkipPressed()) break;
                yield return null;
            }
            cardGroup.alpha = 0f;
        }

        IEnumerator FadePanel(float to, float seconds)
        {
            float from = panel.alpha;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                panel.alpha = Mathf.Lerp(from, to, t / seconds);
                yield return null;
            }
            panel.alpha = to;
        }

        /// <summary>The picture drifts slowly (Ken Burns) and the embers rise and sway.</summary>
        IEnumerator Animate()
        {
            float t = 0f;
            var size = ((RectTransform)panel.transform).rect.size;
            while (true)
            {
                t += Time.unscaledDeltaTime;
                if (backgroundRect != null)
                {
                    backgroundRect.localScale = Vector3.one * (1.12f + Mathf.Sin(t * 0.05f) * 0.05f);
                    backgroundRect.anchoredPosition = new Vector2(Mathf.Sin(t * 0.07f) * 40f, Mathf.Cos(t * 0.05f) * 20f);
                }
                foreach (var (rect, speed, sway) in embers)
                {
                    var p = rect.anchoredPosition;
                    p.y += speed * Time.unscaledDeltaTime;
                    p.x += Mathf.Sin(t * 1.5f + sway) * 12f * Time.unscaledDeltaTime;
                    if (p.y > size.y * 0.5f + 20f) p = new Vector2(Random.Range(-size.x, size.x) * 0.5f, -size.y * 0.5f - 20f);
                    rect.anchoredPosition = p;
                }
                yield return null;
            }
        }

        // ---------------- Everything on screen is created here, so the builder only needs the references ----------------

        void BuildLook()
        {
            var root = (RectTransform)panel.transform;
            var font = menuFont != null ? menuFont : MenuWidgets.Font;

            if (background != null)
            {
                var picture = MenuWidgets.Picture("Background", root, background);
                backgroundRect = (RectTransform)picture.transform;
                backgroundRect.anchorMin = backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
                float aspect = background.width / (float)background.height;
                float width = Mathf.Max(root.rect.width, root.rect.height * aspect);
                backgroundRect.sizeDelta = new Vector2(width, width / aspect);
                picture.color = new Color(0.45f, 0.4f, 0.42f);
                backgroundRect.SetSiblingIndex(0);
                var shade = MenuWidgets.Stretch(MenuWidgets.Image("Shade", root, new Color(0f, 0f, 0f, 0.45f)).rectTransform);
                shade.SetSiblingIndex(1);
            }

            var ember = MenuWidgets.Rect("Embers", root);
            MenuWidgets.Stretch(ember);
            for (int i = 0; i < emberCount; i++)
            {
                var e = MenuWidgets.Image("Ember", ember, Random.value < 0.6f ? new Color(1f, 0.5f, 0.15f, 0.8f) : new Color(1f, 0.8f, 0.3f, 0.6f));
                var r = e.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.sizeDelta = Vector2.one * Random.Range(3f, 7f);
                r.anchoredPosition = new Vector2(Random.Range(-0.5f, 0.5f) * root.rect.width, Random.Range(-0.5f, 0.5f) * root.rect.height);
                embers.Add((r, Random.Range(25f, 70f), Random.Range(0f, 6f)));
            }

            if (greca != null)
                foreach (bool top in new[] { true, false })
                {
                    var band = MenuWidgets.Image(top ? "GrecaTop" : "GrecaBottom", root, new Color(0.78f, 0.59f, 0.24f));
                    band.sprite = greca;
                    band.type = Image.Type.Tiled;
                    var r = band.rectTransform;
                    r.anchorMin = new Vector2(0f, top ? 1f : 0f);
                    r.anchorMax = new Vector2(1f, top ? 1f : 0f);
                    r.pivot = new Vector2(0.5f, top ? 1f : 0f);
                    r.sizeDelta = new Vector2(0f, 36f);
                    r.anchoredPosition = Vector2.zero;
                    if (!top) r.localScale = new Vector3(1f, -1f, 1f);
                }

            creditsText.font = font;
            creditsText.supportRichText = true;
            creditsText.fontSize = 36;
            creditsText.lineSpacing = 1.1f;
            creditsText.verticalOverflow = VerticalWrapMode.Overflow;
            creditsText.color = MexicaPalette.Bone;
            creditsText.text = EndCredits;
            Shadow(creditsText);
            scrollingText.sizeDelta = new Vector2(1100f, creditsText.preferredHeight + 200f);
            scrollingText.anchoredPosition = new Vector2(0f, -5000f);   // hidden below until the scroll starts
            scrollingText.SetAsLastSibling();

            card = MenuWidgets.Label("Card", root, "", 58, MexicaPalette.Bone, TextAnchor.MiddleCenter);
            card.font = font;
            card.supportRichText = true;
            MenuWidgets.Center(card.rectTransform, Vector2.zero, new Vector2(1400f, 400f));
            cardGroup = card.gameObject.AddComponent<CanvasGroup>();
            cardGroup.alpha = 0f;
            Shadow(card);
        }

        static void Shadow(Text text)
        {
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            shadow.effectDistance = new Vector2(3f, -3f);
        }

        static bool SkipPressed()
        {
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            return (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                || (pad != null && pad.buttonSouth.wasPressedThisFrame);
        }
    }
}
