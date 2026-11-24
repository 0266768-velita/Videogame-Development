using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// Full Game Over screen (task 4.18). CombatRewards.Defeat waits for it:
    /// the player chooses "Retry" (back to the checkpoint) or "Main Menu".
    /// Keys: ENTER / SPACE / gamepad A = retry, ESC / gamepad B = main menu.
    /// </summary>
    public class DefeatScreen : MonoBehaviour
    {
        public static DefeatScreen Instance { get; private set; }

        [SerializeField] CanvasGroup panel;
        [SerializeField] Text tipText;
        [SerializeField] Button retryButton;
        [SerializeField] Button menuButton;
        [Tooltip("Scene loaded by Main Menu. If it is not in Build Settings, the button retries instead.")]
        [SerializeField] string mainMenuScene = SceneIds.MainMenu;
        [SerializeField, Min(0.01f)] float fadeTime = 0.6f;
        [Tooltip("Seconds before the choice is accepted (avoids skipping it by accident while mashing keys).")]
        [SerializeField, Min(0f)] float inputDelay = 1f;
        [SerializeField, TextArea] string[] tips =
        {
            "Wait for the last moment to dodge: a PERFECT dodge gives stamina, Tonalli and a counter-attack.",
            "Spells cannot be dodged. Interrupt them with 1.",
            "Ending your turn with stamina left is fine: you recover part of it every turn.",
            "Jump onto an enemy before the battle to AMBUSH it and act first.",
            "Green orbs heal you. Collect them before the next fight.",
            "The Giant's quake cannot be dodged. Keep your health high before it charges.",
            "Quetzalcóatl changes form at half health. Save your Tonalli Burst for that moment."
        };

        enum Choice { None, Retry, Menu }
        Choice choice;

        void Awake()
        {
            Instance = this;
            Hide();
            if (retryButton != null) retryButton.onClick.AddListener(() => choice = Choice.Retry);
            if (menuButton != null) menuButton.onClick.AddListener(() => choice = Choice.Menu);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Shows the screen and waits until the player chooses. Called during the defeat.</summary>
        public IEnumerator Show()
        {
            choice = Choice.None;
            // The keys follow the device in use: [ENTER] / [A] / [CROSS] and [ESC] / [B] / [CIRCLE].
            SetLabel(retryButton, "RETRY FROM THE BEGINNING   {Confirm}");   // a defeat sends you back to the start of the map
            SetLabel(menuButton, "MAIN MENU   {Back}");
            GameAudio.PlaySfx("defeat");
            GameAudio.PlayMusic(MusicTrack.GameOver);
            if (tipText != null && tips.Length > 0) tipText.text = "TIP: " + tips[Random.Range(0, tips.Length)];
            panel.blocksRaycasts = true;
            panel.interactable = true;
            for (float a = 0f; a < 1f; a += Time.unscaledDeltaTime / fadeTime)
            {
                panel.alpha = a;
                yield return null;
            }
            panel.alpha = 1f;

            yield return new WaitForSecondsRealtime(inputDelay);
            while (choice == Choice.None)
            {
                ReadKeys();
                yield return null;
            }

            GameAudio.PlaySfx(choice == Choice.Menu ? "ui_back" : "ui_select");
            if (choice == Choice.Menu && Application.CanStreamedLevelBeLoaded(mainMenuScene))
            {
                SceneFader.Load(mainMenuScene);
                yield break;
            }
            Hide();
        }

        void ReadKeys()
        {
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            if ((keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                || (pad != null && pad.buttonSouth.wasPressedThisFrame))
                choice = Choice.Retry;
            else if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || (pad != null && pad.buttonEast.wasPressedThisFrame))
                choice = Choice.Menu;
        }

        static void SetLabel(Button button, string text)
        {
            var label = button != null ? button.GetComponentInChildren<Text>() : null;
            if (label != null) label.text = InputPrompts.Localize(text);
        }

        void Hide()
        {
            if (panel == null) return;
            panel.alpha = 0f;
            panel.blocksRaycasts = false;
            panel.interactable = false;
        }
    }
}
