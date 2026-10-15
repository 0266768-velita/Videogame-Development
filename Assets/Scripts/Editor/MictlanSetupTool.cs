#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds everything the game needs inside Unity: sprite import settings, EnemyData assets, enemy and player
/// prefabs and the three scenes (Boot, MainMenu, Game) wired together and added to Build Settings.
/// The LEVEL itself (ground, rivers, trees, pyramid, enemy spawns) is built at runtime by LevelBuilder from
/// Resources/Level/level.json, so the Game scene is tiny.
///
/// It runs AUTOMATICALLY the first time the project is opened (see MictlanAutoSetup below).
/// You can re-run it any time from the menu:  Mictlan > Rebuild Scenes.
/// Existing prefabs and EnemyData assets are kept (your tuning is never overwritten);
/// the three scenes are recreated from scratch, so back them up first if you edited them by hand.
/// </summary>
public static class MictlanSetupTool
{
    public const string BootScenePath = "Assets/Scenes/Boot.unity";
    public const string FinalPrefabPath = "Assets/Resources/Enemies/Enemy_Quetzalcoatl.prefab";

    const string LogoPath = "Assets/Art/Branding/MictaForge_Logo.png";
    const string PrefabDir = "Assets/Prefabs";
    const string EnemyDir = "Assets/Resources/Enemies";
    const string SceneDir = "Assets/Scenes";

