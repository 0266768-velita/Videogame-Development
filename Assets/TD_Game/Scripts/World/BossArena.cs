using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Entrance of the boss zone (task 4.7). Put it on a trigger at the entrance of the arena.
    /// When the player walks in: the gates close behind them, the boss name appears and the boss music plays.
    /// If the player loses, the gates open again (they respawn outside, at the checkpoint).
    /// When the boss dies, the gates open and CreditsScreen rolls the credits.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BossArena : MonoBehaviour
    {
        [SerializeField] CombatEnemy boss;
        [Tooltip("Objects with a collider (rocks, a wall of bones...) that block the exit. Hidden while the arena is open.")]
        [SerializeField] GameObject[] gates;
        [Tooltip("Optional: music of the boss fight.")]
        [SerializeField] AudioSource bossMusic;
        [SerializeField] string entranceTitle = "QUETZALCÓATL\nThe Feathered Serpent awaits";
        [SerializeField, Min(0f)] float titleSeconds = 2.5f;

        bool closed;

        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        void Awake() => SetGates(false);

        void OnEnable()
        {
            CombatManager.CombatLost += Open;
            CombatEnemy.Defeated += OnEnemyDefeated;
        }

        void OnDisable()
        {
            CombatManager.CombatLost -= Open;
            CombatEnemy.Defeated -= OnEnemyDefeated;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (closed || boss == null || !boss.gameObject.activeInHierarchy || boss.Health.IsDead) return;
            if (other.GetComponentInParent<PlayerCombatant>() == null) return;

            closed = true;
            SetGates(true);
            CombatFeed.ShowBanner(entranceTitle, titleSeconds);
            CombatFeed.RequestShake(0.12f, 0.6f);
            if (bossMusic != null) bossMusic.Play();
            else GameAudio.PlayMusic(MusicTrack.Boss);
            GameAudio.PlaySfx("door");
            GameAudio.PlaySfx("roar");
            var ai = boss.GetComponent<EnemyAI>();
            if (ai != null) ai.Alert();
        }

        void OnEnemyDefeated(CombatEnemy enemy)
        {
            if (enemy == boss) Open();
        }

        void Open()
        {
            closed = false;
            SetGates(false);
            if (bossMusic != null) bossMusic.Stop();
        }

        void SetGates(bool active)
        {
            if (gates == null) return;
            foreach (var gate in gates)
                if (gate != null) gate.SetActive(active);
        }
    }
}
