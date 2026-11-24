using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TD_Game
{
    public enum InputScheme { Keyboard, Xbox, PlayStation }

    /// <summary>
    /// Watches which device the player used LAST: keyboard / mouse, an Xbox-style controller or a PlayStation one
    /// (DualShock / DualSense, or any controller made by Sony). The button icons of the whole UI follow it in real
    /// time through InputPrompts. A tiny hidden object polls the devices every frame; nothing has to be in the scene.
    /// </summary>
    public static class InputDevices
    {
        public static InputScheme Detected { get; private set; } = InputScheme.Keyboard;

        /// <summary>Raised when the player switches device.</summary>
        public static event Action<InputScheme> Changed;

        const float StickDeadZone = 0.5f;
        const float MouseMovePixels = 6f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Changed = null;
            Detected = InputScheme.Keyboard;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void CreateWatcher()
        {
            var go = new GameObject("[InputDevices]") { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Watcher>();
        }

        class Watcher : MonoBehaviour
        {
            void Update() => Poll();
        }

        static void Poll()
        {
            foreach (var pad in Gamepad.all)
                if (WasUsed(pad)) { Set(SchemeOf(pad)); return; }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame) { Set(InputScheme.Keyboard); return; }
            var mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame
                                  || mouse.delta.ReadValue().magnitude > MouseMovePixels))
                Set(InputScheme.Keyboard);
        }

        /// <summary>A button pressed this frame, or a stick / trigger clearly moved (drift does not count).</summary>
        static bool WasUsed(Gamepad pad)
        {
            if (!pad.wasUpdatedThisFrame) return false;
            if (pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame
                || pad.buttonNorth.wasPressedThisFrame || pad.leftShoulder.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame
                || pad.startButton.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame
                || pad.leftStickButton.wasPressedThisFrame || pad.rightStickButton.wasPressedThisFrame
                || pad.dpad.up.wasPressedThisFrame || pad.dpad.down.wasPressedThisFrame
                || pad.dpad.left.wasPressedThisFrame || pad.dpad.right.wasPressedThisFrame)
                return true;
            return pad.leftStick.ReadValue().magnitude > StickDeadZone || pad.rightStick.ReadValue().magnitude > StickDeadZone
                || pad.leftTrigger.ReadValue() > StickDeadZone || pad.rightTrigger.ReadValue() > StickDeadZone;
        }

        /// <summary>PlayStation if it is a DualShock / DualSense, or its description says Sony / PlayStation.</summary>
        public static InputScheme SchemeOf(Gamepad pad)
        {
            if (pad is UnityEngine.InputSystem.DualShock.DualShockGamepad) return InputScheme.PlayStation;
            var d = pad.description;
            string text = (d.manufacturer + " " + d.product + " " + pad.layout).ToLowerInvariant();
            return text.Contains("sony") || text.Contains("playstation") || text.Contains("dualsense") || text.Contains("dualshock")
                ? InputScheme.PlayStation : InputScheme.Xbox;
        }

        static void Set(InputScheme scheme)
        {
            if (scheme == Detected) return;
            Detected = scheme;
            Changed?.Invoke(scheme);
        }
    }
}
