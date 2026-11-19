using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TD_Game
{
    /// <summary>
    /// A timed button press ("action command"): the prompt appears during the last part of a wind-up
    /// and the player must press the button inside that window. Pressing too early locks the button
    /// for a moment, so mashing never works. Used for critical hits; the enemy counters use the same idea.
    /// </summary>
    public static class ActionCommand
    {
        public enum Result { Miss, Good, Perfect }

        /// <param name="duration">Total wind-up time.</param>
        /// <param name="window">Seconds at the end of the wind-up in which the press counts.</param>
        /// <param name="perfectFraction">Last part of the window (fraction) that counts as Perfect.</param>
        public static IEnumerator Run(InputAction action, float duration, float window, float perfectFraction,
                                      float earlyLockout, string prompt, Action<Result> onDone)
        {
            float windowStart = duration - Mathf.Clamp(window, 0.05f, duration);
            float perfectFrom = windowStart + (duration - windowStart) * (1f - perfectFraction);
            float lockedUntil = 0f;
            var result = Result.Miss;

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                bool open = t >= windowStart;
                CombatFeed.SetCounterPrompt(open && result == Result.Miss ? prompt : null);

                if (result == Result.Miss && action != null && action.WasPressedThisFrame())
                {
                    if (open && t >= lockedUntil) result = t >= perfectFrom ? Result.Perfect : Result.Good;
                    else lockedUntil = t + earlyLockout;
                }
                yield return null;
            }

            CombatFeed.SetCounterPrompt(null);
            onDone?.Invoke(result);
        }

        // ---------------- Quick time event: a RANDOM key each time (harder than always the same button) ----------------

        /// <summary>Keys to defend (dodge / interrupt): W A S D. Gamepad: Y X A B.</summary>
        public static readonly Key[] DefendKeys = { Key.W, Key.A, Key.S, Key.D };
        /// <summary>Keys for the critical hit of an attack (combo): J K L. Gamepad: X Y B.</summary>
        public static readonly Key[] StrikeKeys = { Key.J, Key.K, Key.L };

        public static IEnumerator RunQuickTime(string verb, float duration, float window, float perfectFraction,
                                               float earlyLockout, Action<Result> onDone) =>
            RunQuickTime(verb, DefendKeys, duration, window, perfectFraction, earlyLockout, onDone);

        /// <summary>
        /// Like Run, but the key changes every time (one of `keys`). The prompt shows "Press [A] to DODGE!",
        /// with the Xbox button instead when the controller prompts are on. Pressing a wrong key counts as a failure.
        /// </summary>
        public static IEnumerator RunQuickTime(string verb, Key[] keys, float duration, float window, float perfectFraction,
                                               float earlyLockout, Action<Result> onDone)
        {
            int k = UnityEngine.Random.Range(0, keys.Length);
            string label = InputPrompts.Label(keys[k].ToString());
            float windowStart = duration - Mathf.Clamp(window, 0.05f, duration);
            float perfectFrom = windowStart + (duration - windowStart) * (1f - perfectFraction);
            float lockedUntil = 0f;
            var result = Result.Miss;
            bool failed = false;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                bool open = t >= windowStart && !failed && result == Result.Miss;
                CombatFeed.SetQuickTime(open ? verb : null, label);
                CombatFeed.SetCounterPrompt(open ? $"{verb}!    [{label}]" : null);

                int pressed = PressedKey(keys);
                if (result == Result.Miss && !failed && pressed >= 0)
                {
                    if (pressed != k) { failed = t >= windowStart; lockedUntil = t + earlyLockout; }   // wrong key
                    else if (t >= windowStart && t >= lockedUntil) result = t >= perfectFrom ? Result.Perfect : Result.Good;
                    else lockedUntil = t + earlyLockout;                                               // too early
                }
                yield return null;
            }
            CombatFeed.SetQuickTime(null, null);
            CombatFeed.SetCounterPrompt(null);
            onDone?.Invoke(result);
        }

        /// <summary>Which of the keys (or its gamepad button) was pressed this frame, or -1.</summary>
        static int PressedKey(Key[] keys)
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
                for (int i = 0; i < keys.Length; i++)
                    if (keyboard[keys[i]].wasPressedThisFrame) return i;
            var pad = Gamepad.current;
            if (pad == null) return -1;
            for (int i = 0; i < keys.Length; i++)
            {
                switch (InputPrompts.LabelForGamepad(keys[i].ToString()))
                {
                    case "Y": if (pad.buttonNorth.wasPressedThisFrame) return i; break;
                    case "X": if (pad.buttonWest.wasPressedThisFrame) return i; break;
                    case "A": if (pad.buttonSouth.wasPressedThisFrame) return i; break;
                    case "B": if (pad.buttonEast.wasPressedThisFrame) return i; break;
                }
            }
            return -1;
        }
    }
}
