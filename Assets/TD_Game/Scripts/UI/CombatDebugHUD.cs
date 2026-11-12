using System.Collections.Generic;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// TEMPORARY combat display drawn with OnGUI so the combat can be tested before the Canvas
    /// combat HUD (calendar, 12 nov). It only listens to CombatFeed and reads CombatManager.
    /// Delete it, or disable it, when the real HUD exists.
    /// </summary>
    public class CombatDebugHUD : MonoBehaviour
    {
        [SerializeField] CombatManager combat;
        [SerializeField] PlayerCombatant player;
        [SerializeField, Min(0.5f)] float popupLifetime = 1f;

        struct Popup { public Vector3 position; public string text; public Color color; public float time; }

        readonly List<Popup> popups = new List<Popup>();
        string banner, turnLabel, counterPrompt;
        float bannerUntil;
        GUIStyle box, big, small;

        void OnEnable()
        {
            CombatFeed.Banner += OnBanner;
            CombatFeed.TurnLabel += OnTurnLabel;
            CombatFeed.CounterPrompt += OnPrompt;
            CombatFeed.Popup += OnPopup;
        }

        void OnDisable()
        {
            CombatFeed.Banner -= OnBanner;
            CombatFeed.TurnLabel -= OnTurnLabel;
            CombatFeed.CounterPrompt -= OnPrompt;
            CombatFeed.Popup -= OnPopup;
        }

        void OnBanner(string text, float seconds) { banner = text; bannerUntil = Time.time + seconds; }
        void OnTurnLabel(string text) => turnLabel = text;
        void OnPrompt(string text) => counterPrompt = text;
        void OnPopup(Vector3 p, string text, Color c) =>
            popups.Add(new Popup { position = p, text = text, color = c, time = Time.time });

        void OnGUI()
        {
            if (player == null) return;
            CreateStyles();
            float scale = Screen.height / 540f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;

            DrawPlayer();
            var context = combat != null ? combat.Context : null;
            if (context != null) DrawCombat(context, w);
            if (banner != null && Time.time < bannerUntil) GUI.Label(new Rect(0, 150, w, 80), banner, big);
            if (!string.IsNullOrEmpty(counterPrompt)) GUI.Label(new Rect(0, 230, w, 60), counterPrompt, big);

            GUI.matrix = Matrix4x4.identity;
            DrawPopups();
        }

        void DrawPlayer()
        {
            var h = player.Health;
            string buff = player.BuffTurnsLeft > 0 ? $"   FURY x{player.BuffTurnsLeft}" : "";
            string weak = player.IsWeakened ? "   WEAKENED" : "";
            GUI.Label(new Rect(8, 8, 330, 70),
                $"HP {h.Current:0}/{h.Max:0}   Stamina {player.Stamina:0}/{player.MaxStamina:0}\n" +
                $"Potions {player.Potions}   Macuahuitl {player.ChargeNormalized * 100f:0}%{buff}{weak}\n" +
                $"Victories {player.Progression.Victories}", box);
        }

        void DrawCombat(CombatContext context, float w)
        {
            var lines = new System.Text.StringBuilder();
            for (int i = 0; i < context.Enemies.Count; i++)
            {
                var e = context.Enemies[i];
                if (e == null || e.Health.IsDead) continue;
                string mark = i == context.TargetIndex ? "> " : "   ";
                lines.AppendLine($"{mark}{e.Data.displayName}  {e.Health.Current:0}/{e.Health.Max:0}");
            }
            GUI.Label(new Rect(w - 248, 8, 240, 80), lines.ToString(), box);
            if (!string.IsNullOrEmpty(turnLabel)) GUI.Label(new Rect(0, 8, w, 30), turnLabel, big);

            var turn = combat.PlayerTurn;
            GUI.Label(new Rect(8, 470, w - 16, 62),
                $"[1/A] {Name(turn.Attack, context)}   [2/X] {Name(turn.Skill1, context)}   [3/Y] {Name(turn.Skill2, context)}   [4/D-pad up] {Name(turn.Potion, context)}\n" +
                "[Q/RB] End turn   [Arrows] Target   Enemy turn: [Space, D/B] Dodge   [1/A] Interrupt", small);
        }

        string Name(CombatAbility ability, CombatContext context)
        {
            if (ability == null) return "-";
            if (!player.Progression.IsUnlocked(ability)) return "(locked)";
            return $"{ability.displayName} ({ability.staminaCost:0})";
        }

        void DrawPopups()
        {
            var cam = Camera.main;
            if (cam == null) return;
            popups.RemoveAll(p => Time.time - p.time > popupLifetime);
            foreach (var p in popups)
            {
                float age = (Time.time - p.time) / popupLifetime;
                Vector3 s = cam.WorldToScreenPoint(p.position + Vector3.up * age * 0.6f);
                var style = new GUIStyle(big) { normal = { textColor = new Color(p.color.r, p.color.g, p.color.b, 1f - age) } };
                GUI.Label(new Rect(s.x - 100, Screen.height - s.y - 20, 200, 40), p.text, style);
            }
        }

        void CreateStyles()
        {
            if (box != null) return;
            box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 13, wordWrap = true };
            big = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 24, fontStyle = FontStyle.Bold, wordWrap = true };
            big.normal.textColor = Color.white;
            small = new GUIStyle(box) { fontSize = 12 };
        }
    }
}
