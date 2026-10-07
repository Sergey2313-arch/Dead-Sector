using NUnit.Framework;
using UnityEngine;
namespace DeadSector.Tests
{
    public class SectorLayoutTests
    {
        [Test] public void WorldCoversExactlyEightKilometresWithoutGaps()
        {
            Assert.AreEqual(-4000, SectorLayout.Origin(Vector2Int.zero).x);
            Assert.AreEqual(4000, SectorLayout.Origin(new Vector2Int(7, 7)).x + SectorLayout.TileSize);
            for (int x = 0; x < 7; x++)
                Assert.AreEqual(SectorLayout.Origin(new Vector2Int(x, 0)).x + SectorLayout.TileSize,
                    SectorLayout.Origin(new Vector2Int(x + 1, 0)).x);
        }
        [Test] public void TileIndexHandlesNegativePositionsAndWorldEdges()
        {
            Assert.AreEqual(new Vector2Int(0, 0), SectorLayout.TileAt(new Vector3(-4000, 0, -4000)));
            Assert.AreEqual(new Vector2Int(3, 3), SectorLayout.TileAt(new Vector3(-.01f, 0, -.01f)));
            Assert.AreEqual(new Vector2Int(4, 4), SectorLayout.TileAt(Vector3.zero));
            Assert.AreEqual(new Vector2Int(7, 7), SectorLayout.TileAt(new Vector3(4000, 0, 4000)));
        }
        [Test] public void StartingSettlementAndRoadsAreFlatAndAllTerrainFitsHeightRange()
        {
            Assert.AreEqual(60, SectorLayout.Height(30, 20), .001f);
            Assert.AreEqual(60, SectorLayout.Height(220, 230), .001f);
            for (int x = -4000; x <= 4000; x += 100)
                for (int z = -4000; z <= 4000; z += 100)
                {
                    float h = SectorLayout.Height(x, z);
                    Assert.IsFalse(float.IsNaN(h)); Assert.That(h, Is.InRange(0f, 500f));
                    if (z == 0 || x == 300) Assert.AreEqual(60, h, .001f);
                }
        }
    }
}
