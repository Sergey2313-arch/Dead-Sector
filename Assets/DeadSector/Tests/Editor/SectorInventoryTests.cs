using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DeadSector.Tests
{
    public sealed class SectorInventoryTests
    {
        [Test]
        public void StacksSplitAtExactItemLimit()
        {
            var inventory = new SectorInventory(4, 100f);
            Assert.IsTrue(inventory.Add("water", 7));
            Assert.AreEqual(2, inventory.UsedSlots);
            Assert.AreEqual(6, inventory.Stacks[0].count);
            Assert.AreEqual(1, inventory.Stacks[1].count);
            Assert.AreEqual(7, inventory.Count("water"));
        }

        [Test]
        public void AddingTooMuchWeightIsAtomic()
        {
            var inventory = new SectorInventory(5, 2f);
            Assert.IsTrue(inventory.Add("wood", 1));
            Assert.IsFalse(inventory.Add("scrap", 1));
            Assert.AreEqual(1, inventory.Count("wood"));
            Assert.AreEqual(0, inventory.Count("scrap"));
            Assert.AreEqual(1f, inventory.Weight, .001f);
        }

        [Test]
        public void SlotLimitNeverCreatesPartiallyAcceptedStack()
        {
            var inventory = new SectorInventory(1, 100f);
            Assert.IsTrue(inventory.Add("water", 5));
            Assert.IsFalse(inventory.Add("water", 7));
            Assert.AreEqual(5, inventory.Count("water"));
            Assert.AreEqual(1, inventory.UsedSlots);
        }

        [Test]
        public void RemoveDoesNotConsumeMoreThanAvailable()
        {
            var inventory = new SectorInventory();
            inventory.Add("bandage", 2);

            Assert.IsFalse(inventory.Remove("bandage", 3));
            Assert.AreEqual(2, inventory.Count("bandage"));

            Assert.IsTrue(inventory.Remove("bandage", 2));
            Assert.AreEqual(0, inventory.Count("bandage"));
        }

        [Test]
        public void InvalidSavedItemsDoNotBypassCapacity()
        {
            var inventory = new SectorInventory(2, 2f);
            inventory.Import(new List<SectorItemStack>
            {
                new SectorItemStack("missing", 4),
                new SectorItemStack("wood", -6),
                new SectorItemStack("wood", 1),
                new SectorItemStack("wood", 80)
            });

            Assert.AreEqual(1, inventory.Count("wood"));
            Assert.AreEqual(1, inventory.UsedSlots);
        }

        [Test]
        public void ExportUsesCopyNotLiveStackReferences()
        {
            var inventory = new SectorInventory();
            inventory.Add("water", 2);
            List<SectorItemStack> saved = inventory.Export();
            saved[0].count = 99;
            Assert.AreEqual(2, inventory.Count("water"));
        }

        [Test]
        public void ItemCatalogHasNoRarityQualityLevel()
        {
            SectorItemDefinition knife = SectorItems.Get("knife");
            SectorItemDefinition rifle = SectorItems.Get("rifle");

            Assert.AreEqual(SectorItemKind.Melee, knife.Kind);
            Assert.AreEqual(SectorItemKind.Firearm, rifle.Kind);
            Assert.Greater(rifle.Weight, knife.Weight);
            Assert.IsFalse(knife.IsConsumable);
        }

        [Test]
        public void SaveJsonRoundTripRetainsInventoryAndLootOverrides()
        {
            var save = new SectorGameSave
            {
                position = new Vector3(31, 71, 44),
                health = 70f,
                hunger = 48f,
                thirst = 23f,
                stamina = 87f,
                primary = "rifle",
                inventory = new List<SectorItemStack> { new SectorItemStack("556", 28) },
                containers = new List<SectorContainerSnapshot>
                {
                    new SectorContainerSnapshot
                    {
                        id = "starter_0",
                        items = new List<SectorItemStack>()
                    }
                }
            };

            SectorGameSave restored =
                JsonUtility.FromJson<SectorGameSave>(JsonUtility.ToJson(save));

            Assert.AreEqual(save.position, restored.position);
            Assert.AreEqual(28, restored.inventory[0].count);
            Assert.AreEqual("starter_0", restored.containers[0].id);
            Assert.AreEqual(0, restored.containers[0].items.Count);
        }

        [Test]
        public void LootPickupKeepsContainerWhenInventoryIsFull()
        {
            var obj = new GameObject("Test_Loot");

            try
            {
                var loot = obj.AddComponent<SectorLootContainer>();
                loot.Initialize(
                    "test-id", "Test",
                    new[] { new SectorItemStack("water", 2) });

                var inventory = new SectorInventory(1, .1f);

                Assert.IsFalse(loot.TakeFirst(inventory, out _));
                Assert.AreEqual(2, loot.Contents[0].count);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }
    }
}
