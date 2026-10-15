using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads the pixel art from Assets/Resources/Sprites and cuts sprite sheets into frames at runtime.
/// To use your own art, replace the PNG with the same name and the same frame size.
/// </summary>
public static class SpriteLib
{
    public const float PPU = 16f;
    static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
    static readonly Dictionary<string, Sprite> singles = new Dictionary<string, Sprite>();
    static readonly Dictionary<string, Sprite[]> rows = new Dictionary<string, Sprite[]>();
    static Sprite white;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { textures.Clear(); singles.Clear(); rows.Clear(); white = null; }

    public static Texture2D Tex(string name)
    {
        if (textures.TryGetValue(name, out var t) && t != null) return t;
        t = Resources.Load<Texture2D>("Sprites/" + name);
        if (t == null) Debug.LogWarning($"[Mictlan] Missing sprite 'Resources/Sprites/{name}.png'.");
        else { t.filterMode = FilterMode.Point; t.wrapMode = TextureWrapMode.Clamp; }
        textures[name] = t;
        return t;
    }

    /// <summary>A whole PNG as one sprite.</summary>
    public static Sprite Single(string name, Vector2? pivot = null)
    {
        var p = pivot ?? new Vector2(0.5f, 0.5f);
        string key = name + "|" + p.x + "|" + p.y;
        if (singles.TryGetValue(key, out var s) && s != null) return s;
        var t = Tex(name);
        if (t == null) return White();
        s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), p, PPU, 0, SpriteMeshType.FullRect);
        singles[key] = s;
        return s;
    }

    /// <summary>Frames of one row of a sheet (row 0 = top row). Pivot = center.</summary>
    public static Sprite[] Row(string sheet, int cellW, int cellH, int row, int count)
    {
        string key = $"{sheet}|{cellW}|{cellH}|{row}|{count}";
        if (rows.TryGetValue(key, out var arr) && arr != null && arr.Length > 0 && arr[0] != null) return arr;
        var t = Tex(sheet);
        if (t == null) return new[] { White() };
        int rowsInSheet = Mathf.Max(1, t.height / cellH);
        int yTop = Mathf.Clamp(row, 0, rowsInSheet - 1);
        int maxCols = Mathf.Max(1, t.width / cellW);
        count = Mathf.Clamp(count, 1, maxCols);
        arr = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            var rect = new Rect(i * cellW, t.height - (yTop + 1) * cellH, cellW, cellH);
            arr[i] = Sprite.Create(t, rect, new Vector2(0.5f, 0.5f), PPU, 0, SpriteMeshType.FullRect);
        }
        rows[key] = arr;
        return arr;
    }

    public static Sprite White()
    {
        if (white != null) return white;
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        white = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);   // 1 world unit = 4 px => 1 px = 0.25... scaled by callers
        return white;
    }
}
