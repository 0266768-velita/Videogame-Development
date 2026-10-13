/// <summary>Scene names (must match the scenes listed in Build Settings).</summary>
public static class SceneNames
{
    public const string Boot = "Boot";
    public const string MainMenu = "MainMenu";
    public const string Game = "Game";
}

/// <summary>Data that travels from the main menu to the game scene.</summary>
public static class GameSession
{
    public static bool ContinueRequested;
}
