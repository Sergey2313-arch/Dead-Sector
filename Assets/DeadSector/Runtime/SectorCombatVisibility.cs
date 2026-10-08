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
            Vector3 delta = hitPoint - origin;
            float distance = delta.magnitude;

            if (distance < .05f || intendedTarget == null)
                return true;

            Vector3 direction = delta / distance;

            // The actor layer (2) is reserved for player visuals; ignore
            // it to avoid self-hits. Environment and enemy colliders remain.
            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                direction,
                distance,
                ~(1 << 2),
                QueryTriggerInteraction.Ignore);

            foreach (RaycastHit hit in hits)
            {
                Collider obstacle = hit.collider;
                if (obstacle == null || !obstacle.enabled)
                    continue;

                // The collider attached to our target is expected.
                SectorZombie hitZombie =
                    obstacle.GetComponentInParent<SectorZombie>();
                if (hitZombie == intendedTarget)
                    continue;

                // Do not consider near-zero contact with our own weapon
                // or floating-point overlaps to be world obstruction.
                if (hit.distance <= .015f)
                    continue;

                // A different zombie or any solid world obstacle blocks.
                return true;
            }

            return false;
        }
    }
}
