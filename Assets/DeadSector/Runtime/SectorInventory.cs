using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadSector
{
    public enum SectorItemKind { Food, Drink, Medical, Material, Melee, Firearm, Ammunition }

    // Game items have no RPG quality tiers: condition/durability can be
    // implemented later as individual-instance state, not colored rarity.
    public readonly struct SectorItemDefinition
    {
        public readonly string Id;
        public readonly string Label;
        public readonly SectorItemKind Kind;
        public readonly float Weight;
        public readonly int MaxStack;
        public readonly float HungerRestore;
        public readonly float ThirstRestore;
        public readonly float HealthRestore;
        public readonly float Damage;

        public SectorItemDefinition(
            string id, string label, SectorItemKind kind,
            float weight, int maxStack, float hunger = 0f,
            float thirst = 0f, float health = 0f, float damage = 0f)
        {
            Id = id; Label = label; Kind = kind;
            Weight = weight; MaxStack = maxStack;
            HungerRestore = hunger; ThirstRestore = thirst;
            HealthRestore = health; Damage = damage;
        }

        public bool IsConsumable =>
            HungerRestore > 0f || ThirstRestore > 0f || HealthRestore > 0f;
    }

    public static class SectorItems
    {
        static readonly Dictionary<string, SectorItemDefinition> Catalog =
            new Dictionary<string, SectorItemDefinition>(StringComparer.Ordinal)
            {
                { "water", new SectorItemDefinition("water", "Bottled Water", SectorItemKind.Drink, .55f, 6, thirst: 35f) },
                { "food", new SectorItemDefinition("food", "Canned Food", SectorItemKind.Food, .45f, 8, hunger: 30f) },
                { "bandage", new SectorItemDefinition("bandage", "Bandage", SectorItemKind.Medical, .15f, 6, health: 20f) },
                { "medkit", new SectorItemDefinition("medkit", "Medical Kit", SectorItemKind.Medical, .75f, 2, health: 65f) },
                { "scrap", new SectorItemDefinition("scrap", "Metal Scrap", SectorItemKind.Material, 1.4f, 12) },
                { "wood", new SectorItemDefinition("wood", "Timber", SectorItemKind.Material, 1.0f, 10) },
                { "cloth", new SectorItemDefinition("cloth", "Fabric", SectorItemKind.Material, .18f, 12) },
                { "knife", new SectorItemDefinition("knife", "Field Knife", SectorItemKind.Melee, .45f, 1, damage: 25f) },
                { "axe", new SectorItemDefinition("axe", "Hatchet", SectorItemKind.Melee, 1.3f, 1, damage: 38f) },
                { "spear", new SectorItemDefinition("spear", "Improvised Spear", SectorItemKind.Melee, 1.15f, 1, damage: 31f) },
                { "torch", new SectorItemDefinition("torch", "Hand Torch", SectorItemKind.Melee, .35f, 1, damage: 15f) },
                { "pistol", new SectorItemDefinition("pistol", "Pistol", SectorItemKind.Firearm, 1.15f, 1, damage: 31f) },
                { "rifle", new SectorItemDefinition("rifle", "Rifle", SectorItemKind.Firearm, 3.6f, 1, damage: 44f) },
                { "9mm", new SectorItemDefinition("9mm", "9mm Ammunition", SectorItemKind.Ammunition, .012f, 60) },
                { "556", new SectorItemDefinition("556", "5.56 Ammunition", SectorItemKind.Ammunition, .014f, 60) }
            };

        public static bool TryGet(string id, out SectorItemDefinition item) =>
            Catalog.TryGetValue(id ?? string.Empty, out item);

        public static SectorItemDefinition Get(string id)
        {
            if (!TryGet(id, out SectorItemDefinition item))
                throw new ArgumentException("Unknown item ID: " + id, nameof(id));

            return item;
        }
    }

    [Serializable]
    public sealed class SectorItemStack
    {
        public string id;
        public int count;

        public SectorItemStack(string id, int count)
        {
            this.id = id;
            this.count = count;
        }
    }

    /// <summary>
    /// Deterministic inventory rules, independent of MonoBehaviour and UI.
    /// All changes are transactional; capacity failures leave state intact.
    /// </summary>
    public sealed class SectorInventory
    {
        public const int DefaultSlots = 22;
        public const float DefaultMaxWeight = 35f;

        readonly List<SectorItemStack> stacks = new List<SectorItemStack>();

        public IReadOnlyList<SectorItemStack> Stacks => stacks;
        public int SlotLimit { get; }
        public float MaxWeight { get; }
        public int UsedSlots => stacks.Count;

        public float Weight
        {
            get
            {
                float total = 0f;

                foreach (SectorItemStack stack in stacks)
                {
                    if (SectorItems.TryGet(stack.id, out SectorItemDefinition item))
                        total += item.Weight * stack.count;
                }

                return total;
            }
        }

        public SectorInventory(int slotLimit = DefaultSlots, float maxWeight = DefaultMaxWeight)
        {
            SlotLimit = Mathf.Max(1, slotLimit);
            MaxWeight = Mathf.Max(.1f, maxWeight);
        }

        public int Count(string id)
        {
            int count = 0;
            foreach (SectorItemStack stack in stacks)
                if (stack.id == id) count += stack.count;

            return count;
        }

        public bool CanAdd(string id, int count)
        {
            if (!SectorItems.TryGet(id, out SectorItemDefinition item) ||
                count <= 0 ||
                Weight + item.Weight * count > MaxWeight + .0001f)
                return false;

            int room = 0;

            foreach (SectorItemStack stack in stacks)
                if (stack.id == id)
                    room += item.MaxStack - stack.count;

            int missing = count - room;
            int newStacks = Mathf.Max(
                0, Mathf.CeilToInt((float)missing / item.MaxStack));

            return UsedSlots + newStacks <= SlotLimit;
        }

        public bool Add(string id, int count)
        {
            if (!CanAdd(id, count))
                return false;

            SectorItemDefinition item = SectorItems.Get(id);

            for (int i = 0; i < stacks.Count && count > 0; i++)
            {
                SectorItemStack stack = stacks[i];
                if (stack.id != id) continue;

                int n = Mathf.Min(count, item.MaxStack - stack.count);
                stack.count += n;
                count -= n;
            }

            while (count > 0)
            {
                int n = Mathf.Min(count, item.MaxStack);
                stacks.Add(new SectorItemStack(id, n));
                count -= n;
            }

            return true;
        }

        public bool Remove(string id, int count)
        {
            if (count <= 0 || Count(id) < count)
                return false;

            for (int i = stacks.Count - 1; i >= 0 && count > 0; i--)
            {
                SectorItemStack stack = stacks[i];
                if (stack.id != id)
                    continue;

                int n = Mathf.Min(stack.count, count);
                stack.count -= n;
                count -= n;

                if (stack.count == 0)
                    stacks.RemoveAt(i);
            }

            return true;
        }

        public List<SectorItemStack> Export()
        {
            var copy = new List<SectorItemStack>();
            foreach (SectorItemStack stack in stacks)
                copy.Add(new SectorItemStack(stack.id, stack.count));
            return copy;
        }

        public void Import(IEnumerable<SectorItemStack> saved)
        {
            stacks.Clear();
            if (saved == null) return;

            foreach (SectorItemStack stack in saved)
            {
                if (stack == null || !SectorItems.TryGet(stack.id, out _) ||
                    stack.count <= 0)
                    continue;

                // Import through the same rules to reject manipulated / stale saves.
                Add(stack.id, stack.count);
            }
        }
    }
}
