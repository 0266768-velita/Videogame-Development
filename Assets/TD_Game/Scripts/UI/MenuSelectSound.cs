using UnityEngine;
using UnityEngine.EventSystems;

namespace TD_Game
{

    /// <summary>Plays the "move" sound when a menu button gets selected with the keyboard, gamepad or mouse.</summary>
    public class MenuSelectSound : MonoBehaviour, ISelectHandler, IPointerEnterHandler
    {
        static float mutedUntil;
        public static void MuteFor(float seconds) => mutedUntil = Time.unscaledTime + seconds;

        public void OnSelect(BaseEventData _) => Play();
        public void OnPointerEnter(PointerEventData _) => Play();

        static void Play()
        {
            if (Time.unscaledTime >= mutedUntil) GameAudio.PlaySfx("ui_move", 0.7f, 0f);
        }
    }
}
