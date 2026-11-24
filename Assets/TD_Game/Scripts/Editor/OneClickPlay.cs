using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// The "Mictlan" menu at the top of Unity (like the previous version had):
    ///   Mictlan > Rebuild Everything And Play: prepares the whole game and starts it from the first screen.
    ///   Mictlan > Play From The Start: just starts the game (TD_Boot), without rebuilding.
    ///   Mictlan > Rebuild The Main Map: deletes MainMap and makes it again (asks first).
    /// </summary>
    public static class OneClickPlay
    {
        [MenuItem("Mictlan/Rebuild Everything And Play", priority = 0)]
        public static void RebuildAndPlay()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            FrontEndBuilder.BuildAll();
            PlayFromStart();
        }

        [MenuItem("Mictlan/Play From The Start", priority = 1)]
        public static void PlayFromStart()
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(FrontEndBuilder.BootPath);
            EditorUtility.audioMasterMute = false;   // the "Mute Audio" button of the Game view must be off to hear the game
            EditorApplication.isPlaying = true;
        }

        public const string BuildFolder = "Builds/Mictlan";

        /// <summary>
        /// Makes the Windows game (.exe) to share with friends, in the folder Builds/Mictlan of the project.
        /// Send the WHOLE folder (zipped): the .exe does not work without the files next to it.
        /// </summary>
        [MenuItem("Mictlan/Build Game For Friends (.exe)", priority = 10)]
        public static void BuildGame()
        {
            PlayerSettings.companyName = "Micta Forge";
            PlayerSettings.productName = "Mictlan";
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.runInBackground = true;
            var options = new BuildPlayerOptions
            {
                scenes = new[] { FrontEndBuilder.BootPath, FrontEndBuilder.MenuPath, FrontEndBuilder.ProloguePath, MainMapBuilder.MainMapPath, CsvMapBuilder.ScenePath },
                locationPathName = BuildFolder + "/Mictlan.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[TD_Game] Build {report.summary.result}: {report.summary.outputPath} ({report.summary.totalSize / (1024 * 1024)} MB)");
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded && !Application.isBatchMode)
                EditorUtility.RevealInFinder(report.summary.outputPath);
        }

        [MenuItem("Mictlan/Rebuild The Main Map (deletes it)", priority = 20)]
        public static void RebuildMainMap()
        {
            if (!EditorUtility.DisplayDialog("Rebuild MainMap",
                    "This deletes MainMap.unity and builds it again. Changes made by hand in that scene are lost.", "Rebuild", "Cancel"))
                return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            AssetDatabase.DeleteAsset(MainMapBuilder.MainMapPath);
            FrontEndBuilder.BuildAll();
        }
    }
}
