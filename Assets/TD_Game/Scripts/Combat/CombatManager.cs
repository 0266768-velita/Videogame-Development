using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Runs a turn-based battle on the map, as in Mictlán v5:
    /// contact with an enemy -> nearby enemies join -> arena and camera zoom ->
    /// player turn / each enemy's turn until victory or defeat -> back to exploration.
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }

        public static event Action CombatStarted;
        /// <summary>Raised after a victory with the data of every defeated enemy (story hooks, saving).</summary>
        public static event Action<IReadOnlyList<EnemyData>> CombatWon;
        public static event Action CombatLost;

        [SerializeField] PlayerCombatant player;
        [SerializeField] CombatInput input;
        [SerializeField] CombatArena arena;
        [SerializeField] PlayerTurn playerTurn;
        [SerializeField] EnemyTurn enemyTurn;
        [SerializeField] CombatRewards rewards;

        [Header("Joining enemies")]
        [Tooltip("Enemies this close (tiles) to the one that started the battle join it.")]
        [SerializeField, Min(0f)] float joinRadius = 4f;
        [SerializeField, Range(1, 4)] int maxEnemies = 3;
        [SerializeField, Min(0f)] float introTime = 1.2f;

        /// <summary>The battle in progress, or null during exploration.</summary>
        public CombatContext Context { get; private set; }
        public bool InCombat => Context != null;
        public PlayerTurn PlayerTurn => playerTurn;
        /// <summary>The enemy acting right now, or null during the player's turn (turn order UI).</summary>
        public CombatEnemy CurrentEnemy { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[{nameof(CombatManager)}] There is more than one in the scene; this one is ignored.", this);
                enabled = false;
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Starts a battle against this enemy and the ones nearby. ambush = the player landed on it from a jump.</summary>
        public bool TryStartCombat(CombatEnemy lead, bool ambush = false)
        {
            if (!enabled || InCombat || !GameState.IsExploring || lead == null || !lead.IsAvailable) return false;
            if (player == null) { Debug.LogError($"[{nameof(CombatManager)}] Player is not assigned.", this); return false; }

            Context = new CombatContext(player, GatherEnemies(lead), input, this);
            StartCoroutine(BattleRoutine(ambush));
            return true;
        }

        List<CombatEnemy> GatherEnemies(CombatEnemy lead)
        {
            var list = new List<CombatEnemy> { lead };
            foreach (var e in FindObjectsByType<CombatEnemy>())
            {
                if (list.Count >= maxEnemies) break;
                if (e == lead || !e.IsAvailable) continue;
                if (Vector2.Distance(e.transform.position, lead.transform.position) <= joinRadius) list.Add(e);
            }
            return list;
        }

        IEnumerator BattleRoutine(bool ambush)
        {
            var context = Context;
            Vector3 returnPosition = player.transform.position;

            GameState.SetMode(GameMode.Combat);
            foreach (var e in context.Enemies) e.EnterCombat();
            player.BeginBattle();
            arena.Enter(player, context.Enemies);
            input.Enable();
            CombatStarted?.Invoke();
            CombatFeed.ShowBanner(ambush ? "AMBUSH!" : "Battle!", introTime);
            yield return new WaitForSeconds(introTime);

            bool enemiesFirst = !ambush && context.Enemies[0].Data.startsBattleFirst;
            if (ambush) yield return Stomp(context);
            while (!context.PlayerDead && !context.AllEnemiesDead)
            {
                if (!enemiesFirst) yield return playerTurn.Run(context);
                enemiesFirst = false;

                foreach (var enemy in context.Enemies)
                {
                    if (context.PlayerDead || context.AllEnemiesDead) break;
                    if (enemy == null || enemy.Health.IsDead) continue;
                    CurrentEnemy = enemy;
                    yield return enemyTurn.Run(context, enemy);
                    CurrentEnemy = null;
                }
            }

            bool victory = !context.PlayerDead;
            input.Disable();
            if (victory) yield return rewards.Victory(context, returnPosition);
            else yield return rewards.Defeat(context);

            arena.Exit(snap: !victory);
            foreach (var e in context.Enemies) if (e != null) e.ExitCombat();
            player.EndBattle();
            Context = null;
            GameState.SetMode(GameMode.Exploration);

            if (victory) CombatWon?.Invoke(context.Defeated);
            else CombatLost?.Invoke();
        }

        /// <summary>Ambush opener: the player falls on the first enemy.</summary>
        IEnumerator Stomp(CombatContext context)
        {
            var lead = context.Enemies[0];
            yield return CombatMotion.Hop(player.transform, 0.8f, 0.35f, null);
            yield return CombatMotion.Lunge(player.transform, lead.transform.position, context.StopDistance(lead), 0.12f,
                () => context.DamageEnemy(lead, player.AttackDamage(player.Stats.ambushMultiplier), critical: true, label: "STOMP"));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
            CombatStarted = null;
            CombatWon = null;
            CombatLost = null;
        }
    }
}
