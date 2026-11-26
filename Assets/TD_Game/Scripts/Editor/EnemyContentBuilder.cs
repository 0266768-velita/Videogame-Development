using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// Creates the data and the prefabs of the 3 enemies and the boss, and the Player prefab (tasks 3.8, 3.13, 3.17, 3.18, 4.6).
    /// Menu: TD_Game > Build Enemy Data And Prefabs.
    /// It NEVER overwrites: if an asset already exists it is kept, so the values you tune in the Inspector are safe.
    /// To rebuild one, delete its asset and run the menu again.
    /// </summary>
    public static class EnemyContentBuilder
    {
        const string DataFolder = "Assets/TD_Game/ScriptableObjects/Enemies";
        const string PrefabFolder = "Assets/TD_Game/Prefabs/Characters";
        const string AbilityFolder = "Assets/TD_Game/ScriptableObjects/Abilities";
        const string PlaceholderSprite = "Assets/TD_Game/Art/Characters/Placeholder/CharacterOutline_40x56.png";

        [MenuItem("TD_Game/Build Enemy Data And Prefabs")]
        public static void Build()
        {
            EnsureFolder(PrefabFolder);

            var wolf = EnsureData("EnemyData_Wolf", SetupWolf);
            var witch = EnsureData("EnemyData_Witch", SetupWitch);
            var giant = EnsureData("EnemyData_Giant", SetupGiant);
            var boss = EnsureData("EnemyData_Quetzalcoatl", SetupQuetzalcoatl);

            //                       data   name                  color                             scale  collider
            EnsurePrefab(wolf, "Enemy_Wolf", new Color(0.45f, 0.45f, 0.5f), 0.9f, new Vector2(0.7f, 0.35f));
            EnsurePrefab(witch, "Enemy_Witch", new Color(0.45f, 0.3f, 0.55f), 1f, new Vector2(0.5f, 0.35f));
            EnsurePrefab(giant, "Enemy_Giant", new Color(0.55f, 0.4f, 0.3f), 1.5f, new Vector2(0.9f, 0.5f));
            EnsurePrefab(boss, "Boss_Quetzalcoatl", new Color(0.2f, 0.55f, 0.45f), 2.2f, new Vector2(1.4f, 0.7f));

            EnsurePlayerPrefab();
            ToughenBoss(boss);
            AssetDatabase.SaveAssets();
            Debug.Log("[TD_Game] Enemy data and prefabs ready in " + DataFolder + " and " + PrefabFolder);
        }

        // ---------------- Enemy 1 (zone 1): Wolf. Fast, weak, attacks first, howls to grow stronger ----------------

        static void SetupWolf(EnemyData d)
        {
            d.displayName = "Cuetlachtli";
            d.maxHealth = 45f; d.attackPower = 9f; d.startsBattleFirst = true; d.bodyRadius = 0.4f;
            d.patrolSpeed = 2f; d.chaseSpeed = 4f; d.detectRadius = 5.5f; d.loseRadius = 9f; d.leashRadius = 12f;
            d.patrolRadius = 3f; d.patrolWait = 1f; d.alertAlliesRadius = 5f;
            d.attacks = new[]
            {
                Attack("Bite", 1f, CounterType.Dodge, weight: 3f),
                Attack("Frenzied Bites", 0.5f, CounterType.Dodge, hits: 3, windUp: 0.8f, window: 0.35f, between: 0.35f, weight: 2f),
                Support("Blood Howl", AttackEffect.EmpowerSelf, 1.3f, weight: 1f),
            };
            d.soulsOnDefeat = 10f; d.healthOnDefeat = 12f; d.concentrationOnDefeat = 15f;
            d.bonusMaxHealth = 5f;
        }

        // ---------------- Enemy 2 (zone 2): Witch. Spells (interrupt them), steals life, drains and weakens ----------------

        static void SetupWitch(EnemyData d)
        {
            d.displayName = "Tlahuelpuchi";
            d.maxHealth = 60f; d.attackPower = 12f; d.bodyRadius = 0.4f;
            d.patrolSpeed = 1.2f; d.chaseSpeed = 2.6f; d.detectRadius = 6f; d.loseRadius = 9f; d.leashRadius = 10f;
            d.patrolRadius = 2f; d.patrolWait = 2f;
            d.attacks = new[]
            {
                Attack("Soul Siphon", 1f, CounterType.Interrupt, lunge: false, windUp: 1.4f, window: 0.45f, weight: 2f,
                    effect: AttackEffect.LifeSteal, amount: 0.5f),
                Attack("Draining Curse", 0.6f, CounterType.Interrupt, lunge: false, windUp: 1.3f, window: 0.45f, weight: 2f,
                    effect: AttackEffect.DrainStamina, amount: 20f),
                Attack("Hex of Weakness", 0.4f, CounterType.Interrupt, lunge: false, windUp: 1.2f, window: 0.4f, weight: 1.5f,
                    effect: AttackEffect.Weaken, amount: 0.6f),
                Attack("Bone Shard", 0.8f, CounterType.Dodge, lunge: false, weight: 1f),
            };
            d.soulsOnDefeat = 15f; d.healthOnDefeat = 10f; d.concentrationOnDefeat = 30f;
            d.bonusMaxStamina = 10f;
            d.unlocksOnDefeat = new[] { LoadAbility("Ability_SeismicJump") };
        }

        // ---------------- Enemy 3 (zone 3): Giant. Slow tank, thick skin, quake that stuns, stone guard ----------------

        static void SetupGiant(EnemyData d)
        {
            d.displayName = "Quinametzin";
            d.maxHealth = 150f; d.attackPower = 16f; d.damageTakenMultiplier = 0.8f; d.bodyRadius = 0.6f;
            d.patrolSpeed = 1f; d.chaseSpeed = 2.2f; d.detectRadius = 4.5f; d.loseRadius = 7f; d.leashRadius = 8f;
            d.patrolRadius = 1.5f; d.patrolWait = 2.5f;
            d.attacks = new[]
            {
                Attack("Stone Fist", 1f, CounterType.Dodge, windUp: 1.6f, window: 0.5f, weight: 3f),
                Attack("Double Smash", 0.8f, CounterType.Dodge, hits: 2, windUp: 1.3f, window: 0.45f, between: 0.7f, weight: 1.5f),
                Attack("Ground Quake", 0.7f, CounterType.Unavoidable, lunge: false, windUp: 1.8f, weight: 1.2f,
                    effect: AttackEffect.Stun),
                Support("Stone Skin", AttackEffect.Guard, 0.5f, weight: 1f),
            };
            d.soulsOnDefeat = 25f; d.healthOnDefeat = 20f; d.concentrationOnDefeat = 20f;
            d.bonusMaxHealth = 15f;
            d.unlocksOnDefeat = new[] { LoadAbility("Ability_MictlanFury") };
        }

        // ---------------- Boss: Quetzalcóatl. Two phases ----------------

        static void SetupQuetzalcoatl(EnemyData d)
        {
            d.displayName = "Quetzalcóatl";
            d.isBoss = true;
            d.maxHealth = 520f; d.attackPower = 18f; d.startsBattleFirst = true; d.bodyRadius = 0.9f;
            d.patrolSpeed = 0.8f; d.chaseSpeed = 2.5f; d.detectRadius = 7f; d.loseRadius = 14f; d.leashRadius = 14f;
            d.patrolRadius = 1.5f; d.patrolWait = 3f;
            // Phase 1: Wind Serpent (Ehécatl).
            d.attacks = new[]
            {
                Attack("Tail Lash", 1f, CounterType.Dodge, weight: 3f),
                Attack("Feather Storm", 0.45f, CounterType.Dodge, lunge: false, hits: 4, windUp: 1f, window: 0.3f, between: 0.4f, weight: 2f),
                Attack("Ehecatl Wind", 0.6f, CounterType.Interrupt, lunge: false, windUp: 1.3f, window: 0.4f, weight: 1.5f,
                    effect: AttackEffect.DrainStamina, amount: 25f),
                Attack("Serpent Coil", 0.5f, CounterType.Dodge, windUp: 1.2f, window: 0.35f, weight: 1f,
                    effect: AttackEffect.Stun),
            };
            // Phase 2 at half health: Feathered Serpent.
            d.phaseTwoAtHealth = 0.5f;
            d.phaseTwoName = "FEATHERED SERPENT";
            d.phaseTwoPowerMultiplier = 1.25f;
            d.phaseTwoAttacks = new[]
            {
                Attack("Plumed Dive", 1.4f, CounterType.Dodge, windUp: 1f, window: 0.3f, weight: 2f),
                Attack("Solar Fire", 0.9f, CounterType.Interrupt, lunge: false, windUp: 1.2f, window: 0.35f, weight: 2f,
                    effect: AttackEffect.LifeSteal, amount: 0.5f),
                Attack("Hurricane", 0.4f, CounterType.Dodge, lunge: false, hits: 5, windUp: 0.9f, window: 0.28f, between: 0.3f, weight: 2f),
                Attack("Morning Star", 0.8f, CounterType.Unavoidable, lunge: false, windUp: 1.5f, weight: 0.8f,
                    effect: AttackEffect.Weaken, amount: 0.6f),
                Support("Rite of the Feathered Serpent", AttackEffect.EmpowerSelf, 1.2f, weight: 0.7f),
            };
            d.soulsOnDefeat = 0f; d.healthOnDefeat = 0f; d.concentrationOnDefeat = 0f;
        }

        /// <summary>
        /// Harder final fight (also applied to an EnemyData that already exists): more health and damage,
        /// it changes form earlier and hits much harder in its second form. It also acts twice per round (CombatManager).
        /// </summary>
        static void ToughenBoss(EnemyData d)
        {
            d.maxHealth = 760f;
            d.attackPower = 22f;
            d.phaseTwoAtHealth = 0.6f;
            d.phaseTwoPowerMultiplier = 1.4f;
            EditorUtility.SetDirty(d);
        }

        // ---------------- Helpers ----------------

        static EnemyAttack Attack(string name, float damage, CounterType counter, bool lunge = true, int hits = 1,
            float windUp = 1.2f, float window = 0.4f, float between = 0.6f, float weight = 1f,
            AttackEffect effect = AttackEffect.None, float amount = 0f)
        {
            return new EnemyAttack
            {
                attackName = name, damageMultiplier = damage, counter = counter, lungeAtTarget = lunge, hits = hits,
                windUpTime = windUp, counterWindow = window, timeBetweenHits = between, weight = weight,
                effect = effect, effectAmount = amount
            };
        }

        /// <summary>An ability that does no damage, only its effect (it cannot be countered).</summary>
        static EnemyAttack Support(string name, AttackEffect effect, float amount, float weight) =>
            Attack(name, 0f, CounterType.Unavoidable, lunge: false, windUp: 0.8f, weight: weight, effect: effect, amount: amount);

        static CombatAbility LoadAbility(string assetName)
        {
            var ability = AssetDatabase.LoadAssetAtPath<CombatAbility>($"{AbilityFolder}/{assetName}.asset");
            if (ability == null) Debug.LogWarning($"[TD_Game] Ability {assetName} not found in {AbilityFolder}.");
            return ability;
        }

        static EnemyData EnsureData(string assetName, System.Action<EnemyData> setup)
        {
            string path = $"{DataFolder}/{assetName}.asset";
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (data != null) return data;
            data = ScriptableObject.CreateInstance<EnemyData>();
            setup(data);
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        static void EnsurePrefab(EnemyData data, string prefabName, Color color, float scale, Vector2 colliderSize)
        {
            string path = $"{PrefabFolder}/{prefabName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            var root = new GameObject(prefabName);
            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var box = root.AddComponent<BoxCollider2D>();
            box.size = colliderSize;
            root.AddComponent<Health>();

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * scale;
            var sprite = visual.AddComponent<SpriteRenderer>();
            sprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSprite);
            sprite.color = color;
            sprite.spriteSortPoint = SpriteSortPoint.Pivot;

            var enemy = root.AddComponent<CombatEnemy>();
            var so = new SerializedObject(enemy);
            so.FindProperty("data").objectReferenceValue = data;
            so.FindProperty("sprite").objectReferenceValue = sprite;
            so.ApplyModifiedPropertiesWithoutUndo();
            root.AddComponent<EnemyAI>();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        /// <summary>Saves the Player of the open scene as a prefab (the scene object is not changed or linked).</summary>
        static void EnsurePlayerPrefab()
        {
            string path = $"{PrefabFolder}/Player.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            var player = Object.FindAnyObjectByType<PlayerCombatant>();
            if (player == null)
            {
                Debug.LogWarning("[TD_Game] Open TestLevel (it has the Player) to also create Player.prefab.");
                return;
            }
            PrefabUtility.SaveAsPrefabAsset(player.gameObject, path);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        // ---------------- Test enemies (2.12) ----------------

        [MenuItem("TD_Game/Add AI To Enemies In Scene")]
        public static void AddAIToSceneEnemies()
        {
            int added = 0;
            foreach (var enemy in Object.FindObjectsByType<CombatEnemy>(FindObjectsInactive.Include))
            {
                if (enemy.GetComponent<EnemyAI>() != null) continue;
                Undo.AddComponent<EnemyAI>(enemy.gameObject);
                added++;
            }
            if (added > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[TD_Game] EnemyAI added to {added} enemies.");
        }

        /// <summary>Batch mode: opens TestLevel, runs every builder and saves the scene.</summary>
        public static void BuildAllOnTestLevel()
        {
            EditorSceneManager.OpenScene("Assets/TD_Game/Scenes/Testing/TestLevel.unity");
            Build();
            AddAIToSceneEnemies();
            GameFlowUIBuilder.Build();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }
    }
}
