using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Everything about the battle in progress: who fights, the current target, the input, what was
    /// defeated and how damage is applied (with its feedback). Turns and abilities receive it
    /// instead of finding things themselves.
    /// </summary>
    public class CombatContext
    {
        static readonly Color DamageColor = MexicaPalette.Damage;
        static readonly Color CriticalColor = MexicaPalette.Critical;
        static readonly Color HitFlash = MexicaPalette.HitFlash;

        readonly MonoBehaviour runner;
        CombatAbility pendingLastStand;

        public PlayerCombatant Player { get; }
        public IReadOnlyList<CombatEnemy> Enemies { get; }
        public CombatInput Input { get; }
        public List<EnemyData> Defeated { get; } = new List<EnemyData>();
        public int TargetIndex { get; private set; }
        public bool HasPendingLastStand => pendingLastStand != null;

        public CombatContext(PlayerCombatant player, List<CombatEnemy> enemies, CombatInput input, MonoBehaviour runner)
        {
            Player = player;
            Enemies = enemies;
            Input = input;
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

        public SpriteRenderer PlayerSprite => Player.GetComponentInChildren<SpriteRenderer>();

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

        /// <summary>Damages an enemy. critical = bigger number, stronger shake and a short hit stop.</summary>
        public void DamageEnemy(CombatEnemy enemy, float amount, bool critical = false, string label = null)
        {
            if (enemy == null || enemy.Health.IsDead) return;
            enemy.Health.TakeDamage(amount);

            string text = Mathf.RoundToInt(amount).ToString();
            if (!string.IsNullOrEmpty(label)) text = label + " " + text;
            CombatFeed.ShowPopup(enemy.transform.position + Vector3.up, critical ? text + "!" : text,
                critical ? CriticalColor : MexicaPalette.Bone);
            Run(CombatMotion.Flash(enemy.Sprite, HitFlash, 0.12f));
            CombatFeed.RequestShake(critical ? 0.14f : 0.05f, critical ? 0.22f : 0.12f);
            if (critical) CombatFeed.RequestHitStop(0.08f);

            if (enemy.Health.IsDead)
            {
                Defeated.Add(enemy.Data);
                CombatFeed.RequestHitStop(0.12f);
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
            CombatFeed.RequestShake(0.1f, 0.18f);
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
    }
}
