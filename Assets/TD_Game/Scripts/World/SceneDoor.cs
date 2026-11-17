using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Door to another map (scene). Walk into it and the game fades to black, loads the other scene and puts the
    /// player on the SpawnPoint with the same name. Needs a Collider2D marked "Is Trigger".
    /// The other scene must be in File > Build Profiles > Scene List.
    /// </summary>
    public class SceneDoor : MonoBehaviour
    {
        [SerializeField] string sceneToLoad = "MainMap";
        [Tooltip("Name of the SpawnPoint where the player appears in the other scene.")]
        [SerializeField] string spawnPointName = "FromDoor";

        /// <summary>Where the player must appear after the next scene loads (read by SpawnPoint).</summary>
        public static string PendingSpawn;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!GameState.IsExploring || other.GetComponentInParent<PlayerCombatant>() == null) return;
            PendingSpawn = spawnPointName;
            GameAudio.PlaySfx("door");
            SceneFader.Load(sceneToLoad);
        }
    }
}
