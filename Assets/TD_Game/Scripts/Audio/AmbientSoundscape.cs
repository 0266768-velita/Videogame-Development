using UnityEngine;
using UnityEngine.Tilemaps;

namespace TD_Game
{
    /// <summary>
    /// The sound of each place, under the music: leaves and birds in the forests, the river when water is near,
    /// the dry wind of the desert of the dead and a low hum inside houses. Each sound loops on its own source and
    /// fades in and out with where Tlahuelli walks (it looks at the ground and the water around him).
    /// Quieter during battles. Its volume follows the effects volume of the Options menu.
    /// </summary>
    public class AmbientSoundscape : MonoBehaviour
    {
        [SerializeField] AudioClip forest;
        [SerializeField] AudioClip river;
        [SerializeField] AudioClip desertWind;
        [SerializeField] AudioClip interior;
        [SerializeField, Range(0f, 1f)] float volume = 0.45f;
        [SerializeField, Range(0f, 1f)] float battleVolume = 0.35f;
        [Tooltip("Tiles around the player that are checked for water.")]
        [SerializeField, Min(1f)] float waterHearing = 6f;

        Transform player;
        Tilemap ground, water;
        AudioSource forestSource, riverSource, windSource, interiorSource;
        float forestTarget, riverTarget, windTarget, interiorTarget, nextSample;

        void Start()
        {
            var p = FindAnyObjectByType<PlayerCombatant>();
            if (p != null) player = p.transform;
            foreach (var map in FindObjectsByType<Tilemap>())
            {
                if (map.name == "Ground") ground = map;
                else if (map.name == "Water") water = map;
            }
            forestSource = GameAudio.LoopSource(forest);
            riverSource = GameAudio.LoopSource(river);
            windSource = GameAudio.LoopSource(desertWind);
            interiorSource = GameAudio.LoopSource(interior);
            // Start the loops at different points so they never sound in step.
            foreach (var s in new[] { forestSource, riverSource, windSource, interiorSource })
                if (s != null && s.clip != null) s.time = Random.Range(0f, s.clip.length * 0.9f);
        }

        void OnDestroy()
        {
            foreach (var s in new[] { forestSource, riverSource, windSource, interiorSource })
                if (s != null) Destroy(s);
        }

        void Update()
        {
            if (player == null) return;
            if (Time.unscaledTime >= nextSample)
            {
                nextSample = Time.unscaledTime + 0.3f;
                Sample();
            }
            float master = volume * GameAudio.SfxVolume * (GameState.Mode == GameMode.Combat ? battleVolume : 1f);
            Fade(forestSource, forestTarget * master);
            Fade(riverSource, riverTarget * master);
            Fade(windSource, windTarget * master);
            Fade(interiorSource, interiorTarget * master);
        }

        static void Fade(AudioSource source, float target)
        {
            if (source != null) source.volume = Mathf.MoveTowards(source.volume, target, Time.unscaledDeltaTime * 0.35f);
        }

        void Sample()
        {
            if (HouseDoor.InInterior)
            {
                forestTarget = riverTarget = windTarget = 0f;
                interiorTarget = 1f;
                return;
            }
            interiorTarget = 0f;

            // Ground around the player: grass = forest, sand = desert.
            int grass = 0, sand = 0, total = 0;
            Vector3 p = player.position;
            for (int dx = -3; dx <= 3; dx++)
                for (int dy = -3; dy <= 3; dy++)
                {
                    if (ground == null) break;
                    var tile = ground.GetTile(ground.WorldToCell(p + new Vector3(dx * 2f, dy * 2f)));
                    if (tile == null) continue;
                    total++;
                    if (tile.name.StartsWith("Sand")) sand++;
                    else if (tile.name.Contains("rass") || tile.name.Contains("lowers")) grass++;
                }
            float desert = total > 0 ? sand / (float)total : 0f;
            forestTarget = total > 0 ? Mathf.Clamp01(grass / (float)total * 1.2f) : 0f;
            windTarget = Mathf.Lerp(0.25f, 1f, desert);   // there is always a little wind

            // The closest water within hearing.
            float nearest = float.MaxValue;
            if (water != null)
                for (int i = 0; i < 24; i++)
                {
                    float angle = i * Mathf.PI * 2f / 8f;
                    float radius = waterHearing * (1 + i / 8) / 3f;
                    var probe = p + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    if (water.HasTile(water.WorldToCell(probe))) nearest = Mathf.Min(nearest, radius);
                }
            if (water != null && water.HasTile(water.WorldToCell(p))) nearest = 0f;
            riverTarget = nearest == float.MaxValue ? 0f : Mathf.Clamp01(1.15f - nearest / waterHearing);
        }
    }
}
