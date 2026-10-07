using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DeadSector.Editor.Player
{
    public static class PlayerMannequinGenerator
    {
        private const string Root = "Assets/DeadSector/Characters/Player";
        private const string Materials = Root + "/Materials";
        private const string Animations = Root + "/Animations";
        private const string Controllers = Root + "/Controllers";
        private const string Prefabs = Root + "/Prefabs";
        private const string Mixamo = Root + "/Mixamo";

        private const string MaterialPath = Materials + "/M_PlayerMannequin.mat";
        private const string IdlePath = Animations + "/AN_Player_Idle.anim";
        private const string WalkPath = Animations + "/AN_Player_Walk.anim";
        private const string RunPath = Animations + "/AN_Player_Run.anim";
        private const string JumpPath = Animations + "/AN_Player_Jump.anim";
        private const string ControllerPath = Controllers + "/AC_PlayerMannequin.controller";

        [MenuItem("Dead Sector/Character/01 - Build Animated Mannequins")]
        public static void BuildAnimatedMannequins()
        {
            EnsureFolders();

            Material material = CreateOrLoadMaterial();

            AnimationClip idle = CreateIdleClip();
            AnimationClip walk = CreateWalkClip();
            AnimationClip run = CreateRunClip();
            AnimationClip jump = CreateJumpClip();

            AnimatorController controller = CreateAnimatorController(idle, walk, run, jump);

            CreatePlayerPrefab("Player_Mannequin_Broad", material, controller, 1.00f, 1.00f);
            CreatePlayerPrefab("Player_Mannequin_Slim", material, controller, 0.86f, 0.96f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Dead Sector] Animated player mannequins created in " + Prefabs);
        }

        [MenuItem("Dead Sector/Character/02 - Open Mannequin Folder")]
        public static void SelectMannequinFolder()
        {
            EnsureFolders();
            UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Root);
            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
        }

        [MenuItem("Dead Sector/Character/03 - Open Mixamo")]
        public static void OpenMixamo()
        {
            Application.OpenURL("https://www.mixamo.com/");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "DeadSector");
            EnsureFolder("Assets/DeadSector", "Characters");
            EnsureFolder("Assets/DeadSector/Characters", "Player");
            EnsureFolder(Root, "Materials");
            EnsureFolder(Root, "Animations");
            EnsureFolder(Root, "Controllers");
            EnsureFolder(Root, "Prefabs");
            EnsureFolder(Root, "Mixamo");
        }

        private static Material CreateOrLoadMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
                return material;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            material = new Material(shader);
            material.name = "M_PlayerMannequin";
            material.color = new Color(0.78f, 0.80f, 0.82f, 1f);

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.38f);

            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0.0f);

            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static void CreatePlayerPrefab(
            string prefabName,
            Material material,
            RuntimeAnimatorController controller,
            float widthScale,
            float heightScale)
        {
            GameObject root = new GameObject(prefabName);
            root.layer = 0;

            CharacterController characterController = root.AddComponent<CharacterController>();
            characterController.height = 1.80f * heightScale;
            characterController.radius = 0.30f * widthScale;
            characterController.center = new Vector3(0f, 0.90f * heightScale, 0f);
            characterController.stepOffset = 0.28f;

            global::PlayerMovement movement = root.AddComponent<global::PlayerMovement>();
            movement.walkSpeed = 3.2f;
            movement.runSpeed = 5.8f;
            movement.jumpHeight = 1.35f;

            GameObject rig = new GameObject("Rig");
            rig.transform.SetParent(root.transform, false);

            Animator animator = rig.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            RigBones bones = BuildRig(rig.transform, material, widthScale, heightScale);

            GameObject cameraObject = new GameObject("PlayerCamera");
            cameraObject.transform.SetParent(root.transform, false);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.03f;
            camera.fieldOfView = 75f;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.tag = "MainCamera";

            movement.playerCamera = camera;
            movement.animator = animator;
            movement.headBone = bones.Head;
            movement.leftHandSocket = bones.LeftHand;
            movement.rightHandSocket = bones.RightHand;
            movement.cameraOffset = new Vector3(0f, 0.015f, 0.105f);

            string prefabPath = Prefabs + "/" + prefabName + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            Debug.Log("[Dead Sector] Created " + prefabPath);
        }

        private static RigBones BuildRig(
            Transform rig,
            Material material,
            float width,
            float height)
        {
            float h = height;
            float w = width;

            Transform hips = Bone(rig, "Hips", new Vector3(0f, 0.96f * h, 0f));
            Part(hips, "Pelvis", PrimitiveType.Sphere, Vector3.zero,
                new Vector3(0.33f * w, 0.22f * h, 0.22f * w), material);

            Transform spine = Bone(hips, "Spine", new Vector3(0f, 0.18f * h, 0f));
            Part(spine, "Abdomen", PrimitiveType.Sphere, new Vector3(0f, 0.08f * h, 0f),
                new Vector3(0.27f * w, 0.20f * h, 0.20f * w), material);

            Transform chest = Bone(spine, "Chest", new Vector3(0f, 0.18f * h, 0f));
            Part(chest, "ChestVisual", PrimitiveType.Sphere, new Vector3(0f, 0.12f * h, 0f),
                new Vector3(0.38f * w, 0.27f * h, 0.23f * w), material);

            Transform neck = Bone(chest, "Neck", new Vector3(0f, 0.30f * h, 0f));
            Part(neck, "NeckVisual", PrimitiveType.Cylinder, new Vector3(0f, 0.035f * h, 0f),
                new Vector3(0.09f * w, 0.035f * h, 0.09f * w), material);

            Transform head = Bone(neck, "Head", new Vector3(0f, 0.13f * h, 0f));
            Part(head, "HeadVisual", PrimitiveType.Sphere, new Vector3(0f, 0.10f * h, 0f),
                new Vector3(0.22f * w, 0.27f * h, 0.20f * w), material);

            Transform leftShoulder = Bone(chest, "LeftShoulder", new Vector3(-0.34f * w, 0.18f * h, 0f));
            Part(leftShoulder, "LeftShoulderCap", PrimitiveType.Sphere, Vector3.zero,
                new Vector3(0.16f * w, 0.15f * h, 0.16f * w), material);

            Transform leftUpperArm = Bone(leftShoulder, "LeftUpperArm", Vector3.zero);
            Part(leftUpperArm, "LeftUpperArmVisual", PrimitiveType.Capsule, new Vector3(0f, -0.16f * h, 0f),
                new Vector3(0.12f * w, 0.18f * h, 0.12f * w), material);

            Transform leftLowerArm = Bone(leftUpperArm, "LeftLowerArm", new Vector3(0f, -0.34f * h, 0f));
            Part(leftLowerArm, "LeftLowerArmVisual", PrimitiveType.Capsule, new Vector3(0f, -0.15f * h, 0f),
                new Vector3(0.10f * w, 0.17f * h, 0.10f * w), material);

            Transform leftHand = Bone(leftLowerArm, "LeftHand", new Vector3(0f, -0.31f * h, 0f));
            Part(leftHand, "LeftHandVisual", PrimitiveType.Sphere, new Vector3(0f, -0.07f * h, 0.01f),
                new Vector3(0.10f * w, 0.13f * h, 0.07f * w), material);

            Transform rightShoulder = Bone(chest, "RightShoulder", new Vector3(0.34f * w, 0.18f * h, 0f));
            Part(rightShoulder, "RightShoulderCap", PrimitiveType.Sphere, Vector3.zero,
                new Vector3(0.16f * w, 0.15f * h, 0.16f * w), material);

            Transform rightUpperArm = Bone(rightShoulder, "RightUpperArm", Vector3.zero);
            Part(rightUpperArm, "RightUpperArmVisual", PrimitiveType.Capsule, new Vector3(0f, -0.16f * h, 0f),
                new Vector3(0.12f * w, 0.18f * h, 0.12f * w), material);

            Transform rightLowerArm = Bone(rightUpperArm, "RightLowerArm", new Vector3(0f, -0.34f * h, 0f));
            Part(rightLowerArm, "RightLowerArmVisual", PrimitiveType.Capsule, new Vector3(0f, -0.15f * h, 0f),
                new Vector3(0.10f * w, 0.17f * h, 0.10f * w), material);

            Transform rightHand = Bone(rightLowerArm, "RightHand", new Vector3(0f, -0.31f * h, 0f));
            Part(rightHand, "RightHandVisual", PrimitiveType.Sphere, new Vector3(0f, -0.07f * h, 0.01f),
                new Vector3(0.10f * w, 0.13f * h, 0.07f * w), material);

            Transform leftUpperLeg = Bone(hips, "LeftUpperLeg", new Vector3(-0.17f * w, -0.10f * h, 0f));
            Part(leftUpperLeg, "LeftThighVisual", PrimitiveType.Capsule, new Vector3(0f, -0.22f * h, 0f),
                new Vector3(0.16f * w, 0.25f * h, 0.16f * w), material);

            Transform leftLowerLeg = Bone(leftUpperLeg, "LeftLowerLeg", new Vector3(0f, -0.47f * h, 0f));
            Part(leftLowerLeg, "LeftCalfVisual", PrimitiveType.Capsule, new Vector3(0f, -0.20f * h, 0f),
                new Vector3(0.13f * w, 0.22f * h, 0.13f * w), material);

            Transform leftFoot = Bone(leftLowerLeg, "LeftFoot", new Vector3(0f, -0.42f * h, 0.04f));
            Part(leftFoot, "LeftFootVisual", PrimitiveType.Cube, new Vector3(0f, -0.04f * h, 0.10f),
                new Vector3(0.20f * w, 0.10f * h, 0.36f * w), material);

            Transform rightUpperLeg = Bone(hips, "RightUpperLeg", new Vector3(0.17f * w, -0.10f * h, 0f));
            Part(rightUpperLeg, "RightThighVisual", PrimitiveType.Capsule, new Vector3(0f, -0.22f * h, 0f),
                new Vector3(0.16f * w, 0.25f * h, 0.16f * w), material);

            Transform rightLowerLeg = Bone(rightUpperLeg, "RightLowerLeg", new Vector3(0f, -0.47f * h, 0f));
            Part(rightLowerLeg, "RightCalfVisual", PrimitiveType.Capsule, new Vector3(0f, -0.20f * h, 0f),
                new Vector3(0.13f * w, 0.22f * h, 0.13f * w), material);

            Transform rightFoot = Bone(rightLowerLeg, "RightFoot", new Vector3(0f, -0.42f * h, 0.04f));
            Part(rightFoot, "RightFootVisual", PrimitiveType.Cube, new Vector3(0f, -0.04f * h, 0.10f),
                new Vector3(0.20f * w, 0.10f * h, 0.36f * w), material);

            return new RigBones
            {
                Head = head,
                LeftHand = leftHand,
                RightHand = rightHand
            };
        }

        private static Transform Bone(Transform parent, string name, Vector3 localPosition)
        {
            GameObject obj = new GameObject(name);
            Transform t = obj.transform;
            t.SetParent(parent, false);
            t.localPosition = localPosition;
            return t;
        }

        private static void Part(
            Transform parent,
            string name,
            PrimitiveType primitive,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = localScale;

            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.DestroyImmediate(collider);

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        private static AnimationClip CreateIdleClip()
        {
            AnimationClip clip = NewOrReplaceClip(IdlePath, "AN_Player_Idle", true);
            SetEulerX(clip, "Hips/Spine/Chest", new[] { 0f, 1f, 2f }, new[] { 0f, 1.4f, 0f });
            SetLocalY(clip, "Hips", new[] { 0f, 1f, 2f }, new[] { 0.96f, 0.968f, 0.96f });
            SaveClip(clip, IdlePath);
            return clip;
        }

        private static AnimationClip CreateWalkClip()
        {
            AnimationClip clip = NewOrReplaceClip(WalkPath, "AN_Player_Walk", true);

            float[] t = { 0f, 0.25f, 0.5f, 0.75f, 1f };
            SetEulerX(clip, "Hips/LeftUpperLeg", t, new[] { 28f, 0f, -28f, 0f, 28f });
            SetEulerX(clip, "Hips/RightUpperLeg", t, new[] { -28f, 0f, 28f, 0f, -28f });
            SetEulerX(clip, "Hips/Spine/Chest/LeftShoulder/LeftUpperArm", t, new[] { -22f, 0f, 22f, 0f, -22f });
            SetEulerX(clip, "Hips/Spine/Chest/RightShoulder/RightUpperArm", t, new[] { 22f, 0f, -22f, 0f, 22f });
            SetEulerX(clip, "Hips/LeftUpperLeg/LeftLowerLeg", t, new[] { 4f, 28f, 8f, 6f, 4f });
            SetEulerX(clip, "Hips/RightUpperLeg/RightLowerLeg", t, new[] { 8f, 6f, 4f, 28f, 8f });
            SetLocalY(clip, "Hips", t, new[] { 0.96f, 0.975f, 0.96f, 0.975f, 0.96f });

            SaveClip(clip, WalkPath);
            return clip;
        }

        private static AnimationClip CreateRunClip()
        {
            AnimationClip clip = NewOrReplaceClip(RunPath, "AN_Player_Run", true);

            float[] t = { 0f, 0.18f, 0.36f, 0.54f, 0.72f };
            SetEulerX(clip, "Hips/LeftUpperLeg", t, new[] { 48f, 0f, -48f, 0f, 48f });
            SetEulerX(clip, "Hips/RightUpperLeg", t, new[] { -48f, 0f, 48f, 0f, -48f });
            SetEulerX(clip, "Hips/Spine/Chest/LeftShoulder/LeftUpperArm", t, new[] { -42f, 0f, 42f, 0f, -42f });
            SetEulerX(clip, "Hips/Spine/Chest/RightShoulder/RightUpperArm", t, new[] { 42f, 0f, -42f, 0f, 42f });
            SetEulerX(clip, "Hips/LeftUpperLeg/LeftLowerLeg", t, new[] { 10f, 55f, 15f, 5f, 10f });
            SetEulerX(clip, "Hips/RightUpperLeg/RightLowerLeg", t, new[] { 15f, 5f, 10f, 55f, 15f });
            SetEulerX(clip, "Hips/Spine", t, new[] { 7f, 10f, 7f, 10f, 7f });
            SetLocalY(clip, "Hips", t, new[] { 0.96f, 0.99f, 0.96f, 0.99f, 0.96f });

            SaveClip(clip, RunPath);
            return clip;
        }

        private static AnimationClip CreateJumpClip()
        {
            AnimationClip clip = NewOrReplaceClip(JumpPath, "AN_Player_Jump", false);

            float[] t = { 0f, 0.18f, 0.45f, 0.75f, 1f };
            SetEulerX(clip, "Hips/LeftUpperLeg", t, new[] { 8f, 28f, 34f, 18f, 4f });
            SetEulerX(clip, "Hips/RightUpperLeg", t, new[] { 8f, 20f, 30f, 14f, 4f });
            SetEulerX(clip, "Hips/LeftUpperLeg/LeftLowerLeg", t, new[] { 15f, 48f, 62f, 36f, 8f });
            SetEulerX(clip, "Hips/RightUpperLeg/RightLowerLeg", t, new[] { 15f, 42f, 58f, 34f, 8f });
            SetEulerX(clip, "Hips/Spine/Chest/LeftShoulder/LeftUpperArm", t, new[] { 0f, -24f, -48f, -20f, 0f });
            SetEulerX(clip, "Hips/Spine/Chest/RightShoulder/RightUpperArm", t, new[] { 0f, -24f, -48f, -20f, 0f });
            SetEulerX(clip, "Hips/Spine", t, new[] { 0f, -4f, -8f, 2f, 0f });

            SaveClip(clip, JumpPath);
            return clip;
        }

        private static AnimationClip NewOrReplaceClip(string path, string name, bool loop)
        {
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null)
                AssetDatabase.DeleteAsset(path);

            AnimationClip clip = new AnimationClip
            {
                name = name,
                frameRate = 60f,
                legacy = false
            };

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        private static void SaveClip(AnimationClip clip, string path)
        {
            AssetDatabase.CreateAsset(clip, path);
            EditorUtility.SetDirty(clip);
        }

        private static void SetEulerX(AnimationClip clip, string path, float[] times, float[] degrees)
        {
            AnimationCurve curve = new AnimationCurve();
            for (int i = 0; i < times.Length; i++)
                curve.AddKey(times[i], degrees[i]);

            EditorCurveBinding binding = EditorCurveBinding.FloatCurve(
                path,
                typeof(Transform),
                "localEulerAnglesRaw.x");

            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        private static void SetLocalY(AnimationClip clip, string path, float[] times, float[] values)
        {
            AnimationCurve curve = new AnimationCurve();
            for (int i = 0; i < times.Length; i++)
                curve.AddKey(times[i], values[i]);

            EditorCurveBinding binding = EditorCurveBinding.FloatCurve(
                path,
                typeof(Transform),
                "m_LocalPosition.y");

            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        private static AnimatorController CreateAnimatorController(
            AnimationClip idle,
            AnimationClip walk,
            AnimationClip run,
            AnimationClip jump)
        {
            AssetDatabase.DeleteAsset(ControllerPath);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
            controller.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState idleState = sm.AddState("Idle");
            AnimatorState walkState = sm.AddState("Walk");
            AnimatorState runState = sm.AddState("Run");
            AnimatorState jumpState = sm.AddState("Jump");

            idleState.motion = idle;
            walkState.motion = walk;
            runState.motion = run;
            jumpState.motion = jump;
            sm.defaultState = idleState;

            AddTransition(idleState, walkState, 0.12f,
                new AnimatorCondition(AnimatorConditionMode.Greater, 0.10f, "Speed"),
                new AnimatorCondition(AnimatorConditionMode.IfNot, 0f, "IsRunning"),
                new AnimatorCondition(AnimatorConditionMode.If, 0f, "Grounded"));

            AddTransition(walkState, idleState, 0.12f,
                new AnimatorCondition(AnimatorConditionMode.Less, 0.10f, "Speed"));

            AddTransition(walkState, runState, 0.10f,
                new AnimatorCondition(AnimatorConditionMode.If, 0f, "IsRunning"));

            AddTransition(runState, walkState, 0.10f,
                new AnimatorCondition(AnimatorConditionMode.IfNot, 0f, "IsRunning"),
                new AnimatorCondition(AnimatorConditionMode.Greater, 0.10f, "Speed"));

            AddTransition(runState, idleState, 0.10f,
                new AnimatorCondition(AnimatorConditionMode.Less, 0.10f, "Speed"));

            AnimatorStateTransition toJump = sm.AddAnyStateTransition(jumpState);
            toJump.hasExitTime = false;
            toJump.duration = 0.06f;
            toJump.canTransitionToSelf = false;
            toJump.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");

            AddTransition(jumpState, idleState, 0.08f,
                new AnimatorCondition(AnimatorConditionMode.If, 0f, "Grounded"),
                new AnimatorCondition(AnimatorConditionMode.Less, 0.10f, "Speed"));

            AddTransition(jumpState, walkState, 0.08f,
                new AnimatorCondition(AnimatorConditionMode.If, 0f, "Grounded"),
                new AnimatorCondition(AnimatorConditionMode.Greater, 0.10f, "Speed"),
                new AnimatorCondition(AnimatorConditionMode.IfNot, 0f, "IsRunning"));

            AddTransition(jumpState, runState, 0.08f,
                new AnimatorCondition(AnimatorConditionMode.If, 0f, "Grounded"),
                new AnimatorCondition(AnimatorConditionMode.If, 0f, "IsRunning"));

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void AddTransition(
            AnimatorState from,
            AnimatorState to,
            float duration,
            params AnimatorCondition[] conditions)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = duration;

            foreach (AnimatorCondition condition in conditions)
                transition.AddCondition(condition.mode, condition.threshold, condition.parameter);
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }

        private struct RigBones
        {
            public Transform Head;
            public Transform LeftHand;
            public Transform RightHand;
        }
    }
}
