using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// The torn painting of the witches' house, on its own screen: the picture is cut in a grid of pieces and shuffled.
    /// Pick a piece and then another one to swap them (mouse: click; keyboard / gamepad: move the gold frame with
    /// the arrows / WASD / D-pad and press {Confirm}). A piece that lands in its place shines and chimes and stays fixed.
    /// When every piece is in place the painting glows and the screen closes by itself. {Back} leaves (the pieces stay
    /// where you left them).
    /// Usage: yield return puzzle.Run(); then check puzzle.Solved.
    /// </summary>
    public class PaintingPuzzle : MonoBehaviour
    {
        [SerializeField] Font font;
        [SerializeField] Texture2D picture;
        [SerializeField, Range(2, 5)] int columns = 3;
        [SerializeField, Range(2, 5)] int rows = 3;
        [SerializeField] string title = "THE TORN CODEX";
        [SerializeField, TextArea] string caption = "Put the painting back together.";

        public bool Solved { get; private set; }

        GameObject root;
        Text captionText, hintText;
        RectTransform board;
        readonly List<RawImage> slots = new List<RawImage>();
        readonly List<Image> frames = new List<Image>();
        int[] pieceAt;          // which piece is in each slot
        int cursor, picked = -1;
        bool running, quit;
        float pieceSize;

        public IEnumerator Run()
        {
            if (Solved || running) yield break;
            running = true;
            quit = false;
            if (root == null) Build();
            root.SetActive(true);
            var previous = GameState.Mode;
            GameState.SetMode(GameMode.Cutscene);
            GameAudio.PlaySfx("ui_select", 1f, 0f);
            Refresh();
            yield return null;

            while (!Solved && !quit)
            {
                HandleKeys();
                yield return null;
            }

            if (Solved) yield return Celebrate();
            root.SetActive(false);
            yield return null;
            GameState.SetMode(previous == GameMode.Cutscene ? GameMode.Exploration : previous);
            running = false;
        }

        /// <summary>For a loaded save: the painting is already whole.</summary>
        public void MarkSolved() => Solved = true;

        // ---------------- Input ----------------

        void HandleKeys()
        {
            if (MenuWidgets.CancelPressed()) { quit = true; GameAudio.PlaySfx("ui_back", 1f, 0f); return; }
            var k = Keyboard.current;
            var pad = Gamepad.current;
            int dx = 0, dy = 0;
            if (k != null)
            {
                if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) dx = -1;
                if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) dx = 1;
                if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) dy = -1;
                if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) dy = 1;
            }
            if (pad != null)
            {
                if (pad.dpad.left.wasPressedThisFrame) dx = -1;
                if (pad.dpad.right.wasPressedThisFrame) dx = 1;
                if (pad.dpad.up.wasPressedThisFrame) dy = -1;
                if (pad.dpad.down.wasPressedThisFrame) dy = 1;
            }
            if (dx != 0 || dy != 0)
            {
                int c = Mathf.Clamp(cursor % columns + dx, 0, columns - 1);
                int r = Mathf.Clamp(cursor / columns + dy, 0, rows - 1);
                cursor = r * columns + c;
                GameAudio.PlaySfx("ui_move", 0.6f, 0.05f);
                Refresh();
            }
            bool confirm = (k != null && (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame))
                           || (pad != null && pad.buttonSouth.wasPressedThisFrame);
            if (confirm) Pick(cursor);
        }

        void Pick(int slot)
        {
            if (IsPlaced(slot)) { GameAudio.PlaySfx("error", 0.4f); return; }   // pieces in place stay fixed
            cursor = slot;
            if (picked < 0)
            {
                picked = slot;
                GameAudio.PlaySfx("ui_select", 0.7f, 0f);
            }
            else if (picked == slot) picked = -1;
            else
            {
                (pieceAt[picked], pieceAt[slot]) = (pieceAt[slot], pieceAt[picked]);
                GameAudio.PlaySfx("swing", 0.35f, 0.1f);
                bool any = false;
                foreach (int s in new[] { picked, slot })
                    if (IsPlaced(s)) { any = true; StartCoroutine(Shine(s)); }
                if (any) GameAudio.PlaySfx("puzzle_place", 0.9f, 0.02f);
                picked = -1;
                Solved = true;
                for (int i = 0; i < pieceAt.Length; i++) if (!IsPlaced(i)) Solved = false;
            }
            Refresh();
        }

        bool IsPlaced(int slot) => pieceAt[slot] == slot;

        // ---------------- Drawing ----------------

        void Refresh()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                int piece = pieceAt[i];
                float w = 1f / columns, h = 1f / rows;
                // Piece 0 is the top-left of the picture (UV rows start at the bottom).
                slots[i].uvRect = new Rect((piece % columns) * w, 1f - (piece / columns + 1) * h, w, h);
                slots[i].color = IsPlaced(i) ? Color.white : new Color(0.82f, 0.78f, 0.74f);
                var frame = frames[i];
                frame.color = i == picked ? MexicaPalette.Turquoise
                    : i == cursor ? MexicaPalette.AgedGold
                    : IsPlaced(i) ? MexicaPalette.WithAlpha(MexicaPalette.Jade, 0.7f)
                    : MexicaPalette.WithAlpha(MexicaPalette.Obsidian, 0.9f);
            }
            int placed = 0;
            for (int i = 0; i < pieceAt.Length; i++) if (IsPlaced(i)) placed++;
            captionText.text = $"{caption}\n<size=24><color=#C9B27A>{placed} / {pieceAt.Length} pieces in place</color></size>";
        }

        IEnumerator Shine(int slot)
        {
            var image = slots[slot];
            for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Sin(t / 0.5f * Mathf.PI);
                image.color = Color.Lerp(Color.white, new Color(1f, 0.9f, 0.55f), k);
                image.rectTransform.localScale = Vector3.one * (1f + 0.08f * k);
                yield return null;
            }
            image.rectTransform.localScale = Vector3.one;
            image.color = Color.white;
        }

        IEnumerator Celebrate()
        {
            GameAudio.PlaySfx("puzzle_done", 1f, 0f);
            hintText.text = "";
            captionText.text = "The painting is whole again.\n<size=24><color=#C9B27A>Something breaks in the walls of the house...</color></size>";
            foreach (var f in frames) f.color = MexicaPalette.AgedGold;
            for (float t = 0f; t < 1.6f; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.PingPong(t * 2.5f, 1f);
                foreach (var s in slots) s.color = Color.Lerp(Color.white, new Color(1f, 0.92f, 0.6f), k);
                board.localScale = Vector3.one * (1f + 0.02f * k);
                yield return null;
            }
            board.localScale = Vector3.one;
            yield return new WaitForSecondsRealtime(0.6f);
        }

        void Build()
        {
            MenuWidgets.Use(font, true);
            var canvas = MenuWidgets.Screen("PuzzleCanvas", 46, transform);
            root = canvas.gameObject;
            // Its own look: a dark ritual room, candle light from below and the painting on an old wooden frame.
            MenuWidgets.Stretch(MenuWidgets.Image("Shade", canvas, new Color(0.05f, 0.02f, 0.04f, 1f)).rectTransform);

            var titleText = MenuWidgets.Label("Title", canvas, title, 58, MexicaPalette.AgedGold);
            MenuWidgets.Center(titleText.rectTransform, new Vector2(0, 465), new Vector2(1400, 80));

            pieceSize = Mathf.Floor(660f / Mathf.Max(columns, rows));
            var wood = MenuWidgets.Image("Frame", canvas, new Color(0.32f, 0.2f, 0.12f));
            MenuWidgets.Center(wood.rectTransform, new Vector2(0, 20), new Vector2(pieceSize * columns + 70, pieceSize * rows + 70));
            var woodInner = MenuWidgets.Image("Inner", wood.transform, new Color(0.12f, 0.07f, 0.05f));
            MenuWidgets.Stretch(woodInner.rectTransform);
            woodInner.rectTransform.offsetMin = new Vector2(18, 18);
            woodInner.rectTransform.offsetMax = new Vector2(-18, -18);
            board = MenuWidgets.Rect("Board", canvas);
            MenuWidgets.Center(board, new Vector2(0, 20), new Vector2(pieceSize * columns, pieceSize * rows));

            int count = columns * rows;
            pieceAt = new int[count];
            for (int i = 0; i < count; i++) pieceAt[i] = i;
            Shuffle();

            for (int i = 0; i < count; i++)
            {
                int slot = i;
                Vector2 pos = new Vector2((i % columns + 0.5f - columns / 2f) * pieceSize, -(i / columns + 0.5f - rows / 2f) * pieceSize);
                var frame = MenuWidgets.Image("Slot", board, Color.black);
                MenuWidgets.Center(frame.rectTransform, pos, new Vector2(pieceSize, pieceSize));
                frame.raycastTarget = true;
                var button = frame.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                var nav = button.navigation;
                nav.mode = Navigation.Mode.None;
                button.navigation = nav;
                button.onClick.AddListener(() =>
                {
                    Pick(slot);
                    // Keep nothing selected: Space / Enter are read by this screen, not by the UI buttons.
                    if (UnityEngine.EventSystems.EventSystem.current != null)
                        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                });
                var piece = MenuWidgets.Picture("Piece", frame.transform, picture);
                MenuWidgets.Stretch(piece.rectTransform);
                piece.rectTransform.offsetMin = new Vector2(4, 4);
                piece.rectTransform.offsetMax = new Vector2(-4, -4);
                frames.Add(frame);
                slots.Add(piece);
            }

            captionText = MenuWidgets.Label("Caption", canvas, "", 30, MexicaPalette.Bone);
            MenuWidgets.Center(captionText.rectTransform, new Vector2(0, -410), new Vector2(1500, 100));
            hintText = MenuWidgets.Label("Hint", canvas, "", 24, MexicaPalette.Ash);
            MenuWidgets.Center(hintText.rectTransform, new Vector2(0, -490), new Vector2(1500, 40));
            hintText.text = InputPrompts.Localize("Click two pieces to swap them  ·  {Move} move  ·  {Confirm} pick  ·  {Back} leave");
            root.SetActive(false);
        }

        /// <summary>A shuffle where no piece starts in its own place.</summary>
        void Shuffle()
        {
            var random = new System.Random(7);
            for (int attempt = 0; attempt < 100; attempt++)
            {
                for (int i = pieceAt.Length - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    (pieceAt[i], pieceAt[j]) = (pieceAt[j], pieceAt[i]);
                }
                bool any = false;
                for (int i = 0; i < pieceAt.Length; i++) if (pieceAt[i] == i) any = true;
                if (!any) return;
            }
        }
    }
}
