using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Combat inputs (follows the GDD section 5 controls table), created in code so no
/// .inputactions asset is needed. Keyboard / Gamepad.
/// </summary>
public static class BattleInput
{
    public static InputAction Attack;     // 1  / A
    public static InputAction Fury;       // 2  / X
    public static InputAction Seismic;    // 3  / Y
    public static InputAction Potion;     // 4  / D-pad up
    public static InputAction EndTurn;    // Q  / RB
    public static InputAction Dodge;      // D  / B
    public static InputAction TargetPrev; // Left arrow  / D-pad left
    public static InputAction TargetNext; // Right arrow / D-pad right

    static InputAction[] all;

    static InputAction Make(string name, params string[] paths)
    {
        var a = new InputAction(name, InputActionType.Button);
        foreach (var p in paths) a.AddBinding(p);
        return a;
    }

    public static void Enable()
    {
        if (all == null)
        {
            Attack = Make("Attack", "<Keyboard>/1", "<Gamepad>/buttonSouth");
            Fury = Make("Fury", "<Keyboard>/2", "<Gamepad>/buttonWest");
            Seismic = Make("Seismic", "<Keyboard>/3", "<Gamepad>/buttonNorth");
            Potion = Make("Potion", "<Keyboard>/4", "<Gamepad>/dpad/up");
            EndTurn = Make("EndTurn", "<Keyboard>/q", "<Gamepad>/rightShoulder");
            Dodge = Make("Dodge", "<Keyboard>/d", "<Gamepad>/buttonEast");
            TargetPrev = Make("TargetPrev", "<Keyboard>/leftArrow", "<Gamepad>/dpad/left");
            TargetNext = Make("TargetNext", "<Keyboard>/rightArrow", "<Gamepad>/dpad/right");
            all = new[] { Attack, Fury, Seismic, Potion, EndTurn, Dodge, TargetPrev, TargetNext };
        }
        foreach (var a in all) a.Enable();
    }

    public static void Disable()
    {
        if (all == null) return;
        foreach (var a in all) a.Disable();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { all = null; }
}
