using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Something on the map you can look at or use with {Interact}: carved glyphs, a painting, an altar.
    /// It can read some lines in the conversation box (lines starting with "> " are Tlahuelli's thoughts) and/or
    /// tell a quest that it was used (Used). Needs a Collider2D marked Is Trigger (on this object).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class InteractSpot : MonoBehaviour
    {
        [SerializeField] string prompt = "{Interact}  Look";
        [SerializeField] string speaker = "";
        [SerializeField, TextArea(2, 5)] string[] lines;
        [SerializeField] Sprite portrait;

        public event System.Action<InteractSpot> Used;

        PlayerCombatant player;
        bool busy;

        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        public void SetPrompt(string text)
        {
            prompt = text;
            if (player != null) InteractPrompt.Show(this, prompt);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var who = other.GetComponentInParent<PlayerCombatant>();
            if (who == null) return;
            player = who;
            InteractPrompt.Show(this, prompt);
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
            if (player != null && !busy && GameState.IsExploring && !DialogueBox.IsOpen && InteractPrompt.Pressed())
                StartCoroutine(Use());
        }

        IEnumerator Use()
        {
            busy = true;
            InteractPrompt.Hide(this);
            if (lines != null && lines.Length > 0)
            {
                var talk = new List<DialogueBox.Line>();
                foreach (string line in lines)
                    talk.Add(line.StartsWith("> ") ? new DialogueBox.Line("Tlahuelli", line.Substring(2)) : new DialogueBox.Line(speaker, line, portrait));
                yield return DialogueBox.Play(talk);
            }
            Used?.Invoke(this);
            yield return null;
            busy = false;
            if (player != null && isActiveAndEnabled) InteractPrompt.Show(this, prompt);
        }
    }
}
