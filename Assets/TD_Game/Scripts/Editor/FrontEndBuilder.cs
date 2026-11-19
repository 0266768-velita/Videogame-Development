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
        public const string ProloguePath = SceneFolder + "/MainMenu/TD_Prologue.unity";
        const string ArtifactPath = "Assets/TD_Game/ScriptableObjects/Abilities/Ability_SmokingMirror.asset";
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
            ArtGenerator.GenerateAll();
            PropArt.GenerateAll();
            UpdateOrbPrefab();
            LegacyArtImporter.ImportAll();
            EnemyContentBuilder.Build();
            ApplyLegacyCharacterArt();
            SetEnemyGold();
            var library = EnsureAudioLibrary();
            BuildBootScene(library);
            BuildMenuScene(library);
            BuildPrologueScene(library);
            StyleScene(BootPath);
            StyleScene(MenuPath);
            StyleScene(ProloguePath);
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
            ApplyStyleToOpenScene();
        }

        // ---------------- Menu style: Pirata One + Aztec frame ----------------

        static void StyleScene(string path)
        {
            var scene = EditorSceneManager.OpenScene(path);
            ApplyStyleToOpenScene();
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>Gives the font and the button frame to every menu of the open scene.</summary>
        static void ApplyStyleToOpenScene()
        {
            ConnectAudio();
            var font = AssetDatabase.LoadAssetAtPath<Font>(ArtGenerator.FontPath);
            var frame = AssetDatabase.LoadAssetAtPath<Sprite>(ArtGenerator.FramePath);
            foreach (var menu in Object.FindObjectsByType<TitleMenu>())
            {
                SetStyle(menu, font, frame);
                var so = new SerializedObject(menu);
                so.FindProperty("glow").objectReferenceValue = ArtGenerator.Effect("glow");
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (var pause in Object.FindObjectsByType<PauseScreen>()) SetStyle(pause, font, frame);
            foreach (var boot in Object.FindObjectsByType<BootScreens>()) SetStyle(boot, font, null);
            foreach (var comic in Object.FindObjectsByType<PrologueComic>()) SetStyle(comic, font, null);
            foreach (var credits in Object.FindObjectsByType<CreditsScreen>())
            {
                SetStyle(credits, font, null);
                var so = new SerializedObject(credits);
                so.FindProperty("background").objectReferenceValue = PropArt.Picture("journey");
                so.FindProperty("greca").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/TD_Game/Art/UI/Greca_Almenas.png");
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void SetStyle(Object component, Font font, Sprite frame)
        {
            var so = new SerializedObject(component);
            so.FindProperty("menuFont").objectReferenceValue = font;
            var frameProperty = so.FindProperty("buttonFrame");
            if (frameProperty != null) frameProperty.objectReferenceValue = frame;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------- Gold of each enemy ----------------

        static void SetEnemyGold()
        {
            Gold("EnemyData_Wolf", 8);
            Gold("EnemyData_Witch", 15);
            Gold("EnemyData_Giant", 30);
            Gold("EnemyData_Quetzalcoatl", 0);
        }

        static void Gold(string asset, int gold)
        {
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>($"Assets/TD_Game/ScriptableObjects/Enemies/{asset}.asset");
            if (data == null) return;
            data.goldOnDefeat = gold;
            EditorUtility.SetDirty(data);
        }

        static void SetAudio(GameAudio audio, AudioLibrary library, MusicTrack music)
        {
            var so = new SerializedObject(audio);
            so.FindProperty("library").objectReferenceValue = library;
            so.FindProperty("sceneMusic").enumValueIndex = (int)music;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Every GameAudio of the open scene gets the AudioLibrary (without it the game is silent).
        /// The library is loaded again from disk so the reference saved in the scene is always the real asset.
        /// </summary>
        static void ConnectAudio()
        {
            AssetDatabase.SaveAssets();
            var library = AssetDatabase.LoadAssetAtPath<AudioLibrary>(LibraryPath);
            if (library == null) { Debug.LogError("[TD_Game] AudioLibrary not found at " + LibraryPath); return; }
            foreach (var audio in Object.FindObjectsByType<GameAudio>(FindObjectsInactive.Include))
            {
                var so = new SerializedObject(audio);
                so.FindProperty("library").objectReferenceValue = library;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(audio);
                Debug.Log($"[TD_Game] Audio connected in {audio.gameObject.scene.name}: {so.FindProperty("library").objectReferenceValue}");
            }
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

        // ---------------- Artifact sold by the merchant ----------------

        /// <summary>The Smoking Mirror of Tezcatlipoca (created once, then edited in the Inspector).</summary>
        public static CombatAbility EnsureArtifact()
        {
            var mirror = AssetDatabase.LoadAssetAtPath<MirrorAbility>(ArtifactPath);
            if (mirror != null) return mirror;
            mirror = ScriptableObject.CreateInstance<MirrorAbility>();
            mirror.displayName = "Smoking Mirror";
            mirror.description = "Tezcatlipoca's mirror strikes every enemy and returns half the damage as health. Once per battle.";
            mirror.staminaCost = 25f;
            mirror.unlockedByDefault = false;   // bought from the merchant
            mirror.oncePerBattle = true;
            AssetDatabase.CreateAsset(mirror, ArtifactPath);
            return mirror;
        }

        /// <summary>The save finds abilities by name: give the SaveController of the open scene every ability asset.</summary>
        public static void RefreshSaveAbilities()
        {
            var save = Object.FindAnyObjectByType<SaveController>();
            if (save == null) return;
            var so = new SerializedObject(save);
            var list = so.FindProperty("allAbilities");
            string[] guids = AssetDatabase.FindAssets("t:CombatAbility", new[] { "Assets/TD_Game" });
            list.arraySize = guids.Length;
            for (int i = 0; i < guids.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<CombatAbility>(AssetDatabase.GUIDToAssetPath(guids[i]));
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------- Prologue comic (GDD section 3) ----------------

        static void BuildPrologueScene(AudioLibrary library)
        {
            if (System.IO.File.Exists(ProloguePath)) return;
            var scene = NewMenuScene();
            var comic = new GameObject("PrologueComic").AddComponent<PrologueComic>();
            SetAudio(new GameObject("GameAudio").AddComponent<GameAudio>(), library, MusicTrack.Menu);

            var city = AssetDatabase.LoadAssetAtPath<Texture2D>(LegacyArtImporter.MenuFolder + "/MenuBackground.png");
            var hero = AssetDatabase.LoadAssetAtPath<Texture2D>(LegacyArtImporter.CharacterFolder + "/tlahuelli.png");
            var giant = AssetDatabase.LoadAssetAtPath<Texture2D>(LegacyArtImporter.CharacterFolder + "/giant.png");
            Rect front = new Rect(0f, 52f / 78f, 18f / 72f, 26f / 78f);   // Tlahuelli looking at us (first frame)
            Rect whole = new Rect(0, 0, 1, 1);
            Rect firstGiant = new Rect(0, 0, 0.25f, 1);
            Color night = new Color(0.45f, 0.5f, 0.8f), blood = new Color(0.8f, 0.35f, 0.3f);
            Color shadow = new Color(0.12f, 0.12f, 0.16f);

            var so = new SerializedObject(comic);
            so.FindProperty("menuFont").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>(ArtGenerator.FontPath);
            var pages = so.FindProperty("pages");
            Texture Art(string name) => PropArt.Picture(name);
            var content = new[]
            {
                new[] { P(city, whole, Color.white, "Teotihuacan, 1430.\nAt the height of the Mexica world..."),
                        P(Art("hut"), whole, Color.white, "His mother was dying.\nThey were all each other had."),
                        P(Art("face"), whole, Color.white, "Tlahuelli could only watch over her.") },
                new[] { P(Art("altar_rain"), whole, Color.white, "He danced for the gods,\nbegging them to hear him."),
                        P(Art("empty_thrones"), whole, Color.white, "They did not answer."),
                        P(Art("grave"), whole, Color.white, "That night, grief became something else.") },
                new[] { P(Art("fist"), whole, Color.white, "\"I will make them answer.\"") },
                new[] { P(Art("mictlantecuhtli"), whole, Color.white, "MICTLANTECUHTLI:\n\"The gods forgot you.\nThey forgot me too.\""),
                        P(Art("gift"), whole, Color.white, "\"Take this Macuahuitl. It grows hungry with every servant of the gods you strike down.\""),
                        P(Art("journey"), whole, Color.white, "And so his journey began.") },
            };
            pages.arraySize = content.Length;
            for (int i = 0; i < content.Length; i++)
            {
                var panels = pages.GetArrayElementAtIndex(i).FindPropertyRelative("panels");
                panels.arraySize = content[i].Length;
                for (int j = 0; j < content[i].Length; j++)
                {
                    var panel = panels.GetArrayElementAtIndex(j);
                    panel.FindPropertyRelative("picture").objectReferenceValue = content[i][j].picture;
                    panel.FindPropertyRelative("crop").rectValue = content[i][j].crop;
                    panel.FindPropertyRelative("tint").colorValue = content[i][j].tint;
                    panel.FindPropertyRelative("caption").stringValue = content[i][j].caption;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            Save(scene, ProloguePath);
        }

        static PrologueComic.ComicPanel P(Texture picture, Rect crop, Color tint, string caption) =>
            new PrologueComic.ComicPanel { picture = picture, crop = crop, tint = tint, caption = caption };

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
                new EditorBuildSettingsScene(ProloguePath, true),
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
            SetBossArt();
            SetCharacter("Player", "tlahuelli", Vector2.zero, false, 0f);
        }

        /// <summary>Orbs easy to see: a shiny spinning gold coin, bright gems and a halo above the darkness.</summary>
        static void UpdateOrbPrefab()
        {
            const string path = "Assets/TD_Game/Prefabs/Interactables/Orb.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;
            var root = PrefabUtility.LoadPrefabContents(path);
            var orb = root.GetComponent<SoulOrb>();
            var halo = root.transform.Find("Halo");
            if (halo == null)
            {
                halo = new GameObject("Halo").transform;
                halo.SetParent(root.transform, false);
                var sr = halo.gameObject.AddComponent<SpriteRenderer>();
                sr.sprite = ArtGenerator.Effect("glow");
                sr.sortingOrder = 501;
                halo.localScale = Vector3.one * 1.2f;
            }
            var so = new SerializedObject(orb);
            so.FindProperty("goldCoin").objectReferenceValue = PropArt.Sprite("orb_coin");
            so.FindProperty("jadeGem").objectReferenceValue = PropArt.Sprite("orb_jade");
            so.FindProperty("tonalliGem").objectReferenceValue = PropArt.Sprite("orb_tonalli");
            so.FindProperty("halo").objectReferenceValue = halo.GetComponent<SpriteRenderer>();
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        /// <summary>The new, more intimidating Quetzalcoatl (PropArt), with its 4 swaying frames.</summary>
        static void SetBossArt()
        {
            string path = $"{PrefabFolder}/Boss_Quetzalcoatl.prefab";
            var frames = PropArt.QuetzalcoatlFrames();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null || frames[0] == null) return;
            var root = PrefabUtility.LoadPrefabContents(path);
            var visual = root.transform.Find("Visual");
            visual.GetComponent<SpriteRenderer>().sprite = frames[0];
            var book = visual.GetComponent<SpriteFlipbook>();
            if (book == null) book = visual.gameObject.AddComponent<SpriteFlipbook>();
            book.Setup(frames, 0.22f);
            var box = root.GetComponent<BoxCollider2D>();
            if (box != null) box.size = new Vector2(3.8f, 1.4f);
            var enemy = root.GetComponent<CombatEnemy>();
            if (enemy != null && enemy.Data != null) { enemy.Data.bodyRadius = 2.4f; EditorUtility.SetDirty(enemy.Data); }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
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
            if (prefabName == "Player") MainMapBuilder.SetupPlayerVisual(root);   // animation, torch and Macuahuitl
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
