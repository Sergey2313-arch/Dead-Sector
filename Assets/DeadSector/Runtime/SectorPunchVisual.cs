using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// Lightweight prototype punch motion over the humanoid arm skeleton.
    /// Actual production clips can replace it later. Damage is immediate;
    /// the brief visible arm motion is feedback, never an attack wind-up.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SectorPunchVisual : MonoBehaviour
    {
        SectorPlayer player;
        Animator animator;
        Transform leftArm;
        Transform rightArm;
        Quaternion leftRest;
        Quaternion rightRest;

        float startedAt = -100f;
        float duration;
        bool heavy;
        bool meleeSwing;
        bool useLeft;
        bool proceduralPoseApplied;
        int jabCounter;

        public void Configure(SectorPlayer target)
        {
            player = target;
            animator = target != null && target.visual != null
                ? target.visual.GetComponentInChildren<Animator>(true)
                : null;

            if (animator != null && animator.isHuman)
            {
                leftArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                rightArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            }
            else if (target != null && target.visual != null)
            {
                SectorMannequin mannequin =
                    target.visual.GetComponent<SectorMannequin>();
                if (mannequin != null)
                {
                    leftArm = mannequin.leftArm;
                    rightArm = mannequin.rightArm;
                }
            }

            if (leftArm != null) leftRest = leftArm.localRotation;
            if (rightArm != null) rightRest = rightArm.localRotation;
        }

        public void Play(bool strong, bool weaponEquipped = false)
        {
            if (player == null)
                return;

            if (animator == null || !animator.isActiveAndEnabled)
                RestoreFallbackPose();

            heavy = strong;
            meleeSwing = weaponEquipped;
            useLeft = !strong && !weaponEquipped &&
                (++jabCounter % 2 == 0);
            startedAt = Time.time;
            duration = meleeSwing
                ? (strong ? .43f : .28f)
                : (strong ? .32f : .17f);

            // Input already applied combat damage this frame.
            // This function only adds a quick animation on the avatar.
        }

        void LateUpdate()
        {
            if (Time.time >= startedAt + duration || duration <= 0f)
            {
                if (proceduralPoseApplied)
                    RestoreFallbackPose();
                return;
            }

            Transform arm = useLeft ? leftArm : rightArm;
            if (arm == null)
                return;

            float t = Mathf.Clamp01((Time.time - startedAt) / duration);
            float extension = Mathf.Sin(t * Mathf.PI);
            float magnitude = heavy ? 85f : 59f;
            float side = useLeft ? -1f : 1f;

            // With an Animator, the bone is reposed every frame before
            // LateUpdate. Without Animator, apply from a fixed rest pose.
            bool animated = animator != null && animator.isActiveAndEnabled;
            if (!animated)
                proceduralPoseApplied = true;

            Quaternion basePose = animated
                ? arm.localRotation
                : useLeft ? leftRest : rightRest;

            // Weapon swings trace a sweeping arc; empty-hand attacks
            // remain short straight jabs. Neither delays damage.
            Quaternion arc = meleeSwing
                ? Quaternion.Euler(
                    -(heavy ? 93f : 58f) * extension,
                    side * (heavy ? 73f : 46f) * extension,
                    side * 22f * extension)
                : Quaternion.Euler(
                    -magnitude * extension,
                    side * (heavy ? 14f : 9f) * extension,
                    side * 8f * extension);

            arm.localRotation = basePose * arc;
        }

        void RestoreFallbackPose()
        {
            if (!proceduralPoseApplied)
                return;

            if (leftArm != null)
                leftArm.localRotation = leftRest;
            if (rightArm != null)
                rightArm.localRotation = rightRest;

            proceduralPoseApplied = false;
        }

        void OnDisable()
        {
            if (animator == null || !animator.isActiveAndEnabled)
                RestoreFallbackPose();
        }
    }
}
