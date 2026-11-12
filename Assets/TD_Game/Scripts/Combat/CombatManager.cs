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

        public bool TryStartCombat(CombatEnemy lead)
        {
            if (!enabled || InCombat || !GameState.IsExploring || lead == null || !lead.IsAvailable) return false;
            if (player == null) { Debug.LogError($"[{nameof(CombatManager)}] Player is not assigned.", this); return false; }

            Context = new CombatContext(player, GatherEnemies(lead), this);
            StartCoroutine(BattleRoutine());
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

        IEnumerator BattleRoutine()
        {
            var context = Context;
            Vector3 returnPosition = player.transform.position;

            GameState.SetMode(GameMode.Combat);
            foreach (var e in context.Enemies) e.EnterCombat();
            player.BeginBattle();
            arena.Enter(player, context.Enemies);
            input.Enable();
            CombatStarted?.Invoke();
            CombatFeed.ShowBanner("Battle!", introTime);
            yield return new WaitForSeconds(introTime);

            bool enemiesFirst = context.Enemies[0].Data.startsBattleFirst;
            while (!context.PlayerDead && !context.AllEnemiesDead)
            {
                if (!enemiesFirst) yield return playerTurn.Run(context);
                enemiesFirst = false;

                foreach (var enemy in context.Enemies)
                {
                    if (context.PlayerDead || context.AllEnemiesDead) break;
                    if (enemy == null || enemy.Health.IsDead) continue;
                    yield return enemyTurn.Run(context, enemy);
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
