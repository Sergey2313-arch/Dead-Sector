using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float gravity = -20f;
    public float jumpHeight = 1.5f;
    public float mouseSensitivity = 150f;

    private CharacterController controller;
    private Transform cameraTransform;

    private Vector3 velocity;
    private float xRotation = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        Camera playerCamera = GetComponentInChildren<Camera>();

        if (playerCamera != null)
        {
            cameraTransform = playerCamera.transform;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        bool grounded = controller.isGrounded;

        if (grounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 move =
            transform.right * x +
            transform.forward * z;

        move = Vector3.ClampMagnitude(move, 1f);

        if (Input.GetButtonDown("Jump") && grounded)
        {
            velocity.y =
                Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;

        Vector3 finalMove =
            move * speed +
            Vector3.up * velocity.y;

        controller.Move(finalMove * Time.deltaTime);

        float mouseX =
            Input.GetAxis("Mouse X") *
            mouseSensitivity *
            Time.deltaTime;

        float mouseY =
            Input.GetAxis("Mouse Y") *
            mouseSensitivity *
            Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);

        if (cameraTransform != null)
        {
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -80f, 80f);

            cameraTransform.localRotation =
                Quaternion.Euler(xRotation, 0f, 0f);
        }
    }
}