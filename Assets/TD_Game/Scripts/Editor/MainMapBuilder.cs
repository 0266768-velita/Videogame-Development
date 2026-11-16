using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// Creates the scene MainMap from Adrián's map (the Tilemaps of TestLevel: left room, rock block with a corridor,
    /// right room, ponds and pillars) and places the final content on it (task 4.3, difficulty curve):
    ///   start (bottom-left) -> zone 1: wolves -> zone 2: witches (top of the left room)
    ///   -> zone 3: the giant guarding the corridor -> right room: Quetzalcóatl (boss zone, the corridor closes behind you).
    /// The positions are fields at the top of this file, so they are easy to move when Adrián enlarges the map.
    /// It never overwrites MainMap: delete it to rebuild. Menu: TD_Game > Build Main Map (Adrian's map).
    /// </summary>
    public static class MainMapBuilder
    {
        public const string MainMapPath = "Assets/TD_Game/Scenes/Levels/MainMap.unity";
        const string TestLevelPath = "Assets/TD_Game/Scenes/Testing/TestLevel.unity";
        const string PrefabFolder = "Assets/TD_Game/Prefabs/Characters";
        const string GateSprite = "Assets/TD_Game/Art/Tilesets/Placeholder/Textures/Square_64.png";

        // ---------------- Layout on Adrián's map (world units, 1 tile = 1 unit) ----------------
        static readonly Vector2 PlayerStart = new Vector2(-12.5f, -7.5f);
        static readonly (string prefab, Vector2 position)[] Enemies =
        {
            ("Enemy_Wolf", new Vector2(-6.5f, -7.5f)),     // zone 1
            ("Enemy_Wolf", new Vector2(-11.5f, -1.5f)),    // zone 1
            ("Enemy_Witch", new Vector2(-11.5f, 5.5f)),    // zone 2
            ("Enemy_Witch", new Vector2(-5.5f, 6.5f)),     // zone 2
            ("Enemy_Giant", new Vector2(-2.5f, 0f)),       // zone 3: guards the corridor
        };
        static readonly Vector2 BossPosition = new Vector2(10.5f, 0f);
        static readonly Vector2 ArenaTrigger = new Vector2(7.5f, -0.5f);   // a band across the right room
        static readonly Vector2 ArenaTriggerSize = new Vector2(1f, 18f);
        static readonly Vector2 GatePosition = new Vector2(5.5f, 0f);      // closes the corridor (cells y -1 and 0)
        static readonly Vector2 GateSize = new Vector2(1f, 2f);

        [MenuItem("TD_Game/Build Main Map (Adrian's map)")]
        public static void BuildFromMenu() => Build(FrontEndBuilder.EnsureAudioLibrary());

        public static void Build(AudioLibrary library)
        {
            if (System.IO.File.Exists(MainMapPath)) { Debug.Log("[TD_Game] MainMap already exists (delete it to rebuild)."); return; }

            FrontEndBuilder.EnsureFolder("Assets/TD_Game/Scenes/Levels");
            AssetDatabase.CopyAsset(TestLevelPath, MainMapPath);
            var scene = EditorSceneManager.OpenScene(MainMapPath);

            // Remove the test enemies (and any orb): the final ones are placed below.
            foreach (var enemy in Object.FindObjectsByType<CombatEnemy>(FindObjectsInactive.Include))
                Object.DestroyImmediate(enemy.gameObject);
            foreach (var orb in Object.FindObjectsByType<SoulOrb>(FindObjectsInactive.Include))
                Object.DestroyImmediate(orb.gameObject);

            AddWallCollisions();
            PlacePlayer();
            var parent = new GameObject("Enemies").transform;
            int i = 0;
            foreach (var (prefab, position) in Enemies) Spawn(prefab, position, parent, $"{prefab}_{++i}");
            BuildBossZone();
            FrontEndBuilder.AddLevelServicesToOpenScene(library, MusicTrack.Explore);

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[TD_Game] MainMap built on Adrian's map.");
        }

        /// <summary>The stone of the Walls tilemap blocks the player and the enemies.</summary>
        static void AddWallCollisions()
        {
            foreach (var map in Object.FindObjectsByType<Tilemap>())
                if (map.name == "Walls" && map.GetComponent<TilemapCollider2D>() == null)
                    map.gameObject.AddComponent<TilemapCollider2D>();
        }

        static void PlacePlayer()
        {
            var player = Object.FindAnyObjectByType<PlayerCombatant>();
            if (player == null) return;
            player.transform.position = PlayerStart;
            // Tlahuelli with the pixel art of Mictlán v5.
            var visual = player.transform.Find("Visual");
            var frames = LegacyArtImporter.Frames("tlahuelli");
            if (visual != null && frames.Length > 0)
            {
                var sr = visual.GetComponent<SpriteRenderer>();
                sr.sprite = frames[0];
                sr.color = Color.white;
            }
            var camera = Object.FindAnyObjectByType<Camera>();
            if (camera != null) camera.transform.position = new Vector3(PlayerStart.x, PlayerStart.y, camera.transform.position.z);
        }

        static CombatEnemy Spawn(string prefabName, Vector2 position, Transform parent, string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{prefabName}.prefab");
            if (prefab == null) { Debug.LogError("[TD_Game] Missing prefab " + prefabName); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.position = position;
            return go.GetComponent<CombatEnemy>();
        }

        /// <summary>Right room: Quetzalcóatl. Walking in closes the corridor until the fight ends (task 4.7).</summary>
        static void BuildBossZone()
        {
            var zone = new GameObject("BossZone").transform;
            var boss = Spawn("Boss_Quetzalcoatl", BossPosition, zone, "Boss_Quetzalcoatl");

            var gate = new GameObject("BossGate");
            gate.transform.SetParent(zone, false);
            gate.transform.position = GatePosition;
            gate.transform.localScale = new Vector3(GateSize.x, GateSize.y, 1f);   // the square sprite is 1 x 1 tile
            var sprite = gate.AddComponent<SpriteRenderer>();
            sprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GateSprite);
            sprite.color = MexicaPalette.Cinnabar;
            gate.AddComponent<BoxCollider2D>();

            var arena = new GameObject("BossArena");
            arena.transform.SetParent(zone, false);
            arena.transform.position = ArenaTrigger;
            var box = arena.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = ArenaTriggerSize;
            var bossArena = arena.AddComponent<BossArena>();
            var so = new SerializedObject(bossArena);
            so.FindProperty("boss").objectReferenceValue = boss;
            var gates = so.FindProperty("gates");
            gates.arraySize = 1;
            gates.GetArrayElementAtIndex(0).objectReferenceValue = gate;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
