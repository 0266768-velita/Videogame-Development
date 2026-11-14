using UnityEngine;
using UnityEngine.InputSystem;

namespace TD_Game
{
    /// <summary>
    /// Reads the player's Input Actions and exposes their values. It contains no gameplay logic:
    /// other components (movement, jump, animation...) read from here.
    /// Named "Reader" to avoid clashing with Unity's own PlayerInput component.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        [Tooltip("Player/Move action from PlayerControls.inputactions (Vector2).")]
        [SerializeField] InputActionReference moveAction;

        [Tooltip("Player/Jump action from PlayerControls.inputactions (Button). Optional.")]
        [SerializeField] InputActionReference jumpAction;

        [Tooltip("Player/Sprint action from PlayerControls.inputactions (Button, hold). Optional.")]
        [SerializeField] InputActionReference sprintAction;

        /// <summary>Raw movement input, each axis in [-1, 1]. Keyboard gives -1/0/1, a stick gives analog values.</summary>
        public Vector2 Move { get; private set; }

        /// <summary>True only on the frame the jump button is pressed.</summary>
        public bool JumpPressed => jumpAction != null && jumpAction.action.WasPressedThisFrame();

        /// <summary>True while the sprint button is held.</summary>
        public bool SprintHeld => sprintAction != null && sprintAction.action.IsPressed();

        void OnEnable()
        {
            if (moveAction == null)
            {
                Debug.LogError($"[{nameof(PlayerInputReader)}] Move action is not assigned.", this);
                enabled = false;
                return;
            }
            moveAction.action.Enable();
            if (jumpAction != null) jumpAction.action.Enable();
            if (sprintAction != null) sprintAction.action.Enable();
        }

        void OnDisable()
        {
            if (moveAction != null) moveAction.action.Disable();
            if (jumpAction != null) jumpAction.action.Disable();
            if (sprintAction != null) sprintAction.action.Disable();
            Move = Vector2.zero;
        }

        void Update()
        {
            Move = moveAction.action.ReadValue<Vector2>();
        }
    }
}
