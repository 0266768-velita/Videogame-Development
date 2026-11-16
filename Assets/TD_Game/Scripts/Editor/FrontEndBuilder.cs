using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// Brings the front end of Mictlán v5 to TD_Game: audio library (same music and effects), boot screens
    /// (logo, rating, description), main menu (same background and music), pause menu, legacy character art
    /// and the Build Profiles scene list. Menu: TD_Game > Build Front End (Menus, Audio, Scenes).
    /// Nothing that already exists is overwritten (delete a scene or asset to rebuild it).
    /// </summary>
    public static class FrontEndBuilder
    {
        const string AudioFolder = "Assets/TD_Game/Audio";
        const string LibraryPath = AudioFolder + "/AudioLibrary.asset";
        const string SceneFolder = "Assets/TD_Game/Scenes";
        public const string BootPath = SceneFolder + "/MainMenu/TD_Boot.unity";
        public const string MenuPath = SceneFolder + "/MainMenu/TD_MainMenu.unity";
        const string TestLevelPath = SceneFolder + "/Testing/TestLevel.unity";
        const string PrefabFolder = "Assets/TD_Game/Prefabs/Characters";

        [MenuItem("TD_Game/Build Front End (Menus, Audio, Scenes)")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildAll();
        }

        /// <summary>Also used from batch mode (-executeMethod TD_Game.EditorTools.FrontEndBuilder.BuildAll).</summary>
        public static void BuildAll()
        {
            LegacyArtImporter.ImportAll();
            EnemyContentBuilder.Build();
            ApplyLegacyCharacterArt();
            var library = EnsureAudioLibrary();
            BuildBootScene(library);
            BuildMenuScene(library);
            MainMapBuilder.Build(library);
            AddLevelServices(TestLevelPath, library);
            SetBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[TD_Game] Front end ready: TD_Boot -> TD_MainMenu -> MainMap.");
        }

        // ---------------- Audio ----------------

        public static AudioLibrary EnsureAudioLibrary()
        {
            var library = AssetDatabase.LoadAssetAtPath<AudioLibrary>(LibraryPath);
            if (library != null) return library;

            library = ScriptableObject.CreateInstance<AudioLibrary>();
            library.menu = Clip("Music/music_menu");
            library.explore = Clip("Music/music_explore");
            library.battle = Clip("Music/music_battle");
            library.boss = Clip("Music/music_boss");
            library.gameOver = Clip("Music/music_gameover");
            library.sfx = AssetDatabase.FindAssets("t:AudioClip", new[] { AudioFolder + "/SFX" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(p => new AudioLibrary.NamedClip
                {
                    name = System.IO.Path.GetFileNameWithoutExtension(p),
                    clip = AssetDatabase.LoadAssetAtPath<AudioClip>(p),
                    volume = 1f
                })
                .OrderBy(c => c.name).ToArray();
            AssetDatabase.CreateAsset(library, LibraryPath);
            return library;
        }

        static AudioClip Clip(string relative) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/{relative}.wav");

        /// <summary>Adds music, game sounds and the pause menu to a level scene (if they are missing) and saves it.</summary>
        public static void AddLevelServices(string scenePath, AudioLibrary library)
        {
            var scene = EditorSceneManager.OpenScene(scenePath);
            AddLevelServicesToOpenScene(library, MusicTrack.Explore);
            EditorSceneManager.SaveScene(scene);
        }

        public static void AddLevelServicesToOpenScene(AudioLibrary library, MusicTrack music)
        {
            if (Object.FindAnyObjectByType<GameAudio>() == null)
            {
                var go = new GameObject("GameAudio");
                SetAudio(go.AddComponent<GameAudio>(), library, music);
                go.AddComponent<GameplayAudio>();
            }
            if (Object.FindAnyObjectByType<PauseScreen>() == null) new GameObject("PauseScreen").AddComponent<PauseScreen>();
        }

        static void SetAudio(GameAudio audio, AudioLibrary library, MusicTrack music)
        {
            var so = new SerializedObject(audio);
            so.FindProperty("library").objectReferenceValue = library;
            so.FindProperty("sceneMusic").enumValueIndex = (int)music;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------- Boot and menu scenes ----------------

        static void BuildBootScene(AudioLibrary library)
        {
            if (System.IO.File.Exists(BootPath)) return;
            var scene = NewMenuScene();
            var boot = new GameObject("BootScreens").AddComponent<BootScreens>();
            var so = new SerializedObject(boot);
            so.FindProperty("studioLogo").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Texture2D>(LegacyArtImporter.MenuFolder + "/MictaForge_Logo.png");
            so.ApplyModifiedPropertiesWithoutUndo();
            SetAudio(new GameObject("GameAudio").AddComponent<GameAudio>(), library, MusicTrack.None);
            Save(scene, BootPath);
        }

        static void BuildMenuScene(AudioLibrary library)
        {
            if (System.IO.File.Exists(MenuPath)) return;
            var scene = NewMenuScene();
            var menu = new GameObject("TitleMenu").AddComponent<TitleMenu>();
            var so = new SerializedObject(menu);
            so.FindProperty("background").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Texture2D>(LegacyArtImporter.MenuFolder + "/MenuBackground.png");
            so.ApplyModifiedPropertiesWithoutUndo();
            SetAudio(new GameObject("GameAudio").AddComponent<GameAudio>(), library, MusicTrack.Menu);
            Save(scene, MenuPath);
        }

        static Scene NewMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            cameraGo.AddComponent<AudioListener>();
            return scene;
        }

        static void Save(Scene scene, string path)
        {
            EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            EditorSceneManager.SaveScene(scene, path);
        }

        static void SetBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootPath, true),
                new EditorBuildSettingsScene(MenuPath, true),
                new EditorBuildSettingsScene(MainMapBuilder.MainMapPath, true),
                new EditorBuildSettingsScene(TestLevelPath, true),
            };
        }

        // ---------------- Characters: the pixel art of Mictlán v5 ----------------

        static void ApplyLegacyCharacterArt()
        {
            //                   prefab               sheet        collider size              animated
            SetCharacter("Enemy_Wolf", "dog", new Vector2(1.1f, 0.5f), true, 0.65f);
            SetCharacter("Enemy_Witch", "witch", new Vector2(0.8f, 0.5f), true, 0.5f);
            SetCharacter("Enemy_Giant", "giant", new Vector2(2f, 0.8f), true, 1.2f);
            SetCharacter("Boss_Quetzalcoatl", "quetzal", new Vector2(3.2f, 1.2f), true, 2f);
            SetCharacter("Player", "tlahuelli", Vector2.zero, false, 0f);
        }

        static void SetCharacter(string prefabName, string sheet, Vector2 colliderSize, bool animated, float bodyRadius)
        {
            string path = $"{PrefabFolder}/{prefabName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;
            var frames = LegacyArtImporter.Frames(sheet);
            if (frames.Length == 0) { Debug.LogWarning($"[TD_Game] No frames for {sheet}."); return; }

            var root = PrefabUtility.LoadPrefabContents(path);
            var visual = root.transform.Find("Visual");
            var sprite = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
            if (sprite != null)
            {
                sprite.sprite = frames[0];
                sprite.color = Color.white;
                visual.localScale = Vector3.one;
                if (animated)
                {
                    var book = visual.GetComponent<SpriteFlipbook>();
                    if (book == null) book = visual.gameObject.AddComponent<SpriteFlipbook>();
                    book.Setup(frames, 0.18f);
                }
            }
            if (colliderSize != Vector2.zero)
            {
                var box = root.GetComponent<BoxCollider2D>();
                if (box != null) box.size = colliderSize;
                var enemy = root.GetComponent<CombatEnemy>();
                if (enemy != null && enemy.Data != null && bodyRadius > 0f)
                {
                    enemy.Data.bodyRadius = bodyRadius;
                    EditorUtility.SetDirty(enemy.Data);
                }
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
