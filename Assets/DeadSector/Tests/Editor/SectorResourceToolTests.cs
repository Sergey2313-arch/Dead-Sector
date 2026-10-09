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
        public void WorldPineCanBeRegisteredForAxeHarvesting()
        {
            var root = new GameObject("TilePine");
            var registry = new GameObject("HarvestRegistry");
            try
            {
                var node = root.AddComponent<SectorResourceNode>();
                node.Configure("pine_4_4_27", SectorResourceType.Tree,
                    "wood", 5);
                var resources = registry.AddComponent<SectorResources>();
                resources.RegisterWorldTree(node);

                Assert.IsTrue(SectorResources.CorrectTool(
                    node.Type, "stone_axe"));
                Assert.IsFalse(SectorResources.CorrectTool(
                    node.Type, "stone_knife"));
                Assert.AreEqual(3, node.ToolHitsRemaining);
                Assert.IsFalse(resources.WasHarvested(node.Id));
                Assert.AreEqual("pine_4_4_27", node.Id);
                Assert.AreEqual("wood", node.ItemId);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(registry);
            }
        }

        [Test]
        public void WorldPineCanBeChoppedWithAxeFacingTheTrunk()
        {
            var playerRoot = new GameObject("HarvestPlayer");
            var treeRoot = new GameObject("PineForChopping");
            var registryRoot = new GameObject("HarvestSystem");
            try
            {
                // No gameplay Update or camera needed for deterministic test.
                playerRoot.SetActive(false);
                playerRoot.AddComponent<CharacterController>();
                var player = playerRoot.AddComponent<SectorPlayer>();
                treeRoot.transform.position = new Vector3(0f, 0f, 2f);
                var tree = treeRoot.AddComponent<SectorResourceNode>();
                tree.Configure("pine_4_4_19", SectorResourceType.Tree,
                    "wood", 5);
                var resources = registryRoot.AddComponent<SectorResources>();
                resources.player = player;
                resources.RegisterWorldTree(tree);
                var bag = new SectorInventory();

                Assert.AreSame(tree,
                    resources.NearbyToolNode(Vector3.forward));

                string collected, warning;
                Assert.IsTrue(resources.StrikeNearest(
                    "stone_axe", bag, Vector3.forward,
                    out collected, out warning));
                Assert.AreEqual(2, tree.ToolHitsRemaining);
                StringAssert.Contains("Рубим дерево", warning);
                Assert.AreEqual(0, bag.Count("wood"));
            }
            finally
            {
                Object.DestroyImmediate(registryRoot);
                Object.DestroyImmediate(treeRoot);
                Object.DestroyImmediate(playerRoot);
            }
        }

        [Test]
        public void ChoppedTreeSplitsYieldBetweenBagAndPersistentGroundCache()
        {
            var nodeObject = new GameObject("ChoppedPine");
            var resourceObject = new GameObject("ResourceRegistry");
            try
            {
                var pine = nodeObject.AddComponent<SectorResourceNode>();
                pine.Configure("pine_4_4_44", SectorResourceType.Tree, "wood", 5);
                var resources = resourceObject.AddComponent<SectorResources>();
                resources.RegisterWorldTree(pine);

                // Same scenario as screenshot: 1 kg of free capacity,
                // five 1-kg logs should produce 1 carried + 4 on ground.
                var bag = new SectorInventory(10, 3f);
                Assert.IsTrue(bag.Add("wood", 2));
                var spilled = new System.Collections.Generic.List<SectorItemStack>();
                resources.StoreOverflow = (node, remaining) =>
                {
                    Assert.AreSame(pine, node);
                    spilled.AddRange(remaining);
                    return true;
                };

                Assert.IsTrue(resources.HarvestToolNode(pine, bag, out string label));
                Assert.AreEqual(3, bag.Count("wood"));
                Assert.AreEqual(1, spilled.Count);
                Assert.AreEqual("wood", spilled[0].id);
                Assert.AreEqual(4, spilled[0].count);
                Assert.IsTrue(resources.WasHarvested("pine_4_4_44"));
                StringAssert.Contains("На земле", label);
            }
            finally
            {
                Object.DestroyImmediate(nodeObject);
                Object.DestroyImmediate(resourceObject);
            }
        }

        [Test]
        public void NoOverflowStoragePreservesTreeAndItemsWithoutLoss()
        {
            var nodeObject = new GameObject("PreservedPine");
            var resourceObject = new GameObject("ResourceRegistry");
            try
            {
                var pine = nodeObject.AddComponent<SectorResourceNode>();
                pine.Configure("pine_4_4_45", SectorResourceType.Tree, "wood", 5);
                var resources = resourceObject.AddComponent<SectorResources>();
                resources.RegisterWorldTree(pine);
                var bag = new SectorInventory(10, 2f);
                Assert.IsTrue(bag.Add("wood", 2));

                Assert.IsFalse(resources.HarvestToolNode(pine, bag, out _));
                Assert.AreEqual(2, bag.Count("wood"));
                Assert.IsFalse(resources.WasHarvested(pine.Id));
            }
            finally
            {
                Object.DestroyImmediate(nodeObject);
                Object.DestroyImmediate(resourceObject);
            }
        }

        [Test]
        public void HarvestCachePositionAndContentsSurviveVersionOneSaveJson()
        {
            var state = new SectorGameSave();
            state.containers.Add(new SectorContainerSnapshot
            {
                id = "harvest_pine_4_4_44",
                position = new Vector3(73.5f, 0f, -141.25f),
                items = new System.Collections.Generic.List<SectorItemStack>
                {
                    new SectorItemStack("wood", 4)
                }
            });
            string json = JsonUtility.ToJson(state);
            var restored = JsonUtility.FromJson<SectorGameSave>(json);
            Assert.AreEqual(1, restored.version);
            Assert.AreEqual("harvest_pine_4_4_44", restored.containers[0].id);
            Assert.AreEqual(new Vector3(73.5f, 0f, -141.25f),
                restored.containers[0].position);
            Assert.AreEqual(4, restored.containers[0].items[0].count);
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
            StringAssert.Contains("МАТЕРИАЛЫ СОБРАНЫ",
                SectorBuildCatalog.MissingLabel(inventory, foundation));
            Assert.AreEqual(4, inventory.Count("wood"));
            Assert.AreEqual(2, inventory.Count("stick"));
        }
    }
}
