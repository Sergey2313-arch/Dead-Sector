using System.Collections.Generic;
using UnityEngine;

namespace DeadSector.Runtime.Player
{
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3.2f;
    public float runSpeed = 5.8f;
    public float gravity = -20f;
    public float jumpHeight = 1.5f;
    public float hardLandingVelocity = -7.5f;
    public float rollLandingVelocity = -11.5f;

    [Header("Look")]
    public float mouseSensitivity = 150f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("Full Body First Person")]
    [Tooltip("Main gameplay camera. If empty, the first child Camera is used.")]
    public Camera playerCamera;

    [Tooltip("Animator used for the player body.")]
    public Animator animator;

    [Tooltip("Optional manual head bone. For Humanoid rigs it is found automatically.")]
    public Transform headBone;

    [Tooltip("Camera position relative to the player root axes and head position.")]
    public Vector3 cameraOffset = new Vector3(0f, 0.03f, 0.09f);

    [Tooltip("Objects hidden only for local first person. Do not add the whole body here.")]
    public GameObject[] localOnlyHiddenObjects;

    [Header("Weapon sockets")]
    public Transform rightHandSocket;
    public Transform leftHandSocket;

    [Header("Prototype melee")]
    public bool enablePrototypeMelee = true;
    public KeyCode combatToggleKey = KeyCode.F;

    [Header("Camera")]
    [Range(0.01f, 0.3f)]
    public float nearClipPlane = 0.03f;

    private CharacterController controller;
    private Transform cameraTransform;
    private Vector3 velocity;
    private float xRotation;
    private float currentMoveAmount;
    private bool running;
    private bool combatMode;
    private bool previousGrounded = true;

    private readonly HashSet<int> animatorParameters = new();

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

        CacheAnimatorParameters();
        AutoFindHumanoidBones();
        HideLocalHeadParts();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        previousGrounded = controller.isGrounded;
    }

    private void Update()
    {
        HandleMovement();
        HandleLook();
        UpdateAnimator();
        HandlePrototypeMelee();
    }

    private void LateUpdate()
    {
        UpdateFullBodyCamera();
    }

    private void HandleMovement()
    {
        bool groundedBeforeMove = controller.isGrounded;

        if (groundedBeforeMove && velocity.y < 0f)
            velocity.y = -2f;

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        move = Vector3.ClampMagnitude(move, 1f);

        running = Input.GetKey(KeyCode.LeftShift) && move.sqrMagnitude > 0.01f;
        float moveSpeed = running ? runSpeed : walkSpeed;

        currentMoveAmount = move.magnitude;

        if (Input.GetButtonDown("Jump") && groundedBeforeMove)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            SetIntIfExists("LandingType", 0);
        }

        velocity.y += gravity * Time.deltaTime;
        float impactVerticalVelocity = velocity.y;

        Vector3 finalMove = move * moveSpeed + Vector3.up * velocity.y;
        controller.Move(finalMove * Time.deltaTime);

        bool groundedAfterMove = controller.isGrounded;

        if (!groundedAfterMove && previousGrounded)
            SetIntIfExists("LandingType", 0);

        if (groundedAfterMove && !previousGrounded)
        {
            int landingType = 0;

            if (impactVerticalVelocity <= rollLandingVelocity)
                landingType = 2;
            else if (impactVerticalVelocity <= hardLandingVelocity)
                landingType = 1;

            SetIntIfExists("LandingType", landingType);
        }

        if (groundedAfterMove && velocity.y < 0f)
            velocity.y = -2f;

        previousGrounded = groundedAfterMove;
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minPitch, maxPitch);
    }

    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        SetFloatIfExists("Speed", currentMoveAmount, 0.1f);
        SetBoolIfExists("Grounded", controller.isGrounded);
        SetBoolIfExists("IsRunning", running);
        SetFloatIfExists("VerticalSpeed", velocity.y);
        SetBoolIfExists("CombatMode", combatMode);
    }

    private void HandlePrototypeMelee()
    {
        if (!enablePrototypeMelee || animator == null)
            return;

        if (Input.GetKeyDown(combatToggleKey))
            combatMode = !combatMode;

        if (Input.GetMouseButtonDown(0))
            SetTriggerIfExists("Punch");

        SetBoolIfExists("Block", Input.GetMouseButton(1));
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

    private void CacheAnimatorParameters()
    {
        animatorParameters.Clear();

        if (animator == null)
            return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
            animatorParameters.Add(parameter.nameHash);
    }

    private bool HasAnimatorParameter(string parameterName)
    {
        return animator != null && animatorParameters.Contains(Animator.StringToHash(parameterName));
    }

    private void SetBoolIfExists(string parameterName, bool value)
    {
        if (HasAnimatorParameter(parameterName))
            animator.SetBool(parameterName, value);
    }

    private void SetFloatIfExists(string parameterName, float value, float dampTime = 0f)
    {
        if (!HasAnimatorParameter(parameterName))
            return;

        if (dampTime > 0f)
            animator.SetFloat(parameterName, value, dampTime, Time.deltaTime);
        else
            animator.SetFloat(parameterName, value);
    }

    private void SetIntIfExists(string parameterName, int value)
    {
        if (HasAnimatorParameter(parameterName))
            animator.SetInteger(parameterName, value);
    }

    private void SetTriggerIfExists(string parameterName)
    {
        if (HasAnimatorParameter(parameterName))
            animator.SetTrigger(parameterName);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (playerCamera != null)
            playerCamera.nearClipPlane = nearClipPlane;
    }
#endif
}

}
