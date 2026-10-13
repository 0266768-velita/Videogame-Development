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
}

/// <summary>
/// Minimal save system stored in PlayerPrefs (closes the GDD's "Checkpoint/continue system" item).
/// It saves automatically after every victory and from the pause menu.
/// </summary>
public static class SaveSystem
{
    const string Key = "mictlan_save_v1";
    static SaveData current;

    public static bool HasSave => PlayerPrefs.HasKey(Key);

    public static void NewGame() { current = new SaveData(); }

    public static bool Load()
    {
        if (!PlayerPrefs.HasKey(Key)) return false;
        current = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key));
        if (current == null) current = new SaveData();
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
