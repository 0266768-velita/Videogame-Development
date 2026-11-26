using System.Collections.Generic;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Marks a place on the full-screen map (M): merchants, villagers, side quests, landmarks and doors.
    /// Enemies, the boss and the player do not need one (the map finds them by itself).
    /// A quest marker can be turned off when the quest is done (Done = true shows it greyed with a tick).
    /// </summary>
    public class MapMarker : MonoBehaviour
    {
        public enum Kind { Merchant, Villager, SideQuest, Landmark, Chest, Door }

        [SerializeField] Kind kind = Kind.Landmark;
        [SerializeField] string label = "";
        [Tooltip("Optional: where it is drawn instead of its own position (an interior shown at its door).")]
        [SerializeField] Transform showAt;

        static readonly List<MapMarker> all = new List<MapMarker>();
        public static IReadOnlyList<MapMarker> All => all;

        public Kind Type => kind;
        public string Label => label;
        public bool Done { get; set; }
        public Vector3 Position => showAt != null ? showAt.position : transform.position;

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        public void Set(Kind newKind, string newLabel)
        {
            kind = newKind;
            label = newLabel;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();
    }
}
