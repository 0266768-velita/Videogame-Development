using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Arrival point of a SceneDoor. If the player came through a door whose spawn name is this object's name,
    /// the player is moved here when the scene starts.
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        void Start()
        {
            if (SceneDoor.PendingSpawn != name) return;
            SceneDoor.PendingSpawn = null;

            var player = FindAnyObjectByType<PlayerCombatant>();
            if (player == null) return;
            player.Teleport(transform.position);
            player.Progression.SetCheckpoint(transform.position);
            var follow = FindAnyObjectByType<CameraFollow>();
            if (follow != null) follow.SnapToTarget();
        }
    }
}
