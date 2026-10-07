using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    public float gravity = -20f;
    public float jumpHeight = 1.5f;

    [Header("Look")]
    public float mouseSensitivity = 150f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("Full Body First Person")]
    [Tooltip("Main gameplay camera. If empty, the first child Camera is used.")]
    public Camera playerCamera;

    [Tooltip("Humanoid Animator. Used to find Head / LeftHand / RightHand automatically.")]
    public Animator animator;

    [Tooltip("Optional manual head bone. For Humanoid rigs it is found automatically.")]
    public Transform headBone;

    [Tooltip("Camera position relative to the head. Small forward offset helps avoid face clipping.")]
    public Vector3 cameraOffset = new Vector3(0f, 0.03f, 0.09f);

    [Tooltip("Objects that should be hidden only from the local player, e.g. separate Head/Hair renderers. Do NOT add the full body renderer here.")]
    public GameObject[] localOnlyHiddenObjects;

    [Header("Weapon sockets")]
    public Transform rightHandSocket;
    public Transform leftHandSocket;

    [Header("Camera")]
    [Range(0.01f, 0.3f)]
    public float nearClipPlane = 0.03f;

    private CharacterController controller;
    private Transform cameraTransform;
    private Vector3 velocity;
    private float xRotation;

    public Transform RightHandSocket => rightHandSocket;
    public Transform LeftHandSocket => leftHandSocket;
    public bool IsGrounded => controller != null && controller.isGrounded;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (playerCamera != null)
        {
            cameraTransform = playerCamera.transform;
            playerCamera.nearClipPlane = nearClipPlane;
        }

        AutoFindHumanoidBones();
        HideLocalHeadParts();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleMovement();
        HandleLook();
    }

    private void LateUpdate()
    {
        UpdateFullBodyCamera();
    }

    private void HandleMovement()
    {
        bool grounded = controller.isGrounded;

        if (grounded && velocity.y < 0f)
            velocity.y = -2f;

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        move = Vector3.ClampMagnitude(move, 1f);

        if (Input.GetButtonDown("Jump") && grounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        velocity.y += gravity * Time.deltaTime;

        Vector3 finalMove = move * speed + Vector3.up * velocity.y;
        controller.Move(finalMove * Time.deltaTime);
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minPitch, maxPitch);
    }

    private void UpdateFullBodyCamera()
    {
        if (cameraTransform == null)
            return;

        if (headBone != null)
        {
            cameraTransform.position =
                headBone.position +
                transform.right * cameraOffset.x +
                transform.up * cameraOffset.y +
                transform.forward * cameraOffset.z;
        }

        cameraTransform.rotation = Quaternion.Euler(
            xRotation,
            transform.eulerAngles.y,
            0f
        );
    }

    private void AutoFindHumanoidBones()
    {
        if (animator == null || !animator.isHuman)
            return;

        if (headBone == null)
            headBone = animator.GetBoneTransform(HumanBodyBones.Head);

        if (rightHandSocket == null)
            rightHandSocket = animator.GetBoneTransform(HumanBodyBones.RightHand);

        if (leftHandSocket == null)
            leftHandSocket = animator.GetBoneTransform(HumanBodyBones.LeftHand);
    }

    private void HideLocalHeadParts()
    {
        if (localOnlyHiddenObjects == null)
            return;

        foreach (GameObject target in localOnlyHiddenObjects)
        {
            if (target != null)
                target.SetActive(false);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (playerCamera != null)
            playerCamera.nearClipPlane = nearClipPlane;
    }
#endif
}
