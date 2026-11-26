using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// Lists the assets that the game never uses: everything that the scenes of the build do not reference (directly or
    /// through prefabs, materials, ScriptableObjects...). Scripts are listed apart, since code can be used without a scene
    /// referencing it. Writes Logs/unused_assets.txt. Menu: TD_Game > Debug > Unused Assets Report.
    /// </summary>
    public static class UnusedAssetsReport
    {
        /// <summary>Runs once after a compile if the file Logs/run_unused_report exists (for tools that cannot click menus).</summary>
        [InitializeOnLoadMethod]
        static void AutoRun()
        {
            if (!File.Exists("Logs/run_unused_report")) return;
            File.Delete("Logs/run_unused_report");
            EditorApplication.delayCall += Run;
        }

        [MenuItem("TD_Game/Debug/Unused Assets Report")]
        public static void Run()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var used = new HashSet<string>(AssetDatabase.GetDependencies(scenes, true));
            var lines = new List<string> { "BUILD SCENES: " + string.Join(", ", scenes), "" };
            var unused = AssetDatabase.GetAllAssetPaths()
                .Where(p => p.StartsWith("Assets/") && !AssetDatabase.IsValidFolder(p) && !used.Contains(p))
                .OrderBy(p => p).ToList();
            foreach (var group in unused.GroupBy(p => string.Join("/", p.Split('/').Take(4))))
            {
                long bytes = group.Sum(p => File.Exists(p) ? new FileInfo(p).Length : 0);
                lines.Add($"{group.Key}  ({group.Count()} files, {bytes / 1024} KB)");
                foreach (var p in group) lines.Add("    " + p);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllLines("Logs/unused_assets.txt", lines);
            File.WriteAllLines("Logs/used_assets.txt", used.OrderBy(p => p));
        }
    }
}
