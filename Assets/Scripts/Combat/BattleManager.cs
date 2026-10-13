using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerAction { None, Attack, Fury, Seismic, Potion, EndTurn }

/// <summary>
/// 2D turn-based combat (classic Zelda/Pokemon feel: Tlahuelli on the left, enemies on the right).
/// It creates itself the first time an enemy needs it (it does not have to be in the scene).
///
/// Flow: EnemyController reaches Tlahuelli -> StartBattle -> player turn (several actions while
/// stamina lasts) -> each enemy's turn (telegraph, dodge window) -> repeat until victory or defeat.
/// </summary>
public class BattleManager : MonoBehaviour
{
    // ---------------- Singleton ----------------
    static BattleManager instance;
    public static BattleManager Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<BattleManager>();
            if (instance == null) instance = new GameObject("BattleManager").AddComponent<BattleManager>();
            return instance;
        }
    }
    public static bool IsInBattle => instance != null && instance.inBattle;

    /// <summary>Story hooks: for example unlock Seismic Jump after the Valley of Tlazolteotl.</summary>
    public static event Action<List<EnemyData>> BattleWon;
    public static event Action BattleLost;
    public static event Action BattleStarted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { instance = null; BattleWon = null; BattleLost = null; BattleStarted = null; }

    // ---------------- Settings (all in tiles) ----------------
    [Header("Arena")]
    [Tooltip("Horizontal distance between Tlahuelli and the enemies.")]
    public float battleDistance = 7f;
    public float enemySpacing = 3f;
    public float groupJoinRadius = 6f;
    public int maxEnemiesPerBattle = 3;

    [Header("Combat")]
    public float playerBodyRadius = 0.5f;
    public float seismicMultiplier = 1.2f;
    [Tooltip("How high Tlahuelli jumps for the Seismic Jump (tiles).")]
    public float seismicJumpHeight = 1.8f;
    [Tooltip("Speed of the shockwave across the ground (tiles/s). Each enemy is hit when the wave reaches it.")]
    public float seismicWaveSpeed = 16f;
    public float seismicShake = 0.3f;
    public Color seismicWaveColor = new Color(1f, 0.75f, 0.3f);
    public float dodgeStepDistance = 1.5f;
    public float perfectDodgeStaminaBonus = 10f;
    [Range(0.1f, 0.9f)] public float perfectWindowFraction = 0.5f;
    [Tooltip("Pressing dodge BEFORE the window opens locks the dodge for this many seconds (anti-spam).")]
    public float earlyPressLockout = 0.3f;
    public Color telegraphColor = new Color(1f, 0.2f, 0.15f);

    // ---------------- Public state (read by the HUD) ----------------
    public PlayerCombatStats Stats { get; private set; }
    public Health PlayerHealth { get; private set; }
    public IReadOnlyList<EnemyController> Enemies => enemies;
    public int TargetIndex => targetIndex;
    public bool AcceptingInput { get; private set; }

    // ---------------- Internal ----------------
    readonly List<EnemyController> enemies = new List<EnemyController>();
    readonly List<EnemyData> defeatedData = new List<EnemyData>();
    Player2D player;
    ActorFeedback playerFx;
    BattleHUD hud;
    BattleCameraController battleCam;

    bool inBattle;
    bool turnEnded;
    bool pendingFuryAwakening;
    float pendingSouls;
    int targetIndex;
    Vector2 preBattlePos;
    PlayerAction pending = PlayerAction.None;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;

        hud = GetComponent<BattleHUD>();
        if (hud == null) hud = gameObject.AddComponent<BattleHUD>();
        battleCam = GetComponent<BattleCameraController>();
        if (battleCam == null) battleCam = gameObject.AddComponent<BattleCameraController>();
    }

    void OnDestroy()
    {
        if (inBattle) BattleInput.Disable();
        if (instance == this) instance = null;
    }

    // =====================================================================
    //  Start
    // =====================================================================
    public bool StartBattle(EnemyController lead)
    {
        if (inBattle || GameState.Mode != GameMode.Exploration) return false;
        if (!FindPlayer()) return false;

        enemies.Clear();
        defeatedData.Clear();
        enemies.Add(lead);
        foreach (var e in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
        {
            if (enemies.Count >= maxEnemiesPerBattle) break;
            if (e == lead || !e.IsAvailableForBattle) continue;
            if (Vector2.Distance(e.transform.position, lead.transform.position) <= groupJoinRadius) enemies.Add(e);
        }

        inBattle = true;
        GameState.SetMode(GameMode.Combat);
        StartCoroutine(BattleRoutine());
        return true;
    }

    bool FindPlayer()
    {
        if (player == null) player = FindFirstObjectByType<Player2D>();
        if (player == null)
        {
            Debug.LogError("BattleManager: cannot find the player (Player2D component).");
            return false;
        }
        Stats = player.GetComponent<PlayerCombatStats>();
        if (Stats == null) Stats = player.gameObject.AddComponent<PlayerCombatStats>();
        PlayerHealth = player.GetComponent<Health>();
        playerFx = player.GetComponent<ActorFeedback>();
        if (playerFx == null) playerFx = player.gameObject.AddComponent<ActorFeedback>();
        return true;
    }

    IEnumerator BattleRoutine()
    {
        BattleInput.Enable();
        preBattlePos = player.Position;
        player.EnterBattle();
        foreach (var e in enemies) e.EnterBattle();
        Stats.BeginBattle();
        pendingSouls = 0f;
        pendingFuryAwakening = false;
        BattleStarted?.Invoke();

        Audio.PlayMusic("music_battle", true, 0.5f);
        Audio.Play("alert");

        Vector2 center = PlaceActors();
        hud.Show(this);
        battleCam.EnterBattle(center);
        hud.ShowBanner("Battle!", 1.2f);
        yield return new WaitForSeconds(1.3f);

        yield return TutorialTips.Instance.Modal("combat_basics", "COMBAT BASICS",
            "Battles are turn-based. On your turn you spend STAMINA (the gold bar).\n\n" +
            "[1 / A] Attack      [4 / D-pad Up] Potion      [Q / RB] End turn\n" +
            "[Left / Right arrows] change target\n\n" +
            "Stamina only partly refills each turn, so spend it wisely.");

        bool victory = false;
        bool firstRound = true;

        while (true)
        {
            bool enemiesFirst = firstRound && enemies[0].data.startsBattleFirst;
            firstRound = false;

            if (!enemiesFirst)
            {
                yield return PlayerTurn();
                if (AllEnemiesDead()) { victory = true; break; }
                if (PlayerHealth.IsDead) break;
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || e.Health.IsDead) continue;
                yield return EnemyTurn(e);
                if (PlayerHealth.IsDead) break;
            }
            if (PlayerHealth.IsDead) break;
        }

        if (victory) yield return VictoryRoutine();
        else yield return DefeatRoutine();

        hud.Hide();
        battleCam.ExitBattle();
        BattleInput.Disable();
        player.ExitBattle();
        inBattle = false;
        GameState.SetMode(GameMode.Exploration);
        Audio.PlayMusic("music_explore", true, 1.2f);

        if (victory)
            TutorialTips.Instance.Toast("victory",
                "Victory!  Every defeated enemy charges your Macuahuitl (more damage).  Health is restored, a potion drops and the game is saved.", 8f);
    }

    // =====================================================================
    //  Placement (Tlahuelli on the left, enemies on the right)
    // =====================================================================
    Vector2 PlaceActors()
    {
        Vector2 center = enemies[0].transform.position;
        float half = battleDistance * 0.5f;

        player.Teleport(center + Vector2.left * half);
        playerFx.Face(1f);

        for (int i = 0; i < enemies.Count; i++)
        {
            Vector2 pos;
            if (i == 0) pos = center + Vector2.right * half;
            else
            {
                float sign = (i % 2 == 1) ? 1f : -1f;
                int row = Mathf.CeilToInt(i / 2f);
                pos = center + new Vector2(half + 0.8f * row, sign * row * enemySpacing);
            }
            var t = enemies[i].transform;
            t.position = new Vector3(pos.x, pos.y, t.position.z);
            enemies[i].Fx.Face(-1f);
        }
        return center + Vector2.up * 0.5f;
    }

    float StopDistance(EnemyController e) => e.data.bodyRadius + playerBodyRadius + 0.4f;

    // =====================================================================
    //  Player turn
    // =====================================================================
    IEnumerator PlayerTurn()
    {
        Stats.BeginTurn();
        turnEnded = false;
        pending = PlayerAction.None;
        targetIndex = FirstAliveIndex();
        hud.SetTurnLabel("Your turn");
        Audio.Play("turn_start", 0.7f);

        while (!turnEnded)
        {
            if (PlayerHealth.IsDead || AllEnemiesDead()) break;

            AcceptingInput = true;
            if (BattleInput.TargetNext.WasPressedThisFrame()) CycleTarget(1);
            if (BattleInput.TargetPrev.WasPressedThisFrame()) CycleTarget(-1);

            if (pending == PlayerAction.None)
            {
                if (BattleInput.Attack.WasPressedThisFrame()) pending = PlayerAction.Attack;
                else if (BattleInput.Fury.WasPressedThisFrame()) pending = PlayerAction.Fury;
                else if (BattleInput.Seismic.WasPressedThisFrame()) pending = PlayerAction.Seismic;
                else if (BattleInput.Potion.WasPressedThisFrame()) pending = PlayerAction.Potion;
                else if (BattleInput.EndTurn.WasPressedThisFrame()) pending = PlayerAction.EndTurn;
            }

            if (pending != PlayerAction.None)
            {
                var action = pending;
                pending = PlayerAction.None;
                AcceptingInput = false;
                yield return DoPlayerAction(action);

                if (AllEnemiesDead() || PlayerHealth.IsDead) break;
                if (!turnEnded && !Stats.CanAfford(Stats.MinActionCost))
                {
                    hud.ShowBanner("Out of energy", 0.8f);
                    yield return new WaitForSeconds(0.7f);
                    turnEnded = true;
                }
            }
            yield return null;
        }

        AcceptingInput = false;
        Stats.EndPlayerTurn();
        hud.SetTurnLabel("");
    }

    /// <summary>Called by the HUD buttons (mouse click).</summary>
    public void RequestAction(PlayerAction action)
    {
        if (AcceptingInput && pending == PlayerAction.None) pending = action;
    }

    IEnumerator DoPlayerAction(PlayerAction action)
    {
        float baseDmg = Mathf.Round(Stats.baseDamage * Stats.DamageMultiplier);

        switch (action)
        {
            case PlayerAction.Attack:
            {
                var target = CurrentTarget();
                if (target == null) yield break;
                if (!Stats.TrySpend(Stats.attackCost)) { Denied("Not enough stamina", 0.7f); yield break; }
                Audio.Play("swing", 0.8f, 0.1f);
                yield return playerFx.Lunge(target.transform.position, StopDistance(target), 0.22f,
                    () => DamageEnemy(target, baseDmg));
                break;
            }
            case PlayerAction.Fury:
            {
                if (!Stats.furyUnlocked) { Denied("You have not awakened this skill yet", 1f); yield break; }
                if (Stats.FuryUsedThisBattle) { Denied("Fury already used this battle", 1f); yield break; }
                if (!Stats.TrySpend(Stats.furyCost)) { Denied("Not enough stamina", 0.7f); yield break; }
                yield return FuryRoutine("Mictlan Fury!");
                break;
            }
            case PlayerAction.Seismic:
            {
                if (!Stats.seismicUnlocked) { Denied("You have not learned this skill yet", 1f); yield break; }
                if (!Stats.TrySpend(Stats.seismicCost)) { Denied("Not enough stamina", 0.7f); yield break; }
                hud.ShowBanner("Seismic Jump!", 1f);
                float dmg = Mathf.Round(baseDmg * seismicMultiplier);
                yield return playerFx.SeismicJump(seismicJumpHeight, () => SeismicImpact(dmg));
                yield return new WaitForSeconds(SeismicWaveTime() + 0.35f);   // let the wave reach everyone
                break;
            }
            case PlayerAction.Potion:
            {
                if (Stats.potions <= 0) { Denied("No potions left", 0.8f); yield break; }
                if (!Stats.TrySpend(Stats.potionCost)) { Denied("Not enough stamina", 0.7f); yield break; }
                Stats.potions--;
                Audio.Play("potion");
                float heal = PlayerHealth.maxHealth * Stats.potionHealFraction;
                PlayerHealth.Heal(heal);
                hud.Popup(player.Position + Vector2.up * 1.5f, "+" + Mathf.RoundToInt(heal), new Color(0.4f, 1f, 0.5f));
                yield return playerFx.Pulse(new Color(0.3f, 1f, 0.4f), 0.4f);
                break;
            }
            case PlayerAction.EndTurn:
                turnEnded = true;
                break;
        }
    }

    void Denied(string message, float seconds)
    {
        hud.ShowBanner(message, seconds);
        Audio.Play("error", 0.7f);
    }

    /// <summary>Farthest living enemy from the player, plus a margin (the wave must pass over everybody).</summary>
    float SeismicRadius()
    {
        float r = 3f;
        foreach (var e in enemies)
            if (e != null && !e.Health.IsDead)
                r = Mathf.Max(r, Vector2.Distance(player.transform.position, e.transform.position) + 1.5f);
        return r;
    }

    float SeismicWaveTime() => SeismicRadius() / Mathf.Max(1f, seismicWaveSpeed);

    /// <summary>The slam: screen shake, ground ring, and each enemy is hit when the wave reaches it.</summary>
    void SeismicImpact(float dmg)
    {
        Vector3 origin = player.transform.position;
        Audio.Play("seismic");
        if (battleCam != null) battleCam.Shake(seismicShake, 0.4f);
        Shockwave.Spawn(origin, SeismicRadius(), SeismicWaveTime(), seismicWaveColor);

        foreach (var e in enemies)
        {
            if (e == null || e.Health.IsDead) continue;
            float delay = Vector2.Distance(origin, e.transform.position) / Mathf.Max(1f, seismicWaveSpeed);
            StartCoroutine(SeismicHit(e, dmg, delay));
        }
    }

    IEnumerator SeismicHit(EnemyController e, float dmg, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (e == null || e.Health.IsDead) yield break;
        DamageEnemy(e, dmg);
        if (battleCam != null) battleCam.Shake(seismicShake * 0.4f, 0.15f);
        if (e != null && !e.Health.IsDead) StartCoroutine(e.Fx.Displace(new Vector3(1.2f, 0f, 0f), 0.2f));
    }

    IEnumerator FuryRoutine(string banner)
    {
        Stats.furyUnlocked = true;
        Stats.ActivateFury();
        Audio.Play("fury");
        hud.ShowBanner(banner, 1.6f);
        yield return playerFx.Pulse(new Color(0.8f, 0.2f, 1f), 0.9f);
    }

    // =====================================================================
    //  Enemy turn
    // =====================================================================
    IEnumerator EnemyTurn(EnemyController e)
    {
        hud.SetTurnLabel(e.data.displayName + " attacks");
        yield return new WaitForSeconds(0.5f);

        var atk = e.PickAttack();

        yield return TutorialTips.Instance.Modal("enemy_turn", "ENEMY TURN: DODGE!",
            "The enemy attacks! It flashes RED while it prepares.\n\n" +
            "When the yellow  DODGE!  prompt appears, press  D  (B on a gamepad).\n" +
            "Don't press too early: your dodge locks for a moment.\n\n" +
            "Dodging late in the window is a PERFECT dodge: +10 stamina.");
        if (atk.counter == CounterType.Interrupt)
            yield return TutorialTips.Instance.Modal("interrupt", "INTERRUPT THE SPELL",
                "Some spells can't be dodged.\n\n" +
                "When you see  INTERRUPT!  press  Attack  ( 1  or  A )  in time to break the spell.");

        hud.ShowBanner(atk.attackName, 1.0f);
        int hits = Mathf.Max(1, atk.hits);

        for (int i = 0; i < hits && !PlayerHealth.IsDead; i++)
        {
            yield return EnemyHit(e, atk);
            if (pendingFuryAwakening) yield return FuryAwakeningRoutine();
            if (i < hits - 1) yield return new WaitForSeconds(atk.timeBetweenHits);
        }
        yield return new WaitForSeconds(0.5f);
        hud.SetTurnLabel("");
    }

    IEnumerator EnemyHit(EnemyController e, EnemyAttackData atk)
    {
        e.Fx.Trigger(atk.animatorTrigger);
        float windUp = Mathf.Max(0.3f, atk.windUpTime);
        StartCoroutine(e.Fx.Telegraph(windUp, telegraphColor));
        Audio.Play("telegraph", 0.7f);

        bool counterable = atk.counter != CounterType.Unavoidable;
        InputAction counterAction = atk.counter == CounterType.Interrupt ? BattleInput.Attack : BattleInput.Dodge;
        float windowStart = windUp - Mathf.Clamp(atk.counterWindow * Difficulty.CounterWindow, 0.1f, windUp);
        float perfectFrom = windowStart + (windUp - windowStart) * perfectWindowFraction;

        bool success = false, perfect = false;
        float lockUntil = 0f, t = 0f;

        while (t < windUp)
        {
            bool open = t >= windowStart;
            hud.SetCounterPrompt(counterable && open && !success, atk.counter);

            if (counterable && !success && counterAction.WasPressedThisFrame())
            {
                if (open && t >= lockUntil) { success = true; perfect = t >= perfectFrom; }
                else { lockUntil = t + earlyPressLockout; Audio.Play("error", 0.5f); }   // too early: locked for a moment (anti-spam)
            }
            t += Time.deltaTime;
            yield return null;
        }
        hud.SetCounterPrompt(false, atk.counter);

        float dmg = Mathf.Round(e.data.attackPower * atk.damageMultiplier * Difficulty.EnemyDamage);

        if (success)
        {
            Audio.Play(perfect ? "perfect" : (atk.counter == CounterType.Interrupt ? "interrupt" : "dodge"));
            if (atk.counter == CounterType.Dodge)
                StartCoroutine(playerFx.Displace(new Vector3(-dodgeStepDistance, 0.5f, 0f), 0.12f));

            hud.ShowBanner(perfect ? "PERFECT!" : (atk.counter == CounterType.Interrupt ? "Interrupted!" : "Dodged!"), 0.9f);
            if (perfect) Stats.AddStamina(perfectDodgeStaminaBonus);
            yield return new WaitForSeconds(0.5f);
        }
        else if (atk.lungeAtTarget)
        {
            Audio.Play("swing", 0.9f, 0.08f);
            yield return e.Fx.Lunge(player.transform.position, StopDistance(e), 0.18f, () => HitPlayer(dmg, e.data));
        }
        else
        {
            HitPlayer(dmg, e.data);
            yield return new WaitForSeconds(0.4f);
        }
    }

    void HitPlayer(float dmg, EnemyData source)
    {
        // Last stand: the FIRST time a Quinametzin would kill Tlahuelli, Mictlantecuhtli keeps him alive at 1 HP
        // (and then awakens Fury below). This guarantees the Fury unlock from GDD section 12 can never be missed.
        bool lastStand = source != null && source.grantsFuryAtCriticalHealth && !Stats.furyUnlocked
                         && dmg >= PlayerHealth.Current;
        if (lastStand) dmg = Mathf.Max(0f, PlayerHealth.Current - 1f);

        PlayerHealth.TakeDamage(dmg);
        Audio.Play("hurt", 1f, 0.06f);
        hud.Popup(player.Position + Vector2.up * 1.5f, "-" + Mathf.RoundToInt(dmg), new Color(1f, 0.3f, 0.3f));
        StartCoroutine(playerFx.HitReaction());
        if (battleCam != null) battleCam.Shake(0.15f, 0.15f);

        if (!PlayerHealth.IsDead && source != null && source.grantsFuryAtCriticalHealth && !Stats.furyUnlocked
            && PlayerHealth.Normalized <= Stats.criticalHealthFraction)
            pendingFuryAwakening = true;
    }

    /// <summary>GDD section 12: against the Quinametzin, at critical health, Mictlantecuhtli grants his power.</summary>
    IEnumerator FuryAwakeningRoutine()
    {
        pendingFuryAwakening = false;
        yield return new WaitForSeconds(0.3f);
        yield return FuryRoutine("Mictlantecuhtli: \"Take my power!\"  (Mictlan Fury unlocked)");
        yield return new WaitForSeconds(0.8f);
        yield return TutorialTips.Instance.Modal("fury", "MICTLAN FURY UNLOCKED",
            "Mictlantecuhtli refuses to let you fall.\n\n" +
            "Use  Mictlan Fury  ( 2  or  X )  on your turn: it heals you, refills your stamina\n" +
            "and boosts your damage for 3 turns.\n\n" +
            "You can use it once per battle.");
    }

    // =====================================================================
    //  Damage to enemies, targeting
    // =====================================================================
    void DamageEnemy(EnemyController e, float dmg)
    {
        if (e == null || e.Health.IsDead) return;
        e.Health.TakeDamage(dmg);
        Audio.Play(e.Health.IsDead ? "enemy_die" : "enemy_hit", 1f, 0.08f);
        hud.Popup(e.transform.position + Vector3.up * e.data.popupHeight, Mathf.RoundToInt(dmg).ToString(), Color.white);
        e.Fx.Trigger("Hit");
        StartCoroutine(e.Fx.HitReaction());

        if (e.Health.IsDead)
        {
            pendingSouls += e.data.soulsOnDefeat;
            defeatedData.Add(e.data);
            e.OnDefeated();
        }
    }

    bool AllEnemiesDead()
    {
        foreach (var e in enemies) if (e != null && !e.Health.IsDead) return false;
        return true;
    }

    int FirstAliveIndex()
    {
        for (int i = 0; i < enemies.Count; i++) if (enemies[i] != null && !enemies[i].Health.IsDead) return i;
        return 0;
    }

    void CycleTarget(int dir)
    {
        for (int step = 1; step <= enemies.Count; step++)
        {
            int idx = ((targetIndex + dir * step) % enemies.Count + enemies.Count) % enemies.Count;
            if (enemies[idx] != null && !enemies[idx].Health.IsDead) { targetIndex = idx; return; }
        }
    }

    EnemyController CurrentTarget()
    {
        if (targetIndex < 0 || targetIndex >= enemies.Count || enemies[targetIndex] == null || enemies[targetIndex].Health.IsDead)
            targetIndex = FirstAliveIndex();
        var t = enemies[targetIndex];
        return (t != null && !t.Health.IsDead) ? t : null;
    }

    // =====================================================================
    //  End of combat
    // =====================================================================
    IEnumerator VictoryRoutine()
    {
        Audio.StopMusic(0.3f);
        Audio.Play("victory");
        Stats.GrantVictoryPotions();
        Stats.AddCharge(pendingSouls);
        hud.ShowBanner($"Victory!  +{Mathf.RoundToInt(pendingSouls)} souls  (Macuahuitl {Mathf.RoundToInt(Stats.ChargeNormalized * 100f)}%)", 2.2f);
        Stats.FullRestore();   // GDD section 14: health refills after winning
        BattleWon?.Invoke(new List<EnemyData>(defeatedData));
        yield return new WaitForSeconds(2.3f);

        // Return to where the fight started, and save (autosave + new respawn point)
        player.Teleport(preBattlePos);
        Stats.SetCheckpoint(preBattlePos);
        SaveSystem.Save(Stats, preBattlePos);
    }

    IEnumerator DefeatRoutine()
    {
        Audio.StopMusic(0.4f);
        hud.ShowBanner("You have fallen...", 2f);
        yield return new WaitForSeconds(1.8f);

        // Game Over screen: Play Again (retry from the last checkpoint) or Main Menu.
        hud.Hide();
        var screen = GetComponent<GameOverScreen>();
        if (screen == null) screen = gameObject.AddComponent<GameOverScreen>();
        yield return screen.Run();   // "Main Menu" loads another scene and never comes back here

        // Play Again: back to the last checkpoint with everything restored.
        player.Teleport(Stats.checkpoint);
        Stats.FullRestore();
        Stats.potions = Mathf.Max(Stats.potions, 2);   // never soft-lock a retry with zero potions
        foreach (var e in enemies)
            if (e != null && !e.Health.IsDead) e.ResetAfterPlayerDefeat();
        BattleLost?.Invoke();
    }
}
