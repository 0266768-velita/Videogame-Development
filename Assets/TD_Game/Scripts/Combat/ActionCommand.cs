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
    }
}
