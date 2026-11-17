using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// Title screen and main menu (adapted from Mictlán v5's MainMenu), with the same background and music.
    /// Stage 1: "PRESS ENTER / START".  Stage 2: Start Game, Continue, Options, Credits, Exit.
    /// Keyboard (arrows + Enter), mouse and gamepad. The song comes from GameAudio (Menu track).
    /// </summary>
    public class TitleMenu : MonoBehaviour
    {
        [Header("Style")]
        [SerializeField] Font menuFont;          // Pirata One
        [Tooltip("Soft round light used for the torches, the moon and the fog of the background.")]
        [SerializeField] Sprite glow;

        [Header("Art")]
        [SerializeField] Texture2D background;
        [Tooltip("Optional logo image; empty = the title is written with text.")]
        [SerializeField] Texture2D gameLogo;
        [SerializeField] string title = "MICTLÁN";
        [SerializeField] string studio = "Micta Forge";
        [SerializeField] string version = "v.04";

        [Header("Scenes")]
        [SerializeField] string gameScene = SceneIds.MainMap;
        [Tooltip("Story comic shown before a NEW game.")]
        [SerializeField] string prologueScene = SceneIds.Prologue;

        [Header("Credits")]
        // Fixed in code (not in the scene) so an old saved text can never replace it.
        const string TeamCredits =
            "MICTLÁN\nA game by Micta Forge\n\n" +
            "Antonio Enríquez Velasco\n" +
            "Alejandro Cornejo Pérez\n" +
            "Adrián Gallardo Medina\n" +
            "Jetzuvely del Carmen González Gutiérrez";

        enum Stage { Title, Menu, Options, Credits }
        Stage stage = Stage.Title;

        GameObject menuPanel, optionsPanel, creditsPanel;
        Text pressStart;
        RawImage backgroundImage;
        Button startButton, continueButton, masterButton, musicButton, sfxButton, fullscreenButton, goreButton;

        void Start()
        {
            Time.timeScale = 1f;
            GameState.SetMode(GameMode.Exploration);
            GameAudio.PlayMusic(MusicTrack.Menu);
            MenuWidgets.Use(menuFont, true);
            Build();
        }

        void Update()
        {
            if (backgroundImage != null)
            {
                // The background breathes and drifts slowly, like a camera floating over Teotihuacan.
                float t = Time.unscaledTime;
                backgroundImage.rectTransform.localScale = Vector3.one * (1.08f + 0.04f * Mathf.Sin(t * 0.25f));
                backgroundImage.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(t * 0.13f) * 40f, Mathf.Cos(t * 0.17f) * 20f);
            }
            MoveEmbers();
            AnimateBackground();

            switch (stage)
            {
                case Stage.Title:
                    pressStart.color = MexicaPalette.WithAlpha(Color.white, 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.2f)));
                    if (MenuWidgets.AnyPress()) { GameAudio.PlaySfx("ui_select", 1f, 0f); ShowMenu(); }
                    break;
                case Stage.Options:
                case Stage.Credits:
                    if (MenuWidgets.CancelPressed()) { GameAudio.PlaySfx("ui_back", 1f, 0f); ShowMenu(); }
                    break;
            }
        }

        // ---------------- Building ----------------

        void Build()
        {
            var root = MenuWidgets.Screen("MenuCanvas", 10, transform);
            MenuWidgets.Stretch(MenuWidgets.Image("Black", root, MexicaPalette.Obsidian).rectTransform);
            if (background != null)
            {
                backgroundImage = MenuWidgets.Picture("Background", root, background);
                MenuWidgets.Stretch(backgroundImage.rectTransform);
                MakeBackgroundAlive(backgroundImage.rectTransform);
            }
            MenuWidgets.Stretch(MenuWidgets.Image("Shade", root, new Color(0f, 0f, 0f, background != null ? 0.25f : 0.5f)).rectTransform);
            MakeEmbers(root);

            if (gameLogo != null)
            {
                var logo = MenuWidgets.Picture("Logo", root, gameLogo);
                float h = 260f;
                MenuWidgets.Center(logo.rectTransform, new Vector2(0, 230), new Vector2(h * gameLogo.width / Mathf.Max(1, gameLogo.height), h));
            }
            else
            {
                var titleText = MenuWidgets.Label("Title", root, title, 150, MexicaPalette.Cinnabar);
                titleText.fontStyle = FontStyle.Bold;
                var outline = titleText.gameObject.AddComponent<Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(4f, -4f);
                MenuWidgets.Center(titleText.rectTransform, new Vector2(0, 230), new Vector2(1400, 200));
            }

            var studioText = MenuWidgets.Label("Studio", root, studio, 24, MexicaPalette.WithAlpha(Color.white, 0.6f), TextAnchor.LowerLeft);
            Corner(studioText.rectTransform, new Vector2(0, 0), new Vector2(24, 16));
            var versionText = MenuWidgets.Label("Version", root, version, 24, MexicaPalette.WithAlpha(Color.white, 0.6f), TextAnchor.LowerRight);
            Corner(versionText.rectTransform, new Vector2(1, 0), new Vector2(-24, 16));

            pressStart = MenuWidgets.Label("PressStart", root, "PRESS ENTER / START", 40, Color.white);
            MenuWidgets.Center(pressStart.rectTransform, new Vector2(0, -230), new Vector2(1000, 60));

            BuildMenu(root);
            BuildOptions(root);
            BuildCredits(root);
            menuPanel.SetActive(false);
            optionsPanel.SetActive(false);
            creditsPanel.SetActive(false);
        }

        static void Corner(RectTransform r, Vector2 anchor, Vector2 offset)
        {
            r.anchorMin = r.anchorMax = r.pivot = anchor;
            r.anchoredPosition = offset;
            r.sizeDelta = new Vector2(500, 40);
        }

        void BuildMenu(RectTransform root)
        {
            menuPanel = MenuWidgets.Stretch(MenuWidgets.Rect("MenuPanel", root)).gameObject;
            var t = menuPanel.transform;
            Vector2 size = new Vector2(440, 64);
            float y = 20f, step = -80f;
            startButton = MenuWidgets.Button(t, "Start Game", new Vector2(0, y), size, OnStartGame);
            continueButton = MenuWidgets.Button(t, "Continue", new Vector2(0, y + step), size, OnContinue);
            MenuWidgets.Button(t, "Options", new Vector2(0, y + step * 2), size, () => ShowPanel(Stage.Options));
            MenuWidgets.Button(t, "Credits", new Vector2(0, y + step * 3), size, () => ShowPanel(Stage.Credits));
            MenuWidgets.Button(t, "Exit Game", new Vector2(0, y + step * 4), size, OnQuit);
            continueButton.interactable = GameSave.HasSave;
        }

        void BuildOptions(RectTransform root)
        {
            optionsPanel = MenuWidgets.Stretch(MenuWidgets.Rect("OptionsPanel", root)).gameObject;
            var t = optionsPanel.transform;
            var head = MenuWidgets.Label("Head", t, "OPTIONS", 52, MexicaPalette.AgedGold);
            MenuWidgets.Center(head.rectTransform, new Vector2(0, 60), new Vector2(700, 70));
            Vector2 size = new Vector2(480, 58);
            masterButton = MenuWidgets.Button(t, "", new Vector2(0, -20), size, () => { GameAudio.MasterVolume = Cycle(GameAudio.MasterVolume); Refresh(); }, 28);
            musicButton = MenuWidgets.Button(t, "", new Vector2(0, -90), size, () => { GameAudio.MusicVolume = Cycle(GameAudio.MusicVolume); Refresh(); }, 28);
            sfxButton = MenuWidgets.Button(t, "", new Vector2(0, -160), size, () => { GameAudio.SfxVolume = Cycle(GameAudio.SfxVolume); Refresh(); }, 28);
            fullscreenButton = MenuWidgets.Button(t, "", new Vector2(0, -230), size, ToggleFullscreen, 28);
            goreButton = MenuWidgets.Button(t, "", new Vector2(0, -300), size, () => { GoreEffects.Enabled = !GoreEffects.Enabled; Refresh(); }, 28);
            MenuWidgets.Button(t, "Back", new Vector2(0, -380), size, ShowMenu, 28);
            Refresh();
        }

        void BuildCredits(RectTransform root)
        {
            creditsPanel = MenuWidgets.Stretch(MenuWidgets.Rect("CreditsPanel", root)).gameObject;
            MenuWidgets.Panel("Plate", creditsPanel.transform, new Vector2(0, -20), new Vector2(1100, 480));
            var body = MenuWidgets.Label("Body", creditsPanel.transform, TeamCredits, 34, MexicaPalette.Bone);
            MenuWidgets.Center(body.rectTransform, new Vector2(0, 10), new Vector2(1050, 380));
            MenuWidgets.Button(creditsPanel.transform, "Back", new Vector2(0, -300), new Vector2(440, 58), ShowMenu, 28);
        }

        // ---------------- A living background: torches, moon, stars and fog ----------------
        // Everything is a child of the background picture, so it moves with it. Positions are 0..1 of the picture.

        Image[] torchLights, stars;
        Image moonHalo;
        RectTransform[] fog;

        Image Light(RectTransform parent, string name, Vector2 where, float size, Color color)
        {
            var image = MenuWidgets.Image(name, parent, color);
            image.sprite = glow;
            var r = image.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(where.x, 1f - where.y);
            r.sizeDelta = Vector2.one * size;
            return image;
        }

        void MakeBackgroundAlive(RectTransform bg)
        {
            if (glow == null) return;
            torchLights = new[]
            {
                Light(bg, "TorchLight", new Vector2(0.347f, 0.36f), 90f, new Color(1f, 0.6f, 0.2f, 0.7f)),
                Light(bg, "TorchLight", new Vector2(0.370f, 0.36f), 90f, new Color(1f, 0.6f, 0.2f, 0.7f)),
            };
            moonHalo = Light(bg, "MoonHalo", new Vector2(0.743f, 0.216f), 380f, new Color(1f, 0.95f, 0.8f, 0.25f));
            stars = new Image[30];
            for (int i = 0; i < stars.Length; i++)
                stars[i] = Light(bg, "Star", new Vector2(Random.value, Random.Range(0.02f, 0.4f)), Random.Range(10f, 18f), Color.white);
            fog = new RectTransform[3];
            for (int i = 0; i < fog.Length; i++)
            {
                var band = Light(bg, "Fog", new Vector2(i / 3f, 0.82f + i * 0.05f), 1f, new Color(0.7f, 0.6f, 0.75f, 0.12f));
                band.rectTransform.sizeDelta = new Vector2(1400f, 160f);
                fog[i] = band.rectTransform;
            }
        }

        void AnimateBackground()
        {
            if (torchLights == null) return;
            float t = Time.unscaledTime;
            for (int i = 0; i < torchLights.Length; i++)   // fire flickers fast and irregularly
            {
                float flicker = 0.75f + 0.15f * Mathf.Sin(t * 13f + i * 2f) + 0.1f * Mathf.Sin(t * 29f + i);
                torchLights[i].rectTransform.localScale = Vector3.one * flicker;
            }
            moonHalo.rectTransform.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(t * 0.8f));   // the moon breathes
            for (int i = 0; i < stars.Length; i++)   // stars twinkle
                stars[i].color = new Color(1f, 1f, 1f, 0.15f + 0.6f * Mathf.Abs(Mathf.Sin(t * (0.7f + i % 5 * 0.3f) + i)));
            for (int i = 0; i < fog.Length; i++)    // fog drifts slowly sideways and wraps around
            {
                float x = Mathf.Repeat(t * (12f + i * 6f) + i * 600f, 2800f) - 1400f;
                fog[i].anchoredPosition = new Vector2(x, 0f);
            }
        }

        // ---------------- Floating embers (simple: small squares that rise and come back to the bottom) ----------------

        RectTransform[] embers;

        void MakeEmbers(RectTransform root)
        {
            embers = new RectTransform[40];
            for (int i = 0; i < embers.Length; i++)
            {
                var ember = MenuWidgets.Image("Ember", root, new Color(1f, Random.Range(0.35f, 0.65f), 0.15f, Random.Range(0.4f, 0.9f)));
                MenuWidgets.Center(ember.rectTransform, new Vector2(Random.Range(-960f, 960f), Random.Range(-540f, 540f)), Vector2.one * Random.Range(4f, 9f));
                embers[i] = ember.rectTransform;
            }
        }

        void MoveEmbers()
        {
            if (embers == null) return;
            for (int i = 0; i < embers.Length; i++)
            {
                Vector2 p = embers[i].anchoredPosition;
                p.y += (30f + i % 5 * 12f) * Time.unscaledDeltaTime;                 // rise
                p.x += Mathf.Sin(Time.unscaledTime * 1.3f + i) * 20f * Time.unscaledDeltaTime;   // sway
                if (p.y > 560f) p = new Vector2(Random.Range(-960f, 960f), -560f);   // back to the bottom
                embers[i].anchoredPosition = p;
            }
        }

        // ---------------- Navigation ----------------

        void ShowMenu()
        {
            stage = Stage.Menu;
            pressStart.gameObject.SetActive(false);
            optionsPanel.SetActive(false);
            creditsPanel.SetActive(false);
            menuPanel.SetActive(true);
            StartCoroutine(SelectNextFrame(startButton.gameObject));
        }

        void ShowPanel(Stage panelStage)
        {
            stage = panelStage;
            menuPanel.SetActive(false);
            var panel = panelStage == Stage.Options ? optionsPanel : creditsPanel;
            panel.SetActive(true);
            StartCoroutine(SelectNextFrame(panel.GetComponentInChildren<Button>().gameObject));
        }

        static IEnumerator SelectNextFrame(GameObject go)
        {
            yield return null;
            MenuWidgets.Select(go);
        }

        // ---------------- Actions ----------------

        void OnStartGame()
        {
            GameSave.NewGame();
            ContextTutorial.ResetAll();
            // New game: first the story of Tlahuelli, then the map.
            SceneFader.Load(Application.CanStreamedLevelBeLoaded(prologueScene) ? prologueScene : gameScene);
        }

        void OnContinue()
        {
            GameSave.Continue();
            var data = GameSave.Read();
            string scene = data != null && !string.IsNullOrEmpty(data.scene) && Application.CanStreamedLevelBeLoaded(data.scene)
                ? data.scene : gameScene;
            SceneFader.Load(scene);
        }

        static void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void ToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
            StartCoroutine(RefreshNextFrame());
        }

        IEnumerator RefreshNextFrame()
        {
            yield return null;
            Refresh();
        }

        /// <summary>0 -> 25 -> 50 -> 75 -> 100 -> 0 %.</summary>
        static float Cycle(float value) => value >= 0.99f ? 0f : Mathf.Min(1f, Mathf.Round(value * 4f + 1f) / 4f);

        void Refresh()
        {
            MenuWidgets.SetLabel(masterButton, $"Master Volume: {Mathf.RoundToInt(GameAudio.MasterVolume * 100f)}%");
            MenuWidgets.SetLabel(musicButton, $"Music Volume: {Mathf.RoundToInt(GameAudio.MusicVolume * 100f)}%");
            MenuWidgets.SetLabel(sfxButton, $"Effects Volume: {Mathf.RoundToInt(GameAudio.SfxVolume * 100f)}%");
            MenuWidgets.SetLabel(fullscreenButton, "Fullscreen: " + (Screen.fullScreen ? "On" : "Off"));
            MenuWidgets.SetLabel(goreButton, "Gore (17+): " + (GoreEffects.Enabled ? "On" : "Off"));
        }
    }
}
