using UnityEditor;
using UnityEditor.SceneManagement;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// Starts the automatic Play Mode test of v0.9 (V09SelfTest) on MainMap_CSV. The report and screenshots go to
    /// Temp/SelfTest. RunAndQuit (for -executeMethod without -batchmode) closes Unity when the test ends.
    /// The test uses its own save file state: it deletes nothing but the save of the current slot is overwritten.
    /// </summary>
    public static class V09SelfTestMenu
    {
        [MenuItem("TD_Game/v0.9/Run Self Test (Play Mode)")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) return;
            EditorSceneManager.OpenScene(CsvMapBuilder.ScenePath);
            EditorPrefs.SetBool(V09SelfTest.PrefKey, true);
            EditorApplication.isPlaying = true;
        }

        /// <summary>Plays the boot scene and takes pictures of the rating screen and the main menu, then quits.</summary>
        public static void BootShotsAndQuit()
        {
            EditorSceneManager.OpenScene("Assets/TD_Game/Scenes/MainMenu/TD_Boot.unity");
            EditorPrefs.SetBool(V09BootShots.PrefKey, true);
            EditorApplication.isPlaying = true;
        }

        public static void RunAndQuit()
        {
            EditorPrefs.SetBool(V09SelfTest.QuitKey, true);
            Run();
        }
    }
}
