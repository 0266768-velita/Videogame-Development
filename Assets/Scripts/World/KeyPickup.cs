using UnityEngine;

/// <summary>The obsidian key that opens the pyramid. Walk over it to pick it up.</summary>
public class KeyPickup : MonoBehaviour
{
    Transform sprite, glow;
    Player2D player;
    float t;

    public static KeyPickup Spawn(Vector2 pos)
    {
        var go = new GameObject("ObsidianKey");
        go.transform.position = pos;
        return go.AddComponent<KeyPickup>();
    }

    void Awake()
    {
        var g = new GameObject("Glow");
        g.transform.SetParent(transform, false);
        var gsr = g.AddComponent<SpriteRenderer>();
        gsr.sprite = SpriteLib.Single("glow");
        gsr.color = new Color(1f, 0.85f, 0.3f, 0.9f);
        gsr.sortingOrder = 7000;
        g.transform.localScale = Vector3.one * 1.2f;
        glow = g.transform;

        var k = new GameObject("Key");
        k.transform.SetParent(transform, false);
        var sr = k.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteLib.Single("key");
        sr.sortingOrder = 7001;
        k.transform.localScale = Vector3.one * 1.4f;
        sprite = k.transform;
    }

    void Update()
    {
        t += Time.deltaTime;
        sprite.localPosition = new Vector3(0f, 0.4f + Mathf.Sin(t * 3f) * 0.12f, 0f);
        glow.localScale = Vector3.one * (1.1f + 0.2f * Mathf.Sin(t * 4f));

        if (GameState.Mode != GameMode.Exploration) return;
        if (player == null) { player = FindFirstObjectByType<Player2D>(); if (player == null) return; }
        if (Vector2.Distance(player.Position, transform.position) > 1.1f) return;

        GameProgress.HasKey = true;
        Audio.Play("key");
        TutorialTips.Instance.Toast(null,
            "You got the OBSIDIAN KEY!  Go to the pyramid in the north. To reach it you must jump over the ravine.", 10f);

        var stats = player.GetComponent<PlayerCombatStats>();
        if (stats != null) SaveSystem.Save(stats, player.Position);
        Destroy(gameObject);
    }
}
