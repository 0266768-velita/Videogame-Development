using UnityEngine;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>Shows the player's gold on screen. Just reads the number every frame.</summary>
    public class GoldCounter : MonoBehaviour
    {
        [SerializeField] PlayerProgression player;
        [SerializeField] Text label;

        void Update()
        {
            if (player != null && label != null) label.text = "GOLD  " + player.Gold;
        }
    }
}
