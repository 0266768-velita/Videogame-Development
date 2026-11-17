using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// One short-lived effect sprite (debris, spark, ring, slash...). It moves, scales, spins and fades
    /// on its own, then destroys itself. Uses scaled time, so the hit stop freezes the effects too.
    /// </summary>
    public class VfxPiece : MonoBehaviour
    {
        public SpriteRenderer Sprite { get; private set; }
        public Vector2 Velocity;
        public float Gravity;
        public float Drag;
        public float Spin;
        public float Life = 0.5f;
        public Vector3 ScaleFrom = Vector3.one;
        public Vector3 ScaleTo = Vector3.one;
        [Tooltip("Fraction of the life that stays fully opaque before fading.")]
        public float HoldAlpha = 0.3f;
        /// <summary>Debris that falls: it stops at this Y (the ground) instead of falling forever.</summary>
        public float? GroundY { get; set; }

        float age;
        Color baseColor;

        public static VfxPiece Create(Sprite sprite, Vector3 position, Color color, int sortingOrder)
        {
            var go = new GameObject("Vfx");
            go.transform.position = position;
            var piece = go.AddComponent<VfxPiece>();
            piece.Sprite = go.AddComponent<SpriteRenderer>();
            piece.Sprite.sprite = sprite;
            piece.Sprite.color = color;
            piece.Sprite.sortingOrder = sortingOrder;
            piece.baseColor = color;
            return piece;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            float k = Mathf.Clamp01(age / Life);

            Velocity.y -= Gravity * dt;
            Velocity *= Mathf.Exp(-Drag * dt);
            Vector3 p = transform.position + (Vector3)(Velocity * dt);
            if (GroundY.HasValue && p.y < GroundY.Value && Velocity.y < 0f)
            {
                p.y = GroundY.Value;
                Velocity = new Vector2(Velocity.x * 0.4f, -Velocity.y * 0.25f);   // small bounce, then rest
            }
            transform.position = p;
            transform.Rotate(0f, 0f, Spin * dt);
            transform.localScale = Vector3.Lerp(ScaleFrom, ScaleTo, 1f - (1f - k) * (1f - k));   // ease-out

            float alpha = k < HoldAlpha ? 1f : 1f - Mathf.InverseLerp(HoldAlpha, 1f, k);
            Sprite.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * alpha);

            if (age >= Life) Destroy(gameObject);
        }
    }
}
