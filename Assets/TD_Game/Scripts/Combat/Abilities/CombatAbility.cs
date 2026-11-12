using System.Collections;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Base class of every player action in combat (attack, skills, potion).
    /// To add a NEW ability: create a class that inherits from this one, implement Execute,
    /// create its asset and assign it to a slot of PlayerTurn. No other code changes are needed.
    /// </summary>
    public abstract class CombatAbility : ScriptableObject
    {
        [Header("General")]
        public string displayName = "Ability";
        [TextArea] public string description;
        [Min(0f)] public float staminaCost = 20f;
        [Tooltip("Available from the start. Off = must be unlocked (enemy reward or Last Stand).")]
        public bool unlockedByDefault = true;
        public bool oncePerBattle = false;

        /// <summary>Why the ability cannot be used right now, or null if it can.</summary>
        public virtual string GetBlockReason(CombatContext context)
        {
            var player = context.Player;
            if (!player.Progression.IsUnlocked(this)) return "You have not learned this skill yet";
            if (oncePerBattle && player.WasUsedThisBattle(this)) return $"{displayName} already used this battle";
            if (!player.CanAfford(staminaCost)) return "Not enough stamina";
            return null;
        }

        /// <summary>Pays the cost and performs the ability. Call GetBlockReason first.</summary>
        public IEnumerator Use(CombatContext context)
        {
            if (!context.Player.TrySpend(staminaCost)) yield break;
            context.Player.MarkUsed(this);
            yield return Execute(context);
        }

        /// <summary>Performs the ability without paying (Last Stand, story events).</summary>
        public IEnumerator ExecuteFree(CombatContext context) => Execute(context);

        protected abstract IEnumerator Execute(CombatContext context);
    }
}
