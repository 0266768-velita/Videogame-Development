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
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.normalColor = new Color(0.16f, 0.06f, 0.06f, 0.92f);
        colors.highlightedColor = new Color(0.55f, 0.12f, 0.12f, 1f);
        colors.selectedColor = Accent;
        colors.pressedColor = new Color(0.9f, 0.3f, 0.3f, 1f);
        colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
        colors.fadeDuration = 0.05f;
        btn.colors = colors;
        img.gameObject.AddComponent<ButtonSounds>();
        btn.onClick.AddListener(() => Audio.Play(label == "Back" || label == "Resume" ? "ui_back" : "ui_select"));
        if (onClick != null) btn.onClick.AddListener(onClick);

        var text = NewText("Label", img.transform, label, fontSize, TextAnchor.MiddleCenter, Color.white);
        Stretch(text.rectTransform);
        return btn;
    }

    public static void SetButtonLabel(Button b, string label)
    {
        var t = b.GetComponentInChildren<Text>();
        if (t != null) t.text = label;
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        var module = es.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
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
