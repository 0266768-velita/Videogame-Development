using System.Collections.Generic;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// The skins Tlahuelli owns and the one he wears. Bought from any merchant (they all share this list), kept in the
    /// save, and shown on his sprite with a copy of the SpriteSkin material (walking, combat poses and all).
    /// </summary>
    public class PlayerSkin : MonoBehaviour
    {
        [SerializeField] SkinCatalog catalog;
        [SerializeField] SpriteRenderer target;

        readonly HashSet<string> owned = new HashSet<string>();
        Material material;

        public SkinCatalog Catalog => catalog;
        public string Equipped { get; private set; }
        public IEnumerable<string> Owned => owned;

        public event System.Action Changed;

        void Awake()
        {
            if (catalog == null || catalog.skins == null || catalog.skins.Length == 0) { enabled = false; return; }
            owned.Add(catalog.skins[0].id);
            Equipped = catalog.skins[0].id;
            if (target != null && catalog.material != null)
            {
                material = new Material(catalog.material);
                target.sharedMaterial = material;
            }
            Refresh();
        }

        public bool Owns(string id) => owned.Contains(id);

        public void Give(string id)
        {
            if (catalog.Find(id) != null) owned.Add(id);
        }

        public void Equip(string id)
        {
            if (!owned.Contains(id)) return;
            Equipped = id;
            Refresh();
        }

        /// <summary>From the save: what he owned and what he wore.</summary>
        public void Load(IEnumerable<string> ownedIds, string equipped)
        {
            if (ownedIds != null) foreach (string id in ownedIds) Give(id);
            if (!string.IsNullOrEmpty(equipped)) Equip(equipped);
        }

        void Refresh()
        {
            SkinCatalog.Apply(material, catalog.Find(Equipped));
            Changed?.Invoke();
        }
    }
}
