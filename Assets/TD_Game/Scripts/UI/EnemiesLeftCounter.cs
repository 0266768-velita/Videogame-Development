using UnityEngine;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// "Enemies Left: X" in the bottom right corner: how many servants of the gods still stand between Tlahuelli and
    /// the obsidian key (the boss and side-quest enemies do not count). Updates after every battle and when a save loads.
    /// Hidden during battles (the command menu lives in that corner).
    /// </summary>
    public class EnemiesLeftCounter : MonoBehaviour
    {
        [SerializeField] Text label;
        [SerializeField] CanvasGroup group;
        [SerializeField] string format = "Enemies Left: {0}";
        [SerializeField] string allDefeated = "Enemies Left: 0  -  find the obsidian key";

        int shown = -1;
        float refreshAt, pulse;

        void OnEnable()
        {
            CombatManager.CombatWon += OnCombatWon;
            CombatEnemy.Defeated += OnDefeated;
        }

        void OnDisable()
        {
            CombatManager.CombatWon -= OnCombatWon;
            CombatEnemy.Defeated -= OnDefeated;
        }

        void OnCombatWon(System.Collections.Generic.IReadOnlyList<EnemyData> _) => refreshAt = 0f;
        void OnDefeated(CombatEnemy _) => refreshAt = Time.time + 1.2f;   // after it fades away

        void Update()
        {
            if (group != null) group.alpha = Mathf.MoveTowards(group.alpha, GameState.Mode == GameMode.Combat || DialogueBox.IsOpen || WorldMapScreen.IsOpen ? 0f : 1f, Time.unscaledDeltaTime * 4f);
            if (Time.time >= refreshAt)
            {
                refreshAt = Time.time + 0.5f;
                int count = CombatEnemy.Remaining();
                if (count != shown)
                {
                    if (shown >= 0) pulse = 1f;
                    shown = count;
                    if (label != null) label.text = count > 0 ? string.Format(format, count) : allDefeated;
                }
            }
            if (pulse > 0f && label != null)
            {
                pulse = Mathf.Max(0f, pulse - Time.unscaledDeltaTime * 1.5f);
                label.transform.localScale = Vector3.one * (1f + 0.25f * pulse);
                label.color = Color.Lerp(MexicaPalette.Bone, MexicaPalette.AgedGold, pulse);
            }
        }
    }
}
