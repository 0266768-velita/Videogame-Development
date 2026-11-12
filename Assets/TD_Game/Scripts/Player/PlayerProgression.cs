using System;
using System.Collections.Generic;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Permanent progress of the player: unlocked abilities, growth from victories and the checkpoint.
    /// This is what the save system will store later.
    /// </summary>
    public class PlayerProgression : MonoBehaviour
    {
        [Tooltip("Abilities unlocked so far (abilities marked 'Unlocked By Default' do not need to be here).")]
        [SerializeField] List<CombatAbility> unlockedAbilities = new List<CombatAbility>();

        public float BonusMaxHealth { get; private set; }
        public float BonusMaxStamina { get; private set; }
        public int Victories { get; private set; }
        /// <summary>Where the player respawns after a defeat.</summary>
        public Vector3 Checkpoint { get; private set; }

        public event Action<CombatAbility> AbilityUnlocked;

        void Awake() => Checkpoint = transform.position;

        public bool IsUnlocked(CombatAbility ability) =>
            ability != null && (ability.unlockedByDefault || unlockedAbilities.Contains(ability));

        /// <summary>Returns true only if the ability was locked before.</summary>
        public bool Unlock(CombatAbility ability)
        {
            if (ability == null || IsUnlocked(ability)) return false;
            unlockedAbilities.Add(ability);
            AbilityUnlocked?.Invoke(ability);
            return true;
        }

        public void Grow(float maxHealthBonus, float maxStaminaBonus)
        {
            BonusMaxHealth += Mathf.Max(0f, maxHealthBonus);
            BonusMaxStamina += Mathf.Max(0f, maxStaminaBonus);
        }

        public void RegisterVictory() => Victories++;

        public void SetCheckpoint(Vector3 position) => Checkpoint = position;
    }
}
