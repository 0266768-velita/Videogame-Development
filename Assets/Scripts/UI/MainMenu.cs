using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Title screen + main menu (GDD section 8).
/// Stage 1: title with "PRESS ENTER / START".  Stage 2: Start Game, Continue, Options, Credits, Exit.
/// Works with keyboard (arrows + Enter), mouse and gamepad.
///
/// To add art: in the MainMenu scene, "MainMenu" object > Game Logo, Background and Music fields.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Art (optional)")]
    public Texture2D gameLogo;        // Mictlan title logo
    public Texture2D background;      // background (for example the Tlaloc rainforest)
    public AudioClip music;
    public string versionText = "v0.2";

    enum Stage { Title, Menu, Options, Credits }
    Stage stage = Stage.Title;

    GameObject menuPanel, optionsPanel, creditsPanel;
    Text pressStart;
    Button startButton, continueButton, fullscreenButton, volumeButton, musicButton, difficultyButton, goreButton;
    RawImage bgImage;
    float volume = 1f;

    void Start()
    {
        Time.timeScale = 1f;
        GameState.SetMode(GameMode.Exploration);
        UIKit.EnsureEventSystem();

        volume = PlayerPrefs.GetFloat("mictlan_volume", 1f);
        AudioListener.volume = volume;

        if (music != null)
        {
            // A custom clip assigned in the Inspector wins over the built-in menu track.
            Audio.StopMusic(0f);
            var src = gameObject.AddComponent<AudioSource>();
            src.clip = music; src.loop = true; src.playOnAwake = false;
            src.Play();
        }
        else Audio.PlayMusic("music_menu", true, 1f);

        Build();
    }

    void Update()
    {
        if (bgImage != null)
        {
            float k = 1.04f + 0.03f * Mathf.Sin(Time.unscaledTime * 0.12f);   // gentle breathing zoom
            bgImage.rectTransform.localScale = Vector3.one * k;
        }
        switch (stage)
        {
            case Stage.Title:
                pressStart.color = new Color(1, 1, 1, 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.2f)));
                if (UIKit.AnyPress()) ShowMenu();
                break;
            case Stage.Options:
            case Stage.Credits:
                if (UIKit.CancelPressed()) ShowMenu();
                break;
        }
    }

    // =====================================================================
    void Build()
    {
        var content = UIKit.CreateScreen("MenuCanvas", 10, transform);

        // Background
        UIKit.Stretch(UIKit.NewImage("Black", content, new Color(0.04f, 0.02f, 0.03f)).rectTransform);
        if (background == null) background = Resources.Load<Texture2D>("UI/MenuBackground");
        if (background != null)
        {
            bgImage = UIKit.NewRawImage("Background", content, background);
            UIKit.Stretch(bgImage.rectTransform);
            bgImage.rectTransform.localScale = Vector3.one * 1.04f;   // slow zoom in Update
        }
        UIKit.Stretch(UIKit.NewImage("Shade", content, new Color(0, 0, 0, background != null ? 0.22f : 0.45f)).rectTransform);

        // Title (always visible)
        if (gameLogo != null)
        {
            var raw = UIKit.NewRawImage("Logo", content, gameLogo);
            float h = 250f;
            float w = h * gameLogo.width / Mathf.Max(1, gameLogo.height);
            UIKit.Center(raw.rectTransform, new Vector2(0, 170), new Vector2(w, h));
        }
        else
        {
            var title = UIKit.NewText("Title", content, "MICTLÁN", 140, TextAnchor.MiddleCenter, UIKit.Accent);
            title.fontStyle = FontStyle.Bold;
            title.gameObject.AddComponent<Outline>().effectColor = Color.black;
            UIKit.Center(title.rectTransform, new Vector2(0, 170), new Vector2(1100, 180));
        }

        var studio = UIKit.NewText("Studio", content, "Micta Forge", 20, TextAnchor.LowerLeft, new Color(1, 1, 1, 0.6f));
        UIKit.Anchor(studio.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(20, 14), new Vector2(400, 30));
        var ver = UIKit.NewText("Version", content, versionText, 20, TextAnchor.LowerRight, new Color(1, 1, 1, 0.6f));
        UIKit.Anchor(ver.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-20, 14), new Vector2(200, 30));

        // Stage 1
        pressStart = UIKit.NewText("PressStart", content, "PRESS ENTER / START", 34, TextAnchor.MiddleCenter, Color.white);
        UIKit.Center(pressStart.rectTransform, new Vector2(0, -190), new Vector2(900, 50));

        // Stage 2: menu
        menuPanel = UIKit.NewRect("MenuPanel", content).gameObject;
        UIKit.Stretch(menuPanel.GetComponent<RectTransform>());
        Vector2 size = new Vector2(380, 54);
        float y = 20f, step = -66f;
        startButton = UIKit.NewButton(menuPanel.transform, "Start Game", new Vector2(0, y), size, OnStartGame);
        continueButton = UIKit.NewButton(menuPanel.transform, "Continue", new Vector2(0, y + step), size, OnContinue);
        UIKit.NewButton(menuPanel.transform, "Options", new Vector2(0, y + step * 2), size, () => ShowPanel(Stage.Options));
        UIKit.NewButton(menuPanel.transform, "Credits", new Vector2(0, y + step * 3), size, () => ShowPanel(Stage.Credits));
        UIKit.NewButton(menuPanel.transform, "Exit Game", new Vector2(0, y + step * 4), size, OnQuit);
        continueButton.interactable = SaveSystem.HasSave;

        BuildOptions(content);
        BuildCredits(content);

        menuPanel.SetActive(false);
        optionsPanel.SetActive(false);
        creditsPanel.SetActive(false);
    }

    void BuildOptions(RectTransform content)
    {
        optionsPanel = UIKit.NewRect("OptionsPanel", content).gameObject;
        UIKit.Stretch(optionsPanel.GetComponent<RectTransform>());
        var t = optionsPanel.transform;

        var head = UIKit.NewText("Head", t, "OPTIONS", 44, TextAnchor.MiddleCenter, Color.white);
        UIKit.Center(head.rectTransform, new Vector2(0, 40), new Vector2(600, 60));

        Vector2 size = new Vector2(380, 50);
        volumeButton = UIKit.NewButton(t, "", new Vector2(0, -20), size, () => ChangeVolume(), 24);
        musicButton = UIKit.NewButton(t, "", new Vector2(0, -80), size, CycleMusic, 24);
        fullscreenButton = UIKit.NewButton(t, "", new Vector2(0, -140), size, ToggleFullscreen, 24);
        difficultyButton = UIKit.NewButton(t, "", new Vector2(0, -200), size, () => { Difficulty.Cycle(); RefreshOptionLabels(); }, 24);
        goreButton = UIKit.NewButton(t, "", new Vector2(0, -260), size, () => { Gore.Enabled = !Gore.Enabled; RefreshOptionLabels(); }, 24);
        UIKit.NewButton(t, "Back", new Vector2(0, -320), size, ShowMenu, 24);
        RefreshOptionLabels();
    }

    void BuildCredits(RectTransform content)
    {
        creditsPanel = UIKit.NewRect("CreditsPanel", content).gameObject;
        UIKit.Stretch(creditsPanel.GetComponent<RectTransform>());
        var t = creditsPanel.transform;

        string text =
            "MICTLÁN\nA game by Micta Forge\n\n" +
            "Jetzuvely del Carmen González Gutiérrez\n" +
            "Adrián Gallardo Medina\n" +
            "Antonio Enríquez Velasco\n" +
            "Alejandro Cornejo Pérez";
        var body = UIKit.NewText("Body", t, text, 30, TextAnchor.MiddleCenter, Color.white);
        UIKit.Center(body.rectTransform, new Vector2(0, 40), new Vector2(1000, 330));
        UIKit.NewButton(t, "Back", new Vector2(0, -230), new Vector2(380, 50), ShowMenu, 24);
    }

    // =====================================================================
    void ShowMenu()
    {
        stage = Stage.Menu;
        pressStart.gameObject.SetActive(false);
        optionsPanel.SetActive(false);
        creditsPanel.SetActive(false);
        menuPanel.SetActive(true);
        StartCoroutine(SelectNextFrame(startButton.gameObject));
    }

    void ShowPanel(Stage s)
    {
        stage = s;
        menuPanel.SetActive(false);
        var panel = s == Stage.Options ? optionsPanel : creditsPanel;
        panel.SetActive(true);
        var first = panel.GetComponentInChildren<Button>();
        if (first != null) StartCoroutine(SelectNextFrame(first.gameObject));
    }

    IEnumerator SelectNextFrame(GameObject go)
    {
        yield return null;
        UIKit.Select(go);
    }

    void OnStartGame()
    {
        GameSession.ContinueRequested = false;
        TutorialTips.ResetAll();
        SaveSystem.NewGame();               // the old save is overwritten at the first autosave
        SceneLoader.Load(SceneNames.Game);
    }

    void OnContinue()
    {
        GameSession.ContinueRequested = true;
        SceneLoader.Load(SceneNames.Game);
    }

    void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void CycleMusic()
    {
        float v = Audio.MusicVolume + 0.1f;
        Audio.MusicVolume = v > 1.001f ? 0f : v;
        RefreshOptionLabels();
    }

    void ChangeVolume()
    {
        float delta = volume >= 0.99f ? -1f : 0.25f;   // cycles 0 > 25 > 50 > 75 > 100
        volume = Mathf.Clamp01(volume + delta);
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("mictlan_volume", volume);
        RefreshOptionLabels();
    }

    void ToggleFullscreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
        StartCoroutine(RefreshAfterFrame());
    }

    IEnumerator RefreshAfterFrame()
    {
        yield return null;
        RefreshOptionLabels();
    }

    void RefreshOptionLabels()
    {
        UIKit.SetButtonLabel(volumeButton, $"Master Volume: {Mathf.RoundToInt(volume * 100f)}%");
        UIKit.SetButtonLabel(musicButton, $"Music Volume: {Mathf.RoundToInt(Audio.MusicVolume * 100f)}%");
        UIKit.SetButtonLabel(difficultyButton, "Difficulty: " + Difficulty.Level);
        UIKit.SetButtonLabel(goreButton, "Gore (17+): " + (Gore.Enabled ? "On" : "Off"));
        UIKit.SetButtonLabel(fullscreenButton, "Fullscreen: " + (Screen.fullScreen ? "On" : "Off"));
    }
}
