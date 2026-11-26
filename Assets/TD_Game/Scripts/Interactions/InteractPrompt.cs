using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// The "{Interact} Talk" message at the bottom of the screen, shared by everything you can use on the map
    /// (merchants, villagers, doors, the painting). Several objects can ask for it; the last one that asked is shown,
    /// and it hides when its owner lets it go. Put it on the MapUI canvas; its Text is the one inside the ShopPrompt plate.
    /// </summary>
    public class InteractPrompt : MonoBehaviour
    {
        [SerializeField] Text text;

        static InteractPrompt instance;
        Object owner;

        void Awake()
        {
            instance = this;
            if (text == null) text = GetComponentInChildren<Text>(true);
            if (text != null) text.transform.parent.gameObject.SetActive(false);   // hidden until something asks for it
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        /// <summary>Shows a message (actions in braces become button icons: "{Interact}  Talk").</summary>
        public static void Show(Object who, string message)
        {
            if (instance == null || instance.text == null) return;
            instance.owner = who;
            instance.text.transform.parent.gameObject.SetActive(true);
            GlyphText.Set(instance.text, message);
        }

        public static void Hide(Object who)
        {
            if (instance == null || (instance.owner != null && instance.owner != who)) return;
            instance.owner = null;
            GlyphText.Set(instance.text, "");
            instance.text.transform.parent.gameObject.SetActive(false);
        }

        /// <summary>The interact button this frame: E on the keyboard, Y / triangle on a gamepad.</summary>
        public static bool Pressed()
        {
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            return (keyboard != null && keyboard.eKey.wasPressedThisFrame) || (pad != null && pad.buttonNorth.wasPressedThisFrame);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;
    }
}
