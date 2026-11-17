using System.Collections.Generic;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Save and continue in the level (task 4.19). Put ONE in each level scene.
    /// - Saves automatically after every victory (the checkpoint is set there) and when Save() is called.
    /// - Loads when the level starts if the menu chose "Continue" (GameSave.ContinueRequested).
    /// Saves: checkpoint, growth, victories, potions, Tonalli, unlocked abilities, defeated enemies and seen tutorials.
    /// Health is not saved: the player always continues with full health.
    /// </summary>
    public class SaveController : MonoBehaviour
    {
        [SerializeField] PlayerCombatant player;
        [Tooltip("EVERY ability that can be unlocked, so the save can find them by name.")]
        [SerializeField] CombatAbility[] allAbilities;
        [Tooltip("For testing in the editor: load the save even without pressing Continue in the menu.")]
        [SerializeField] bool alwaysLoadInEditor = false;

        readonly HashSet<string> defeated = new HashSet<string>();

        public static SaveController Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            if (player == null) player = FindAnyObjectByType<PlayerCombatant>();
        }

        void OnEnable()
        {
            CombatEnemy.Defeated += OnEnemyDefeated;
            CombatManager.CombatWon += OnCombatWon;
        }

        void OnDisable()
        {
            CombatEnemy.Defeated -= OnEnemyDefeated;
            CombatManager.CombatWon -= OnCombatWon;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            bool load = GameSave.ContinueRequested || (Application.isEditor && alwaysLoadInEditor);
            GameSave.ContinueRequested = false;
            if (load) Load();
        }

        void OnEnemyDefeated(CombatEnemy enemy)
        {
            // The boss is not remembered: after the credits the save is deleted anyway.
            if (!enemy.Data.isBoss) defeated.Add(enemy.SaveId);
        }

        void OnCombatWon(IReadOnlyList<EnemyData> _) => Save();

        public void Save()
        {
            if (player == null) return;
            var progression = player.Progression;
            var data = new SaveData
            {
                scene = gameObject.scene.name,
                checkpointX = progression.Checkpoint.x,
                checkpointY = progression.Checkpoint.y,
                bonusMaxHealth = progression.BonusMaxHealth,
                bonusMaxStamina = progression.BonusMaxStamina,
                victories = progression.Victories,
                potions = player.Potions,
                gold = progression.Gold,
                hasKey = progression.HasKey,
                concentration = player.Concentration,
                defeatedEnemies = new List<string>(defeated),
                seenTutorials = new List<string>(ContextTutorial.Seen)
            };
            foreach (var ability in progression.UnlockedAbilities)
                if (ability != null) data.abilities.Add(ability.name);
            GameSave.Write(data);
        }

        void Load()
        {
            var data = GameSave.Read();
            if (data == null || player == null) return;
            if (!string.IsNullOrEmpty(data.scene) && data.scene != gameObject.scene.name)
                Debug.LogWarning($"[{nameof(SaveController)}] The save is from scene {data.scene}; loading it here anyway.", this);

            var abilities = new List<CombatAbility>();
            foreach (string abilityName in data.abilities)
            {
                var ability = FindAbility(abilityName);
                if (ability != null) abilities.Add(ability);
                else Debug.LogWarning($"[{nameof(SaveController)}] Ability '{abilityName}' is not in All Abilities.", this);
            }

            var checkpoint = new Vector3(data.checkpointX, data.checkpointY, player.transform.position.z);
            player.Progression.LoadSaved(data.bonusMaxHealth, data.bonusMaxStamina, data.victories, checkpoint, abilities);
            player.FullRestore();
            player.AddPotions(data.potions - player.Potions);
            player.Progression.LoadGold(data.gold);
            if (data.hasKey) player.Progression.GiveKey();
            player.ClearConcentration();
            player.AddConcentration(data.concentration);
            player.Teleport(checkpoint);

            defeated.Clear();
            foreach (string id in data.defeatedEnemies) defeated.Add(id);
            foreach (var enemy in FindObjectsByType<CombatEnemy>())
                if (defeated.Contains(enemy.SaveId)) enemy.gameObject.SetActive(false);

            ContextTutorial.MarkSeen(data.seenTutorials);
            var follow = FindAnyObjectByType<CameraFollow>();
            if (follow != null) follow.SnapToTarget();
        }

        CombatAbility FindAbility(string abilityName)
        {
            if (allAbilities == null) return null;
            foreach (var a in allAbilities)
                if (a != null && a.name == abilityName) return a;
            return null;
        }
    }
}
