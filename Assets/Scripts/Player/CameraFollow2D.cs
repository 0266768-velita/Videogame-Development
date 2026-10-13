using UnityEngine;

/// <summary>
/// 2D exploration camera: follows the player smoothly and stays inside the map bounds.
/// BattleCameraController turns it off during combat and turns it back on afterwards.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    public Transform target;
    public Vector2 offset;
    [Tooltip("0 = glued to the player. 0.1-0.2 = smooth.")]
    public float smoothTime = 0.12f;

    [Header("Map bounds")]
    public bool useBounds = true;
    public Rect bounds = new Rect(-32f, -20f, 64f, 40f);

    [Header("Pixel art")]
    public bool pixelSnap = false;
    public float pixelsPerUnit = 16f;

    Camera cam;
    Vector3 velocity;

    void Awake() { cam = GetComponent<Camera>(); }
    void OnEnable() { velocity = Vector3.zero; }
    void Start() { SnapToTarget(); }

    public void SnapToTarget()
    {
        if (target == null) return;
        transform.position = GetDesiredPosition();
    }

    public Vector3 GetDesiredPosition()
    {
        if (cam == null) cam = GetComponent<Camera>();
        Vector3 p = target.position + (Vector3)offset;
        p.z = -10f;

        if (useBounds)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            p.x = bounds.width <= halfW * 2f ? bounds.center.x : Mathf.Clamp(p.x, bounds.xMin + halfW, bounds.xMax - halfW);
            p.y = bounds.height <= halfH * 2f ? bounds.center.y : Mathf.Clamp(p.y, bounds.yMin + halfH, bounds.yMax - halfH);
        }
        return p;
    }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 desired = GetDesiredPosition();
        Vector3 pos = smoothTime > 0f ? Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime) : desired;

        if (pixelSnap)
        {
            float u = 1f / pixelsPerUnit;
            pos.x = Mathf.Round(pos.x / u) * u;
            pos.y = Mathf.Round(pos.y / u) * u;
        }
        transform.position = pos;
    }
}
