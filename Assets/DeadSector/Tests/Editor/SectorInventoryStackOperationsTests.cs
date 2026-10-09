using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DeadSector.Tests
{
    public sealed class SectorInventoryStackOperationsTests
    {
        [Test]
        public void SplitPreservesItemCountWeightAndDistinctStacksAcrossSaveImport()
        {
            var inventory = new SectorInventory(4, 35f);
            Assert.IsTrue(inventory.Add("bandage", 5));

            float beforeWeight = inventory.Weight;
            Assert.IsTrue(inventory.SplitStack(0, 2));
            Assert.AreEqual(2, inventory.UsedSlots);
            Assert.AreEqual(3, inventory.Stacks[0].count);
            Assert.AreEqual(2, inventory.Stacks[1].count);
            Assert.AreEqual(5, inventory.Count("bandage"));
            Assert.AreEqual(beforeWeight, inventory.Weight, .0001f);

            List<SectorItemStack> snapshot = inventory.Export();
            var restored = new SectorInventory(4, 35f);
            restored.Import(snapshot);

            Assert.AreEqual(2, restored.UsedSlots,
                "Saving and loading must preserve a player's split stacks.");
            Assert.AreEqual(3, restored.Stacks[0].count);
            Assert.AreEqual(2, restored.Stacks[1].count);
            Assert.AreEqual(5, restored.Count("bandage"));
            Assert.AreEqual(beforeWeight, restored.Weight, .0001f);
        }

        [Test]
        public void SplitRejectsFullBagsAndInvalidAmountsWithoutMutation()
        {
            var inventory = new SectorInventory(1, 35f);
            Assert.IsTrue(inventory.Add("water", 4));

            Assert.IsFalse(inventory.SplitStack(0, 2));
            Assert.IsFalse(inventory.SplitStack(0, 0));
            Assert.IsFalse(inventory.SplitStack(0, 4));
            Assert.IsFalse(inventory.SplitStack(-1, 1));
            Assert.AreEqual(1, inventory.UsedSlots);
            Assert.AreEqual(4, inventory.Count("water"));

            var bigger = new SectorInventory(2, 35f);
            Assert.IsTrue(bigger.Add("wood_helmet", 1));
            Assert.IsFalse(bigger.SplitStack(0, 1),
                "Single-instance armor may not be divided.");
        }

        [Test]
        public void SortingChangesOnlyOrderNotTotalsOrWeight()
        {
            var inventory = new SectorInventory(8, 35f);
            Assert.IsTrue(inventory.Add("scrap", 2));
            Assert.IsTrue(inventory.Add("water", 2));
            Assert.IsTrue(inventory.Add("bandage", 3));
            Assert.IsTrue(inventory.SplitStack(2, 1));

            List<SectorItemStack> before = inventory.Export();
            float beforeWeight = inventory.Weight;
            inventory.SortStacks();

            Assert.AreEqual(before.Count, inventory.Stacks.Count);
            Assert.AreEqual(beforeWeight, inventory.Weight, .0001f);
            Assert.AreEqual(2, inventory.Count("scrap"));
            Assert.AreEqual(2, inventory.Count("water"));
            Assert.AreEqual(3, inventory.Count("bandage"));
            Assert.AreEqual("water", inventory.Stacks[0].id);
            Assert.AreEqual("bandage", inventory.Stacks[1].id);
            Assert.AreEqual("bandage", inventory.Stacks[2].id);
            Assert.AreEqual("scrap", inventory.Stacks[3].id);
        }

        [Test]
        public void ImportRejectsOverweightAndOverflowButPreservesLegalSplits()
        {
            var inventory = new SectorInventory(3, 2f);
            inventory.Import(new[]
            {
                new SectorItemStack("bandage", 2),
                new SectorItemStack("bandage", 2),
                new SectorItemStack("rifle", 1),
                new SectorItemStack("unknown_invalid", 900)
            });

            Assert.AreEqual(2, inventory.UsedSlots);
            Assert.AreEqual(4, inventory.Count("bandage"));
            Assert.AreEqual(0, inventory.Count("rifle"));
            Assert.LessOrEqual(inventory.Weight, inventory.MaxWeight);
        }

        [Test]
        public void JournalEscClosesBeforeOpeningPause()
        {
            Assert.AreEqual(
                SectorEscapeAction.CloseJournal,
                SectorMenuRules.OnEscape(false, false,
                    false, false, false, true));
            Assert.AreEqual(
                SectorEscapeAction.CloseJournal,
                SectorMenuRules.OnEscape(false, false,
                    true, false, false, true));
            Assert.AreEqual(
                SectorEscapeAction.OpenPause,
                SectorMenuRules.OnEscape(false, false,
                    false, false, false, false));
        }
    }
}
