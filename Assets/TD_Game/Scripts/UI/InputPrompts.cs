using UnityEngine;
using UnityEngine.InputSystem;

namespace TD_Game
{
    /// <summary>
    /// Which buttons the game SHOWS: keyboard (default) or Xbox controller. Never both at the same time.
    /// Xbox buttons appear only when the option is On in Options AND a controller is connected.
    /// </summary>
    public static class InputPrompts
    {
        const string Key = "td_gamepad_prompts";

        /// <summary>Option "Controller buttons" (saved). Default: Off = keyboard.</summary>
        public static bool GamepadEnabled
        {
            get => PlayerPrefs.GetInt(Key, 0) == 1;
            set => PlayerPrefs.SetInt(Key, value ? 1 : 0);
        }

        public static bool UseGamepad => GamepadEnabled && Gamepad.current != null;

        /// <summary>The label to show for a keyboard key: the key itself, or its Xbox button.</summary>
        public static string Label(string keyboardKey) => UseGamepad ? LabelForGamepad(keyboardKey) : keyboardKey;

        /// <summary>The Xbox button that does the same as this keyboard key.</summary>
        public static string LabelForGamepad(string keyboardKey)
        {
            switch (keyboardKey)
            {
                case "W": return "Y";
                case "A": return "X";
                case "S": return "A";
                case "D": return "B";
                case "J": return "X";
                case "K": return "Y";
                case "L": return "B";
                case "1": return "A";
                case "2": return "X";
                case "3": return "Y";
                case "4": return "↑";
                case "5": return "LB";
                case "6": return "RT";
                case "Q": return "RB";
                case "E": return "Y";
                default: return keyboardKey;
            }
        }

        /// <summary>Colour of the box: Xbox colours for the controller (A green, B red, X blue, Y yellow).</summary>
        public static Color BoxColor(string label, Color keyboardColor)
        {
            if (!UseGamepad) return keyboardColor;
            switch (label)
            {
                case "A": return new Color(0.35f, 0.75f, 0.25f);
                case "B": return new Color(0.85f, 0.2f, 0.2f);
                case "X": return new Color(0.2f, 0.45f, 0.9f);
                case "Y": return new Color(0.95f, 0.8f, 0.15f);
                default: return new Color(0.75f, 0.75f, 0.75f);
            }
        }
    }
}
