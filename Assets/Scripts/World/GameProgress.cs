using UnityEngine;

/// <summary>
/// Story progress: the obsidian key (dropped when every normal enemy is dead) and the boss.
/// Saved by SaveSystem together with the rest of the game.
/// </summary>
public static class GameProgress
{
    public static bool HasKey;
    public static bool KeyDropped;
    public static Vector2 KeyPosition;
    public static bool BossDefeated;
    public static int EnemiesDefeated;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset()
    {
        HasKey = false; KeyDropped = false; KeyPosition = Vector2.zero; BossDefeated = false; EnemiesDefeated = 0;
    }

    /// <summary>Normal (non-boss) enemies still alive in the level.</summary>
    public static int NormalEnemiesAlive()
    {
        int n = 0;
        foreach (var e in EnemyController.Active)
            if (e != null && !e.data.isBoss && e.CurrentState != EnemyController.State.Defeated && !e.Health.IsDead) n++;
        return n;
    }

    /// <summary>Called after every victory. If the level is cleared, the key drops near the player.</summary>
    public static void TryDropKey(Vector2 nearPosition)
    {
        if (KeyDropped || HasKey || BossDefeated) return;
        if (NormalEnemiesAlive() > 0) return;

        Vector2 pos = nearPosition + new Vector2(0f, 1.3f);
        if (!Hazards.IsSafe(pos, 0.3f)) pos = nearPosition;
        KeyDropped = true;
        KeyPosition = pos;
        KeyPickup.Spawn(pos);
        Audio.Play("key", 0.8f);
        TutorialTips.Instance.Toast(null, "Every enemy is dead... something shiny dropped nearby. Pick up the OBSIDIAN KEY!", 8f);
    }
}
