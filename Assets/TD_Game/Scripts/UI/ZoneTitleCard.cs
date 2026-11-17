using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// Big zone name that appears and fades away when you enter a new territory (like the area names in GTA).
    /// ZoneArea calls Show(); the same zone is not shown twice in a row.
    /// </summary>
    public class ZoneTitleCard : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] Text title;
        [SerializeField] Text subtitle;
        [SerializeField] float fadeSeconds = 0.8f;
        [SerializeField] float holdSeconds = 2.5f;

        static ZoneTitleCard instance;
        string current;
        Coroutine routine;

        void Awake()
        {
            instance = this;
            if (group != null) group.alpha = 0f;
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        public static void Show(string zoneName, string zoneSubtitle)
        {
            if (instance == null || instance.current == zoneName) return;
            instance.current = zoneName;
            if (instance.routine != null) instance.StopCoroutine(instance.routine);
            instance.routine = instance.StartCoroutine(instance.Play(zoneName, zoneSubtitle));
        }

        IEnumerator Play(string zoneName, string zoneSubtitle)
        {
            title.text = zoneName;
            subtitle.text = zoneSubtitle;
            GameAudio.PlaySfx("tip", 0.8f, 0f);
            yield return Fade(1f);
            yield return new WaitForSeconds(holdSeconds);
            yield return Fade(0f);
            routine = null;
        }

        IEnumerator Fade(float target)
        {
            float start = group.alpha;
            for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(start, target, t / fadeSeconds);
                yield return null;
            }
            group.alpha = target;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;
    }
}
