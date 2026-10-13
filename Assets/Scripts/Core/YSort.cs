using UnityEngine;

/// <summary>
/// Top-down draw order: whatever is lower on screen is drawn on top.
/// Put it on the player, enemies and trees. Use isStatic = true for objects that never move.
/// </summary>
public class YSort : MonoBehaviour
{
    public bool isStatic = false;
    public float yOffset = 0f;
    SpriteRenderer[] renderers;

    void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        Apply();
    }

    void LateUpdate()
    {
        if (!isStatic) Apply();
    }

    void Apply()
    {
        int order = -Mathf.RoundToInt((transform.position.y + yOffset) * 100f);
        foreach (var r in renderers) if (r != null) r.sortingOrder = order;
    }
}
