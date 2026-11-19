using System.Collections;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// The sealed door of Quetzalcóatl's chamber. It blocks the way (solid collider) until Tlahuelli touches it
    /// carrying the obsidian key; then it opens forever with a short cutscene:
    /// the key answers, the camera looks at the door, its glyphs light up one by one, the ground shakes,
    /// the stone slab rises into the lintel shedding dust and rubble, and a wave of light runs into the temple.
    /// </summary>
    public class KeyDoor : MonoBehaviour
    {
        [SerializeField] SpriteRenderer sprite;
        [SerializeField] Sprite closedSprite;
        [SerializeField] Sprite openSprite;
        [SerializeField] Collider2D blocker;   // the solid part that stops you

        [Header("Opening cutscene")]
        [SerializeField, Min(0.5f)] float slabRiseTime = 2.2f;
        [SerializeField, Min(0f)] float slabRiseHeight = 2.2f;
        [Tooltip("How far the wave of light runs into the temple (tiles, to the east).")]
        [SerializeField, Min(0f)] float lightWaveLength = 14f;

        static readonly Color GlyphLight = new Color(0.35f, 0.95f, 0.85f);

        float messageCooldown;
        bool opening;

        void Update()
        {
            if (messageCooldown > 0f) messageCooldown -= Time.deltaTime;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            var player = collision.collider.GetComponentInParent<PlayerCombatant>();
            if (player == null || opening) return;

            if (player.Progression.HasKey)
            {
                if (GameState.IsExploring) StartCoroutine(OpeningCutscene(player));
                else Open();
            }
            else if (messageCooldown <= 0f)
            {
                CombatFeed.ShowBanner("The door is sealed.\nDefeat every servant of the gods to find the obsidian key.", 3f);
                GameAudio.PlaySfx("error");
                messageCooldown = 3f;
            }
        }

        /// <summary>Opens at once, without the cutscene (also after loading a save where the player has the key).</summary>
        public void Open()
        {
            if (!blocker.enabled) return;
            blocker.enabled = false;
            sprite.sprite = openSprite;
            sprite.color = Color.white;
        }

        IEnumerator OpeningCutscene(PlayerCombatant player)
        {
            opening = true;
            GameState.SetMode(GameMode.Cutscene);
            var cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            Transform previousTarget = cam != null ? cam.Target : null;
            Vector3 door = transform.position;
            Vector3 middle = door + Vector3.up;

            // 1. The obsidian key answers: it shines over Tlahuelli's head and flies into the door.
            Vector3 head = player.transform.position + Vector3.up * 1.4f;
            GameAudio.PlaySfx("key");
            Vfx.Ring(head, MexicaPalette.Obsidian, 0.1f, 0.8f, 0.4f);
            Vfx.Ring(head, GlyphLight, 0.1f, 1.1f, 0.5f);
            Vfx.Wisps(player.transform.position, 8, GlyphLight, 1.6f, 0.4f);
            yield return new WaitForSeconds(0.5f);
            if (cam != null) cam.Follow(transform);   // the camera glides to the door
            yield return Vfx.Projectile(head, middle, GlyphLight, 0.6f);
            Vfx.Ring(middle, GlyphLight, 0.2f, 1.6f, 0.45f);
            Vfx.Burst(middle, 14, GlyphLight, new Vector2(2f, 4.5f), 0f, 1f, 0.5f, toGround: false, upward: 0f);
            yield return new WaitForSeconds(0.4f);

            // 2. The glyphs light up, from the bottom to the top, three pulses that grow.
            for (int i = 0; i < 3; i++)
            {
                GameAudio.PlaySfx("tip", 0.8f, 0f);
                Vector3 glyph = door + Vector3.up * (0.4f + i * 0.6f);
                Vfx.Ring(glyph, GlyphLight, 0.1f, 0.7f + i * 0.3f, 0.35f);
                Vfx.Wisps(glyph, 3, GlyphLight, 0.8f, 0.3f);
                for (float t = 0f; t < 0.35f; t += Time.deltaTime)
                {
                    sprite.color = Color.Lerp(Color.white, GlyphLight, Mathf.Sin(t / 0.35f * Mathf.PI) * (0.4f + i * 0.2f));
                    yield return null;
                }
                sprite.color = Color.white;
                yield return new WaitForSeconds(0.12f);
            }

            // 3. The ground rumbles: the door trembles and dust falls from the lintel.
            GameAudio.PlaySfx("seismic");
            if (cam != null) cam.Shake(0.08f, 1f);
            Vector3 home = transform.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime)
            {
                transform.position = home + (Vector3)(Random.insideUnitCircle * 0.04f);
                if (Random.value < 0.3f)
                    Vfx.Burst(door + new Vector3(Random.Range(-0.5f, 0.5f), 2.1f), 2, MexicaPalette.Ash, new Vector2(0.2f, 0.8f), 9f, 0.6f, 0.8f, toGround: true, upward: 0f);
                yield return null;
            }
            transform.position = home;

            // 4. The stone slab rises into the lintel. The open doorway stays behind it.
            var slab = new GameObject("RisingSlab").AddComponent<SpriteRenderer>();
            slab.sprite = closedSprite;
            slab.sortingLayerID = sprite.sortingLayerID;
            slab.sortingOrder = sprite.sortingOrder + 1;
            slab.spriteSortPoint = sprite.spriteSortPoint;
            slab.transform.position = home;
            sprite.sprite = openSprite;
            blocker.enabled = false;
            GameAudio.PlaySfx("door");
            if (cam != null) cam.Shake(0.12f, slabRiseTime);
            for (float t = 0f; t < slabRiseTime; t += Time.deltaTime)
            {
                float k = t / slabRiseTime;
                // It rises in heavy jerks, as if someone pulled it with ropes.
                float jerk = Mathf.SmoothStep(0f, 1f, k) + Mathf.Sin(k * Mathf.PI * 6f) * 0.02f;
                slab.transform.position = home + Vector3.up * (jerk * slabRiseHeight) + (Vector3)(Random.insideUnitCircle * 0.02f);
                slab.color = new Color(1f, 1f, 1f, 1f - Mathf.SmoothStep(0.55f, 1f, k));
                if (Random.value < 0.35f)
                    Vfx.Dust(door + new Vector3(Random.Range(-0.6f, 0.6f), 0.05f), 2, MexicaPalette.Ash, 0.8f);
                if (Random.value < 0.12f)
                    Vfx.Burst(slab.transform.position + new Vector3(Random.Range(-0.4f, 0.4f), 0.1f), 3, MexicaPalette.Ash,
                        new Vector2(0.5f, 1.5f), 14f, 0.8f, 0.9f, toGround: true, upward: 0.3f);
                yield return null;
            }
            Destroy(slab.gameObject);
            Vfx.Dust(door, 12, MexicaPalette.Ash, 1.6f);
            Vfx.Ring(door, MexicaPalette.Ash, 0.3f, 2.2f, 0.5f, onGround: true, squashY: 0.4f);
            if (cam != null) cam.Shake(0.2f, 0.3f);
            GameAudio.PlaySfx("land");

            // 5. A wave of light runs into the temple: the Celestial Temple wakes up.
            yield return new WaitForSeconds(0.3f);
            GameAudio.PlaySfx("roar", 0.7f, 0f);
            for (float x = 1f; x <= lightWaveLength; x += 1.5f)
            {
                Vector3 p = door + new Vector3(x, 0.1f, 0f);
                Vfx.Ring(p, GlyphLight, 0.1f, 1.2f, 0.45f, onGround: true, squashY: 0.45f);
                Vfx.Wisps(p, 2, GlyphLight, 1.2f, 0.4f);
                yield return new WaitForSeconds(0.07f);
            }
            CombatFeed.ShowBanner("THE CELESTIAL TEMPLE IS OPEN\nQuetzalcóatl awaits.", 3f);
            yield return new WaitForSeconds(1.2f);

            if (cam != null) cam.Follow(previousTarget != null ? previousTarget : player.transform);
            yield return new WaitForSeconds(0.6f);
            GameState.SetMode(GameMode.Exploration);
            opening = false;
        }

        void Start()
        {
            sprite.sprite = closedSprite;
            Invoke(nameof(OpenIfLoadedWithKey), 0.3f);   // wait for the save to load
        }

        void OpenIfLoadedWithKey()
        {
            var player = FindAnyObjectByType<PlayerCombatant>();
            if (player != null && player.Progression.HasKey) Open();
        }
    }
}
