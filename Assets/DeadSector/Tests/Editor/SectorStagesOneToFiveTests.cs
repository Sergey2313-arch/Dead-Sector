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
