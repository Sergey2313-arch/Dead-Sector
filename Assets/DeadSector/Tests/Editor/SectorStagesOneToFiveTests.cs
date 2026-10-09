using NUnit.Framework;
using UnityEngine;

namespace DeadSector.Tests
{
    // Stage-by-stage acceptance invariants that do not require PlayMode
    // input devices, downloadable FBX assets or authored Unity scenes.
    public sealed class SectorStagesOneToFiveTests
    {
        [Test]
        public void StageOneWeaponStancesDifferByHeldEquipment()
        {
            Assert.AreEqual(SectorCarryPose.Unarmed,
                SectorEquipmentVisuals.PoseFor(""));
            Assert.AreEqual(SectorCarryPose.Tool,
                SectorEquipmentVisuals.PoseFor("stone_axe"));
            Assert.AreEqual(SectorCarryPose.Tool,
                SectorEquipmentVisuals.PoseFor("stone_pickaxe"));
            Assert.AreEqual(SectorCarryPose.Pistol,
                SectorEquipmentVisuals.PoseFor("pistol"));
            Assert.AreEqual(SectorCarryPose.Rifle,
                SectorEquipmentVisuals.PoseFor("rifle"));
            Assert.AreNotEqual(
                SectorMannequin.ArmAngles(SectorCarryPose.Tool, true),
                SectorMannequin.ArmAngles(SectorCarryPose.Rifle, true));
            Assert.AreNotEqual(
                SectorMannequin.ArmAngles(SectorCarryPose.Unarmed, false),
                SectorMannequin.ArmAngles(SectorCarryPose.Pistol, false));
            Assert.AreNotEqual(
                SectorMannequin.ArmAngles(SectorCarryPose.Infected, true),
                SectorMannequin.ArmAngles(SectorCarryPose.Unarmed, true));
        }

        [Test]
        public void StageOneCrouchCapsuleIsShorterThanStanding()
        {
            Assert.That(SectorPlayer.CrouchingHeight,
                Is.GreaterThan(1f).And.LessThan(SectorPlayer.StandingHeight));
            Assert.That(SectorPlayer.StandingHeight,
                Is.GreaterThan(1.7f).And.LessThan(1.9f));
        }

        [Test]
        public void StageTwoLowPolyConiferMeshHasOutwardFacesAndBounds()
        {
            Mesh cone = SectorVegetationMesh.CreateConiferCone(10);
            try
            {
                Assert.AreEqual(12, cone.vertexCount);
                Assert.AreEqual(60, cone.triangles.Length);
                Assert.That(cone.bounds.min.y, Is.EqualTo(0f).Within(.001f));
                Assert.That(cone.bounds.max.y, Is.EqualTo(1f).Within(.001f));
                Assert.Greater(cone.bounds.size.x, .9f);
                Assert.Greater(cone.bounds.size.z, .9f);
            }
            finally
            {
                Object.DestroyImmediate(cone);
            }
        }

        [Test]
        public void StageThreeWallsSnapToRealFoundationEdges()
        {
            Vector3 foundation = new Vector3(21f, 60f, -36f);
            Vector3 north = SectorBaseBuilding.FoundationEdge(foundation, 0f);
            Vector3 east = SectorBaseBuilding.FoundationEdge(foundation, 90f);
            Assert.That(north.z, Is.EqualTo(-34.5f).Within(.001f));
            Assert.That(north.y, Is.EqualTo(60.25f).Within(.001f));
            Assert.That(east.x, Is.EqualTo(22.5f).Within(.001f));
            Assert.That(east.z, Is.EqualTo(-36f).Within(.001f));
        }

