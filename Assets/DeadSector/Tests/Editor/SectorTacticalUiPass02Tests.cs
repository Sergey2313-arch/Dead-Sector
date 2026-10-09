using NUnit.Framework;
using UnityEngine;

namespace DeadSector.Tests
{
    public sealed class SectorTacticalUiPass02Tests
    {
        [Test]
        public void NorthUpMapProjectsEastRightAndNorthUp()
        {
            Vector2 center = new Vector2(20f, -30f);
            float diameter = 340f;
            Vector2 middle = SectorTacticalMapRaster.Project(
                center, center, diameter);
            Vector2 north = SectorTacticalMapRaster.Project(
                center + Vector2.up * 85f, center, diameter);
            Vector2 east = SectorTacticalMapRaster.Project(
                center + Vector2.right * 85f, center, diameter);

            Assert.AreEqual(.5f, middle.x, .001f);
            Assert.AreEqual(.5f, middle.y, .001f);
            Assert.AreEqual(.75f, north.y, .001f);
            Assert.AreEqual(.75f, east.x, .001f);
        }

        [Test]
        public void RasterFillsAnOpaqueReadoutEvenWithoutLoadedUnityTerrain()
        {
            var pixels = new Color32[64];
            SectorTacticalMapRaster.Write(pixels,
                new Vector2(0f, 0f), 100f, 8);

            foreach (Color32 pixel in pixels)
                Assert.AreEqual(255, pixel.a,
                    "Map colors are independent of day/night scene lighting.");
        }

        [Test]
        public void InventoryIconsProduceUniqueCachedSprites()
        {
            string[] ids =
            {
                "water", "food", "bandage", "medkit",
                "scrap", "wood", "cloth", "stone", "stick",
                "plant_fiber", "cotton", "cord",
                "knife", "stone_knife", "stone_axe", "wood_club",
                "axe", "spear", "torch", "pistol", "rifle",
                "9mm", "556", "wood_helmet", "wood_vest",
                "wood_leggings", "cotton_hood", "cotton_shirt",
                "cotton_pants", "cotton_boots", "cotton_bag", "fists"
            };

            try
            {
                foreach (string id in ids)
                {
                    Sprite first = SectorItemIcons.Get(id);
                    Assert.IsNotNull(first, id);
                    Assert.AreEqual(48f, first.rect.width, .001f);
                    Assert.AreSame(first, SectorItemIcons.Get(id));
                }

                Assert.AreNotSame(
                    SectorItemIcons.Get("water"),
                    SectorItemIcons.Get("medkit"));
            }
            finally
            {
                SectorItemIcons.Release();
            }
        }

        [Test]
        public void CompassWrapsCorrectlyAcrossNorth()
        {
            Assert.AreEqual(3f,
                SectorCompass.RelativeAngle(359f, 2f), .001f);
            Assert.AreEqual(-3f,
                SectorCompass.RelativeAngle(2f, 359f), .001f);
            Assert.AreEqual("N", SectorCompass.CardinalName(360));
            Assert.AreEqual("E", SectorCompass.CardinalName(90));
        }
    }
}
