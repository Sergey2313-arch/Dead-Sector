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
        public void ZombieMeleeCannotDamagePlayerThroughClosedWall()
        {
            var attacker = new GameObject("AttackingZombie");
            var defender = new GameObject("PlayerBehindWall");
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);

            try
            {
                attacker.transform.position = Vector3.zero;
                defender.transform.position = new Vector3(0f, 0f, 2f);
                wall.transform.position = new Vector3(0f, 1f, 1f);
                wall.transform.localScale =
                    new Vector3(2f, 2.4f, .2f);
                Physics.SyncTransforms();

                Assert.IsTrue(SectorCombatVisibility.HasBlockingGeometry(
                    Vector3.up, defender.transform.position + Vector3.up,
                    attacker.transform, defender.transform));

                wall.SetActive(false);
                Physics.SyncTransforms();

                Assert.IsFalse(SectorCombatVisibility.HasBlockingGeometry(
                    Vector3.up, defender.transform.position + Vector3.up,
                    attacker.transform, defender.transform));
            }
            finally
            {
                Object.DestroyImmediate(wall);
                Object.DestroyImmediate(attacker);
                Object.DestroyImmediate(defender);
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
