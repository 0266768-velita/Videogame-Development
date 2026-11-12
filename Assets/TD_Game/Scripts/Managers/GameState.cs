using System;
using UnityEngine;

namespace TD_Game
{
    /// <summary>Global game mode. Player, enemies and camera check it to know whether they may act.</summary>
    public enum GameMode { Exploration, Combat, Paused, Cutscene }

    /// <summary>
    /// Global game state, adapted from the previous game (Mictlán v5).
    /// Only Exploration lets the player and enemies move freely on the map.
    /// </summary>
    public static class GameState
    {
        public static GameMode Mode { get; private set; } = GameMode.Exploration;
        public static bool IsExploring => Mode == GameMode.Exploration;

        /// <summary>Raised with the new mode every time it changes.</summary>
        public static event Action<GameMode> ModeChanged;

        public static void SetMode(GameMode newMode)
        {
            if (newMode == Mode) return;
            Mode = newMode;
            ModeChanged?.Invoke(newMode);
        }

        // Keeps the state clean when "Enter Play Mode Options" disables the domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Mode = GameMode.Exploration;
            ModeChanged = null;
        }
    }
}
