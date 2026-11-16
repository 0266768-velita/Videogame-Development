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
        [Header("Art")]
        [SerializeField] Texture2D background;
        [Tooltip("Optional logo image; empty = the title is written with text.")]
        [SerializeField] Texture2D gameLogo;
        [SerializeField] string title = "MICTLÁN";
        [SerializeField] string studio = "Micta Forge";
        [SerializeField] string version = "v.04";

        [Header("Scenes")]
        [SerializeField] string gameScene = SceneIds.MainMap;

        [Header("Credits")]
        [SerializeField, TextArea(4, 12)] string credits =
            "MICTLÁN\nA game by Micta Forge\n\n" +
            "Jetzuvely del Carmen González Gutiérrez\n" +
            "Adrián Gallardo Medina\n" +
            "Antonio Enríquez Velasco\n" +
            "Alejandro Cornejo Pérez";

        enum Stage { Title, Menu, Options, Credits }
        Stage stage = Stage.Title;

        GameObject menuPanel, optionsPanel, creditsPanel;
        Text pressStart;
        RawImage backgroundImage;
        Button startButton, continueButton, masterButton, musicButton, sfxButton, fullscreenButton;

        void Start()
        {
            Time.timeScale = 1f;
            GameState.SetMode(GameMode.Exploration);
            GameAudio.PlayMusic(MusicTrack.Menu);
            Build();
        }

        void Update()
        {
            if (backgroundImage != null)   // slow "breathing" zoom, as in Mictlán v5
                backgroundImage.rectTransform.localScale = Vector3.one * (1.04f + 0.03f * Mathf.Sin(Time.unscaledTime * 0.12f));

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
            }
            MenuWidgets.Stretch(MenuWidgets.Image("Shade", root, new Color(0f, 0f, 0f, background != null ? 0.25f : 0.5f)).rectTransform);

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
            MenuWidgets.Button(t, "Back", new Vector2(0, -310), size, ShowMenu, 28);
            Refresh();
        }

        void BuildCredits(RectTransform root)
        {
            creditsPanel = MenuWidgets.Stretch(MenuWidgets.Rect("CreditsPanel", root)).gameObject;
            var plate = MenuWidgets.Image("Plate", creditsPanel.transform, MexicaPalette.WithAlpha(MexicaPalette.ObsidianPlate, 0.85f));
            MenuWidgets.Center(plate.rectTransform, new Vector2(0, -20), new Vector2(1100, 480));
            var body = MenuWidgets.Label("Body", creditsPanel.transform, credits, 34, MexicaPalette.Bone);
            MenuWidgets.Center(body.rectTransform, new Vector2(0, 10), new Vector2(1050, 380));
            MenuWidgets.Button(creditsPanel.transform, "Back", new Vector2(0, -300), new Vector2(440, 58), ShowMenu, 28);
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
            SceneFader.Load(gameScene);
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
        }
    }
}
