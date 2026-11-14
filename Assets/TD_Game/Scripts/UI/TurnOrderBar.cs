using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// Turn order at the top of the screen: the player, then every living enemy, in the order they act.
    /// The one acting now is bright and bigger; the rest are dimmed.
    /// </summary>
    public class TurnOrderBar : MonoBehaviour
    {
        [SerializeField] CombatManager combat;
        [SerializeField] string playerName = "Tlahuelli";
        [Tooltip("Inactive child plate (Image + child Text) used as the template of each entry.")]
        [SerializeField] RectTransform template;
        [SerializeField] Color activePlate = MexicaPalette.Tezontle;
        [SerializeField] Color idlePlate = MexicaPalette.ObsidianPlate;
        [SerializeField] Color activeText = MexicaPalette.Bone;
        [SerializeField] Color idleText = MexicaPalette.Ash;

        readonly List<RectTransform> entries = new List<RectTransform>();

        void Awake()
        {
            if (template != null) template.gameObject.SetActive(false);
        }

        void Update()
        {
            var context = combat != null ? combat.Context : null;
            int used = 0;
            if (context != null)
            {
                bool playerActive = combat.CurrentEnemy == null;
                Set(used++, playerName, playerActive);
                foreach (var enemy in context.Enemies)
                    if (enemy != null && !enemy.Health.IsDead)
                        Set(used++, enemy.Data.displayName, enemy == combat.CurrentEnemy);
            }
            for (int i = used; i < entries.Count; i++) entries[i].gameObject.SetActive(false);
        }

        void Set(int index, string label, bool active)
        {
            while (entries.Count <= index)
            {
                var entry = Instantiate(template, template.parent);
                entries.Add(entry);
            }
            var e = entries[index];
            e.gameObject.SetActive(true);
            e.SetSiblingIndex(index + 1);
            e.localScale = Vector3.one * (active ? 1.12f : 1f);
            e.GetComponent<Image>().color = active ? activePlate : idlePlate;
            var text = e.GetComponentInChildren<Text>();
            text.text = (index == 0 ? "" : "▸ ") + label.ToUpperInvariant();
            text.color = active ? activeText : idleText;
        }
    }
}
