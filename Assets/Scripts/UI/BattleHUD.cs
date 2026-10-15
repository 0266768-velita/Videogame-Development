using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Battle HUD built in code (no Canvas to set up by hand).
/// GDD sections 10 and 14: health and stamina bars at the bottom right. It also shows the Macuahuitl
/// charge, the action menu, enemy health, the dodge prompt and floating damage numbers.
/// Uses the red of the Micta Forge logo as its accent color.
/// When final art exists, replace this with your own UI; BattleManager only calls the public methods.
/// </summary>
public class BattleHUD : MonoBehaviour
{
    static readonly Color StaminaGold = new Color(0.95f, 0.72f, 0.15f);
    static readonly Color ChargePurple = new Color(0.62f, 0.25f, 0.85f);

    class EnemyBar { public GameObject root; public RectTransform fill; public Text name; public EnemyController enemy; }
    class MenuEntry { public PlayerAction action; public Button button; public Text label; }
    class PopupText { public Text text; public Vector3 world; public float born; public Color color; }

    RectTransform content;
    RectTransform enemyContainer;
    RectTransform hpFill, stFill, chFill;
    Text hpText, stText, chText, furyText;
    Text banner, prompt, turnLabel;
    float bannerUntil;
    bool built;
    BattleManager bm;

    readonly List<EnemyBar> enemyBars = new List<EnemyBar>();
    readonly List<MenuEntry> menu = new List<MenuEntry>();
    readonly List<PopupText> popups = new List<PopupText>();

    void Awake() { Build(); }

    // =====================================================================
    //  Public API (used by BattleManager)
    // =====================================================================
    public void Show(BattleManager manager)
    {
        Build();
        bm = manager;
        foreach (var b in enemyBars) if (b.root != null) Destroy(b.root);
        enemyBars.Clear();

        int n = bm.Enemies.Count;
        for (int i = 0; i < n; i++) CreateEnemyBar(bm.Enemies[i], (i - (n - 1) * 0.5f) * 330f);

        content.gameObject.SetActive(true);
        banner.text = "";
        prompt.text = "";
        turnLabel.text = "";
    }

    public void Hide()
    {
        if (content != null) content.gameObject.SetActive(false);
        foreach (var p in popups) if (p.text != null) Destroy(p.text.gameObject);
        popups.Clear();
        bm = null;
    }

    public void ShowBanner(string text, float seconds)
    {
        if (banner == null) return;
        banner.text = text;
        bannerUntil = Time.time + seconds;
    }

    public void SetTurnLabel(string text) { if (turnLabel != null) turnLabel.text = text; }

    public void SetCounterPrompt(bool on, CounterType type)
    {
        if (prompt == null) return;
        if (!on) { prompt.text = ""; return; }
        prompt.text = type == CounterType.Interrupt ? "INTERRUPT!   (1 / A)" : "DODGE!   (D / B)";
    }

    public void Popup(Vector3 worldPos, string text, Color color)
    {
        if (content == null || !content.gameObject.activeSelf) return;
        var t = UIKit.NewText("Popup", content, text, 40, TextAnchor.MiddleCenter, color);
        UIKit.Center(t.rectTransform, Vector2.zero, new Vector2(200, 50));
        t.fontStyle = FontStyle.Bold;
        t.gameObject.AddComponent<Outline>().effectColor = Color.black;
        popups.Add(new PopupText { text = t, world = worldPos, born = Time.time, color = color });
    }

    // =====================================================================
    //  Update
    // =====================================================================
    void Update()
    {
        if (!built || bm == null || content == null || !content.gameObject.activeSelf) return;

        var stats = bm.Stats;
        var hp = bm.PlayerHealth;
        if (stats != null && hp != null)
        {
            SetBar(hpFill, hp.Normalized);
            hpText.text = $"Health  {Mathf.CeilToInt(hp.Current)} / {Mathf.CeilToInt(hp.maxHealth)}";
            SetBar(stFill, stats.maxStamina > 0 ? stats.stamina / stats.maxStamina : 0f);
            stText.text = $"Stamina  {Mathf.CeilToInt(stats.stamina)} / {Mathf.CeilToInt(stats.maxStamina)}";
            SetBar(chFill, stats.ChargeNormalized);
            chText.text = $"Macuahuitl  {Mathf.RoundToInt(stats.ChargeNormalized * 100f)}%";
            furyText.text = stats.FuryTurnsLeft > 0 ? $"FURY ({stats.FuryTurnsLeft})" : "";
            UpdateMenu(stats);
        }

        for (int i = 0; i < enemyBars.Count; i++)
        {
            var eb = enemyBars[i];
            if (eb.enemy == null) { SetBar(eb.fill, 0f); continue; }
            SetBar(eb.fill, eb.enemy.Health.Normalized);
            bool isTarget = i == bm.TargetIndex && !eb.enemy.Health.IsDead && bm.Enemies.Count > 1;
            eb.name.text = (isTarget ? "> " : "") + eb.enemy.data.displayName;
        }

        if (banner.text != "" && Time.time > bannerUntil) banner.text = "";
        UpdatePopups();
    }

