using UnityEngine;
using UnityEngine.InputSystem;

namespace TD_Game
{
    /// <summary>
    /// Reads the player's Input Actions and exposes their values. It contains no gameplay logic:
    /// other components (movement, animation...) read from here.
    /// Named "Reader" to avoid clashing with Unity's own PlayerInput component.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        [Tooltip("Player/Move action from PlayerControls.inputactions (Vector2).")]
        [SerializeField] InputActionReference moveAction;

        /// <summary>Raw movement input, each axis in [-1, 1]. Keyboard gives -1/0/1, a stick gives analog values.</summary>
        public Vector2 Move { get; private set; }

        void OnEnable()
        {
            if (moveAction == null)
            {
                Debug.LogError($"[{nameof(PlayerInputReader)}] Move action is not assigned.", this);
                enabled = false;
                return;
            }
            moveAction.action.Enable();
        }

        void OnDisable()
        {
            if (moveAction != null) moveAction.action.Disable();
            Move = Vector2.zero;
        }

        void Update()
        {
            Move = moveAction.action.ReadValue<Vector2>();
        }
    }
}
