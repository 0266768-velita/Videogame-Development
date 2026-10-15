using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Minimap in the top-right corner of the screen (exploration only).
/// It is a second orthographic camera that looks at the whole map from above and draws into a small
/// RenderTexture, so it always shows your real map and real art (no extra work when sprites change).
/// Markers: white = Tlahuelli, red = enemy, orange pulsing = an enemy that has noticed you.
/// A thin white rectangle shows what the main camera currently sees.
/// Press M (gamepad Select) to hide or show it.
///
/// It is added automatically by GameBootstrap. Map size comes from CameraFollow2D.bounds.
/// </summary>
public class Minimap : MonoBehaviour
{
    [Header("Size (UI units on the 1280x720 reference screen)")]
    public float mapWidth = 300f;
    public Vector2 margin = new Vector2(18f, 18f);
    public int textureWidth = 384;

    Camera mapCam;
    RenderTexture rt;
    Rect bounds;
    float mapHeight;

    GameObject root;
    CanvasGroup group;
    RectTransform mapRect;
    RectTransform viewFrame;
    Image playerMarker;
    Text label;

    readonly Dictionary<EnemyController, Image> markers = new Dictionary<EnemyController, Image>();
    readonly List<EnemyController> stale = new List<EnemyController>();
    float refreshTimer;
    bool visible = true;
    InputAction toggle;

    Transform player;
    Camera mainCam;

    const string PrefKey = "mictlan_minimap_visible";

    void Start()
    {
        visible = PlayerPrefs.GetInt(PrefKey, 1) == 1;

        var follow = FindFirstObjectByType<CameraFollow2D>();
        bounds = follow != null ? follow.bounds : new Rect(-32f, -20f, 64f, 40f);
        if (bounds.width < 1f || bounds.height < 1f) bounds = new Rect(-32f, -20f, 64f, 40f);
        mapHeight = mapWidth * bounds.height / bounds.width;

        var p = FindFirstObjectByType<Player2D>();
        if (p != null) player = p.transform;
        mainCam = Camera.main;

        BuildCamera();
        BuildUI();

        toggle = new InputAction("ToggleMap", InputActionType.Button);
        toggle.AddBinding("<Keyboard>/m");
        toggle.AddBinding("<Gamepad>/select");
        toggle.Enable();
    }

    void OnDestroy()
    {
        if (toggle != null) { toggle.Disable(); toggle.Dispose(); }
        if (rt != null) { rt.Release(); Destroy(rt); }
    }

    // ---------------- Camera ----------------
    void BuildCamera()
    {
        int texH = Mathf.Max(32, Mathf.RoundToInt(textureWidth * bounds.height / bounds.width));
        rt = new RenderTexture(textureWidth, texH, 0, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Point };
        rt.Create();

        var go = new GameObject("MinimapCamera");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(bounds.center.x, bounds.center.y, -20f);
        mapCam = go.AddComponent<Camera>();
        mapCam.orthographic = true;
        mapCam.orthographicSize = bounds.height * 0.5f;   // the texture has the same aspect as the map
        mapCam.clearFlags = CameraClearFlags.SolidColor;
        mapCam.backgroundColor = new Color(0.03f, 0.04f, 0.03f, 1f);
        mapCam.cullingMask = ~(1 << 5);                    // everything except the UI layer
        mapCam.targetTexture = rt;
        mapCam.depth = -50f;
        mapCam.nearClipPlane = 0.1f;
        mapCam.farClipPlane = 60f;
        mapCam.useOcclusionCulling = false;
        mapCam.allowHDR = false;
        mapCam.allowMSAA = false;
    }

    // ---------------- UI ----------------
    void BuildUI()
    {
        var content = UIKit.CreateScreen("MinimapCanvas", 20, transform);
        root = content.transform.parent.gameObject;
        group = content.gameObject.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        var frame = UIKit.NewImage("Frame", content, new Color(0.55f, 0.07f, 0.09f, 0.95f));
        UIKit.Anchor(frame.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-margin.x, -margin.y - 18f), new Vector2(mapWidth + 8f, mapHeight + 8f));

        mapRect = UIKit.NewRect("Map", frame.transform);
        UIKit.Anchor(mapRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(mapWidth, mapHeight));
        var raw = mapRect.gameObject.AddComponent<RawImage>();
        raw.texture = rt;
        raw.raycastTarget = false;

