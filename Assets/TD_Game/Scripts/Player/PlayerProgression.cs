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

        /// <summary>Where the game starts. After a defeat you go back HERE, not to the last checkpoint.</summary>
        public Vector3 StartPoint { get; private set; }

        void Awake()
        {
            StartPoint = transform.position;
            Checkpoint = transform.position;
        }

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

        // ---------------- Gold (enemies give it, the shop takes it) ----------------

        public int Gold { get; private set; }

        public void AddGold(int amount) => Gold += Mathf.Max(0, amount);

        /// <summary>Pays if there is enough gold. Returns false (and pays nothing) if not.</summary>
        public bool TrySpendGold(int price)
        {
            if (Gold < price) return false;
            Gold -= price;
            return true;
        }

        public void LoadGold(int amount) => Gold = Mathf.Max(0, amount);

        // ---------------- Obsidian key and shop artifact ----------------

        /// <summary>The obsidian key that opens Quetzalcóatl's door.</summary>
        public bool HasKey { get; private set; }
        public void GiveKey() => HasKey = true;

        public void SetCheckpoint(Vector3 position) => Checkpoint = position;

        /// <summary>Abilities in the list (for the save).</summary>
        public IReadOnlyList<CombatAbility> UnlockedAbilities => unlockedAbilities;

        /// <summary>Loads saved progress (SaveController). Does not raise AbilityUnlocked.</summary>
        public void LoadSaved(float bonusMaxHealth, float bonusMaxStamina, int victories, Vector3 checkpoint,
            IEnumerable<CombatAbility> abilities)
        {
            BonusMaxHealth = Mathf.Max(0f, bonusMaxHealth);
            BonusMaxStamina = Mathf.Max(0f, bonusMaxStamina);
            Victories = Mathf.Max(0, victories);
            Checkpoint = checkpoint;
            foreach (var ability in abilities)
                if (ability != null && !unlockedAbilities.Contains(ability)) unlockedAbilities.Add(ability);
        }
    }
}
