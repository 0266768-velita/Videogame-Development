using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public float charge;
    public int potions = 3;
    public bool fury;
    public bool seismic;
    public float px, py;
    public bool hasPosition;
    public List<string> defeated = new List<string>();
    // progression (v5)
    public float maxHealth = 0f;       // 0 = not saved yet (use the prefab default)
    public float maxStamina = 0f;
    public bool hasKey, keyDropped, bossDefeated;
    public float keyX, keyY;
    public int enemiesDefeated;
}

/// <summary>
/// Minimal save system stored in PlayerPrefs (closes the GDD's "Checkpoint/continue system" item).
/// It saves automatically after every victory and from the pause menu.
/// </summary>
public static class SaveSystem
{
    const string Key = "mictlan_save_v2";
    static SaveData current;

    /// <summary>True when there is a game in progress. A finished game (boss defeated) is never continuable.</summary>
    public static bool HasSave
    {
        get
        {
            if (!PlayerPrefs.HasKey(Key)) return false;
            var d = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key));
            return d != null && !d.bossDefeated;
        }
    }

    public static void NewGame() { current = new SaveData(); GameProgress.Reset(); }

    public static bool Load()
    {
        if (!HasSave) return false;
        current = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key));
        if (current == null) current = new SaveData();
        GameProgress.HasKey = current.hasKey; GameProgress.KeyDropped = current.keyDropped;
        GameProgress.KeyPosition = new Vector2(current.keyX, current.keyY);
        GameProgress.BossDefeated = current.bossDefeated; GameProgress.EnemiesDefeated = current.enemiesDefeated;
        return true;
    }

    public static void Save(PlayerCombatStats stats, Vector2 position)
    {
        if (current == null) current = new SaveData();
        current.charge = stats.charge;
        current.potions = stats.potions;
        current.fury = stats.furyUnlocked;
        current.seismic = stats.seismicUnlocked;
        current.px = position.x; current.py = position.y; current.hasPosition = true;
        current.maxHealth = stats.Health != null ? stats.Health.maxHealth : 0f;
        current.maxStamina = stats.maxStamina;
        current.hasKey = GameProgress.HasKey; current.keyDropped = GameProgress.KeyDropped;
        current.keyX = GameProgress.KeyPosition.x; current.keyY = GameProgress.KeyPosition.y;
        current.bossDefeated = GameProgress.BossDefeated; current.enemiesDefeated = GameProgress.EnemiesDefeated;
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(current));
        PlayerPrefs.Save();
    }

    public static void ApplyTo(PlayerCombatStats stats)
    {
        if (current == null) return;
        stats.charge = current.charge;
        stats.AddCharge(0f);            // refreshes the Macuahuitl form
        stats.potions = current.potions;
        stats.furyUnlocked = current.fury;
        stats.seismicUnlocked = current.seismic;
        if (current.maxHealth > 0f) { stats.Health.SetMax(current.maxHealth); stats.Health.HealFull(); }
        if (current.maxStamina > 0f) { stats.maxStamina = current.maxStamina; stats.stamina = stats.maxStamina; }
    }

    public static bool TryGetPosition(out Vector2 pos)
    {
        pos = Vector2.zero;
        if (current == null || !current.hasPosition) return false;
        pos = new Vector2(current.px, current.py);
        return true;
    }

    public static bool IsDefeated(string id) => current != null && current.defeated.Contains(id);

    public static void MarkDefeated(string id)
    {
        if (current == null) current = new SaveData();
        if (!current.defeated.Contains(id)) current.defeated.Add(id);
    }

    public static void Delete()
    {
        PlayerPrefs.DeleteKey(Key);
        current = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { current = null; }
}
