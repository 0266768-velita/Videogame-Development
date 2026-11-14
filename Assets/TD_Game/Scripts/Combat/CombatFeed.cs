using System;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Messages from the combat logic to whatever UI is listening (today CombatDebugHUD,
    /// later the Canvas combat HUD). The combat code never talks to the UI directly.
    /// </summary>
    public static class CombatFeed
    {
        /// <summary>Big centred message: text and seconds on screen.</summary>
        public static event Action<string, float> Banner;
        /// <summary>Whose turn it is ("" hides it).</summary>
        public static event Action<string> TurnLabel;
        /// <summary>Counter prompt such as "DODGE!" (null hides it).</summary>
        public static event Action<string> CounterPrompt;
        /// <summary>Floating number or word at a world position.</summary>
        public static event Action<Vector3, string, Color> Popup;

        /// <summary>Camera shake: amplitude in tiles and duration in seconds.</summary>
        public static event Action<float, float> Shake;
        /// <summary>Hit stop: the game freezes for a few hundredths of a second so big hits feel heavy.</summary>
        public static event Action<float> HitStop;

        public static void RequestShake(float amplitude, float duration) => Shake?.Invoke(amplitude, duration);
        public static void RequestHitStop(float seconds) => HitStop?.Invoke(seconds);
        public static void ShowBanner(string text, float seconds) => Banner?.Invoke(text, seconds);
        public static void SetTurnLabel(string text) => TurnLabel?.Invoke(text);
        public static void SetCounterPrompt(string text) => CounterPrompt?.Invoke(text);
        public static void ShowPopup(Vector3 worldPosition, string text, Color color) => Popup?.Invoke(worldPosition, text, color);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Banner = null;
            TurnLabel = null;
            CounterPrompt = null;
            Popup = null;
            Shake = null;
            HitStop = null;
        }
    }
}
