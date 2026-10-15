using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Aztec-styled frames (stepped-fret "greca" border, obsidian fill) drawn in code, so every button and panel
/// gets a texture that matches its exact size (no stretching). Pixel scale: 1 texel = 2 UI units.
/// This is a 1:1 port of Tools/aztec_ui_preview.py (run it to see the frames as a PNG).
/// </summary>
public static class AztecUI
{
    public enum Look { Normal, Highlight, Selected, Pressed, Disabled, Panel }

    const float TexelSize = 2f;
    const int Band = 5;

    static readonly string[] Fret =
    {
        "11111110",
        "10000010",
        "10111010",
        "10100010",
        "10111110",
    };

    struct Pal
    {
        public Color32 fill, fill2, band, bright, mid, edge, line, glint;
        public bool hasGlint;
        public byte alpha;
    }

    static readonly Dictionary<long, Sprite> cache = new Dictionary<long, Sprite>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { cache.Clear(); }

    static Color32 C(int r, int g, int b) { return new Color32((byte)r, (byte)g, (byte)b, 255); }

    static Pal GetPal(Look look)
    {
        var p = new Pal();
        p.alpha = 255;
        switch (look)
        {
            case Look.Highlight:
                p.fill = C(62, 17, 15); p.fill2 = C(72, 22, 18); p.band = C(92, 28, 22);
                p.bright = C(96, 232, 214); p.mid = C(52, 160, 150); p.edge = C(8, 3, 3); p.line = C(40, 12, 10);
                break;
            case Look.Selected:
                p.fill = C(124, 24, 26); p.fill2 = C(140, 30, 30); p.band = C(150, 40, 30);
                p.bright = C(255, 226, 110); p.mid = C(220, 170, 60); p.edge = C(8, 3, 3); p.line = C(60, 14, 12);
                p.glint = C(255, 150, 90); p.hasGlint = true;
                break;
            case Look.Pressed:
                p.fill = C(176, 66, 52); p.fill2 = C(188, 74, 58); p.band = C(190, 80, 60);
                p.bright = C(255, 244, 190); p.mid = C(240, 200, 120); p.edge = C(8, 3, 3); p.line = C(90, 30, 24);
                break;
            case Look.Disabled:
                p.fill = C(30, 28, 28); p.fill2 = C(34, 32, 32); p.band = C(46, 44, 44);
                p.bright = C(100, 98, 92); p.mid = C(72, 70, 66); p.edge = C(10, 9, 9); p.line = C(24, 22, 22);
                break;
            case Look.Panel:
                p.fill = C(16, 7, 8); p.fill2 = C(21, 10, 10); p.band = C(44, 14, 13);
                p.bright = C(176, 140, 40); p.mid = C(112, 86, 26); p.edge = C(6, 2, 2); p.line = C(22, 8, 8);
                p.alpha = 236;
                break;
            default:
                p.fill = C(26, 11, 11); p.fill2 = C(33, 15, 13); p.band = C(62, 20, 17);
                p.bright = C(214, 172, 52); p.mid = C(150, 112, 32); p.edge = C(8, 3, 3); p.line = C(30, 10, 9);
                break;
        }
        return p;
    }

    static int Hash(int x, int y)
    {
        int h = (x * 73856093) ^ (y * 19349663);
        return (h ^ (h >> 7)) & 0xFF;
    }

