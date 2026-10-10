using UnityEngine;

namespace DeadSector
{
    // Procedural prototype pose controller until Humanoid/Mixamo animation
    // controllers are installed. Weapon type affects relaxed arm/torso pose.
    public enum SectorCarryPose { Unarmed, Tool, Pistol, Rifle, Infected }

    public sealed class SectorMannequin : MonoBehaviour
    {
        public Transform leftArm, rightArm, leftLeg, rightLeg, torso;
        float speed, phase, airborne, lean, poseWeight;
        SectorCarryPose carryPose;
        float crouchBlend;
        bool crouching;
        bool basePoseCaptured;
        Vector3 torsoRest, leftLegRest, rightLegRest;

        // Procedural hips/head must descend as well as rotating joints.
        // Leg pivots descend less: bending the legs already raises the boots.
        public const float CrouchTorsoDrop = .56f;
        public const float CrouchLegDrop = .14f;

        public void SetCrouching(bool value) => crouching = value;

        void CaptureBasePose()
        {
            if (basePoseCaptured || torso == null ||
                leftLeg == null || rightLeg == null)
                return;
            torsoRest = torso.localPosition;
            leftLegRest = leftLeg.localPosition;
            rightLegRest = rightLeg.localPosition;
            basePoseCaptured = true;
        }

        public SectorCarryPose CarryPose => carryPose;

        public void SetCarryPose(SectorCarryPose pose)
        {
            carryPose = pose;
        }

        public static Vector3 ArmAngles(SectorCarryPose pose, bool right)
        {
            switch (pose)
            {
                case SectorCarryPose.Tool:
                    // Tool stays low at the thigh, wrist inward.
                    return right ? new Vector3(-17f, -7f, -18f)
                        : new Vector3(5f, 0f, 9f);
                case SectorCarryPose.Pistol:
                    return right ? new Vector3(-49f, -12f, -8f)
                        : new Vector3(-17f, 14f, 18f);
                case SectorCarryPose.Rifle:
                    return right ? new Vector3(-62f, -21f, -16f)
                        : new Vector3(-51f, 18f, 26f);
                case SectorCarryPose.Infected:
                    // Arms reaching out, asymmetric shoulders, no T-pose.
                    return right ? new Vector3(-57f, -13f, -13f)
                        : new Vector3(-48f, 16f, 20f);
                default:
                    // Natural relaxed posture, not straight out like a T-pose.
                    return right ? new Vector3(9f, 0f, 12f)
                        : new Vector3(9f, 0f, -12f);
            }
        }

        public void SetMotion(float velocity, bool grounded, float vertical)
        {
            speed = Mathf.Lerp(speed, velocity,
                1f - Mathf.Exp(-12f * Time.deltaTime));
            airborne = Mathf.MoveTowards(
                airborne, grounded ? 0f : 1f, Time.deltaTime * 8f);
            lean = Mathf.Lerp(lean, vertical > 0f ? -8f : 8f,
                1f - Mathf.Exp(-6f * Time.deltaTime));
        }

        void LateUpdate()
        {
            if (leftArm == null || rightArm == null || leftLeg == null ||
                rightLeg == null || torso == null)
                return;

            CaptureBasePose();
            float normalized = Mathf.Clamp01(speed / 7f);
            float dt = Time.deltaTime;
            crouchBlend = Mathf.MoveTowards(crouchBlend,
                crouching ? 1f : 0f, dt * 5f);
            float poseDamping = 1f - Mathf.Exp(-12f * dt);
            torso.localPosition = Vector3.Lerp(
                torso.localPosition,
                torsoRest + new Vector3(0f, -CrouchTorsoDrop * crouchBlend,
                    .085f * crouchBlend), poseDamping);
            leftLeg.localPosition = Vector3.Lerp(
                leftLeg.localPosition,
                leftLegRest + Vector3.down * CrouchLegDrop * crouchBlend,
                poseDamping);
            rightLeg.localPosition = Vector3.Lerp(
                rightLeg.localPosition,
                rightLegRest + Vector3.down * CrouchLegDrop * crouchBlend,
                poseDamping);
            phase += dt * Mathf.Lerp(0.75f, 12f, normalized);
            float stride = Mathf.Sin(phase) * 48f * normalized *
                (1f - airborne);
            float breathe = Mathf.Sin(Time.time * 1.35f) * 1.15f;

            leftLeg.localRotation = Quaternion.Lerp(
                leftLeg.localRotation,
                Quaternion.Euler(stride - airborne * 32f -
                    crouchBlend * 31f, 0f, 0f),
                1f - Mathf.Exp(-16f * dt));
            rightLeg.localRotation = Quaternion.Lerp(
                rightLeg.localRotation,
                Quaternion.Euler(-stride - airborne * 12f -
                    crouchBlend * 31f, 0f, 0f),
                1f - Mathf.Exp(-16f * dt));

            // Hands are attached under the right arm, so their weapon
            // transforms follow the changing carry posture automatically.
            Vector3 leftBase = ArmAngles(carryPose, false);
            Vector3 rightBase = ArmAngles(carryPose, true);
            float armSwing = carryPose == SectorCarryPose.Rifle
                ? .25f : carryPose == SectorCarryPose.Pistol ? .4f : 1f;
            leftArm.localRotation = Quaternion.Lerp(
                leftArm.localRotation,
                Quaternion.Euler(leftBase.x - stride * armSwing -
                    airborne * 38f + breathe, leftBase.y, leftBase.z),
                1f - Mathf.Exp(-13f * dt));
            rightArm.localRotation = Quaternion.Lerp(
                rightArm.localRotation,
                Quaternion.Euler(rightBase.x + stride * armSwing -
                    airborne * 38f + breathe, rightBase.y, rightBase.z),
                1f - Mathf.Exp(-13f * dt));
            torso.localRotation = Quaternion.Lerp(
                torso.localRotation,
                Quaternion.Euler(
                    Mathf.Lerp(speed * 1.1f, lean, airborne) +
                    breathe * .3f + crouchBlend * 21f,
                    carryPose == SectorCarryPose.Rifle ? -5f : 0f, 0f),
                1f - Mathf.Exp(-9f * dt));
        }
    }
}
