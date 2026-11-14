using UnityEngine;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>The player's bars, always visible. They are named after the three animic forces of Mexica belief:
    /// teyolia (the heart, life), ihiyotl (the breath, stamina) and tonalli (the spirit, the special bar).</summary>
    public class PlayerStatusPanel : MonoBehaviour
    {
        [SerializeField] PlayerCombatant player;
        [SerializeField] UIBar health;
        [SerializeField] UIBar stamina;
        [SerializeField] UIBar tonalli;
        [SerializeField] Text info;
        [Tooltip("Shown and pulsing when the Tonalli bar is full.")]
        [SerializeField] Text tonalliReady;

        void Start()
        {
            Refresh();
            health.Snap(); stamina.Snap(); tonalli.Snap();
        }

        void Update() => Refresh();

        void Refresh()
        {
            if (player == null) return;
            var h = player.Health;
            health.Set(h.Current, h.Max, $"TEYOLIA · LIFE   {h.Current:0}/{h.Max:0}");
            stamina.Set(player.Stamina, player.MaxStamina, $"IHIYOTL · BREATH   {player.Stamina:0}/{player.MaxStamina:0}");
            tonalli.Set(player.Concentration, player.MaxConcentration, $"TONALLI · SPIRIT   {player.Concentration:0}/{player.MaxConcentration:0}");

            string buff = player.BuffTurnsLeft > 0 ? $"   FURY {player.BuffTurnsLeft}" : "";
            string weak = player.IsWeakened ? "   WEAKENED" : "";
            if (info != null)
                info.text = $"Potions {player.Potions}   Macuahuitl {player.ChargeNormalized * 100f:0}%{buff}{weak}";

            if (tonalliReady != null)
            {
                tonalliReady.enabled = player.IsConcentrationFull;
                float pulse = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f));
                tonalliReady.color = MexicaPalette.WithAlpha(MexicaPalette.Turquoise, pulse);
            }
        }
    }
}
