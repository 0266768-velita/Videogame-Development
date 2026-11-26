using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TD_Game
{
    /// <summary>
    /// The door of a house you can walk into. Its trigger takes Tlahuelli to the other side with a short fade:
    /// the inside of a house is a separate room far from the map (its own Tilemap), so the camera switches to the
    /// edges of that room and the world map shows him at the door outside.
    /// Put one door outside (Leads Inside on) and one inside, each pointing at an arrival point next to the other.
    /// After loading a save inside a house, or after a defeat inside (you wake up at the start of the map), the
    /// camera and the map are fixed by themselves.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class HouseDoor : MonoBehaviour
    {
        [SerializeField] Transform destination;
        [Tooltip("The Tilemap of the place you arrive at (the room's floor, or the map's Ground). The camera stays inside it.")]
        [SerializeField] Tilemap destinationBounds;
        [SerializeField] bool leadsInside = true;
        [Tooltip("Inside: where the world map draws Tlahuelli (the door outside).")]
        [SerializeField] Transform mapPosition;
        [Tooltip("Shown the first time you go through (empty = nothing).")]
        [SerializeField, TextArea] string firstTimeBanner = "";
        [SerializeField] string id = "house";

        /// <summary>True while Tlahuelli is inside a house.</summary>
        public static bool InInterior { get; private set; }
        public static event System.Action<HouseDoor, bool> Crossed;

        bool busy;

        public bool LeadsInside => leadsInside;
        public string Id => id;

        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        void OnEnable()
        {
            if (leadsInside) CombatManager.CombatLost += OnCombatLost;
        }

        void OnDisable()
        {
            if (leadsInside) CombatManager.CombatLost -= OnCombatLost;
        }

        void Start()
        {
            if (leadsInside) Invoke(nameof(CheckLoadedInside), 0.5f);   // after the save has moved the player
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerCombatant>();
            if (player == null || busy || !GameState.IsExploring) return;
            StartCoroutine(Cross(player));
        }

        IEnumerator Cross(PlayerCombatant player)
        {
            busy = true;
            GameState.SetMode(GameMode.Cutscene);
            player.Body.linearVelocity = Vector2.zero;
            GameAudio.PlaySfx("door", 0.7f, 0.05f);
            yield return ScreenFade.To(1f, 0.3f);

            player.Teleport(destination.position);
            Arrive(player, leadsInside);
            yield return new WaitForSeconds(0.15f);
            yield return ScreenFade.To(0f, 0.35f);

            GameState.SetMode(GameMode.Exploration);
            if (!string.IsNullOrEmpty(firstTimeBanner) && !ContextTutorial.HasSeen("door_" + id))
            {
                ContextTutorial.MarkSeen(new[] { "door_" + id });
                CombatFeed.ShowBanner(firstTimeBanner, 3.5f);
            }
            busy = false;
            Crossed?.Invoke(this, leadsInside);
        }

        void Arrive(PlayerCombatant player, bool inside)
        {
            InInterior = inside;
            WorldMapScreen.PlayerPositionOverride = inside && mapPosition != null ? mapPosition.position : (Vector3?)null;
            var cam = FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.SetBounds(destinationBounds);
            var hazards = player.GetComponent<PlayerHazards>();
            if (hazards != null) hazards.ResetSafeSpot();
            var minimap = FindAnyObjectByType<Minimap>();
            if (minimap != null) minimap.gameObject.SetActive(!inside);
        }

        /// <summary>A save loaded with the player inside this house: switch the camera to the room.</summary>
        void CheckLoadedInside()
        {
            var player = FindAnyObjectByType<PlayerCombatant>();
            if (player == null || destinationBounds == null) return;
            destinationBounds.CompressBounds();
            var cells = destinationBounds.cellBounds;
            var min = destinationBounds.CellToWorld(cells.min);
            var max = destinationBounds.CellToWorld(cells.max);
            var p = player.transform.position;
            if (p.x >= min.x && p.x <= max.x && p.y >= min.y && p.y <= max.y) Arrive(player, true);
        }

        /// <summary>Defeated inside: the player wakes up at the start of the map, so the camera goes back to the map.</summary>
        void OnCombatLost()
        {
            if (!InInterior) return;
            var player = FindAnyObjectByType<PlayerCombatant>();
            Tilemap map = null;
            foreach (var t in FindObjectsByType<Tilemap>())
                if (t.name == "Ground") map = t;
            InInterior = false;
            WorldMapScreen.PlayerPositionOverride = null;
            var cam = FindAnyObjectByType<CameraFollow>();
            if (cam != null && map != null) cam.SetBounds(map);
            var minimap = FindAnyObjectByType<Minimap>(FindObjectsInactive.Include);
            if (minimap != null) minimap.gameObject.SetActive(true);
            if (player != null && player.GetComponent<PlayerHazards>() != null) player.GetComponent<PlayerHazards>().ResetSafeSpot();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            InInterior = false;
            Crossed = null;
        }
    }
}
