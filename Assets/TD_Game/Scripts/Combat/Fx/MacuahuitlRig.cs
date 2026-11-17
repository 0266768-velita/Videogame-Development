using System;
using System.Collections;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Tlahuelli's Macuahuitl in combat: a separate sprite held at the hand that swings, glows and leaves
    /// a trail of ghost blades. While it is shown, the character uses an "unarmed" copy of its frame so the
    /// club drawn in the sprite does not appear twice. Outside combat it is hidden and the art is untouched.
    /// It follows the body animation (ActorFx: height, squash, lean) but is not squashed itself.
    /// </summary>
    [DefaultExecutionOrder(100)]   // after ActorFx, so it reads the body pose of this frame
    public class MacuahuitlRig : MonoBehaviour
    {
        [Tooltip("Weapon art (blade up, pivot on the grip). Empty = the pixel-art Macuahuitl generated in code.")]
        [SerializeField] Sprite weaponSprite;
        [Tooltip("Hand position, as a fraction of the body sprite size, from its pivot (feet).")]
        [SerializeField] Vector2 grip = new Vector2(0.3f, 0.36f);
        [Tooltip("Weapon length relative to the body height (the art's own size is used for Tlahuelli's 16 px sprite).")]
        [SerializeField, Min(0.1f)] float sizeToBody = 0.65f;
        [Tooltip("Guard stance angle while idle (0 = up, positive = back).")]
        [SerializeField] float guardAngle = -35f;
        [SerializeField, Min(0f)] float swayDegrees = 4f;
        [SerializeField] Color trailColor = new Color(0.85f, 0.8f, 0.7f, 1f);

        [Header("Combat pose (optional)")]
        [Tooltip("Frame shown during combat (e.g. the side view facing the enemies). Empty = keep the current frame.")]
        [SerializeField] Sprite combatPose;
        [Tooltip("Hand position for the combat pose (fraction of the sprite size from its pivot).")]
        [SerializeField] Vector2 combatGrip = new Vector2(0.02f, 0.37f);

        SpriteRenderer body, weapon;
        Transform visual;
        Sprite armedFrame;     // last frame with the club drawn in it
        Sprite outOfCombatFrame;
        float angle, angleVelocity;
        int driven;            // > 0 while a swing or a raise controls the angle
        float hold = float.NaN; // angle held after a raise (NaN = return to the guard)

        public bool IsShown => weapon != null && weapon.enabled;
        public float Angle => angle;

        /// <summary>World position of the blade tip (for sparks and glows).</summary>
        public Vector3 Tip => weapon != null ? weapon.transform.TransformPoint(new Vector3(0f, 0.85f, 0f)) : transform.position;

        /// <summary>The rig if this character has its weapon out, otherwise null.</summary>
        public static MacuahuitlRig Active(Component c)
        {
            var rig = c != null ? c.GetComponent<MacuahuitlRig>() : null;
            return rig != null && rig.IsShown ? rig : null;
        }

        public static MacuahuitlRig Of(Component c)
        {
            if (c == null) return null;
            var rig = c.GetComponent<MacuahuitlRig>();
            return rig != null ? rig : c.gameObject.AddComponent<MacuahuitlRig>();
        }

        void Awake()
        {
            visual = transform.Find("Visual");
            body = visual != null ? visual.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
            var go = new GameObject("Macuahuitl");
            go.transform.SetParent(transform, false);
            weapon = go.AddComponent<SpriteRenderer>();
            weapon.sprite = weaponSprite != null ? weaponSprite : WeaponSprites.Macuahuitl;
            weapon.enabled = false;
            angle = guardAngle;
        }

        /// <summary>Takes the weapon out (combat start) or puts it away (combat end).</summary>
        public void Show(bool show)
        {
            if (weapon == null) return;
            if (show == weapon.enabled) return;
            weapon.enabled = show;
            hold = float.NaN;
            if (show && body != null)
            {
                outOfCombatFrame = body.sprite;
                if (combatPose != null) body.sprite = combatPose;
            }
            if (!show && body != null && outOfCombatFrame != null) body.sprite = outOfCombatFrame;   // give the original art back
            armedFrame = null;
            if (show) { angle = guardAngle; angleVelocity = 0f; }
            SetOtherWeapons(!show);
        }

        /// <summary>
        /// The player may also carry the simpler attack-only Macuahuitl (MacuahuitlSwing, made by MainMapBuilder).
        /// While this rig is out it is switched off, so the weapon never appears twice; it comes back afterwards.
        /// </summary>
        void SetOtherWeapons(bool enabled)
        {
            foreach (var other in GetComponentsInChildren<MacuahuitlSwing>(true))
            {
                other.enabled = enabled;
                var sprite = other.GetComponent<SpriteRenderer>();
                if (sprite != null && !enabled) sprite.enabled = false;   // when re-enabled it decides by itself
            }
        }

        // ---------------- Moves ----------------

        /// <summary>Raises (or lowers) the weapon to an angle and keeps it there until Release or a swing.</summary>
        public IEnumerator RaiseTo(float target, float time)
        {
            driven++;
            float from = angle;
            for (float t = 0f; t < time; t += Time.deltaTime)
            {
                float k = t / time;
                angle = Mathf.Lerp(from, target, 1f - (1f - k) * (1f - k));   // ease-out
                yield return null;
            }
            angle = target;
            hold = target;
            driven--;
        }

        /// <summary>Lets the weapon spring back to the guard stance.</summary>
        public void Release() => hold = float.NaN;

        /// <summary>
        /// A blow from the current angle (ideally already at swing.windAngle) to swing.endAngle,
        /// accelerating, with a ghost-blade trail. onImpact is called when the blade hits.
        /// </summary>
        public IEnumerator Strike(MacuahuitlTechnique swing, Action onImpact)
        {
            driven++;
            hold = float.NaN;
            var fx = ActorFx.Of(this);
            float from = angle, to = swing.endAngle;
            bool hit = false;
            if (swing.spin) fx.Squash(new Vector2(1.15f, 0.9f));

            for (float t = 0f; t < swing.swingTime; t += Time.deltaTime)
            {
                float k = t / swing.swingTime;
                angle = Mathf.Lerp(from, to, k * k);                         // ease-in: the blow accelerates
                if (swing.spin) fx.Lean = -360f * k;
                if (swing.hop) fx.Height = Mathf.Sin(k * Mathf.PI) * 0.35f;
                Vfx.Afterimage(weapon, trailColor, 0.14f);
                if (!hit && k >= swing.impactAt) { hit = true; Impact(swing, onImpact); }
                yield return null;
            }
            angle = to;
            if (!hit) Impact(swing, onImpact);
            fx.Lean = 0f;
            fx.Height = 0f;

            // Follow-through: the weight of the weapon carries it a little further, then back to guard.
            float over = to + Mathf.Sign(to - from) * 18f;
            for (float t = 0f; t < 0.08f; t += Time.deltaTime)
            {
                angle = Mathf.Lerp(to, over, t / 0.08f);
                yield return null;
            }
            angle = Normalize(over);
            angleVelocity = 0f;
            driven--;
        }

        void Impact(MacuahuitlTechnique swing, Action onImpact)
        {
            ActorFx.Of(this).Squash(swing.heavy ? new Vector2(1.3f, 0.75f) : new Vector2(1.12f, 0.9f));
            if (swing.heavy)
            {
                Vfx.Burst(Tip, 10, MexicaPalette.Bone, new Vector2(2f, 5f), 12f, 1.3f, 0.45f, toGround: false, upward: 0.5f);
                Vfx.Burst(Tip, 6, new Color(0.25f, 0.23f, 0.33f), new Vector2(1.5f, 4f), 14f, 1.6f, 0.6f);   // obsidian chips
            }
            onImpact?.Invoke();
        }

        /// <summary>Tints the weapon (and sparks at the tip) for a while: charges, Fury, Tonalli.</summary>
        public IEnumerator Glow(Color color, float time)
        {
            if (weapon == null) yield break;
            for (float t = 0f; t < time; t += Time.deltaTime)
            {
                weapon.color = Color.Lerp(Color.white, color, 0.5f + 0.5f * Mathf.Sin(t * 18f));
                if (Time.frameCount % 4 == 0) Vfx.Wisps(Tip, 1, color, 0.8f, 0.1f);
                yield return null;
            }
            weapon.color = Color.white;
        }

        // ---------------- Every frame ----------------

        void LateUpdate()
        {
            if (!IsShown) return;

            // The character keeps its art but without the club drawn in it.
            if (body != null && body.sprite != null && body.sprite != WeaponSprites.Unarmed(armedFrame))
            {
                armedFrame = body.sprite;
                body.sprite = WeaponSprites.Unarmed(armedFrame);
            }

            if (driven == 0)
            {
                // Guard stance with a slow sway, springing back after a blow.
                float rest = float.IsNaN(hold) ? guardAngle + Mathf.Sin(Time.time * 2.3f) * swayDegrees : hold;
                angleVelocity += (Mathf.DeltaAngle(angle, rest)) * 220f * Time.deltaTime;
                angleVelocity *= Mathf.Exp(-14f * Time.deltaTime);
                angle += angleVelocity * Time.deltaTime;
            }

            PlaceWeapon();
        }

        void PlaceWeapon()
        {
            if (body == null || body.sprite == null || visual == null) return;
            Vector3 size = body.sprite.bounds.size;
            // Hand in the body's local space (follows height, squash and lean of the Visual).
            Vector2 g = combatPose != null ? combatGrip : grip;
            Vector3 hand = visual.TransformPoint(new Vector3(g.x * size.x, g.y * size.y, 0f));
            weapon.transform.position = new Vector3(hand.x, hand.y, transform.position.z);

            float bodyLean = visual.eulerAngles.z;
            weapon.transform.rotation = Quaternion.Euler(0f, 0f, angle + bodyLean);

            // Same pixel density as the 16 px character art; scaled for other bodies (placeholders).
            float bodyHeight = size.y * Mathf.Abs(visual.lossyScale.y);
            float artHeight = weapon.sprite.bounds.size.y;
            float wanted = bodyHeight * sizeToBody;
            float scale = Mathf.Abs(body.sprite.pixelsPerUnit - weapon.sprite.pixelsPerUnit) < 0.01f ? 1f : wanted / artHeight;
            weapon.transform.localScale = Vector3.one * scale;

            weapon.sortingLayerID = body.sortingLayerID;
            weapon.sortingOrder = body.sortingOrder + 1;
            weapon.spriteSortPoint = SpriteSortPoint.Pivot;
        }

        static float Normalize(float a)
        {
            a %= 360f;
            return a > 180f ? a - 360f : a < -180f ? a + 360f : a;
        }

        void OnDisable() => Show(false);
    }
}
