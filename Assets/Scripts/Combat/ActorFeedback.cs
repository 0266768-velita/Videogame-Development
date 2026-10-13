using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visual effects and Animator bridge for any 2D combatant (enemies and player).
/// Works with placeholder sprites (tints, lunges) and with real art:
/// if the Animator has the parameters it uses them, otherwise it silently ignores them.
/// </summary>
public class ActorFeedback : MonoBehaviour
{
    [Tooltip("true = your sprites face RIGHT by default (flipX is used when going left).")]
    public bool spritesFaceRight = true;

    SpriteRenderer[] sprites;
    Color[] originalColors;
    Animator anim;
    Rigidbody2D rb;
    readonly HashSet<int> animParams = new HashSet<int>();
    Vector3 baseScale;

    void Awake()
    {
        sprites = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) originalColors[i] = sprites[i].color;

        rb = GetComponent<Rigidbody2D>();
        baseScale = transform.localScale;
        anim = GetComponentInChildren<Animator>();
        if (anim != null && anim.runtimeAnimatorController != null)
            foreach (var p in anim.parameters) animParams.Add(p.nameHash);
    }

    // ---------- Animator ----------
    public void Trigger(string triggerName)
    {
        if (anim == null || string.IsNullOrEmpty(triggerName)) return;
        int h = Animator.StringToHash(triggerName);
        if (animParams.Contains(h)) anim.SetTrigger(h);
    }

    public void SetFloat(string paramName, float value)
    {
        if (anim == null) return;
        int h = Animator.StringToHash(paramName);
        if (animParams.Contains(h)) anim.SetFloat(h, value);
    }

    public void SetBool(string paramName, bool value)
    {
        if (anim == null) return;
        int h = Animator.StringToHash(paramName);
        if (animParams.Contains(h)) anim.SetBool(h, value);
    }

    /// <summary>Flips the sprite according to facing direction (dirX &lt; 0 = left).</summary>
    public void Face(float dirX)
    {
        if (Mathf.Abs(dirX) < 0.01f || sprites == null) return;
        bool flip = spritesFaceRight ? dirX < 0f : dirX > 0f;
        foreach (var s in sprites) if (s != null) s.flipX = flip;
    }

    /// <summary>Turns physics off/on (in combat the BattleManager moves the transform directly).</summary>
    public void SetPhysics(bool on)
    {
        if (rb == null) return;
        if (!on) rb.linearVelocity = Vector2.zero;
        rb.simulated = on;
    }

    // ---------- Tints ----------
    public void Tint(Color c)
    {
        if (sprites == null) return;
        foreach (var s in sprites) if (s != null) s.color = new Color(c.r, c.g, c.b, s.color.a);
    }

    public void ClearTint()
    {
        if (sprites == null) return;
        for (int i = 0; i < sprites.Length; i++) if (sprites[i] != null) sprites[i].color = originalColors[i];
    }

    /// <summary>Warning flash while the enemy prepares its attack.</summary>
    public IEnumerator Telegraph(float duration, Color warn)
    {
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float ping = Mathf.PingPong(t * 5f, 1f);
            Tint(Color.Lerp(new Color(warn.r * 0.5f, warn.g * 0.5f, warn.b * 0.5f, 1f), warn, ping));
            yield return null;
        }
        ClearTint();
    }

    public IEnumerator Pulse(Color c, float duration)
    {
        Tint(c);
        yield return new WaitForSeconds(duration);
        ClearTint();
    }

    /// <summary>White flash + small scale punch when taking damage.</summary>
    public IEnumerator HitReaction()
    {
        Tint(Color.white);
        transform.localScale = baseScale * 1.08f;
        yield return new WaitForSeconds(0.1f);
        transform.localScale = baseScale;
        ClearTint();
    }

    // ---------- Combat movement ----------
    /// <summary>Lunges toward a point, fires onImpact on arrival, then returns.</summary>
    public IEnumerator Lunge(Vector3 targetPoint, float stopDistance, float duration, System.Action onImpact)
    {
        Vector3 start = transform.position;
        Vector3 flat = targetPoint - start; flat.z = 0f;
        float dist = flat.magnitude;
        Vector3 dir = dist > 0.01f ? flat / dist : Vector3.right;
        Vector3 end = start + dir * Mathf.Max(0f, dist - stopDistance);

        yield return MoveTo(start, end, duration);
        onImpact?.Invoke();
        yield return new WaitForSeconds(0.12f);
        yield return MoveTo(end, start, duration * 1.6f);
    }

    /// <summary>Moves by an offset and comes back (player dodge / enemy knockback).</summary>
    public IEnumerator Displace(Vector3 offset, float duration)
    {
        Vector3 start = transform.position;
        Vector3 end = start + offset;
        yield return MoveTo(start, end, duration);
        yield return new WaitForSeconds(0.1f);
        yield return MoveTo(end, start, duration * 1.4f);
    }

    /// <summary>Hop in place (Seismic Jump).</summary>
    public IEnumerator Hop(float height, float duration)
    {
        Vector3 start = transform.position;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;
            transform.position = start + Vector3.up * (Mathf.Sin(k * Mathf.PI) * height);
            yield return null;
        }
        transform.position = start;
    }

    /// <summary>
    /// Seismic Jump: crouch (anticipation) -> launch -> hang -> fast slam -> squash and recover.
    /// Uses FakeHeight, so only the visual moves. onImpact fires on the exact frame of the slam.
    /// Total time is about 0.9 s.
    /// </summary>
    public IEnumerator SeismicJump(float height, System.Action onImpact)
    {
        var fh = GetComponent<FakeHeight>();
        if (fh == null) fh = gameObject.AddComponent<FakeHeight>();

        var crouch = new Vector2(1.25f, 0.65f);
        var stretch = new Vector2(0.85f, 1.25f);
        var slam = new Vector2(1.4f, 0.6f);

        // 1) Anticipation: crouch down
        yield return Tween(0.18f, k => fh.Squash = Vector2.Lerp(Vector2.one, crouch, EaseOut(k)));
        // 2) Launch: shoot up fast, slow near the top
        yield return Tween(0.30f, k =>
        {
            fh.Height = height * (1f - Mathf.Pow(1f - k, 3f));
            fh.Squash = Vector2.Lerp(crouch, stretch, EaseOut(Mathf.Min(1f, k * 3f)));
        });
        // 3) Hang time at the top
        yield return Tween(0.08f, k =>
        {
            fh.Height = height + 0.06f * Mathf.Sin(k * Mathf.PI);
            fh.Squash = Vector2.Lerp(stretch, Vector2.one, k);
        });
        // 4) Slam down (accelerates)
        yield return Tween(0.14f, k =>
        {
            fh.Height = height * (1f - k * k * k);
            fh.Squash = Vector2.Lerp(Vector2.one, stretch, k);
        });
        // 5) Impact
        fh.Height = 0f;
        fh.Squash = slam;
        onImpact?.Invoke();
        // 6) Recover with a little overshoot
        yield return Tween(0.22f, k => fh.Squash = Vector2.LerpUnclamped(slam, Vector2.one, EaseOutBack(k)));
        fh.ResetVisual();
    }

    static IEnumerator Tween(float duration, System.Action<float> step)
    {
        duration = Mathf.Max(0.01f, duration);
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            step(t / duration);
            yield return null;
        }
        step(1f);
    }

    static float EaseOut(float k) => 1f - (1f - k) * (1f - k);
    static float EaseOutBack(float k)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(k - 1f, 3f) + c1 * Mathf.Pow(k - 1f, 2f);
    }

    IEnumerator MoveTo(Vector3 a, Vector3 b, float dur)
    {
        dur = Mathf.Max(0.01f, dur);
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(a, b, Mathf.SmoothStep(0f, 1f, t / dur));
            yield return null;
        }
        transform.position = b;
    }

    // ---------- Death ----------
    public IEnumerator DeathRoutine(float delay, float duration, bool destroyAfter)
    {
        Trigger("Die");
        yield return new WaitForSeconds(delay);
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float a = 1f - t / duration;
            foreach (var s in sprites)
                if (s != null) { var c = s.color; c.a = a; s.color = c; }
            yield return null;
        }
        if (destroyAfter) Destroy(gameObject);
    }
}
