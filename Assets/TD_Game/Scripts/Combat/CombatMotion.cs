using System;
using System.Collections;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Scripted combat movements with "game feel": anticipation, squash and stretch, motion trails,
    /// impacts and recovery. They move the actor's root (position on the map) and animate its sprite
    /// through ActorFx. Placeholders for the Aseprite animations: the timings and the onHit callbacks
    /// stay the same when the Animator replaces them.
    /// </summary>
    public static class CombatMotion
    {
        /// <summary>
        /// Attack run: wind back, dash with a trail, hit (onHit), recoil and hop back to the start.
        /// duration = time of the dash itself.
        /// strike = optional weapon blow performed on arrival (it must call the action it receives at the
        /// moment of impact); without it the hit is instantaneous.
        /// </summary>
        public static IEnumerator Lunge(Transform actor, Vector3 target, float stopDistance, float duration, Action onHit,
                                        Func<Action, IEnumerator> strike = null)
        {
            var fx = ActorFx.Of(actor);
            var sprite = actor.GetComponentInChildren<SpriteRenderer>();
            Vector3 start = actor.position;
            Vector3 toTarget = target - start;
            toTarget.z = 0f;
            float side = Mathf.Sign(toTarget.x == 0f ? 1f : toTarget.x);
            float travel = Mathf.Max(0f, toTarget.magnitude - stopDistance);
            Vector3 dir = toTarget.normalized;
            Vector3 hitPoint = start + dir * travel;

            // 1. Anticipation: lean back and crouch.
            fx.Pose = new Vector2(1.15f, 0.85f);
            fx.Lean = 10f * side;
            yield return Move(actor, start, start - dir * 0.18f, 0.08f);

            // 2. Dash: stretched forward, leaning in, leaving afterimages.
            fx.Pose = new Vector2(1.3f, 0.8f);
            fx.Lean = -14f * side;
            Vfx.Dust(start, 2, MexicaPalette.Ash, 0.6f);
            yield return MoveWithTrail(actor, start - dir * 0.18f, hitPoint, duration, sprite);

            // 3. Impact (or the weapon blow, which calls onHit itself when the blade lands).
            fx.Pose = Vector2.one;
            fx.Squash(new Vector2(0.8f, 1.2f));
            fx.Lean = 0f;
            if (strike != null) yield return strike(onHit);
            else onHit?.Invoke();
            yield return new WaitForSeconds(0.1f);

            // 4. Recover: small hop back home.
            yield return HopTo(actor, hitPoint, start, 0.3f, duration * 1.4f);
            fx.Squash(new Vector2(1.15f, 0.88f));
        }

        /// <summary>Jumps in place (only the sprite rises; the shadow stays) and calls onLand on touching down.</summary>
        public static IEnumerator Hop(Transform actor, float height, float duration, Action onLand)
        {
            var fx = ActorFx.Of(actor);
            fx.Squash(new Vector2(1.2f, 0.8f));
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                fx.Height = 4f * height * k * (1f - k);
                yield return null;
            }
            fx.Height = 0f;
            fx.Squash(new Vector2(1.25f, 0.78f));
            onLand?.Invoke();
        }

        /// <summary>Dodge: a quick hop by an offset with a trail, then back to the start.</summary>
        public static IEnumerator Step(Transform actor, Vector3 offset, float duration)
        {
            var fx = ActorFx.Of(actor);
            var sprite = actor.GetComponentInChildren<SpriteRenderer>();
            Vector3 start = actor.position;
            Vector3 away = start + new Vector3(offset.x, 0f, 0f);
            float side = Mathf.Sign(offset.x == 0f ? -1f : offset.x);

            fx.Squash(new Vector2(0.8f, 1.2f));
            fx.Lean = 18f * -side;
            var rig = actor.GetComponent<MacuahuitlRig>();
            if (rig != null && rig.IsShown) rig.StartCoroutine(rig.RaiseTo(110f, duration));   // weapon swept back
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                actor.position = Vector3.Lerp(start, away, Mathf.SmoothStep(0f, 1f, k));
                fx.Height = Mathf.Sin(k * Mathf.PI) * Mathf.Max(0.35f, offset.y);
                if (Time.frameCount % 2 == 0) Vfx.Afterimage(sprite, MexicaPalette.Turquoise, 0.18f);
                yield return null;
            }
            actor.position = away;
            fx.Height = 0f;
            fx.Lean = 0f;
            fx.Squash(new Vector2(1.2f, 0.82f));
            Vfx.Dust(away, 2, MexicaPalette.Ash, 0.6f);
            yield return new WaitForSeconds(0.15f);
            if (rig != null) rig.Release();
            yield return Move(actor, away, start, duration * 1.3f);
        }

        /// <summary>Taking a hit: knocked back, squashed and tilted for a moment.</summary>
        public static void HitReaction(Transform actor, Vector3 from, float strength = 1f)
        {
            var fx = ActorFx.Of(actor);
            float side = Mathf.Sign(actor.position.x - from.x == 0f ? 1f : actor.position.x - from.x);
            fx.Kick(new Vector2(0.28f * side * strength, 0.05f * strength));
            fx.Squash(new Vector2(1f + 0.25f * strength, 1f - 0.2f * strength));
        }

        /// <summary>The enemy gathers strength while its attack is telegraphed: crouches and trembles more and more.</summary>
        public static IEnumerator ChargeUp(Transform actor, float duration)
        {
            var fx = ActorFx.Of(actor);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                fx.Pose = Vector2.Lerp(Vector2.one, new Vector2(1.18f, 0.82f), k);
                fx.Tremble = 0.03f * k * k;
                yield return null;
            }
            fx.Pose = Vector2.one;
            fx.Tremble = 0f;
        }

        /// <summary>Death: flattens, souls (gold wisps) leave the body, debris, then fades out.</summary>
        public static IEnumerator Death(Transform actor, SpriteRenderer sprite, float fadeTime)
        {
            var fx = ActorFx.Of(actor);
            Vector3 p = actor.position;
            fx.Breathing = false;
            fx.Squash(new Vector2(1.4f, 0.6f));
            fx.Pose = new Vector2(1.25f, 0.55f);
            Vfx.Wisps(p + Vector3.up * 0.3f, 10, MexicaPalette.AgedGold, 2f, 0.4f);
            Vfx.Burst(p + Vector3.up * 0.3f, 12, MexicaPalette.Ash, new Vector2(1.5f, 3.5f), 12f, 1.6f, 0.7f);
            Vfx.Ring(p, MexicaPalette.AgedGold, 0.2f, 1.1f, 0.4f, onGround: true, squashY: 0.5f);

            if (sprite != null)
            {
                Color c = sprite.color;
                for (float t = 0f; t < fadeTime; t += Time.deltaTime)
                {
                    sprite.color = new Color(c.r, c.g, c.b, 1f - t / fadeTime);
                    yield return null;
                }
            }
            fx.Restore();
        }

        /// <summary>Tints the sprite for a moment (hit, heal, buff).</summary>
        public static IEnumerator Flash(SpriteRenderer sprite, Color color, float duration)
        {
            if (sprite == null) yield break;
            Color original = sprite.color;
            sprite.color = color;
            yield return new WaitForSeconds(duration);
            if (sprite != null) sprite.color = original;
        }

        /// <summary>Blinks faster and faster in the warning colour until the attack lands.</summary>
        public static IEnumerator Telegraph(SpriteRenderer sprite, float duration, Color color)
        {
            if (sprite == null) yield break;
            Color original = sprite.color;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float speed = Mathf.Lerp(4f, 16f, t / duration);
                sprite.color = Color.Lerp(original, color, Mathf.PingPong(t * speed, 1f));
                yield return null;
            }
            if (sprite != null) sprite.color = original;
        }

        // ---------------- Helpers ----------------

        static IEnumerator Move(Transform actor, Vector3 from, Vector3 to, float duration)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                actor.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            actor.position = to;
        }

        static IEnumerator MoveWithTrail(Transform actor, Vector3 from, Vector3 to, float duration, SpriteRenderer sprite)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                actor.position = Vector3.Lerp(from, to, k * k);   // accelerates into the hit
                Vfx.Afterimage(sprite, MexicaPalette.Bone, 0.16f);
                yield return null;
            }
            actor.position = to;
        }

        static IEnumerator HopTo(Transform actor, Vector3 from, Vector3 to, float height, float duration)
        {
            var fx = ActorFx.Of(actor);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                actor.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, k));
                fx.Height = Mathf.Sin(k * Mathf.PI) * height;
                yield return null;
            }
            actor.position = to;
            fx.Height = 0f;
        }
    }
}
