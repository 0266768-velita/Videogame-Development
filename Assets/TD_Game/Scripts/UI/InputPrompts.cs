using System;
using System.Collections.Generic;
using UnityEngine;

namespace TD_Game
{
    /// <summary>Which icons the UI shows. Auto = the last device used (InputDevices).</summary>
    public enum IconMode { Auto, Keyboard, Xbox, PlayStation }

    /// <summary>
    /// The names of the buttons the UI shows, for keyboard / mouse, Xbox and PlayStation. Never two at once.
    /// Prompts ask by ACTION ("Dodge", "Interact", "Next"...) because the same key does different things
    /// (SPACE jumps in exploration = A, but dodges in combat = B). Raw keys ("1", "Q", "E") also work.
    /// ButtonGlyph draws them as icons; Label() / Localize() give plain text for sentences.
    /// </summary>
    public static class InputPrompts
    {
        const string ModeKey = "td_button_icons";

        // action -> (keyboard, Xbox). PlayStation is derived from the Xbox button (A = CROSS, LB = L1...).
        static readonly Dictionary<string, (string keyboard, string xbox)> Actions = new Dictionary<string, (string, string)>
        {
            { "Move", ("WASD", "L-STICK") }, { "Sprint", ("SHIFT", "RT") }, { "Jump", ("SPACE", "A") },
            { "Interact", ("E", "Y") }, { "Pause", ("ESC", "START") }, { "Map", ("M", "VIEW") },
            { "Attack", ("1", "A") }, { "Skill1", ("2", "X") }, { "Skill2", ("3", "Y") }, { "Potion", ("4", "D-PAD ▲") },
            { "Ultimate", ("5", "LB") }, { "Artifact", ("6", "RT") }, { "EndTurn", ("Q", "RB") },
            { "Target", ("◄ ►", "D-PAD ◄ ►") }, { "Dodge", ("SPACE", "B") }, { "Interrupt", ("SPACE", "B") },
            { "Next", ("SPACE", "A") }, { "Skip", ("ESC", "B") }, { "Confirm", ("ENTER", "A") }, { "Back", ("ESC", "B") },
            { "Click", ("CLICK", "A") },
        };

        // Old prompts give raw keyboard keys: their Xbox button.
        static readonly Dictionary<string, string> KeyToXbox = new Dictionary<string, string>
        {
            { "W", "Y" }, { "A", "X" }, { "S", "A" }, { "D", "B" }, { "J", "X" }, { "K", "Y" }, { "L", "B" },
            { "1", "A" }, { "2", "X" }, { "3", "Y" }, { "4", "D-PAD ▲" }, { "5", "LB" }, { "6", "RT" }, { "Q", "RB" },
            { "E", "Y" }, { "SPACE", "B" }, { "ESC", "B" }, { "ENTER", "A" }, { "TAB", "D-PAD ◄ ►" },
        };

        static readonly Dictionary<string, string> XboxToPlayStation = new Dictionary<string, string>
        {
            { "A", "CROSS" }, { "B", "CIRCLE" }, { "X", "SQUARE" }, { "Y", "TRIANGLE" },
            { "LB", "L1" }, { "RB", "R1" }, { "LT", "L2" }, { "RT", "R2" }, { "START", "OPTIONS" }, { "VIEW", "SHARE" },
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            changed = null;
            hooked = false;
        }

        static bool hooked;

        /// <summary>Raised when the icons must change (other device, or the option changed).</summary>
        public static event Action Changed
        {
            add { Hook(); changed += value; }
            remove => changed -= value;
        }
        static event Action changed;

        static void Hook()
        {
            if (hooked) return;
            hooked = true;
            InputDevices.Changed += _ => changed?.Invoke();
        }

        /// <summary>Option "Button icons" (saved). Auto by default.</summary>
        public static IconMode Mode
        {
            get => (IconMode)Mathf.Clamp(PlayerPrefs.GetInt(ModeKey, 0), 0, 3);
            set { PlayerPrefs.SetInt(ModeKey, (int)value); changed?.Invoke(); }
        }

        public static void CycleMode() => Mode = (IconMode)(((int)Mode + 1) % 4);

        public static InputScheme Scheme => Mode == IconMode.Auto ? InputDevices.Detected : (InputScheme)((int)Mode - 1);
        public static bool UseGamepad => Scheme != InputScheme.Keyboard;

        public static string SchemeName(InputScheme scheme) =>
            scheme == InputScheme.Keyboard ? "keyboard" : scheme == InputScheme.Xbox ? "Xbox" : "PlayStation";

        /// <summary>For the options menus: "Auto (Xbox)", "Keyboard"...</summary>
        public static string ModeLabel => Mode == IconMode.Auto ? $"Auto ({SchemeName(Scheme)})" : SchemeName((InputScheme)((int)Mode - 1));

        /// <summary>The button of an action (or raw key) for the current scheme: "SPACE", "B", "CIRCLE"...</summary>
        public static string Label(string actionOrKey) => LabelFor(actionOrKey, Scheme);

        public static string LabelFor(string actionOrKey, InputScheme scheme)
        {
            if (string.IsNullOrEmpty(actionOrKey)) return "";
            string keyboard, xbox;
            if (Actions.TryGetValue(actionOrKey, out var pair)) (keyboard, xbox) = pair;
            else
            {
                keyboard = actionOrKey.ToUpperInvariant();
                xbox = KeyToXbox.TryGetValue(keyboard, out var b) ? b : keyboard;
            }
            if (scheme == InputScheme.Keyboard) return keyboard;
            if (scheme == InputScheme.Xbox) return xbox;
            foreach (var kv in XboxToPlayStation)   // also inside "D-PAD ▲" style labels
                if (xbox == kv.Key) return kv.Value;
            return xbox;
        }

        /// <summary>Kept for old callers: the Xbox button of a keyboard key.</summary>
        public static string LabelForGamepad(string keyboardKey) => LabelFor(keyboardKey, InputScheme.Xbox);

        /// <summary>Replaces {Action} tokens in a sentence with [BUTTON]: "{Dodge} right before the hit" -> "[B] right before the hit".</summary>
        public static string Localize(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text;
            var result = new System.Text.StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                int close = text[i] == '{' ? text.IndexOf('}', i) : -1;
                if (close > i)
                {
                    result.Append('[').Append(Label(text.Substring(i + 1, close - i - 1))).Append(']');
                    i = close;
                }
                else result.Append(text[i]);
            }
            return result.ToString();
        }

        /// <summary>Colour of a face button, muted to the palette of the game (Xbox: A green, B red, X blue, Y yellow).</summary>
        public static Color BoxColor(string label, Color keyboardColor)
        {
            switch (label)
            {
                case "A": case "TRIANGLE": return MexicaPalette.Jade;
                case "B": case "CIRCLE": return MexicaPalette.Cinnabar;
                case "X": case "CROSS": return MexicaPalette.Turquoise;
                case "Y": return MexicaPalette.AgedGold;
                case "SQUARE": return new Color(0.78f, 0.45f, 0.6f);
                default: return UseGamepad ? MexicaPalette.Ash : keyboardColor;
            }
        }
    }
}
