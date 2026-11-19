using UnityEngine;
using UnityEngine.InputSystem;

namespace TD_Game
{
    /// <summary>
    /// Choose which enemy to attack when you fight several: click on it with the mouse
    /// (or use the arrows / TAB, which also change the target). The red ▼ marker shows the target.
    /// </summary>
    public class ClickToTarget : MonoBehaviour
    {
        [SerializeField] Camera worldCamera;

        void Update()
        {
            var combat = CombatManager.Instance;
            if (combat == null || !combat.InCombat || !combat.PlayerTurn.AcceptingInput) return;

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
            {
                combat.Context.CycleTarget(1);
                GameAudio.PlaySfx("ui_move", 0.7f, 0f);
            }

            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            if (worldCamera == null) worldCamera = Camera.main;
            Vector2 world = worldCamera.ScreenToWorldPoint(mouse.position.ReadValue());

            // The enemy whose body (collider) or picture is under the mouse.
            foreach (var enemy in combat.Context.Enemies)
            {
                if (enemy == null || enemy.Health.IsDead) continue;
                bool hit = enemy.Sprite != null && enemy.Sprite.bounds.Contains(new Vector3(world.x, world.y, enemy.Sprite.bounds.center.z));
                if (hit && combat.Context.SelectTarget(enemy))
                {
                    GameAudio.PlaySfx("ui_select", 0.8f, 0f);
                    return;
                }
            }
        }
    }
}
