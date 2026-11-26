using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// A merchant of Mictlán: a hooded trader who waits in every region of the map with the same goods (and the same
    /// gold purse, since what Tlahuelli buys belongs to Tlahuelli). Stand in front of the stall and press {Interact}:
    ///   - Potion.
    ///   - Gamble: bet a little or a lot; a bit less than half the time the bones pay double.
    ///   - Artifact: the Smoking Mirror of Tezcatlipoca, a new combat skill (key 6). Only once.
    ///   - Skins of Tlahuelli: see each one on him before paying; wear any skin you already own.
    /// He talks: a greeting, a line when you buy and when you win or lose a bet. Each moment has several lines
    /// (never the same one twice in a row); the voice and the subtitle go together.
    /// Needs a Collider2D marked "Is Trigger" in front of the stall (where the player stands to buy).
    /// </summary>
    public class MerchantShop : MonoBehaviour
    {
        [System.Serializable]
        public class VoiceLine
        {
            [TextArea] public string text;
            public AudioClip clip;
        }

        [SerializeField] string merchantName = "MERCHANT OF MICTLÁN";

        [Header("Prices")]
        [SerializeField] int potionPrice = 25;
        [SerializeField] int smallBet = 10;
        [SerializeField] int bigBet = 30;
        [Tooltip("Chance of winning a bet (it pays double). Below 0.5: the house of the dead always wins in the end.")]
        [SerializeField, Range(0f, 1f)] float winChance = 0.45f;
        [SerializeField] int artifactPrice = 120;
        [SerializeField] CombatAbility artifact;          // Ability_SmokingMirror

        [Header("Voice")]
        [SerializeField] VoiceLine[] greetLines;
        [SerializeField] VoiceLine[] buyLines;
        [SerializeField] VoiceLine[] winLines;
        [SerializeField] VoiceLine[] loseLines;
        [SerializeField] VoiceLine[] goodbyeLines;
        [Tooltip("Pitch of his voice (below 1 = deeper).")]
        [SerializeField, Range(0.5f, 1.5f)] float voicePitch = 0.92f;

        [Header("Skins")]
        [Tooltip("Tlahuelli walking towards the camera, for the preview.")]
        [SerializeField] Sprite[] previewFrames;

        [Header("UI")]
        [Tooltip("Small text on screen: \"[E] Talk to the merchant\" (used only if there is no InteractPrompt).")]
        [SerializeField] Text promptText;
        [SerializeField] Font menuFont;

        PlayerCombatant player;    // the player standing at the stall, or null
        GameObject window, mainPage, skinPage;
        Text goldText, messageText, skinInfo;
        Button artifactButton, skinsButton;
        Image preview;
        Material previewMaterial;
        readonly List<(Button button, SkinCatalog.Skin skin)> skinButtons = new List<(Button, SkinCatalog.Skin)>();
        readonly Dictionary<VoiceLine[], int> lastLine = new Dictionary<VoiceLine[], int>();
        SkinCatalog.Skin previewed;

        void Start() => ShowPrompt("");

        // ---------------- Standing at the stall ----------------

        void OnTriggerEnter2D(Collider2D other)
        {
            var who = other.GetComponentInParent<PlayerCombatant>();
            if (who == null) return;
            player = who;
            ShowPrompt("{Interact}  Talk to the merchant");
            ContextTutorial.ShowOnce("shop", "Merchants wait in every region. They sell potions, an artifact and new colours for Tlahuelli. Gold is scarce: spend it well.");
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
                if (MenuWidgets.CancelPressed())
                {
                    if (skinPage.activeSelf) ShowMainPage();
                    else Close();
                }
                else if (skinPage.activeSelf) PreviewSelected();
                return;
            }
            if (player != null && GameState.IsExploring && !DialogueBox.IsOpen && InteractPrompt.Pressed()) Open();
        }

        // ---------------- The shop window ----------------

        void Open()
        {
            if (window == null) BuildWindow();
            window.SetActive(true);
            GameState.SetMode(GameMode.Cutscene);   // the player and the enemies stop
            ShowPrompt("");
            ShowMainPage();
            Speak(greetLines, "\"What do you want, stranger?\"");
            GameAudio.PlaySfx("ui_select", 1f, 0f);
        }

        void Close()
        {
            window.SetActive(false);
            StartCoroutine(ResumeNextFrame());   // next frame, so the same ESC does not also open the pause menu
            GameAudio.PlaySfx("ui_back", 1f, 0f);
            Speak(goodbyeLines, null);
            if (player != null) ShowPrompt("{Interact}  Talk to the merchant");
            if (SaveController.Instance != null) SaveController.Instance.Save();   // what you bought is kept
        }

        IEnumerator ResumeNextFrame()
        {
            yield return null;
            GameState.SetMode(GameMode.Exploration);
        }

        IEnumerator SelectFirst(GameObject page)
        {
            yield return null;
            var first = page.GetComponentInChildren<Button>();
            if (first != null) MenuWidgets.Select(first.gameObject);
        }

        void ShowMainPage()
        {
            mainPage.SetActive(true);
            skinPage.SetActive(false);
            Refresh();
            StartCoroutine(SelectFirst(mainPage));
        }

        void ShowSkinPage()
        {
            mainPage.SetActive(false);
            skinPage.SetActive(true);
            Refresh();
            StartCoroutine(SelectFirst(skinPage));
        }

        void BuildWindow()
        {
            MenuWidgets.Use(menuFont, true);
            var root = MenuWidgets.Screen("ShopCanvas", 40, transform);
            window = root.gameObject;
            MenuWidgets.Stretch(MenuWidgets.Image("Shade", root, new Color(0f, 0f, 0f, 0.6f)).rectTransform);
            MenuWidgets.Panel("Window", root, new Vector2(0, 0), new Vector2(1180, 860));

            var title = MenuWidgets.Label("Title", root, merchantName, 56, MexicaPalette.AgedGold);
            MenuWidgets.Center(title.rectTransform, new Vector2(0, 345), new Vector2(1000, 70));
            goldText = MenuWidgets.Label("Gold", root, "", 30, MexicaPalette.AgedGold);
            MenuWidgets.Center(goldText.rectTransform, new Vector2(0, 285), new Vector2(1000, 40));

            mainPage = MenuWidgets.Rect("Main", root).gameObject;
            Vector2 size = new Vector2(720, 60);
            MenuWidgets.Button(mainPage.transform, $"Potion  -  {potionPrice} gold", new Vector2(0, 205), size, BuyPotion, 30);
            MenuWidgets.Button(mainPage.transform, $"Gamble {smallBet} gold  (double or nothing)", new Vector2(0, 135), size, () => Gamble(smallBet), 30);
            MenuWidgets.Button(mainPage.transform, $"Gamble {bigBet} gold  (double or nothing)", new Vector2(0, 65), size, () => Gamble(bigBet), 30);
            artifactButton = MenuWidgets.Button(mainPage.transform, "", new Vector2(0, -5), size, BuyArtifact, 26);
            skinsButton = MenuWidgets.Button(mainPage.transform, "Skins of Tlahuelli", new Vector2(0, -75), size, ShowSkinPage, 30);
            MenuWidgets.Button(mainPage.transform, "Leave", new Vector2(0, -145), size, Close, 30);

            BuildSkinPage(root);

            messageText = MenuWidgets.Label("Message", root, "", 27, MexicaPalette.Bone);
            MenuWidgets.Center(messageText.rectTransform, new Vector2(0, -300), new Vector2(1060, 110));
        }

        void BuildSkinPage(RectTransform root)
        {
            skinPage = MenuWidgets.Rect("Skins", root).gameObject;
            var page = skinPage.transform;
            var skin = player != null ? player.GetComponent<PlayerSkin>() : FindAnyObjectByType<PlayerSkin>();
            var catalog = skin != null ? skin.Catalog : null;

            // Preview: Tlahuelli big, walking towards us, with the skin under the cursor.
            var frame = MenuWidgets.Image("PreviewFrame", page, MexicaPalette.AgedGold);
            MenuWidgets.Center(frame.rectTransform, new Vector2(-330, 30), new Vector2(330, 400));
            var inside = MenuWidgets.Image("Inside", frame.transform, new Color(0.1f, 0.08f, 0.09f));
            MenuWidgets.Stretch(inside.rectTransform);
            inside.rectTransform.offsetMin = new Vector2(6, 6);
            inside.rectTransform.offsetMax = new Vector2(-6, -6);
            preview = MenuWidgets.Image("Preview", inside.transform, Color.white);
            MenuWidgets.Center(preview.rectTransform, new Vector2(0, 10), new Vector2(288, 300));
            preview.preserveAspect = true;
            if (catalog != null && catalog.material != null)
            {
                previewMaterial = new Material(catalog.material);
                preview.material = previewMaterial;
            }
            skinInfo = MenuWidgets.Label("Info", page, "", 24, MexicaPalette.Bone);
            MenuWidgets.Center(skinInfo.rectTransform, new Vector2(-330, -215), new Vector2(420, 90));

            skinButtons.Clear();
            if (catalog != null)
                for (int i = 0; i < catalog.skins.Length; i++)
                {
                    var s = catalog.skins[i];
                    var button = MenuWidgets.Button(page, s.displayName, new Vector2(190, 205 - i * 66), new Vector2(600, 58), () => ChooseSkin(s), 26);
                    skinButtons.Add((button, s));
                }
            MenuWidgets.Button(page, "Back", new Vector2(190, 205 - skinButtons.Count * 66), new Vector2(600, 58), ShowMainPage, 28);
        }

        // ---------------- What you can buy ----------------

        void BuyPotion()
        {
            if (player.Potions >= player.Stats.maxPotions) { Say("You can't carry more potions, stranger."); GameAudio.PlaySfx("error"); }
            else if (player.Progression.TrySpendGold(potionPrice))
            {
                player.AddPotions(1);
                Speak(buyLines, null);
                AppendSay($"A potion. You now carry {player.Potions}.");
                GameAudio.PlaySfx("potion");
            }
            else NotEnoughGold(potionPrice);
            Refresh();
        }

        void Gamble(int bet)
        {
            if (!player.Progression.TrySpendGold(bet)) { NotEnoughGold(bet); Refresh(); return; }

            bool win = Random.value < winChance;   // the bones of the dead decide
            if (win)
            {
                player.Progression.AddGold(bet * 2);
                Speak(winLines, null);
                AppendSay($"The bones fall your way! You win {bet * 2} gold.");
                GameAudio.PlaySfx("victory", 0.7f);
            }
            else
            {
                Speak(loseLines, null);
                AppendSay($"The gods laugh. You lose {bet} gold.");
                GameAudio.PlaySfx("defeat", 0.6f);
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
                Speak(buyLines, null);
                AppendSay($"The {artifact.displayName} is yours. Use it in battle with {{Artifact}}.");
                GameAudio.PlaySfx("fury");
            }
            else NotEnoughGold(artifactPrice);
            Refresh();
        }

        void ChooseSkin(SkinCatalog.Skin s)
        {
            var skin = player.GetComponent<PlayerSkin>();
            if (skin == null) return;
            if (skin.Owns(s.id))
            {
                skin.Equip(s.id);
                Say($"Tlahuelli now wears: {s.displayName}.");
                GameAudio.PlaySfx("key", 0.6f);
            }
            else if (player.Progression.TrySpendGold(s.price))
            {
                skin.Give(s.id);
                skin.Equip(s.id);
                Speak(buyLines, null);
                AppendSay($"{s.displayName} is yours, and Tlahuelli is wearing it.");
                GameAudio.PlaySfx("fury", 0.7f);
            }
            else NotEnoughGold(s.price);
            Refresh();
        }

        void NotEnoughGold(int price)
        {
            Say($"Not enough gold, stranger. It costs {price}; you have {player.Progression.Gold}.");
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
            var skin = player.GetComponent<PlayerSkin>();
            skinsButton.gameObject.SetActive(skin != null && skinButtons.Count > 0);
            if (skin == null) return;
            foreach (var (button, s) in skinButtons)
            {
                string state = s.id == skin.Equipped ? "WEARING" : skin.Owns(s.id) ? "wear" : $"{s.price} gold";
                MenuWidgets.SetLabel(button, $"{s.displayName}  -  {state}");
            }
            if (previewed == null) Preview(skin.Catalog.Find(skin.Equipped));
        }

        // ---------------- Skin preview ----------------

        void PreviewSelected()
        {
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            foreach (var (button, s) in skinButtons)
                if (button.gameObject == selected && s != previewed) Preview(s);
            if (preview != null && previewFrames != null && previewFrames.Length > 0)
                preview.sprite = previewFrames[(int)(Time.unscaledTime * 6f) % previewFrames.Length];
        }

        void Preview(SkinCatalog.Skin s)
        {
            previewed = s;
            if (s == null) return;
            SkinCatalog.Apply(previewMaterial, s);
            if (preview != null) preview.SetMaterialDirty();
            if (skinInfo != null) skinInfo.text = $"{s.displayName}\n<size=20>{s.description}</size>";
        }

        // ---------------- Talking ----------------

        /// <summary>Plays one of the lines of a moment (never the same twice in a row) and shows it as a subtitle.</summary>
        void Speak(VoiceLine[] lines, string fallback)
        {
            if (lines == null || lines.Length == 0)
            {
                Say(fallback ?? "");
                return;
            }
            lastLine.TryGetValue(lines, out int last);
            int i = Random.Range(0, lines.Length);
            if (lines.Length > 1 && i == last) i = (i + 1) % lines.Length;
            lastLine[lines] = i;
            if (lines[i].clip != null) GameAudio.PlayVoice(lines[i].clip, 1f, voicePitch);
            if (messageText != null) Say($"\"{lines[i].text}\"");
        }

        void Say(string message)
        {
            if (messageText != null) messageText.text = InputPrompts.Localize(message);
        }

        void AppendSay(string message)
        {
            if (messageText == null) return;
            string quote = messageText.text.StartsWith("\"") ? messageText.text + "\n" : "";
            messageText.text = quote + InputPrompts.Localize(message);
        }

        void ShowPrompt(string message)
        {
            if (FindAnyObjectByType<InteractPrompt>() != null)
            {
                if (message == "") InteractPrompt.Hide(this);
                else InteractPrompt.Show(this, message);
                return;
            }
            if (promptText == null) return;
            promptText.transform.parent.gameObject.SetActive(message != "");
            GlyphText.Set(promptText, message);   // "{Interact}" becomes the E / Y / triangle icon
        }
    }
}