    [MenuItem("Mictlan/Rebuild Scenes (Boot, MainMenu, Game)")]
    static void MenuRebuild()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        CreateAll(false);
    }

    public static void CreateAll(bool silent)
    {
        EnsureFolder("Assets/Art");
        EnsureFolder("Assets/Resources"); EnsureFolder(EnemyDir);
        EnsureFolder(PrefabDir);
        EnsureFolder(SceneDir);

        Physics2D.gravity = Vector2.zero;

        ConfigureTextures();
        Texture2D logo = PrepareLogo();

        var dog = MakeDogData();
        var witch = MakeWitchData();
        var giant = MakeGiantData();
        var boss = MakeBossData();
        MakeEnemyPrefab("Dog", dog, 0.5f);
        MakeEnemyPrefab("Witch", witch, 0.5f);
        MakeEnemyPrefab("Giant", giant, 1.0f);
        MakeEnemyPrefab("Quetzalcoatl", boss, 1.8f);
        var playerPrefab = MakePlayerPrefab();

        CreateBootScene(logo);
        CreateMenuScene();
        CreateGameScene(playerPrefab);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(SceneDir + "/Boot.unity", true),
            new EditorBuildSettingsScene(SceneDir + "/MainMenu.unity", true),
            new EditorBuildSettingsScene(SceneDir + "/Game.unity", true),
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(BootScenePath);

        Debug.Log("[Mictlan] Project ready. Scenes Boot, MainMenu and Game created. Open Boot and press Play.");
        if (!silent)
            EditorUtility.DisplayDialog("Mictlan",
                "Done.\n\n- Scenes Boot, MainMenu and Game were rebuilt and added to Build Settings.\n- Open Boot and press Play to see the whole flow.\n- Set the Game view aspect to 16:9.", "OK");
    }

    // ------------------------------------------------------------------
    //  Texture import settings (pixel art: no filtering, no compression)
    // ------------------------------------------------------------------
    static void ConfigureTextures()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Sprites", "Assets/Resources/UI" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;
            bool sprites = path.Contains("/Resources/Sprites/");
            bool readable = sprites && !path.EndsWith("ground.png") && !path.Contains("water_");   // readable: gore cuts enemy sprites into pieces
            bool dirty = imp.filterMode != FilterMode.Point || imp.mipmapEnabled || imp.textureCompression != TextureImporterCompression.Uncompressed
                         || imp.isReadable != readable || imp.maxTextureSize != 2048 || imp.npotScale != TextureImporterNPOTScale.None;
            if (!dirty) continue;
            imp.textureType = TextureImporterType.Default;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.isReadable = readable;
            imp.maxTextureSize = 2048;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
        }
    }

    // ------------------------------------------------------------------
    //  Scenes
    // ------------------------------------------------------------------
    static Camera NewCamera(Color bg)
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = bg;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 50f;
        go.transform.position = new Vector3(0f, 0f, -10f);
        go.AddComponent<AudioListener>();
        go.AddComponent<AspectRatioLock>();
        return cam;
    }

    static void CreateBootScene(Texture2D logo)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        NewCamera(Color.black);
        var splash = new GameObject("BootSplash").AddComponent<BootSplash>();
        splash.studioLogo = logo;   // Micta Forge logo; replace it here with any other texture
        EditorSceneManager.SaveScene(scene, SceneDir + "/Boot.unity");
    }

    static void CreateMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        NewCamera(Color.black);
        new GameObject("MainMenu").AddComponent<MainMenu>();
        EditorSceneManager.SaveScene(scene, SceneDir + "/MainMenu.unity");
    }

    static void CreateGameScene(GameObject playerPrefab)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = NewCamera(new Color(0.02f, 0.02f, 0.04f));
        cam.orthographicSize = 6.5f;

        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.position = new Vector3(0.5f, -21f, 0);
        cam.transform.position = new Vector3(0.5f, -21f, -10);

        var follow = cam.gameObject.AddComponent<CameraFollow2D>();
        follow.target = player.transform;
        follow.useBounds = true;      // the bounds are set by LevelBuilder from level.json

        // Session start (also builds the level) + pause menu
        var boot = new GameObject("GameBootstrap");
        boot.AddComponent<GameBootstrap>();
        boot.AddComponent<PauseMenu>();

        EditorSceneManager.SaveScene(scene, SceneDir + "/Game.unity");
    }

    // ------------------------------------------------------------------
    //  Prefabs (existing ones are kept so your edits survive a rebuild)
    // ------------------------------------------------------------------
    static GameObject MakePlayerPrefab()
    {
        string path = PrefabDir + "/Tlahuelli.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
        {
            if (existing.GetComponent<CharacterVisuals>() != null) return existing;
            AssetDatabase.DeleteAsset(path);    // old prefab from a previous version (no sprites): rebuild it
        }

        var root = new GameObject("Tlahuelli");
        root.tag = "Player";

        var model = new GameObject("Model");
        model.transform.SetParent(root.transform, false);
        model.AddComponent<SpriteRenderer>();

        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        var col = root.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;

        var health = root.AddComponent<Health>();
        health.maxHealth = 60f;                 // starts weak: grows as enemies are defeated
        root.AddComponent<ActorFeedback>();
        root.AddComponent<PlayerCombatStats>();
        root.AddComponent<YSort>();
        var cv = root.AddComponent<CharacterVisuals>();
        cv.sheet = "tlahuelli"; cv.cellWidth = 18; cv.cellHeight = 26; cv.frames = 4;
        cv.feetOffset = 0.4f; cv.directional = true;
        root.AddComponent<Player2D>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static GameObject MakeEnemyPrefab(string id, EnemyData data, float colliderRadius)
    {
        string path = EnemyDir + "/Enemy_" + id + ".prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var root = new GameObject("Enemy_" + id);

        var model = new GameObject("Model");
        model.transform.SetParent(root.transform, false);
        model.AddComponent<SpriteRenderer>();

        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        var col = root.AddComponent<CircleCollider2D>();
        col.radius = colliderRadius;

        root.AddComponent<Health>();
        root.AddComponent<ActorFeedback>();
        root.AddComponent<YSort>();
        root.AddComponent<CharacterVisuals>();     // reads the sprite sheet from the EnemyData
        var ec = root.AddComponent<EnemyController>();
        ec.data = data;

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ------------------------------------------------------------------
    //  Enemy data (created once; edit the assets freely afterwards)
    // ------------------------------------------------------------------
    static EnemyData MakeDogData()
    {
        return LoadOrCreate<EnemyData>(EnemyDir + "/Itzcuintli_Hound.asset", d =>
        {
            d.displayName = "Itzcuintli";
            d.archetype = EnemyArchetype.Hound;
            d.maxHealth = 45f; d.attackPower = 14f; d.soulsOnDefeat = 4f;
            d.bonusMaxHealth = 3f; d.bonusMaxStamina = 1f;
            d.bodyRadius = 0.8f; d.popupHeight = 1.3f;
            d.spriteSheet = "dog"; d.frameWidth = 26; d.frameHeight = 18; d.feetOffset = 0.4f;
            d.gorePieceCols = 3; d.gorePieceRows = 1; d.goreAmount = 0.7f;
            d.patrolRadius = 5f; d.patrolSpeed = 1.5f; d.chaseSpeed = 3.6f;
            d.detectionRadius = 8f; d.fieldOfView = 160f; d.hearingRadius = 3f; d.loseSightRadius = 11f;
            d.engageDistance = 2.2f;
            d.attacks = new[]
            {
                new EnemyAttackData { attackName = "Bite", damageMultiplier = 1.0f, hits = 1, windUpTime = 0.9f, counterWindow = 0.5f, weight = 1f },
                new EnemyAttackData { attackName = "Pounce", damageMultiplier = 0.6f, hits = 2, windUpTime = 0.8f, counterWindow = 0.45f, timeBetweenHits = 0.6f, weight = 1f },
            };
        });
    }

    static EnemyData MakeWitchData()
    {
        return LoadOrCreate<EnemyData>(EnemyDir + "/Witch_Tlazolteotl.asset", d =>
        {
            d.displayName = "Witch";
            d.archetype = EnemyArchetype.Witch;
            d.maxHealth = 90f; d.attackPower = 25f; d.soulsOnDefeat = 10f;
            d.bonusMaxHealth = 6f; d.bonusMaxStamina = 2f;
            d.unlocksSeismic = true;     // the first Witch teaches Seismic Jump
            d.bodyRadius = 0.6f; d.popupHeight = 1.9f;
            d.spriteSheet = "witch"; d.frameWidth = 22; d.frameHeight = 30; d.feetOffset = 0.5f;
            d.gorePieceCols = 2; d.gorePieceRows = 2; d.goreAmount = 1f;
            d.patrolRadius = 4f; d.patrolSpeed = 1.3f; d.chaseSpeed = 2.8f;
            d.detectionRadius = 9f; d.fieldOfView = 130f; d.hearingRadius = 3f; d.loseSightRadius = 12f;
            d.engageDistance = 4f;
            d.attacks = new[]
            {
                new EnemyAttackData { attackName = "Arcane Bolt", damageMultiplier = 1.0f, hits = 1, windUpTime = 1.0f, counterWindow = 0.35f, lungeAtTarget = false, weight = 1f },
                new EnemyAttackData { attackName = "Hex", damageMultiplier = 1.5f, hits = 1, windUpTime = 1.9f, counterWindow = 0.60f, counter = CounterType.Interrupt, lungeAtTarget = false, weight = 0.8f },
                new EnemyAttackData { attackName = "Spark Rain", damageMultiplier = 0.5f, hits = 3, windUpTime = 0.8f, counterWindow = 0.35f, timeBetweenHits = 0.45f, lungeAtTarget = false, weight = 1f },
            };
        });
    }

    static EnemyData MakeGiantData()
    {
        return LoadOrCreate<EnemyData>(EnemyDir + "/Quinametzin_Giant.asset", d =>
        {
            d.displayName = "Quinametzin";
            d.archetype = EnemyArchetype.Giant;
            d.maxHealth = 150f; d.attackPower = 36f; d.soulsOnDefeat = 14f;
            d.bonusMaxHealth = 8f; d.bonusMaxStamina = 3f;
            d.unlocksFury = true;
            d.grantsFuryAtCriticalHealth = true;
            d.bodyRadius = 1.6f; d.popupHeight = 3.6f;
            d.spriteSheet = "giant"; d.frameWidth = 50; d.frameHeight = 58; d.feetOffset = 1.2f;
            d.gorePieceCols = 2; d.gorePieceRows = 3; d.goreAmount = 1.8f;
            d.patrolRadius = 5f; d.patrolSpeed = 1.1f; d.chaseSpeed = 3.0f;
            d.detectionRadius = 8f; d.fieldOfView = 150f; d.hearingRadius = 3f; d.loseSightRadius = 12f;
            d.engageDistance = 3.4f;
            d.attacks = new[]
            {
                new EnemyAttackData { attackName = "Hammer Smash", damageMultiplier = 1.2f, hits = 1, windUpTime = 1.3f, counterWindow = 0.40f, weight = 1f },
                new EnemyAttackData { attackName = "Double Strike", damageMultiplier = 0.7f, hits = 2, windUpTime = 1.0f, counterWindow = 0.35f, timeBetweenHits = 0.7f, weight = 1f },
                new EnemyAttackData { attackName = "Sweep", damageMultiplier = 0.9f, hits = 1, windUpTime = 1.1f, counterWindow = 0.35f, weight = 0.7f },
            };
        });
    }

    static EnemyData MakeBossData()
    {
        return LoadOrCreate<EnemyData>(EnemyDir + "/Quetzalcoatl_Boss.asset", d =>
        {
            d.displayName = "Quetzalcoatl";
            d.archetype = EnemyArchetype.Boss;
            d.isBoss = true;
            d.maxHealth = 480f; d.attackPower = 34f; d.soulsOnDefeat = 40f;
            d.bodyRadius = 3.4f; d.popupHeight = 5.4f;
            d.spriteSheet = "quetzal"; d.frameWidth = 98; d.frameHeight = 82; d.feetOffset = 1.8f;
            d.gorePieceCols = 3; d.gorePieceRows = 2; d.goreAmount = 3f;
            d.patrolRadius = 0f; d.patrolSpeed = 0f; d.chaseSpeed = 2.0f;
            d.detectionRadius = 9f; d.fieldOfView = 360f; d.hearingRadius = 9f; d.loseSightRadius = 25f;
            d.loseTargetAfterSeconds = 99f;
            d.engageDistance = 5.5f;
            d.phase2HealthFraction = 0.5f; d.phase2AttacksPerTurn = 2;
            d.attacks = new[]
            {
                new EnemyAttackData { attackName = "Wind Cutter", damageMultiplier = 0.8f, hits = 2, windUpTime = 1.1f, counterWindow = 0.40f, timeBetweenHits = 0.7f, lungeAtTarget = false, weight = 1f },
                new EnemyAttackData { attackName = "Feather Storm", damageMultiplier = 0.45f, hits = 4, windUpTime = 0.9f, counterWindow = 0.30f, timeBetweenHits = 0.4f, lungeAtTarget = false, weight = 1f },
                new EnemyAttackData { attackName = "Tail Sweep", damageMultiplier = 1.3f, hits = 1, windUpTime = 1.2f, counterWindow = 0.40f, weight = 1f },
                new EnemyAttackData { attackName = "Solar Chant", damageMultiplier = 1.8f, hits = 1, windUpTime = 2.0f, counterWindow = 0.60f, counter = CounterType.Interrupt, lungeAtTarget = false, weight = 0.7f },
            };
            d.phase2Attacks = new[]
            {
                new EnemyAttackData { attackName = "Serpent Dive", damageMultiplier = 1.0f, hits = 2, windUpTime = 0.9f, counterWindow = 0.35f, timeBetweenHits = 0.6f, weight = 1f },
                new EnemyAttackData { attackName = "Quetzal Fury", damageMultiplier = 0.5f, hits = 5, windUpTime = 0.8f, counterWindow = 0.28f, timeBetweenHits = 0.35f, lungeAtTarget = false, weight = 1f },
                new EnemyAttackData { attackName = "Wind Cutter", damageMultiplier = 0.8f, hits = 2, windUpTime = 0.9f, counterWindow = 0.35f, timeBetweenHits = 0.6f, lungeAtTarget = false, weight = 1f },
                new EnemyAttackData { attackName = "Solar Chant", damageMultiplier = 1.8f, hits = 1, windUpTime = 1.8f, counterWindow = 0.55f, counter = CounterType.Interrupt, lungeAtTarget = false, weight = 0.7f },
            };
        });
    }

    // ------------------------------------------------------------------
    //  Utilities
    // ------------------------------------------------------------------
    static Texture2D PrepareLogo()
    {
        if (!File.Exists(LogoPath)) return null;
        AssetDatabase.ImportAsset(LogoPath, ImportAssetOptions.ForceSynchronousImport);
        var imp = AssetImporter.GetAtPath(LogoPath) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Default;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.mipmapEnabled = false;
            imp.maxTextureSize = 1024;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(LogoPath);
    }

    static T LoadOrCreate<T>(string path, System.Action<T> init) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        init(asset);
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}

/// <summary>
/// Runs the setup automatically the first time the project is opened (when Boot.unity does not exist yet),
/// so there is nothing to click: open the project, wait for it to finish, press Play.
/// </summary>
[InitializeOnLoad]
static class MictlanAutoSetup
{
    static MictlanAutoSetup()
    {
        if (File.Exists(MictlanSetupTool.BootScenePath) && File.Exists(MictlanSetupTool.FinalPrefabPath)) return;
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EditorApplication.update -= Tick;
        if (File.Exists(MictlanSetupTool.BootScenePath) && File.Exists(MictlanSetupTool.FinalPrefabPath)) return;

        MictlanSetupTool.CreateAll(true);
    }
}
#endif
