using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// One brazier of the BrazierPuzzle. Needs a Collider2D marked "Is Trigger" (where the player stands to light it).
    /// Unlit = cold stone bowl; lit = flame animation + warm light.
    /// </summary>
    public class PuzzleBrazier : MonoBehaviour
    {
        [SerializeField] SpriteRenderer bowl;
        [SerializeField] Sprite unlitSprite;
        [SerializeField] Sprite[] litFrames;
        [SerializeField] GameObject lightGlow;

        BrazierPuzzle puzzle;
        SpriteFlipbook flame;

        public bool Lit { get; private set; }

        public void SetPuzzle(BrazierPuzzle owner) => puzzle = owner;

        void Awake()
        {
            flame = bowl.GetComponent<SpriteFlipbook>();
            SetLit(false);
        }

        public void SetLit(bool lit)
        {
            Lit = lit;
            if (flame != null) flame.enabled = lit;
            bowl.sprite = lit && litFrames.Length > 0 ? litFrames[0] : unlitSprite;
            if (lightGlow != null) lightGlow.SetActive(lit);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (puzzle != null && other.GetComponentInParent<PlayerCombatant>() != null) puzzle.PlayerNear(this, true);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (puzzle != null && other.GetComponentInParent<PlayerCombatant>() != null) puzzle.PlayerNear(this, false);
        }
    }
}
