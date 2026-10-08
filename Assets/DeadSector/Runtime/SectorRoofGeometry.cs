using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// Consistent pitched roof rotation for both the starter settlement and
    /// distant world landmarks. The roof must slope DOWN away from the ridge.
    /// </summary>
    public static class SectorRoofGeometry
    {
        public static float PanelRotationZ(int side, float pitchDegrees)
        {
            return side < 0
                ? Mathf.Abs(pitchDegrees)
                : -Mathf.Abs(pitchDegrees);
        }

        public static float Rise(float width, float pitchDegrees)
        {
            return Mathf.Tan(Mathf.Abs(pitchDegrees) * Mathf.Deg2Rad)
                * width * .5f;
        }
    }
}
