using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// Debug views of MainMap_CSV for checking the level without playing: a picture of the whole map
    /// (or of a part of it) without the darkness of the night, and a list of the root objects of the scene.
    /// Menu: TD_Game > Debug > Map Snapshot. In batch mode: -executeMethod TD_Game.EditorTools.MapSnapshot.Run
    /// (output folder in the environment variable TD_SNAPSHOT_DIR, default Temp/Snapshots).
    /// </summary>
    public static class MapSnapshot
    {
        static string OutputFolder
        {
            get
            {
                string dir = System.Environment.GetEnvironmentVariable("TD_SNAPSHOT_DIR");
                if (string.IsNullOrEmpty(dir)) dir = Path.Combine(Directory.GetCurrentDirectory(), "Temp/Snapshots");
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        [MenuItem("TD_Game/Debug/Map Snapshot")]
        public static void Run()
        {
            EditorSceneManager.OpenScene(CsvMapBuilder.ScenePath);
            DumpRoots();
            Render("map_full.png", new Rect(0, 0, 300, 140), 8);
            string views = System.Environment.GetEnvironmentVariable("TD_SNAPSHOT_VIEWS");
            if (!string.IsNullOrEmpty(views))
                foreach (string view in views.Split(';'))
                {
                    var p = view.Split(',');
                    if (p.Length < 6) continue;
                    Render(p[0], new Rect(float.Parse(p[1]), float.Parse(p[2]), float.Parse(p[3]), float.Parse(p[4])), int.Parse(p[5]));
                }
        }

        public static void DumpRoots()
        {
            var sb = new StringBuilder();
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                sb.Append(root.name).Append(root.activeSelf ? "" : " (off)").Append("  @").Append(root.transform.position).Append("  [");
                foreach (var c in root.GetComponents<Component>()) if (c != null) sb.Append(c.GetType().Name).Append(' ');
                sb.Append("] children ").Append(root.transform.childCount).AppendLine();
                for (int i = 0; i < root.transform.childCount && i < 40; i++)
                {
                    var child = root.transform.GetChild(i);
                    sb.Append("    ").Append(child.name).Append("  @").Append(child.position).Append("  [");
                    foreach (var c in child.GetComponents<Component>()) if (c != null) sb.Append(c.GetType().Name).Append(' ');
                    sb.AppendLine("]");
                }
            }
            File.WriteAllText(Path.Combine(OutputFolder, "roots.txt"), sb.ToString());
        }

        /// <summary>Renders a world rectangle (in tiles) at a number of pixels per tile, with the night darkness hidden.</summary>
        public static void Render(string file, Rect area, int pixelsPerTile)
        {
            var hidden = new System.Collections.Generic.List<Renderer>();
            foreach (var r in Object.FindObjectsByType<SpriteRenderer>())
                if (r.enabled && (r.sortingOrder == 500 || r.gameObject.layer == LayerMask.NameToLayer("TransparentFX") || r.name.Contains("Darkness")))
                { r.enabled = false; hidden.Add(r); }

            int w = Mathf.RoundToInt(area.width * pixelsPerTile), h = Mathf.RoundToInt(area.height * pixelsPerTile);
            var go = new GameObject("SnapshotCamera");
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = area.height / 2f;
            cam.aspect = area.width / area.height;
            cam.transform.position = new Vector3(area.center.x, area.center.y, -50f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.cullingMask = ~LayerMask.GetMask("UI");
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(OutputFolder, file), tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
            foreach (var r in hidden) r.enabled = true;
        }
    }
}
