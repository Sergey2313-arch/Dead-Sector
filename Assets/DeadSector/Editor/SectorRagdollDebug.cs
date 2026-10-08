using UnityEditor;
using UnityEngine;

namespace DeadSector.Editor
{
    /// <summary>
    /// Play Mode test: kill the nearest living infected with a known impulse
    /// so body physics can be checked before a full night horde.
    /// </summary>
    public static class SectorRagdollDebug
    {
        [MenuItem("Dead Sector/Debug/Test Nearest Zombie Ragdoll")]
        public static void TestNearest()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning(
                    "[Dead Sector] Start the playable scene in Play Mode first.");
                return;
            }

            SectorPlayer player =
                Object.FindFirstObjectByType<SectorPlayer>();
            if (player == null)
            {
                Debug.LogWarning(
                    "[Dead Sector] Ragdoll test requires a live player.");
                return;
            }

            SectorZombie closest = null;
            float distance = float.MaxValue;

            foreach (SectorZombie zombie in
                Object.FindObjectsByType<SectorZombie>(
                    FindObjectsSortMode.None))
            {
                if (zombie == null || zombie.Dead)
                    continue;

                float sqrDistance =
                    (zombie.transform.position -
                     player.transform.position).sqrMagnitude;

                if (sqrDistance < distance)
                {
                    closest = zombie;
                    distance = sqrDistance;
                }
            }

            if (closest == null)
            {
                Debug.LogWarning(
                    "[Dead Sector] No living infected exists yet. " +
                    "Wait for NavMesh/AI spawn or prepare a night horde.");
                return;
            }

            Vector3 direction = closest.transform.position -
                player.transform.position;
            direction.y = .20f;

            Vector3 point = closest.transform.position + Vector3.up * 1.2f;

            closest.TakeDamage(
                closest.Health + 1f,
                direction.normalized,
                point);

            SectorZombieRagdoll ragdoll =
                closest.GetComponent<SectorZombieRagdoll>();

            Debug.Log(
                "[Dead Sector] Forced kill of " + closest.name +
                ". Physical ragdoll active: " +
                (ragdoll != null && ragdoll.IsActive) +
                ". Physics bodies: " +
                (ragdoll != null ? ragdoll.ActiveBodyCount : 0) +
                ". Check Game and Scene windows for collision/limb stability.");
        }
    }
}
