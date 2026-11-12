using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Everything about the battle in progress: who fights, the current target, what was defeated
    /// and how damage is applied. Turns and abilities receive it instead of finding things themselves.
    /// </summary>
    public class CombatContext
    {
        static readonly Color DamageColor = new Color(1f, 0.35f, 0.3f);
        static readonly Color HitFlash = new Color(1f, 0.5f, 0.5f);

        readonly MonoBehaviour runner;
        CombatAbility pendingLastStand;

        public PlayerCombatant Player { get; }
        public IReadOnlyList<CombatEnemy> Enemies { get; }
        public List<EnemyData> Defeated { get; } = new List<EnemyData>();
        public float Souls { get; private set; }
        public int TargetIndex { get; private set; }
        public bool HasPendingLastStand => pendingLastStand != null;

        public CombatContext(PlayerCombatant player, List<CombatEnemy> enemies, MonoBehaviour runner)
        {
            Player = player;
            Enemies = enemies;
            this.runner = runner;
        }

        public bool PlayerDead => Player.Health.IsDead;

        public bool AllEnemiesDead
        {
            get
            {
                foreach (var e in Enemies) if (e != null && !e.Health.IsDead) return false;
                return true;
            }
        }

        public Coroutine Run(IEnumerator routine) => runner.StartCoroutine(routine);

        public float StopDistance(CombatEnemy enemy) => enemy.Data.bodyRadius + Player.Stats.bodyRadius + 0.2f;

        // ---------------- Targeting ----------------

        public CombatEnemy CurrentTarget
        {
            get
            {
                if (!IsAlive(TargetIndex)) SelectFirstAlive();
                return IsAlive(TargetIndex) ? Enemies[TargetIndex] : null;
            }
        }

        public void SelectFirstAlive()
        {
            for (int i = 0; i < Enemies.Count; i++)
                if (IsAlive(i)) { TargetIndex = i; return; }
        }

        public void CycleTarget(int direction)
        {
            int count = Enemies.Count;
            for (int step = 1; step <= count; step++)
            {
                int i = ((TargetIndex + direction * step) % count + count) % count;
                if (IsAlive(i)) { TargetIndex = i; return; }
            }
        }

        bool IsAlive(int i) => i >= 0 && i < Enemies.Count && Enemies[i] != null && !Enemies[i].Health.IsDead;

        // ---------------- Damage ----------------

        public void DamageEnemy(CombatEnemy enemy, float amount)
        {
            if (enemy == null || enemy.Health.IsDead) return;
            enemy.Health.TakeDamage(amount);
            CombatFeed.ShowPopup(enemy.transform.position + Vector3.up, Mathf.RoundToInt(amount).ToString(), Color.white);
            Run(CombatMotion.Flash(enemy.Sprite, HitFlash, 0.12f));

            if (enemy.Health.IsDead)
            {
                Souls += enemy.Data.soulsOnDefeat;
                Defeated.Add(enemy.Data);
                enemy.OnDefeated();
            }
        }

        public void DamagePlayer(float amount, EnemyData source)
        {
            // Last Stand: the first time this enemy would kill the player, they survive with 1 HP
            // and receive the enemy's lastStandAbility (Mictlan Fury in Mictlán v5).
            CombatAbility lastStand = source != null ? source.lastStandAbility : null;
            if (lastStand != null && !Player.Progression.IsUnlocked(lastStand) && amount >= Player.Health.Current)
            {
                amount = Mathf.Max(0f, Player.Health.Current - 1f);
                pendingLastStand = lastStand;
            }

            Player.Health.TakeDamage(amount);
            CombatFeed.ShowPopup(Player.transform.position + Vector3.up, "-" + Mathf.RoundToInt(amount), DamageColor);
            Run(CombatMotion.Flash(PlayerSprite, HitFlash, 0.15f));
        }

        public IEnumerator ResolveLastStand()
        {
            if (pendingLastStand == null) yield break;
            var ability = pendingLastStand;
            pendingLastStand = null;

            Player.Progression.Unlock(ability);
            CombatFeed.ShowBanner($"Last Stand!  {ability.displayName} awakens", 1.8f);
            yield return new WaitForSeconds(0.6f);
            yield return ability.ExecuteFree(this);
            Player.MarkUsed(ability);
        }

        public SpriteRenderer PlayerSprite => Player.GetComponentInChildren<SpriteRenderer>();
    }
}
