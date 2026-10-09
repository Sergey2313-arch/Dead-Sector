using UnityEditor;
using UnityEngine;
using DeadSector.Runtime.Player;

namespace DeadSector.Editor.Player
{
    /// <summary>Non-destructive validation of the generated full-body player prefab.</summary>
    public static class PlayerPrefabAudit
    {
        private const string PrefabPath =
            "Assets/DeadSector/Characters/Player/Prefabs/Player_Mixamo_FullBody.prefab";

        [MenuItem("Dead Sector/Audit/Validate Player Prefab")]
        public static void ValidatePlayerPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[Dead Sector Audit] Player prefab missing: " + PrefabPath);
                return;
            }

            int errors = 0;
            PlayerMovement movement = prefab.GetComponent<PlayerMovement>();
            CharacterController capsule = prefab.GetComponent<CharacterController>();
            Animator animator = prefab.GetComponentInChildren<Animator>(true);
            Camera camera = prefab.GetComponentInChildren<Camera>(true);

            if (movement == null)
            {
                Debug.LogError("[Dead Sector Audit] Missing PlayerMovement.", prefab);
                errors++;
            }

            if (capsule == null || capsule.height <= 0f || capsule.radius <= 0f)
            {
                Debug.LogError("[Dead Sector Audit] Missing or invalid CharacterController.", prefab);
                errors++;
            }
            else if (capsule.stepOffset >= capsule.height)
            {
                Debug.LogError("[Dead Sector Audit] CharacterController stepOffset exceeds height.", prefab);
                errors++;
            }

            if (animator == null || !animator.isHuman || !animator.avatar ||
                !animator.avatar.isValid || animator.runtimeAnimatorController == null)
            {
                Debug.LogError("[Dead Sector Audit] Humanoid animator/avatar/controller missing or invalid.", prefab);
                errors++;
            }
            else if (animator.applyRootMotion)
            {
                Debug.LogError("[Dead Sector Audit] Root Motion must be disabled when CharacterController drives movement.", prefab);
                errors++;
            }

            if (camera == null)
            {
                Debug.LogError("[Dead Sector Audit] Player camera missing.", prefab);
                errors++;
            }

            if (movement != null)
            {
                if (movement.animator != animator || movement.playerCamera != camera)
                {
                    Debug.LogError("[Dead Sector Audit] PlayerMovement references do not match player children.", prefab);
                    errors++;
                }
                if (movement.walkSpeed <= 0f || movement.runSpeed < movement.walkSpeed ||
                    movement.gravity >= 0f || movement.jumpHeight <= 0f)
                {
                    Debug.LogError("[Dead Sector Audit] Invalid movement tuning.", prefab);
                    errors++;
                }
            }

            if (errors == 0)
                Debug.Log("[Dead Sector Audit] Player prefab passed structural validation. Play Mode physics and animation checks are still required.", prefab);
            else
                Debug.LogError("[Dead Sector Audit] Player prefab failed: " + errors + " structural issue(s).", prefab);
        }
    }
}
