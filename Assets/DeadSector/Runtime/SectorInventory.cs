using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadSector
{
    public enum SectorItemKind { Food, Drink, Medical, Material, Melee, Firearm, Ammunition, Armor }

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
                { "stone", new SectorItemDefinition("stone", "Loose Stone", SectorItemKind.Material, .38f, 24) },
                { "stick", new SectorItemDefinition("stick", "Dry Stick", SectorItemKind.Material, .28f, 24) },
                { "plant_fiber", new SectorItemDefinition("plant_fiber", "Plant Fiber", SectorItemKind.Material, .05f, 30) },
                { "cotton", new SectorItemDefinition("cotton", "Raw Cotton", SectorItemKind.Material, .08f, 30) },
                { "cord", new SectorItemDefinition("cord", "Twisted Cord", SectorItemKind.Material, .12f, 18) },
                { "knife", new SectorItemDefinition("knife", "Field Knife", SectorItemKind.Melee, .45f, 1, damage: 25f) },
                { "stone_knife", new SectorItemDefinition("stone_knife", "Stone Knife", SectorItemKind.Melee, .6f, 1, damage: 19f) },
                { "stone_axe", new SectorItemDefinition("stone_axe", "Stone Axe", SectorItemKind.Melee, 1.4f, 1, damage: 29f) },
                { "wood_club", new SectorItemDefinition("wood_club", "Wooden Club", SectorItemKind.Melee, 1.25f, 1, damage: 23f) },
                { "axe", new SectorItemDefinition("axe", "Hatchet", SectorItemKind.Melee, 1.3f, 1, damage: 38f) },
                { "spear", new SectorItemDefinition("spear", "Improvised Spear", SectorItemKind.Melee, 1.15f, 1, damage: 31f) },
                { "torch", new SectorItemDefinition("torch", "Hand Torch", SectorItemKind.Melee, .35f, 1, damage: 15f) },
                { "pistol", new SectorItemDefinition("pistol", "Pistol", SectorItemKind.Firearm, 1.15f, 1, damage: 31f) },
                { "rifle", new SectorItemDefinition("rifle", "Rifle", SectorItemKind.Firearm, 3.6f, 1, damage: 44f) },
                { "9mm", new SectorItemDefinition("9mm", "9mm Ammunition", SectorItemKind.Ammunition, .012f, 60) },
                { "556", new SectorItemDefinition("556", "5.56 Ammunition", SectorItemKind.Ammunition, .014f, 60) },
                { "wood_helmet", new SectorItemDefinition("wood_helmet", "Wooden Head Guard", SectorItemKind.Armor, .95f, 1) },
                { "wood_vest", new SectorItemDefinition("wood_vest", "Wooden Chest Guard", SectorItemKind.Armor, 2.6f, 1) },
                { "wood_leggings", new SectorItemDefinition("wood_leggings", "Wooden Leg Guards", SectorItemKind.Armor, 1.8f, 1) },
                { "cotton_hood", new SectorItemDefinition("cotton_hood", "Cotton Hood", SectorItemKind.Armor, .35f, 1) },
                { "cotton_shirt", new SectorItemDefinition("cotton_shirt", "Cotton Shirt", SectorItemKind.Armor, .7f, 1) },
                { "cotton_pants", new SectorItemDefinition("cotton_pants", "Cotton Trousers", SectorItemKind.Armor, .65f, 1) },
                { "cotton_boots", new SectorItemDefinition("cotton_boots", "Cotton Footwraps", SectorItemKind.Armor, .4f, 1) },
                { "cotton_bag", new SectorItemDefinition("cotton_bag", "Cotton Backpack", SectorItemKind.Armor, 1.25f, 1) }
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
        public int SlotLimit { get; private set; }
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

        public void SetSlotLimit(int slots)
        {
            // Reducing carrying capacity never destroys items.
            SlotLimit = Mathf.Max(Mathf.Max(1, slots), UsedSlots);
        }

        public void ResetForLoad(int slots)
        {
            stacks.Clear();
            SlotLimit = Mathf.Max(1, slots);
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

        /// <summary>
        /// Rearranges existing stacks only. Amounts and carried mass remain
        /// exactly the same; it does not merge stacks or drop items.
        /// Sort by gameplay category, then visible name and stable ID.
        /// </summary>
        public void SortStacks()
        {
            stacks.Sort((left, right) =>
            {
                bool leftKnown = SectorItems.TryGet(
                    left.id, out SectorItemDefinition a);
                bool rightKnown = SectorItems.TryGet(
                    right.id, out SectorItemDefinition b);

                if (leftKnown != rightKnown)
                    return leftKnown ? -1 : 1;

                int kind = leftKnown
                    ? a.Kind.CompareTo(b.Kind) : 0;
                if (kind != 0)
                    return kind;

                int name = leftKnown
                    ? string.Compare(a.Label, b.Label,
                        StringComparison.Ordinal) : 0;
                if (name != 0)
                    return name;

                return string.Compare(
                    left.id, right.id, StringComparison.Ordinal);
            });
        }

        /// <summary>
        /// Split just one stack, not the combined count of an item ID.
        /// Inventory.Add would merge the result straight back, so this
        /// direct operation enforces all stack/slot invariants instead.
        /// </summary>
        public bool SplitStack(int index, int count)
        {
            if (index < 0 || index >= stacks.Count ||
                count <= 0 || UsedSlots >= SlotLimit)
                return false;

            SectorItemStack original = stacks[index];
            if (original == null ||
                !SectorItems.TryGet(original.id,
                    out SectorItemDefinition item) ||
                item.MaxStack <= 1 || count >= original.count ||
                count > item.MaxStack)
                return false;

            original.count -= count;
            stacks.Insert(index + 1,
                new SectorItemStack(original.id, count));
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
            if (saved == null)
                return;

            float carriedWeight = 0f;

            foreach (SectorItemStack stack in saved)
            {
                if (stack == null ||
                    !SectorItems.TryGet(stack.id,
                        out SectorItemDefinition item) ||
                    stack.count <= 0)
                    continue;

                // Preserve separate stacks as authored by the player.
                // Using Add here merges split stacks on F5/F9 load.
                // At the same time reject malformed, overweight or
                // over-capacity save data before it enters the inventory.
                int remaining = stack.count;
                while (remaining > 0 && stacks.Count < SlotLimit)
                {
                    int batch = Mathf.Min(remaining, item.MaxStack);
                    float weight = item.Weight * batch;

                    if (carriedWeight + weight > MaxWeight + .0001f)
                        break;

                    stacks.Add(new SectorItemStack(stack.id, batch));
                    carriedWeight += weight;
                    remaining -= batch;
                }
            }
        }
    }
}
