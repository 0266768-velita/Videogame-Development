using System.Collections;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Catalogue of combat effects built from VfxPiece: shockwave rings, dust, debris, sparks,
    /// slashes, ground cracks, afterimages, soul wisps and projectiles.
    /// Colours come from MexicaPalette so everything matches the HUD.
    /// </summary>
    public static class Vfx
    {
        // Sorting: ground marks under the characters, everything else above them. In combat the fighters are
        // drawn at CombatArena.FighterOrder (510, above the darkness of the map), so the front effects go above that.
        public const int GroundOrder = -5;
        public const int FrontOrder = 520;

        /// <summary>Expanding ring. radius in tiles. onGround = drawn under the characters (shockwaves).</summary>
        public static VfxPiece Ring(Vector3 position, Color color, float fromRadius, float toRadius, float time, bool onGround = false, float squashY = 1f)
        {
            var p = VfxPiece.Create(VfxSprites.Ring, position, color, onGround ? GroundOrder : FrontOrder);
            p.Life = time;
            p.HoldAlpha = 0.25f;
            // The ring sprite is 1 tile wide: scale = diameter. squashY < 1 lays it on the ground (perspective).
            p.ScaleFrom = new Vector3(fromRadius * 2f, fromRadius * 2f * squashY, 1f);
            p.ScaleTo = new Vector3(toRadius * 2f, toRadius * 2f * squashY, 1f);
            return p;
        }

        /// <summary>Soft dust clouds that spread along the ground and fade.</summary>
        public static void Dust(Vector3 position, int count, Color color, float spread = 1.2f)
        {
            for (int i = 0; i < count; i++)
            {
                float dir = i % 2 == 0 ? 1f : -1f;
                var p = VfxPiece.Create(VfxSprites.Soft, position + new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0f, 0.1f), 0f),
                    new Color(color.r, color.g, color.b, 0.55f), FrontOrder - 1);
                p.Velocity = new Vector2(dir * Random.Range(0.5f, spread) * 2f, Random.Range(0.1f, 0.6f));
                p.Drag = 4f;
                p.Life = Random.Range(0.45f, 0.75f);
                float s = Random.Range(0.4f, 0.7f);
                p.ScaleFrom = Vector3.one * s;
                p.ScaleTo = Vector3.one * s * 2.2f;
                p.HoldAlpha = 0.1f;
            }
        }

        /// <summary>Pixel chunks thrown up that fall back to the ground (rocks, sparks, blood).</summary>
        public static void Burst(Vector3 position, int count, Color color, Vector2 speed, float gravity = 14f,
                                 float size = 1.5f, float life = 0.7f, bool toGround = true, float upward = 0.6f)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 dir = Random.insideUnitCircle.normalized;
                dir.y = Mathf.Abs(dir.y) * upward + (1f - upward) * dir.y;
                var tint = Color.Lerp(color, Color.black, Random.Range(0f, 0.35f));
                var p = VfxPiece.Create(VfxSprites.Pixel, position, tint, FrontOrder);
                p.Velocity = dir.normalized * Random.Range(speed.x, speed.y) + Vector2.up * Random.Range(0f, speed.y * upward);
                p.Gravity = gravity;
                p.Spin = Random.Range(-540f, 540f);
                p.Life = life * Random.Range(0.7f, 1.2f);
                p.ScaleFrom = p.ScaleTo = Vector3.one * size * Random.Range(0.6f, 1.4f);
                p.HoldAlpha = 0.6f;
                if (toGround) p.GroundY = position.y - Random.Range(0f, 0.25f);
            }
        }

        /// <summary>Crescent slash at the impact. direction = +1 right, -1 left.</summary>
        public static void Slash(Vector3 position, float direction, Color color, float size = 1f, float angle = 0f)
        {
            var p = VfxPiece.Create(VfxSprites.Crescent, position, color, FrontOrder + 1);
            p.transform.rotation = Quaternion.Euler(0f, 0f, angle + (direction < 0f ? 180f : 0f));
            p.Life = 0.18f;
            p.HoldAlpha = 0.2f;
            p.ScaleFrom = Vector3.one * size * 0.6f;
            p.ScaleTo = Vector3.one * size * 1.3f;
            p.Spin = direction * -260f;
        }

        /// <summary>Star of cracks left on the ground by a heavy impact.</summary>
        public static void Crack(Vector3 position, float size, float life = 1.6f)
        {
            var p = VfxPiece.Create(VfxSprites.Crack, position, new Color(0.05f, 0.03f, 0.03f, 0.85f), GroundOrder);
            p.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            p.Life = life;
            p.HoldAlpha = 0.65f;
            p.ScaleFrom = new Vector3(size * 0.7f, size * 0.35f, 1f);    // flattened: lies on the floor
            p.ScaleTo = new Vector3(size, size * 0.5f, 1f);
        }

        /// <summary>The order a sprite is really drawn at: its SortingGroup's if it is inside one (fighters in combat).</summary>
        static int DrawnOrder(SpriteRenderer sprite)
        {
            var group = sprite.GetComponentInParent<UnityEngine.Rendering.SortingGroup>();
            return group != null ? group.sortingOrder : sprite.sortingOrder;
        }

        /// <summary>Faded copy of a sprite at its current pose (motion trails).</summary>
        public static void Afterimage(SpriteRenderer source, Color tint, float life = 0.22f)
        {
            if (source == null || source.sprite == null) return;
            var p = VfxPiece.Create(source.sprite, source.transform.position, new Color(tint.r, tint.g, tint.b, 0.45f), DrawnOrder(source) - 1);
            p.Sprite.flipX = source.flipX;
            p.Sprite.flipY = source.flipY;
            p.transform.rotation = source.transform.rotation;
            p.ScaleFrom = p.ScaleTo = source.transform.lossyScale;
            p.Life = life;
            p.HoldAlpha = 0f;
        }

        /// <summary>Glowing diamonds rising slowly (souls leaving a body, auras, healing).</summary>
        public static void Wisps(Vector3 position, int count, Color color, float height = 1.6f, float spread = 0.5f)
        {
            for (int i = 0; i < count; i++)
            {
                var p = VfxPiece.Create(VfxSprites.Diamond, position + new Vector3(Random.Range(-spread, spread), Random.Range(0f, 0.6f), 0f),
                    color, FrontOrder);
                p.Velocity = new Vector2(Random.Range(-0.2f, 0.2f), Random.Range(height * 0.6f, height * 1.2f));
                p.Drag = 1.2f;
                p.Life = Random.Range(0.7f, 1.2f);
                p.ScaleFrom = Vector3.one * Random.Range(0.8f, 1.2f);
                p.ScaleTo = Vector3.one * 0.3f;
                p.HoldAlpha = 0.4f;
            }
        }

        /// <summary>A glowing orb flying from one point to another (spells). Yields until it arrives.</summary>
        public static IEnumerator Projectile(Vector3 from, Vector3 to, Color color, float time)
        {
            var core = VfxPiece.Create(VfxSprites.Soft, from, color, FrontOrder + 1);
            core.Life = time + 0.05f;
            core.HoldAlpha = 1f;
            core.ScaleFrom = core.ScaleTo = Vector3.one * 0.9f;
            for (float t = 0f; t < time; t += Time.deltaTime)
            {
                float k = t / time;
                Vector3 p = Vector3.Lerp(from, to, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 0.6f);
                if (core != null) core.transform.position = p;
                var trail = VfxPiece.Create(VfxSprites.Soft, p, new Color(color.r, color.g, color.b, 0.5f), FrontOrder);
                trail.Life = 0.2f;
                trail.HoldAlpha = 0f;
                trail.ScaleFrom = Vector3.one * 0.6f;
                trail.ScaleTo = Vector3.one * 0.1f;
                yield return null;
            }
            if (core != null) Object.Destroy(core.gameObject);
            Ring(to, color, 0.1f, 0.7f, 0.25f);
            Burst(to, 8, color, new Vector2(2f, 4f), 6f, 1.2f, 0.35f, false, 0.2f);
        }
    }
}
