using System.Collections;
using UnityEngine;

/// <summary>
/// 2D battle camera: centers on the arena and zooms in smoothly (classic battle-screen feel);
/// when the fight ends it blends back to CameraFollow2D.
/// </summary>
public class BattleCameraController : MonoBehaviour
{
    [Tooltip("Tiles tall that are visible during combat (lower = closer).")]
    public float battleViewHeight = 10f;
    public float smoothTime = 0.35f;

    Camera cam;
    CameraFollow2D follow;
    bool active;
    Vector3 targetPos;
    float targetSize, baseSize;
    Vector3 vel;
    float sizeVel;
    Vector3 smoothPos;                       // smoothed position without shake
    float shakeAmp, shakeDur, shakeTime;

    /// <summary>Quick screen shake (impacts, Seismic Jump). amplitude in tiles.</summary>
    public void Shake(float amplitude, float duration)
    {
        shakeAmp = amplitude;
        shakeDur = Mathf.Max(0.01f, duration);
        shakeTime = shakeDur;
    }

    public void EnterBattle(Vector2 arenaCenter)
    {
        cam = Camera.main;
        if (cam == null) { Debug.LogWarning("No Main Camera found (tag MainCamera)."); return; }
        StopAllCoroutines();
        follow = cam.GetComponent<CameraFollow2D>();
        if (follow != null) follow.enabled = false;

        baseSize = cam.orthographicSize;
        targetSize = battleViewHeight * 0.5f;
        targetPos = new Vector3(arenaCenter.x, arenaCenter.y, cam.transform.position.z);
        vel = Vector3.zero;
        sizeVel = 0f;
        smoothPos = cam.transform.position;
        shakeTime = 0f;
        active = true;
    }

    public void ExitBattle()
    {
        active = false;
        shakeTime = 0f;
        if (cam == null) return;
        StartCoroutine(BlendBack());
    }

    void LateUpdate()
    {
        if (!active || cam == null) return;
        smoothPos = Vector3.SmoothDamp(smoothPos, targetPos, ref vel, smoothTime);
        Vector3 shake = Vector3.zero;
        if (shakeTime > 0f)
        {
            shakeTime -= Time.deltaTime;
            float k = Mathf.Clamp01(shakeTime / shakeDur);
            shake = (Vector3)(Random.insideUnitCircle * (shakeAmp * k * k));
        }
        cam.transform.position = smoothPos + shake;
        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetSize, ref sizeVel, smoothTime);
    }

    IEnumerator BlendBack()
    {
        const float dur = 0.6f;
        Vector3 p0 = cam.transform.position;
        float s0 = cam.orthographicSize;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / dur);
            Vector3 dest = (follow != null && follow.target != null) ? follow.GetDesiredPosition() : p0;
            cam.transform.position = Vector3.Lerp(p0, dest, k);
            cam.orthographicSize = Mathf.Lerp(s0, baseSize, k);
            yield return null;
        }
        cam.orthographicSize = baseSize;
        if (follow != null) follow.enabled = true;
    }
}
