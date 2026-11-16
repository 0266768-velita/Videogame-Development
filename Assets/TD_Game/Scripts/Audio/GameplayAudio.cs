using System.Collections.Generic;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Connects the game events to the music and the sound effects of Mictlán v5.
    /// The combat code never plays sounds itself: this component listens to its events (like the HUD does).
    /// Put one in each level scene.
    /// </summary>
    public class GameplayAudio : MonoBehaviour
    {
        [SerializeField] PlayerCombatant player;

        readonly List<CombatEnemy> enemies = new List<CombatEnemy>();

        void Start()
        {
            if (player == null) player = FindAnyObjectByType<PlayerCombatant>();
            if (player != null)
            {
                player.Health.Damaged += OnPlayerDamaged;
                player.Health.Healed += OnPlayerHealed;
            }
            foreach (var enemy in FindObjectsByType<CombatEnemy>(FindObjectsInactive.Include))
            {
                if (enemy.Health == null) continue;
                enemy.Health.Damaged += OnEnemyDamaged;
                enemies.Add(enemy);
            }
        }

        void OnEnable()
        {
            CombatManager.CombatStarted += OnCombatStarted;
            CombatManager.CombatWon += OnCombatWon;
            CombatManager.CombatLost += OnCombatLost;
            CombatEnemy.Defeated += OnEnemyDefeated;
            CombatFeed.Banner += OnBanner;
            CombatFeed.CounterPrompt += OnCounterPrompt;
            CombatFeed.TurnLabel += OnTurnLabel;
            OrbSpawner.Collected += OnOrbCollected;
        }

        void OnDisable()
        {
            CombatManager.CombatStarted -= OnCombatStarted;
            CombatManager.CombatWon -= OnCombatWon;
            CombatManager.CombatLost -= OnCombatLost;
            CombatEnemy.Defeated -= OnEnemyDefeated;
            CombatFeed.Banner -= OnBanner;
            CombatFeed.CounterPrompt -= OnCounterPrompt;
            CombatFeed.TurnLabel -= OnTurnLabel;
            OrbSpawner.Collected -= OnOrbCollected;
        }

        void OnDestroy()
        {
            if (player != null)
            {
                player.Health.Damaged -= OnPlayerDamaged;
                player.Health.Healed -= OnPlayerHealed;
            }
            foreach (var enemy in enemies)
                if (enemy != null) enemy.Health.Damaged -= OnEnemyDamaged;
        }

        // ---------------- Battle ----------------

        void OnCombatStarted()
        {
            bool boss = false;
            var context = CombatManager.Instance != null ? CombatManager.Instance.Context : null;
            if (context != null)
                foreach (var e in context.Enemies)
                    if (e != null && e.Data.isBoss) boss = true;
            GameAudio.PlayMusic(boss ? MusicTrack.Boss : MusicTrack.Battle);
            GameAudio.PlaySfx(boss ? "roar" : "alert");
        }

        void OnCombatWon(IReadOnlyList<EnemyData> _)
        {
            GameAudio.PlaySfx("victory");
            GameAudio.PlayMusic(MusicTrack.Explore);
        }

        // The defeat sound and the Game Over song start in DefeatScreen; after the retry the map song returns.
        void OnCombatLost() => GameAudio.PlayMusic(MusicTrack.Explore);

        void OnEnemyDefeated(CombatEnemy _) => GameAudio.PlaySfx("enemy_die");

        void OnBanner(string text, float _)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (text.StartsWith("PERFECT")) GameAudio.PlaySfx("perfect");
            else if (text.StartsWith("Dodged")) GameAudio.PlaySfx("dodge");
            else if (text.StartsWith("Interrupted")) GameAudio.PlaySfx("interrupt");
            else if (text.StartsWith("Last Stand")) GameAudio.PlaySfx("fury");
            else if (text.Contains("transforms")) GameAudio.PlaySfx("roar");
            else if (text.StartsWith("Stunned")) GameAudio.PlaySfx("land");
            else if (text.StartsWith("Not enough") || text.StartsWith("Out of stamina") || text.StartsWith("You have not")
                     || text.Contains("already used")) GameAudio.PlaySfx("error");
            else if (text.StartsWith("New skill")) GameAudio.PlaySfx("key");
        }

        void OnCounterPrompt(string prompt)
        {
            if (!string.IsNullOrEmpty(prompt)) GameAudio.PlaySfx("telegraph", 0.8f);
        }

        void OnTurnLabel(string label)
        {
            if (label == "YOUR TURN") GameAudio.PlaySfx("turn_start", 0.7f);
        }

        // ---------------- Hits and pickups ----------------

        void OnPlayerDamaged(float _) => GameAudio.PlaySfx("hurt");

        void OnPlayerHealed(float amount)
        {
            if (amount > 0.5f && GameState.Mode == GameMode.Combat) GameAudio.PlaySfx("potion");
        }

        void OnEnemyDamaged(float _)
        {
            GameAudio.PlaySfx("swing", 0.7f);
            GameAudio.PlaySfx("enemy_hit");
        }

        void OnOrbCollected(SoulOrb orb, PlayerCombatant who) => GameAudio.PlaySfx("key", 0.35f, 0.15f);
    }
}
