using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// Pause menu (adapted from Mictlán v5's PauseMenu). ESC / Start during exploration.
    /// Resume, music and effects volume, Main Menu (the game is saved after every victory).
    /// Put one in each level scene.
    /// </summary>
    public class PauseScreen : MonoBehaviour
    {
        [SerializeField] string mainMenuScene = SceneIds.MainMenu;
        [SerializeField] Font menuFont;          // Pirata One

        GameObject panel;
        Button resumeButton, musicButton, sfxButton, controllerButton;
        bool paused;

        void Start()
        {
            MenuWidgets.Use(menuFont, true);
            var root = MenuWidgets.Screen("PauseCanvas", 50, transform);
            panel = MenuWidgets.Stretch(MenuWidgets.Rect("PausePanel", root)).gameObject;
            var t = panel.transform;
            MenuWidgets.Stretch(MenuWidgets.Image("Shade", t, new Color(0f, 0f, 0f, 0.7f)).rectTransform);
            var head = MenuWidgets.Label("Head", t, "PAUSED", 70, MexicaPalette.AgedGold);
            head.fontStyle = FontStyle.Bold;
            MenuWidgets.Center(head.rectTransform, new Vector2(0, 200), new Vector2(800, 100));

            Vector2 size = new Vector2(460, 60);
            resumeButton = MenuWidgets.Button(t, "Resume", new Vector2(0, 70), size, Resume);
            musicButton = MenuWidgets.Button(t, "", new Vector2(0, -10), size, () => { GameAudio.MusicVolume = Cycle(GameAudio.MusicVolume); Refresh(); }, 28);
            sfxButton = MenuWidgets.Button(t, "", new Vector2(0, -90), size, () => { GameAudio.SfxVolume = Cycle(GameAudio.SfxVolume); Refresh(); }, 28);
            controllerButton = MenuWidgets.Button(t, "", new Vector2(0, -170), size, () => { InputPrompts.GamepadEnabled = !InputPrompts.GamepadEnabled; Refresh(); }, 26);
            MenuWidgets.Button(t, "Main Menu", new Vector2(0, -250), size, ToMainMenu);
            var note = MenuWidgets.Label("Note", t, "Your progress is saved after every victory.", 24, MexicaPalette.Ash);
            MenuWidgets.Center(note.rectTransform, new Vector2(0, -330), new Vector2(900, 40));
            Refresh();
            panel.SetActive(false);
        }

        void Update()
        {
            if (!MenuWidgets.PausePressed()) return;
            if (paused) Resume();
            else if (GameState.IsExploring) Pause();
        }

        void Pause()
        {
            paused = true;
            GameState.SetMode(GameMode.Paused);
            Time.timeScale = 0f;
            panel.SetActive(true);
            GameAudio.PlaySfx("ui_select", 1f, 0f);
            StartCoroutine(SelectNextFrame());
        }

        void Resume()
        {
            if (!paused) return;
            paused = false;
            Time.timeScale = 1f;
            panel.SetActive(false);
            GameState.SetMode(GameMode.Exploration);
            GameAudio.PlaySfx("ui_back", 1f, 0f);
        }

        void ToMainMenu()
        {
            paused = false;
            Time.timeScale = 1f;
            SceneFader.Load(mainMenuScene);
        }

        IEnumerator SelectNextFrame()
        {
            yield return null;
            MenuWidgets.Select(resumeButton.gameObject);
        }

        static float Cycle(float value) => value >= 0.99f ? 0f : Mathf.Min(1f, Mathf.Round(value * 4f + 1f) / 4f);

        void Refresh()
        {
            MenuWidgets.SetLabel(musicButton, $"Music Volume: {Mathf.RoundToInt(GameAudio.MusicVolume * 100f)}%");
            MenuWidgets.SetLabel(sfxButton, $"Effects Volume: {Mathf.RoundToInt(GameAudio.SfxVolume * 100f)}%");
            MenuWidgets.SetLabel(controllerButton, "Controller buttons: " + (!InputPrompts.GamepadEnabled ? "Off (keyboard)" : UnityEngine.InputSystem.Gamepad.current != null ? "On (Xbox)" : "On (no controller found)"));
        }

        void OnDestroy()
        {
            if (paused) Time.timeScale = 1f;
        }
    }
}
