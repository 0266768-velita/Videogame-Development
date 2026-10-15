using System.Collections.Generic;
using UnityEngine;

public enum TerrainKind { Ground, Water, Pit }

/// <summary>
/// Knows where the water and the pits are. Water/pit areas have physical colliders (they block walking, for the
/// player and the enemies); Tlahuelli can jump over them: while he is in the air those colliders are ignored,
/// and if he lands inside one he falls (see Player2D).
/// </summary>
public static class Hazards
{
    static readonly List<Rect> water = new List<Rect>();
    static readonly List<Rect> pits = new List<Rect>();
    static readonly List<Collider2D> blockers = new List<Collider2D>();

    public static IReadOnlyList<Collider2D> Blockers => blockers;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Clear() { water.Clear(); pits.Clear(); blockers.Clear(); }

    public static void Add(TerrainKind kind, Rect rect, Collider2D collider)
    {
        (kind == TerrainKind.Water ? water : pits).Add(rect);
        if (collider != null) blockers.Add(collider);
    }

    public static TerrainKind KindAt(Vector2 p)
    {
        for (int i = 0; i < water.Count; i++) if (water[i].Contains(p)) return TerrainKind.Water;
        for (int i = 0; i < pits.Count; i++) if (pits[i].Contains(p)) return TerrainKind.Pit;
        return TerrainKind.Ground;
    }

    /// <summary>True when the point and a ring of 'margin' tiles around it are all solid ground.</summary>
    public static bool IsSafe(Vector2 p, float margin)
    {
        if (KindAt(p) != TerrainKind.Ground) return false;
        if (margin <= 0f) return true;
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI * 0.25f;
            if (KindAt(p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * margin) != TerrainKind.Ground) return false;
        }
        return true;
    }
}
