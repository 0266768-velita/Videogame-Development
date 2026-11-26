using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// The skins of Tlahuelli the merchants sell: the same warrior, his clothes dyed in other colours.
    /// Each skin changes the hue, saturation and brightness of his blue clothes (shader TD_Game/SpriteSkin).
    /// Create with: Create > TD_Game > Skin Catalog. The first skin is the one he starts with (free).
    /// </summary>
    [CreateAssetMenu(menuName = "TD_Game/Skin Catalog", fileName = "SkinCatalog")]
    public class SkinCatalog : ScriptableObject
    {
        [System.Serializable]
        public class Skin
        {
            public string id = "default";
            public string displayName = "Warrior of Mictlán";
            [TextArea] public string description = "";
            [Min(0)] public int price;
            [Range(-1f, 1f)] public float hueShift;
            [Range(0f, 2f)] public float saturation = 1f;
            [Range(0f, 2f)] public float brightness = 1f;
        }

        public Material material;
        public Skin[] skins;

        public Skin Find(string id)
        {
            if (skins == null || skins.Length == 0) return null;
            foreach (var s in skins)
                if (s != null && s.id == id) return s;
            return skins[0];
        }

        public static void Apply(Material target, Skin skin)
        {
            if (target == null || skin == null) return;
            target.SetFloat("_HueShift", skin.hueShift);
            target.SetFloat("_SatMul", skin.saturation);
            target.SetFloat("_ValMul", skin.brightness);
        }
    }
}
