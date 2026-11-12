using System;
using System.Collections;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Small scripted movements and colour effects used in combat. They are placeholders for the
    /// Animator animations (attack, hit, dodge...) that arrive with the Aseprite art.
    /// </summary>
    public static class CombatMotion
    {
        /// <summary>Runs to the target, calls onHit on contact and returns to the start.</summary>
        public static IEnumerator Lunge(Transform actor, Vector3 target, float stopDistance, float duration, Action onHit)
        {
            Vector3 start = actor.position;
            Vector3 toTarget = target - start;
            toTarget.z = 0f;
            float travel = Mathf.Max(0f, toTarget.magnitude - stopDistance);
            Vector3 hitPoint = start + toTarget.normalized * travel;

            yield return Move(actor, start, hitPoint, duration);
            onHit?.Invoke();
            yield return new WaitForSeconds(0.08f);
            yield return Move(actor, hitPoint, start, duration);
        }

        /// <summary>Jumps in place and calls onLand when touching the ground again.</summary>
        public static IEnumerator Hop(Transform actor, float height, float duration, Action onLand)
        {
            Vector3 start = actor.position;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                actor.position = start + Vector3.up * (4f * height * k * (1f - k));
                yield return null;
            }
            actor.position = start;
            onLand?.Invoke();
        }

        /// <summary>Steps by an offset and comes back (dodge).</summary>
        public static IEnumerator Step(Transform actor, Vector3 offset, float duration)
        {
            Vector3 start = actor.position;
            yield return Move(actor, start, start + offset, duration);
            yield return new WaitForSeconds(0.15f);
            yield return Move(actor, start + offset, start, duration);
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

        static IEnumerator Move(Transform actor, Vector3 from, Vector3 to, float duration)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                actor.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            actor.position = to;
        }
    }
}
