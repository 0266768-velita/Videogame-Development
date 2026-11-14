using UnityEngine;
using UnityEngine.InputSystem;

namespace TD_Game
{
    /// <summary>
    /// Reads the "Combat" action map of PlayerControls.inputactions. It is only enabled during battles.
    /// Keyboard / Gamepad: Attack 1/A · Skill1 2/X · Skill2 3/Y · Potion 4/D-pad up · Ultimate 5/LB · EndTurn Q/RB
    /// · TargetPrev/Next left/right arrows or D-pad · Dodge Space,D/B · Interrupt 1/A.
    /// </summary>
    public class CombatInput : MonoBehaviour
    {
        const string MapName = "Combat";

        [Tooltip("PlayerControls.inputactions")]
        [SerializeField] InputActionAsset actions;

        InputActionMap map;

        public InputAction Attack { get; private set; }
        public InputAction Skill1 { get; private set; }
        public InputAction Skill2 { get; private set; }
        public InputAction Potion { get; private set; }
        public InputAction Ultimate { get; private set; }
        public InputAction EndTurn { get; private set; }
        public InputAction TargetPrev { get; private set; }
        public InputAction TargetNext { get; private set; }
        public InputAction Dodge { get; private set; }
        public InputAction Interrupt { get; private set; }

        void Awake()
        {
            if (actions == null)
            {
                Debug.LogError($"[{nameof(CombatInput)}] The Input Actions asset is not assigned.", this);
                enabled = false;
                return;
            }
            map = actions.FindActionMap(MapName, throwIfNotFound: true);
            Attack = map.FindAction("Attack", true);
            Skill1 = map.FindAction("Skill1", true);
            Skill2 = map.FindAction("Skill2", true);
            Potion = map.FindAction("Potion", true);
            Ultimate = map.FindAction("Ultimate", true);
            EndTurn = map.FindAction("EndTurn", true);
            TargetPrev = map.FindAction("TargetPrev", true);
            TargetNext = map.FindAction("TargetNext", true);
            Dodge = map.FindAction("Dodge", true);
            Interrupt = map.FindAction("Interrupt", true);
        }

        public void Enable() => map?.Enable();
        public void Disable() => map?.Disable();

        void OnDisable() => Disable();
    }
}
