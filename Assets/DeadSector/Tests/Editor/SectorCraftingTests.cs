using NUnit.Framework;

namespace DeadSector.Tests
{
    public sealed class SectorCraftingTests
    {
        [Test]
        public void BandageCraftConsumesExactlyTwoCloth()
        {
            var inventory = new SectorInventory();
            Assert.IsTrue(inventory.Add("cloth", 4));

            Assert.IsTrue(SectorCrafting.Craft(inventory, "bandage"));
            Assert.AreEqual(2, inventory.Count("cloth"));
            Assert.AreEqual(1, inventory.Count("bandage"));
        }

        [Test]
        public void CraftingRejectsMissingMaterialsWithoutMutation()
        {
            var inventory = new SectorInventory();
            Assert.IsTrue(inventory.Add("wood", 2));

            Assert.IsFalse(SectorCrafting.Craft(inventory, "spear"));
            Assert.AreEqual(2, inventory.Count("wood"));
            Assert.AreEqual(0, inventory.Count("spear"));
        }

        [Test]
        public void CraftedSpearUsesNewMeleeRules()
        {
            var inventory = new SectorInventory();
            inventory.Add("stick", 3);
            inventory.Add("stone", 1);
            inventory.Add("cord", 1);

            Assert.IsTrue(SectorCrafting.Craft(inventory, "spear"));
            Assert.AreEqual(1, inventory.Count("spear"));
            Assert.Greater(
                SectorCombatRules.ForAttack("spear", true).Damage,
                SectorCombatRules.ForAttack("spear", false).Damage);
        }

        [Test]
        public void GroundResourcesProduceStoneAgeKnifeAndAxe()
        {
            var inventory = new SectorInventory();
            inventory.Add("stone", 7);
            inventory.Add("stick", 5);
            inventory.Add("plant_fiber", 5);

            Assert.IsTrue(SectorCrafting.Craft(inventory, "stone_knife"));
            Assert.IsTrue(SectorCrafting.Craft(inventory, "cord"));
            Assert.IsTrue(SectorCrafting.Craft(inventory, "stone_axe"));

            Assert.AreEqual(1, inventory.Count("stone_knife"));
            Assert.AreEqual(1, inventory.Count("stone_axe"));
        }

        [Test]
        public void CottonProgressionRequiresFabricBeforeClothes()
        {
            var inventory = new SectorInventory();
            inventory.Add("cotton", 8);
            inventory.Add("cord", 2);

            Assert.IsFalse(SectorCrafting.Craft(inventory, "cotton_hood"));
            Assert.IsTrue(SectorCrafting.Craft(inventory, "cloth_cotton"));
            Assert.IsTrue(SectorCrafting.Craft(inventory, "cotton_hood"));
            Assert.AreEqual(1, inventory.Count("cotton_hood"));
        }

        [Test]
        public void RecipeOutputMustExistInItemCatalog()
        {
            foreach (SectorRecipe recipe in SectorCrafting.Recipes)
            {
                Assert.IsTrue(SectorItems.TryGet(recipe.OutputId, out _));
                foreach (SectorIngredient ingredient in recipe.Ingredients)
                {
                    Assert.IsTrue(
                        SectorItems.TryGet(ingredient.ItemId, out _));
                    Assert.Greater(ingredient.Count, 0);
                }
            }
        }
    }
}
