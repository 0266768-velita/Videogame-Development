using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TD_Game
{
    /// <summary>
    /// A timed press of ONE button (in the spirit of Clair Obscur: Expedition 33): the prompt appears during the last
    /// part of a wind-up and the player presses the button inside that window, right before the impact.
    ///   - inside the window = Good; in its last part = Perfect;
    ///   - too early = "TOO EARLY" and the button is LOCKED for a moment; every extra early press locks it longer,
    ///     so mashing leaves you locked when the real hit arrives;
    ///   - no press (or too late) = Miss.
    /// Used by the enemy turn (dodge / interrupt, Space or B) and by the critical of the Attack (1 or A).
    /// </summary>
    public static class ActionCommand
    {
        public enum Result { Miss, Good, Perfect }

        /// <param name="action">The only button that counts.</param>
        /// <param name="verb">DODGE, INTERRUPT or STRIKE: shown in the prompt.</param>
        /// <param name="duration">Total wind-up time; the impact is at its end.</param>
        /// <param name="window">Seconds before the impact in which the press counts.</param>
        /// <param name="perfectFraction">Last part of the window (fraction) that counts as Perfect.</param>
        /// <param name="lockout">Seconds the button is locked after a press out of time (x1.5 for each extra one).</param>
        /// <param name="anchor">Where the "TOO EARLY" / "LOCKED" feedback appears (the player).</param>
        public static IEnumerator Run(InputAction action, string verb, float duration, float window, float perfectFraction,
                                      float lockout, Transform anchor, Action<Result> onDone)
        {
            string label = action != null ? action.name : "Dodge";   // the ACTION: the UI shows its key, Xbox or PlayStation button
            float windowStart = duration - Mathf.Clamp(window, 0.05f, duration);
            float perfectFrom = windowStart + (duration - windowStart) * (1f - perfectFraction);
            float lockedUntil = -1f;
            float lockLength = lockout;
            var result = Result.Miss;
            int shown = -1;   // 0 hidden, 1 key box, 2 locked (only tell the UI when it changes)

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                bool open = t >= windowStart && result == Result.Miss;
                bool locked = t < lockedUntil;
                int state = !open ? 0 : locked ? 2 : 1;
                if (state != shown)
                {
                    // Locked: the key box hides, so it is clear that pressing now does nothing.
                    CombatFeed.SetQuickTime(state == 1 ? verb : null, label);
                    if (shown <= 0 || state == 0) CombatFeed.SetCounterPrompt(state > 0 ? $"{verb}!    [{InputPrompts.Label(label)}]" : null);
                    shown = state;
                }

                if (t > 0f && result == Result.Miss && action != null && action.WasPressedThisFrame())   // t > 0: not the press that chose the action
                {
                    if (t >= windowStart && !locked)
                    {
                        result = t >= perfectFrom ? Result.Perfect : Result.Good;
                        CombatFeed.SetQuickTime(null, null);
                        CombatFeed.SetCounterPrompt(null);
                        shown = 0;
                    }
                    else
                    {
                        // Out of time: locked, and longer for every extra press (anti-spam).
                        lockedUntil = t + lockLength;
                        lockLength *= 1.5f;
                        GameAudio.PlaySfx("error", 0.5f);
                        if (anchor != null)
                            CombatFeed.ShowPopup(anchor.position + Vector3.up * 1.4f, locked ? "LOCKED" : "TOO EARLY", MexicaPalette.Ash);
                    }
                }
                yield return null;
            }

            CombatFeed.SetQuickTime(null, null);
            CombatFeed.SetCounterPrompt(null);
            onDone?.Invoke(result);
        }
    }
}
