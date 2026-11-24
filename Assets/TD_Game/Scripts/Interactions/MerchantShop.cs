using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// The merchant of Mictlán. Stand in front of the stall and press E (gamepad Y) to open the shop:
    ///   - Potion: 20 gold.
    ///   - Gamble: bet 10 or 50 gold; 50% chance to win double, 50% to lose it.
    ///   - Artifact: the Smoking Mirror of Tezcatlipoca, a new combat skill (key 6). Only once.
    /// Needs a Collider2D marked "Is Trigger" in front of the stall (where the player stands to buy).
    /// The window is built with MenuWidgets (same font and Aztec frames as the menu).
    /// </summary>
    public class MerchantShop : MonoBehaviour
    {
        [Header("Prices")]
        [SerializeField] int potionPrice = 20;
        [SerializeField] int smallBet = 10;
        [SerializeField] int bigBet = 50;
        [SerializeField] int artifactPrice = 60;
        [SerializeField] CombatAbility artifact;          // Ability_SmokingMirror

        [Header("UI")]
        [Tooltip("Small text on screen: \"[E] Talk to the merchant\".")]
        [SerializeField] Text promptText;
        [SerializeField] Font menuFont;

        PlayerCombatant player;    // the player standing at the stall, or null
        GameObject window;
        Text goldText, messageText;
        Button artifactButton;

        void Start() => ShowPrompt("");

        // ---------------- Standing at the stall ----------------

        void OnTriggerEnter2D(Collider2D other)
        {
            var who = other.GetComponentInParent<PlayerCombatant>();
            if (who == null) return;
            player = who;
            ShowPrompt("{Interact}  Talk to the merchant");
            ContextTutorial.ShowOnce("shop", "The merchant sells potions and a powerful artifact. Defeat enemies to earn gold.");
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerCombatant>() == null) return;
            player = null;
            ShowPrompt("");
        }

        void Update()
        {
            if (window != null && window.activeSelf)
            {
                if (MenuWidgets.CancelPressed()) Close();
                return;
            }
            if (player != null && GameState.IsExploring && TalkPressed()) Open();
        }

        static bool TalkPressed()
        {
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            return (keyboard != null && keyboard.eKey.wasPressedThisFrame) || (pad != null && pad.buttonNorth.wasPressedThisFrame);
        }

        // ---------------- The shop window ----------------

        void Open()
        {
            if (window == null) BuildWindow();
            window.SetActive(true);
            GameState.SetMode(GameMode.Cutscene);   // the player and the enemies stop
            ShowPrompt("");
            Say("\"Gold for the living, gold for the dead... what do you need, warrior?\"");
            Refresh();
            GameAudio.PlaySfx("ui_select", 1f, 0f);
            StartCoroutine(SelectFirst());
        }

        void Close()
        {
            window.SetActive(false);
            StartCoroutine(ResumeNextFrame());   // next frame, so the same ESC does not also open the pause menu
            GameAudio.PlaySfx("ui_back", 1f, 0f);
            if (player != null) ShowPrompt("{Interact}  Talk to the merchant");
        }

        IEnumerator ResumeNextFrame()
        {
            yield return null;
            GameState.SetMode(GameMode.Exploration);
        }

        IEnumerator SelectFirst()
        {
            yield return null;
            MenuWidgets.Select(window.GetComponentInChildren<Button>().gameObject);
        }

        void BuildWindow()
        {
            MenuWidgets.Use(menuFont, true);
            var root = MenuWidgets.Screen("ShopCanvas", 40, transform);
            window = root.gameObject;
            MenuWidgets.Stretch(MenuWidgets.Image("Shade", root, new Color(0f, 0f, 0f, 0.6f)).rectTransform);
            MenuWidgets.Panel("Window", root, new Vector2(0, 0), new Vector2(900, 700));

            var title = MenuWidgets.Label("Title", root, "MERCHANT OF MICTLÁN", 56, MexicaPalette.AgedGold);
            MenuWidgets.Center(title.rectTransform, new Vector2(0, 280), new Vector2(800, 70));
            goldText = MenuWidgets.Label("Gold", root, "", 30, MexicaPalette.AgedGold);
            MenuWidgets.Center(goldText.rectTransform, new Vector2(0, 220), new Vector2(800, 40));

            Vector2 size = new Vector2(680, 62);
            MenuWidgets.Button(root, $"Potion  -  {potionPrice} gold", new Vector2(0, 140), size, BuyPotion, 30);
            MenuWidgets.Button(root, $"Gamble {smallBet} gold  (double or nothing)", new Vector2(0, 65), size, () => Gamble(smallBet), 30);
            MenuWidgets.Button(root, $"Gamble {bigBet} gold  (double or nothing)", new Vector2(0, -10), size, () => Gamble(bigBet), 30);
            artifactButton = MenuWidgets.Button(root, "", new Vector2(0, -85), size, BuyArtifact, 26);
            MenuWidgets.Button(root, "Leave", new Vector2(0, -160), size, Close, 30);

            messageText = MenuWidgets.Label("Message", root, "", 26, MexicaPalette.Bone);
            MenuWidgets.Center(messageText.rectTransform, new Vector2(0, -250), new Vector2(820, 90));
        }

        // ---------------- What you can buy ----------------

        void BuyPotion()
        {
            if (player.Potions >= player.Stats.maxPotions) { Say("You can't carry more potions."); GameAudio.PlaySfx("error"); }
            else if (player.Progression.TrySpendGold(potionPrice))
            {
                player.AddPotions(1);
                Say($"A potion. You now carry {player.Potions}.");
                GameAudio.PlaySfx("potion");
            }
            else NotEnoughGold(potionPrice);
            Refresh();
        }

        void Gamble(int bet)
        {
            if (!player.Progression.TrySpendGold(bet)) { NotEnoughGold(bet); Refresh(); return; }

            bool win = Random.value < 0.5f;   // a coin: heads you win, tails you lose
            if (win)
            {
                player.Progression.AddGold(bet * 2);
                Say($"The bones fall your way! You win {bet * 2} gold.");
                GameAudio.PlaySfx("victory");
            }
            else
            {
                Say($"The gods laugh. You lose {bet} gold.");
                GameAudio.PlaySfx("defeat");
            }
            Refresh();
        }

        void BuyArtifact()
        {
            if (artifact == null) return;
            if (player.Progression.IsUnlocked(artifact)) { Say("You already carry the mirror."); return; }
            if (player.Progression.TrySpendGold(artifactPrice))
            {
                player.Progression.Unlock(artifact);
                Say($"The {artifact.displayName} is yours. Use it in battle with key 6.");
                GameAudio.PlaySfx("fury");
            }
            else NotEnoughGold(artifactPrice);
            Refresh();
        }

        void NotEnoughGold(int price)
        {
            Say($"You need {price} gold. You have {player.Progression.Gold}.");
            GameAudio.PlaySfx("error");
        }

        void Refresh()
        {
            goldText.text = $"Your gold: {player.Progression.Gold}     Potions: {player.Potions}";
            if (artifact != null)
            {
                bool owned = player.Progression.IsUnlocked(artifact);
                MenuWidgets.SetLabel(artifactButton, owned ? $"{artifact.displayName}  -  SOLD" : $"Artifact: {artifact.displayName}  -  {artifactPrice} gold");
                artifactButton.interactable = !owned;
            }
        }

        void Say(string message) => messageText.text = message;

        void ShowPrompt(string message)
        {
            if (promptText == null) return;
            promptText.transform.parent.gameObject.SetActive(message != "");
            GlyphText.Set(promptText, message);   // "{Interact}" becomes the E / Y / triangle icon
        }
    }
}
