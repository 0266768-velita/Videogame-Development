using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TD_Game
{
    /// <summary>Everything the save file stores (task 4.19). JsonUtility turns it into text.</summary>
    [System.Serializable]
    public class SaveData
    {
        public string scene;
        public float checkpointX, checkpointY;
        public float bonusMaxHealth, bonusMaxStamina;
        public int victories;
        public int potions;
        public int gold;
        public bool hasKey;
        public float concentration;
        public List<string> abilities = new List<string>();
        public List<string> defeatedEnemies = new List<string>();
        public List<string> seenTutorials = new List<string>();
    }

    /// <summary>
    /// Reads and writes the save file in Application.persistentDataPath (one slot).
    /// Called "GameSave" so it does not get confused with the SaveSystem of the previous game.
    /// The main menu uses HasSave, NewGame() and Continue(); SaveController does the rest in the level.
    /// </summary>
    public static class GameSave
    {
        const string FileName = "td_game_save.json";
        static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>Set by the main menu: true = load the save when the level starts.</summary>
        public static bool ContinueRequested { get; set; }

        public static bool HasSave => File.Exists(FilePath);

        public static void NewGame()
        {
            Delete();
            ContinueRequested = false;
        }

        public static void Continue() => ContinueRequested = HasSave;

        public static void Write(SaveData data)
        {
            try { File.WriteAllText(FilePath, JsonUtility.ToJson(data, true)); }
            catch (System.Exception e) { Debug.LogError($"[{nameof(GameSave)}] Could not save: {e.Message}"); }
        }

        /// <summary>Null if there is no save or it cannot be read.</summary>
        public static SaveData Read()
        {
            if (!HasSave) return null;
            try { return JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)); }
            catch (System.Exception e)
            {
                Debug.LogError($"[{nameof(GameSave)}] The save file is damaged: {e.Message}");
                return null;
            }
        }

        public static void Delete()
        {
            if (HasSave) File.Delete(FilePath);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => ContinueRequested = false;
    }
}
