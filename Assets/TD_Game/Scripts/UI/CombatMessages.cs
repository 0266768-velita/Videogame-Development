using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// Text feedback of the combat: banner, turn label, the DODGE!/NOW! prompt, combo counter and
    /// floating damage numbers. Also shows a pop-up when an orb is picked up during exploration.
    /// </summary>
    public class CombatMessages : MonoBehaviour
    {
        [SerializeField] PlayerCombatant player;
        [SerializeField] Text banner;
        [SerializeField] Text turnLabel;
        [SerializeField] Text prompt;
        [SerializeField] Text combo;
        [Tooltip("Inactive Text used as the template of the floating numbers.")]
        [SerializeField] Text popupTemplate;
        [SerializeField, Min(0.2f)] float popupLifetime = 0.9f;

        class Popup { public Text text; public Vector3 world; public float born; }

        readonly List<Popup> popups = new List<Popup>();
        readonly Stack<Text> pool = new Stack<Text>();
        float bannerUntil, bannerShown;
        Camera cam;

        void OnEnable()
        {
            CombatFeed.Banner += OnBanner;
            CombatFeed.TurnLabel += OnTurnLabel;
            CombatFeed.CounterPrompt += OnPrompt;
            CombatFeed.Popup += OnPopup;
            OrbSpawner.Collected += OnOrb;
        }

        void OnDisable()
        {
            CombatFeed.Banner -= OnBanner;
            CombatFeed.TurnLabel -= OnTurnLabel;
            CombatFeed.CounterPrompt -= OnPrompt;
            CombatFeed.Popup -= OnPopup;
            OrbSpawner.Collected -= OnOrb;
        }

        void Start()
        {
            if (popupTemplate != null) popupTemplate.gameObject.SetActive(false);
            banner.text = ""; turnLabel.text = ""; prompt.text = ""; combo.text = "";
        }

        void OnBanner(string text, float seconds)
        {
            banner.text = text;
            bannerShown = Time.unscaledTime;
            bannerUntil = Time.unscaledTime + seconds;
        }

        void OnTurnLabel(string text)
        {
            turnLabel.text = text ?? "";
            ShowPlate(turnLabel, turnLabel.text.Length > 0);
        }

        // The plate is toggled right away (not in Update) so it never shows up empty for a frame.
        void OnPrompt(string text)
        {
            prompt.text = text ?? "";
            ShowPlate(prompt, prompt.text.Length > 0);
        }

        void OnPopup(Vector3 world, string text, Color color)
        {
            var t = pool.Count > 0 ? pool.Pop() : Instantiate(popupTemplate, popupTemplate.transform.parent);
            t.gameObject.SetActive(true);
            t.text = text;
            t.color = color;
            popups.Add(new Popup { text = t, world = world, born = Time.unscaledTime });
        }

        void OnOrb(SoulOrb orb, PlayerCombatant collector)
        {
            string text = orb.Type switch
            {
                OrbType.Soul => $"+{orb.Value:0} soul",
                OrbType.Jade => $"+{orb.Value:0} HP",
                _ => $"+{orb.Value:0} tonalli"
            };
            Color color = orb.Type switch
            {
                OrbType.Soul => MexicaPalette.AgedGold,
                OrbType.Jade => MexicaPalette.Jade,
                _ => MexicaPalette.Turquoise
            };
            OnPopup(collector.transform.position + Vector3.up * 0.9f, text, color);
        }

        // Banner, turn label and prompt each sit on a dark "plate" (their parent) that is hidden when empty,
        // so the text is always on an opaque background and readable over any part of the map.
        static Transform PlateOf(Text text) => text.transform.parent;

        static void ShowPlate(Text text, bool visible)
        {
            var plate = PlateOf(text);
            if (plate.gameObject.activeSelf != visible) plate.gameObject.SetActive(visible);
        }

        void Update()
        {
            float now = Time.unscaledTime;

            // Banner: pops in bigger, then fades at the end.
            float left = bannerUntil - now;
            ShowPlate(banner, left > 0f && banner.text.Length > 0);
            if (left > 0f)
            {
                float age = now - bannerShown;
                var plate = PlateOf(banner);
                plate.localScale = Vector3.one * Mathf.Lerp(1.2f, 1f, Mathf.Clamp01(age / 0.15f));
                var group = plate.GetComponent<CanvasGroup>();
                if (group != null) group.alpha = Mathf.Clamp01(left / 0.3f);
            }

            ShowPlate(turnLabel, turnLabel.text.Length > 0);

            // Prompt pulses so it cannot be missed.
            ShowPlate(prompt, prompt.text.Length > 0);
            if (prompt.text.Length > 0)
                PlateOf(prompt).localScale = Vector3.one * (1f + 0.08f * Mathf.Abs(Mathf.Sin(now * 14f)));

            int hits = player != null ? player.ComboCount : 0;
            combo.text = GameState.Mode == GameMode.Combat && hits >= 2 ? $"COMBO  x{hits}" : "";

            UpdatePopups(now);
        }

        void UpdatePopups(float now)
        {
            if (cam == null) cam = Camera.main;
            var area = (RectTransform)popupTemplate.transform.parent;
            for (int i = popups.Count - 1; i >= 0; i--)
            {
                var p = popups[i];
                float k = (now - p.born) / popupLifetime;
                if (k >= 1f || cam == null)
                {
                    p.text.gameObject.SetActive(false);
                    pool.Push(p.text);
                    popups.RemoveAt(i);
                    continue;
                }
                Vector2 screen = cam.WorldToScreenPoint(p.world + Vector3.up * (0.7f * k));
                RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, null, out var local);
                p.text.rectTransform.anchoredPosition = local;
                p.text.transform.localScale = Vector3.one * (k < 0.1f ? Mathf.Lerp(1.6f, 1f, k / 0.1f) : 1f);
                var c = p.text.color; c.a = 1f - Mathf.Clamp01((k - 0.6f) / 0.4f); p.text.color = c;
            }
        }
    }
}
