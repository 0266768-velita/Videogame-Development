#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds everything the game needs inside Unity: placeholder sprite, EnemyData assets, prefabs and the
/// three scenes (Boot, MainMenu, Game) wired together and added to Build Settings.
///
/// It runs AUTOMATICALLY the first time the project is opened (see MictlanAutoSetup below).
/// You can re-run it any time from the menu:  Mictlan > Rebuild Scenes.
/// Existing prefabs and EnemyData assets are kept (your tuning is never overwritten);
/// the three scenes are recreated from scratch, so back them up first if you edited them by hand.
/// </summary>
public static class MictlanSetupTool
{
    public const string BootScenePath = "Assets/Scenes/Boot.unity";

    const string LogoPath = "Assets/Art/Branding/MictaForge_Logo.png";
    const string ArtDir = "Assets/Art/Placeholders";
    const string PrefabDir = "Assets/Prefabs";
    const string EnemyPrefabDir = "Assets/Prefabs/Enemies";
    const string DataDir = "Assets/Data/Enemies";
    const string SceneDir = "Assets/Scenes";

    static readonly Rect MapBounds = new Rect(-32f, -20f, 64f, 40f);

    [MenuItem("Mictlan/Rebuild Scenes (Boot, MainMenu, Game)")]
    static void MenuRebuild()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        CreateAll(false);
    }

    public static void CreateAll(bool silent)
    {
        EnsureFolder("Assets/Art"); EnsureFolder(ArtDir);
        EnsureFolder("Assets/Data"); EnsureFolder(DataDir);
        EnsureFolder(PrefabDir); EnsureFolder(EnemyPrefabDir);
        EnsureFolder(SceneDir);

        Physics2D.gravity = Vector2.zero;

        Texture2D logo = PrepareLogo();
        Sprite sq = EnsurePlaceholderSprite();
        var giantData = MakeGiantData();
        var witchData = MakeWitchData();

        var giantPrefab = MakeEnemyPrefab("Enemy_Quinametzin", giantData, sq, new Vector2(3f, 3f), new Color(0.42f, 0.28f, 0.2f), 1.1f);
        var witchPrefab = MakeEnemyPrefab("Enemy_Witch", witchData, sq, new Vector2(1f, 1.2f), new Color(0.55f, 0.2f, 0.65f), 0.4f);
        var playerPrefab = MakePlayerPrefab(sq);

        CreateBootScene(logo);
        CreateMenuScene();
        CreateGameScene(sq, playerPrefab, giantPrefab, witchPrefab);

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

    static void CreateGameScene(Sprite sq, GameObject playerPrefab, GameObject giantPrefab, GameObject witchPrefab)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = NewCamera(new Color(0.05f, 0.08f, 0.05f));

        // Ground
        var ground = NewSpriteObject("Ground", sq, new Color(0.30f, 0.45f, 0.25f), new Vector2(MapBounds.width, MapBounds.height), Vector2.zero);
        ground.GetComponent<SpriteRenderer>().sortingOrder = -32000;

        // Walls around the map
        float t = 2f;
        MakeWall("Wall_N", sq, new Vector2(0, MapBounds.yMax + t * 0.5f), new Vector2(MapBounds.width + t * 2, t));
        MakeWall("Wall_S", sq, new Vector2(0, MapBounds.yMin - t * 0.5f), new Vector2(MapBounds.width + t * 2, t));
        MakeWall("Wall_W", sq, new Vector2(MapBounds.xMin - t * 0.5f, 0), new Vector2(t, MapBounds.height));
        MakeWall("Wall_E", sq, new Vector2(MapBounds.xMax + t * 0.5f, 0), new Vector2(t, MapBounds.height));

        // Placeholder trees (fixed seed)
        var rnd = new System.Random(7);
        for (int i = 0; i < 45; i++)
        {
            float x = Mathf.Lerp(MapBounds.xMin + 2, MapBounds.xMax - 2, (float)rnd.NextDouble());
            float y = Mathf.Lerp(MapBounds.yMin + 2, MapBounds.yMax - 2, (float)rnd.NextDouble());
            if (Vector2.Distance(new Vector2(x, y), new Vector2(0, -14)) < 6f) continue; // keep the start area clear
            MakeTree(sq, new Vector2(x, y));
        }

        // Player
        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.position = new Vector3(0, -14, 0);
        cam.transform.position = new Vector3(0, -14, -10);

        var follow = cam.gameObject.AddComponent<CameraFollow2D>();
        follow.target = player.transform;
        follow.bounds = MapBounds;
        follow.useBounds = true;

        // Enemies
        PlaceEnemy(giantPrefab, new Vector2(10, 6));
        PlaceEnemy(giantPrefab, new Vector2(-18, 10));
        PlaceEnemy(witchPrefab, new Vector2(-10, 0));
        PlaceEnemy(witchPrefab, new Vector2(18, -4));

        // Session start + pause menu
        var boot = new GameObject("GameBootstrap");
        boot.AddComponent<GameBootstrap>();
        boot.AddComponent<PauseMenu>();

        EditorSceneManager.SaveScene(scene, SceneDir + "/Game.unity");
    }

    static GameObject NewSpriteObject(string name, Sprite sprite, Color color, Vector2 size, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.position = new Vector3(pos.x, pos.y, 0);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        return go;
    }

    static void MakeWall(string name, Sprite sq, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = size;
        var visual = NewSpriteObject("Visual", sq, new Color(0.12f, 0.1f, 0.08f), size, Vector2.zero);
        visual.transform.SetParent(go.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.GetComponent<SpriteRenderer>().sortingOrder = -31000;
    }

    static void MakeTree(Sprite sq, Vector2 pos)
    {
        var root = new GameObject("Tree");
        root.transform.position = pos;
        var col = root.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.6f);
        col.offset = new Vector2(0f, -0.5f);
        var trunk = NewSpriteObject("Trunk", sq, new Color(0.35f, 0.22f, 0.12f), new Vector2(0.4f, 1.0f), Vector2.zero);
        trunk.transform.SetParent(root.transform, false);
        trunk.transform.localPosition = new Vector3(0, -0.2f, 0);
        var crown = NewSpriteObject("Crown", sq, new Color(0.10f, 0.32f, 0.12f), new Vector2(1.6f, 1.4f), Vector2.zero);
        crown.transform.SetParent(root.transform, false);
        crown.transform.localPosition = new Vector3(0, 0.7f, 0);
        var ys = root.AddComponent<YSort>();
        ys.isStatic = true;
        ys.yOffset = -0.5f;
    }

    static void PlaceEnemy(GameObject prefab, Vector2 pos)
    {
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        inst.transform.position = new Vector3(pos.x, pos.y, 0);
    }

    // ------------------------------------------------------------------
    //  Prefabs (existing ones are kept so your edits survive a rebuild)
    // ------------------------------------------------------------------
    static GameObject MakePlayerPrefab(Sprite sq)
    {
        string path = PrefabDir + "/Tlahuelli.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var root = new GameObject("Tlahuelli");
        root.tag = "Player";

        var model = new GameObject("Model");
        model.transform.SetParent(root.transform, false);
        model.transform.localScale = new Vector3(0.9f, 1.1f, 1f);
        var sr = model.AddComponent<SpriteRenderer>();
        sr.sprite = sq;
        sr.color = new Color(0.85f, 0.65f, 0.4f);

        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        var col = root.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;

        root.AddComponent<Health>();
        root.AddComponent<ActorFeedback>();
        root.AddComponent<PlayerCombatStats>();
        root.AddComponent<YSort>();
        root.AddComponent<Player2D>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static GameObject MakeEnemyPrefab(string name, EnemyData data, Sprite sq, Vector2 size, Color color, float colliderRadius)
    {
        string path = EnemyPrefabDir + "/" + name + ".prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var root = new GameObject(name);

        // Replace the "Model" child with your sprite/animation when you have it
        var model = new GameObject("Model");
        model.transform.SetParent(root.transform, false);
        model.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = model.AddComponent<SpriteRenderer>();
        sr.sprite = sq;
        sr.color = color;

        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        var col = root.AddComponent<CircleCollider2D>();
        col.radius = colliderRadius;

        root.AddComponent<Health>();
        root.AddComponent<ActorFeedback>();
        root.AddComponent<YSort>();
        var ec = root.AddComponent<EnemyController>();
        ec.data = data;

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ------------------------------------------------------------------
    //  Enemy data (created once; edit the assets freely afterwards)
    // ------------------------------------------------------------------
    static EnemyData MakeGiantData()
    {
        return LoadOrCreate<EnemyData>(DataDir + "/Quinametzin_Giant.asset", d =>
        {
            d.displayName = "Quinametzin";
            d.archetype = EnemyArchetype.Giant;
            d.maxHealth = 130f; d.attackPower = 32f; d.soulsOnDefeat = 12f;
            d.grantsFuryAtCriticalHealth = true;
            d.bodyRadius = 1.5f; d.popupHeight = 2.2f;
            d.patrolRadius = 5f; d.patrolSpeed = 1.1f; d.chaseSpeed = 3.0f;
            d.detectionRadius = 8f; d.fieldOfView = 150f; d.hearingRadius = 3f; d.loseSightRadius = 12f;
            d.engageDistance = 2.8f;
            d.attacks = new[]
            {
                new EnemyAttackData { attackName = "Hammer Smash", damageMultiplier = 1.2f, hits = 1, windUpTime = 1.3f, counterWindow = 0.40f, animatorTrigger = "Attack_Smash", weight = 1f },
                new EnemyAttackData { attackName = "Double Strike", damageMultiplier = 0.7f, hits = 2, windUpTime = 1.0f, counterWindow = 0.35f, timeBetweenHits = 0.7f, animatorTrigger = "Attack_Combo", weight = 1f },
                new EnemyAttackData { attackName = "Sweep", damageMultiplier = 0.9f, hits = 1, windUpTime = 1.1f, counterWindow = 0.35f, animatorTrigger = "Attack_Sweep", weight = 0.7f },
            };
        });
    }

    static EnemyData MakeWitchData()
    {
        return LoadOrCreate<EnemyData>(DataDir + "/Witch_Tlazolteotl.asset", d =>
        {
            d.displayName = "Witch";
            d.archetype = EnemyArchetype.Witch;
            d.maxHealth = 80f; d.attackPower = 22f; d.soulsOnDefeat = 8f;
            d.bodyRadius = 0.5f; d.popupHeight = 1.3f;
            d.patrolRadius = 4f; d.patrolSpeed = 1.3f; d.chaseSpeed = 2.8f;
            d.detectionRadius = 9f; d.fieldOfView = 130f; d.hearingRadius = 3f; d.loseSightRadius = 12f;
            d.engageDistance = 4f;
            d.attacks = new[]
            {
                new EnemyAttackData { attackName = "Arcane Bolt", damageMultiplier = 1.0f, hits = 1, windUpTime = 1.0f, counterWindow = 0.35f, animatorTrigger = "Cast_Bolt", lungeAtTarget = false, weight = 1f },
                new EnemyAttackData { attackName = "Hex", damageMultiplier = 1.5f, hits = 1, windUpTime = 1.9f, counterWindow = 0.60f, counter = CounterType.Interrupt, animatorTrigger = "Cast_Curse", lungeAtTarget = false, weight = 0.8f },
                new EnemyAttackData { attackName = "Spark Rain", damageMultiplier = 0.5f, hits = 3, windUpTime = 0.8f, counterWindow = 0.35f, timeBetweenHits = 0.45f, animatorTrigger = "Cast_Spark", lungeAtTarget = false, weight = 1f },
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

    static Sprite EnsurePlaceholderSprite()
    {
        string path = ArtDir + "/px_white.png";
        if (!File.Exists(path))
        {
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            var px = new Color[16 * 16];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }

        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 16f;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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
        if (File.Exists(MictlanSetupTool.BootScenePath)) return;
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EditorApplication.update -= Tick;
        if (File.Exists(MictlanSetupTool.BootScenePath)) return;

        MictlanSetupTool.CreateAll(true);
    }
}
#endif
