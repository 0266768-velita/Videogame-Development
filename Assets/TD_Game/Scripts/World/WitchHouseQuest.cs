using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Side quest "The Prisoner of the Four Witches", in the house hidden at the west edge of the Valley of Tlazoltéotl.
    ///   1. Inside, a man hangs in chains from the wall, guarded by four witches (one battle against all four).
    ///   2. With the witches dead, the prisoner explains that the chains are bound to a torn painting on the wall.
    ///   3. The painting opens its own puzzle screen (PaintingPuzzle). Whole again, it breaks the spell:
    ///      the chains burst and the prisoner (Painal, runner of the gods) falls free.
    ///   4. He tells his story and gives Tlahuelli the Amulet of Painal: Super Speed (he walks and runs faster).
    /// Everything is remembered in the save (ContextTutorial ids "witch_puzzle_solved", "painal_freed"; the witches
    /// are remembered as defeated enemies), so the prisoner is never chained again and the reward is given once.
    /// </summary>
    public class WitchHouseQuest : MonoBehaviour
    {
        public const string SolvedId = "witch_puzzle_solved";
        public const string FreedId = "painal_freed";

        [SerializeField] CombatEnemy[] witches;
        [SerializeField] DialogueNPC prisoner;
        [SerializeField] SpriteRenderer prisonerSprite;
        [SerializeField] Sprite chainedSprite;
        [SerializeField] Sprite freedSprite;
        [Tooltip("The chains and shackles on the wall (hidden when he is free).")]
        [SerializeField] GameObject chains;
        [SerializeField] InteractSpot paintingSpot;
        [SerializeField] SpriteRenderer paintingSprite;
        [SerializeField] Sprite tornPainting;
        [SerializeField] Sprite wholePainting;
        [SerializeField] PaintingPuzzle puzzle;
        [SerializeField] MapMarker marker;

        [Header("Reward: Amulet of Painal")]
        [Tooltip("Walking and running speed multiplier with the amulet (1.3 = 30% faster).")]
        [SerializeField, Range(1f, 2f)] float speedBonus = 1.3f;
        [SerializeField] Transform freedSpot;

        [Header("What the prisoner says")]
        [SerializeField, TextArea(2, 4)] string[] whileGuarded =
        {
            "Stranger... no. Run. They will drink your tonalli as they drank mine.",
            "> Who are you? Why do the witches keep you here?",
            "There is no time! Four of them... four... they never sleep.",
        };
        [SerializeField, TextArea(2, 4)] string[] afterWitches =
        {
            "They are gone? You... you beat all four of them!",
            "But these chains are not iron. They are a spell, woven into that painting on the wall.",
            "The witches tore it to pieces and hung it upside down. While the picture is broken, so am I.",
            "Please, warrior: put the painting back together.",
        };
        [SerializeField, TextArea(2, 4)] string[] story =
        {
            "Free... I can feel my legs again. Thank you, warrior of the living.",
            "I am Painal, the runner of the gods. I carried the names of the newly dead down the nine levels of Mictlán, so Mictlantecuhtli would know who was coming.",
            "The four witches were once women of the west, the Cihuateteo. Mictlán took their names; they wanted my feet to run back to the world above.",
            "They caught me at the crossroads with a song, painted my capture on amate paper and tore it apart. A torn picture is a torn fate.",
            "> The painting showed you running, and four shadows closing in.",
            "Yes. And now it shows me free. Take this: the Amulet of Painal, feathers of the hummingbird and the jaguar.",
            "Wear it and the wind will carry you. You will need fast feet where you are going... Quetzalcóatl does not wait.",
        };
        [SerializeField, TextArea(2, 4)] string[] afterReward =
        {
            "I will rest here a while. When my legs are strong, I will run again.",
            "The merchants say there is a narrow stretch of the river where a fast warrior can jump across. With my amulet, you could fly over it.",
        };

        PlayerCombatant player;
        bool witchesDown, freed, busy;
        float trailTimer;

        void Start()
        {
            player = FindAnyObjectByType<PlayerCombatant>();
            if (paintingSpot != null) paintingSpot.Used += OnPaintingUsed;
            prisoner.SetTalk(whileGuarded, null, false);
            Invoke(nameof(LoadState), 0.4f);   // after the save has loaded
        }

        void OnDestroy()
        {
            if (paintingSpot != null) paintingSpot.Used -= OnPaintingUsed;
        }

        void LoadState()
        {
            if (ContextTutorial.HasSeen(FreedId) || ContextTutorial.HasSeen(SolvedId))
            {
                witchesDown = true;
                SetFreed();
                if (puzzle != null) puzzle.MarkSolved();
                ApplyAmulet();
            }
        }

        void Update()
        {
            if (!witchesDown && AllWitchesDown())
            {
                witchesDown = true;
                prisoner.SetTalk(afterWitches, afterWitches, markNew: true);
                if (paintingSpot != null) paintingSpot.SetPrompt("{Interact}  Look at the torn painting");
                ContextTutorial.ShowOnce("witch_house_free", "The witches are dead. Talk to the prisoner, then look at the painting on the wall.");
            }
            AmuletTrail();
        }

        bool AllWitchesDown()
        {
            foreach (var w in witches)
                if (w != null && w.gameObject.activeInHierarchy && !w.Health.IsDead) return false;
            return true;
        }

        void OnPaintingUsed(InteractSpot _)
        {
            if (busy || freed) return;
            if (!witchesDown)
            {
                CombatFeed.ShowBanner("The witches are watching you.", 2f);
                return;
            }
            StartCoroutine(SolveAndRelease());
        }

        IEnumerator SolveAndRelease()
        {
            busy = true;
            yield return puzzle.Run();
            if (puzzle.Solved)
            {
                ContextTutorial.MarkSeen(new[] { SolvedId });
                if (paintingSpot != null) paintingSpot.SetPrompt("{Interact}  Look at the painting");
                if (paintingSprite != null && wholePainting != null) paintingSprite.sprite = wholePainting;
                yield return Release();
            }
            busy = false;
        }

        IEnumerator Release()
        {
            GameState.SetMode(GameMode.Cutscene);
            var cam = FindAnyObjectByType<CameraFollow>();
            Transform previous = cam != null ? cam.Target : null;
            if (cam != null) cam.Follow(prisoner.transform);
            yield return new WaitForSeconds(0.6f);

            // The painting glows, then the light runs into the chains.
            Vector3 paintingPos = paintingSprite != null ? paintingSprite.transform.position + Vector3.up * 0.6f : transform.position;
            Vector3 chainsPos = prisoner.transform.position + Vector3.up * 1.2f;
            Vfx.Ring(paintingPos, MexicaPalette.AgedGold, 0.2f, 1.4f, 0.5f);
            GameAudio.PlaySfx("tip", 1f, 0f);
            yield return new WaitForSeconds(0.3f);
            yield return Vfx.Projectile(paintingPos, chainsPos, MexicaPalette.Turquoise, 0.5f);
            CombatFeed.RequestShake(0.1f, 0.6f);
            for (int i = 0; i < 3; i++)
            {
                Vfx.Ring(chainsPos, MexicaPalette.Turquoise, 0.1f, 0.8f + i * 0.3f, 0.3f);
                GameAudio.PlaySfx("tip", 0.7f, 0.1f);
                yield return new WaitForSeconds(0.25f);
            }

            // The chains burst.
            GameAudio.PlaySfx("chains_break", 1f, 0f);
            Vfx.Burst(chainsPos, 16, new Color(0.6f, 0.6f, 0.65f), new Vector2(2f, 5f), 14f, 0.8f, 0.9f, toGround: true, upward: 0.6f);
            Vfx.Ring(chainsPos, Color.white, 0.2f, 2f, 0.4f);
            CombatFeed.RequestShake(0.2f, 0.3f);
            if (chains != null) chains.SetActive(false);

            // He falls to the floor and stands up.
            Vector3 from = prisonerSprite.transform.position;
            Vector3 to = freedSpot != null ? freedSpot.position : prisoner.transform.position;
            prisonerSprite.sprite = freedSprite;
            for (float t = 0f; t < 0.35f; t += Time.deltaTime)
            {
                float k = t / 0.35f;
                prisonerSprite.transform.position = Vector3.Lerp(from, to, k * k);
                yield return null;
            }
            prisonerSprite.transform.position = to;
            GameAudio.PlaySfx("land", 0.8f);
            Vfx.Dust(to, 8, MexicaPalette.Ash, 1f);
            yield return new WaitForSeconds(0.8f);

            freed = true;
            MoveNpcTo(to);
            GameState.SetMode(GameMode.Exploration);

            // His story.
            var lines = new List<DialogueBox.Line>();
            Sprite face = freedSprite;
            foreach (string line in story)
                lines.Add(line.StartsWith("> ") ? new DialogueBox.Line("Tlahuelli", line.Substring(2)) : new DialogueBox.Line("Painal", line, face));
            yield return DialogueBox.Play(lines);

            // The reward.
            ContextTutorial.MarkSeen(new[] { FreedId });
            ApplyAmulet();
            GameAudio.PlaySfx("fury", 0.9f);
            if (player != null)
            {
                Vfx.Ring(player.transform.position + Vector3.up * 0.5f, MexicaPalette.Turquoise, 0.2f, 1.6f, 0.5f);
                Vfx.Wisps(player.transform.position, 10, MexicaPalette.Turquoise, 1.6f, 0.5f);
            }
            CombatFeed.ShowBanner("AMULET OF PAINAL", 3f);
            ContextTutorial.ShowOnce("amulet_painal", "AMULET OF PAINAL - Super Speed: Tlahuelli now walks and runs 30% faster. Sprint ({Sprint}) to leave a trail of wind.");
            prisoner.SetTalk(afterReward, afterReward, false);
            if (marker != null) marker.Done = true;
            if (cam != null) cam.Follow(previous != null ? previous : player.transform);
            if (SaveController.Instance != null) SaveController.Instance.Save();
        }

        void SetFreed()
        {
            freed = true;
            if (chains != null) chains.SetActive(false);
            if (prisonerSprite != null)
            {
                prisonerSprite.sprite = freedSprite;
                if (freedSpot != null) prisonerSprite.transform.position = freedSpot.position;
            }
            if (freedSpot != null) MoveNpcTo(freedSpot.position);
            if (paintingSprite != null && wholePainting != null) paintingSprite.sprite = wholePainting;
            if (paintingSpot != null) paintingSpot.SetPrompt("{Interact}  Look at the painting");
            prisoner.SetTalk(afterReward, afterReward, false);
            if (marker != null) marker.Done = true;
        }

        /// <summary>The talk area goes with him to where he stands now.</summary>
        void MoveNpcTo(Vector3 position)
        {
            var sprite = prisonerSprite.transform;
            var keep = sprite.position;
            prisoner.transform.position = position;
            sprite.position = keep;
        }

        void ApplyAmulet()
        {
            if (player == null) return;
            var movement = player.GetComponent<PlayerMovement>();
            if (movement != null) movement.SpeedBonus = speedBonus;
        }

        /// <summary>With the amulet, running leaves a faint turquoise trail.</summary>
        void AmuletTrail()
        {
            if (player == null || !ContextTutorial.HasSeen(FreedId)) return;
            var movement = player.GetComponent<PlayerMovement>();
            if (movement == null || !movement.IsSprinting || !GameState.IsExploring) return;
            trailTimer -= Time.deltaTime;
            if (trailTimer > 0f) return;
            trailTimer = 0.12f;
            var sprite = player.GetComponentInChildren<SpriteRenderer>();
            if (sprite != null) Vfx.Afterimage(sprite, MexicaPalette.WithAlpha(MexicaPalette.Turquoise, 0.5f), 0.25f);
        }
    }
}
