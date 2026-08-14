using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float mouseSensitivity = 0.1f;

    private CharacterController controller;
    private Transform cameraTransform;

    private float verticalVelocity;
    private float cameraPitch;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        cameraTransform = GetComponentInChildren<Camera>().transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        Move();
        Look();
    }

    private void Move()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            input = new Vector2(
                (Keyboard.current.dKey.isPressed ? 1f : 0f) -
                (Keyboard.current.aKey.isPressed ? 1f : 0f),

                (Keyboard.current.wKey.isPressed ? 1f : 0f) -
                (Keyboard.current.sKey.isPressed ? 1f : 0f)
            );
        }

        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 movement =
            transform.right * input.x +
            transform.forward * input.y;

        movement *= moveSpeed;

        // Gravity
        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += Physics.gravity.y * Time.deltaTime;

        movement.y = verticalVelocity;

        controller.Move(movement * Time.deltaTime);
    }

    private void Look()
    {
        if (Mouse.current == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;

        // Rotate player horizontally
        transform.Rotate(Vector3.up * mouseX);

        // Rotate camera vertically
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f);

        cameraTransform.localRotation =
            Quaternion.Euler(cameraPitch, 0f, 0f);
    }
}