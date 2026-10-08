using System;
using System.Collections.Generic;

namespace DeadSector
{
    public readonly struct SectorIngredient
    {
        public readonly string ItemId;
        public readonly int Count;

        public SectorIngredient(string itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }
    }

    public sealed class SectorRecipe
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string OutputId;
        public readonly int OutputCount;
        public readonly SectorIngredient[] Ingredients;

        public SectorRecipe(
            string id, string name, string outputId,
            int outputCount, params SectorIngredient[] ingredients)
        {
            Id = id;
            Name = name;
            OutputId = outputId;
            OutputCount = outputCount;
            Ingredients = ingredients;
        }
    }

    public static class SectorCrafting
    {
        static readonly SectorRecipe[] RecipesInternal =
        {
            new SectorRecipe(
                "bandage", "Make Bandage", "bandage", 1,
                new SectorIngredient("cloth", 2)),
            new SectorRecipe(
                "medkit", "Assemble Medkit", "medkit", 1,
                new SectorIngredient("bandage", 3),
                new SectorIngredient("cloth", 2)),
            new SectorRecipe(
                "hatchet", "Build Hatchet", "axe", 1,
                new SectorIngredient("scrap", 3),
                new SectorIngredient("wood", 1)),
            new SectorRecipe(
                "spear", "Build Wooden Spear", "spear", 1,
                new SectorIngredient("wood", 2),
                new SectorIngredient("scrap", 1)),
            new SectorRecipe(
                "torch", "Make Hand Torch", "torch", 1,
                new SectorIngredient("wood", 1),
                new SectorIngredient("cloth", 1))
        };

        public static IReadOnlyList<SectorRecipe> Recipes => RecipesInternal;

        public static SectorRecipe Find(string id)
        {
            foreach (SectorRecipe recipe in RecipesInternal)
                if (string.Equals(recipe.Id, id, StringComparison.Ordinal))
                    return recipe;

            return null;
        }

        public static bool CanCraft(SectorInventory inventory, SectorRecipe recipe)
        {
            if (inventory == null || recipe == null)
                return false;

            foreach (SectorIngredient ingredient in recipe.Ingredients)
            {
                if (inventory.Count(ingredient.ItemId) < ingredient.Count)
                    return false;
            }

            // The finished item may fit only after ingredients were removed.
            var simulation = new SectorInventory(inventory.SlotLimit, inventory.MaxWeight);
            simulation.Import(inventory.Export());

            foreach (SectorIngredient ingredient in recipe.Ingredients)
                if (!simulation.Remove(ingredient.ItemId, ingredient.Count))
                    return false;

            return simulation.CanAdd(recipe.OutputId, recipe.OutputCount);
        }

        public static bool Craft(SectorInventory inventory, string recipeId)
        {
            SectorRecipe recipe = Find(recipeId);
            if (!CanCraft(inventory, recipe))
                return false;

            List<SectorItemStack> snapshot = inventory.Export();

            foreach (SectorIngredient ingredient in recipe.Ingredients)
            {
                if (!inventory.Remove(ingredient.ItemId, ingredient.Count))
                {
                    inventory.Import(snapshot);
                    return false;
                }
            }

            if (!inventory.Add(recipe.OutputId, recipe.OutputCount))
            {
                inventory.Import(snapshot);
                return false;
            }

            return true;
        }
    }
}
