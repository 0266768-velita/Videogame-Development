using System.Collections;
using UnityEngine;

/// <summary>Enter / leave the boss chamber inside the pyramid (fade, teleport, new respawn point).</summary>
public class BossRoom : MonoBehaviour
{
    static BossRoom instance;
    public static bool Inside { get; private set; }
    public static Vector2 EntryPoint, OutsidePoint;
    bool busy;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { instance = null; Inside = false; }

    public static void Setup(Vector2 entry, Vector2 outside)
    {
        EntryPoint = entry; OutsidePoint = outside; Inside = false;
        if (instance == null) instance = new GameObject("BossRoom").AddComponent<BossRoom>();
    }

    public static void Enter(Player2D player)
    {
        if (instance == null || instance.busy || Inside) return;
        instance.StartCoroutine(instance.Travel(player, true));
    }

    public static void Leave(Player2D player)
    {
        if (instance == null || instance.busy || !Inside) return;
        instance.StartCoroutine(instance.Travel(player, false));
    }

    IEnumerator Travel(Player2D player, bool enter)
    {
        busy = true;
        GameState.SetMode(GameMode.Cutscene);
        Audio.Play("door", 0.7f);
        yield return ScreenFade.Out(0.6f);

        Vector2 target = enter ? EntryPoint : OutsidePoint;
        player.Teleport(target);
        var stats = player.GetComponent<PlayerCombatStats>();
        if (stats != null) stats.SetCheckpoint(target);
        var cam = FindFirstObjectByType<CameraFollow2D>();
        if (cam != null) cam.SnapToTarget();
        Inside = enter;

        yield return new WaitForSeconds(0.2f);
        yield return ScreenFade.In(0.6f);
        GameState.SetMode(GameMode.Exploration);

        if (enter)
            TutorialTips.Instance.Toast("boss_intro", "Quetzalcoatl, the Feathered Serpent, guards the chamber. Heal up and be ready. You cannot leave once the fight starts.", 9f);
        busy = false;
    }
}

/// <summary>Dark doorway at the west end of the chamber that leads back outside.</summary>
public class RoomExit : MonoBehaviour
{
    Player2D player;
    float cooldown;

    void Update()
    {
        if (!BossRoom.Inside || GameState.Mode != GameMode.Exploration) return;
        if (player == null) { player = FindFirstObjectByType<Player2D>(); if (player == null) return; }
        if (Vector2.Distance(player.Position, transform.position) < 1.0f) BossRoom.Leave(player);
    }
}
