using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// A trigger area on the map that shows a hint the first time the player walks in (task 4.18).
    /// Needs a Collider2D marked Is Trigger.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TutorialZone : MonoBehaviour
    {
        [Tooltip("Unique id of the hint (it is remembered in the save).")]
        [SerializeField] string id = "zone_hint";
        [SerializeField, TextArea] string message = "Hint";

        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerCombatant>() != null) ContextTutorial.ShowOnce(id, message);
        }
    }
}
