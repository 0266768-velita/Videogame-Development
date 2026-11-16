using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>Names of the scenes of the new game (they must be in File > Build Profiles > Scene List).</summary>
    public static class SceneIds
    {
        // "TD_" so they never get confused with the Boot / MainMenu scenes of the previous game.
        public const string Boot = "TD_Boot";
        public const string MainMenu = "TD_MainMenu";
        public const string MainMap = "MainMap";
    }

    /// <summary>
    /// Changes scene with a fade to black (adapted from Mictlán v5's SceneLoader). Usage: SceneFader.Load(SceneIds.MainMap);
    /// It creates itself the first time and survives scene changes.
    /// </summary>
    public class SceneFader : MonoBehaviour
    {
        const float FadeSeconds = 0.4f;

        static SceneFader instance;
        CanvasGroup fade;
        bool busy;

        public static void Load(string sceneName)
        {
            if (instance == null) Create();
            if (!instance.busy) instance.StartCoroutine(instance.Go(sceneName));
        }

        static void Create()
        {
            var go = new GameObject("SceneFader");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SceneFader>();

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            instance.fade = go.AddComponent<CanvasGroup>();
            instance.fade.alpha = 0f;
            instance.fade.blocksRaycasts = false;

            var black = new GameObject("Black", typeof(RectTransform)).AddComponent<Image>();
            black.transform.SetParent(go.transform, false);
            black.color = Color.black;
            var r = black.rectTransform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        IEnumerator Go(string sceneName)
        {
            busy = true;
            fade.blocksRaycasts = true;
            yield return Fade(1f);

            Time.timeScale = 1f;
            GameState.SetMode(GameMode.Exploration);
            var op = SceneManager.LoadSceneAsync(sceneName);
            if (op == null) Debug.LogError($"[{nameof(SceneFader)}] Cannot load '{sceneName}'. Is it in the Build Profiles scene list?");
            else while (!op.isDone) yield return null;
            yield return null;

            yield return Fade(0f);
            fade.blocksRaycasts = false;
            busy = false;
        }

        IEnumerator Fade(float target)
        {
            float start = fade.alpha;
            for (float t = 0f; t < FadeSeconds; t += Time.unscaledDeltaTime)
            {
                fade.alpha = Mathf.Lerp(start, target, t / FadeSeconds);
                yield return null;
            }
            fade.alpha = target;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;
    }
}
