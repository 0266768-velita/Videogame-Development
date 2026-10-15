using UnityEngine;

/// <summary>
/// Lives in the Game scene. Prepares the session: new game or continue from the save file.
/// </summary>
[DefaultExecutionOrder(-200)]
public class GameBootstrap : MonoBehaviour
{
    bool continuing;

    void Awake()
    {
        Time.timeScale = 1f;
        GameState.SetMode(GameMode.Exploration);

        // Decided BEFORE enemies run Start(), so defeated enemies do not respawn.
        continuing = GameSession.ContinueRequested && SaveSystem.Load();
        GameSession.ContinueRequested = false;
        if (!continuing) GameProgress.Reset();

        // The level (ground, rivers, props, pyramid, enemies) is built from Resources/Level/level.json.
        // It needs the save data loaded first, so defeated enemies do not respawn.
        gameObject.AddComponent<LevelBuilder>();
    }

    void Start()
    {
        if (FindFirstObjectByType<Minimap>() == null) gameObject.AddComponent<Minimap>();
        if (!continuing) TutorialTips.Instance.StartExplorationGuide();
        if (!continuing) return;

        var player = FindFirstObjectByType<Player2D>();
        if (player == null) return;

        var stats = player.GetComponent<PlayerCombatStats>();
        if (stats != null) SaveSystem.ApplyTo(stats);

        if (SaveSystem.TryGetPosition(out var pos))
        {
            player.Teleport(pos);
            if (stats != null) stats.SetCheckpoint(pos);
        }

        var cam = FindFirstObjectByType<CameraFollow2D>();
        if (cam != null) cam.SnapToTarget();
    }
}
