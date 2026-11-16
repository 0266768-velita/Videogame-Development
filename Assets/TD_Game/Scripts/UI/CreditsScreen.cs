using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// Final screen with credits (task 4.7). After winning the battle against the boss (EnemyData.isBoss):
    /// the game is finished, so the save is deleted, the credits scroll up and then the main menu loads.
    /// ENTER / SPACE / gamepad A skips to the end.
    /// </summary>
    public class CreditsScreen : MonoBehaviour
    {
        [SerializeField] CanvasGroup panel;
        [Tooltip("The text that scrolls up (child of a mask or of the panel).")]
        [SerializeField] RectTransform scrollingText;
        [SerializeField] Text creditsText;
        [SerializeField, Min(1f)] float scrollSeconds = 25f;
        [SerializeField, Min(0f)] float delayAfterVictory = 1.5f;
        [Tooltip("Loaded at the end. If it is not in Build Settings, the current scene restarts.")]
        [SerializeField] string mainMenuScene = SceneIds.MainMenu;
        [SerializeField, TextArea(10, 30)] string credits =
            "MICTLÁN\n\n" +
            "Tlahuelli crossed the nine levels of the underworld\nand defeated Quetzalcóatl.\n\n\n" +
            "A GAME BY\n\nAdrian\nAlex\nToño\nVely\n\n\n" +
            "PROGRAMMING\nAdrian · Alex · Toño\n\n" +
            "ART\nVely\n\n" +
            "GAME DESIGN\nThe whole team\n\n\n" +
            "Made with Unity 6\n\n\n" +
            "THANK YOU FOR PLAYING";

        bool playing;

        void Awake()
        {
            if (panel != null) { panel.alpha = 0f; panel.blocksRaycasts = false; }
            if (creditsText != null) creditsText.text = credits;
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

            panel.blocksRaycasts = true;
            for (float a = 0f; a < 1f; a += Time.unscaledDeltaTime / 1.2f)
            {
                panel.alpha = a;
                yield return null;
            }
            panel.alpha = 1f;

            // Scroll from below the screen to above it.
            var area = (RectTransform)scrollingText.parent;
            float start = -area.rect.height * 0.5f - scrollingText.rect.height * 0.5f;
            float end = area.rect.height * 0.5f + scrollingText.rect.height * 0.5f;
            for (float t = 0f; t < scrollSeconds; t += Time.unscaledDeltaTime)
            {
                scrollingText.anchoredPosition = new Vector2(0f, Mathf.Lerp(start, end, t / scrollSeconds));
                if (SkipPressed()) break;
                yield return null;
            }

            if (Application.CanStreamedLevelBeLoaded(mainMenuScene)) SceneFader.Load(mainMenuScene);
            else SceneFader.Load(SceneManager.GetActiveScene().name);
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
