using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DeadSector.Tests
{
    public sealed class SectorLootCapacityTests
    {
        [Test]
        public void TooHeavyFirstStackDoesNotBlockLaterBandage()
        {
            GameObject obj = new GameObject("SupplyCache");
            try
            {
                var cache = obj.AddComponent<SectorLootContainer>();
                cache.Initialize("test_cache", "Test", new[]
                {
                    new SectorItemStack("scrap", 2),
                    new SectorItemStack("bandage", 1)
                });

                var inventory = new SectorInventory(2, 1f);

                Assert.IsTrue(cache.TakeFirst(inventory, out string label));
                Assert.AreEqual(0, inventory.Count("scrap"));
                Assert.AreEqual(1, inventory.Count("bandage"));
                Assert.AreEqual(2, cache.Contents.Count > 0
                    ? cache.Contents[0].count : 0);
                StringAssert.Contains("Bandage", label);
            }
            finally
            {
                Object.DestroyImmediate(obj);
            }
        }

        [Test]
        public void FailedLootPickupNeverConsumesCachedItems()
        {
            GameObject obj = new GameObject("SupplyCache_Full");
            try
            {
                var cache = obj.AddComponent<SectorLootContainer>();
                cache.Initialize("test_cache", "Test", new[]
                {
                    new SectorItemStack("scrap", 3)
                });

                var inventory = new SectorInventory(1, .5f);
                Assert.IsFalse(cache.TakeFirst(inventory, out _));
                Assert.AreEqual(3, cache.Contents[0].count);
                Assert.AreEqual(0, inventory.UsedSlots);
            }
            finally
            {
                Object.DestroyImmediate(obj);
            }
        }
    }
}
