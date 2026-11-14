using UnityEngine;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// The combat command menu: a diagonal list of rows (keys 1-5 plus End Turn) on the right side.
    /// On the player's turn it slides in; on the enemy's turn it slides out and a clear
    /// "how to defend" bar replaces it, so nothing has to be read on a dimmed menu.
    /// </summary>
    public class CombatActionMenu : MonoBehaviour
    {
        [SerializeField] CombatManager combat;
        [Tooltip("One per PlayerTurn slot, in order: attack, skill 1, skill 2, potion, ultimate.")]
        [SerializeField] ActionButton[] buttons;
        [SerializeField] string[] keyHints = { "1", "2", "3", "4", "5" };
        [SerializeField] ActionButton endTurnButton;
        [Tooltip("Plate with the target, combo and the description of the action in progress.")]
        [SerializeField] Text hint;
        [SerializeField] CanvasGroup menuGroup;
        [Tooltip("Shown during the enemy's turn: which keys dodge and interrupt.")]
        [SerializeField] CanvasGroup enemyTurnBar;
        [SerializeField, Min(0f)] float slideDistance = 320f;
        [SerializeField, Min(0.01f)] float slideTime = 0.18f;

        RectTransform menuRect;
        Vector2 menuBase;
        float shown;   // 0 = hidden, 1 = visible

        void Awake()
        {
            if (menuGroup != null)
            {
                menuRect = (RectTransform)menuGroup.transform;
                menuBase = menuRect.anchoredPosition;
            }
        }

        void Update()
        {
            var context = combat != null ? combat.Context : null;
            if (context == null) { Slide(false, false); return; }
            var turn = combat.PlayerTurn;
            var slots = turn.Slots;
            bool playerTurn = turn.AcceptingInput || turn.Performing != null;

            for (int i = 0; i < buttons.Length; i++)
            {
                var ability = i < slots.Length ? slots[i] : null;
                buttons[i].gameObject.SetActive(ability != null);
                if (ability == null) continue;

                string key = i < keyHints.Length ? keyHints[i] : "";
                string cost = ability is TonalliBurstAbility
                    ? $"{context.Player.Concentration:0}/{context.Player.MaxConcentration:0}"
                    : $"{ability.staminaCost:0} ST";
                buttons[i].Setup(key, ability.displayName, cost, () => turn.Request(ability));
                buttons[i].Show(StateOf(ability, context, turn), ability is TonalliBurstAbility && context.Player.IsConcentrationFull);
            }

            if (endTurnButton != null)
            {
                endTurnButton.Setup("Q", "End turn", "", turn.RequestEndTurn);
                endTurnButton.Show(turn.AcceptingInput ? ActionButton.State.Ready : ActionButton.State.Waiting, false);
            }

            if (hint != null) hint.text = HintText(context, turn);
            Slide(playerTurn, !playerTurn);
        }

        void Slide(bool menuVisible, bool enemyBarVisible)
        {
            shown = Mathf.MoveTowards(shown, menuVisible ? 1f : 0f, Time.unscaledDeltaTime / slideTime);
            float eased = Mathf.SmoothStep(0f, 1f, shown);
            if (menuGroup != null)
            {
                menuGroup.alpha = eased;
                menuGroup.interactable = menuGroup.blocksRaycasts = menuVisible;
                menuRect.anchoredPosition = menuBase + Vector2.right * (1f - eased) * slideDistance;
            }
            if (enemyTurnBar != null)
                enemyTurnBar.alpha = Mathf.MoveTowards(enemyTurnBar.alpha, enemyBarVisible ? 1f : 0f, Time.unscaledDeltaTime / slideTime);
        }

        static ActionButton.State StateOf(CombatAbility ability, CombatContext context, PlayerTurn turn)
        {
            if (turn.Performing == ability) return ActionButton.State.Active;
            if (!context.Player.Progression.IsUnlocked(ability)) return ActionButton.State.Locked;
            if (!turn.AcceptingInput) return ActionButton.State.Waiting;
            return ability.GetBlockReason(context) == null ? ActionButton.State.Ready : ActionButton.State.Unaffordable;
        }

        static string HintText(CombatContext context, PlayerTurn turn)
        {
            if (turn.Performing != null)
                return turn.Performing is AttackAbility ? "Press ATTACK again on  NOW!  for a critical" : turn.Performing.description;
            var target = context.CurrentTarget;
            string combo = context.Player.ComboCount > 0 ? $"     Next hit  x{context.Player.ComboCount + 1}" : "";
            return $"◄ ►  Target:  {(target != null ? target.Data.displayName : "-")}{combo}";
        }
    }
}
