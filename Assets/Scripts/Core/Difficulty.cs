using UnityEngine;

public enum DifficultyLevel { Easy, Normal, Hard }

/// <summary>
/// Global difficulty (Options menu). It scales how much damage enemies deal and how long the
/// dodge / interrupt window stays open. Saved in PlayerPrefs.
/// Numbers come from a combat simulation (see README, "Balance"): Normal is the tuned baseline.
/// </summary>
public static class Difficulty
{
    const string Key = "mictlan_difficulty";
    static int cached = -1;

    public static DifficultyLevel Level
    {
        get
        {
            if (cached < 0) cached = Mathf.Clamp(PlayerPrefs.GetInt(Key, (int)DifficultyLevel.Normal), 0, 2);
            return (DifficultyLevel)cached;
        }
        set
        {
            cached = (int)value;
            PlayerPrefs.SetInt(Key, cached);
            PlayerPrefs.Save();
        }
    }

    /// <summary>Multiplier on damage dealt BY enemies.</summary>
    public static float EnemyDamage
    {
        get
        {
            switch (Level)
            {
                case DifficultyLevel.Easy: return 0.7f;
                case DifficultyLevel.Hard: return 1.3f;
                default: return 1f;
            }
        }
    }

    /// <summary>Multiplier on the length of the dodge / interrupt window.</summary>
    public static float CounterWindow
    {
        get
        {
            switch (Level)
            {
                case DifficultyLevel.Easy: return 1.3f;
                case DifficultyLevel.Hard: return 0.8f;
                default: return 1f;
            }
        }
    }

    public static void Cycle()
    {
        Level = (DifficultyLevel)(((int)Level + 1) % 3);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { cached = -1; }
}
