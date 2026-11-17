using System.Collections.Generic;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Blood and gore (the game is 17+), made with Unity's own tools:
    ///   - every hit leaves blood on the floor AND on the enemy: the more you hit it, the bloodier it looks;
    ///   - when an enemy dies, its own picture is cut into pieces (Sprite.Create) that fly away with a Rigidbody2D,
    ///     plus bones and a blood pool.
    /// It can be turned off in Options (GoreEffects.Enabled). Put one in each level scene.
    /// </summary>
    public class GoreEffects : MonoBehaviour
    {
        [SerializeField] Sprite[] bloodStains;   // blood_0 ... blood_5
        [SerializeField] Sprite[] gibs;          // gib_0_0 ... gib_3_2 (bones and bits)
        [SerializeField] int piecesAcross = 3;   // the dead enemy is cut into piecesAcross x piecesAcross parts
        [SerializeField] float pieceSpeed = 6f;
        [SerializeField] int maxBloodOnEnemy = 8;
        [SerializeField] float stainsLifetime = 60f;   // seconds before the blood disappears

        /// <summary>On/off, saved in PlayerPrefs (Options menu).</summary>
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt("td_gore", 1) == 1;
            set => PlayerPrefs.SetInt("td_gore", value ? 1 : 0);
        }

        // Blood spots already painted on each enemy.
        readonly Dictionary<CombatEnemy, List<GameObject>> spots = new Dictionary<CombatEnemy, List<GameObject>>();

        void Start()
        {
            // Listen to every enemy: when it is hit, blood.
            foreach (var enemy in FindObjectsByType<CombatEnemy>(FindObjectsInactive.Include))
            {
                var target = enemy;   // a copy for the lambda
                enemy.GetComponent<Health>().Damaged += damage => OnEnemyHit(target);
            }
        }

        void OnEnable() => CombatEnemy.Defeated += OnEnemyDied;
        void OnDisable() => CombatEnemy.Defeated -= OnEnemyDied;

        // ---------------- Hits: blood on the floor and on the enemy ----------------

        void OnEnemyHit(CombatEnemy enemy)
        {
            if (!Enabled) return;
            SpawnStain(enemy.transform.position + (Vector3)Random.insideUnitCircle * 0.6f, Random.Range(0.6f, 1f), -5);

            // The lower its health, the more blood spots it carries.
            int wanted = Mathf.RoundToInt((1f - enemy.Health.Normalized) * maxBloodOnEnemy);
            if (!spots.TryGetValue(enemy, out var list)) spots[enemy] = list = new List<GameObject>();
            var sprite = enemy.Sprite;
            if (sprite == null) return;
            while (list.Count < wanted)
            {
                // a small stain somewhere on the body (the middle part of the picture)
                Bounds b = sprite.bounds;
                Vector3 p = new Vector3(Random.Range(b.min.x + b.size.x * 0.25f, b.max.x - b.size.x * 0.25f),
                                        Random.Range(b.min.y + b.size.y * 0.15f, b.max.y - b.size.y * 0.3f), 0f);
                var spot = Stain(p, Random.Range(0.25f, 0.45f), sprite.sortingOrder + 1);
                spot.transform.SetParent(sprite.transform, true);   // it moves with the enemy
                list.Add(spot);
            }
        }

        // ---------------- Death: the enemy bursts into pieces ----------------

        void OnEnemyDied(CombatEnemy enemy)
        {
            if (!Enabled) return;
            var sprite = enemy.Sprite;
            Vector3 position = enemy.transform.position;
            SpawnStain(position, 2.2f, -5);   // big blood pool

            if (sprite != null && sprite.sprite != null)
            {
                CutIntoPieces(sprite);
                sprite.enabled = false;        // the pieces replace the enemy
            }
            if (spots.TryGetValue(enemy, out var list))
            {
                foreach (var spot in list) if (spot != null) Destroy(spot);
                spots.Remove(enemy);
            }
            for (int i = 0; i < 4; i++) Fly(NewPiece(gibs.Length > 0 ? gibs[Random.Range(0, gibs.Length)] : null, position + Vector3.up * 0.4f, false, -4));
            for (int i = 0; i < 6; i++) SpawnStain(position + (Vector3)Random.insideUnitCircle * 1.6f, Random.Range(0.4f, 0.8f), -5);
            GameAudio.PlaySfx("gib");
        }

        /// <summary>Cuts the current picture of the enemy into a grid of small sprites and throws them away.</summary>
        void CutIntoPieces(SpriteRenderer source)
        {
            Sprite whole = source.sprite;
            Rect area = whole.textureRect;               // where this frame is inside its texture
            float ppu = whole.pixelsPerUnit;
            Vector2 pivot = whole.pivot;                 // in pixels, from the bottom-left of the frame
            Vector3 scale = source.transform.lossyScale;
            float w = area.width / piecesAcross, h = area.height / piecesAcross;

            for (int i = 0; i < piecesAcross; i++)
                for (int j = 0; j < piecesAcross; j++)
                {
                    var rect = new Rect(area.x + i * w, area.y + j * h, w, h);
                    var piece = Sprite.Create(whole.texture, rect, new Vector2(0.5f, 0.5f), ppu);
                    // Where this piece was on the enemy (relative to the enemy's pivot).
                    float localX = (i * w + w / 2f - pivot.x) / ppu * scale.x;
                    float localY = (j * h + h / 2f - pivot.y) / ppu * scale.y;
                    if (source.flipX) localX = -localX;
                    var go = NewPiece(piece, source.transform.position + new Vector3(localX, localY, 0f), source.flipX, source.sortingOrder);
                    go.transform.localScale = scale;
                    go.GetComponent<SpriteRenderer>().color = new Color(1f, 0.8f, 0.8f);   // bloody pieces
                    Fly(go);
                }
        }

        GameObject NewPiece(Sprite sprite, Vector3 position, bool flip, int order)
        {
            var go = new GameObject("Gib");
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.flipX = flip;
            sr.sortingOrder = order;
            Destroy(go, stainsLifetime);
            return go;
        }

        /// <summary>Unity physics does the flying: a push away from the body; damping slows it down until it stops.</summary>
        void Fly(GameObject piece)
        {
            var body = piece.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.linearDamping = 3.5f;
            body.angularDamping = 2f;
            body.linearVelocity = Random.insideUnitCircle.normalized * Random.Range(pieceSpeed * 0.4f, pieceSpeed);
            body.angularVelocity = Random.Range(-720f, 720f);
        }

        void SpawnStain(Vector3 position, float size, int order) => Stain(position, size, order);

        GameObject Stain(Vector3 position, float size, int order)
        {
            var stain = new GameObject("Blood");
            stain.transform.position = position;
            stain.transform.localScale = Vector3.one * size;
            stain.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            var sr = stain.AddComponent<SpriteRenderer>();
            if (bloodStains != null && bloodStains.Length > 0) sr.sprite = bloodStains[Random.Range(0, bloodStains.Length)];
            sr.sortingOrder = order;
            Destroy(stain, stainsLifetime);
            return stain;
        }
    }
}