        [Test]
        public void StageThreeConstructionSupportsIgnoreOnlyIntendedContacts()
        {
            Assert.IsTrue(SectorBaseBuilding.CanOverlapSupports(
                SectorBuildKind.Roof, SectorBuildKind.Wall));
            Assert.IsTrue(SectorBaseBuilding.CanOverlapSupports(
                SectorBuildKind.Wall, SectorBuildKind.Foundation));
            Assert.IsTrue(SectorBaseBuilding.CanOverlapSupports(
                SectorBuildKind.Door, SectorBuildKind.Foundation));
            Assert.IsFalse(SectorBaseBuilding.CanOverlapSupports(
                SectorBuildKind.Wall, SectorBuildKind.Wall));
            Assert.IsFalse(SectorBaseBuilding.CanOverlapSupports(
                SectorBuildKind.Roof, SectorBuildKind.Roof));
            Assert.IsFalse(SectorBaseBuilding.CanOverlapSupports(
                SectorBuildKind.Foundation, SectorBuildKind.Wall));
        }

        [Test]
        public void StageThreeRestRadiusOnlyIncludesExistingCampfire()
        {
            var root = new GameObject("WarmBase");
            try
            {
                var building = root.AddComponent<SectorBaseBuilding>();
                building.Import(new[]
                {
                    new SectorBuildSnapshot
                    {
                        id = "campfire_saved_1",
                        kind = SectorBuildKind.Campfire,
                        health = SectorBuildCatalog.At(
                            (int)SectorBuildKind.Campfire).Health,
                        position = new Vector3(40f, 60f, 20f)
                    }
                });
                Assert.IsTrue(building.IsNearCampfire(
                    new Vector3(42f, 60f, 21f)));
                Assert.IsFalse(building.IsNearCampfire(
                    new Vector3(55f, 60f, 20f)));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void StageFourCrouchReducesAudibleDetectionAndStandingStillIsSilent()
        {
            float walking = SectorZombie.EffectiveHearingRange(22f, 4f, false);
            float hidden = SectorZombie.EffectiveHearingRange(22f, 4f, true);
            Assert.Greater(walking, 0f);
            Assert.That(hidden, Is.EqualTo(walking * .4f).Within(.0001f));
            Assert.AreEqual(0f,
                SectorZombie.EffectiveHearingRange(22f, 0f, true));
            Assert.AreEqual(0f,
                SectorZombie.EffectiveHearingRange(22f, 0f, false));
        }

        [Test]
        public void StageFourBrutesBreakDoorsFasterThanRunnersBreakBarricades()
        {
            float brute = SectorZombie.StructureDamage(
                SectorZombieKind.Brute, SectorBuildKind.Door, 10f);
            float runner = SectorZombie.StructureDamage(
                SectorZombieKind.Runner, SectorBuildKind.Barricade, 10f);
            Assert.Greater(brute, runner);
            Assert.Greater(brute, 10f);
            Assert.Less(runner, 10f);
            Assert.AreEqual(0f, SectorZombie.StructureDamage(
                SectorZombieKind.Brute, SectorBuildKind.Door, 0f));
        }

        [Test]
        public void StageFiveFlashlightToggleUpdatesActualSpotlight()
        {
            var root = new GameObject("FlashlightRegression");
            try
            {
                var lamp = root.AddComponent<SectorFlashlight>();
                lamp.Configure(null);
                var beam = root.GetComponentInChildren<Light>(true);
                Assert.IsNotNull(beam);
                Assert.AreEqual(LightType.Spot, beam.type);
                Assert.IsFalse(lamp.Enabled);
                Assert.IsFalse(beam.enabled);
                lamp.SetEnabled(true);
                Assert.IsTrue(lamp.Enabled);
                Assert.IsTrue(beam.enabled);
                lamp.SetEnabled(false);
                Assert.IsFalse(beam.enabled);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void StageFiveFlashlightHasInputBinding()
        {
            Assert.IsTrue(SectorInput.HasNewInputBinding(KeyCode.F));
            Assert.IsTrue(SectorInput.HasNewInputBinding(KeyCode.H));
            for (int i = 0; i < SectorBuildCatalog.Count; i++)
                Assert.IsTrue(SectorInput.HasNewInputBinding(KeyCode.Alpha1 + i));
        }
    }
}
