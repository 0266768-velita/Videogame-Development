using UnityEngine;

namespace TD_Game
{
    /// <summary>The treasure of the brazier puzzle: walk into it to open it. Needs a Collider2D marked "Is Trigger".</summary>
    public class TreasureChest : MonoBehaviour
    {
        [SerializeField] BrazierPuzzle puzzle;

        void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerCombatant>();
            if (player != null && puzzle != null) puzzle.OpenTreasure(player);
        }
    }
}
