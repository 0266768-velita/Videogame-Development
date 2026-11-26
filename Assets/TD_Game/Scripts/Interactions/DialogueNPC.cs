using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// A villager of Mictlán you can talk to. Stand close (its TalkArea trigger) and press {Interact}.
    /// Over its head floats a sign: a gold "!" while it still has something new to tell, a grey "..." once you heard it;
    /// it grows when you are close enough to talk. The first talk can be different from the next ones.
    /// Lines that start with "> " are said by Tlahuelli. Talked-to villagers are remembered in the save
    /// (ContextTutorial ids), so the "!" does not come back after loading.
    /// The talk area needs a Collider2D marked Is Trigger (on this object or a child).
    /// </summary>
    public class DialogueNPC : MonoBehaviour
    {
        [SerializeField] string npcName = "Villager";
        [Tooltip("Unique id, remembered in the save once you talked to it.")]
        [SerializeField] string id = "npc";
        [SerializeField, TextArea(2, 5)] string[] firstTalk = { "..." };
        [Tooltip("What it says the next times. Empty = repeats the first talk.")]
        [SerializeField, TextArea(2, 5)] string[] laterTalk;
        [SerializeField] SpriteRenderer body;
        [Tooltip("Picture shown in the conversation box. Empty = the body sprite.")]
        [SerializeField] Sprite portrait;
        [SerializeField] Sprite playerPortrait;

        [Header("Sign over the head")]
        [SerializeField] SpriteRenderer sign;
        [SerializeField] Sprite newSign;
        [SerializeField] Sprite talkedSign;
        [SerializeField] bool idleBob = true;

        PlayerCombatant player;
        Vector3 signBase, bodyScale;
        bool talking;

        public string NpcName => npcName;
        public bool Talked => ContextTutorial.HasSeen(SeenId);
        string SeenId => "npc_" + id;

        /// <summary>Raised after a conversation ends (quests listen to it).</summary>
        public event System.Action<DialogueNPC> Finished;

        void Awake()
        {
            if (sign != null) signBase = sign.transform.localPosition;
            if (body != null) bodyScale = body.transform.localScale;
        }

        /// <summary>Replaces what it says (quests change the lines when something happens).</summary>
        public void SetTalk(string[] first, string[] later, bool markNew)
        {
            firstTalk = first;
            laterTalk = later;
            if (markNew) ContextTutorial.Forget(SeenId);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var who = other.GetComponentInParent<PlayerCombatant>();
            if (who == null) return;
            player = who;
            InteractPrompt.Show(this, $"{{Interact}}  Talk to {npcName}");
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerCombatant>() == null) return;
            player = null;
            InteractPrompt.Hide(this);
        }

        void OnDisable()
        {
            player = null;
            InteractPrompt.Hide(this);
        }

        void Update()
        {
            AnimateSign();
            if (idleBob && body != null && !talking)
                body.transform.localScale = new Vector3(bodyScale.x, bodyScale.y * (1f + Mathf.Sin(Time.time * 2.2f + id.Length) * 0.025f), bodyScale.z);
            if (player != null && !talking && GameState.IsExploring && !DialogueBox.IsOpen && InteractPrompt.Pressed())
                StartCoroutine(Talk());
        }

        void AnimateSign()
        {
            if (sign == null) return;
            bool hasNews = !Talked;
            sign.sprite = hasNews ? newSign : talkedSign;
            sign.enabled = sign.sprite != null && !talking;
            float near = player != null ? 1.25f : 1f;
            sign.transform.localPosition = signBase + Vector3.up * (Mathf.Sin(Time.time * 3f) * 0.06f);
            sign.transform.localScale = Vector3.one * near;
        }

        IEnumerator Talk()
        {
            talking = true;
            InteractPrompt.Hide(this);
            if (body != null && player != null) body.flipX = player.transform.position.x < transform.position.x;
            if (player != null) player.Direction.Face(((Vector2)(transform.position - player.transform.position)).normalized);

            var lines = Talked && laterTalk != null && laterTalk.Length > 0 ? laterTalk : firstTalk;
            var talk = new List<DialogueBox.Line>();
            Sprite face = portrait != null ? portrait : body != null ? body.sprite : null;
            foreach (string line in lines)
            {
                if (line.StartsWith("> ")) talk.Add(new DialogueBox.Line("Tlahuelli", line.Substring(2), playerPortrait));
                else talk.Add(new DialogueBox.Line(npcName, line, face));
            }
            yield return DialogueBox.Play(talk);
            ContextTutorial.MarkSeen(new[] { SeenId });
            talking = false;
            if (player != null) InteractPrompt.Show(this, $"{{Interact}}  Talk to {npcName}");
            Finished?.Invoke(this);
        }
    }
}
