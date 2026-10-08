using NUnit.Framework;
using UnityEngine;

namespace DeadSector.Tests
{
    public sealed class SectorLayoutTests
    {
        [Test]
        public void WorldCoversExactlyEightKilometresWithoutGaps()
        {
            Assert.AreEqual(-4000, SectorLayout.Origin(Vector2Int.zero).x);
            Assert.AreEqual(4000,
                SectorLayout.Origin(new Vector2Int(7, 7)).x + SectorLayout.TileSize);

            for (int x = 0; x < 7; x++)
                Assert.AreEqual(
                    SectorLayout.Origin(new Vector2Int(x, 0)).x + SectorLayout.TileSize,
                    SectorLayout.Origin(new Vector2Int(x + 1, 0)).x);
        }

        [Test]
        public void TileIndexHandlesNegativePositionsAndWorldEdges()
        {
            Assert.AreEqual(new Vector2Int(0, 0),
                SectorLayout.TileAt(new Vector3(-4000, 0, -4000)));
            Assert.AreEqual(new Vector2Int(3, 3),
                SectorLayout.TileAt(new Vector3(-.01f, 0, -.01f)));
            Assert.AreEqual(new Vector2Int(4, 4),
                SectorLayout.TileAt(Vector3.zero));
            Assert.AreEqual(new Vector2Int(7, 7),
                SectorLayout.TileAt(new Vector3(4000, 0, 4000)));
        }

        [Test]
        public void LegacyStarterVillageRemainsStable()
        {
            Assert.AreEqual(60, SectorLayout.Height(30, 20), .001f);
            Assert.AreEqual(60, SectorLayout.Height(220, 230), .001f);
            Assert.AreEqual(60, SectorLayout.Height(0, 0), .001f);
        }

        [Test]
        public void WorldHeightSamplesStayFiniteAndWithinTerrainDataRange()
        {
            for (int x = -4000; x <= 4000; x += 120)
            {
                for (int z = -4000; z <= 4000; z += 120)
                {
                    float h = SectorLayout.Height(x, z);
                    Assert.IsFalse(float.IsNaN(h), "NaN at " + x + "," + z);
                    Assert.That(h, Is.InRange(2f, 480f));
                }
            }
        }

        [Test]
        public void PhysicalLakesAreActuallyCarvedBelowWaterSurface()
        {
            Assert.IsTrue(SectorGeography.TryGetWaterLevel(
                -2650f, 2850f, out float mountainLevel));

            Assert.Less(SectorLayout.Height(-2650f, 2850f), mountainLevel);

            Assert.IsTrue(SectorGeography.TryGetWaterLevel(
                -250f, -2900f, out float southernLevel));

            Assert.Less(SectorLayout.Height(-250f, -2900f), southernLevel);
        }

        [Test]
        public void RiverIsContinuousAndBelowLowWaterLevel()
        {
            for (int z = -3000; z <= 3200; z += 200)
            {
                float x = SectorGeography.RiverX(z);

                Assert.IsTrue(
                    SectorGeography.TryGetWaterLevel(x, z, out float water),
                    "Missing water at Z=" + z);

                Assert.Less(
                    SectorLayout.Height(x, z),
                    water,
                    "River bed out of water at Z=" + z);
            }
        }

        [Test]
        public void RoadsFollowApprovedWorldRoutes()
        {
            foreach (Vector2[] route in SectorMapPlan.RoadRoutes)
            {
                foreach (Vector2 point in route)
                {
                    Assert.Less(SectorGeography.DistanceToRoad(point.x, point.y), .001f);
                }
            }
        }
    }
}
