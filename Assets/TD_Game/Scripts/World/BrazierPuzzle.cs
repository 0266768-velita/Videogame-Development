using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// Puzzle of the four fires: a stone stela shows four colours in order. Light the braziers (E) in that order.
    /// A wrong brazier puts all the fires out. When all four burn, a treasure chest appears (gold + a potion).
    /// The braziers are children with a PuzzleBrazier each; this script keeps the order.
    /// </summary>
    public class BrazierPuzzle : MonoBehaviour
    {
        [Tooltip("The braziers in the CORRECT order (the order shown on the stela).")]
        [SerializeField] PuzzleBrazier[] correctOrder;
        [SerializeField] GameObject treasure;      // hidden until the puzzle is solved
        [SerializeField] int treasureGold = 80;
        [SerializeField] Text promptText;          // "[E] Light the brazier"

        const string SolvedId = "puzzle_braziers";   // remembered in the save (with the seen tutorials)
        int next;                                     // how many braziers are lit correctly
        PuzzleBrazier near;                           // the brazier the player is standing at

        void Start()
        {
            treasure.SetActive(false);
            foreach (var b in correctOrder) b.SetPuzzle(this);
            Invoke(nameof(RestoreIfSolved), 0.3f);   // after the save loads
        }

        void RestoreIfSolved()
        {
            if (!ContextTutorial.HasSeen(SolvedId)) return;
            foreach (var b in correctOrder) b.SetLit(true);
            next = correctOrder.Length;
        }

        public void PlayerNear(PuzzleBrazier brazier, bool isNear)
        {
            if (isNear) near = brazier;
            else if (near == brazier) near = null;
            Prompt(near != null && !near.Lit && next < correctOrder.Length ? "[E]  Light the brazier" : "");
        }

        void Update()
        {
            if (near == null || near.Lit || !GameState.IsExploring) return;
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            bool pressed = (keyboard != null && keyboard.eKey.wasPressedThisFrame) || (pad != null && pad.buttonNorth.wasPressedThisFrame);
            if (pressed) Light(near);
        }

        void Light(PuzzleBrazier brazier)
        {
            if (brazier == correctOrder[next])
            {
                brazier.SetLit(true);
                next++;
                GameAudio.PlaySfx("seismic", 0.5f);
                if (next == correctOrder.Length) Solve();
            }
            else
            {
                // Wrong order: every fire goes out.
                foreach (var b in correctOrder) b.SetLit(false);
                next = 0;
                GameAudio.PlaySfx("error");
                CombatFeed.ShowBanner("The fires die out...\nLook at the stela: light them in its order.", 2.5f);
            }
            Prompt("");
        }

        void Solve()
        {
            ContextTutorial.MarkSeen(new[] { SolvedId });
            treasure.SetActive(true);
            GameAudio.PlaySfx("victory");
            CombatFeed.ShowBanner("The four fires burn!\nA treasure appears.", 3f);
        }

        /// <summary>Called by the treasure chest when the player opens it.</summary>
        public void OpenTreasure(PlayerCombatant player)
        {
            player.Progression.AddGold(treasureGold);
            player.AddPotions(1);
            GameAudio.PlaySfx("key");
            CombatFeed.ShowBanner($"Treasure: +{treasureGold} gold and a potion", 2.5f);
            treasure.SetActive(false);
        }

        void Prompt(string message)
        {
            if (promptText == null) return;
            promptText.text = message;
            promptText.transform.parent.gameObject.SetActive(message != "");
        }
    }
}
