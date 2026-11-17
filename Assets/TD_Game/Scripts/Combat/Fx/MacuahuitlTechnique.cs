using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// One Macuahuitl blow. Angles are in degrees with 0 = blade up and positive = turning back
    /// (counter-clockwise when facing right): 90 = behind, 180 = down, 270 (or -90) = forward.
    /// The weapon travels from windAngle to endAngle in the direction given by the sign of the difference.
    /// </summary>
    [System.Serializable]
    public class MacuahuitlTechnique
    {
        public string name = "Cleave";
        [Tooltip("Where the weapon is raised to during the wind-up.")]
        public float windAngle = 70f;
        [Tooltip("Where the blow ends (the weapon passes through the enemy on the way).")]
        public float endAngle = -130f;
        [Min(0.03f)] public float swingTime = 0.11f;
        [Tooltip("Fraction of the swing at which the blade hits.")]
        [Range(0.3f, 1f)] public float impactAt = 0.7f;
        [Tooltip("Rotation of the slash effect at the impact (0 = horizontal).")]
        public float slashAngle = -40f;
        [Tooltip("Height of the slash effect above the enemy's feet (tiles).")]
        public float slashHeight = 0.5f;
        [Tooltip("Little jump during the blow (rising cuts).")]
        public bool hop;
        [Tooltip("The whole body spins with the weapon (whirlwind).")]
        public bool spin;
        [Tooltip("Shakes the ground: obsidian sparks and a small crack (heavy blows).")]
        public bool heavy;

        public MacuahuitlTechnique() { }

        public MacuahuitlTechnique(string name, float wind, float end, float time, float slash, float height,
                               bool hop = false, bool spin = false, bool heavy = false)
        {
            this.name = name; windAngle = wind; endAngle = end; swingTime = time;
            slashAngle = slash; slashHeight = height; this.hop = hop; this.spin = spin; this.heavy = heavy;
        }

        // ---- The warrior's repertoire (combo order) ----
        /// <summary>Overhead diagonal cut.</summary>
        public static MacuahuitlTechnique Cleave => new MacuahuitlTechnique("Cleave", 70f, -130f, 0.11f, -40f, 0.55f);
        /// <summary>Low sweep at the legs, passing under.</summary>
        public static MacuahuitlTechnique Sweep => new MacuahuitlTechnique("Low Sweep", 150f, 262f, 0.12f, -8f, 0.25f);
        /// <summary>Rising cut with a small jump.</summary>
        public static MacuahuitlTechnique RisingCut => new MacuahuitlTechnique("Rising Cut", 205f, 345f, 0.11f, 40f, 0.6f, hop: true);
        /// <summary>Whirlwind: the body turns with the weapon.</summary>
        public static MacuahuitlTechnique Whirlwind => new MacuahuitlTechnique("Whirlwind", 60f, -300f, 0.24f, 0f, 0.45f, spin: true);
        /// <summary>Obsidian strike: overhead smash that cracks the ground (critical hits, ambush, Seismic).</summary>
        public static MacuahuitlTechnique ObsidianStrike => new MacuahuitlTechnique("Obsidian Strike", 85f, -165f, 0.08f, -60f, 0.45f, heavy: true);
        /// <summary>Fast counter cut after a PERFECT.</summary>
        public static MacuahuitlTechnique Backhand => new MacuahuitlTechnique("Backhand", 40f, -110f, 0.08f, -25f, 0.5f);
    }
}
