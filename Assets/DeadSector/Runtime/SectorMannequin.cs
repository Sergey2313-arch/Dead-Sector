using UnityEngine;

namespace DeadSector
{
    // Articulated prototype rig. No third-party model or animation download required.
    public sealed class SectorMannequin : MonoBehaviour
    {
        public Transform leftArm, rightArm, leftLeg, rightLeg, torso;
        float speed, phase, airborne, lean;
        public void SetMotion(float velocity, bool grounded, float vertical)
        {
            speed = Mathf.Lerp(speed, velocity, 12f * Time.deltaTime);
            airborne = Mathf.MoveTowards(airborne, grounded ? 0 : 1, Time.deltaTime * 8f);
            lean = Mathf.Lerp(lean, vertical > 0 ? -8 : 8, Time.deltaTime * 6);
        }
        void LateUpdate()
        {
            phase += Time.deltaTime * Mathf.Lerp(0, 12, Mathf.Clamp01(speed / 7));
            float stride = Mathf.Sin(phase) * Mathf.Lerp(0, 48, Mathf.Clamp01(speed / 7)) * (1 - airborne);
            leftLeg.localRotation = Quaternion.Euler(stride - airborne * 32, 0, 0);
            rightLeg.localRotation = Quaternion.Euler(-stride - airborne * 12, 0, 0);
            leftArm.localRotation = Quaternion.Euler(-stride - airborne * 60, 0, -5 - airborne * 15);
            rightArm.localRotation = Quaternion.Euler(stride - airborne * 60, 0, 5 + airborne * 15);
            torso.localRotation = Quaternion.Euler(Mathf.Lerp(speed * 1.3f, lean, airborne), 0, 0);
        }
    }
}
