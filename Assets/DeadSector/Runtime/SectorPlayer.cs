using UnityEngine;

namespace DeadSector
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class SectorPlayer : MonoBehaviour
    {
        [Header("Movement")]
        public float walkSpeed = 4f;
        public float runSpeed = 7f;
        public float jumpHeight = 1.3f;
        public float gravity = 20f;
        public float hardLandingVelocity = -7.5f;
        public float rollLandingVelocity = -11.5f;

        [Header("View")]
        public Camera view;
        public Transform visual;
        public bool thirdPerson = true;
        public float thirdPersonDistance = 3.35f;
        public float thirdPersonShoulder = .58f;
        public float cameraHeight = 1.62f;
        public float cameraCollisionRadius = .18f;
        public float cameraSmooth = 18f;
        public float thirdPersonFov = 68f;
        public float firstPersonFov = 75f;

        public bool Ready { get; set; }
        public float Health { get; private set; } = 100f;
        public float Speed { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsGrounded => body != null && body.isGrounded;
        public string AnimationState { get; private set; } = "None";

        CharacterController body;
        SectorMannequin rig;
        Animator animator;
        Transform headBone;
        float pitch = 12f;
        float vertical = -2f;
        float groundedAt = -10f;
        float jumpAt = -10f;
        bool previousGrounded = true;
        Renderer[] renderers;
        Vector3 smoothedCameraPosition;
        bool cameraInitialized;

        static readonly int SpeedHash = Animator.StringToHash("Speed");
        static readonly int GroundedHash = Animator.StringToHash("Grounded");
        static readonly int RunningHash = Animator.StringToHash("IsRunning");
        static readonly int VerticalHash = Animator.StringToHash("VerticalSpeed");
        static readonly int LandingHash = Animator.StringToHash("LandingType");

        void Awake()
        {
            body = GetComponent<CharacterController>();
            BindVisual(visual);
            SetCursor(true);
        }

        /// <summary>
        /// Bootstrap injects the selected model after AddComponent. Rebind
        /// here as well so an inactive prefab and a runtime fallback both work.
        /// </summary>
        public void BindVisual(Transform bodyVisual)
        {
            visual = bodyVisual;
            rig = visual != null ? visual.GetComponent<SectorMannequin>() : null;
            animator = visual != null
                ? visual.GetComponentInChildren<Animator>(true)
                : null;
            renderers = visual != null
                ? visual.GetComponentsInChildren<Renderer>(true)
                : new Renderer[0];

            headBone = animator != null && animator.isHuman
                ? animator.GetBoneTransform(HumanBodyBones.Head)
                : null;

            PrimeAnimation();
        }

        void Update()
        {
            if (SectorInput.Pressed(KeyCode.Escape))
                SetCursor(false);

            if (SectorInput.Click && Health > 0)
                SetCursor(true);

            if (SectorInput.Pressed(KeyCode.R))
                Respawn();

            if (!Ready || Health <= 0 || Cursor.lockState != CursorLockMode.Locked)
            {
                Speed = 0;
                IsSprinting = false;
                SetMotion(0, true, -2f, false);
                UpdateAnimationStateName();
                return;
            }

            if (SectorInput.Pressed(KeyCode.V))
            {
                thirdPerson = !thirdPerson;
                cameraInitialized = false;
            }

            Vector2 look = SectorInput.Look;
            transform.Rotate(0, look.x, 0);
            pitch = Mathf.Clamp(pitch - look.y, -75f, 75f);

            bool groundedBeforeMove = body.isGrounded;

            if (groundedBeforeMove)
            {
                groundedAt = Time.time;
                if (vertical < 0)
                    vertical = -2f;
            }

            if (SectorInput.Pressed(KeyCode.Space))
                jumpAt = Time.time;

            SectorSurvival needs = GetComponent<SectorSurvival>();
            if (Time.time - groundedAt < .12f && Time.time - jumpAt < .12f &&
                (needs == null || needs.CanJump))
            {
                needs?.ConsumeJumpStamina();
                vertical = Mathf.Sqrt(jumpHeight * 2f * gravity);
                groundedAt = jumpAt = -10f;
            }

            Vector2 input = SectorInput.Move;
            Vector3 move = transform.right * input.x + transform.forward * input.y;
            bool sprinting = SectorInput.Sprint &&
                input.sqrMagnitude > .01f &&
                (needs == null || needs.CanSprint);
            IsSprinting = sprinting;

            vertical = Mathf.Max(vertical - gravity * Time.deltaTime, -45f);
            float impactVelocity = vertical;
            float currentSpeed = sprinting ? runSpeed : walkSpeed;

            CollisionFlags flags = body.Move(
                (move * currentSpeed + Vector3.up * vertical) * Time.deltaTime);

            if ((flags & CollisionFlags.Above) != 0 && vertical > 0)
                vertical = 0;

            bool groundedAfterMove =
                body.isGrounded || (flags & CollisionFlags.Below) != 0;

            if (groundedAfterMove)
            {
                groundedAt = Time.time;
                if (vertical < 0)
                    vertical = -2f;
            }

            if (groundedAfterMove && !previousGrounded)
                SetLandingType(impactVelocity);

            previousGrounded = groundedAfterMove;

            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x, -3990, 3990);
            p.z = Mathf.Clamp(p.z, -3990, 3990);

            if (p != transform.position)
            {
                body.enabled = false;
                transform.position = p;
                body.enabled = true;
            }

            if (p.y < -30f)
            {
                Respawn();
                return;
            }

            Vector3 planarVelocity = body.velocity;
            planarVelocity.y = 0;
            Speed = planarVelocity.magnitude;

            SetMotion(Speed, groundedAfterMove, vertical, sprinting);
            UpdateAnimationStateName();
        }

        void LateUpdate()
        {
            if (view == null)
                return;

            Quaternion rotation = Quaternion.Euler(
                pitch,
                transform.eulerAngles.y,
                0);

            Vector3 pivot =
                transform.position + Vector3.up * cameraHeight;

            if (!thirdPerson && headBone != null)
            {
                pivot =
                    headBone.position +
                    transform.forward * .11f +
                    transform.up * .025f;
            }

            Vector3 desiredPosition = pivot;

            if (thirdPerson)
            {
                Vector3 desiredOffset =
                    rotation * new Vector3(
                        thirdPersonShoulder,
                        .08f,
                        -thirdPersonDistance);

                float length = desiredOffset.magnitude;

                if (length > .001f &&
                    Physics.SphereCast(
                        pivot,
                        cameraCollisionRadius,
                        desiredOffset / length,
                        out RaycastHit hit,
                        length,
                        ~(1 << 2),
                        QueryTriggerInteraction.Ignore))
                {
                    desiredOffset =
                        desiredOffset.normalized *
                        Mathf.Max(.12f, hit.distance - .10f);
                }

                desiredPosition = pivot + desiredOffset;
            }

            if (!cameraInitialized)
            {
                smoothedCameraPosition = desiredPosition;
                cameraInitialized = true;
            }
            else
            {
                float blend =
                    1f - Mathf.Exp(-cameraSmooth * Time.deltaTime);

                smoothedCameraPosition =
                    Vector3.Lerp(
                        smoothedCameraPosition,
                        desiredPosition,
                        blend);
            }

            view.transform.SetPositionAndRotation(
                smoothedCameraPosition,
                rotation);

            float targetFov =
                thirdPerson ? thirdPersonFov : firstPersonFov;

            view.fieldOfView =
                Mathf.Lerp(
                    view.fieldOfView,
                    targetFov,
                    1f - Mathf.Exp(-10f * Time.deltaTime));

            foreach (Renderer renderer in renderers)
            {
                if (renderer != null && !renderer.enabled)
                    renderer.enabled = true;
            }
        }

        public void ActivateAtSpawn()
        {
            if (body == null)
                body = GetComponent<CharacterController>();

            body.enabled = false;
            transform.position = SectorLayout.Spawn;
            body.enabled = true;

            Physics.SyncTransforms();

            vertical = -2f;
            groundedAt = Time.time;
            jumpAt = -10f;
            previousGrounded = true;
            Speed = 0;
            IsSprinting = false;
            Health = 100f;
            cameraInitialized = false;

            PrimeAnimation();
            Ready = true;
        }

        void PrimeAnimation()
        {
            if (animator == null)
                return;

            // Rebind restores the X Bot bind pose before the controller starts.
            // This prevents a previous landing/fall pose from surviving a
            // prefab rebuild or respawn.
            animator.Rebind();
            animator.Update(0f);

            animator.SetFloat(SpeedHash, 0f);
            animator.SetBool(GroundedHash, true);
            animator.SetBool(RunningHash, false);
            animator.SetFloat(VerticalHash, -2f);
            animator.SetInteger(LandingHash, 0);
            animator.Update(0f);

            AnimationState = "Idle";
        }

        void SetMotion(
            float speed,
            bool grounded,
            float verticalSpeed,
            bool sprinting)
        {
            if (rig != null)
                rig.SetMotion(speed, grounded, verticalSpeed);

            if (animator == null)
                return;

            animator.SetFloat(SpeedHash, speed);
            animator.SetBool(GroundedHash, grounded);
            animator.SetBool(RunningHash, sprinting && speed > .1f);
            animator.SetFloat(VerticalHash, verticalSpeed);
        }

        void SetLandingType(float impactVelocity)
        {
            if (animator == null)
                return;

            int landingType = 0;

            if (impactVelocity <= rollLandingVelocity)
                landingType = 2;
            else if (impactVelocity <= hardLandingVelocity)
                landingType = 1;

            animator.SetInteger(LandingHash, landingType);
        }

        void UpdateAnimationStateName()
        {
            if (animator == null || !animator.isActiveAndEnabled)
            {
                AnimationState = rig != null ? "Procedural" : "None";
                return;
            }

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            AnimationState = StateName(state);
        }

        static string StateName(AnimatorStateInfo state)
        {
            if (state.IsName("Idle")) return "Idle";
            if (state.IsName("Walk")) return "Walk";
            if (state.IsName("Run")) return "Run";
            if (state.IsName("Jump")) return "Jump";
            if (state.IsName("Running Jump")) return "Running Jump";
            if (state.IsName("Falling")) return "Falling";
            if (state.IsName("Landing")) return "Landing";
            if (state.IsName("Hard Landing")) return "Hard Landing";
            if (state.IsName("Landing Roll")) return "Landing Roll";
            return "Other";
        }

        public void Heal(float amount)
        {
            if (amount > 0f && Health > 0f)
                Health = Mathf.Clamp(Health + amount, 0f, 100f);
        }

        public void RestoreHealth(float savedHealth)
        {
            Health = Mathf.Clamp(savedHealth, 0f, 100f);
        }

        public void TeleportTo(Vector3 position)
        {
            if (body == null)
                body = GetComponent<CharacterController>();

            body.enabled = false;
            transform.position = position;
            body.enabled = true;
            vertical = -2f;
            cameraInitialized = false;
            Physics.SyncTransforms();
        }

        public void Damage(float amount)
        {
            if (amount <= 0f) return;
            Health = Mathf.Max(0, Health - amount);

            if (Health <= 0)
                SetCursor(false);
        }

        public void Respawn()
        {
            if (!Ready)
                return;

            ActivateAtSpawn();
            SetCursor(true);
        }

        void OnDisable()
        {
            if (Application.isPlaying)
                SetCursor(false);
        }

        static void SetCursor(bool locked)
        {
            Cursor.lockState =
                locked ? CursorLockMode.Locked : CursorLockMode.None;

            Cursor.visible = !locked;
        }
    }
}
