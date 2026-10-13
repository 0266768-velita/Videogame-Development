using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum GameOverChoice { None, PlayAgain, MainMenu }

/// <summary>
/// Game Over screen shown when Tlahuelli falls in battle. Two options:
///   Play Again  - retry from the last checkpoint (the last victory / save), enemies reset.
///   Main Menu   - go back to the home screen.
/// Built in code like the rest of the UI. BattleManager runs it with:  yield return screen.Run();
/// then reads screen.Choice.
/// </summary>
public class GameOverScreen : MonoBehaviour
{
    public GameOverChoice Choice { get; private set; }

    GameObject root;
    CanvasGroup group;
    Text title, subtitle;
    Button playAgain, mainMenu;
    bool built;

    public IEnumerator Run()
    {
        Build();
        Choice = GameOverChoice.None;
        root.SetActive(true);
        group.alpha = 0f;
        title.text = "GAME OVER";
        subtitle.text = SaveSystem.HasSave
            ? "Mictlantecuhtli is not done with you yet."
            : "Mictlantecuhtli is not done with you yet.\nYou will restart from the beginning of the area.";

        Audio.PlayMusic("music_gameover", false, 0.6f);
        Audio.Play("defeat");

        for (float t = 0f; t < 1.2f; t += Time.unscaledDeltaTime)
        {
            group.alpha = t / 1.2f;
            yield return null;
        }
        group.alpha = 1f;
        playAgain.interactable = true;
        mainMenu.interactable = true;
        UIKit.Select(playAgain.gameObject);

        while (Choice == GameOverChoice.None) yield return null;

        playAgain.interactable = false;
        mainMenu.interactable = false;

        if (Choice == GameOverChoice.MainMenu)
        {
            Audio.StopMusic(0.5f);
            SceneLoader.Load(SceneNames.MainMenu);
            // The scene is about to unload; wait here so nothing else runs meanwhile.
            while (true) yield return null;
        }

        // Play again: fade out the screen, then give control back to BattleManager.
        for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
        {
            group.alpha = 1f - t / 0.5f;
            yield return null;
        }
        root.SetActive(false);
    }

    void Build()
    {
        if (built) return;
        built = true;
        UIKit.EnsureEventSystem();

        var content = UIKit.CreateScreen("GameOverCanvas", 60, transform);
        root = content.transform.parent.gameObject;
        group = content.gameObject.AddComponent<CanvasGroup>();

        UIKit.Stretch(UIKit.NewImage("Dim", content, new Color(0.02f, 0f, 0f, 0.88f)).rectTransform);

        // Red band behind the title
        var band = UIKit.NewImage("Band", content, new Color(0.45f, 0.03f, 0.05f, 0.55f));
        UIKit.Center(band.rectTransform, new Vector2(0, 150), new Vector2(1280, 150));

        title = UIKit.NewText("Title", content, "GAME OVER", 110, TextAnchor.MiddleCenter, new Color(0.95f, 0.15f, 0.15f));
        title.fontStyle = FontStyle.Bold;
        title.gameObject.AddComponent<Outline>().effectColor = Color.black;
        UIKit.Center(title.rectTransform, new Vector2(0, 150), new Vector2(1100, 140));

        subtitle = UIKit.NewText("Subtitle", content, "", 26, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.8f));
        UIKit.Center(subtitle.rectTransform, new Vector2(0, 60), new Vector2(1000, 80));

        Vector2 size = new Vector2(400, 60);
        playAgain = UIKit.NewButton(content, "Play Again", new Vector2(0, -40), size, () => Choice = GameOverChoice.PlayAgain, 28);
        mainMenu = UIKit.NewButton(content, "Main Menu", new Vector2(0, -115), size, () => Choice = GameOverChoice.MainMenu, 28);
        playAgain.interactable = false;
        mainMenu.interactable = false;

        var hint = UIKit.NewText("Hint", content, "Play Again retries from your last checkpoint.", 20, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.5f));
        UIKit.Center(hint.rectTransform, new Vector2(0, -200), new Vector2(900, 30));
    }
}
