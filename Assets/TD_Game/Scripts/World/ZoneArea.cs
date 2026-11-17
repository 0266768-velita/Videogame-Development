using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// A territory of the map (Collider2D marked "Is Trigger" covering the zone).
    /// When the player walks in, its name appears big on the screen and fades away (ZoneTitleCard).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ZoneArea : MonoBehaviour
    {
        [SerializeField] string zoneName = "TERRITORIES OF TLÁLOC";
        [SerializeField] string subtitle = "The rainforest of the rain god";

        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerCombatant>() != null) ZoneTitleCard.Show(zoneName, subtitle);
        }
    }
}
