using UnityEngine;

namespace DeadSector
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class SectorPlayer : MonoBehaviour
    {
        // Real-world metres. Shared by fallback CharacterController creation
        // and procedural mannequin sizing; imported humanoid FBX assets stay intact.
        public const float StandingHeight = 1.78f;
        public const float StandingRadius = .28f;
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
        public float thirdPersonDistance = 2.65f;
        public float thirdPersonShoulder = .40f;
        public float cameraHeight = 1.62f;
        public float cameraCollisionRadius = .18f;
        public float cameraSmooth = 18f;
        [Header("Look Smoothing")]
        [Tooltip("Exponential turn damping. Higher values react faster; 0 disables smoothing.")]
        [Min(0f)] public float lookSmooth = 24f;
        public float thirdPersonFov = 68f;
        public float firstPersonFov = 75f;

        public bool Ready { get; set; }
        public bool InputBlockedByUI { get; set; }
        // Modern main/pause menu owns Escape and pointer lock.
        // Remains false in the legacy IMGUI fallback.
        public bool EscapeHandledByUi { get; set; }
        public float Health { get; private set; } = 100f;
        public float Speed { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsGrounded => body != null && body.isGrounded;
        public float TerrainUnderPlayer => GroundHeightAt(transform.position);
        public string AnimationState { get; private set; } = "None";

        CharacterController body;
        SectorSurvival cachedSurvival;
        Terrain cachedGroundTerrain;
        SectorMannequin rig;
        Animator animator;
        Transform headBone;
        float pitch = 12f;
        float targetPitch = 12f;
        float targetYaw;
        bool lookInitialized;
        float vertical = -2f;
        float groundedAt = -10f;
        float jumpAt = -10f;
        bool previousGrounded = true;
        Renderer[] renderers;
        bool? visibleInThirdPerson;
        bool visualCalibrated;
        Vector3 calibratedVisualLocalPosition;
        SkinnedMeshRenderer[] calibratedSkins;
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
            cachedSurvival = GetComponent<SectorSurvival>();
            BindVisual(visual);
            ResetLookSmoothing();
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

            visibleInThirdPerson = null;
            visualCalibrated = false;
            calibratedSkins = null;

            if (animator != null && animator.isActiveAndEnabled)
                PrimeAnimation();
        }

        void Update()
        {
            if (!EscapeHandledByUi &&
                SectorInput.Pressed(KeyCode.Escape))
                SetCursor(false);

            if (SectorInput.Click && Health > 0 && !InputBlockedByUI)
                SetCursor(true);

            // R is reserved for weapon reload while alive.
            // Respawn is allowed only after actual death, never as a
            // free heal during combat.
            if (Health <= 0f && !InputBlockedByUI &&
                SectorInput.Pressed(KeyCode.R))
                Respawn();

            if (!Ready || Health <= 0 || InputBlockedByUI ||
                Cursor.lockState != CursorLockMode.Locked)
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
            if (!lookInitialized)
                ResetLookSmoothing();

            targetYaw += look.x;
            targetPitch = Mathf.Clamp(targetPitch - look.y, -75f, 75f);

            // A single damping model drives both the character heading and
            // the view direction, instead of abruptly rotating the body while
            // the third-person camera position lags behind it.
            float blend = TurnBlend(lookSmooth, Time.deltaTime);
            float yaw = Mathf.LerpAngle(transform.eulerAngles.y, targetYaw, blend);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            pitch = Mathf.Lerp(pitch, targetPitch, blend);

            bool groundedBeforeMove = body.isGrounded;

            if (groundedBeforeMove)
            {
                groundedAt = Time.time;
                if (vertical < 0)
                    vertical = -2f;
            }

            if (SectorInput.Pressed(KeyCode.Space))
                jumpAt = Time.time;

            // SectorSurvival may be added by bootstrap after SectorPlayer.Awake.
            // Once found, reuse it instead of GetComponent every frame.
            if (cachedSurvival == null)
                cachedSurvival = GetComponent<SectorSurvival>();
            SectorSurvival needs = cachedSurvival;
            if (Time.time - groundedAt < .12f && Time.time - jumpAt < .12f &&
                (needs == null || needs.CanJump))
            {
                needs?.ConsumeJumpStamina();
                vertical = Mathf.Sqrt(jumpHeight * 2f * gravity);
                groundedAt = jumpAt = -10f;
            }

            UpdateCrouch(SectorInput.Crouch);

            Vector2 input = SectorInput.Move;
            Vector3 move = transform.right * input.x + transform.forward * input.y;
            bool sprinting = SectorInput.Sprint && !IsCrouching &&
                input.sqrMagnitude > .01f &&
                (needs == null || needs.CanSprint);
            IsSprinting = sprinting;

            vertical = Mathf.Max(vertical - gravity * Time.deltaTime, -45f);
            float impactVelocity = vertical;
            float currentSpeed = IsCrouching
                ? walkSpeed * .53f : sprinting ? runSpeed : walkSpeed;

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

            // A restored scene or streamed tile may have slightly higher
            // terrain than the original spawn. Lift a player embedded under
            // the surface instead of keeping the camera inside the ground.
            float terrainSurface = GroundHeightAt(p);
            if (p.y < terrainSurface - .18f)
            {
                // Only rescue if the controller actually penetrated the
                // terrain. Jumping above the surface never resets velocity.
                TeleportTo(new Vector3(
                    p.x, terrainSurface + .18f, p.z));
                p = transform.position;
                groundedAfterMove = true;
                vertical = -2f;
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

        void UpdateCrouch(bool requested)
        {
            if (body == null || !body.enabled)
                return;
            // Do not stand through a low ceiling: stay crouched until
            // the full-height capsule can fit.
            if (!requested && IsCrouching)
            {
                Vector3 bottom = transform.position + Vector3.up * StandingRadius;
                Vector3 top = transform.position + Vector3.up *
                    (StandingHeight - StandingRadius);
                if (Physics.CheckCapsule(bottom, top,
                        StandingRadius * .92f, ~(1 << 2),
                        QueryTriggerInteraction.Ignore))
                    return;
            }
            if (IsCrouching == requested) return;
            IsCrouching = requested;
            body.height = requested ? 1.16f : StandingHeight;
            body.center = Vector3.up * body.height * .5f;
            cameraInitialized = false;
        }

        void LateUpdate()
        {
            if (view == null)
                return;

            StabilizeVisualAboveTerrain();

            Quaternion rotation = Quaternion.Euler(
                pitch,
                transform.eulerAngles.y,
                0);

            // Mixamo FBX head bones can be offset or scaled differently
            // from the CharacterController. Never mount the gameplay camera
            // inside the actual head mesh: it can intersect skin or terrain.
            float eyeHeight = body != null
                ? IsCrouching ? 1.01f : Mathf.Clamp(
                    body.center.y + body.height * .40f,
                    1.35f, 1.82f)
                : cameraHeight;
            Vector3 pivot = transform.position +
                Vector3.up * eyeHeight;

            if (!thirdPerson)
                pivot += transform.forward * .10f;

            // Even an imported animation with a displaced pelvis must not
            // put the view inside a terrain heightfield while jumping.
            pivot.y = CameraAboveSurface(
                pivot.y, GroundHeightAt(pivot), false);

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

            desiredPosition.y = CameraAboveSurface(
                desiredPosition.y, GroundHeightAt(desiredPosition),
                thirdPerson);

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

            // Smoothing can temporarily drag the camera through a hill
            // after a steep movement or mode switch; guard the final view.
            smoothedCameraPosition.y = CameraAboveSurface(
                smoothedCameraPosition.y,
                GroundHeightAt(smoothedCameraPosition), thirdPerson);

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

            ApplyViewVisibility();
        }

        public void ActivateAtSpawn()
        {
            if (body == null)
                body = GetComponent<CharacterController>();

            body.enabled = false;
            // A new spawn starts upright even if the old character was
            // crouching under cover at the moment of death/save.
            IsCrouching = false;
            body.height = StandingHeight;
            body.center = Vector3.up * StandingHeight * .5f;
            cachedGroundTerrain = null;
            Vector3 spawn = SectorLayout.Spawn;
            spawn.y = GroundHeightAt(spawn) + .16f;
            transform.position = spawn;
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
            ResetLookSmoothing();

            PrimeAnimation();
            CalibrateBodyMesh();
            ApplyViewVisibility();
            Ready = true;
        }

        // Resolve against the terrain actually generated/loaded in the
        // scene. This avoids placing the controller under an altered terrain
        // tile even when the static design height differs slightly.
        float GroundHeightAt(Vector3 position)
        {
            // The camera, body and rescue logic query ground height several
            // times per frame. Reuse the current tile until we cross its bounds.
            // This also avoids repeatedly obtaining Unity's activeTerrain array.
            Terrain terrain = cachedGroundTerrain;
            if (terrain == null || !terrain.isActiveAndEnabled ||
                terrain.terrainData == null ||
                !IsInsideTerrainXZ(terrain.transform.position,
                    terrain.terrainData.size, position))
            {
                cachedGroundTerrain = null;
                foreach (Terrain candidate in Terrain.activeTerrains)
                {
                    if (candidate == null || !candidate.isActiveAndEnabled ||
                        candidate.terrainData == null)
                        continue;
                    if (!IsInsideTerrainXZ(candidate.transform.position,
                            candidate.terrainData.size, position))
                        continue;

                    cachedGroundTerrain = candidate;
                    break;
                }
                terrain = cachedGroundTerrain;
            }

            if (terrain != null)
                return terrain.SampleHeight(position) + terrain.transform.position.y;

            // No streamed terrain at this position: use the analytic fallback.
            return SectorLayout.Height(position.x, position.z);
        }

        public static bool IsInsideTerrainXZ(
            Vector3 tileOrigin, Vector3 tileSize, Vector3 point)
        {
            return point.x >= tileOrigin.x &&
                   point.x <= tileOrigin.x + tileSize.x &&
                   point.z >= tileOrigin.z &&
                   point.z <= tileOrigin.z + tileSize.z;
        }

        // Public deterministic helper so damping is regression-testable.
        // The exponential coefficient is frame-rate independent.
        public static float TurnBlend(float sharpness, float deltaTime)
        {
            if (sharpness <= 0f) return 1f;
            if (deltaTime <= 0f) return 0f;
            return 1f - Mathf.Exp(-sharpness * deltaTime);
        }

        void ResetLookSmoothing()
        {
            targetYaw = transform.eulerAngles.y;
            targetPitch = pitch;
            lookInitialized = true;
        }

        public static float CameraAboveSurface(
            float desiredY, float groundY, bool thirdPersonView)
        {
            // Third person may fly low during camera pitches, whereas
            // first person needs at least a waist-high clearance.
            float clearance = thirdPersonView ? .32f : 1.20f;
            return Mathf.Max(desiredY, groundY + clearance);
        }

        // Normalize imported Mixamo mesh size to the 1.8-m controller and
        // move the skinned feet to the controller's base. Runs once per model,
        // never changes the FBX asset and leaves the procedural rig intact.
        void CalibrateBodyMesh()
        {
            if (visualCalibrated || visual == null)
                return;

            SkinnedMeshRenderer[] skins =
                visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (skins.Length == 0)
                return;

            if (!TryBodyBounds(skins, out Bounds bounds))
                return;

            float scaleFactor = BodyScaleForHeight(bounds.size.y);

            if (Mathf.Abs(scaleFactor - 1f) > .02f)
            {
                visual.localScale *= scaleFactor;
                if (animator != null && animator.isActiveAndEnabled)
                    animator.Update(0f);

                if (!TryBodyBounds(skins, out bounds))
                    return;
            }

            float deltaY = transform.position.y + .045f - bounds.min.y;
            if (Mathf.Abs(deltaY) > .02f && Mathf.Abs(deltaY) < 4f)
                visual.position += Vector3.up * deltaY;

            calibratedSkins = skins;
            calibratedVisualLocalPosition = visual.localPosition;
            visualCalibrated = true;
        }

        // Some Mixamo jump clips keep their imported pelvis translation,
        // making the visible model cross the ground even while its
        // CharacterController remains above the surface. Keep the feet
        // above the real heightfield, without changing the physics capsule.
        void StabilizeVisualAboveTerrain()
        {
            if (!visualCalibrated || visual == null ||
                calibratedSkins == null || calibratedSkins.Length == 0)
                return;

            // Re-evaluate relative to the original calibrated offset;
            // otherwise a correction accumulates every animation frame.
            visual.localPosition = calibratedVisualLocalPosition;

            if (!TryBodyBounds(calibratedSkins, out Bounds bounds))
                return;

            float surface = GroundHeightAt(transform.position);
            float lift = VisualLiftForGround(bounds.min.y, surface);

            if (lift > .06f)
                visual.position += Vector3.up * lift;
        }

        public static float VisualLiftForGround(
            float footBottom, float groundHeight)
        {
            return Mathf.Clamp(groundHeight - footBottom + .02f, 0f, 1.5f);
        }

        public static float BodyScaleForHeight(float meshHeight)
        {
            // Reject corrupt/empty bounds instead of magnifying an FBX
            // to infinity. The body target is a plausible human height.
            if (meshHeight < .05f || meshHeight > 100f)
                return 1f;

            return Mathf.Clamp(1.76f / meshHeight, .2f, 8f);
        }

        static bool TryBodyBounds(
            SkinnedMeshRenderer[] skins, out Bounds bounds)
        {
            bounds = default;
            bool found = false;

            foreach (SkinnedMeshRenderer skin in skins)
            {
                if (skin == null || skin.sharedMesh == null)
                    continue;

                if (!found)
                {
                    bounds = skin.bounds;
                    found = true;
                }
                else
                    bounds.Encapsulate(skin.bounds);
            }

            return found && bounds.size.y > .05f;
        }

        // The local avatar is hidden in FPP, but infected remain visible.
        // Keep the calculation pure so culling regressions have EditMode tests.
        public static int CullingMaskForView(int currentMask, bool thirdPersonView)
        {
            int playerVisual = 1 << SectorArt.PlayerVisualLayer;
            int mask = thirdPersonView
                ? currentMask | playerVisual : currentMask & ~playerVisual;
            return mask & ~(1 << 31); // tactical arrow layer
        }

        void ApplyViewVisibility()
        {
            if (visibleInThirdPerson == thirdPerson || view == null)
                return;

            view.cullingMask = CullingMaskForView(view.cullingMask, thirdPerson);
            visibleInThirdPerson = thirdPerson;
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
            {
                rig.SetCrouching(IsCrouching);
                rig.SetMotion(speed, grounded, verticalSpeed);
            }

            if (animator == null)
                return;

            // Damp locomotion parameter around sprint exit so the
            // animation blend does not repeatedly restart at low stamina.
            animator.SetFloat(SpeedHash, speed, .10f, Time.deltaTime);
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

        public void ApplyCombatRecoil(float degrees)
        {
            if (!Ready || degrees <= 0f)
                return;

            pitch = Mathf.Clamp(pitch - degrees, -75f, 75f);
        }

        public void TeleportTo(Vector3 position)
        {
            if (body == null)
                body = GetComponent<CharacterController>();

            body.enabled = false;
            IsCrouching = false;
            body.height = StandingHeight;
            body.center = Vector3.up * StandingHeight * .5f;
            transform.position = position;
            cachedGroundTerrain = null;
            body.enabled = true;
            vertical = -2f;
            cameraInitialized = false;
            ResetLookSmoothing();
            Physics.SyncTransforms();
        }

        public void Damage(float amount)
        {
            if (amount <= 0f) return;
            SectorEquipment armor = GetComponent<SectorEquipment>();
            float applied = amount * (armor != null ? armor.DamageMultiplier : 1f);
            Health = Mathf.Max(0, Health - applied);

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
