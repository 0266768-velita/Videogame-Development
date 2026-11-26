using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// Tutorials in context (task 4.18): a short hint the FIRST time something happens
    /// (start, first battle, first dodge, first spell, first orbs, first new skill, first defeat).
    /// Each hint appears only once; the seen ones are stored in the save.
    /// TutorialZone (a trigger on the map) can show extra hints with ShowOnce.
    /// </summary>
    public class ContextTutorial : MonoBehaviour
    {
        [SerializeField] CanvasGroup panel;
        [SerializeField] Text text;
        [SerializeField, Min(0.5f)] float secondsPerHint = 5f;
        [SerializeField, Min(0.01f)] float fadeTime = 0.25f;
        [Tooltip("During a battle the box moves to the bottom centre (units of the canvas, from the bottom edge), so it never " +
                 "covers the turn order, the name of the enemy attack or the fighters.")]
        [SerializeField] Vector2 battlePosition = new Vector2(-120f, 62f);   // a bit left: clear of the command menu

        [Header("Texts")]
        [SerializeField, TextArea] string startHint = "{Move} move   ·   {Sprint} sprint   ·   {Jump} jump\nLand on an enemy from a jump to AMBUSH it.";
        [SerializeField, TextArea] string battleControlsHint = "{Attack} attack   ·   {Skill1} {Skill2} skills   ·   {Potion} potion   ·   {Ultimate} Tonalli Burst   ·   {Artifact} artifact   ·   {EndTurn} end turn\nWhile attacking, press {Attack} again right before the blow for a CRITICAL. {Target} (or click an enemy) chooses the target.";
        [SerializeField, TextArea] string quickTimeDodgeHint = "The enemy glows red before it strikes.\nPress {Dodge} right before the hit: no damage and a counter-attack. Too early = the button locks.";
        [SerializeField, TextArea] string quickTimeInterruptHint = "Spells cannot be dodged: INTERRUPT them.\nPress {Interrupt} right before the spell is cast.";
        [SerializeField, TextArea] string orbsHint = "Walk over the orbs before leaving:\ngold = Macuahuitl charge   ·   green = life   ·   blue = Tonalli";
        [SerializeField, TextArea] string unlockHint = "You learned a new skill. Use it in battle with {Skill1} or {Skill2}.";
        [SerializeField, TextArea] string backToStartHint = "When you fall, you go back to the BEGINNING of the map.\nYou keep your skills, gold and growth, and you always get potions to retry.";

        [Header("Tips while exploring (one every few seconds, each only once)")]
        [SerializeField, Min(5f)] float secondsBetweenTips = 25f;
        [SerializeField, TextArea] string[] explorationTips =
        {
            "The map of the whole level is in the top right corner. The red dot is you.",
            "Enemies chase you when they see you. Walls hide you from them.",
            "Defeated enemies drop gold. The merchant in the Valley of Tlazolteotl sells potions and an artifact.",
            "Green orbs heal you, blue orbs fill your Tonalli. Collect them after every battle.",
            "Defeat every servant of the gods: the obsidian key opens the door to Quetzalcoatl.",
            "Press {Pause} to pause the game and change the volume.",
        };

        static readonly HashSet<string> seen = new HashSet<string>();
        static ContextTutorial instance;

        /// <summary>Ids of the hints already shown (for the save).</summary>
        public static IEnumerable<string> Seen => seen;

        /// <summary>True if this id was already shown or done (also used to remember the solved puzzle).</summary>
        public static bool HasSeen(string id) => seen.Contains(id);

        /// <summary>New game: every hint can appear again.</summary>
        public static void ResetAll() => seen.Clear();

        /// <summary>Forgets one id (a villager with something new to say).</summary>
        public static void Forget(string id) => seen.Remove(id);

        public static void MarkSeen(IEnumerable<string> ids)
        {
            foreach (string id in ids) seen.Add(id);
        }

        /// <summary>Shows a hint if it was never shown before. Safe to call without a ContextTutorial in the scene.</summary>
        public static void ShowOnce(string id, string message)
        {
            if (string.IsNullOrEmpty(id) || seen.Contains(id) || instance == null) return;
            seen.Add(id);
            instance.queue.Enqueue(message);
        }

        readonly Queue<string> queue = new Queue<string>();
        PlayerProgression progression;
        RectTransform box;
        Vector2 homeAnchor, homePivot, homePosition;
        bool inBattlePlace;

        void Awake()
        {
            instance = this;
            if (panel != null) panel.alpha = 0f;
        }

        void LateUpdate()
        {
            bool battle = GameState.Mode == GameMode.Combat;
            if (battle != inBattlePlace) PlaceBox(battle);
        }

        /// <summary>Top centre while exploring (as built), bottom centre during a battle.</summary>
        void PlaceBox(bool battle)
        {
            if (panel == null) return;
            if (box == null)
            {
                box = (RectTransform)panel.transform;
                homeAnchor = box.anchorMin; homePivot = box.pivot; homePosition = box.anchoredPosition;
            }
            inBattlePlace = battle;
            var bottom = new Vector2(0.5f, 0f);
            box.anchorMin = box.anchorMax = battle ? bottom : homeAnchor;
            box.pivot = battle ? bottom : homePivot;
            box.anchoredPosition = battle ? battlePosition : homePosition;
        }

        void OnEnable()
        {
            CombatManager.CombatStarted += OnCombatStarted;
            CombatManager.CombatWon += OnCombatWon;
            CombatManager.CombatLost += OnCombatLost;
            CombatFeed.CounterPrompt += OnCounterPrompt;
        }

        void OnDisable()
        {
            CombatManager.CombatStarted -= OnCombatStarted;
            CombatManager.CombatWon -= OnCombatWon;
            CombatManager.CombatLost -= OnCombatLost;
            CombatFeed.CounterPrompt -= OnCounterPrompt;
            if (progression != null) progression.AbilityUnlocked -= OnAbilityUnlocked;
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        IEnumerator Start()
        {
            var player = FindAnyObjectByType<PlayerCombatant>();
            if (player != null)
            {
                progression = player.GetComponent<PlayerProgression>();
                if (progression != null) progression.AbilityUnlocked += OnAbilityUnlocked;
            }
            // Wait a moment so a loaded save can mark the hints as seen first.
            yield return null;
            ShowOnce("start", startHint);
            StartCoroutine(ShowQueue());
            StartCoroutine(ExplorationTips());
        }

        /// <summary>Every few seconds of exploring, the next tip that was not shown yet.</summary>
        IEnumerator ExplorationTips()
        {
            for (int i = 0; i < explorationTips.Length; i++)
            {
                float explored = 0f;
                while (explored < secondsBetweenTips)
                {
                    if (GameState.IsExploring) explored += Time.deltaTime;
                    yield return null;
                }
                ShowOnce("tip" + i, explorationTips[i]);
            }
        }

        void OnCombatStarted() => ShowOnce("combat", battleControlsHint);
        void OnCombatWon(IReadOnlyList<EnemyData> _) => ShowOnce("orbs", orbsHint);
        void OnCombatLost() => ShowOnce("defeat", backToStartHint);
        void OnAbilityUnlocked(CombatAbility _) => ShowOnce("unlock", unlockHint);

        void OnCounterPrompt(string prompt)
        {
            if (string.IsNullOrEmpty(prompt)) return;
            if (prompt.Contains("INTERRUPT")) ShowOnce("interrupt", quickTimeInterruptHint);
            else if (prompt.Contains("DODGE")) ShowOnce("dodge", quickTimeDodgeHint);
        }

        IEnumerator ShowQueue()
        {
            while (true)
            {
                while (queue.Count == 0) yield return null;
                if (text != null) text.text = InputPrompts.Localize(queue.Dequeue());   // {Dodge} -> [SPACE] / [B] / [CIRCLE]
                GameAudio.PlaySfx("tip", 0.6f, 0f);
                yield return Fade(1f);
                yield return new WaitForSecondsRealtime(secondsPerHint);
                yield return Fade(0f);
            }
        }

        IEnumerator Fade(float to)
        {
            if (panel == null) yield break;
            while (!Mathf.Approximately(panel.alpha, to))
            {
                panel.alpha = Mathf.MoveTowards(panel.alpha, to, Time.unscaledDeltaTime / fadeTime);
                yield return null;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            seen.Clear();
            instance = null;
        }
    }
}