    void UpdateMenu(PlayerCombatStats s)
    {
        foreach (var m in menu)
        {
            bool ok = bm.AcceptingInput;
            string text = "";
            switch (m.action)
            {
                case PlayerAction.Attack:
                    text = $"Attack  [1 / A]  (-{s.attackCost:0})";
                    ok &= s.CanAfford(s.attackCost);
                    break;
                case PlayerAction.Fury:
                    text = s.furyUnlocked ? $"Mictlan Fury  [2 / X]  (-{s.furyCost:0})" : "??? (locked)";
                    ok &= s.furyUnlocked && !s.FuryUsedThisBattle && s.CanAfford(s.furyCost);
                    break;
                case PlayerAction.Seismic:
                    text = s.seismicUnlocked ? $"Seismic Jump  [3 / Y]  (-{s.seismicCost:0})" : "??? (locked)";
                    ok &= s.seismicUnlocked && s.CanAfford(s.seismicCost);
                    break;
                case PlayerAction.Potion:
                    text = $"Potion x{s.potions}  [4 / D-pad up]";
                    ok &= s.potions > 0 && s.CanAfford(s.potionCost);
                    break;
                case PlayerAction.EndTurn:
                    text = "End turn  [Q / RB]";
                    break;
            }
            m.label.text = text;
            m.button.interactable = ok;
        }
    }

    void UpdatePopups()
    {
        var cam = Camera.main;
        for (int i = popups.Count - 1; i >= 0; i--)
        {
            var p = popups[i];
            float age = Time.time - p.born;
            if (p.text == null || age > 1.1f || cam == null)
            {
                if (p.text != null) Destroy(p.text.gameObject);
                popups.RemoveAt(i);
                continue;
            }
            Vector3 sp = cam.WorldToScreenPoint(p.world + Vector3.up * (age * 1.5f));
            p.text.enabled = sp.z > 0f;
            if (sp.z <= 0f) continue;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(content, sp, null, out Vector2 lp);
            p.text.rectTransform.anchoredPosition = lp;
            var c = p.color; c.a = 1f - Mathf.Clamp01((age - 0.6f) / 0.5f);
            p.text.color = c;
        }
    }

    // =====================================================================
    //  UI construction
    // =====================================================================
    void Build()
    {
        if (built) return;
        built = true;

        UIKit.EnsureEventSystem();

        // "content" is always the largest 16:9 rectangle that fits the window.
        content = UIKit.CreateScreen("BattleCanvas", 50, transform);

        // Slightly darken the world during combat to add focus.
        UIKit.Stretch(UIKit.NewImage("Dim", content, new Color(0f, 0f, 0f, 0.28f)).rectTransform);

        BuildPlayerPanel();
        BuildMenu();
        BuildTexts();

        enemyContainer = UIKit.NewRect("Enemies", content);
        UIKit.Anchor(enemyContainer, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -16), new Vector2(1, 1));

