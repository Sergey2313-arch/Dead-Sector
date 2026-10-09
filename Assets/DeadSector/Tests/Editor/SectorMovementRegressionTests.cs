using NUnit.Framework;
using UnityEngine;

namespace DeadSector.Tests
{
    public sealed class SectorMovementRegressionTests
    {
        [Test]
        public void SprintDoesNotRapidlyRestartNearEmptyStamina()
        {
            var obj = new GameObject("SprintHysteresisTests");
            try
            {
                SectorSurvival needs = obj.AddComponent<SectorSurvival>();
                needs.stamina = 100f;
                Assert.IsTrue(needs.CanSprint);

                needs.stamina = 4f;
                Assert.IsFalse(needs.CanSprint,
                    "Below 5 stamina, sprint must be locked.");

                needs.stamina = 6f;
                Assert.IsFalse(needs.CanSprint,
                    "Slight stamina recovery must not restart running.");

                needs.stamina = 29f;
                Assert.IsFalse(needs.CanSprint,
                    "Shift must still produce steady walking while recovering.");

                needs.stamina = 30f;
                Assert.IsTrue(needs.CanSprint,
                    "Sprinting is allowed again after meaningful recovery.");

                needs.stamina = 4f;
                Assert.IsFalse(needs.CanSprint);

                needs.ApplySaved(0f, 0f, 12f);
                Assert.IsFalse(needs.CanSprint,
                    "Loading an exhausted character must not bypass lockout.");
            }
            finally
            {
                Object.DestroyImmediate(obj);
            }
        }

        [Test]
        public void CameraStaysAboveTerrainDuringJumpAndShoulderView()
        {
            Assert.AreEqual(
                61.20f,
                SectorPlayer.CameraAboveSurface(59f, 60f, false),
                .001f);
            Assert.AreEqual(
                60.32f,
                SectorPlayer.CameraAboveSurface(58f, 60f, true),
                .001f);
            Assert.AreEqual(
                64f,
                SectorPlayer.CameraAboveSurface(64f, 60f, false),
                .001f);
        }

        [Test]
        public void ImportedJumpPoseCannotSinkBelowWorldByTwoMetres()
        {
            Assert.AreEqual(
                1.5f,
                SectorPlayer.VisualLiftForGround(58f, 60f),
                .001f);
            Assert.AreEqual(
                0f,
                SectorPlayer.VisualLiftForGround(62f, 60f),
                .001f);
            Assert.AreEqual(
                .12f,
                SectorPlayer.VisualLiftForGround(59.9f, 60f),
                .001f);
        }

        [Test]
        public void GroundTileLookupIncludesEdgesAndRejectsOtherTiles()
        {
            Vector3 origin = new Vector3(-1000f, 0f, 2000f);
            Vector3 size = new Vector3(1000f, 500f, 1000f);

            Assert.IsTrue(SectorPlayer.IsInsideTerrainXZ(origin, size,
                new Vector3(-500f, 100f, 2500f)));
            Assert.IsTrue(SectorPlayer.IsInsideTerrainXZ(origin, size,
                new Vector3(0f, -100f, 3000f)));
            Assert.IsFalse(SectorPlayer.IsInsideTerrainXZ(origin, size,
                new Vector3(0.01f, 0f, 2500f)));
            Assert.IsFalse(SectorPlayer.IsInsideTerrainXZ(origin, size,
                new Vector3(-500f, 0f, 1999.99f)));
        }

        [Test]
        public void TurnDampingIsSmoothAndFrameRateIndependent()
        {
            float oneStep = SectorPlayer.TurnBlend(24f, 1f / 60f);
            float halfStep = SectorPlayer.TurnBlend(24f, 1f / 120f);
            float twoHalfSteps = 1f - (1f - halfStep) * (1f - halfStep);

            Assert.Greater(oneStep, 0f);
            Assert.Less(oneStep, 1f, "Turning should not snap at normal frame rates.");
            Assert.AreEqual(oneStep, twoHalfSteps, .0001f);
            Assert.AreEqual(0f, SectorPlayer.TurnBlend(24f, 0f));
            Assert.AreEqual(1f, SectorPlayer.TurnBlend(0f, .016f));
        }

        [Test]
        public void SmoothedYawUsesShortestPathAcrossZero()
        {
            float yaw = Mathf.LerpAngle(359f, 1f,
                SectorPlayer.TurnBlend(24f, 1f / 60f));
            Assert.Less(Mathf.DeltaAngle(359f, yaw), 2.1f);
            Assert.Greater(Mathf.DeltaAngle(359f, yaw), 0f);
        }

        [Test]
        public void SprintLockRequiresFoodAndWater()
        {
            var obj = new GameObject("SprintNeedsTests");
            try
            {
                SectorSurvival needs = obj.AddComponent<SectorSurvival>();
                needs.stamina = 100f;
                needs.hunger = 0f;
                Assert.IsFalse(needs.CanSprint);
                needs.hunger = 80f;
                needs.thirst = 0f;
                Assert.IsFalse(needs.CanSprint);
                needs.thirst = 80f;
                Assert.IsTrue(needs.CanSprint);
            }
            finally
            {
                Object.DestroyImmediate(obj);
            }
        }
    }
}
