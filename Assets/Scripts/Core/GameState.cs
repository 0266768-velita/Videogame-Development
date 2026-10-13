using System;
using UnityEngine;

/// <summary>
/// Global game mode. The player, enemies and camera check this to know whether
/// they should move (Exploration) or stay still (Combat, Paused, ...).
/// </summary>
public enum GameMode { Exploration, Combat, Cutscene, Paused }

public static class GameState
{
    public static GameMode Mode { get; private set; } = GameMode.Exploration;
    public static event Action<GameMode> OnModeChanged;

    public static void SetMode(GameMode newMode)
    {
        if (newMode == Mode) return;
        Mode = newMode;
        OnModeChanged?.Invoke(newMode);
    }

    // Needed if "Domain Reload" is disabled in Project Settings > Editor > Enter Play Mode Options.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Mode = GameMode.Exploration;
        OnModeChanged = null;
    }
}
