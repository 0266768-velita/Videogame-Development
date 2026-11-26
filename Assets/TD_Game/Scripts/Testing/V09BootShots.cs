#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Editor-only check of the first screens: plays TD_Boot, takes a picture of the age rating slide and of the main
    /// menu (with the version number), then closes Unity if asked. Started by TD_Game > v0.9 > Boot Screenshots.
    /// </summary>
    public class V09BootShots : MonoBehaviour
    {
        public const string PrefKey = "td_v09_bootshots";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!UnityEditor.EditorPrefs.GetBool(PrefKey, false)) return;
            UnityEditor.EditorPrefs.SetBool(PrefKey, false);
            var go = new GameObject("V09BootShots");
            DontDestroyOnLoad(go);
            go.AddComponent<V09BootShots>();
        }

        IEnumerator Start()
        {
            Application.runInBackground = true;
            string folder = Path.Combine(Directory.GetCurrentDirectory(), "Logs/SelfTest");
            Directory.CreateDirectory(folder);
            yield return new WaitForSecondsRealtime(5.2f);   // logo (3 s + fades), then the rating slide
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, "boot_rating.png"));
            yield return new WaitForSecondsRealtime(14f);    // description, then the main menu
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, "main_menu.png"));
            yield return new WaitForSecondsRealtime(1f);
            UnityEditor.EditorApplication.Exit(0);
        }
    }
}
#endif
