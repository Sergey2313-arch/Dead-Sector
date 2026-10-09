using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DeadSector.Tests
{
    public sealed class SectorContentPassTests
    {
        [Test]
        public void BiomesAreDeterministicAcrossRepeatedQueries()
        {
            Vector2[] positions =
            {
                new Vector2(300f, 500f),
                new Vector2(-1350f, -2300f),
                new Vector2(2050f, -1950f),
                new Vector2(-500f, 900f)
            };
            foreach (Vector2 point in positions)
            {
                SectorBiome before = SectorBiomeRules.At(point.x, point.y);
                Assert.AreEqual(before, SectorBiomeRules.At(point.x, point.y));
                Assert.That((int)before, Is.InRange(0, 4));
            }
            Assert.Greater(
                SectorBiomeRules.TreeDensity(SectorBiome.ConiferForest),
                SectorBiomeRules.TreeDensity(SectorBiome.DrySteppe));
            Assert.Greater(
                SectorBiomeRules.TreeDensity(SectorBiome.MixedForest),
                SectorBiomeRules.TreeDensity(SectorBiome.RockyHighland));
        }

        [Test]
        public void BiomesShiftResourcesTowardOreCottonAndDryBrush()
        {
            // Hash 16 => ore/normal, cotton/meadow, ore/highland.
            Assert.AreEqual(SectorResourceType.Ore,
                SectorResources.ResourceForBiome(
                    SectorBiome.RockyHighland, 16u));
            Assert.AreEqual(SectorResourceType.CottonPlant,
                SectorResources.ResourceForBiome(
                    SectorBiome.Meadow, 16u));
            Assert.AreEqual(SectorResourceType.DryBush,
                SectorResources.ResourceForBiome(
                    SectorBiome.DrySteppe, 13u));
            // The baseline resources remain obtainable in every biome.
            foreach (SectorBiome biome in Enum.GetValues(typeof(SectorBiome)))
                Assert.AreEqual(SectorResourceType.GroundStone,
                    SectorResources.ResourceForBiome(biome, 0u));
        }

        [Test]
        public void AllBiomeGrassColoursStayWithinDisplayRange()
        {
            foreach (SectorBiome biome in Enum.GetValues(typeof(SectorBiome)))
            {
                Color c = SectorBiomeRules.GrassColor(biome);
                Assert.That(c.r, Is.InRange(0f, 1f));
                Assert.That(c.g, Is.InRange(0f, 1f));
                Assert.That(c.b, Is.InRange(0f, 1f));
            }
            Assert.IsFalse(SectorWorld.CanGrowGrass(0f, 0f));
        }

        [Test]
        public void ZombieDropsAreDifferentAndUseValidSavedItemIds()
        {
            string[] expected =
            {
                "cloth", "9mm", "scrap"
            };
            SectorZombieKind[] kinds =
            {
                SectorZombieKind.Shambler,
                SectorZombieKind.Runner,
                SectorZombieKind.Brute
            };
            for (int i = 0; i < kinds.Length; i++)
            {
                List<SectorItemStack> drop =
                    SectorZombieLootTable.For(kinds[i]);
                Assert.IsNotEmpty(drop);
                Assert.AreEqual(expected[i], drop[0].id);
                foreach (SectorItemStack stack in drop)
                {
                    Assert.IsTrue(SectorItems.TryGet(stack.id, out _));
                    Assert.Greater(stack.count, 0);
                }
            }
        }

        [Test]
        public void HouseAndClinicAndShopCachesHaveThemedLoot()
        {
            var root = new GameObject("InteriorCacheRegression");
            try
            {
                var cache = root.AddComponent<SectorLootContainer>();
                cache.Initialize("interior_Clinic", "Медицина");
                Assert.IsFalse(cache.Empty);
                Assert.AreEqual("bandage", cache.Contents[0].id);
                Assert.GreaterOrEqual(cache.Contents.Count, 2);

                cache.Initialize("interior_FuelShop", "Продукты");
                Assert.AreEqual("food", cache.Contents[0].id);

                cache.Initialize("interior_Factory", "Механика");
                Assert.AreEqual("scrap", cache.Contents[0].id);

                cache.Initialize("interior_Village_House_N_1", "Дом");
                Assert.AreEqual("food", cache.Contents[0].id);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DepletedInteriorCacheStaysEmptyAfterSaveLoad()
        {
            var root = new GameObject("DepletedInterior");
            try
            {
                var crate = root.AddComponent<SectorLootContainer>();
                crate.Initialize("interior_Clinic", "Аптечный шкаф",
                    new List<SectorItemStack>());
                Assert.IsTrue(crate.Empty);
                List<SectorItemStack> restored = crate.Export();
                crate.Initialize("interior_Clinic", "Аптечный шкаф", restored);
                Assert.IsTrue(crate.Empty,
                    "A previously searched cabinet must not spawn new loot.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RoofHasTwoSlopesAndCampfireEmitsLight()
        {
            GameObject roofRoot = new GameObject("TestRoof");
            GameObject campfireRoot = new GameObject("TestCampfire");
            Material surface = null;
            try
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                Assert.IsNotNull(shader);
                surface = new Material(shader);
                SectorBuildSpecification roofPlan = SectorBuildCatalog.At(
                    (int)SectorBuildKind.Roof);
                var roof = roofRoot.AddComponent<SectorBuildPiece>();
                roof.Configure(new SectorBuildSnapshot
                {
                    id = "roof_t",
                    kind = SectorBuildKind.Roof,
                    health = roofPlan.Health,
                    position = Vector3.zero
                }, surface, surface);
                Assert.IsNotNull(roofRoot.transform.Find("Roof_Slope_Left"));
                Assert.IsNotNull(roofRoot.transform.Find("Roof_Slope_Right"));
                Assert.IsNotNull(roofRoot.transform.Find("Ridge_Beam"));

                SectorBuildSpecification firePlan = SectorBuildCatalog.At(
                    (int)SectorBuildKind.Campfire);
                var fire = campfireRoot.AddComponent<SectorBuildPiece>();
                fire.Configure(new SectorBuildSnapshot
                {
                    id = "fire_t",
                    kind = SectorBuildKind.Campfire,
                    health = firePlan.Health,
                    position = new Vector3(7f, 0f, 0f)
                }, surface, surface);
                Transform core = campfireRoot.transform.Find("Fire_Core");
                Assert.IsNotNull(core);
                Light light = core.GetComponent<Light>();
                Assert.IsNotNull(light);
                Assert.AreEqual(LightType.Point, light.type);
                Assert.Greater(light.range, 3f);
                Assert.AreEqual(LightShadows.None, light.shadows);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(roofRoot);
                UnityEngine.Object.DestroyImmediate(campfireRoot);
                if (surface != null)
                    UnityEngine.Object.DestroyImmediate(surface);
            }
        }

        [Test]
        public void RoofAndCampfireHaveSeparateStableBlueprintIds()
        {
            SectorBuildSpecification roof = SectorBuildCatalog.At(
                (int)SectorBuildKind.Roof);
            SectorBuildSpecification fire = SectorBuildCatalog.At(
                (int)SectorBuildKind.Campfire);
            Assert.AreEqual("build_roof", SectorBuildCatalog.KitId(roof.Kind));
            Assert.AreEqual("build_campfire", SectorBuildCatalog.KitId(fire.Kind));
            Assert.AreEqual(7, SectorBuildCatalog.Count);
            Assert.AreNotEqual(roof.Dimensions, fire.Dimensions);
            Assert.IsNotNull(SectorCrafting.Find("build_roof"));
            Assert.IsNotNull(SectorCrafting.Find("build_campfire"));
        }
    }
}
