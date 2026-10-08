using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DeadSector.Tests
{
    public sealed class SectorMapPlanTests
    {
        [Test]
        public void AtlasContainsOfficialLocationsOnlyOnceAndWithinWorldBounds()
        {
            Assert.GreaterOrEqual(SectorMapPlan.Locations.Count, 17);

            var unique = new HashSet<string>();

            foreach (SectorMapPlan.Location location in SectorMapPlan.Locations)
            {
                Assert.IsTrue(
                    unique.Add(location.Id),
                    "Duplicate map identifier: " + location.Id);

                Assert.IsTrue(
                    SectorMapPlan.IsWithinWorld(location.MapPosition),
                    "Location outside the 8km world: " + location.Name);

                Assert.AreEqual(
                    2,
                    location.GridCell.Length,
                    "Location has an invalid grid cell: " + location.Name);
            }
        }

        [Test]
        public void CanonicalGridRunsWestToEastAndNorthToSouth()
        {
            Assert.AreEqual("A1", SectorMapPlan.GridCell(new Vector2(-3500, 3500)));
            Assert.AreEqual("H1", SectorMapPlan.GridCell(new Vector2(3500, 3500)));
            Assert.AreEqual("A8", SectorMapPlan.GridCell(new Vector2(-3500, -3500)));
            Assert.AreEqual("H8", SectorMapPlan.GridCell(new Vector2(3500, -3500)));
            Assert.AreEqual("B3", SectorMapPlan.FindById("village").GridCell);
            Assert.AreEqual("G5", SectorMapPlan.FindById("warehouse").GridCell);
        }

        [Test]
        public void PlannedRoadCorridorsRemainInsideMap()
        {
            Assert.GreaterOrEqual(SectorMapPlan.RoadRoutes.Count, 5);

            foreach (Vector2[] road in SectorMapPlan.RoadRoutes)
            {
                Assert.GreaterOrEqual(road.Length, 2);

                foreach (Vector2 point in road)
                {
                    Assert.IsTrue(SectorMapPlan.IsWithinWorld(point),
                        "Road coordinate outside 8km bounds: " + point);
                }
            }
        }
    }
}
