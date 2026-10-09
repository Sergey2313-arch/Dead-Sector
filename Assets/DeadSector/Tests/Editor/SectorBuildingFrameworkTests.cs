using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DeadSector.Tests
{
    public sealed class SectorBuildingFrameworkTests
    {
        [Test]
        public void AllBuildPlansHaveSafeDimensionsAndRealItemCosts()
        {
            Assert.AreEqual(5, SectorBuildCatalog.Count);

            for (int i = 0; i < SectorBuildCatalog.Count; i++)
            {
                SectorBuildSpecification plan = SectorBuildCatalog.At(i);
                Assert.IsTrue(SectorBuildCatalog.IsValid(plan.Kind));
                Assert.Greater(plan.Dimensions.x, 0f);
                Assert.Greater(plan.Dimensions.y, 0f);
                Assert.Greater(plan.Dimensions.z, 0f);
                Assert.Greater(plan.Health, 0f);
                Assert.AreEqual(plan.ItemIds.Length, plan.Counts.Length);

                for (int j = 0; j < plan.Counts.Length; j++)
                {
                    Assert.IsTrue(SectorItems.TryGet(
                        plan.ItemIds[j], out _));
                    Assert.Greater(plan.Counts[j], 0);
                }
            }
        }

        [Test]
        public void UnaffordablePlacementNeverTakesAnyItems()
        {
            var inventory = new SectorInventory(10, 80f);
            inventory.Add("wood", 3);
            SectorBuildSpecification wall =
                SectorBuildCatalog.At((int)SectorBuildKind.Wall);

            Assert.IsFalse(SectorBuildCatalog.CanAfford(inventory, wall));
            Assert.IsFalse(SectorBuildCatalog.Consume(inventory, wall));
            Assert.AreEqual(3, inventory.Count("wood"));
            Assert.AreEqual(0, inventory.Count("stick"));
        }

        [Test]
        public void SuccessfulPlacementConsumesExactlyOneRecipe()
        {
            var inventory = new SectorInventory(10, 80f);
            Assert.IsTrue(inventory.Add("wood", 9));
            Assert.IsTrue(inventory.Add("stick", 5));
            SectorBuildSpecification wall =
                SectorBuildCatalog.At((int)SectorBuildKind.Wall);

            Assert.IsTrue(SectorBuildCatalog.Consume(inventory, wall));
            Assert.AreEqual(4, inventory.Count("wood"));
            Assert.AreEqual(3, inventory.Count("stick"));
        }

        [Test]
        public void SaveSnapshotsRoundtripWithoutBreakingOldSaveVersions()
        {
            var save = new SectorGameSave
            {
                version = 1,
                position = SectorLayout.Spawn,
                health = 75f,
                structures = new List<SectorBuildSnapshot>
                {
                    new SectorBuildSnapshot
                    {
                        id = "camp_wall_1",
                        kind = SectorBuildKind.Wall,
                        position = new Vector3(43f, 60f, 28f),
                        angle = 90f,
                        health = 44f
                    },
                    new SectorBuildSnapshot
                    {
                        id = "camp_storage_1",
                        kind = SectorBuildKind.Storage,
                        position = new Vector3(41f, 60f, 28f),
                        health = 70f,
                        storage = new List<SectorItemStack>
                        {
                            new SectorItemStack("water", 2)
                        }
                    },
                    new SectorBuildSnapshot
                    {
                        id = "camp_door_1",
                        kind = SectorBuildKind.Door,
                        position = new Vector3(42f, 60f, 30f),
                        health = 100f,
                        doorOpen = true
                    }
                }
            };

            string json = JsonUtility.ToJson(save);
            var loaded = JsonUtility.FromJson<SectorGameSave>(json);

            Assert.AreEqual(1, loaded.version);
            Assert.AreEqual(3, loaded.structures.Count);
            Assert.AreEqual(44f, loaded.structures[0].health, .001f);
            Assert.AreEqual(SectorBuildKind.Storage,
                loaded.structures[1].kind);
            Assert.AreEqual(2, loaded.structures[1].storage[0].count);
            Assert.IsTrue(loaded.structures[2].doorOpen);
        }

        [Test]
        public void ExistingV1SaveWithoutBuildFieldCanStillBeRead()
        {
            string oldJson = "{\"version\":1,\"health\":100," +
                "\"position\":{\"x\":30,\"y\":60,\"z\":20}," +
                "\"inventory\":[],\"containers\":[]}";
            SectorGameSave loaded =
                JsonUtility.FromJson<SectorGameSave>(oldJson);

            Assert.AreEqual(1, loaded.version);
            // A missing field can be either null or a default empty list
            // depending on the Unity JsonUtility initialization path.
            Assert.IsTrue(loaded.structures == null ||
                loaded.structures.Count == 0);
        }
    }
}
