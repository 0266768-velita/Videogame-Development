using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// The game's colour palette: serious, mature and Mexica. It takes the materials of the
    /// Aztec world instead of saturated "video game" colours:
    /// obsidian (black volcanic glass), bone and ash (Mictlan, the underworld), aged gold and ochre,
    /// cinnabar and dried blood (the red of the codices and the sacrifice), jade (life) and
    /// turquoise / xiuhuitl (the precious, the divine: tonalli).
    /// Every UI and combat colour should come from here so the whole game stays coherent.
    /// </summary>
    public static class MexicaPalette
    {
        // ---- Base materials ----
        public static readonly Color Obsidian = new Color(0.022f, 0.018f, 0.02f, 1f);
        public static readonly Color ObsidianPlate = new Color(0.04f, 0.03f, 0.03f, 1f);
        public static readonly Color Tezontle = new Color(0.19f, 0.04f, 0.035f, 1f);     // dark volcanic red stone
        public static readonly Color Bone = new Color(0.8f, 0.75f, 0.65f, 1f);
        public static readonly Color Ash = new Color(0.46f, 0.43f, 0.39f, 1f);
        public static readonly Color AgedGold = new Color(0.6f, 0.45f, 0.22f, 1f);
        public static readonly Color DarkGold = new Color(0.33f, 0.24f, 0.11f, 1f);

        // ---- Resources ----
        public static readonly Color Cinnabar = new Color(0.47f, 0.07f, 0.055f, 1f);      // health
        public static readonly Color Ochre = new Color(0.52f, 0.34f, 0.13f, 1f);         // stamina
        public static readonly Color Turquoise = new Color(0.13f, 0.41f, 0.42f, 1f);    // tonalli
        public static readonly Color Jade = new Color(0.12f, 0.37f, 0.25f, 1f);           // healing

        // ---- Combat feedback ----
        public static readonly Color Blood = new Color(0.55f, 0.035f, 0.025f, 1f);        // enemy warning
        public static readonly Color HitFlash = new Color(0.84f, 0.79f, 0.68f, 1f);       // bone-white impact
        public static readonly Color Damage = new Color(0.7f, 0.15f, 0.11f, 1f);        // damage taken
        public static readonly Color Critical = new Color(0.76f, 0.56f, 0.25f, 1f);     // aged gold, brighter
        public static readonly Color Deathly = new Color(0.66f, 0.64f, 0.58f, 1f);       // Mictlan Fury (Mictlantecuhtli)
        public static readonly Color Shortage = new Color(0.66f, 0.23f, 0.17f, 1f);

        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
