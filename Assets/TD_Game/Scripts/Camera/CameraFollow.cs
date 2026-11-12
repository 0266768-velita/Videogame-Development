using UnityEngine;
using UnityEngine.Tilemaps;

namespace TD_Game
{
    /// <summary>
    /// Orthographic top-down camera that smoothly follows a target and stays inside the bounds of a Tilemap.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] Transform target;
        [SerializeField] Vector2 offset;
        [Tooltip("Time (seconds) to catch up with the target. 0 = no smoothing.")]
        [SerializeField, Min(0f)] float smoothTime = 0.15f;

        [Header("Bounds")]
        [Tooltip("The camera never shows anything outside this Tilemap (usually the Ground layer).")]
        [SerializeField] Tilemap boundsTilemap;

        [Header("Pixel art")]
        [Tooltip("Rounds the camera position to whole texels so the pixel art does not shimmer while moving.")]
        [SerializeField] bool snapToPixelGrid = true;
        [SerializeField, Min(1)] int pixelsPerUnit = 64;

        Camera cam;
        Vector3 velocity;
        Vector3 smoothedPosition;
        Bounds worldBounds;
        bool hasBounds;

        public Transform Target
        {
            get => target;
            set { target = value; SnapToTarget(); }
        }

        void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            smoothedPosition = transform.position;
            RefreshBounds();
        }

        void Start() => SnapToTarget();

        void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = Clamp(DesiredPosition());
            smoothedPosition = smoothTime > 0f
                ? Vector3.SmoothDamp(smoothedPosition, desired, ref velocity, smoothTime)
                : desired;
            smoothedPosition = Clamp(smoothedPosition);
            transform.position = snapToPixelGrid ? SnapToPixels(smoothedPosition) : smoothedPosition;
        }

        /// <summary>Changes the target and glides to it with the usual smoothing (combat focus).</summary>
        public void Follow(Transform newTarget) => target = newTarget;

        /// <summary>Jumps straight to the target (scene start, teleports, respawns).</summary>
        public void SnapToTarget()
        {
            if (target == null) return;
            velocity = Vector3.zero;
            smoothedPosition = Clamp(DesiredPosition());
            transform.position = snapToPixelGrid ? SnapToPixels(smoothedPosition) : smoothedPosition;
        }

        /// <summary>Recomputes the bounds after the Tilemap changes size.</summary>
        public void RefreshBounds()
        {
            hasBounds = false;
            if (boundsTilemap == null) return;

            boundsTilemap.CompressBounds();
            BoundsInt cells = boundsTilemap.cellBounds;
            if (cells.size.x <= 0 || cells.size.y <= 0) return;

            Vector3 min = boundsTilemap.CellToWorld(cells.min);
            Vector3 max = boundsTilemap.CellToWorld(cells.max);
            worldBounds = new Bounds((min + max) * 0.5f, max - min);
            hasBounds = true;
        }

        Vector3 DesiredPosition()
        {
            Vector3 p = target.position;
            return new Vector3(p.x + offset.x, p.y + offset.y, transform.position.z);
        }

        Vector3 Clamp(Vector3 p)
        {
            if (!hasBounds) return p;

            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            p.x = ClampAxis(p.x, worldBounds.min.x, worldBounds.max.x, halfWidth);
            p.y = ClampAxis(p.y, worldBounds.min.y, worldBounds.max.y, halfHeight);
            return p;
        }

        // If the map is smaller than the view on this axis, the camera stays centred on the map.
        static float ClampAxis(float value, float min, float max, float halfView)
        {
            if (max - min <= halfView * 2f) return (min + max) * 0.5f;
            return Mathf.Clamp(value, min + halfView, max - halfView);
        }

        Vector3 SnapToPixels(Vector3 p)
        {
            float unit = 1f / pixelsPerUnit;
            return new Vector3(Mathf.Round(p.x / unit) * unit, Mathf.Round(p.y / unit) * unit, p.z);
        }

        void OnDrawGizmosSelected()
        {
            if (!hasBounds) RefreshBounds();
            if (!hasBounds) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
        }
    }
}
