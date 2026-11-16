using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Loops a few sprites on a timer (water ripples, torch flames, idle characters). Adapted from Mictlán v5's SpriteFlipper.
    /// Optional: a soft size pulse for glows.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFlipbook : MonoBehaviour
    {
        [SerializeField] Sprite[] frames;
        [SerializeField, Min(0.02f)] float secondsPerFrame = 0.5f;
        [Tooltip("0 = no pulse. 0.1 = the size changes +-10% (glows).")]
        [SerializeField, Range(0f, 0.5f)] float pulse = 0f;

        SpriteRenderer sprite;
        float timer, seed;
        int index;
        Vector3 baseScale;

        public void Setup(Sprite[] newFrames, float seconds, float newPulse = 0f)
        {
            frames = newFrames;
            secondsPerFrame = seconds;
            pulse = newPulse;
        }

        void Awake()
        {
            sprite = GetComponent<SpriteRenderer>();
            baseScale = transform.localScale;
            seed = Random.value * 10f;
            timer = Random.value * secondsPerFrame;
        }

        void Update()
        {
            if (frames != null && frames.Length > 1)
            {
                timer += Time.deltaTime;
                if (timer >= secondsPerFrame)
                {
                    timer = 0f;
                    index = (index + 1) % frames.Length;
                    sprite.sprite = frames[index];
                }
            }
            if (pulse > 0f)
                transform.localScale = baseScale * (1f + pulse * Mathf.Sin(Time.time * 7f + seed) + pulse * 0.5f * Mathf.Sin(Time.time * 13f + seed));
        }
    }
}
