using UnityEngine;
using UnityEngine.UI;

/// <summary>Pause menu (Esc / Start). Exploration only. Resume, Save, Main Menu.</summary>
public class PauseMenu : MonoBehaviour
{
    GameObject root;
    Text savedLabel;
    Button resumeButton;
    GameObject controlsPanel, menuButtons;
    bool built;

    void Update()
    {
        if (!UIKit.PausePressed()) return;

        if (GameState.Mode == GameMode.Paused) Resume();
        else if (GameState.Mode == GameMode.Exploration) Pause();
    }

    void Pause()
    {
        Build();
        root.SetActive(true);
        ShowControls(false);
        savedLabel.text = "";
        GameState.SetMode(GameMode.Paused);
        Time.timeScale = 0f;
        UIKit.Select(resumeButton.gameObject);
    }

    void Resume()
    {
        if (root != null) root.SetActive(false);
        Time.timeScale = 1f;
        GameState.SetMode(GameMode.Exploration);
    }

    void ShowControls(bool show)
    {
        controlsPanel.SetActive(show);
        menuButtons.SetActive(!show);
        savedLabel.text = "";
        if (!show) UIKit.Select(resumeButton.gameObject);
    }

    void Save()
    {
        var player = FindFirstObjectByType<Player2D>();
        var stats = player != null ? player.GetComponent<PlayerCombatStats>() : null;
        if (player == null || stats == null) return;
        stats.SetCheckpoint(player.Position);
        SaveSystem.Save(stats, player.Position);
        savedLabel.text = "Game saved";
    }

    void ToMainMenu()
    {
        Time.timeScale = 1f;
        GameState.SetMode(GameMode.Exploration);
        SceneLoader.Load(SceneNames.MainMenu);
    }

    void Build()
    {
        if (built) return;
        built = true;
        UIKit.EnsureEventSystem();

        var content = UIKit.CreateScreen("PauseCanvas", 40, transform);
        root = content.transform.parent.gameObject;

        UIKit.Stretch(UIKit.NewImage("Dim", content, new Color(0, 0, 0, 0.6f)).rectTransform);
        var head = UIKit.NewText("Head", content, "PAUSED", 64, TextAnchor.MiddleCenter, Color.white);
        UIKit.Center(head.rectTransform, new Vector2(0, 170), new Vector2(600, 90));

        Vector2 size = new Vector2(380, 54);
        menuButtons = UIKit.NewRect("Buttons", content).gameObject;
        UIKit.Stretch(menuButtons.GetComponent<RectTransform>());
        var mb = menuButtons.transform;
        resumeButton = UIKit.NewButton(mb, "Resume", new Vector2(0, 80), size, Resume);
        UIKit.NewButton(mb, "Controls", new Vector2(0, 14), size, () => ShowControls(true));
        UIKit.NewButton(mb, "Save Game", new Vector2(0, -52), size, Save);
        UIKit.NewButton(mb, "Main Menu", new Vector2(0, -118), size, ToMainMenu);

        controlsPanel = UIKit.NewRect("Controls", content).gameObject;
        UIKit.Stretch(controlsPanel.GetComponent<RectTransform>());
        var body = UIKit.NewText("Body", controlsPanel.transform,
            "EXPLORATION\n" +
            "Move: WASD / Arrows / Left stick      Sprint: Shift / LB\n" +
            "Hop: Space / A      Minimap: M / Select      Pause: Esc / Start\n\n" +
            "COMBAT\n" +
            "Attack: 1 / A      Fury: 2 / X      Seismic: 3 / Y      Potion: 4 / D-pad Up\n" +
            "End turn: Q / RB      Change target: Left / Right\n" +
            "Dodge: D / B      Interrupt a spell: Attack",
            26, TextAnchor.MiddleCenter, Color.white);
        UIKit.Center(body.rectTransform, new Vector2(0, 50), new Vector2(1500, 360));
        UIKit.NewButton(controlsPanel.transform, "Back", new Vector2(0, -190), size, () => ShowControls(false));

        savedLabel = UIKit.NewText("Saved", content, "", 24, TextAnchor.MiddleCenter, new Color(0.5f, 1f, 0.6f));
        UIKit.Center(savedLabel.rectTransform, new Vector2(0, -140), new Vector2(600, 40));
    }
}
