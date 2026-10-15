using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Quick black fade used for entering the pyramid, falling into water, etc.</summary>
public class ScreenFade : MonoBehaviour
{
    static ScreenFade instance;
    CanvasGroup group;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { instance = null; }

    static ScreenFade Get()
    {
        if (instance == null)
        {
            var go = new GameObject("ScreenFade");
            instance = go.AddComponent<ScreenFade>();
            var content = UIKit.CreateScreen("FadeCanvas", 100, go.transform);
            instance.group = content.gameObject.AddComponent<CanvasGroup>();
            instance.group.blocksRaycasts = false;
            instance.group.alpha = 0f;
            UIKit.Stretch(UIKit.NewImage("Black", content, Color.black).rectTransform);
        }
        return instance;
    }

    public static IEnumerator Out(float seconds) { yield return Get().Fade(0f, 1f, seconds); }
    public static IEnumerator In(float seconds) { yield return Get().Fade(1f, 0f, seconds); }

    IEnumerator Fade(float from, float to, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(from, to, t / seconds);
            yield return null;
        }
        group.alpha = to;
    }
}
