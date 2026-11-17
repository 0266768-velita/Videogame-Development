using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// Map of the whole level in a corner of the screen. Reusable in ANY map: it reads the size of the
    /// Ground tilemap of the scene and points a second camera at it, so it updates itself when the map changes.
    ///   1. A second Camera looks at the whole map and draws into a texture (RenderTexture).
    ///   2. A RawImage on the screen shows that texture.
    ///   3. A small dot shows where the player is.
    /// Hidden during battles.
    /// </summary>
    public class Minimap : MonoBehaviour
    {
        [SerializeField] Tilemap ground;          // the floor of the map (empty = the tilemap called "Ground")
        [SerializeField] Transform player;
        [SerializeField] RawImage mapImage;       // where the map is drawn
        [SerializeField] RectTransform playerDot;   // an arrow that points where Tlahuelli is looking
        [SerializeField] CanvasGroup group;       // to hide it during battles
        [SerializeField] int textureSize = 256;

        Camera mapCamera;
        Bounds bounds;
        PlayerDirection direction;

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
            if (ground == null) { enabled = false; return; }
            if (player != null) direction = player.GetComponent<PlayerDirection>();

            // 1. Size of the map.
            ground.CompressBounds();
            Vector3 min = ground.CellToWorld(ground.cellBounds.min);
            Vector3 max = ground.CellToWorld(ground.cellBounds.max);
            bounds = new Bounds((min + max) / 2f, max - min);

            // 2. A camera above the centre of the map, big enough to see all of it.
            var texture = new RenderTexture(textureSize, Mathf.RoundToInt(textureSize * bounds.size.y / bounds.size.x), 16);
            texture.filterMode = FilterMode.Point;
            mapCamera = new GameObject("MinimapCamera").AddComponent<Camera>();
            mapCamera.orthographic = true;
            mapCamera.orthographicSize = bounds.size.y / 2f;
            mapCamera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -50f);
            mapCamera.clearFlags = CameraClearFlags.SolidColor;
            mapCamera.backgroundColor = Color.black;
            mapCamera.cullingMask = ~LayerMask.GetMask("UI", "TransparentFX");   // no UI and no darkness
            mapCamera.targetTexture = texture;
            mapCamera.depth = -10;

            // 3. Show it on the screen with the same proportions as the map.
            mapImage.texture = texture;
            var rect = mapImage.rectTransform;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, rect.sizeDelta.x * bounds.size.y / bounds.size.x);
        }

        void LateUpdate()
        {
            if (group != null) group.alpha = GameState.Mode == GameMode.Combat ? 0f : 1f;
            if (player == null || playerDot == null) return;

            // Player position from 0 to 1 inside the map -> position inside the picture.
            float x = Mathf.InverseLerp(bounds.min.x, bounds.max.x, player.position.x);
            float y = Mathf.InverseLerp(bounds.min.y, bounds.max.y, player.position.y);
            Vector2 size = mapImage.rectTransform.rect.size;
            playerDot.anchoredPosition = new Vector2((x - 0.5f) * size.x, (y - 0.5f) * size.y);

            // Turn the arrow: the picture points up, so the angle is measured from "up".
            if (direction != null && direction.LastValid != Vector2.zero)
            {
                float angle = Mathf.Atan2(direction.LastValid.y, direction.LastValid.x) * Mathf.Rad2Deg - 90f;
                playerDot.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
    }
}
