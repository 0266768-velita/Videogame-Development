using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TD_Game
{
    /// <summary>
    /// Water and ravines are not walls for Tlahuelli: walking into them (or landing in them after a short jump)
    /// makes him fall. Each fall has its own animation and sound:
    ///   - Ravine: he slips, spins and shrinks into the dark, dust rises from the edge, a far thud.
    ///   - Water: a splash, he sinks with rings and drops, the water closes over him.
    /// Then the screen fades and he climbs out at the last firm ground he stood on (never stuck), with a little
    /// less life (a fall never kills) and a moment of blinking.
    /// The tilemaps "Water" and "Pits" (layer Hazard) say where the hazards are; a tile only counts when the feet are
    /// well inside it, so touching the shore is safe.
    /// </summary>
    [RequireComponent(typeof(PlayerCombatant))]
    public class PlayerHazards : MonoBehaviour
    {
        enum Hazard { None, Water, Pit }

        [SerializeField] Tilemap water;
        [SerializeField] Tilemap pits;
        [SerializeField] Transform visual;
        [SerializeField] Transform shadow;

        [Tooltip("How far (tiles) the feet must be inside water or a ravine to fall. Touching the shore is safe.")]
        [SerializeField, Range(0f, 0.5f)] float edgeMargin = 0.3f;
        [Tooltip("The last safe spot must be at least this far (tiles) from any water or ravine.")]
        [SerializeField, Min(0.2f)] float safeDistance = 0.8f;
        [Tooltip("Part of the maximum life lost in a fall (a fall never kills).")]
        [SerializeField, Range(0f, 0.5f)] float damageFraction = 0.08f;
        [SerializeField, Min(0.1f)] float blinkSeconds = 1.2f;

        PlayerCombatant player;
        PlayerJump jump;
        Rigidbody2D body;
        SpriteRenderer visualSprite;
        Vector3 lastSafe, visualPosition, visualScale = Vector3.one;
        bool falling;
        float graceUntil;

        /// <summary>Raised when Tlahuelli falls (true = into water). Used by tutorials and statistics.</summary>
        public static event System.Action<bool> Fell;

        public bool IsFalling => falling;

        void Awake()
        {
            player = GetComponent<PlayerCombatant>();
            jump = GetComponent<PlayerJump>();
            body = GetComponent<Rigidbody2D>();
            if (visual != null) { visualSprite = visual.GetComponent<SpriteRenderer>(); visualPosition = visual.localPosition; visualScale = visual.localScale; }
            if (water == null || pits == null)
                foreach (var map in FindObjectsByType<Tilemap>())
                {
                    if (water == null && map.name == "Water") water = map;
                    if (pits == null && map.name == "Pits") pits = map;
                }
        }

        void Start() => lastSafe = transform.position;

        /// <summary>After a teleport (doors, battles, loading) the new spot is the safe one.</summary>
        public void ResetSafeSpot()
        {
            lastSafe = transform.position;
            graceUntil = Time.time + 0.3f;
        }

        void FixedUpdate()
        {
            if (falling || !GameState.IsExploring || (jump != null && jump.IsAirborne) || Time.time < graceUntil) return;
            Vector2 feet = body.position;
            var hazard = HazardAt(feet);
            if (hazard != Hazard.None) StartCoroutine(Fall(hazard, feet));
            else if (IsSafe(feet)) lastSafe = feet;
        }

        Hazard HazardAt(Vector2 feet)
        {
            if (DeepInside(water, feet)) return Hazard.Water;
            if (DeepInside(pits, feet)) return Hazard.Pit;
            return Hazard.None;
        }

        bool DeepInside(Tilemap map, Vector2 p)
        {
            if (map == null) return false;
            return Has(map, p) && Has(map, p + new Vector2(edgeMargin, 0f)) && Has(map, p + new Vector2(-edgeMargin, 0f))
                   && Has(map, p + new Vector2(0f, edgeMargin)) && Has(map, p + new Vector2(0f, -edgeMargin));
        }

        static bool Has(Tilemap map, Vector2 p) => map.HasTile(map.WorldToCell(p));

        bool IsSafe(Vector2 p)
        {
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                var probe = p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * safeDistance;
                if ((water != null && Has(water, probe)) || (pits != null && Has(pits, probe))) return false;
            }
            return (water == null || !Has(water, p)) && (pits == null || !Has(pits, p));
        }

        IEnumerator Fall(Hazard hazard, Vector2 where)
        {
            falling = true;
            var previous = GameState.Mode;
            GameState.SetMode(GameMode.Cutscene);
            body.linearVelocity = Vector2.zero;
            bool inWater = hazard == Hazard.Water;
            Fell?.Invoke(inWater);

            // Slide a little towards the middle of the hazard, so he does not fall "on the shore".
            Vector2 push = (Vector2)transform.position - (Vector2)lastSafe;
            if (push.sqrMagnitude > 0.0001f) where += push.normalized * 0.35f;
            player.Teleport(where);

            if (shadow != null) shadow.gameObject.SetActive(false);
            if (inWater) yield return SinkInWater(where);
            else yield return FallIntoPit(where);

            // A fall hurts a little, but never kills.
            float damage = Mathf.Min(player.Health.Max * damageFraction, player.Health.Current - 1f);
            if (damage > 0f) player.Health.TakeDamage(damage);

            yield return ScreenFade.To(1f, 0.25f);
            RestoreVisual();
            player.Teleport(lastSafe);
            var cam = FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.SnapToTarget();
            yield return ScreenFade.To(0f, 0.3f);

            ContextTutorial.ShowOnce("first_fall", inWater
                ? "Tlahuelli cannot swim in the rivers of Mictlán. Cross by the bridges, or JUMP ({Jump}) over the narrow parts: sprint ({Sprint}) first to jump farther."
                : "Falling into a ravine costs life. Narrow ravines can be JUMPED ({Jump}); sprint ({Sprint}) before jumping to reach farther.");

            GameState.SetMode(previous == GameMode.Cutscene ? GameMode.Exploration : previous);
            graceUntil = Time.time + 0.3f;
            falling = false;
            yield return Blink();
        }

        IEnumerator FallIntoPit(Vector2 where)
        {
            GameAudio.PlaySfx("fall_pit", 1f, 0.03f);
            Vfx.Dust(where, 8, MexicaPalette.Ash, 1f);
            Vfx.Burst(where, 6, new Color(0.45f, 0.36f, 0.26f), new Vector2(0.5f, 1.4f), 10f, 0.6f, 0.7f, toGround: true, upward: 0f);
            Vector3 startScale = visual.localScale;
            Color startColor = visualSprite != null ? visualSprite.color : Color.white;
            // A short wobble on the edge...
            for (float t = 0f; t < 0.18f; t += Time.deltaTime)
            {
                visual.localPosition = visualPosition + new Vector3(Mathf.Sin(t * 70f) * 0.05f, 0f, 0f);
                yield return null;
            }
            // ...then down into the dark, spinning and shrinking.
            const float time = 0.75f;
            for (float t = 0f; t < time; t += Time.deltaTime)
            {
                float k = t / time;
                float ease = k * k;
                visual.localScale = startScale * Mathf.Lerp(1f, 0.08f, ease);
                visual.localRotation = Quaternion.Euler(0f, 0f, 540f * ease);
                visual.localPosition = visualPosition + new Vector3(0f, -0.35f * ease, 0f);
                if (visualSprite != null) visualSprite.color = Color.Lerp(startColor, Color.black, ease * 1.2f);
                yield return null;
            }
            visual.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.25f);
            GameAudio.PlaySfx("land", 0.35f, 0.02f);   // a far thud at the bottom
            CombatFeed.RequestShake(0.05f, 0.2f);
            yield return new WaitForSeconds(0.2f);
        }

        IEnumerator SinkInWater(Vector2 where)
        {
            GameAudio.PlaySfx("splash", 1f, 0.05f);
            var foam = new Color(0.8f, 0.9f, 0.95f);
            var deep = new Color(0.3f, 0.55f, 0.8f);
            Vfx.Ring(where, foam, 0.1f, 1.1f, 0.5f, onGround: true, squashY: 0.45f);
            Vfx.Burst(where + Vector2.up * 0.2f, 12, foam, new Vector2(1.5f, 3.2f), 12f, 0.7f, 0.6f, toGround: true, upward: 0.8f);
            Vector3 startScale = visual.localScale;
            Color startColor = visualSprite != null ? visualSprite.color : Color.white;
            const float time = 0.85f;
            float nextRing = 0.2f;
            for (float t = 0f; t < time; t += Time.deltaTime)
            {
                float k = t / time;
                // Struggles (side to side), then sinks: the body goes down and fades in the blue.
                visual.localPosition = visualPosition + new Vector3(Mathf.Sin(t * 22f) * 0.06f * (1f - k), -0.55f * k, 0f);
                visual.localScale = new Vector3(startScale.x * (1f + 0.15f * Mathf.Sin(t * 18f) * (1f - k)), startScale.y * Mathf.Lerp(1f, 0.6f, k), 1f);
                if (visualSprite != null)
                {
                    var c = Color.Lerp(startColor, deep, k);
                    c.a = Mathf.Lerp(1f, 0f, Mathf.SmoothStep(0.35f, 1f, k));
                    visualSprite.color = c;
                }
                if (t >= nextRing)
                {
                    Vfx.Ring(where, deep, 0.2f, 0.9f, 0.45f, onGround: true, squashY: 0.45f);
                    nextRing += 0.25f;
                }
                yield return null;
            }
            visual.gameObject.SetActive(false);
            // Bubbles rise where he went down.
            Vfx.Wisps(where, 5, foam, 0.6f, 0.25f);
            yield return new WaitForSeconds(0.35f);
        }

        void RestoreVisual()
        {
            if (visual == null) return;
            visual.gameObject.SetActive(true);
            visual.localPosition = visualPosition;
            visual.localRotation = Quaternion.identity;
            visual.localScale = visualScale;
            if (visualSprite != null) visualSprite.color = Color.white;
            if (shadow != null) shadow.gameObject.SetActive(true);
        }

        IEnumerator Blink()
        {
            if (visualSprite == null) yield break;
            for (float t = 0f; t < blinkSeconds; t += Time.deltaTime)
            {
                var c = visualSprite.color;
                c.a = Mathf.Repeat(t, 0.16f) < 0.08f ? 0.35f : 1f;
                visualSprite.color = c;
                yield return null;
            }
            var end = visualSprite.color;
            end.a = 1f;
            visualSprite.color = end;
        }
    }
}
