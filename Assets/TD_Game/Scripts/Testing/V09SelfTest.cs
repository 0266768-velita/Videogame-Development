#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Tilemaps;

namespace TD_Game
{
    /// <summary>
    /// Automatic play test of the v0.9 features, run inside Play Mode in the editor (never in a build):
    /// HUD counter, world map, jumping over a log, falling into water, a battle against a camp, a merchant (skins and
    /// gambling), a villager's conversation and the whole side quest of the four witches. It presses keys with a virtual
    /// keyboard (like a player), takes screenshots and writes a report. Started by TD_Game > v0.9 > Run Self Test.
    /// </summary>
    public class V09SelfTest : MonoBehaviour
    {
        public const string PrefKey = "td_v09_selftest";
        public const string QuitKey = "td_v09_selftest_quit";

        static string Folder => Path.Combine(Directory.GetCurrentDirectory(), "Logs/SelfTest");   // Temp is deleted when Unity closes

        readonly StringBuilder report = new StringBuilder();
        Keyboard keys;
        PlayerCombatant player;
        int failures;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!UnityEditor.EditorPrefs.GetBool(PrefKey, false)) return;
            UnityEditor.EditorPrefs.SetBool(PrefKey, false);
            new GameObject("V09SelfTest").AddComponent<V09SelfTest>();
        }

        IEnumerator Start()
        {
            Application.runInBackground = true;
            Directory.CreateDirectory(Folder);
            foreach (var f in Directory.GetFiles(Folder)) File.Delete(f);
            // The test saves after its battles: keep the player's real save and put it back at the end.
            string save = Path.Combine(Application.persistentDataPath, "td_game_save.json");
            string backup = save + ".selftest_backup";
            bool hadSave = File.Exists(save);
            if (hadSave) File.Copy(save, backup, true);
            keys = InputSystem.AddDevice<Keyboard>("SelfTestKeyboard");
            yield return new WaitForSeconds(2f);
            player = FindAnyObjectByType<PlayerCombatant>();
            player.Health.SetMax(5000f);   // the test must not die by accident
            player.Health.Restore();

            yield return Step("HUD and Enemies Left", Hud());
            yield return Step("World map (M)", WorldMap());
            yield return Step("Jump over a fallen log", JumpLog());
            yield return Step("Fall into water and recover", FallWater());
            yield return Step("Fall into a ravine and recover", FallPit());
            yield return Step("Battle against a camp", CampBattle());
            yield return Step("Merchant: skins and gambling", Merchant());
            yield return Step("Villager conversation", Villager());
            yield return Step("Side quest: the four witches", WitchQuest());
            yield return Step("Save", SaveCheck());

            Log($"\nDONE. Failures: {failures}");
            File.WriteAllText(Path.Combine(Folder, "report.txt"), report.ToString());
            Debug.Log("[TD_Game] SELFTEST REPORT\n" + report);
            InputSystem.RemoveDevice(keys);
            if (hadSave) { File.Copy(backup, save, true); File.Delete(backup); }
            else if (File.Exists(save)) File.Delete(save);
            if (UnityEditor.EditorPrefs.GetBool(QuitKey, false))
            {
                UnityEditor.EditorPrefs.SetBool(QuitKey, false);
                UnityEditor.EditorApplication.Exit(failures == 0 ? 0 : 3);
            }
        }

        // ---------------- Helpers ----------------

        void Log(string line) { report.AppendLine(line); Debug.Log("[SelfTest] " + line); }

        void Check(bool ok, string what)
        {
            if (!ok) failures++;
            Log((ok ? "  PASS " : "  FAIL ") + what);
        }

        IEnumerator Step(string name, IEnumerator body)
        {
            Log("== " + name + $"   (mode {GameState.Mode}, timeScale {Time.timeScale}, dialogue {DialogueBox.IsOpen}, map {WorldMapScreen.IsOpen})");
            Release();
            // Errors inside a step are reported and the next step still runs.
            while (true)
            {
                object current;
                try
                {
                    if (!body.MoveNext()) break;
                    current = body.Current;
                }
                catch (System.Exception e) { failures++; Log("  EXCEPTION " + e.Message + "\n" + e.StackTrace); break; }
                yield return current;
            }
            Release();
            yield return new WaitForSeconds(0.3f);
        }

        void Hold(params Key[] pressed) => InputSystem.QueueStateEvent(keys, new KeyboardState(pressed));
        void Release() { if (keys != null) InputSystem.QueueStateEvent(keys, new KeyboardState()); }

        IEnumerator Tap(Key key, params Key[] also)
        {
            Hold(also.Append(key).ToArray());
            yield return null;
            yield return null;
            Hold(also);
            yield return null;
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(Folder, name + ".png"));
            yield return null;
        }

        IEnumerator WaitUntil(System.Func<bool> condition, float timeout)
        {
            for (float t = 0f; t < timeout && !condition(); t += Time.unscaledDeltaTime) yield return null;
        }

        void Teleport(Vector2 p)
        {
            player.Teleport(p);
            var cam = FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.SnapToTarget();
            var hazards = player.GetComponent<PlayerHazards>();
            if (hazards != null) hazards.ResetSafeSpot();
        }

        static object Call(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Invoke(target, args);

        IEnumerator WinBattle()
        {
            yield return WaitUntil(() => CombatManager.Instance.InCombat, 5f);
            yield return new WaitForSeconds(2f);
            while (CombatManager.Instance.InCombat)
            {
                var context = CombatManager.Instance.Context;
                if (context != null)
                    foreach (var e in context.Enemies.ToList())
                        if (e != null && !e.Health.IsDead) context.DamageEnemy(e, 99999f);
                yield return new WaitForSeconds(0.5f);
                yield return Tap(Key.Space);
            }
            yield return new WaitForSeconds(1f);
        }

        // ---------------- Steps ----------------

        IEnumerator Hud()
        {
            var counter = FindAnyObjectByType<EnemiesLeftCounter>();
            Check(counter != null, "Enemies Left counter exists");
            yield return new WaitForSeconds(1f);
            var label = counter.GetComponentInChildren<UnityEngine.UI.Text>();
            int remaining = CombatEnemy.Remaining();
            Log($"  remaining = {remaining}, label = '{label.text}'");
            Check(label.text.Contains(remaining.ToString()), "counter shows the remaining enemies");
            Check(remaining >= 30, "there are many enemies now (camps)");
            int camps = FindObjectsByType<CombatEnemy>().Where(e => !string.IsNullOrEmpty(e.Encounter) && !e.Optional).Select(e => e.Encounter).Distinct().Count();
            Log($"  camps with 2+ enemies: {camps}");
            Check(camps >= 10, "many fights against groups");
            yield return Shot("01_explore");
        }

        IEnumerator WorldMap()
        {
            yield return Tap(Key.M);
            yield return new WaitForSeconds(0.6f);
            Check(WorldMapScreen.IsOpen, "M opens the map");
            Check(GameState.Mode == GameMode.Cutscene, "the world stops while the map is open");
            yield return Shot("02_world_map");
            yield return Tap(Key.M);
            yield return new WaitForSeconds(0.4f);
            Check(!WorldMapScreen.IsOpen && GameState.IsExploring, "M closes the map");
        }

        IEnumerator JumpLog()
        {
            var log = FindObjectsByType<SpriteRenderer>().FirstOrDefault(s => s.name == "FallenLog" && s.sprite != null && s.sprite.name == "log_vertical");
            Check(log != null, "a fallen log exists on a path");
            if (log == null) yield break;
            var box = log.GetComponent<BoxCollider2D>();
            Vector2 center = (Vector2)log.transform.position + box.offset;
            Teleport(center + Vector2.left * 2.2f);
            yield return new WaitForSeconds(0.3f);
            Hold(Key.D);
            yield return new WaitForSeconds(1.2f);
            float blockedX = player.transform.position.x;
            Check(blockedX < center.x, $"walking into the log is blocked (x {blockedX:0.00} < log {center.x:0.00})");
            Hold();
            Teleport(center + Vector2.left * 1.6f);
            yield return new WaitForSeconds(0.2f);
            Hold(Key.D, Key.LeftShift);
            yield return new WaitForSeconds(0.15f);
            Hold(Key.D, Key.LeftShift, Key.Space);
            yield return null; yield return null;
            Hold(Key.D, Key.LeftShift);
            yield return Shot("03_jump_log");
            yield return new WaitForSeconds(0.9f);
            Hold();
            float after = player.transform.position.x;
            Check(after > center.x + 0.5f, $"jumping crosses the log (x {after:0.00} > {center.x:0.00})");
        }

        Tilemap Map(string name) => FindObjectsByType<Tilemap>().First(t => t.name == name);

        /// <summary>A land tile next to the hazard and the direction into it.</summary>
        static bool FindEdge(Tilemap hazard, Tilemap ground, out Vector2 land, out Vector2 dir, Vector2 near)
        {
            land = Vector2.zero; dir = Vector2.zero;
            var bounds = hazard.cellBounds;
            float best = float.MaxValue;
            foreach (var cell in bounds.allPositionsWithin)
            {
                if (!hazard.HasTile(cell)) continue;
                foreach (var d in new[] { Vector3Int.left, Vector3Int.right, Vector3Int.up, Vector3Int.down })
                {
                    // Three hazard tiles in a row going in, and two land tiles behind.
                    if (!hazard.HasTile(cell + d * -1) || !hazard.HasTile(cell + d * -2)) continue;
                    var landCell = cell + d;
                    if (hazard.HasTile(landCell) || hazard.HasTile(landCell + d) || !ground.HasTile(landCell)) continue;
                    var p = (Vector2)hazard.GetCellCenterWorld(landCell + d);
                    float dist = Vector2.Distance(p, near);
                    if (dist < best) { best = dist; land = p; dir = new Vector2(-d.x, -d.y); }
                }
            }
            return best < float.MaxValue;
        }

        IEnumerator FallInto(string tilemap, string shot)
        {
            var hazard = Map(tilemap);
            bool found = FindEdge(hazard, Map("Ground"), out var land, out var dir, player.transform.position);
            Check(found, $"found the edge of the {tilemap}");
            if (!found) yield break;
            Teleport(land);
            yield return new WaitForSeconds(0.4f);
            float before = player.Health.Current;
            var hazards = player.GetComponent<PlayerHazards>();
            Key k = dir.x > 0 ? Key.D : dir.x < 0 ? Key.A : dir.y > 0 ? Key.W : Key.S;
            Hold(k);
            yield return WaitUntil(() => hazards.IsFalling, 3f);
            Hold();
            Check(hazards.IsFalling, $"walking into the {tilemap} makes Tlahuelli fall");
            yield return new WaitForSeconds(0.45f);
            yield return Shot(shot);
            yield return WaitUntil(() => !hazards.IsFalling, 6f);
            yield return new WaitForSeconds(0.5f);
            var cell = hazard.WorldToCell(player.transform.position);
            Check(!hazard.HasTile(cell), "he climbs out on firm ground");
            Check(player.Health.Current < before && player.Health.Current > 0f, $"the fall costs some life ({before:0} -> {player.Health.Current:0})");
            Check(GameState.IsExploring, "control returns to the player");
        }

        IEnumerator FallWater() => FallInto("Water", "04_fall_water");
        IEnumerator FallPit() => FallInto("Pits", "05_fall_pit");

        IEnumerator CampBattle()
        {
            var lead = FindObjectsByType<CombatEnemy>().Where(e => !e.Optional && !string.IsNullOrEmpty(e.Encounter))
                .GroupBy(e => e.Encounter).OrderByDescending(g => g.Count()).First().First();
            int expected = FindObjectsByType<CombatEnemy>().Count(e => e.Encounter == lead.Encounter);
            int before = CombatEnemy.Remaining();
            int gold = player.Progression.Gold;
            Teleport(lead.transform.position + Vector3.left * 1.2f);
            yield return WaitUntil(() => CombatManager.Instance.InCombat, 6f);
            Check(CombatManager.Instance.InCombat, "touching a camp starts a battle");
            if (!CombatManager.Instance.InCombat) yield break;
            int count = CombatManager.Instance.Context.Enemies.Count;
            Check(count == expected, $"the whole camp fights together ({count} of {expected})");
            yield return new WaitForSeconds(2.2f);
            yield return Shot("06_battle_camp");
            yield return WinBattle();
            yield return new WaitForSeconds(1.5f);
            int after = CombatEnemy.Remaining();
            Check(after == before - expected, $"Enemies Left goes down by the camp ({before} -> {after})");
            Log($"  gold {gold} -> {player.Progression.Gold}");
            Check(player.Progression.Gold - gold <= 30, "a camp gives modest gold");
            var label = FindAnyObjectByType<EnemiesLeftCounter>().GetComponentInChildren<UnityEngine.UI.Text>();
            yield return new WaitForSeconds(0.6f);
            Check(label.text.Contains(after.ToString()), $"the counter updates ('{label.text}')");
        }

        IEnumerator Merchant()
        {
            var shops = FindObjectsByType<MerchantShop>();
            Check(shops.Length >= 4, $"merchants in every region ({shops.Length})");
            var shop = shops.OrderBy(s => s.transform.position.x).Skip(1).First();
            var area = shop.GetComponent<BoxCollider2D>();
            Teleport((Vector2)shop.transform.position + area.offset);
            yield return new WaitForSeconds(0.4f);
            yield return Tap(Key.E);
            yield return new WaitForSeconds(0.8f);
            var window = shop.transform.Find("ShopCanvas");
            Check(window != null && window.gameObject.activeSelf, "E opens the shop");
            yield return Shot("07_shop");
            player.Progression.AddGold(400);
            Call(shop, "ShowSkinPage");
            yield return new WaitForSeconds(0.5f);
            var skin = player.GetComponent<PlayerSkin>();
            var jaguar = skin.Catalog.skins.First(s => s.id == "jaguar");
            Call(shop, "Preview", jaguar);
            yield return Shot("08_skins_preview");
            int gold = player.Progression.Gold;
            Call(shop, "ChooseSkin", jaguar);
            Check(skin.Equipped == "jaguar" && skin.Owns("jaguar"), "buying a skin equips it");
            Check(player.Progression.Gold == gold - jaguar.price, "the skin costs its price");
            Call(shop, "ChooseSkin", skin.Catalog.skins[0]);
            Check(skin.Equipped == "default", "an owned skin can be worn again for free");
            Call(shop, "ChooseSkin", jaguar);
            Call(shop, "ShowMainPage");
            gold = player.Progression.Gold;
            Call(shop, "Gamble", 10);
            Log($"  gamble 10: gold {gold} -> {player.Progression.Gold}");
            Check(player.Progression.Gold == gold - 10 || player.Progression.Gold == gold + 10, "gambling pays double or loses the bet");
            yield return new WaitForSeconds(0.5f);
            yield return Shot("09_gamble");
            yield return Tap(Key.Escape);
            yield return new WaitForSeconds(0.6f);
            Check(GameState.IsExploring && !window.gameObject.activeSelf, "ESC closes the shop");
        }

        IEnumerator Villager()
        {
            var npc = FindObjectsByType<DialogueNPC>().FirstOrDefault(n => n.NpcName.StartsWith("Itzel"));
            Check(npc != null, "Itzel exists");
            if (npc == null) yield break;
            Teleport(npc.transform.position + Vector3.down * 1f);
            yield return new WaitForSeconds(0.4f);
            yield return Tap(Key.E);
            yield return new WaitForSeconds(1.2f);
            Check(DialogueBox.IsOpen, "E starts a conversation");
            yield return Shot("10_dialogue");
            for (int i = 0; i < 20 && DialogueBox.IsOpen; i++)
            {
                yield return Tap(Key.Space);
                yield return new WaitForSeconds(0.25f);
            }
            yield return new WaitForSeconds(0.3f);
            Check(!DialogueBox.IsOpen && GameState.IsExploring, "the conversation can be finished");
            Check(npc.Talked, "the villager remembers the talk");
        }

        IEnumerator WitchQuest()
        {
            var outside = FindObjectsByType<HouseDoor>().First(d => d.LeadsInside);
            Teleport(outside.transform.position + Vector3.down * 1.6f);
            yield return new WaitForSeconds(0.4f);
            Hold(Key.W);
            yield return WaitUntil(() => HouseDoor.InInterior, 4f);
            Hold();
            Check(HouseDoor.InInterior, "walking into the door enters the house");
            yield return new WaitForSeconds(1.2f);
            yield return Shot("11_witch_house");
            // The witches see Tlahuelli and attack together.
            var quest = FindAnyObjectByType<WitchHouseQuest>();
            var witches = FindObjectsByType<CombatEnemy>().Where(e => e.Encounter == "witch_house").ToList();
            Check(witches.Count == 4, "four witches guard the prisoner");
            if (!CombatManager.Instance.InCombat) Teleport(witches[0].transform.position + Vector3.down * 0.9f);
            yield return WaitUntil(() => CombatManager.Instance.InCombat, 8f);
            Check(CombatManager.Instance.InCombat && CombatManager.Instance.Context.Enemies.Count == 4, "one battle against the four witches");
            yield return new WaitForSeconds(2.2f);
            yield return Shot("12_four_witches");
            yield return WinBattle();
            yield return new WaitForSeconds(1.5f);
            Check(witches.All(w => w == null || !w.gameObject.activeSelf || w.Health.IsDead), "the witches are defeated");

            // The painting.
            var spot = FindObjectsByType<InteractSpot>().First(s => s.name == "PaintingSpot");
            Teleport(spot.transform.position + Vector3.down * 0.3f);
            yield return new WaitForSeconds(0.5f);
            yield return Tap(Key.E);
            yield return new WaitForSeconds(0.8f);
            var puzzle = FindAnyObjectByType<PaintingPuzzle>();
            var canvas = puzzle.transform.Find("PuzzleCanvas");
            Check(canvas != null && canvas.gameObject.activeSelf, "the painting opens the puzzle screen");
            yield return Shot("13_puzzle");
            // Solve it like a player would: swap pieces until each is in place (arrow keys + Enter).
            var field = typeof(PaintingPuzzle).GetField("pieceAt", BindingFlags.Instance | BindingFlags.NonPublic);
            var pieces = (int[])field.GetValue(puzzle);
            for (int guard = 0; guard < 20 && !puzzle.Solved; guard++)
            {
                int slot = System.Array.FindIndex(pieces, p => true) ;
                for (int s = 0; s < pieces.Length; s++) if (pieces[s] != s) { slot = s; break; }
                int target = System.Array.IndexOf(pieces, slot);
                Call(puzzle, "Pick", slot);
                Call(puzzle, "Pick", target);
                yield return new WaitForSeconds(0.25f);
                if (guard == 3) yield return Shot("14_puzzle_progress");
            }
            Check(puzzle.Solved, "the puzzle is solved when every piece is in place");
            // Release cutscene and Painal's story.
            yield return new WaitForSeconds(5f);
            yield return Shot("15_release");
            yield return WaitUntil(() => DialogueBox.IsOpen, 8f);
            Check(DialogueBox.IsOpen, "Painal tells his story");
            yield return Shot("16_painal_story");
            for (int i = 0; i < 30 && DialogueBox.IsOpen; i++)
            {
                yield return Tap(Key.Space);
                yield return new WaitForSeconds(0.3f);
            }
            yield return new WaitForSeconds(1f);
            yield return Shot("17_amulet");
            var movement = player.GetComponent<PlayerMovement>();
            Check(ContextTutorial.HasSeen(WitchHouseQuest.FreedId), "the quest is completed and remembered");
            Check(movement.SpeedBonus > 1.2f, $"the Amulet of Painal makes him faster (x{movement.SpeedBonus})");
            Check(CombatEnemy.Remaining() == FindObjectsByType<CombatEnemy>().Count(e => e.CountsAsRemaining), "the witches never counted as pending enemies");

            // Leave the house.
            var inside = FindObjectsByType<HouseDoor>().First(d => !d.LeadsInside);
            Teleport(inside.transform.position + Vector3.up * 1.6f);
            yield return new WaitForSeconds(0.4f);
            Hold(Key.S);
            yield return WaitUntil(() => !HouseDoor.InInterior, 4f);
            Hold();
            yield return new WaitForSeconds(1f);
            Check(!HouseDoor.InInterior, "the door takes him back outside");
            yield return Shot("18_back_outside");
        }

        IEnumerator SaveCheck()
        {
            SaveController.Instance.Save();
            yield return null;
            string path = Path.Combine(Application.persistentDataPath, "td_game_save.json");
            string json = File.Exists(path) ? File.ReadAllText(path) : "";
            Check(json.Contains("jaguar"), "the save keeps the skins");
            Check(json.Contains(WitchHouseQuest.FreedId), "the save keeps the side quest");
        }
    }
}
#endif
