using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// The Macuahuitl in Tlahuelli's hand. While Tlahuelli attacks in battle, the weapon is raised behind him and
    /// swung forward, stretching out to reach the enemy. Out of an attack it rests on his shoulder.
    /// Put it on a child of "Visual" that has the Macuahuitl SpriteRenderer (pivot at the handle).
    /// </summary>
    public class MacuahuitlSwing : MonoBehaviour
    {
        [SerializeField] float restAngle = 35f;      // resting on the shoulder
        [SerializeField] float raisedAngle = 110f;   // raised behind the head
        [SerializeField] float hitAngle = -80f;      // swung forward
        [SerializeField] float swingsPerSecond = 2.5f;
        [SerializeField] float stretch = 1.6f;       // how much longer it gets at the moment of the hit

        float timer;
        SpriteRenderer blade;

        void Awake() => blade = GetComponent<SpriteRenderer>();

        void Update()
        {
            // The Macuahuitl is already drawn in Tlahuelli's sprite: this big one only appears for the swing.
            bool attacking = IsAttacking();
            if (blade != null) blade.enabled = attacking;
            if (!attacking)
            {
                timer = 0f;
                transform.localRotation = Quaternion.Euler(0f, 0f, restAngle);
                transform.localScale = Vector3.one;
                return;
            }

            // 0 -> 0.5: raise it.  0.5 -> 1: swing it forward and stretch it.
            timer += Time.deltaTime * swingsPerSecond;
            float t = timer % 1f;
            float angle, length;
            if (t < 0.5f)
            {
                angle = Mathf.Lerp(restAngle, raisedAngle, t * 2f);
                length = 1f;
            }
            else
            {
                float k = (t - 0.5f) * 2f;
                angle = Mathf.Lerp(raisedAngle, hitAngle, k);
                length = Mathf.Lerp(1f, stretch, Mathf.Sin(k * Mathf.PI));
            }
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            transform.localScale = new Vector3(1f, length, 1f);   // the blade is vertical in the picture: stretch Y
        }

        /// <summary>True while the player performs an action that hits (not while drinking a potion).</summary>
        static bool IsAttacking()
        {
            var combat = CombatManager.Instance;
            if (combat == null || !combat.InCombat) return false;
            var action = combat.PlayerTurn.Performing;
            return action != null && !(action is PotionAbility);
        }
    }
}
