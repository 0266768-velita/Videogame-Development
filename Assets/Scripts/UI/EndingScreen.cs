using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shown after Quetzalcoatl falls: THE END, credits and a button back to the home screen.</summary>
public class EndingScreen : MonoBehaviour
{
    GameObject root;
    CanvasGroup group;
    Button menuButton;
    bool clicked, built;

    public IEnumerator Run()
    {
        Build();
        root.SetActive(true);
        group.alpha = 0f;
        Audio.PlayMusic("music_menu", true, 1.5f);
        Audio.Play("victory");

        for (float t = 0f; t < 2f; t += Time.unscaledDeltaTime)
        {
            group.alpha = t / 2f;
            yield return null;
        }
        group.alpha = 1f;
        menuButton.interactable = true;
        UIKit.Select(menuButton.gameObject);

        while (!clicked) yield return null;
        menuButton.interactable = false;
        SceneLoader.Load(SceneNames.MainMenu);
        while (true) yield return null;   // the scene is unloading
    }

    void Build()
    {
        if (built) return;
        built = true;
        UIKit.EnsureEventSystem();
        var content = UIKit.CreateScreen("EndingCanvas", 70, transform);
        root = content.transform.parent.gameObject;
        group = content.gameObject.AddComponent<CanvasGroup>();

        UIKit.Stretch(UIKit.NewImage("Dim", content, new Color(0.01f, 0.01f, 0.03f, 0.94f)).rectTransform);

        var title = UIKit.NewText("Title", content, "THE END", 120, TextAnchor.MiddleCenter, new Color(0.35f, 0.9f, 0.7f));
        title.fontStyle = FontStyle.Bold;
        title.gameObject.AddComponent<Outline>().effectColor = Color.black;
        UIKit.Center(title.rectTransform, new Vector2(0, 220), new Vector2(1200, 150));

        var story = UIKit.NewText("Story", content,
            "Quetzalcoatl, the Feathered Serpent, falls silent.\n" +
            "The way through Mictlan is open and Tlahuelli's journey goes on...\n\n" +
            "Thanks for playing.", 30, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.9f));
        UIKit.Center(story.rectTransform, new Vector2(0, 80), new Vector2(1300, 200));

        var credits = UIKit.NewText("Credits", content,
            "MICTLÁN  -  A game by Micta Forge\n" +
            "Jetzuvely del Carmen González Gutiérrez   Adrián Gallardo Medina\n" +
            "Antonio Enríquez Velasco   Alejandro Cornejo Pérez", 22, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.65f));
        UIKit.Center(credits.rectTransform, new Vector2(0, -80), new Vector2(1500, 110));

        menuButton = UIKit.NewButton(content, "Main Menu", new Vector2(0, -230), new Vector2(400, 60), () => clicked = true, 28);
        menuButton.interactable = false;
    }
}
