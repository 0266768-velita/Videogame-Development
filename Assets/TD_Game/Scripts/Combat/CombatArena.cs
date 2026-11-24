using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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
        [Tooltip("How far (tiles) the fight may move to stand on open ground: no trees, water, walls or props under anyone.")]
        [SerializeField, Min(0f)] float clearSearchRadius = 6f;

        [Header("Drawing")]
        [Tooltip("While fighting, every fighter is drawn at this order (a SortingGroup): above the map, its trees and stalls " +
                 "and the darkness of the night (500), so nothing can hide Tlahuelli or the enemies. Front effects use 520.")]
        [SerializeField] int fighterOrder = FighterOrder;

        public const int FighterOrder = 510;

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
        readonly List<ActorFx> actors = new List<ActorFx>();
        MacuahuitlRig weapon;
        readonly List<(SortingGroup group, bool added, int order)> raised = new List<(SortingGroup, bool, int)>();

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
            // Big enemies (giant, boss) stand a bit farther, and the camera opens a little more for them.
            float bigness = Mathf.Max(0f, enemies[0].Data.bodyRadius - 0.5f);
            float half = battleDistance * 0.5f + bigness * 0.5f;
            float viewSize = combatOrthographicSize + bigness;

            // The camera cannot go past the edge of the map: move the whole fight inside what it can show,
            // so the player (and the dodge step to the left) never leaves the screen.
            if (cameraFollow != null) center = cameraFollow.ClampForView(center, viewSize);
            // Nobody fights on top of a tree, a stall or the river: move the fight to the nearest open ground.
            center = ClearCenter(center, half, enemies.Count);
            if (cameraFollow != null) center = cameraFollow.ClampForView(center, viewSize);

            player.Teleport(center + Vector3.left * half);
            player.Direction.Face(Vector2.right);

            for (int i = 0; i < enemies.Count; i++)
            {
                enemies[i].transform.position = center + EnemyOffset(i, half);
                // The enemy pictures look to the right: flip them so they face Tlahuelli (on the left).
                if (enemies[i].Sprite != null) enemies[i].Sprite.flipX = true;
            }

            // Everyone takes a fighting stance: a little pop, dust, and breathing while the battle lasts.
            // Tlahuelli draws the animated Macuahuitl (MacuahuitlRig).
            actors.Clear();
            actors.Add(ActorFx.Of(player));
            weapon = MacuahuitlRig.Of(player);
            weapon.Show(true);
            foreach (var e in enemies) actors.Add(ActorFx.Of(e));
            raised.Clear();
            Raise(player);
            foreach (var e in enemies) Raise(e);
            foreach (var fx in actors)
            {
                fx.Breathing = true;
                fx.Squash(new Vector2(0.8f, 1.25f));
                Vfx.Dust(fx.transform.position, 3, MexicaPalette.Ash, 0.7f);
            }

            if (cameraFollow == null) return;
            var cam = cameraFollow.GetComponent<Camera>();
            explorationSize = cam.orthographicSize;
            previousTarget = cameraFollow.Target;
            // The camera looks a bit to the right of the fight, so both fighters sit left of the command menu.
            focus.position = center + Vector3.right * 1.5f;
            cameraFollow.Follow(focus);
            Zoom(cam, viewSize);
        }

        /// <summary>Gives the camera back to the player. snap = jump instead of gliding (respawn).</summary>
        public void Exit(bool snap)
        {
            foreach (var fx in actors)
                if (fx != null) { fx.Breathing = false; fx.Restore(); }
            if (weapon != null) weapon.Show(false);
            foreach (var (group, added, order) in raised)
            {
                if (group == null) continue;
                if (added) Destroy(group); else group.sortingOrder = order;
            }
            raised.Clear();
            actors.Clear();

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

        Vector3 EnemyOffset(int i, float half)
        {
            int row = (i + 1) / 2;                     // 0, 1, 1, 2, 2...
            float side = i % 2 == 1 ? 1f : -1f;        // extra enemies alternate above and below
            return new Vector3(half + 0.5f * row, side * row * enemySpacing, 0f);
        }

        /// <summary>The nearest centre (spiral search) where the player and every enemy stand on open ground.</summary>
        Vector3 ClearCenter(Vector3 center, float half, int enemyCount)
        {
            const float step = 0.5f;
            int rings = Mathf.CeilToInt(clearSearchRadius / step);
            for (int k = 0; k <= rings; k++)
                for (int dx = -k; dx <= k; dx++)
                    for (int dy = -k; dy <= k; dy++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != k) continue;
                        var candidate = center + new Vector3(dx * step, dy * step, 0f);
                        bool clear = IsOpen(candidate + Vector3.left * half);
                        for (int i = 0; clear && i < enemyCount; i++) clear = IsOpen(candidate + EnemyOffset(i, half));
                        if (clear) return candidate;
                    }
            return center;
        }

        /// <summary>True when no solid collider (tilemap walls, props, the shop) is there. Characters do not count.</summary>
        static bool IsOpen(Vector3 spot)
        {
            foreach (var hit in Physics2D.OverlapCircleAll(spot, 0.45f))
            {
                if (hit.isTrigger) continue;
                if (hit.GetComponentInParent<PlayerCombatant>() != null || hit.GetComponentInParent<CombatEnemy>() != null) continue;
                return false;
            }
            return true;
        }

        /// <summary>Draws a fighter (with its shadow, torch and weapon) above the map and the darkness.</summary>
        void Raise(Component fighter)
        {
            var group = fighter.GetComponent<SortingGroup>();
            bool added = group == null;
            if (added) group = fighter.gameObject.AddComponent<SortingGroup>();
            raised.Add((group, added, group.sortingOrder));
            group.sortingOrder = fighterOrder;
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
