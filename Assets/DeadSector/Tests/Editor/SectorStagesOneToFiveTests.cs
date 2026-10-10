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
        public void StageTwoForestGroundResourceKeepsReferencedTextures()
        {
            // Real asset integration test. A broken YAML GUID or missing
            // Resources catalog must fail visibly, not silently use flat paint.
            TerrainLayer forest =
                SectorTerrainAssetCatalog.LoadForestGround();
            Assert.IsNotNull(forest,
                "Forest Ground 03 must resolve from TerrainCatalog");
            Assert.IsNotNull(forest.diffuseTexture);
            Assert.IsNotNull(forest.normalMapTexture);
            Assert.That(forest.tileSize.x, Is.EqualTo(4f).Within(.01f));
            Assert.That(forest.tileSize.y, Is.EqualTo(4f).Within(.01f));
            Assert.That(forest.metallic, Is.EqualTo(0f));
            Assert.That(forest.smoothness, Is.EqualTo(0f));
        }

        [Test]
        public void StageTwoForestGroundCoverageProtectsRoadsAndHighRock()
        {
            Assert.AreEqual(0f, SectorWorld.ForestFloorFraction(
                285f, -416f, true, false));
            Assert.AreEqual(0f, SectorWorld.ForestFloorFraction(
                285f, -416f, false, true));
            float forest = SectorWorld.ForestFloorFraction(
                285f, -416f, false, false);
            Assert.That(forest, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.AreEqual(forest, SectorWorld.ForestFloorFraction(
                285f, -416f, false, false));
        }

        [Test]
        public void StageTwoGroundPaintBlendsSoilAndRockWithoutBlueWater()
        {
            Vector2[] sites =
            {
                new Vector2(285f, -416f),
                new Vector2(-600f, 820f),
                new Vector2(1350f, -1740f)
            };
            foreach (Vector2 site in sites)
            {
                Vector3 meadow = SectorWorld.GroundBlend(
                    site.x, site.y, false, false);
                Assert.That(meadow.x + meadow.y + meadow.z,
                    Is.EqualTo(1f).Within(.00001f));
                Assert.Greater(meadow.x, .50f);
                Assert.Greater(meadow.y, .09f);
                Assert.GreaterOrEqual(meadow.z, 0f);
                Assert.AreEqual(meadow, SectorWorld.GroundBlend(
                    site.x, site.y, false, false));
            }

            Vector3 road = SectorWorld.GroundBlend(0f, 0f, true, false);
            Vector3 mountain = SectorWorld.GroundBlend(
                0f, 0f, false, true);
            Assert.Greater(road.y, .9f);
            Assert.Greater(mountain.z, .6f);
            Assert.That(road.x + road.y + road.z,
                Is.EqualTo(1f).Within(.00001f));
        }

        [Test]
        public void StageTwoCameraBackdropUpdatesWithFog()
        {
            GameObject obj = new GameObject("Backdrop");
            try
            {
                Camera camera = obj.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                SectorWeather.SyncCameraBackdrop(camera,
                    new Color(.43f, .47f, .43f));
                Assert.That(camera.backgroundColor.g,
                    Is.EqualTo(.47f).Within(.0001f));
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.backgroundColor = Color.red;
                SectorWeather.SyncCameraBackdrop(camera, Color.black);
                Assert.AreEqual(Color.red, camera.backgroundColor);
            }
            finally
            {
                Object.DestroyImmediate(obj);
            }
        }

        [Test]
        public void StageTwoDaylightFogRetainsVisibleTerrainDetail()
        {
            float daylight = SectorWorldClock.BaseFogDensity(1f);
            float nighttime = SectorWorldClock.BaseFogDensity(0f);
            Assert.Greater(nighttime, daylight);
            Assert.That(daylight, Is.GreaterThan(0f).And.LessThan(.0005f));
            Assert.That(SectorWeather.FogMultiplier(
                    SectorWeatherKind.Clear),
                Is.EqualTo(1f));
            Assert.Greater(SectorWeather.FogMultiplier(
                SectorWeatherKind.Mist), 1f);
            Assert.Less(SectorWeather.FogMultiplier(
                SectorWeatherKind.Overcast), 1.3f);
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
        public void CrouchingDropsTorsoMoreThanLegRoots()
        {
            // Procedural foot meshes pivot around their own hips; moving
            // the torso only was the cause of the stiff, tall crouch.
            Assert.That(SectorMannequin.CrouchTorsoDrop,
                Is.GreaterThan(.4f).And.LessThan(.8f));
            Assert.That(SectorMannequin.CrouchLegDrop,
                Is.GreaterThan(0f).And.LessThan(.25f));
            Assert.Greater(SectorMannequin.CrouchTorsoDrop,
                SectorMannequin.CrouchLegDrop * 2f);
        }

        [Test]
        public void ZombieGroundCorrectionIsPositiveOnlyAndClamped()
        {
            Assert.That(SectorZombieVisualGrounding.RequiredLift(59f, 60f),
                Is.EqualTo(1.025f).Within(.0001f));
            Assert.AreEqual(0f,
                SectorZombieVisualGrounding.RequiredLift(60.2f, 60f));
            Assert.AreEqual(3f,
                SectorZombieVisualGrounding.RequiredLift(50f, 60f));
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
