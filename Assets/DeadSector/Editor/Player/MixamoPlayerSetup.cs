using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using DeadSector.Runtime.Player;

namespace DeadSector.Editor.Player
{
    public static class MixamoPlayerSetup
    {
        private const string Root = "Assets/DeadSector/Characters/Player";
        private const string MixamoFolder = Root + "/Mixamo";
        private const string ControllersFolder = Root + "/Controllers";
        private const string PrefabsFolder = Root + "/Prefabs";
        private const string ControllerPath = ControllersFolder + "/AC_Player_Mixamo.controller";
        private const string UpperBodyMaskPath = ControllersFolder + "/MASK_Player_UpperBody.mask";
        private const string PlayerPrefabPath = PrefabsFolder + "/Player_Mixamo_FullBody.prefab";

        // Only a dedicated character FBX may be used as the visible player body.
        // Animation FBXs can also contain a skinned mesh, but using one of them
        // makes the prefab silently pick the wrong character (for example Beta).
        private static readonly string[] DedicatedPlayerModelNames =
        {
            "PlayerModel",
            "Player Model",
            "X Bot",
            "Y Bot"
        };

        [MenuItem("Dead Sector/Character/Mixamo/01 - Configure FBX As Humanoid")]
        public static void ConfigureFbxAsHumanoid()
        {
            EnsureFolders();

            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { MixamoFolder });

            if (guids.Length == 0)
            {
                Debug.LogWarning("[Dead Sector] No FBX models found in " + MixamoFolder);
                return;
            }

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (!path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
                    continue;

                string fileName = Path.GetFileNameWithoutExtension(path);

                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = true;

                ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;

                if (clips != null && clips.Length > 0)
                {
                    bool loop = ShouldLoop(fileName);

                    foreach (ModelImporterClipAnimation clip in clips)
                    {
                        clip.loopTime = loop;
                        clip.loopPose = loop;

                        // Dead Sector movement is driven by CharacterController.
                        // Bake FBX root translation into the pose so the mesh
                        // does not walk away from the player capsule.
                        clip.lockRootPositionXZ = true;
                        clip.lockRootHeightY = true;
                        clip.lockRootRotation = true;

                        clip.keepOriginalPositionXZ = false;
                        clip.keepOriginalPositionY = false;
                        clip.keepOriginalOrientation = false;
                    }

                    importer.clipAnimations = clips;
                }

                importer.SaveAndReimport();
                Debug.Log("[Dead Sector] Humanoid configured: " + path);
            }

            AssetDatabase.Refresh();
        }

