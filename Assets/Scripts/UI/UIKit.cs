using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>Helpers to build UI in code (Boot, menus, pause, battle HUD).</summary>
public static class UIKit
{
    public static readonly Color Accent = new Color(0.80f, 0.10f, 0.12f);   // red of the Micta Forge logo
    public static readonly Color PanelColor = new Color(0.07f, 0.04f, 0.04f, 0.92f);

    static Font font;
    public static Font DefaultFont
    {
        get
        {
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            }
            return font;
        }
    }

    public static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    public static void Anchor(RectTransform r, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        r.anchorMin = aMin; r.anchorMax = aMax; r.pivot = pivot;
        r.anchoredPosition = pos; r.sizeDelta = size;
    }

    public static void Stretch(RectTransform r)
    {
        Anchor(r, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
    }

    /// <summary>Places an element at the center of its parent with a fixed size and an (x, y) offset.</summary>
    public static void Center(RectTransform r, Vector2 pos, Vector2 size)
    {
        Anchor(r, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
    }

    public static Image NewImage(string name, Transform parent, Color c)
    {
        var r = NewRect(name, parent);
        var img = r.gameObject.AddComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
        return img;
    }

    public static RawImage NewRawImage(string name, Transform parent, Texture tex)
    {
        var r = NewRect(name, parent);
        var raw = r.gameObject.AddComponent<RawImage>();
        raw.texture = tex;
        raw.raycastTarget = false;
        return raw;
    }

    public static Text NewText(string name, Transform parent, string s, int size, TextAnchor anchor, Color c)
    {
        var r = NewRect(name, parent);
        var t = r.gameObject.AddComponent<Text>();
        t.font = DefaultFont; t.fontSize = size; t.alignment = anchor; t.color = c; t.text = s;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    /// <summary>
    /// Creates an Overlay Canvas whose content is ALWAYS a 16:9 rectangle (1280x720 logical units)
    /// centered in the window. Returns that rectangle.
    /// </summary>
    public static RectTransform CreateScreen(string name, int sortingOrder, Transform parent)
    {
        var canvasGo = new GameObject(name);
        if (parent != null) canvasGo.transform.SetParent(parent, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        canvasGo.AddComponent<GraphicRaycaster>();

        var content = NewRect("Content", canvasGo.transform);
        Stretch(content);
        var fitter = content.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 16f / 9f;
        return content;
    }

    public static Button NewButton(Transform parent, string label, Vector2 pos, Vector2 size, UnityAction onClick, int fontSize = 26)
    {
        var img = NewImage("Btn_" + label, parent, Color.white);
        img.raycastTarget = true;
        Center(img.rectTransform, pos, size);

        var btn = img.gameObject.AddComponent<Button>();
        AztecUI.StyleButton(btn, img, size.x, size.y);
        img.gameObject.AddComponent<ButtonSounds>();
        btn.onClick.AddListener(() => Audio.Play(label == "Back" || label == "Resume" ? "ui_back" : "ui_select"));
        if (onClick != null) btn.onClick.AddListener(onClick);

        var text = NewText("Label", img.transform, label, fontSize, TextAnchor.MiddleCenter, Color.white);
        Stretch(text.rectTransform);
        text.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);
        return btn;
    }

    public static void SetButtonLabel(Button b, string label)
    {
        var t = b.GetComponentInChildren<Text>();
        if (t != null) t.text = label;
    }

    public static void EnsureEventSystem()
    {
        var es = Object.FindFirstObjectByType<EventSystem>();
        if (es == null)
        {
            var go = new GameObject("EventSystem");
            es = go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }
        // Our own navigation replaces the module's slow "move" action (it waited ~0.5 s before repeating).
        var ui = es.GetComponent<InputSystemUIInputModule>();
        if (ui != null && ui.move != null) ui.move = null;
        if (es.GetComponent<MenuNavigator>() == null) es.gameObject.AddComponent<MenuNavigator>();
    }

    static float suppressSelectSoundUntil;

    public static void Select(GameObject go)
    {
        if (EventSystem.current == null || go == null) return;
        suppressSelectSoundUntil = Time.unscaledTime + 0.15f;   // no "move" blip when a menu opens
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(go);
    }

    public static bool SelectSoundSuppressed => Time.unscaledTime < suppressSelectSoundUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { suppressSelectSoundUntil = 0f; font = null; }

    // ---------- Quick input checks ----------
    public static bool AnyPress()
    {
        var k = Keyboard.current;
        if (k != null && k.anyKey.wasPressedThisFrame) return true;
        var m = Mouse.current;
        if (m != null && (m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame)) return true;
        var g = Gamepad.current;
        if (g != null && (g.buttonSouth.wasPressedThisFrame || g.startButton.wasPressedThisFrame
                          || g.buttonEast.wasPressedThisFrame || g.buttonWest.wasPressedThisFrame || g.buttonNorth.wasPressedThisFrame)) return true;
        return false;
    }

    public static bool CancelPressed()
    {
        var k = Keyboard.current;
        if (k != null && k.escapeKey.wasPressedThisFrame) return true;
        var g = Gamepad.current;
        return g != null && g.buttonEast.wasPressedThisFrame;
    }

    public static bool PausePressed()
    {
        var k = Keyboard.current;
        if (k != null && k.escapeKey.wasPressedThisFrame) return true;
        var g = Gamepad.current;
        return g != null && g.startButton.wasPressedThisFrame;
    }
}

/// <summary>Plays a soft "move" blip when a button is hovered with the mouse or selected with keys / gamepad.</summary>
public class ButtonSounds : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    Button button;

    void Awake() { button = GetComponent<Button>(); }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && button.interactable) Audio.Play("ui_move", 0.5f);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (UIKit.SelectSoundSuppressed) return;
        if (button != null && button.interactable) Audio.Play("ui_move", 0.5f);
    }
}

