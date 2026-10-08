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
            inventory.Add("wood", 2);
            inventory.Add("scrap", 1);

            Assert.IsTrue(SectorCrafting.Craft(inventory, "spear"));
            Assert.AreEqual(1, inventory.Count("spear"));
            Assert.Greater(
                SectorCombatRules.ForAttack("spear", true).Damage,
                SectorCombatRules.ForAttack("spear", false).Damage);
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
