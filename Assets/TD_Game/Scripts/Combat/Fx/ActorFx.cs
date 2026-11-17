using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Procedural animation of a combatant's sprite (the "Visual" child), on top of its normal look:
    /// squash and stretch with a springy wobble, height (jumps), lean, knock-back and breathing.
    /// It only touches the Visual, never the root, so colliders and the shadow stay on the ground.
    /// These are placeholders with "game feel" until the Aseprite animations arrive; the Animator can
    /// replace them later without changing the combat code.
    /// </summary>
    public class ActorFx : MonoBehaviour
    {
        [Tooltip("The sprite holder that gets animated. Empty = child named 'Visual' or the first SpriteRenderer.")]
        [SerializeField] Transform visual;
        [Tooltip("Optional shadow on the ground; it shrinks with height. Empty = child named 'Shadow'.")]
        [SerializeField] Transform shadow;

        [Header("Spring (squash & stretch)")]
        [SerializeField, Min(1f)] float stiffness = 320f;
        [SerializeField, Min(0f)] float damping = 13f;

        [Header("Breathing during combat")]
        [SerializeField, Range(0f, 0.1f)] float breathAmount = 0.03f;
        [SerializeField, Min(0.1f)] float breathSpeed = 2.4f;

        Vector3 basePosition, baseScale, shadowBaseScale;
        float bodyCenterY;   // height of the body centre above the pivot (feet), so spins turn around the middle
        Quaternion baseRotation;
        Vector2 squash = Vector2.one, squashVelocity;
        Vector2 kick, kickVelocity;
        float breathPhase;
        bool wasActive;

        /// <summary>Rest pose the spring returns to ((1,1) = normal). Use it to hold a crouch or a charge.</summary>
        public Vector2 Pose { get; set; } = Vector2.one;
        /// <summary>Visual height above the ground, in tiles.</summary>
        public float Height { get; set; }
        /// <summary>Tilt in degrees (positive = counter-clockwise).</summary>
        public float Lean { get; set; }
        /// <summary>Small random tremble in tiles (charging, fear).</summary>
        public float Tremble { get; set; }
        public bool Breathing { get; set; }
        public Transform Visual => visual;

        public static ActorFx Of(Component c)
        {
            if (c == null) return null;
            var fx = c.GetComponent<ActorFx>();
            return fx != null ? fx : c.gameObject.AddComponent<ActorFx>();
        }

        void Awake()
        {
            if (visual == null) visual = transform.Find("Visual");
            if (visual == null)
            {
                var sr = GetComponentInChildren<SpriteRenderer>();
                if (sr != null && sr.transform != transform) visual = sr.transform;
            }
            if (shadow == null) shadow = transform.Find("Shadow");
            if (visual != null)
            {
                basePosition = visual.localPosition;
                baseScale = visual.localScale;
                baseRotation = visual.localRotation;
                var sr = visual.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null) bodyCenterY = sr.sprite.bounds.center.y;
            }
            if (shadow != null) shadowBaseScale = shadow.localScale;
            breathPhase = Random.value * 10f;
        }

        /// <summary>Instant squash (x wide, y tall) that springs back to the pose with a wobble.</summary>
        public void Squash(Vector2 scale) => squash = scale;

        /// <summary>Pushes the sprite by an offset (tiles) that springs back: hit reactions.</summary>
        public void Kick(Vector2 offset) => kick += offset;

        bool Active =>
            Breathing || Height > 0.0001f || Mathf.Abs(Lean) > 0.01f || Tremble > 0f ||
            (squash - Vector2.one).sqrMagnitude > 0.00001f || squashVelocity.sqrMagnitude > 0.0001f ||
            (Pose - Vector2.one).sqrMagnitude > 0.00001f || kick.sqrMagnitude > 0.00001f;

        void LateUpdate()
        {
            if (visual == null) return;
            float dt = Time.deltaTime;

            // Damped springs: they overshoot a little, which is what makes it feel alive.
            squashVelocity += (Pose - squash) * stiffness * dt;
            squashVelocity *= Mathf.Exp(-damping * dt);
            squash += squashVelocity * dt;
            kickVelocity += -kick * stiffness * 0.6f * dt;
            kickVelocity *= Mathf.Exp(-damping * dt);
            kick += kickVelocity * dt;

            bool active = Active;
            if (!active)
            {
                // Hand the Visual back untouched (exploration jump, Animator...).
                if (wasActive) Restore();
                wasActive = false;
                return;
            }
            wasActive = true;

            float breath = Breathing ? 1f + breathAmount * Mathf.Sin((Time.time + breathPhase) * breathSpeed) : 1f;
            Vector2 s = squash;
            visual.localScale = new Vector3(baseScale.x * s.x, baseScale.y * s.y * breath, baseScale.z);

            Vector3 tremble = Tremble > 0f ? (Vector3)(Random.insideUnitCircle * Tremble) : Vector3.zero;
            Quaternion lean = Quaternion.Euler(0f, 0f, Lean);
            // Rotate around the body centre instead of the feet: shift by (c - R·c).
            Vector3 centre = new Vector3(0f, bodyCenterY * visual.localScale.y, 0f);
            Vector3 pivotFix = centre - lean * centre;
            visual.localPosition = basePosition + new Vector3(kick.x, kick.y + Height, 0f) + tremble + pivotFix;
            visual.localRotation = baseRotation * lean;

            if (shadow != null)
                shadow.localScale = shadowBaseScale * Mathf.Lerp(1f, 0.45f, Mathf.Clamp01(Height / 2.5f));
        }

        /// <summary>Back to the exact original transform.</summary>
        public void Restore()
        {
            squash = Pose = Vector2.one;
            squashVelocity = kick = kickVelocity = Vector2.zero;
            Height = Lean = Tremble = 0f;
            if (visual != null)
            {
                visual.localPosition = basePosition;
                visual.localScale = baseScale;
                visual.localRotation = baseRotation;
            }
            if (shadow != null) shadow.localScale = shadowBaseScale;
        }
    }
}
