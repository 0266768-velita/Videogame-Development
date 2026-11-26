using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// A black curtain over the whole screen inside the level (falls, doors, the release of the prisoner).
    /// Scene changes use SceneFader; this one only darkens and clears the screen. Created on first use.
    /// Usage: yield return ScreenFade.To(1f, 0.3f);  ...teleport...  yield return ScreenFade.To(0f, 0.3f);
    /// </summary>
    public static class ScreenFade
    {
        static Image curtain;

        static Image Curtain
        {
            get
            {
                if (curtain != null) return curtain;
                var go = new GameObject("ScreenFade", typeof(RectTransform), typeof(Canvas));
                var canvas = go.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 95;   // above the HUD, below the scene fader
                curtain = MenuWidgets.Image("Curtain", go.transform, new Color(0f, 0f, 0f, 0f));
                MenuWidgets.Stretch(curtain.rectTransform);
                return curtain;
            }
        }

        public static IEnumerator To(float alpha, float seconds, Color? color = null)
        {
            var image = Curtain;
            var c = color ?? Color.black;
            float start = image.color.a;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                c.a = Mathf.Lerp(start, alpha, t / seconds);
                image.color = c;
                yield return null;
            }
            c.a = alpha;
            image.color = c;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => curtain = null;
    }
}
