using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Teaches the game while you play. Three kinds of tips, each shown only once (remembered in PlayerPrefs;
/// starting a NEW game shows them again):
///   Toast  - a small card at the bottom of the screen that does not stop the game.
///   Modal  - a card that pauses the battle until you press a key (used before the dodge and combat basics).
///   Guide  - the exploration walkthrough (move, sprint/hop, map, enemies, pause), driven by what you actually do.
/// BattleManager and GameBootstrap call this; nothing needs to be placed in a scene.
/// </summary>
public class TutorialTips : MonoBehaviour
{
    // Every tip id (so a new game can reset them all).
    static readonly string[] AllIds =
    {
        "move", "sprint", "map", "enemies", "pause",
        "combat_basics", "enemy_turn", "interrupt", "victory", "fury", "seismic"
    };

    static TutorialTips instance;
    public static TutorialTips Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("TutorialTips");
                instance = go.AddComponent<TutorialTips>();
            }
            return instance;
        }
    }

    public static bool Enabled
    {
        get => PlayerPrefs.GetInt("mictlan_tips_enabled", 1) == 1;
        set { PlayerPrefs.SetInt("mictlan_tips_enabled", value ? 1 : 0); }
    }

    public static bool Seen(string id) => PlayerPrefs.GetInt("mictlan_tip_" + id, 0) == 1;
    static void MarkSeen(string id) { PlayerPrefs.SetInt("mictlan_tip_" + id, 1); }

    /// <summary>Call when a NEW game starts so the tips are shown again.</summary>
    public static void ResetAll()
    {
        foreach (var id in AllIds) PlayerPrefs.DeleteKey("mictlan_tip_" + id);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { instance = null; }

    // ---------------- UI ----------------
    RectTransform content;
    GameObject toastRoot;
    Text toastText;
    GameObject modalRoot;
    Text modalTitle, modalBody, modalFooter;
    Coroutine toastRoutine;
    bool built;

    void Build()
    {
        if (built) return;
        built = true;

        content = UIKit.CreateScreen("TutorialCanvas", 70, transform);

        // --- Toast (bottom center) ---
        var toast = UIKit.NewImage("Toast", content, new Color(0.06f, 0.03f, 0.03f, 0.92f));
        toastRoot = toast.gameObject;
        UIKit.Anchor(toast.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 28), new Vector2(980, 96));
        var edge = UIKit.NewImage("Edge", toast.transform, UIKit.Accent);
        UIKit.Anchor(edge.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), Vector2.zero, new Vector2(6, 0));
        var tag = UIKit.NewText("Tag", toast.transform, "TIP", 18, TextAnchor.UpperLeft, UIKit.Accent);
        tag.fontStyle = FontStyle.Bold;
        UIKit.Anchor(tag.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -8), new Vector2(80, 24));
        toastText = UIKit.NewText("Text", toast.transform, "", 24, TextAnchor.MiddleLeft, Color.white);
        toastText.horizontalOverflow = HorizontalWrapMode.Wrap;
        UIKit.Stretch(toastText.rectTransform);
        toastText.rectTransform.offsetMin = new Vector2(20, 6);
        toastText.rectTransform.offsetMax = new Vector2(-16, -24);
        toastRoot.SetActive(false);

        // --- Modal (center card) ---
        var modal = UIKit.NewImage("Modal", content, new Color(0, 0, 0, 0.72f));
        modalRoot = modal.gameObject;
        UIKit.Stretch(modal.rectTransform);
        var card = UIKit.NewImage("Card", modal.transform, new Color(0.08f, 0.04f, 0.04f, 0.98f));
        UIKit.Center(card.rectTransform, new Vector2(0, 10), new Vector2(940, 440));
        var top = UIKit.NewImage("TopBar", card.transform, UIKit.Accent);
        UIKit.Anchor(top.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 8));

        modalTitle = UIKit.NewText("Title", card.transform, "", 42, TextAnchor.UpperCenter, new Color(0.95f, 0.25f, 0.25f));
        modalTitle.fontStyle = FontStyle.Bold;
        UIKit.Anchor(modalTitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -26), new Vector2(0, 60));

        modalBody = UIKit.NewText("Body", card.transform, "", 27, TextAnchor.UpperLeft, Color.white);
        modalBody.horizontalOverflow = HorizontalWrapMode.Wrap;
        UIKit.Stretch(modalBody.rectTransform);
        modalBody.rectTransform.offsetMin = new Vector2(46, 70);
        modalBody.rectTransform.offsetMax = new Vector2(-46, -100);

        modalFooter = UIKit.NewText("Footer", card.transform, "Press any key to continue", 22, TextAnchor.LowerCenter, new Color(1, 1, 1, 0.7f));
        UIKit.Anchor(modalFooter.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(0, 34));
        modalRoot.SetActive(false);
    }

    // =====================================================================
    //  Toast: non-blocking
    // =====================================================================
    public void Toast(string id, string text, float seconds = 6f)
    {
        if (!Enabled) return;
        if (id != null)
        {
            if (Seen(id)) return;
            MarkSeen(id);
        }
        Build();
        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(ToastRoutine(text, seconds));
    }

    /// <summary>Shows a toast and keeps it until `stop` returns true (or `timeout` seconds pass).</summary>
    public IEnumerator ToastUntil(string text, System.Func<bool> stop, float timeout)
    {
        Build();
        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastText.text = text;
        toastRoot.SetActive(true);
        Audio.Play("tip", 0.6f);
        float t = 0f;
        while (t < timeout && !stop())
        {
            // Hide the card during battles (the battle HUD needs that space) and only count time while exploring.
            bool exploring = GameState.Mode == GameMode.Exploration;
            toastRoot.SetActive(GameState.Mode != GameMode.Combat);
            if (exploring) t += Time.unscaledDeltaTime;
            yield return null;
        }
        toastRoot.SetActive(false);
    }

    IEnumerator ToastRoutine(string text, float seconds)
    {
        toastText.text = text;
        toastRoot.SetActive(true);
        Audio.Play("tip", 0.6f);
        yield return new WaitForSecondsRealtime(seconds);
        toastRoot.SetActive(false);
        toastRoutine = null;
    }

    // =====================================================================
    //  Modal: waits for a key
    // =====================================================================
    public IEnumerator Modal(string id, string title, string body)
    {
        if (!Enabled) yield break;
        if (id != null)
        {
            if (Seen(id)) yield break;
            MarkSeen(id);
        }
        Build();
        if (toastRoot != null) toastRoot.SetActive(false);

        modalTitle.text = title;
        modalBody.text = body;
        modalRoot.SetActive(true);
        Audio.Play("tip");

        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime;
            modalFooter.color = new Color(1, 1, 1, 0.45f + 0.5f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.5f)));
            if (t > 0.6f && UIKit.AnyPress()) break;
            yield return null;
        }
        modalRoot.SetActive(false);
        Audio.Play("ui_select", 0.6f);
        yield return null;   // one extra frame so the key that closed the card is not read as a battle command
    }

    // =====================================================================
    //  Exploration guide
    // =====================================================================
    Coroutine guide;

    public void StartExplorationGuide()
    {
        if (!Enabled || guide != null) return;
        guide = StartCoroutine(GuideRoutine());
    }

    IEnumerator GuideRoutine()
    {
        Build();
        yield return new WaitForSecondsRealtime(1.2f);

        var player = FindFirstObjectByType<Player2D>();
        if (player == null) yield break;
        var rb = player.GetComponent<Rigidbody2D>();

        // 1) Move
        if (!Seen("move"))
        {
            MarkSeen("move");
            float moved = 0f;
            yield return ToastUntil("Welcome, Tlahuelli.   Move with  W A S D  or the arrow keys  (left stick on a gamepad).",
                () => { if (rb != null && rb.linearVelocity.magnitude > 0.5f) moved += Time.unscaledDeltaTime; else moved = 0f; return moved > 0.8f; }, 120f);
            yield return new WaitForSecondsRealtime(0.8f);
        }

        // 2) Sprint + hop
        if (!Seen("sprint"))
        {
            MarkSeen("sprint");
            yield return ToastUntil("Hold  Left Shift  to sprint  (click the left stick).   Press  Space  (A button) to hop.",
                () => player != null && player.IsJumping, 14f);
            yield return new WaitForSecondsRealtime(0.8f);
        }

        // 3) Map
        if (!Seen("map"))
        {
            MarkSeen("map");
            yield return ToastUntil("Top-right is your MAP.  White dot = you,  red dots = enemies  (orange = it has noticed you).   Press  M  to hide or show it.",
                () => false, 9f);
            yield return new WaitForSecondsRealtime(0.8f);
        }

        // 4) Enemies
        if (!Seen("enemies"))
        {
            MarkSeen("enemies");
            yield return ToastUntil("Enemies see you in front of them and hear you up close.   Sneak around them, or walk into one to start a turn-based battle.",
                () => false, 10f);
            yield return new WaitForSecondsRealtime(0.8f);
        }

        // 5) Pause
        if (!Seen("pause"))
        {
            MarkSeen("pause");
            yield return ToastUntil("Press  Esc  (Start) to pause, save your game and review the controls.",
                () => false, 7f);
        }
        guide = null;
    }
}
