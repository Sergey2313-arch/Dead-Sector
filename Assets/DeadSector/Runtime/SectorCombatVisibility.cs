using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// Stops hits through buildings, terrain, and other living infected.
    /// A target's OWN capsule or skinned mesh collider is the intended
    /// impact, not a wall blocking that same target.
    /// </summary>
    public static class SectorCombatVisibility
    {
        public static bool IsObstructed(
            Vector3 origin, Vector3 hitPoint, SectorZombie intendedTarget)
        {
            if (intendedTarget == null)
                return true;

            return HasBlockingGeometry(
                origin, hitPoint, null, intendedTarget.transform);
        }

        /// <summary>
        /// Reused by zombie bites: owner and intended victim colliders
        /// are not scenery, whereas walls, closed doors and other
        /// physical obstructions prevent contact damage.
        /// </summary>
        public static bool HasBlockingGeometry(
            Vector3 origin, Vector3 hitPoint,
            Transform sourceRoot, Transform intendedTargetRoot)
        {
            if (intendedTargetRoot == null)
                return true;

            Vector3 delta = hitPoint - origin;
            float distance = delta.magnitude;
            if (distance < .05f)
                return true;

            // Actor layer 2 is reserved for player visual elements and
            // excludes self camera/model mesh overlap. World props remain.
            RaycastHit[] hits = Physics.RaycastAll(
                origin, delta / distance, distance, ~(1 << 2),
                QueryTriggerInteraction.Ignore);

            foreach (RaycastHit hit in hits)
            {
                Collider obstacle = hit.collider;
                if (obstacle == null || !obstacle.enabled ||
                    hit.distance <= .015f)
                    continue;

                Transform colliderRoot = obstacle.transform;
                if (sourceRoot != null &&
                    colliderRoot.IsChildOf(sourceRoot))
                    continue;

                if (colliderRoot.IsChildOf(intendedTargetRoot))
                    continue;

                return true;
            }

            return false;
        }
    }
}
