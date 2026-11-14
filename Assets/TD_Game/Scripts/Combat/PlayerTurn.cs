using System.Collections;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// The player's turn: several actions while there is stamina, target selection and End Turn.
    /// Each slot holds an ability asset, so new abilities are added by swapping assets.
    /// </summary>
    public class PlayerTurn : MonoBehaviour
    {
        [SerializeField] CombatInput input;

        [Header("Action slots (order = keys 1-5 and the action menu)")]
        [SerializeField] CombatAbility attack;
        [SerializeField] CombatAbility skill1;
        [SerializeField] CombatAbility skill2;
        [SerializeField] CombatAbility potion;
        [SerializeField] CombatAbility ultimate;

        [SerializeField, Min(0f)] float outOfStaminaPause = 0.7f;

        CombatAbility requested;
        bool endRequested;
        CombatAbility[] slots;

        /// <summary>The five slots in menu order: attack, skill 1, skill 2, potion, ultimate.</summary>
        public CombatAbility[] Slots => slots ??= new[] { attack, skill1, skill2, potion, ultimate };
        public CombatAbility Attack => attack;
        public CombatAbility Skill1 => skill1;
        public CombatAbility Skill2 => skill2;
        public CombatAbility Potion => potion;
        public CombatAbility Ultimate => ultimate;
        public bool AcceptingInput { get; private set; }
        /// <summary>The ability being performed right now (null when waiting for input).</summary>
        public CombatAbility Performing { get; private set; }

        /// <summary>For UI buttons: asks for an ability as if its key had been pressed.</summary>
        public void Request(CombatAbility ability)
        {
            if (AcceptingInput && requested == null) requested = ability;
        }

        public void RequestEndTurn()
        {
            if (AcceptingInput) endRequested = true;
        }

        public IEnumerator Run(CombatContext context)
        {
            var player = context.Player;
            player.BeginTurn();
            context.SelectFirstAlive();
            requested = null;
            endRequested = false;
            CombatFeed.SetTurnLabel("YOUR TURN");

            while (!context.PlayerDead && !context.AllEnemiesDead)
            {
                AcceptingInput = true;
                ReadInput(context);
                if (endRequested) break;

                if (requested != null)
                {
                    var ability = requested;
                    requested = null;
                    AcceptingInput = false;

                    string reason = ability.GetBlockReason(context);
                    if (reason != null) CombatFeed.ShowBanner(reason, 0.8f);
                    else
                    {
                        Performing = ability;
                        yield return ability.Use(context);
                        Performing = null;
                    }

                    if (context.PlayerDead || context.AllEnemiesDead) break;
                    if (!CanDoAnything(context))
                    {
                        CombatFeed.ShowBanner("Out of stamina", 0.8f);
                        yield return new WaitForSeconds(outOfStaminaPause);
                        break;
                    }
                }
                yield return null;
            }

            AcceptingInput = false;
            player.EndTurn();
            CombatFeed.SetTurnLabel("");
        }

        void ReadInput(CombatContext context)
        {
            if (input.TargetNext.WasPressedThisFrame()) context.CycleTarget(1);
            if (input.TargetPrev.WasPressedThisFrame()) context.CycleTarget(-1);
            if (input.EndTurn.WasPressedThisFrame()) endRequested = true;
            if (requested != null) return;

            if (input.Attack.WasPressedThisFrame()) requested = attack;
            else if (input.Skill1.WasPressedThisFrame()) requested = skill1;
            else if (input.Skill2.WasPressedThisFrame()) requested = skill2;
            else if (input.Potion.WasPressedThisFrame()) requested = potion;
            else if (input.Ultimate.WasPressedThisFrame()) requested = ultimate;
        }

        bool CanDoAnything(CombatContext context)
        {
            foreach (var ability in Slots)
                if (ability != null && ability.GetBlockReason(context) == null) return true;
            return false;
        }
    }
}
