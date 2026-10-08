using NUnit.Framework;
using UnityEngine;

namespace DeadSector.Tests
{
    public sealed class SectorLocationLootTests
    {
        [TestCase("poi_clinic", "medkit")]
        [TestCase("poi_forest_camp", "stick")]
        [TestCase("poi_radio_station", "scrap")]
        [TestCase("poi_quarry", "stone")]
        [TestCase("poi_construction", "cord")]
        [TestCase("poi_airfield", "food")]
        public void NamedLandmarksHaveThematicSupplies(
            string cacheId, string expectedItem)
        {
            GameObject obj = new GameObject(cacheId);
            try
            {
                var cache = obj.AddComponent<SectorLootContainer>();
                cache.Initialize(cacheId, cacheId);

                int count = 0;
                foreach (SectorItemStack stack in cache.Contents)
                    if (stack.id == expectedItem)
                        count += stack.count;

                Assert.Greater(count, 0, cacheId + " has no " + expectedItem);
            }
            finally
            {
                Object.DestroyImmediate(obj);
            }
        }

        [Test]
        public void SavedCacheOverridesNewDefaultLoot()
        {
            GameObject obj = new GameObject("SavedCache");
            try
            {
                var cache = obj.AddComponent<SectorLootContainer>();
                cache.Initialize("poi_clinic", "Clinic", new[]
                {
                    new SectorItemStack("water", 1)
                });

                Assert.AreEqual(1, cache.Contents.Count);
                Assert.AreEqual("water", cache.Contents[0].id);
            }
            finally
            {
                Object.DestroyImmediate(obj);
            }
        }
    }
}
