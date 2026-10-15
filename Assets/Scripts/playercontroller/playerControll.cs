using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControll : MonoBehaviour
{
    public float speed = 3f;
    public float sprintSpeed = 4f;

    public float jumpHeight = 3f;
    public float gravity = -9.81f;

    private Vector2 move;
    private CharacterController controller;

    private float verticalVelocity;
    private bool sprinting;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    // Movement input
    public void OnMove(InputValue value)
    {
        move = value.Get<Vector2>();

        Debug.Log("MOVE: " + move);
    }

    // Sprint input
    public void OnSprint(InputValue value)
    {
        sprinting = value.isPressed;
    }

    // Jump input
    public void OnJump()
    {
        if (controller.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -1f * gravity);
        }
    }

    void Update()
    {
        // Movement
        Vector3 movement = new Vector3(move.x, 0, move.y);

        // Choose walking or sprinting speed
        float currentSpeed = sprinting ? sprintSpeed : speed;

        controller.Move(movement * currentSpeed * Time.deltaTime);

        // Gravity
        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        // Vertical movement
        Vector3 verticalMovement = new Vector3(0, verticalVelocity, 0);

        controller.Move(verticalMovement * Time.deltaTime);
    }
}