        content.gameObject.SetActive(false);
    }

    void BuildPlayerPanel()
    {
        var panel = UIKit.NewImage("PlayerPanel", content, Color.white);
        UIKit.Anchor(panel.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-20, 20), new Vector2(404, 160));
        AztecUI.StylePanel(panel, 404, 160);

        var title = UIKit.NewText("Name", panel.transform, "TLAHUELLÍ", 18, TextAnchor.UpperLeft, Color.white);
        UIKit.Anchor(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(24, -14), new Vector2(-48, 24));
        furyText = UIKit.NewText("Fury", panel.transform, "", 18, TextAnchor.UpperRight, new Color(0.9f, 0.5f, 1f));
        UIKit.Anchor(furyText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -14), new Vector2(-48, 24));

        MakeBar(panel.transform, new Vector2(20, -40), new Vector2(364, 30), UIKit.Accent, out hpFill, out hpText);
        MakeBar(panel.transform, new Vector2(20, -76), new Vector2(364, 26), StaminaGold, out stFill, out stText);
        MakeBar(panel.transform, new Vector2(20, -108), new Vector2(364, 26), ChargePurple, out chFill, out chText);
    }

    void BuildMenu()
    {
        var panel = UIKit.NewImage("Menu", content, Color.white);
        UIKit.Anchor(panel.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(20, 20), new Vector2(394, 5 * 48 + 40));
        AztecUI.StylePanel(panel, 394, 5 * 48 + 40);

        PlayerAction[] actions = { PlayerAction.Attack, PlayerAction.Fury, PlayerAction.Seismic, PlayerAction.Potion, PlayerAction.EndTurn };
        for (int i = 0; i < actions.Length; i++)
        {
            var act = actions[i];
            var img = UIKit.NewImage("Btn_" + act, panel.transform, Color.white);
            img.raycastTarget = true;
            UIKit.Anchor(img.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20 - i * 48), new Vector2(354, 42));

            var btn = img.gameObject.AddComponent<Button>();
            AztecUI.StyleButton(btn, img, 354, 42);
            img.gameObject.AddComponent<ButtonSounds>();
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            btn.onClick.AddListener(() => BattleManager.Instance.RequestAction(act));

            var label = UIKit.NewText("Label", img.transform, "", 17, TextAnchor.MiddleLeft, Color.white);
            UIKit.Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(16, 0);
            label.rectTransform.offsetMax = new Vector2(-12, 0);
            label.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);

            menu.Add(new MenuEntry { action = act, button = btn, label = label });
        }
    }

    void BuildTexts()
    {
        banner = UIKit.NewText("Banner", content, "", 34, TextAnchor.MiddleCenter, Color.white);
        UIKit.Anchor(banner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -110), new Vector2(1100, 70));
        banner.fontStyle = FontStyle.Bold;
        banner.gameObject.AddComponent<Outline>().effectColor = Color.black;

        prompt = UIKit.NewText("Prompt", content, "", 58, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.2f));
        UIKit.Center(prompt.rectTransform, new Vector2(0, -60), new Vector2(1000, 90));
        prompt.fontStyle = FontStyle.Bold;
        prompt.gameObject.AddComponent<Outline>().effectColor = Color.black;

        turnLabel = UIKit.NewText("Turn", content, "", 26, TextAnchor.MiddleCenter, Color.white);
        UIKit.Anchor(turnLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 54), new Vector2(700, 40));
        turnLabel.gameObject.AddComponent<Outline>().effectColor = Color.black;

        var hint = UIKit.NewText("Hint", content, "Target: left/right arrows   |   Dodge: D / B", 16, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.7f));
        UIKit.Anchor(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 18), new Vector2(800, 30));
    }

    void CreateEnemyBar(EnemyController e, float x)
    {
        var panel = UIKit.NewImage("EnemyBar", enemyContainer, UIKit.PanelColor);
        UIKit.Anchor(panel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, 0), new Vector2(320, 56));

        var name = UIKit.NewText("Name", panel.transform, e.data.displayName, 18, TextAnchor.UpperCenter, Color.white);
        UIKit.Anchor(name.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -3), new Vector2(0, 24));

        MakeBar(panel.transform, new Vector2(10, -28), new Vector2(300, 20), UIKit.Accent, out var fill, out var label);
        label.text = "";

        enemyBars.Add(new EnemyBar { root = panel.gameObject, fill = fill, name = name, enemy = e });
    }

    // ---------------- Helpers ----------------
    static void MakeBar(Transform parent, Vector2 pos, Vector2 size, Color fillColor, out RectTransform fill, out Text label)
    {
        var bg = UIKit.NewImage("Bar", parent, new Color(0, 0, 0, 0.7f));
        UIKit.Anchor(bg.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), pos, size);

        var f = UIKit.NewImage("Fill", bg.transform, fillColor);
        UIKit.Anchor(f.rectTransform, Vector2.zero, Vector2.one, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
        fill = f.rectTransform;

        label = UIKit.NewText("Label", bg.transform, "", 15, TextAnchor.MiddleCenter, Color.white);
        UIKit.Stretch(label.rectTransform);
        label.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
    }

    static void SetBar(RectTransform fill, float value)
    {
        if (fill == null) return;
        fill.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
    }
}
