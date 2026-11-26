using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// The full-screen map of Mictlán. {Map} (M / View) opens and closes it while exploring; {Back} also closes it.
    /// Every time it opens, a camera takes a picture of the whole level (so opened chests and changes show up),
    /// and on top of it: Tlahuelli (an arrow that points where he looks), every enemy still alive (red; the boss
    /// is a big skull), merchants, villagers, side quests and landmarks (MapMarker), the name of each region,
    /// a legend and how many enemies are left. The world stops while the map is open.
    /// </summary>
    public class WorldMapScreen : MonoBehaviour
    {
        [SerializeField] Font font;
        [SerializeField] Tilemap ground;
        [SerializeField] Transform player;

        [Header("Icons")]
        [SerializeField] Sprite playerIcon;
        [SerializeField] Sprite enemyIcon;
        [SerializeField] Sprite bossIcon;
        [SerializeField] Sprite merchantIcon;
        [SerializeField] Sprite villagerIcon;
        [SerializeField] Sprite questIcon;
        [SerializeField] Sprite landmarkIcon;
        [SerializeField] Sprite doorIcon;

        [SerializeField, Min(2)] int pixelsPerTile = 6;

        /// <summary>Where the player is drawn instead of his real position (inside a house: at its door).</summary>
        public static Vector3? PlayerPositionOverride { get; set; }

        GameObject root;
        RawImage picture;
        RectTransform markerLayer;
        Text enemiesText, hintText;
        RenderTexture texture;
        Bounds bounds;
        readonly List<GameObject> markers = new List<GameObject>();
        RectTransform playerArrow;
        PlayerDirection direction;
        bool open;

        public static bool IsOpen { get; private set; }

        void Start()
        {
            if (ground == null)
                foreach (var map in FindObjectsByType<Tilemap>())
                    if (map.name == "Ground") ground = map;
            if (player == null)
            {
                var p = FindAnyObjectByType<PlayerCombatant>();
                if (p != null) player = p.transform;
            }
            if (player != null) direction = player.GetComponent<PlayerDirection>();
            if (ground != null)
            {
                ground.CompressBounds();
                Vector3 min = ground.CellToWorld(ground.cellBounds.min), max = ground.CellToWorld(ground.cellBounds.max);
                bounds = new Bounds((min + max) / 2f, max - min);
            }
        }

        void OnDestroy()
        {
            if (texture != null) texture.Release();
            IsOpen = false;
        }

        void Update()
        {
            if (open)
            {
                if (TogglePressed() || MenuWidgets.CancelPressed()) StartCoroutine(Close());
                else UpdatePlayerArrow();
                return;
            }
            if (TogglePressed() && GameState.IsExploring && !DialogueBox.IsOpen && ground != null) Open();
        }

        static bool TogglePressed()
        {
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            return (keyboard != null && (keyboard.mKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame))
                   || (pad != null && pad.selectButton.wasPressedThisFrame);
        }

        void Open()
        {
            if (root == null) Build();
            open = IsOpen = true;
            GameState.SetMode(GameMode.Cutscene);
            RenderWorld();
            PlaceMarkers();
            root.SetActive(true);
            GameAudio.PlaySfx("ui_select", 0.8f, 0f);
        }

        IEnumerator Close()
        {
            open = false;
            root.SetActive(false);
            GameAudio.PlaySfx("ui_back", 0.8f, 0f);
            yield return null;   // the ESC that closed the map must not open the pause menu
            IsOpen = false;
            GameState.SetMode(GameMode.Exploration);
        }

        // ---------------- Picture of the world ----------------

        void RenderWorld()
        {
            int w = Mathf.Clamp(Mathf.RoundToInt(bounds.size.x * pixelsPerTile), 64, 2400);
            int h = Mathf.Clamp(Mathf.RoundToInt(bounds.size.y * pixelsPerTile), 64, 1400);
            if (texture == null)
            {
                texture = new RenderTexture(w, h, 16) { filterMode = FilterMode.Bilinear };
                picture.texture = texture;
            }

            // Characters are drawn as icons, not in the picture: hide them while the camera shoots.
            var hidden = new List<Renderer>();
            foreach (var enemy in FindObjectsByType<CombatEnemy>())
                foreach (var r in enemy.GetComponentsInChildren<Renderer>())
                    if (r.enabled) { r.enabled = false; hidden.Add(r); }
            if (player != null)
                foreach (var r in player.GetComponentsInChildren<Renderer>())
                    if (r.enabled) { r.enabled = false; hidden.Add(r); }

            var go = new GameObject("WorldMapCamera");
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = bounds.size.y / 2f;
            cam.aspect = bounds.size.x / bounds.size.y;
            cam.transform.position = new Vector3(bounds.center.x, bounds.center.y, -50f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.cullingMask = ~LayerMask.GetMask("UI", "TransparentFX");   // no UI and no darkness of the night
            cam.targetTexture = texture;
            cam.enabled = false;
            cam.Render();
            cam.targetTexture = null;
            Destroy(go);
            foreach (var r in hidden) r.enabled = true;
        }

        // ---------------- Icons ----------------

        Vector2 ToMap(Vector3 world)
        {
            Vector2 size = picture.rectTransform.rect.size;
            world = HouseDoor.OutsidePositionOf(world) ?? world;   // inside a room: shown at its door outside
            float x = Mathf.InverseLerp(bounds.min.x, bounds.max.x, world.x);
            float y = Mathf.InverseLerp(bounds.min.y, bounds.max.y, world.y);
            return new Vector2((x - 0.5f) * size.x, (y - 0.5f) * size.y);
        }

        void PlaceMarkers()
        {
            foreach (var m in markers) Destroy(m);
            markers.Clear();

            // Region names, big and faint, in the middle of each region.
            foreach (var zone in FindObjectsByType<ZoneArea>())
            {
                string zoneName = zone.name.StartsWith("Zone_") ? zone.name.Substring(5) : zone.name;
                var label = MenuWidgets.Label("Zone", markerLayer, zoneName, 34, MexicaPalette.WithAlpha(MexicaPalette.Bone, 0.55f));
                label.fontStyle = FontStyle.Bold;
                var outline = label.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
                outline.effectDistance = new Vector2(2, -2);
                // A region inside a room (the temple) is named above its door, so it does not cover the icons there.
                bool inRoom = HouseDoor.OutsidePositionOf(zone.transform.position) != null;
                MenuWidgets.Center(label.rectTransform, ToMap(zone.transform.position) + (inRoom ? new Vector2(0f, 60f) : Vector2.zero), new Vector2(600, 50));
                markers.Add(label.gameObject);
            }

            foreach (var marker in MapMarker.All)
            {
                Sprite icon = marker.Type switch
                {
                    MapMarker.Kind.Merchant => merchantIcon,
                    MapMarker.Kind.Villager => villagerIcon,
                    MapMarker.Kind.SideQuest => questIcon,
                    MapMarker.Kind.Door => doorIcon,
                    _ => landmarkIcon,
                };
                float size = marker.Type == MapMarker.Kind.Villager ? 22f : 34f;
                var color = marker.Done ? MexicaPalette.Ash : Color.white;
                string text = marker.Done ? marker.Label + "  ✓" : marker.Label;
                bool showText = marker.Type != MapMarker.Kind.Villager;
                Icon(icon, marker.Position, size, color, showText ? text : null);
            }

            int remaining = 0;
            foreach (var enemy in FindObjectsByType<CombatEnemy>())
            {
                if (enemy.Health == null || enemy.Health.IsDead) continue;
                bool boss = enemy.Data != null && enemy.Data.isBoss;
                if (enemy.CountsAsRemaining) remaining++;
                Icon(boss ? bossIcon : enemyIcon, enemy.transform.position, boss ? 40f : 16f,
                    enemy.Optional ? new Color(0.75f, 0.4f, 0.9f) : Color.white, boss ? "Quetzalcóatl" : null);
            }
            enemiesText.text = remaining > 0 ? $"Enemies left: {remaining}" : "Every servant of the gods has fallen";

            var arrow = Icon(playerIcon, PlayerPosition(), 30f, Color.white, null);
            playerArrow = arrow.rectTransform;
            playerArrow.SetAsLastSibling();
            UpdatePlayerArrow();
        }

        Vector3 PlayerPosition() => PlayerPositionOverride ?? (player != null ? player.position : bounds.center);

        void UpdatePlayerArrow()
        {
            if (playerArrow == null) return;
            playerArrow.anchoredPosition = ToMap(PlayerPosition());
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.15f;
            playerArrow.localScale = Vector3.one * pulse;
            if (direction != null && direction.LastValid != Vector2.zero)
            {
                float angle = Mathf.Atan2(direction.LastValid.y, direction.LastValid.x) * Mathf.Rad2Deg - 90f;
                playerArrow.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        Image Icon(Sprite sprite, Vector3 world, float size, Color color, string label)
        {
            var image = MenuWidgets.Image("Icon", markerLayer, color);
            image.sprite = sprite;
            image.preserveAspect = true;
            MenuWidgets.Center(image.rectTransform, ToMap(world), new Vector2(size, size));
            markers.Add(image.gameObject);
            if (!string.IsNullOrEmpty(label))
            {
                var text = MenuWidgets.Label("Label", markerLayer, label, 20, MexicaPalette.Bone);
                var outline = text.gameObject.AddComponent<Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(1.5f, -1.5f);
                MenuWidgets.Center(text.rectTransform, ToMap(world) + new Vector2(0, -size * 0.5f - 13f), new Vector2(260, 26));
                markers.Add(text.gameObject);
            }
            return image;
        }

        // ---------------- Window ----------------

        void Build()
        {
            MenuWidgets.Use(font, true);
            var canvas = MenuWidgets.Screen("WorldMapCanvas", 42, transform);
            root = canvas.gameObject;
            MenuWidgets.Stretch(MenuWidgets.Image("Shade", canvas, new Color(0.02f, 0.015f, 0.02f, 1f)).rectTransform);

            var title = MenuWidgets.Label("Title", canvas, "MAP OF MICTLÁN", 54, MexicaPalette.AgedGold);
            MenuWidgets.Center(title.rectTransform, new Vector2(0, 478), new Vector2(1200, 70));

            float aspect = bounds.size.y > 0f ? bounds.size.x / bounds.size.y : 2f;
            float width = Mathf.Min(1760f, 760f * aspect);
            var frame = MenuWidgets.Panel("Frame", canvas, new Vector2(0, 20), new Vector2(width + 44, width / aspect + 44));
            picture = MenuWidgets.Picture("World", frame.transform, null);
            MenuWidgets.Center(picture.rectTransform, Vector2.zero, new Vector2(width, width / aspect));
            markerLayer = MenuWidgets.Rect("Markers", picture.transform);
            MenuWidgets.Stretch(markerLayer);

            // Legend and counter at the bottom.
            var legend = MenuWidgets.Rect("Legend", canvas);
            MenuWidgets.Center(legend, new Vector2(-170, -478), new Vector2(1400, 50));
            (Sprite icon, string text, Color color)[] items =
            {
                (playerIcon, "Tlahuelli", Color.white), (enemyIcon, "Enemy", Color.white), (bossIcon, "Boss", Color.white),
                (merchantIcon, "Merchant", Color.white), (villagerIcon, "Villager", Color.white),
                (questIcon, "Side quest", Color.white), (landmarkIcon, "Landmark", Color.white),
            };
            for (int i = 0; i < items.Length; i++)
            {
                float x = -660 + i * 200;
                var icon = MenuWidgets.Image("Key", legend, items[i].color);
                icon.sprite = items[i].icon;
                icon.preserveAspect = true;
                MenuWidgets.Center(icon.rectTransform, new Vector2(x, 0), new Vector2(30, 30));
                var label = MenuWidgets.Label("KeyText", legend, items[i].text, 24, MexicaPalette.Bone, TextAnchor.MiddleLeft);
                MenuWidgets.Center(label.rectTransform, new Vector2(x + 95, 0), new Vector2(150, 40));
            }
            enemiesText = MenuWidgets.Label("Enemies", canvas, "", 28, MexicaPalette.Cinnabar, TextAnchor.MiddleRight);
            MenuWidgets.Center(enemiesText.rectTransform, new Vector2(640, 478), new Vector2(560, 50));
            hintText = MenuWidgets.Label("Hint", canvas, "", 26, MexicaPalette.Ash, TextAnchor.MiddleRight);
            MenuWidgets.Center(hintText.rectTransform, new Vector2(700, -478), new Vector2(420, 50));
            GlyphText.Set(hintText, "{Map}  Close");
            root.SetActive(false);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            IsOpen = false;
            PlayerPositionOverride = null;
        }
    }
}
