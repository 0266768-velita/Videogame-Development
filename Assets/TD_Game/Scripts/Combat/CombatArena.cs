using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Sets up the battle on the same map: places the player on the left and the enemies on the right,
    /// and moves and zooms the camera onto the fight. Restores the camera afterwards.
    /// </summary>
    public class CombatArena : MonoBehaviour
    {
        [Header("Placement (tiles)")]
        [Tooltip("Horizontal distance between the player and the first enemy.")]
        [SerializeField, Min(1f)] float battleDistance = 5f;
        [Tooltip("Vertical gap between extra enemies.")]
        [SerializeField, Min(0.5f)] float enemySpacing = 1.6f;

        [Header("Camera")]
        [SerializeField] CameraFollow cameraFollow;
        [Tooltip("2.8125 = integer x3 pixel scale at 1080p with PPU 64 (exploration uses 4.21875, x2).")]
        [SerializeField, Min(0.5f)] float combatOrthographicSize = 2.8125f;
        [SerializeField, Min(0f)] float zoomTime = 0.4f;

        Transform focus;
        Transform previousTarget;
        float explorationSize;
        Coroutine zoomRoutine;

        Coroutine hitStopRoutine;

        void Awake()
        {
            focus = new GameObject("CombatFocus").transform;
            focus.SetParent(transform, false);
        }

        void OnEnable()
        {
            CombatFeed.Shake += OnShake;
            CombatFeed.HitStop += OnHitStop;
        }

        void OnDisable()
        {
            CombatFeed.Shake -= OnShake;
            CombatFeed.HitStop -= OnHitStop;
            Time.timeScale = 1f;
        }

        void OnShake(float amplitude, float duration)
        {
            if (cameraFollow != null) cameraFollow.Shake(amplitude, duration);
        }

        void OnHitStop(float seconds)
        {
            if (hitStopRoutine != null) StopCoroutine(hitStopRoutine);
            hitStopRoutine = StartCoroutine(HitStopRoutine(seconds));
        }

        // Freezes the game for a moment so heavy hits feel heavy.
        IEnumerator HitStopRoutine(float seconds)
        {
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(seconds);
            Time.timeScale = 1f;
            hitStopRoutine = null;
        }

        public void Enter(PlayerCombatant player, IReadOnlyList<CombatEnemy> enemies)
        {
            Vector3 lead = enemies[0].transform.position;
            Vector3 center = (player.transform.position + lead) * 0.5f;
            float half = battleDistance * 0.5f;

            player.Teleport(center + Vector3.left * half);
            player.Direction.Face(Vector2.right);

            for (int i = 0; i < enemies.Count; i++)
            {
                int row = (i + 1) / 2;                     // 0, 1, 1, 2, 2...
                float side = i % 2 == 1 ? 1f : -1f;        // extra enemies alternate above and below
                Vector3 offset = new Vector3(half + 0.5f * row, side * row * enemySpacing, 0f);
                enemies[i].transform.position = center + offset;
            }

            if (cameraFollow == null) return;
            var cam = cameraFollow.GetComponent<Camera>();
            explorationSize = cam.orthographicSize;
            previousTarget = cameraFollow.Target;
            focus.position = center;
            cameraFollow.Follow(focus);
            Zoom(cam, combatOrthographicSize);
        }

        /// <summary>Gives the camera back to the player. snap = jump instead of gliding (respawn).</summary>
        public void Exit(bool snap)
        {
            if (cameraFollow == null) return;
            var cam = cameraFollow.GetComponent<Camera>();
            cameraFollow.Follow(previousTarget);
            if (snap)
            {
                if (zoomRoutine != null) StopCoroutine(zoomRoutine);
                cam.orthographicSize = explorationSize;
                cameraFollow.SnapToTarget();
            }
            else Zoom(cam, explorationSize);
        }

        void Zoom(Camera cam, float size)
        {
            if (zoomRoutine != null) StopCoroutine(zoomRoutine);
            zoomRoutine = StartCoroutine(ZoomRoutine(cam, size));
        }

        IEnumerator ZoomRoutine(Camera cam, float size)
        {
            float start = cam.orthographicSize;
            for (float t = 0f; t < zoomTime; t += Time.deltaTime)
            {
                cam.orthographicSize = Mathf.Lerp(start, size, Mathf.SmoothStep(0f, 1f, t / zoomTime));
                yield return null;
            }
            cam.orthographicSize = size;
            zoomRoutine = null;
        }
    }
}
