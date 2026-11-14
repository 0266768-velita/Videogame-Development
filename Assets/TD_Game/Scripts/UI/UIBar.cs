using UnityEngine;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// A resource bar (health, stamina, Tonalli...). The fill glides to its value; when the value drops,
    /// a pale "trail" stays behind for a moment so the player can read how much was lost.
    /// </summary>
    public class UIBar : MonoBehaviour
    {
        [Tooltip("Image with Image Type = Filled, Horizontal.")]
        [SerializeField] Image fill;
        [Tooltip("Optional Filled image behind the fill that shows recent losses.")]
        [SerializeField] Image trail;
        [SerializeField] Text label;
        [SerializeField, Min(0.1f)] float fillSpeed = 4f;
        [SerializeField, Min(0.1f)] float trailSpeed = 0.8f;
        [SerializeField, Min(0f)] float trailDelay = 0.35f;

        float target = 1f;
        float trailWaitUntil;

        public Image Fill => fill;

        public void Set(float current, float max, string text = null)
        {
            float value = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            if (value < target - 0.001f) trailWaitUntil = Time.unscaledTime + trailDelay;
            target = value;
            if (label != null && text != null) label.text = text;
        }

        /// <summary>Jumps to the value without animation (when the bar first appears).</summary>
        public void Snap()
        {
            if (fill != null) fill.fillAmount = target;
            if (trail != null) trail.fillAmount = target;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (fill != null) fill.fillAmount = Mathf.MoveTowards(fill.fillAmount, target, fillSpeed * dt);
            if (trail == null) return;
            if (trail.fillAmount < target) trail.fillAmount = target;
            else if (Time.unscaledTime >= trailWaitUntil)
                trail.fillAmount = Mathf.MoveTowards(trail.fillAmount, target, trailSpeed * dt);
        }
    }
}
