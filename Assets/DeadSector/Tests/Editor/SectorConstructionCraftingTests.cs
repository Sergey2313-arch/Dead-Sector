using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DeadSector.Tests
{
    public sealed class SectorConstructionCraftingTests
    {
        [Test]
        public void NewInputSystemRecognizesAllConstructionHotkeys()
        {
            // Regression: New Input System used to silently drop B/Q/4/5
            // even while C crafting and 1-3 weapon slots worked.
            Assert.IsTrue(SectorInput.HasNewInputBinding(KeyCode.B),
                "B must open construction mode.");
            Assert.IsTrue(SectorInput.HasNewInputBinding(KeyCode.Q),
                "Q must rotate placement.");
            Assert.IsTrue(SectorInput.HasNewInputBinding(KeyCode.E),
                "E must rotate placement.");
            for (int i = 0; i < SectorBuildCatalog.Count; i++)
                Assert.IsTrue(SectorInput.HasNewInputBinding(
                    KeyCode.Alpha1 + i),
                    "Every construction plan needs a working 1-5 hotkey.");
        }

        [Test]
        public void AllFivePlansHaveVisibleRussianCraftableKitsAtTop()
        {
            Assert.AreEqual(5, SectorBuildCatalog.Count);
            for (int i = 0; i < SectorBuildCatalog.Count; i++)
            {
                SectorBuildSpecification plan = SectorBuildCatalog.At(i);
                string kitId = SectorBuildCatalog.KitId(plan.Kind);
                Assert.IsFalse(string.IsNullOrEmpty(kitId));
                Assert.AreEqual(kitId, SectorCrafting.Recipes[i].Id,
                    "Construction recipes must be FIRST in the C menu.");
                Assert.AreEqual(kitId, SectorCrafting.Recipes[i].OutputId);
                Assert.AreEqual(3, SectorCrafting.Recipes[i].Tier);
                Assert.AreEqual(1, SectorCrafting.Recipes[i].OutputCount);
                Assert.AreEqual(SectorRussian.ItemName(kitId),
                    SectorItems.Get(kitId).Label);
                Assert.IsNotNull(SectorItemIcons.Get(kitId));
            }
            SectorItemIcons.Release();
        }

        [Test]
        public void KitRecipesMatchExactlyTheAuthoritativeBuildCosts()
        {
            for (int i = 0; i < SectorBuildCatalog.Count; i++)
            {
                SectorBuildSpecification plan = SectorBuildCatalog.At(i);
                SectorRecipe recipe = SectorCrafting.Find(
                    SectorBuildCatalog.KitId(plan.Kind));
                Assert.IsNotNull(recipe);
                Assert.AreEqual(plan.ItemIds.Length,
                    recipe.Ingredients.Length);
                for (int k = 0; k < plan.ItemIds.Length; k++)
                {
                    Assert.AreEqual(plan.ItemIds[k],
                        recipe.Ingredients[k].ItemId);
                    Assert.AreEqual(plan.Counts[k],
                        recipe.Ingredients[k].Count);
                }
            }
        }

        [Test]
        public void CraftedKitsPlaceWithoutPayingTwice()
        {
            foreach (SectorBuildKind kind in Enum.GetValues(typeof(SectorBuildKind)))
            {
                SectorBuildSpecification plan = SectorBuildCatalog.At((int)kind);
                var inventory = new SectorInventory();
                for (int k = 0; k < plan.ItemIds.Length; k++)
                    Assert.IsTrue(inventory.Add(plan.ItemIds[k],
                        plan.Counts[k]), kind.ToString());
                string kitId = SectorBuildCatalog.KitId(kind);
                Assert.IsTrue(SectorCrafting.Craft(inventory, kitId),
                    "Cannot craft " + kitId);
                Assert.AreEqual(1, inventory.Count(kitId));
                for (int k = 0; k < plan.ItemIds.Length; k++)
                    Assert.AreEqual(0, inventory.Count(plan.ItemIds[k]));
                Assert.IsTrue(SectorBuildCatalog.CanAfford(inventory, plan));
                Assert.IsTrue(SectorBuildCatalog.Consume(inventory, plan));
                Assert.AreEqual(0, inventory.Count(kitId));
                Assert.IsFalse(SectorBuildCatalog.Consume(inventory, plan),
                    "Cannot build twice with a single kit.");
            }
        }

        [Test]
        public void PreferKitAndPreserveRawMaterialsWhenBothArePresent()
        {
            SectorBuildSpecification plan = SectorBuildCatalog.At(
                (int)SectorBuildKind.Wall);
            var inventory = new SectorInventory();
            Assert.IsTrue(inventory.Add("wood", 5));
            Assert.IsTrue(inventory.Add("stick", 2));
            Assert.IsTrue(inventory.Add("build_wall", 1));
            Assert.IsTrue(SectorBuildCatalog.Consume(inventory, plan));
            Assert.AreEqual(0, inventory.Count("build_wall"));
            Assert.AreEqual(5, inventory.Count("wood"));
            Assert.AreEqual(2, inventory.Count("stick"));
        }

        [Test]
        public void ExistingDirectConstructionStillConsumesItsOriginalCosts()
        {
            SectorBuildSpecification plan = SectorBuildCatalog.At(
                (int)SectorBuildKind.Foundation);
            var inventory = new SectorInventory();
            inventory.Add("wood", 4);
            inventory.Add("stick", 2);
            Assert.IsTrue(SectorBuildCatalog.CanAfford(inventory, plan));
            Assert.IsTrue(SectorBuildCatalog.Consume(inventory, plan));
            Assert.AreEqual(0, inventory.Count("wood"));
            Assert.AreEqual(0, inventory.Count("stick"));
        }

        [Test]
        public void CraftedKitRoundTripsThroughExistingSaveInventoryJson()
        {
            var save = new SectorGameSave
            {
                inventory = new List<SectorItemStack>
                {
                    new SectorItemStack("build_foundation", 2),
                    new SectorItemStack("build_storage", 1)
                }
            };
            SectorGameSave restored = JsonUtility.FromJson<SectorGameSave>(
                JsonUtility.ToJson(save));
            var bag = new SectorInventory();
            bag.Import(restored.inventory);
            Assert.AreEqual(2, bag.Count("build_foundation"));
            Assert.AreEqual(1, bag.Count("build_storage"));
        }
    }
}
