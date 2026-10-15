using UnityEngine;

/// <summary>
/// Locks the camera to 16:9 (black bars if the window has another ratio) and keeps it orthographic (2D),
/// with a view height measured in tiles (16 px per unit).
/// Put it on the Main Camera of EVERY scene.
/// </summary>
[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(-100)]
public class AspectRatioLock : MonoBehaviour
{
    public float targetAspect = 16f / 9f;
    [Tooltip("How many tiles tall the view is. HIGHER = more zoomed out. 12 is about classic Zelda/Pokemon at 16:9.")]
    public float viewHeightInTiles = 12f;
    public bool applyOrthographic = true;

    static Camera letterboxCam;
    Camera cam;
    int lastW, lastH;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (applyOrthographic)
        {
            cam.orthographic = true;
            cam.orthographicSize = viewHeightInTiles * 0.5f;
        }
        EnsureLetterboxCamera();
        Apply();
    }

    void Update()
    {
        if (Screen.width != lastW || Screen.height != lastH) Apply();
    }

    void Apply()
    {
        lastW = Screen.width; lastH = Screen.height;
        if (lastW <= 0 || lastH <= 0) return;

        float window = (float)lastW / lastH;
        Rect r;
        if (window > targetAspect)          // window is wider: bars on the sides
        {
            float w = targetAspect / window;
            r = new Rect((1f - w) * 0.5f, 0f, w, 1f);
        }
        else                                // window is taller: bars on top and bottom
        {
            float h = window / targetAspect;
            r = new Rect(0f, (1f - h) * 0.5f, 1f, h);
        }
        cam.rect = r;
    }

    void EnsureLetterboxCamera()
    {
        if (letterboxCam != null) return;
        var go = new GameObject("LetterboxCamera");
        letterboxCam = go.AddComponent<Camera>();
        letterboxCam.clearFlags = CameraClearFlags.SolidColor;
        letterboxCam.backgroundColor = Color.black;
        letterboxCam.cullingMask = 0;
        letterboxCam.depth = cam.depth - 100f;
        letterboxCam.useOcclusionCulling = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { letterboxCam = null; }
}
