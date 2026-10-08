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
        public readonly int Tier;
        public readonly string OutputId;
        public readonly int OutputCount;
        public readonly SectorIngredient[] Ingredients;

        public SectorRecipe(
            string id, string name, string outputId,
            int outputCount, params SectorIngredient[] ingredients)
        {
            Id = id;
            Name = name;
            Tier = id.StartsWith("cotton_") || id == "cloth_cotton" ? 2 :
                id.StartsWith("wood_") || id == "hatchet" ? 1 : 0;
            OutputId = outputId;
            OutputCount = outputCount;
            Ingredients = ingredients;
        }
    }

    public static class SectorCrafting
    {
        // Survival progression: collect stones and dry sticks first, then
        // spin fiber into bindings, then unlock wooden or cotton gear.
        static readonly SectorRecipe[] RecipesInternal =
        {
            new SectorRecipe("cord", "Twist Plant Cord", "cord", 1,
                new SectorIngredient("plant_fiber", 3)),
            new SectorRecipe("stone_knife", "Stone Knife", "stone_knife", 1,
                new SectorIngredient("stone", 2),
                new SectorIngredient("stick", 1),
                new SectorIngredient("plant_fiber", 2)),
            new SectorRecipe("stone_axe", "Stone Axe", "stone_axe", 1,
                new SectorIngredient("stone", 3),
                new SectorIngredient("stick", 2),
                new SectorIngredient("cord", 1)),
            new SectorRecipe("wood_club", "Wooden Club", "wood_club", 1,
                new SectorIngredient("stick", 3)),
            new SectorRecipe("spear", "Wooden Spear", "spear", 1,
                new SectorIngredient("stick", 3),
                new SectorIngredient("stone", 1),
                new SectorIngredient("cord", 1)),
            new SectorRecipe("torch", "Hand Torch", "torch", 1,
                new SectorIngredient("stick", 1),
                new SectorIngredient("plant_fiber", 3)),
            new SectorRecipe("wood_helmet", "Wooden Head Guard", "wood_helmet", 1,
                new SectorIngredient("wood", 2),
                new SectorIngredient("cord", 1)),
            new SectorRecipe("wood_vest", "Wooden Chest Guard", "wood_vest", 1,
                new SectorIngredient("wood", 4),
                new SectorIngredient("cord", 2)),
            new SectorRecipe("wood_leggings", "Wooden Leg Guards", "wood_leggings", 1,
                new SectorIngredient("wood", 3),
                new SectorIngredient("cord", 2)),
            new SectorRecipe("cloth_cotton", "Weave Cotton Fabric", "cloth", 2,
                new SectorIngredient("cotton", 4)),
            new SectorRecipe("cotton_hood", "Cotton Hood", "cotton_hood", 1,
                new SectorIngredient("cloth", 2),
                new SectorIngredient("cord", 1)),
            new SectorRecipe("cotton_shirt", "Cotton Shirt", "cotton_shirt", 1,
                new SectorIngredient("cloth", 4),
                new SectorIngredient("cord", 2)),
            new SectorRecipe("cotton_pants", "Cotton Pants", "cotton_pants", 1,
                new SectorIngredient("cloth", 3),
                new SectorIngredient("cord", 2)),
            new SectorRecipe("cotton_boots", "Cotton Footwraps", "cotton_boots", 1,
                new SectorIngredient("cloth", 2),
                new SectorIngredient("cord", 1)),
            new SectorRecipe("bandage", "Make Bandage", "bandage", 1,
                new SectorIngredient("cloth", 2)),
            new SectorRecipe("medkit", "Assemble Medkit", "medkit", 1,
                new SectorIngredient("bandage", 3),
                new SectorIngredient("cloth", 2)),
            new SectorRecipe("hatchet", "Metal Hatchet", "axe", 1,
                new SectorIngredient("scrap", 3),
                new SectorIngredient("wood", 1))
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
