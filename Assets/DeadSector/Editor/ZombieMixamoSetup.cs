using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DeadSector.Editor
{
    public static class ZombieMixamoSetup
    {
        const string ZombieRoot = "Assets/DeadSector/Characters/Zombie";
        const string ZombieMixamo = ZombieRoot + "/Mixamo";
        const string ZombieControllers = ZombieRoot + "/Controllers";
        const string PlayerMixamo = "Assets/DeadSector/Characters/Player/Mixamo";
        const string ControllerPath = ZombieControllers + "/AC_Zombie_Mixamo.controller";
        const string ResourcesRoot = "Assets/Resources";
        const string ResourcesDeadSector = ResourcesRoot + "/DeadSector";
        const string PrefabPath = ResourcesDeadSector + "/Zombie_XBot.prefab";

        [MenuItem("Dead Sector/Character/Zombies/00 - BUILD ZOMBIES")]
        public static void BuildEverything()
        {
            EnsureFolders();
            ConfigureZombieAnimations();
            BuildAnimator();
            BuildZombiePrefab();
        }

        [MenuItem("Dead Sector/Character/Zombies/01 - Configure Zombie FBX")]
        public static void ConfigureZombieAnimations()
        {
            EnsureFolders();

            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { ZombieMixamo });

            if (guids.Length == 0)
            {
                Debug.LogWarning(
                    "[Dead Sector] No zombie FBX files found in " + ZombieMixamo +
                    ". Put the zombie animation FBXs there, then run BUILD ZOMBIES again.");
                return;
            }

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
                    continue;

                string file = Path.GetFileNameWithoutExtension(path);

                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = true;

                ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
                if (clips != null && clips.Length > 0)
                {
                    bool loop = ShouldLoop(file);

                    foreach (ModelImporterClipAnimation clip in clips)
                    {
                        clip.loopTime = loop;
                        clip.loopPose = loop;
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
                Debug.Log("[Dead Sector] Zombie Humanoid configured: " + path);
            }

            AssetDatabase.Refresh();
        }

        [MenuItem("Dead Sector/Character/Zombies/02 - Build Zombie Animator")]
        public static void BuildAnimator()
        {
            EnsureFolders();

            AnimationClip idle = FindClip("Zombie Idle", "Zombie Agonizing");
            AnimationClip walk = FindClip("Zombie Walk", "Zombie Run");
            AnimationClip run = FindClip("Zombie Run");
            AnimationClip standUp = FindClip("Zombie Stand Up");
            AnimationClip scream = FindClip("Zombie Scream");
            AnimationClip bite = FindClip("Zombie Biting");
            AnimationClip punch = FindClip("Zombie Punching");
            AnimationClip kick = FindClip("Zombie Kicking");
            AnimationClip crawl = FindClip("Zombie Crawl");

            if (run == null)
            {
                Debug.LogError(
                    "[Dead Sector] Zombie Run.fbx is required to build the zombie controller.");
                return;
            }

            if (idle == null)
            {
                Debug.LogWarning(
                    "[Dead Sector] Zombie Idle.fbx is missing. Add it later for a better idle. " +
                    "The controller will temporarily use Zombie Run at very low speed.");
                idle = run;
            }

            if (walk == null)
                walk = run;

            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            AddParameter(controller, "MoveSpeed", AnimatorControllerParameterType.Float);
            AddParameter(controller, "Aggro", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
            AddParameter(controller, "AttackVariant", AnimatorControllerParameterType.Int);
            AddParameter(controller, "Scream", AnimatorControllerParameterType.Trigger);
            AddParameter(controller, "Crawl", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            sm.name = "Zombie Base";

            AnimatorState idleState = sm.AddState("Idle");
            idleState.motion = idle;
            idleState.speed = idle == run ? .12f : 1f;
            sm.defaultState = idleState;

            AnimatorState walkState = sm.AddState("Walk");
            walkState.motion = walk;
            walkState.speed = walk == run ? .48f : 1f;

            AnimatorState runState = sm.AddState("Run");
            runState.motion = run;

            AddTransition(
                idleState,
                walkState,
                .18f,
                false,
                C(AnimatorConditionMode.Greater, .08f, "MoveSpeed"),
                C(AnimatorConditionMode.Less, 2.2f, "MoveSpeed"));

            AddTransition(
                walkState,
                idleState,
                .18f,
                false,
                C(AnimatorConditionMode.Less, .08f, "MoveSpeed"));

            AddTransition(
                walkState,
                runState,
                .12f,
                false,
                C(AnimatorConditionMode.Greater, 2.2f, "MoveSpeed"));

            AddTransition(
                runState,
                walkState,
                .12f,
                false,
                C(AnimatorConditionMode.Less, 2.2f, "MoveSpeed"),
                C(AnimatorConditionMode.Greater, .08f, "MoveSpeed"));

            AddTransition(
                runState,
                idleState,
                .15f,
                false,
                C(AnimatorConditionMode.Less, .08f, "MoveSpeed"));

            if (standUp != null)
            {
                AnimatorState state = sm.AddState("Stand Up");
                state.motion = standUp;

                AnimatorStateTransition exit = state.AddTransition(idleState);
                exit.hasExitTime = true;
                exit.exitTime = .95f;
                exit.duration = .12f;
            }

            if (scream != null)
            {
                AnimatorState state = sm.AddState("Scream");
                state.motion = scream;

                AnimatorStateTransition enter = sm.AddAnyStateTransition(state);
                enter.hasExitTime = false;
                enter.duration = .08f;
                enter.canTransitionToSelf = false;
                enter.AddCondition(AnimatorConditionMode.If, 0f, "Scream");

                ExitToLocomotion(state, idleState, walkState, runState);
            }

            AddAttack(sm, idleState, walkState, runState, bite, "Bite", 0);
            AddAttack(sm, idleState, walkState, runState, punch, "Punch", 1);
            AddAttack(sm, idleState, walkState, runState, kick, "Kick", 2);

            if (crawl != null)
            {
                AnimatorState crawlState = sm.AddState("Crawl");
                crawlState.motion = crawl;

                AnimatorStateTransition enter = sm.AddAnyStateTransition(crawlState);
                enter.hasExitTime = false;
                enter.duration = .12f;
                enter.canTransitionToSelf = false;
                enter.AddCondition(AnimatorConditionMode.If, 0f, "Crawl");

                AnimatorStateTransition exit = crawlState.AddTransition(idleState);
                exit.hasExitTime = false;
                exit.duration = .15f;
                exit.AddCondition(AnimatorConditionMode.IfNot, 0f, "Crawl");
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "[Dead Sector] Zombie Animator built. " +
                "Idle=" + NameOf(idle) +
                ", Walk=" + NameOf(walk) +
                ", Run=" + NameOf(run) +
                ", Bite=" + NameOf(bite) +
                ", Punch=" + NameOf(punch) +
                ", Kick=" + NameOf(kick) +
                ", Scream=" + NameOf(scream) +
                ", Crawl=" + NameOf(crawl));
        }

        [MenuItem("Dead Sector/Character/Zombies/03 - Build X Bot Zombie Prefab")]
        public static void BuildZombiePrefab()
        {
            EnsureFolders();

            RuntimeAnimatorController controller =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);

            if (controller == null)
            {
                BuildAnimator();
                controller =
                    AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            }

            if (controller == null)
                return;

            string xBotPath = FindXBot();
            if (string.IsNullOrEmpty(xBotPath))
            {
                Debug.LogError(
                    "[Dead Sector] X Bot.fbx (With Skin) was not found in " +
                    PlayerMixamo + ". Build the player model first.");
                return;
            }

            GameObject modelAsset =
                AssetDatabase.LoadAssetAtPath<GameObject>(xBotPath);

            Avatar avatar = AssetDatabase
                .LoadAllAssetsAtPath(xBotPath)
                .OfType<Avatar>()
                .FirstOrDefault(a => a != null && a.isHuman);

            if (modelAsset == null || avatar == null)
            {
                Debug.LogError(
                    "[Dead Sector] X Bot is missing a valid Humanoid avatar: " + xBotPath);
                return;
            }

            GameObject root = new GameObject("Zombie_XBot");
            GameObject body = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;

            if (body == null)
            {
                UnityEngine.Object.DestroyImmediate(root);
                Debug.LogError("[Dead Sector] Could not instantiate X Bot for zombie.");
                return;
            }

            body.name = "FullBody";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;

            Animator animator = body.GetComponentInChildren<Animator>();
            if (animator == null)
                animator = body.AddComponent<Animator>();

            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // Zombies use the exact same mannequin body as the player.
            // Runtime AI only changes animation and behaviour.
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Dead Sector] X Bot zombie prefab created: " + PrefabPath +
                ". Runtime AI will now use these full-body mannequins.");
        }

        static void AddAttack(
            AnimatorStateMachine sm,
            AnimatorState idle,
            AnimatorState walk,
            AnimatorState run,
            AnimationClip clip,
            string stateName,
            int variant)
        {
            if (clip == null)
                return;

            AnimatorState attack = sm.AddState(stateName);
            attack.motion = clip;

            AnimatorStateTransition enter = sm.AddAnyStateTransition(attack);
            enter.hasExitTime = false;
            enter.duration = .06f;
            enter.canTransitionToSelf = false;
            enter.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            enter.AddCondition(AnimatorConditionMode.Equals, variant, "AttackVariant");

            ExitToLocomotion(attack, idle, walk, run);
        }

        static void ExitToLocomotion(
            AnimatorState source,
            AnimatorState idle,
            AnimatorState walk,
            AnimatorState run)
        {
            AnimatorStateTransition toIdle = source.AddTransition(idle);
            toIdle.hasExitTime = true;
            toIdle.exitTime = .92f;
            toIdle.duration = .10f;
            toIdle.AddCondition(AnimatorConditionMode.Less, .08f, "MoveSpeed");

            AnimatorStateTransition toWalk = source.AddTransition(walk);
            toWalk.hasExitTime = true;
            toWalk.exitTime = .90f;
            toWalk.duration = .10f;
            toWalk.AddCondition(AnimatorConditionMode.Greater, .08f, "MoveSpeed");
            toWalk.AddCondition(AnimatorConditionMode.Less, 2.2f, "MoveSpeed");

            AnimatorStateTransition toRun = source.AddTransition(run);
            toRun.hasExitTime = true;
            toRun.exitTime = .90f;
            toRun.duration = .10f;
            toRun.AddCondition(AnimatorConditionMode.Greater, 2.2f, "MoveSpeed");
        }

        static AnimationClip FindClip(params string[] names)
        {
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { ZombieMixamo });

            foreach (string wanted in names)
            {
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    string file = Path.GetFileNameWithoutExtension(path);

                    if (!file.Equals(wanted, StringComparison.OrdinalIgnoreCase))
                        continue;

                    AnimationClip clip = AssetDatabase
                        .LoadAllAssetsAtPath(path)
                        .OfType<AnimationClip>()
                        .FirstOrDefault(c =>
                            !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase));

                    if (clip != null)
                        return clip;
                }
            }

            foreach (string wanted in names)
            {
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    string file = Path.GetFileNameWithoutExtension(path);

                    if (!file.Contains(wanted, StringComparison.OrdinalIgnoreCase))
                        continue;

                    AnimationClip clip = AssetDatabase
                        .LoadAllAssetsAtPath(path)
                        .OfType<AnimationClip>()
                        .FirstOrDefault(c =>
                            !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase));

                    if (clip != null)
                        return clip;
                }
            }

            return null;
        }

        static string FindXBot()
        {
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { PlayerMixamo });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = Path.GetFileNameWithoutExtension(path);

                if (file.Equals("X Bot", StringComparison.OrdinalIgnoreCase))
                    return path;
            }

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = Path.GetFileNameWithoutExtension(path);

                if (file.Contains("X Bot", StringComparison.OrdinalIgnoreCase))
                    return path;
            }

            return null;
        }

        static bool ShouldLoop(string file)
        {
            return
                file.Contains("Idle", StringComparison.OrdinalIgnoreCase) ||
                file.Contains("Walk", StringComparison.OrdinalIgnoreCase) ||
                file.Contains("Run", StringComparison.OrdinalIgnoreCase) ||
                file.Contains("Crawl", StringComparison.OrdinalIgnoreCase) ||
                file.Contains("Agonizing", StringComparison.OrdinalIgnoreCase);
        }

        static void AddParameter(
            AnimatorController controller,
            string name,
            AnimatorControllerParameterType type)
        {
            if (!controller.parameters.Any(p => p.name == name))
                controller.AddParameter(name, type);
        }

        static void AddTransition(
            AnimatorState from,
            AnimatorState to,
            float duration,
            bool exitTime,
            params ConditionSpec[] conditions)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = exitTime;
            transition.duration = duration;

            foreach (ConditionSpec condition in conditions)
                transition.AddCondition(
                    condition.mode,
                    condition.threshold,
                    condition.parameter);
        }

        static ConditionSpec C(
            AnimatorConditionMode mode,
            float threshold,
            string parameter)
        {
            return new ConditionSpec
            {
                mode = mode,
                threshold = threshold,
                parameter = parameter
            };
        }

        static string NameOf(AnimationClip clip)
        {
            return clip == null ? "missing" : clip.name;
        }

        static void EnsureFolders()
        {
            EnsureFolder("Assets", "DeadSector");
            EnsureFolder("Assets/DeadSector", "Characters");
            EnsureFolder("Assets/DeadSector/Characters", "Zombie");
            EnsureFolder(ZombieRoot, "Mixamo");
            EnsureFolder(ZombieRoot, "Controllers");
            EnsureFolder("Assets", "Resources");
            EnsureFolder(ResourcesRoot, "DeadSector");
        }

        static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }

        struct ConditionSpec
        {
            public AnimatorConditionMode mode;
            public float threshold;
            public string parameter;
        }
    }
}