/// <summary>
/// Fast menu navigation: arrows / WASD / D-pad / left stick. The first press moves immediately (taps are never
/// swallowed) and holding repeats quickly. Wraps from the last button to the first on a fresh press.
/// </summary>
public class MenuNavigator : MonoBehaviour
{
    const float FirstRepeatDelay = 0.26f;
    const float RepeatRate = 0.075f;

    Vector2Int lastDir;
    float nextTime;

    void Update()
    {
        var es = EventSystem.current;
        if (es == null) return;
        var sel = es.currentSelectedGameObject;
        if (sel == null || !sel.activeInHierarchy) return;
        var s = sel.GetComponent<Selectable>();
        if (s == null || !s.IsInteractable()) return;

        Vector2Int dir = ReadDirection();
        if (dir == Vector2Int.zero) { lastDir = Vector2Int.zero; return; }

        bool fresh = dir != lastDir;
        if (!fresh && Time.unscaledTime < nextTime) return;
        lastDir = dir;
        nextTime = Time.unscaledTime + (fresh ? FirstRepeatDelay : RepeatRate);

        Selectable next = Find(s, dir);
        if (next == null && fresh)
        {
            // wrap around: walk to the far end in the opposite direction
            Selectable w = s;
            for (int i = 0; i < 32; i++)
            {
                var n2 = Find(w, -dir);
                if (n2 == null || n2 == w) break;
                w = n2;
            }
            if (w != s) next = w;
        }
        if (next != null) es.SetSelectedGameObject(next.gameObject);
    }

    static Selectable Find(Selectable s, Vector2Int d)
    {
        if (d.y > 0) return s.FindSelectableOnUp();
        if (d.y < 0) return s.FindSelectableOnDown();
        if (d.x < 0) return s.FindSelectableOnLeft();
        return s.FindSelectableOnRight();
    }

    static Vector2Int ReadDirection()
    {
        int x = 0, y = 0;
        var k = Keyboard.current;
        if (k != null)
        {
            if (k.upArrowKey.isPressed || k.wKey.isPressed) y += 1;
            if (k.downArrowKey.isPressed || k.sKey.isPressed) y -= 1;
            if (k.leftArrowKey.isPressed || k.aKey.isPressed) x -= 1;
            if (k.rightArrowKey.isPressed || k.dKey.isPressed) x += 1;
        }
        var g = Gamepad.current;
        if (g != null)
        {
            if (g.dpad.up.isPressed) y += 1;
            if (g.dpad.down.isPressed) y -= 1;
            if (g.dpad.left.isPressed) x -= 1;
            if (g.dpad.right.isPressed) x += 1;
            Vector2 st = g.leftStick.ReadValue();
            if (st.y > 0.6f) y += 1; else if (st.y < -0.6f) y -= 1;
            if (st.x > 0.6f) x += 1; else if (st.x < -0.6f) x -= 1;
        }
        x = Mathf.Clamp(x, -1, 1); y = Mathf.Clamp(y, -1, 1);
        if (y != 0) return new Vector2Int(0, y);   // menus are vertical: vertical wins
        return new Vector2Int(x, 0);
    }
}
