using UnityEngine;

/// <summary>
/// The sealed door of the pyramid. Stays closed until Tlahuelli has the obsidian key; then it opens and
/// walking through it enters the boss chamber (BossRoom).
/// </summary>
public class PyramidDoor : MonoBehaviour
{
    public Vector2 doorSize = new Vector2(2f, 2.25f);

    SpriteRenderer sr;
    BoxCollider2D blocker;
    Player2D player;
    bool open;
    float messageCooldown;

    void Awake()
    {
        var child = new GameObject("DoorSprite");
        child.transform.SetParent(transform, false);
        sr = child.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteLib.Single("door_closed");
        sr.sortingOrder = -28990;

        blocker = gameObject.AddComponent<BoxCollider2D>();
        blocker.size = doorSize;
    }

    void Update()
    {
        if (GameState.Mode != GameMode.Exploration) return;
        if (player == null) { player = FindFirstObjectByType<Player2D>(); if (player == null) return; }

        messageCooldown -= Time.deltaTime;
        Vector2 d = player.Position - (Vector2)transform.position;

        if (!open)
        {
            if (d.magnitude < 4f && GameProgress.HasKey) Open();
            else if (d.magnitude < 3f && messageCooldown <= 0f)
            {
                messageCooldown = 9f;
                TutorialTips.Instance.Toast(null, GameProgress.KeyDropped
                    ? "The door is sealed by an obsidian lock. Find the key that dropped."
                    : "The door is sealed by an obsidian lock. Defeat every enemy in the land to get the key.", 6f);
            }
            return;
        }

        // Open: stepping into the doorway enters the chamber
        if (Mathf.Abs(d.x) < 0.9f && d.y > -0.4f && d.y < 1.5f) BossRoom.Enter(player);
    }

    void Open()
    {
        open = true;
        sr.sprite = SpriteLib.Single("door_open");
        blocker.enabled = false;
        Audio.Play("door");
        TutorialTips.Instance.Toast(null, "The obsidian key turns... the pyramid door opens. Quetzalcoatl waits inside. Are you ready?", 8f);
    }
}