        [MenuItem("Dead Sector/Character/Mixamo/02 - Build Animator From Current FBX")]
        public static void BuildAnimator()
        {
            EnsureFolders();

            AnimationClip idle = FindClip("Idle");
            AnimationClip walk = FindClip("Swagger Walk", "Walking", "Walk");
            AnimationClip run = FindClip("Running", "Run");

            AnimationClip jump = FindClip("Jumping");
            AnimationClip runningJump = FindClip("Running Jump");
            AnimationClip falling = FindClip("Jumping Down", "Falling Idle");
            AnimationClip landing = FindClip("Falling To Landing", "Landing");
            AnimationClip hardLanding = FindClip("Hard Landing");
            AnimationClip rollLanding = FindClip("Falling To Roll");

            AnimationClip boxing = FindClip("Boxing");
            AnimationClip punch = FindClip("Punching");
            AnimationClip block = FindClip("Outward Block");

            if (idle == null)
            {
                Debug.LogError("[Dead Sector] Idle.fbx is required before building the player Animator.");
                return;
            }

            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            AddParameter(controller, "Speed", AnimatorControllerParameterType.Float);
            AddParameter(controller, "Grounded", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "IsRunning", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "VerticalSpeed", AnimatorControllerParameterType.Float);
            AddParameter(controller, "LandingType", AnimatorControllerParameterType.Int);
            AddParameter(controller, "CombatMode", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "Punch", AnimatorControllerParameterType.Trigger);
            AddParameter(controller, "Block", AnimatorControllerParameterType.Bool);

            BuildLocomotionLayer(
                controller,
                idle,
                walk,
                run,
                jump,
                runningJump,
                falling,
                landing,
                hardLanding,
                rollLanding);
            BuildUpperBodyLayer(controller, boxing, punch, block);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "[Dead Sector] Mixamo Animator created. " +
                "Current locomotion: Idle=" + NameOf(idle) +
                ", Walk=" + NameOf(walk) +
                ", Run=" + NameOf(run) +
                ", Jump=" + NameOf(jump) +
                ", RunningJump=" + NameOf(runningJump) +
                ", Falling=" + NameOf(falling) +
                ", Landing=" + NameOf(landing) +
                ", HardLanding=" + NameOf(hardLanding) +
                ", RollLanding=" + NameOf(rollLanding) +
                ". Combat: Boxing=" + NameOf(boxing) +
                ", Punch=" + NameOf(punch) +
                ", Block=" + NameOf(block));
        }

        [MenuItem("Dead Sector/Character/Mixamo/03 - Build Playable Full-Body Player")]
        public static void BuildPlayablePlayer()
        {
            EnsureFolders();

            RuntimeAnimatorController controller =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);

            if (controller == null)
            {
                Debug.LogWarning("[Dead Sector] Animator controller is missing. Building it first...");
                BuildAnimator();
                controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            }

            if (controller == null)
                return;

            string modelPath = FindSkinnedHumanoidModel();

            if (string.IsNullOrEmpty(modelPath))
            {
                Debug.LogError(
                    "[Dead Sector] No dedicated FULL-BODY player model was found in " + MixamoFolder + ".\n" +
                    "Animation FBXs are intentionally NOT used as the visible body anymore. " +
                    "Add a dedicated model named X Bot.fbx (or PlayerModel.fbx) downloaded with Skin = With Skin, " +
                    "then rerun BUILD EVERYTHING.");
                return;
            }

            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelAsset == null)
            {
                Debug.LogError("[Dead Sector] Could not load player model: " + modelPath);
                return;
            }

            GameObject root = new GameObject("Player_Mixamo_FullBody");

            CharacterController characterController = root.AddComponent<CharacterController>();
            characterController.height = 1.82f;
            characterController.radius = 0.30f;
            characterController.center = new Vector3(0f, 0.91f, 0f);
            characterController.stepOffset = 0.30f;

            PlayerMovement movement = root.AddComponent<PlayerMovement>();
            movement.walkSpeed = 3.2f;
            movement.runSpeed = 5.8f;
            movement.jumpHeight = 1.4f;
            movement.hardLandingVelocity = -7.5f;
            movement.rollLandingVelocity = -11.5f;

            GameObject modelInstance =
                PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;

            if (modelInstance == null)
            {
                UnityEngine.Object.DestroyImmediate(root);
                Debug.LogError("[Dead Sector] Could not instantiate player model.");
                return;
            }

            modelInstance.name = "FullBody";
            modelInstance.transform.SetParent(root.transform, false);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;

            Animator animator = modelInstance.GetComponentInChildren<Animator>();

            if (animator == null)
                animator = modelInstance.AddComponent<Animator>();

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            if (!animator.isHuman)
            {
                UnityEngine.Object.DestroyImmediate(root);
                Debug.LogError(
                    "[Dead Sector] The selected model is not a valid Humanoid. " +
                    "Run '01 - Configure FBX As Humanoid' first.");
                return;
            }

            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);

            GameObject cameraObject = new GameObject("PlayerCamera");
            cameraObject.transform.SetParent(root.transform, false);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.02f;
            camera.fieldOfView = 75f;
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();

            movement.playerCamera = camera;
            movement.animator = animator;
            movement.headBone = head;
            movement.leftHandSocket = leftHand;
            movement.rightHandSocket = rightHand;
            movement.cameraOffset = new Vector3(0f, 0.015f, 0.10f);
            movement.nearClipPlane = 0.02f;

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Dead Sector] Playable full-body player created: " + PlayerPrefabPath +
                "\nDrag it into a test scene and press Play.");
        }

        [MenuItem("Dead Sector/Character/Mixamo/00 - BUILD EVERYTHING")]
        public static void BuildEverything()
        {
            ConfigureFbxAsHumanoid();
            BuildAnimator();
            BuildPlayablePlayer();
        }

        private static void BuildLocomotionLayer(
            AnimatorController controller,
            AnimationClip idle,
            AnimationClip walk,
            AnimationClip run,
            AnimationClip jump,
            AnimationClip runningJump,
            AnimationClip falling,
            AnimationClip landing,
            AnimationClip hardLanding,
            AnimationClip rollLanding)
        {
            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            sm.name = "Locomotion";

            AnimatorState idleState = sm.AddState("Idle");
            idleState.motion = idle;
            sm.defaultState = idleState;

            AnimatorState walkState = null;
            AnimatorState runState = null;

            if (walk != null)
            {
                walkState = sm.AddState("Walk");
                walkState.motion = walk;

                AddTransition(
                    idleState,
                    walkState,
                    0.12f,
                    false,
                    C(AnimatorConditionMode.Greater, 0.10f, "Speed"),
                    C(AnimatorConditionMode.IfNot, 0f, "IsRunning"),
                    C(AnimatorConditionMode.If, 0f, "Grounded"));

                AddTransition(
                    walkState,
                    idleState,
                    0.12f,
                    false,
                    C(AnimatorConditionMode.Less, 0.10f, "Speed"));
            }

            if (run != null)
            {
                runState = sm.AddState("Run");
                runState.motion = run;

                if (walkState != null)
                {
                    AddTransition(
                        walkState,
                        runState,
                        0.10f,
                        false,
                        C(AnimatorConditionMode.If, 0f, "IsRunning"));

                    AddTransition(
                        runState,
                        walkState,
                        0.10f,
                        false,
                        C(AnimatorConditionMode.IfNot, 0f, "IsRunning"),
                        C(AnimatorConditionMode.Greater, 0.10f, "Speed"));
                }
                else
                {
                    AddTransition(
                        idleState,
                        runState,
                        0.10f,
                        false,
                        C(AnimatorConditionMode.Greater, 0.10f, "Speed"));
                }

                AddTransition(
                    runState,
                    idleState,
                    0.10f,
                    false,
                    C(AnimatorConditionMode.Less, 0.10f, "Speed"));
            }

            AnimatorState jumpState = null;
            AnimatorState runningJumpState = null;
            AnimatorState fallingState = null;
            AnimatorState landingState = null;
            AnimatorState hardLandingState = null;
            AnimatorState rollLandingState = null;

            if (jump != null)
            {
                jumpState = sm.AddState("Jump");
                jumpState.motion = jump;

                AddTransition(
                    idleState,
                    jumpState,
                    0.06f,
                    false,
                    C(AnimatorConditionMode.IfNot, 0f, "Grounded"),
                    C(AnimatorConditionMode.Greater, 0.05f, "VerticalSpeed"));

                if (walkState != null)
                {
                    AddTransition(
                        walkState,
                        jumpState,
                        0.06f,
                        false,
                        C(AnimatorConditionMode.IfNot, 0f, "Grounded"),
                        C(AnimatorConditionMode.Greater, 0.05f, "VerticalSpeed"),
                        C(AnimatorConditionMode.IfNot, 0f, "IsRunning"));
                }
            }

            if (runningJump != null && runState != null)
            {
                runningJumpState = sm.AddState("Running Jump");
                runningJumpState.motion = runningJump;

                AddTransition(
                    runState,
                    runningJumpState,
                    0.05f,
                    false,
                    C(AnimatorConditionMode.IfNot, 0f, "Grounded"),
                    C(AnimatorConditionMode.Greater, 0.05f, "VerticalSpeed"));
            }

            if (falling != null)
            {
                fallingState = sm.AddState("Falling");
                fallingState.motion = falling;

                AnimatorStateTransition fallFromIdle = idleState.AddTransition(fallingState);
                fallFromIdle.hasExitTime = false;
                fallFromIdle.duration = 0.08f;
                fallFromIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
                fallFromIdle.AddCondition(AnimatorConditionMode.Less, -0.05f, "VerticalSpeed");

                if (walkState != null)
                {
                    AnimatorStateTransition fallFromWalk = walkState.AddTransition(fallingState);
                    fallFromWalk.hasExitTime = false;
                    fallFromWalk.duration = 0.08f;
                    fallFromWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
                    fallFromWalk.AddCondition(AnimatorConditionMode.Less, -0.05f, "VerticalSpeed");
                }

                if (runState != null)
                {
                    AnimatorStateTransition fallFromRun = runState.AddTransition(fallingState);
                    fallFromRun.hasExitTime = false;
                    fallFromRun.duration = 0.08f;
                    fallFromRun.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
                    fallFromRun.AddCondition(AnimatorConditionMode.Less, -0.05f, "VerticalSpeed");
                }

                if (jumpState != null)
                {
                    AddTransition(
                        jumpState,
                        fallingState,
                        0.08f,
                        false,
                        C(AnimatorConditionMode.Less, -0.05f, "VerticalSpeed"));
                }

                if (runningJumpState != null)
                {
                    AddTransition(
                        runningJumpState,
                        fallingState,
                        0.08f,
                        false,
                        C(AnimatorConditionMode.Less, -0.05f, "VerticalSpeed"));
                }
            }

            if (landing != null)
            {
                landingState = sm.AddState("Landing");
                landingState.motion = landing;
            }

            if (hardLanding != null)
            {
                hardLandingState = sm.AddState("Hard Landing");
                hardLandingState.motion = hardLanding;
            }

            if (rollLanding != null)
            {
                rollLandingState = sm.AddState("Landing Roll");
                rollLandingState.motion = rollLanding;
            }

            if (fallingState != null)
            {
                if (landingState != null)
                {
                    AddTransition(
                        fallingState,
                        landingState,
                        0.04f,
                        false,
                        C(AnimatorConditionMode.If, 0f, "Grounded"),
                        C(AnimatorConditionMode.Equals, 0f, "LandingType"));
                }

                if (hardLandingState != null)
                {
                    AddTransition(
                        fallingState,
                        hardLandingState,
                        0.04f,
                        false,
                        C(AnimatorConditionMode.If, 0f, "Grounded"),
                        C(AnimatorConditionMode.Equals, 1f, "LandingType"));
                }

                if (rollLandingState != null)
                {
                    AddTransition(
                        fallingState,
                        rollLandingState,
                        0.04f,
                        false,
                        C(AnimatorConditionMode.If, 0f, "Grounded"),
                        C(AnimatorConditionMode.Equals, 2f, "LandingType"));
                }
            }

            if (landingState != null)
                AddExitToLocomotion(landingState, idleState, walkState, runState);

            if (hardLandingState != null)
                AddExitToLocomotion(hardLandingState, idleState, walkState, runState);

            if (rollLandingState != null)
                AddExitToLocomotion(rollLandingState, idleState, walkState, runState);
        }

        private static void AddExitToLocomotion(
            AnimatorState source,
            AnimatorState idle,
            AnimatorState walk,
            AnimatorState run)
        {
            AnimatorStateTransition toIdle = source.AddTransition(idle);
            toIdle.hasExitTime = true;
            toIdle.exitTime = 0.90f;
            toIdle.duration = 0.10f;
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.10f, "Speed");

            if (walk != null)
            {
                AnimatorStateTransition toWalk = source.AddTransition(walk);
                toWalk.hasExitTime = true;
                toWalk.exitTime = 0.88f;
                toWalk.duration = 0.10f;
                toWalk.AddCondition(AnimatorConditionMode.Greater, 0.10f, "Speed");
                toWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsRunning");
            }

            if (run != null)
            {
                AnimatorStateTransition toRun = source.AddTransition(run);
                toRun.hasExitTime = true;
                toRun.exitTime = 0.88f;
                toRun.duration = 0.10f;
                toRun.AddCondition(AnimatorConditionMode.Greater, 0.10f, "Speed");
                toRun.AddCondition(AnimatorConditionMode.If, 0f, "IsRunning");
            }
        }

        private static void BuildUpperBodyLayer(
            AnimatorController controller,
            AnimationClip boxing,
            AnimationClip punch,
            AnimationClip block)
        {
            if (boxing == null && punch == null && block == null)
                return;

            AvatarMask mask = CreateUpperBodyMask();

            controller.AddLayer("Upper Body");

            AnimatorControllerLayer[] layers = controller.layers;
            AnimatorControllerLayer layer = layers[layers.Length - 1];
            layer.defaultWeight = 1f;
            layer.blendingMode = AnimatorLayerBlendingMode.Override;
            layer.avatarMask = mask;
            layers[layers.Length - 1] = layer;
            controller.layers = layers;

            AnimatorStateMachine sm = controller.layers[controller.layers.Length - 1].stateMachine;
            AnimatorState empty = sm.AddState("Empty");
            sm.defaultState = empty;

            AnimatorState boxingState = null;

            if (boxing != null)
            {
                boxingState = sm.AddState("Boxing");
                boxingState.motion = boxing;

                AddTransition(
                    empty,
                    boxingState,
                    0.10f,
                    false,
                    C(AnimatorConditionMode.If, 0f, "CombatMode"));

                AddTransition(
                    boxingState,
                    empty,
                    0.10f,
                    false,
                    C(AnimatorConditionMode.IfNot, 0f, "CombatMode"));
            }

            if (punch != null)
            {
                AnimatorState punchState = sm.AddState("Punch");
                punchState.motion = punch;

                AnimatorStateTransition enterPunch = sm.AddAnyStateTransition(punchState);
                enterPunch.hasExitTime = false;
                enterPunch.duration = 0.04f;
                enterPunch.canTransitionToSelf = false;
                enterPunch.AddCondition(AnimatorConditionMode.If, 0f, "Punch");

                AnimatorStateTransition punchToEmpty = punchState.AddTransition(empty);
                punchToEmpty.hasExitTime = true;
                punchToEmpty.exitTime = 0.92f;
                punchToEmpty.duration = 0.08f;
                punchToEmpty.AddCondition(AnimatorConditionMode.IfNot, 0f, "CombatMode");

                if (boxingState != null)
                {
                    AnimatorStateTransition punchToBoxing = punchState.AddTransition(boxingState);
                    punchToBoxing.hasExitTime = true;
                    punchToBoxing.exitTime = 0.92f;
                    punchToBoxing.duration = 0.08f;
                    punchToBoxing.AddCondition(AnimatorConditionMode.If, 0f, "CombatMode");
                }
            }

            if (block != null)
            {
                AnimatorState blockState = sm.AddState("Block");
                blockState.motion = block;

                AnimatorStateTransition enterBlock = sm.AddAnyStateTransition(blockState);
                enterBlock.hasExitTime = false;
                enterBlock.duration = 0.05f;
                enterBlock.canTransitionToSelf = false;
                enterBlock.AddCondition(AnimatorConditionMode.If, 0f, "Block");

                AnimatorStateTransition blockToEmpty = blockState.AddTransition(empty);
                blockToEmpty.hasExitTime = true;
                blockToEmpty.exitTime = 0.92f;
                blockToEmpty.duration = 0.08f;
                blockToEmpty.AddCondition(AnimatorConditionMode.IfNot, 0f, "CombatMode");

                if (boxingState != null)
                {
                    AnimatorStateTransition blockToBoxing = blockState.AddTransition(boxingState);
                    blockToBoxing.hasExitTime = true;
                    blockToBoxing.exitTime = 0.92f;
                    blockToBoxing.duration = 0.08f;
                    blockToBoxing.AddCondition(AnimatorConditionMode.If, 0f, "CombatMode");
                }
            }
        }

        private static AvatarMask CreateUpperBodyMask()
        {
            AssetDatabase.DeleteAsset(UpperBodyMaskPath);

            AvatarMask mask = new AvatarMask();
            mask.name = "MASK_Player_UpperBody";

            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Root, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);

            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg, false);

            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);

            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFootIK, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFootIK, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftHandIK, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK, true);

            AssetDatabase.CreateAsset(mask, UpperBodyMaskPath);
            return mask;
        }

        private static AnimationClip FindClip(params string[] preferredNames)
        {
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { MixamoFolder });

            foreach (string preferred in preferredNames)
            {
                AnimationClip exact = FindClipPass(guids, preferred, true);
                if (exact != null)
                    return exact;
            }

            foreach (string preferred in preferredNames)
            {
                AnimationClip partial = FindClipPass(guids, preferred, false);
                if (partial != null)
                    return partial;
            }

            return null;
        }

        private static AnimationClip FindClipPass(
            string[] guids,
            string preferred,
            bool exactMatch)
        {
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = Path.GetFileNameWithoutExtension(path);

                bool matches = exactMatch
                    ? file.Equals(preferred, StringComparison.OrdinalIgnoreCase)
                    : file.Contains(preferred, StringComparison.OrdinalIgnoreCase);

                if (!matches)
                    continue;

                AnimationClip clip = AssetDatabase
                    .LoadAllAssetsAtPath(path)
                    .OfType<AnimationClip>()
                    .FirstOrDefault(c =>
                        !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase));

                if (clip != null)
                    return clip;
            }

            return null;
        }

        private static string FindSkinnedHumanoidModel()
        {
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { MixamoFolder });

            // Exact dedicated model names first. This prevents Unity from
            // accidentally using the skin embedded in Idle/Boxing/etc.
            foreach (string preferredName in DedicatedPlayerModelNames)
            {
                string exact = FindDedicatedSkinnedModel(guids, preferredName, true);
                if (!string.IsNullOrEmpty(exact))
                    return exact;
            }

            // Then allow names such as "X Bot Character" or "PlayerModel Male".
            foreach (string preferredName in DedicatedPlayerModelNames)
            {
                string partial = FindDedicatedSkinnedModel(guids, preferredName, false);
                if (!string.IsNullOrEmpty(partial))
                    return partial;
            }

            return null;
        }

        private static string FindDedicatedSkinnedModel(
            string[] guids,
            string preferredName,
            bool exactMatch)
        {
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = Path.GetFileNameWithoutExtension(path);

                bool nameMatches = exactMatch
                    ? file.Equals(preferredName, StringComparison.OrdinalIgnoreCase)
                    : file.Contains(preferredName, StringComparison.OrdinalIgnoreCase);

                if (!nameMatches)
                    continue;

                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null)
                    continue;

                GameObject instance = PrefabUtility.InstantiatePrefab(asset) as GameObject;
                if (instance == null)
                    continue;

                bool hasRenderer =
                    instance.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length > 0;

                Animator animator = instance.GetComponentInChildren<Animator>();
                bool isHumanoid = animator != null && animator.isHuman;

                UnityEngine.Object.DestroyImmediate(instance);

                if (hasRenderer && isHumanoid)
                {
                    Debug.Log("[Dead Sector] FULL-BODY player model selected: " + path);
                    return path;
                }
            }

            return null;
        }

        private static bool ShouldLoop(string fileName)
        {
            return
                fileName.Contains("Idle", StringComparison.OrdinalIgnoreCase) ||
                fileName.Contains("Walk", StringComparison.OrdinalIgnoreCase) ||
                fileName.Contains("Running", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("Run", StringComparison.OrdinalIgnoreCase) ||
                fileName.Contains("Jumping Down", StringComparison.OrdinalIgnoreCase) ||
                fileName.Contains("Falling Idle", StringComparison.OrdinalIgnoreCase) ||
                fileName.Contains("Boxing", StringComparison.OrdinalIgnoreCase);
        }

        private static void AddParameter(
            AnimatorController controller,
            string name,
            AnimatorControllerParameterType type)
        {
            if (controller.parameters.Any(p => p.name == name))
                return;

            controller.AddParameter(name, type);
        }

        private static void AddTransition(
            AnimatorState from,
            AnimatorState to,
            float duration,
            bool hasExitTime,
            params ConditionSpec[] conditions)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = hasExitTime;
            transition.duration = duration;

            foreach (ConditionSpec condition in conditions)
                transition.AddCondition(condition.Mode, condition.Threshold, condition.Parameter);
        }

        private static ConditionSpec C(
            AnimatorConditionMode mode,
            float threshold,
            string parameter)
        {
            return new ConditionSpec
            {
                Mode = mode,
                Threshold = threshold,
                Parameter = parameter
            };
        }

        private static string NameOf(AnimationClip clip)
        {
            return clip == null ? "missing" : clip.name;
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "DeadSector");
            EnsureFolder("Assets/DeadSector", "Characters");
            EnsureFolder("Assets/DeadSector/Characters", "Player");
            EnsureFolder(Root, "Mixamo");
            EnsureFolder(Root, "Controllers");
            EnsureFolder(Root, "Prefabs");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }

        private struct ConditionSpec
        {
            public AnimatorConditionMode Mode;
            public float Threshold;
            public string Parameter;
        }
    }
}