        // What the main camera sees (4 thin lines)
        viewFrame = UIKit.NewRect("ViewFrame", mapRect);
        viewFrame.anchorMin = viewFrame.anchorMax = new Vector2(0f, 0f);
        viewFrame.pivot = new Vector2(0.5f, 0.5f);
        var lineColor = new Color(1f, 1f, 1f, 0.55f);
        for (int i = 0; i < 4; i++)
        {
            var l = UIKit.NewImage("Line" + i, viewFrame, lineColor);
            var r = l.rectTransform;
            switch (i)
            {
                case 0: UIKit.Anchor(r, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 2)); break;
                case 1: UIKit.Anchor(r, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 2)); break;
                case 2: UIKit.Anchor(r, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), Vector2.zero, new Vector2(2, 0)); break;
                default: UIKit.Anchor(r, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), Vector2.zero, new Vector2(2, 0)); break;
            }
        }

        playerMarker = NewMarker("Player", Color.white, 11f);

        label = UIKit.NewText("Label", content, "MAP  [M]", 15, TextAnchor.LowerRight, new Color(1, 1, 1, 0.7f));
        UIKit.Anchor(label.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-margin.x, -margin.y + 2f), new Vector2(200, 20));
    }

    Image NewMarker(string name, Color c, float size)
    {
        var outline = UIKit.NewImage(name, mapRect, Color.black);
        var r = outline.rectTransform;
        r.anchorMin = r.anchorMax = Vector2.zero;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(size + 4f, size + 4f);
        var inner = UIKit.NewImage("Dot", outline.transform, c);
        UIKit.Stretch(inner.rectTransform);
        inner.rectTransform.offsetMin = new Vector2(2, 2);
        inner.rectTransform.offsetMax = new Vector2(-2, -2);
        return inner;
    }

    // ---------------- Update ----------------
    Vector2 ToMap(Vector2 world)
    {
        float nx = (world.x - bounds.xMin) / bounds.width;
        float ny = (world.y - bounds.yMin) / bounds.height;
        return new Vector2(Mathf.Clamp01(nx) * mapWidth, Mathf.Clamp01(ny) * mapHeight);
    }

    void Update()
    {
        if (mapCam == null) return;

        if (toggle != null && toggle.WasPressedThisFrame() && GameState.Mode == GameMode.Exploration)
        {
            visible = !visible;
            PlayerPrefs.SetInt(PrefKey, visible ? 1 : 0);
            Audio.Play("ui_move", 0.7f);
        }

        // Only shown while exploring (also stays visible, dimmed, behind the pause menu)
        bool show = visible && (GameState.Mode == GameMode.Exploration || GameState.Mode == GameMode.Paused);
        if (root.activeSelf != show) root.SetActive(show);
        mapCam.enabled = show;
        if (!show) return;

        if (player == null)
        {
            var p = FindFirstObjectByType<Player2D>();
            if (p != null) player = p.transform;
        }
        if (mainCam == null) mainCam = Camera.main;

        if (player != null) ((RectTransform)playerMarker.transform.parent).anchoredPosition = ToMap(player.position);

        // Camera view rectangle
        if (mainCam != null)
        {
            float halfH = mainCam.orthographicSize;
            float halfW = halfH * mainCam.aspect;
            Vector2 c = ToMap(mainCam.transform.position);
            viewFrame.anchoredPosition = c;
            viewFrame.sizeDelta = new Vector2(halfW * 2f / bounds.width * mapWidth, halfH * 2f / bounds.height * mapHeight);
        }

        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = 0.25f;
            SyncMarkers();
        }

        float pulse = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f));
        foreach (var kv in markers)
        {
            var e = kv.Key;
            var img = kv.Value;
            if (e == null) continue;
            ((RectTransform)img.transform.parent).anchoredPosition = ToMap(e.transform.position);
            bool alert = e.CurrentState == EnemyController.State.Chase;
            img.color = alert ? new Color(1f, 0.65f * pulse + 0.2f, 0.1f) : new Color(0.9f, 0.18f, 0.18f);
        }
    }

    void SyncMarkers()
    {
        // Add markers for new enemies
        foreach (var e in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
        {
            if (e == null || e.CurrentState == EnemyController.State.Defeated) continue;
            if (!markers.ContainsKey(e))
            {
                float size = e.data != null && e.data.archetype == EnemyArchetype.Giant ? 12f : 9f;
                markers[e] = NewMarker("Enemy", new Color(0.9f, 0.18f, 0.18f), size);
            }
        }
        // Remove markers of enemies that are gone or defeated
        stale.Clear();
        foreach (var kv in markers)
            if (kv.Key == null || kv.Key.CurrentState == EnemyController.State.Defeated) stale.Add(kv.Key);
        foreach (var e in stale)
        {
            if (markers.TryGetValue(e, out var img) && img != null) Destroy(img.transform.parent.gameObject);
            markers.Remove(e);
        }
    }
}