    /// <summary>Returns a sprite sized for a w x h (UI units) rectangle. Cached per size and look.</summary>
    public static Sprite Get(float w, float h, Look look)
    {
        int tw = Mathf.Max(2 * Band + 6, Mathf.RoundToInt(w / TexelSize));
        int th = Mathf.Max(2 * Band + 6, Mathf.RoundToInt(h / TexelSize));
        long key = ((long)tw * 4096L + th) * 8L + (long)look;
        Sprite s;
        if (cache.TryGetValue(key, out s) && s != null) return s;

        var tex = new Texture2D(tw, th, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.hideFlags = HideFlags.HideAndDontSave;
        tex.SetPixels32(Draw(tw, th, look));
        tex.Apply(false, true);
        s = Sprite.Create(tex, new Rect(0, 0, tw, th), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        s.hideFlags = HideFlags.HideAndDontSave;
        cache[key] = s;
        return s;
    }

    static Color32[] Draw(int tw, int th, Look look)
    {
        Pal p = GetPal(look);
        var px = new Color32[tw * th];   // index = y * tw + x, y = 0 is the BOTTOM row (Unity); we draw top-down and flip below
        var top = new Color32[tw * th];  // top-down working buffer: index = y * tw + x, y = 0 is the TOP row

        for (int y = 0; y < th; y++)
            for (int x = 0; x < tw; x++)
                Put(top, tw, th, x, y, Hash(x, y) % 6 == 0 ? p.fill2 : p.fill, p.alpha);

        for (int x = 0; x < tw; x++) { Put(top, tw, th, x, 0, p.edge, p.alpha); Put(top, tw, th, x, th - 1, p.edge, p.alpha); }
        for (int y = 0; y < th; y++) { Put(top, tw, th, 0, y, p.edge, p.alpha); Put(top, tw, th, tw - 1, y, p.edge, p.alpha); }

        for (int y = 1; y < th - 1; y++)
            for (int x = 1; x < tw - 1; x++)
                if (Mathf.Min(Mathf.Min(x - 1, tw - 2 - x), Mathf.Min(y - 1, th - 2 - y)) < Band)
                    Put(top, tw, th, x, y, p.band, p.alpha);

        for (int y = 1 + Band; y < th - 1 - Band; y++)
            for (int x = 1 + Band; x < tw - 1 - Band; x++)
            {
                int d = Mathf.Min(Mathf.Min(x - (1 + Band), tw - 2 - Band - x), Mathf.Min(y - (1 + Band), th - 2 - Band - y));
                if (d == 0) Put(top, tw, th, x, y, p.line, p.alpha);
                else if (d == 1 && p.hasGlint) Put(top, tw, th, x, y, p.glint, p.alpha);
            }

        // corner studs
        int[] cxs = { 1, tw - 1 - Band, 1, tw - 1 - Band };
        int[] cys = { 1, 1, th - 1 - Band, th - 1 - Band };
        for (int k = 0; k < 4; k++)
            for (int r = 0; r < Band; r++)
                for (int c = 0; c < Band; c++)
                {
                    bool center = r == Band / 2 && c == Band / 2;
                    Put(top, tw, th, cxs[k] + c, cys[k] + r, center ? p.band : FretColor(p, r, c), p.alpha);
                }

        // horizontal runs (top and bottom)
        int x0 = 1 + Band, x1 = tw - 1 - Band;
        int n = (x1 - x0) / 8;
        int off = x0 + ((x1 - x0) - n * 8) / 2;
        for (int k = 0; k < n; k++)
            for (int r = 0; r < Band; r++)
                for (int c = 0; c < 8; c++)
                    if (Fret[r][c] == '1')
                    {
                        Color32 col = FretColor(p, r, c);
                        Put(top, tw, th, off + k * 8 + c, 1 + r, col, p.alpha);
                        Put(top, tw, th, off + k * 8 + c, th - 2 - r, col, p.alpha);
                    }

        // vertical runs (left and right)
        int y0 = 1 + Band, y1 = th - 1 - Band;
        n = (y1 - y0) / 8;
        off = y0 + ((y1 - y0) - n * 8) / 2;
        for (int k = 0; k < n; k++)
            for (int r = 0; r < Band; r++)
                for (int c = 0; c < 8; c++)
                    if (Fret[r][c] == '1')
                    {
                        Color32 col = FretColor(p, r, c);
                        Put(top, tw, th, 1 + r, off + k * 8 + c, col, p.alpha);
                        Put(top, tw, th, tw - 2 - r, off + k * 8 + c, col, p.alpha);
                    }

        // flip vertically (Texture2D rows start at the bottom)
        for (int y = 0; y < th; y++)
            for (int x = 0; x < tw; x++)
                px[(th - 1 - y) * tw + x] = top[y * tw + x];
        return px;
    }

    static Color32 FretColor(Pal p, int r, int c) { return (r == 0 || c == 0) ? p.bright : p.mid; }

    static void Put(Color32[] buf, int tw, int th, int x, int y, Color32 c, byte alpha)
    {
        if (x < 0 || y < 0 || x >= tw || y >= th) return;
        buf[y * tw + x] = new Color32(c.r, c.g, c.b, alpha);
    }

    // ------------------------------------------------------------------ helpers
    /// <summary>Gives an Image a static Aztec frame (panels, cards).</summary>
    public static void StylePanel(Image img, float w, float h)
    {
        img.sprite = Get(w, h, Look.Panel);
        img.type = Image.Type.Simple;
        img.color = Color.white;
    }

    /// <summary>Gives a Button an Aztec frame with sprite-swap states (normal, hover, selected, pressed, disabled).</summary>
    public static void StyleButton(Button btn, Image img, float w, float h)
    {
        img.sprite = Get(w, h, Look.Normal);
        img.type = Image.Type.Simple;
        img.color = Color.white;
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.SpriteSwap;
        var ss = new SpriteState();
        ss.highlightedSprite = Get(w, h, Look.Highlight);
        ss.selectedSprite = Get(w, h, Look.Selected);
        ss.pressedSprite = Get(w, h, Look.Pressed);
        ss.disabledSprite = Get(w, h, Look.Disabled);
        btn.spriteState = ss;
    }
}
