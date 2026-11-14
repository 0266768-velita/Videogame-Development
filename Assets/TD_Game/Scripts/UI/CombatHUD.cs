using UnityEngine;

namespace TD_Game
{
    /// <summary>Fades the combat-only part of the HUD (action menu, enemy bars) in and out with the battle.</summary>
    public class CombatHUD : MonoBehaviour
    {
        [SerializeField] CombatManager combat;
        [SerializeField] CanvasGroup combatGroup;
        [SerializeField, Min(0.01f)] float fadeTime = 0.25f;

        void Update()
        {
            if (combatGroup == null) return;
            bool show = combat != null && combat.InCombat;
            combatGroup.alpha = Mathf.MoveTowards(combatGroup.alpha, show ? 1f : 0f, Time.unscaledDeltaTime / fadeTime);
            combatGroup.blocksRaycasts = show;
            combatGroup.interactable = show;
        }
    }
}
