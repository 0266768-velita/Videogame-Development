using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Loads scenes with a fade to black. Usage: SceneLoader.Load(SceneNames.Game);</summary>
public class SceneLoader : MonoBehaviour
{
    static SceneLoader instance;
    CanvasGroup fade;
    bool busy;

    public static void Load(string sceneName)
    {
        var loader = Get();
        if (!loader.busy) loader.StartCoroutine(loader.Go(sceneName));
    }

    static SceneLoader Get()
    {
        if (instance == null)
        {
            var go = new GameObject("SceneLoader");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SceneLoader>();
            instance.BuildFade();
        }
        return instance;
    }

    void BuildFade()
    {
        var canvasGo = new GameObject("FadeCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        fade = canvasGo.AddComponent<CanvasGroup>();
        fade.alpha = 0f;
        fade.blocksRaycasts = false;
        fade.interactable = false;

        var img = new GameObject("Black", typeof(RectTransform)).AddComponent<Image>();
        img.transform.SetParent(canvasGo.transform, false);
        img.color = Color.black;
        img.raycastTarget = true;
        var r = img.rectTransform;
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
    }

    IEnumerator Go(string sceneName)
    {
        busy = true;
        fade.blocksRaycasts = true;
        yield return Fade(1f, 0.4f);

        Time.timeScale = 1f;
        var op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError($"Cannot load scene '{sceneName}'. Is it listed in File > Build Profiles > Scene List?");
        }
        else
        {
            while (!op.isDone) yield return null;
            yield return null;
        }

        yield return Fade(0f, 0.4f);
        fade.blocksRaycasts = false;
        busy = false;
    }

    IEnumerator Fade(float target, float seconds)
    {
        float start = fade.alpha;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            fade.alpha = Mathf.Lerp(start, target, t / seconds);
            yield return null;
        }
        fade.alpha = target;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { instance = null; }
}
