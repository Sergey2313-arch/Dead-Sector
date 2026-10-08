using NUnit.Framework;
using UnityEngine;

namespace DeadSector.Tests
{
    public sealed class SectorCombatVisibilityTests
    {
        [Test]
        public void TargetColliderDoesNotCancelLegitimateAttack()
        {
            var targetObject = new GameObject("Target_In_Front");
            try
            {
                targetObject.transform.position = new Vector3(0f, 0f, 4f);
                var capsule = targetObject.AddComponent<CapsuleCollider>();
                capsule.center = Vector3.up;
                capsule.height = 1.8f;
                capsule.radius = .3f;
                SectorZombie zombie = targetObject.AddComponent<SectorZombie>();
                Physics.SyncTransforms();

                Assert.IsFalse(SectorCombatVisibility.IsObstructed(
                    new Vector3(0f, 1f, 0f),
                    new Vector3(0f, 1f, 4f), zombie),
                    "The intended victim's own CapsuleCollider is an impact.");
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void SolidWallBetweenPlayerAndZombieBlocksAttack()
        {
            var targetObject = new GameObject("Target_Behind_Wall");
            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                targetObject.transform.position = new Vector3(0f, 0f, 4f);
                var capsule = targetObject.AddComponent<CapsuleCollider>();
                capsule.center = Vector3.up;
                capsule.height = 1.8f;
                capsule.radius = .3f;
                SectorZombie zombie = targetObject.AddComponent<SectorZombie>();

                obstacle.name = "Wall_Obstacle";
                obstacle.transform.position = new Vector3(0f, 1f, 2f);
                obstacle.transform.localScale = new Vector3(2f, 2f, .4f);
                Physics.SyncTransforms();

                Assert.IsTrue(SectorCombatVisibility.IsObstructed(
                    new Vector3(0f, 1f, 0f),
                    new Vector3(0f, 1f, 4f), zombie));
            }
            finally
            {
                Object.DestroyImmediate(obstacle);
                Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void MissingTargetIsNeverReportedAsValidHit()
        {
            Assert.IsTrue(SectorCombatVisibility.IsObstructed(
                Vector3.zero, Vector3.forward, null));
        }
    }
}
