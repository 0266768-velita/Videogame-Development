using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Boot screens (GDD section 7): studio logo -> rating screen -> legal notice -> Main Menu.
/// Any key / click / gamepad button skips the current screen (after 0.5 s).
///
/// WHERE TO PUT YOUR LOGO: in the Boot scene, select the "BootSplash" object and drag your PNG
/// into the "Studio Logo" field (any image works, it does not need to be set up as a Sprite).
/// The Micta Forge logo is already assigned by default.
/// </summary>
public class BootSplash : MonoBehaviour
{
    [Header("1) Studio logo")]
    public Texture2D studioLogo;
    public string studioName = "MICTA FORGE";
    public float logoSeconds = 3f;
    public AudioClip logoSound;

    [Header("2) Rating (optional: assign the official ESRB image)")]
    public Texture2D ratingImage;
    public float ratingSeconds = 3f;

    [Header("3) Legal notice")]
    [TextArea(2, 5)]
    public string legalText = "Mictlán is an original work inspired by Mexica mythology.\nCharacters, events and brands shown here are fictional.";
    public float legalSeconds = 3.5f;

    class Slide { public CanvasGroup group; public float seconds; }
    readonly List<Slide> slides = new List<Slide>();

    void Start()
    {
        Time.timeScale = 1f;
        GameState.SetMode(GameMode.Exploration);

        var content = UIKit.CreateScreen("BootCanvas", 10, transform);
        UIKit.Stretch(UIKit.NewImage("Black", content, Color.black).rectTransform);

        BuildLogoSlide(content);
        BuildRatingSlide(content);
        BuildLegalSlide(content);

        StartCoroutine(Run());
    }

    CanvasGroup NewSlide(RectTransform parent, string name, float seconds)
    {
        var r = UIKit.NewRect(name, parent);
        UIKit.Stretch(r);
        var g = r.gameObject.AddComponent<CanvasGroup>();
        g.alpha = 0f;
        slides.Add(new Slide { group = g, seconds = seconds });
        return g;
    }

    void BuildLogoSlide(RectTransform parent)
    {
        var g = NewSlide(parent, "Slide_Logo", logoSeconds);
        if (studioLogo != null)
        {
            var raw = UIKit.NewRawImage("Logo", g.transform, studioLogo);
            float h = 520f;
            float w = h * studioLogo.width / Mathf.Max(1, studioLogo.height);
            UIKit.Center(raw.rectTransform, Vector2.zero, new Vector2(w, h));
        }
        else
        {
            // Placeholder: shown until you drag your logo into the "Studio Logo" field.
            var t = UIKit.NewText("StudioName", g.transform, studioName, 84, TextAnchor.MiddleCenter, UIKit.Accent);
            t.fontStyle = FontStyle.Bold;
            UIKit.Center(t.rectTransform, new Vector2(0, 20), new Vector2(1000, 120));
            var hint = UIKit.NewText("Hint", g.transform, "(studio logo goes here: drag it into BootSplash > Studio Logo)", 20, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.45f));
            UIKit.Center(hint.rectTransform, new Vector2(0, -60), new Vector2(1000, 40));
        }
    }

    void BuildRatingSlide(RectTransform parent)
    {
        var g = NewSlide(parent, "Slide_Rating", ratingSeconds);
        if (ratingImage != null)
        {
            var raw = UIKit.NewRawImage("Rating", g.transform, ratingImage);
            float h = 360f;
            float w = h * ratingImage.width / Mathf.Max(1, ratingImage.height);
            UIKit.Center(raw.rectTransform, Vector2.zero, new Vector2(w, h));
            return;
        }
        var box = UIKit.NewImage("Box", g.transform, Color.white);
        UIKit.Center(box.rectTransform, new Vector2(0, 20), new Vector2(210, 270));
        var inner = UIKit.NewImage("Inner", box.transform, Color.black);
        UIKit.Stretch(inner.rectTransform);
        inner.rectTransform.offsetMin = new Vector2(8, 8);
        inner.rectTransform.offsetMax = new Vector2(-8, -8);
        var m = UIKit.NewText("M", inner.transform, "M", 170, TextAnchor.MiddleCenter, Color.white);
        m.fontStyle = FontStyle.Bold;
        UIKit.Center(m.rectTransform, new Vector2(0, 20), new Vector2(200, 200));
        var label = UIKit.NewText("Label", inner.transform, "MATURE 17+", 28, TextAnchor.MiddleCenter, Color.white);
        UIKit.Center(label.rectTransform, new Vector2(0, -95), new Vector2(200, 40));
        var note = UIKit.NewText("Note", g.transform, "ESRB rating: M (Mature)", 22, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.7f));
        UIKit.Center(note.rectTransform, new Vector2(0, -150), new Vector2(900, 40));
    }

    void BuildLegalSlide(RectTransform parent)
    {
        var g = NewSlide(parent, "Slide_Legal", legalSeconds);
        var t = UIKit.NewText("Legal", g.transform, legalText, 28, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.9f));
        UIKit.Center(t.rectTransform, Vector2.zero, new Vector2(1000, 200));
    }

    IEnumerator Run()
    {
        yield return null;
        if (logoSound != null) AudioSource.PlayClipAtPoint(logoSound, Vector3.zero);
        else Audio.Play("logo");

        foreach (var s in slides)
        {
            yield return Fade(s.group, 1f, 0.6f);

            float t = 0f;
            while (t < s.seconds)
            {
                t += Time.unscaledDeltaTime;
                if (t > 0.5f && UIKit.AnyPress()) break;
                yield return null;
            }
            yield return Fade(s.group, 0f, 0.5f);
        }
        SceneLoader.Load(SceneNames.MainMenu);
    }

    IEnumerator Fade(CanvasGroup g, float target, float seconds)
    {
        float start = g.alpha;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            g.alpha = Mathf.Lerp(start, target, t / seconds);
            yield return null;
        }
        g.alpha = target;
    }
}
