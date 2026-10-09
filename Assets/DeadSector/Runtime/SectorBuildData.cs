using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadSector
{
    public enum SectorBuildKind
    {
        Foundation,
        Wall,
        Barricade,
        Storage,
        Door
    }

    [Serializable]
    public sealed class SectorBuildSnapshot
    {
        public string id = "";
        public SectorBuildKind kind;
        public Vector3 position;
        public float angle;
        public float health;
        public bool doorOpen;
        public List<SectorItemStack> storage =
            new List<SectorItemStack>();
    }

    public readonly struct SectorBuildSpecification
    {
        public readonly SectorBuildKind Kind;
        public readonly string Label;
        public readonly Vector3 Dimensions;
        public readonly float Health;
        public readonly string[] ItemIds;
        public readonly int[] Counts;

        public SectorBuildSpecification(SectorBuildKind kind, string label,
            Vector3 dimensions, float health,
            string[] itemIds, int[] counts)
        {
            Kind = kind;
            Label = label;
            Dimensions = dimensions;
            Health = health;
            ItemIds = itemIds;
            Counts = counts;
        }
    }

    /// <summary>
    /// Single authoritative set of build costs and footprints. Unity's
    /// art pass may replace primitives without changing this data.
    /// </summary>
    public static class SectorBuildCatalog
    {
        public const int MaxPieces = 200;

        static readonly SectorBuildSpecification[] Plans =
        {
            new SectorBuildSpecification(
                SectorBuildKind.Foundation, "Деревянное основание",
                new Vector3(3f, .25f, 3f), 170f,
                new[] { "wood", "stick" }, new[] { 4, 2 }),
            new SectorBuildSpecification(
                SectorBuildKind.Wall, "Деревянная стена",
                new Vector3(3f, 2.5f, .3f), 140f,
                new[] { "wood", "stick" }, new[] { 5, 2 }),
            new SectorBuildSpecification(
                SectorBuildKind.Barricade, "Баррикада",
                new Vector3(3f, 1.25f, .5f), 90f,
                new[] { "wood", "stone" }, new[] { 3, 2 }),
            new SectorBuildSpecification(
                SectorBuildKind.Storage, "Ящик для хранения",
                new Vector3(1.3f, 1.1f, 1.1f), 80f,
                new[] { "wood", "stick" }, new[] { 3, 1 }),
            new SectorBuildSpecification(
                SectorBuildKind.Door, "Дверь на петлях",
                new Vector3(3f, 2.6f, .4f), 120f,
                new[] { "wood", "stick" }, new[] { 4, 2 })
        };

        public static int Count => Plans.Length;

        public static SectorBuildSpecification At(int index) =>
            Plans[Mathf.Clamp(index, 0, Plans.Length - 1)];

        public static bool IsValid(SectorBuildKind kind) =>
            (int)kind >= 0 && (int)kind < Plans.Length;

        public static bool CanAfford(
            SectorInventory inventory, SectorBuildSpecification plan)
        {
            if (inventory == null)
                return false;

            for (int i = 0; i < plan.ItemIds.Length; i++)
                if (inventory.Count(plan.ItemIds[i]) < plan.Counts[i])
                    return false;
            return true;
        }

        public static bool Consume(
            SectorInventory inventory, SectorBuildSpecification plan)
        {
            if (!CanAfford(inventory, plan))
                return false;

            // All counts checked before the first removal.
            for (int i = 0; i < plan.ItemIds.Length; i++)
                inventory.Remove(plan.ItemIds[i], plan.Counts[i]);
            return true;
        }

        public static string CostLabel(SectorBuildSpecification plan)
        {
            string text = "";
            for (int i = 0; i < plan.ItemIds.Length; i++)
            {
                if (i > 0) text += "  +  ";
                text += SectorItems.Get(plan.ItemIds[i]).Label +
                    " x" + plan.Counts[i];
            }
            return text;
        }
    }
}
