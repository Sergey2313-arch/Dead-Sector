using NUnit.Framework;
using UnityEngine;

namespace DeadSector.Tests
{
    /// <summary>Regression tests for mouse-click mining, chopping and building costs.</summary>
    public sealed class SectorResourceToolTests
    {
        [Test]
        public void TreeNeedsAxeAndOreNeedsPickaxe()
        {
            Assert.IsTrue(SectorResources.RequiresTool(SectorResourceType.Tree));
            Assert.IsTrue(SectorResources.RequiresTool(SectorResourceType.Ore));
            Assert.IsFalse(SectorResources.RequiresTool(SectorResourceType.GroundStone));

            Assert.IsTrue(SectorResources.CorrectTool(SectorResourceType.Tree, "stone_axe"));
            Assert.IsTrue(SectorResources.CorrectTool(SectorResourceType.Tree, "axe"));
            Assert.IsFalse(SectorResources.CorrectTool(SectorResourceType.Tree, "stone_pickaxe"));
            Assert.IsFalse(SectorResources.CorrectTool(SectorResourceType.Tree, "knife"));
            Assert.IsTrue(SectorResources.CorrectTool(SectorResourceType.Ore, "stone_pickaxe"));
            Assert.IsFalse(SectorResources.CorrectTool(SectorResourceType.Ore, "stone_axe"));
            Assert.IsFalse(SectorResources.CorrectTool(SectorResourceType.Ore, ""));
        }

        [Test]
        public void StandingTreeNeedsThreeStrikesAndOreNeedsFour()
        {
            var holder = new GameObject("HarvestTest");
            try
            {
                SectorResourceNode tree = holder.AddComponent<SectorResourceNode>();
                tree.Configure("tree_1", SectorResourceType.Tree, "wood", 5);
                Assert.AreEqual(3, tree.ToolHitsRemaining);
                Assert.AreEqual(2, tree.Strike());
                Assert.AreEqual(1, tree.Strike());
                Assert.AreEqual(0, tree.Strike());
                Assert.AreEqual(0, tree.Strike());

                var oreHolder = new GameObject("OreTest");
                oreHolder.transform.SetParent(holder.transform, false);
                SectorResourceNode ore = oreHolder.AddComponent<SectorResourceNode>();
                ore.Configure("ore_1", SectorResourceType.Ore, "scrap", 3);
                Assert.AreEqual(4, ore.ToolHitsRemaining);
                Assert.AreEqual(3, ore.Strike());
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void StonePickaxeCanBeCraftedAndEquippedAsMelee()
        {
            var inventory = new SectorInventory();
            Assert.IsTrue(inventory.Add("stone", 3));
            Assert.IsTrue(inventory.Add("stick", 2));
            Assert.IsTrue(inventory.Add("plant_fiber", 3));

            Assert.IsTrue(SectorCrafting.Craft(inventory, "cord"));
            Assert.IsTrue(SectorCrafting.Craft(inventory, "stone_pickaxe"));
            Assert.AreEqual(1, inventory.Count("stone_pickaxe"));
            Assert.AreEqual(SectorItemKind.Melee,
                SectorItems.Get("stone_pickaxe").Kind);
            Assert.AreEqual("Каменная кирка",
                SectorItems.Get("stone_pickaxe").Label);
        }

        [Test]
        public void ConstructionReportsMissingWoodAndCostsDoNotConsumeResources()
        {
            var inventory = new SectorInventory();
            SectorBuildSpecification foundation =
                SectorBuildCatalog.At((int)SectorBuildKind.Foundation);
            Assert.IsFalse(SectorBuildCatalog.CanAfford(inventory, foundation));
            StringAssert.Contains("НЕ ХВАТАЕТ",
                SectorBuildCatalog.MissingLabel(inventory, foundation));
            Assert.AreEqual(0, inventory.Count("wood"));

            Assert.IsTrue(inventory.Add("wood", 4));
            Assert.IsTrue(inventory.Add("stick", 2));
            Assert.IsTrue(SectorBuildCatalog.CanAfford(inventory, foundation));
            Assert.AreEqual("МАТЕРИАЛЫ СОБРАНЫ",
                SectorBuildCatalog.MissingLabel(inventory, foundation));
            Assert.AreEqual(4, inventory.Count("wood"));
            Assert.AreEqual(2, inventory.Count("stick"));
        }
    }
}
