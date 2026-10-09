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
        // Normal shots avoid per-call RaycastAll allocations. Overflow uses the
        // allocating path to preserve correctness in unusually dense geometry.
        private static readonly RaycastHit[] HitBuffer = new RaycastHit[64];
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
            int hitCount = Physics.RaycastNonAlloc(
                origin, delta / distance, HitBuffer, distance, ~(1 << 2),
                QueryTriggerInteraction.Ignore);

            if (hitCount == HitBuffer.Length)
            {
                // A full buffer may have omitted the blocking collider.
                // Use the complete query rather than allowing wall penetration.
                RaycastHit[] allHits = Physics.RaycastAll(
                    origin, delta / distance, distance, ~(1 << 2),
                    QueryTriggerInteraction.Ignore);
                foreach (RaycastHit hit in allHits)
                    if (Blocks(hit, sourceRoot, intendedTargetRoot))
                        return true;
                return false;
            }

            for (int i = 0; i < hitCount; i++)
                if (Blocks(HitBuffer[i], sourceRoot, intendedTargetRoot))
                    return true;

            return false;
        }

        private static bool Blocks(
            RaycastHit hit, Transform sourceRoot, Transform intendedTargetRoot)
        {
            Collider obstacle = hit.collider;
            if (obstacle == null || !obstacle.enabled ||
                hit.distance <= .015f)
                return false;

            Transform colliderRoot = obstacle.transform;
            if (sourceRoot != null && colliderRoot.IsChildOf(sourceRoot))
                return false;
            if (colliderRoot.IsChildOf(intendedTargetRoot))
                return false;

            return true;
        }
    }
}
