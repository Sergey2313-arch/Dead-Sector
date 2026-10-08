using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// A single deterministic, persistent loot cache. Its id is a world key
    /// rather than a runtime instance id, so unload/reload never duplicates loot.
    /// </summary>
    public sealed class SectorLootContainer : MonoBehaviour
    {
        public string containerId;
        public string title = "Supply Cache";

        readonly List<SectorItemStack> contents = new List<SectorItemStack>();
        public IReadOnlyList<SectorItemStack> Contents => contents;
        public bool Empty => contents.Count == 0;

        public void Initialize(
            string id,
            string displayName,
            IEnumerable<SectorItemStack> stored = null)
        {
            containerId = id;
            title = displayName;
            contents.Clear();

            if (stored != null)
            {
                foreach (SectorItemStack item in stored)
                    AddInitial(item);
                return;
            }

            // Guaranteed equipment discoveries to exercise each weapon slot.
            if (containerId == "poi_factory")
            {
                AddInitial(new SectorItemStack("pistol", 1));
                AddInitial(new SectorItemStack("9mm", 24));
                return;
            }

            if (containerId == "poi_military_checkpoint")
            {
                AddInitial(new SectorItemStack("rifle", 1));
                AddInitial(new SectorItemStack("556", 45));
                return;
            }

            if (containerId == "poi_garages")
            {
                AddInitial(new SectorItemStack("axe", 1));
                AddInitial(new SectorItemStack("bandage", 2));
                return;
            }

            // A stable seeded selection across Unity/editor sessions.
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in containerId)
                    hash = (hash ^ c) * 16777619;

                switch (hash % 5)
                {
                    case 0:
                        AddInitial(new SectorItemStack("water", 2));
                        AddInitial(new SectorItemStack("food", 2));
                        break;
                    case 1:
                        AddInitial(new SectorItemStack("bandage", 2));
                        AddInitial(new SectorItemStack("scrap", 3));
                        AddInitial(new SectorItemStack("cloth", 2));
                        break;
                    case 2:
                        AddInitial(new SectorItemStack("water", 1));
                        AddInitial(new SectorItemStack("9mm", 12));
                        break;
                    case 3:
                        AddInitial(new SectorItemStack("wood", 3));
                        AddInitial(new SectorItemStack("cloth", 2));
                        AddInitial(new SectorItemStack("food", 1));
                        break;
                    default:
                        AddInitial(new SectorItemStack("medkit", 1));
                        AddInitial(new SectorItemStack("scrap", 2));
                        break;
                }
            }
        }

        void AddInitial(SectorItemStack stack)
        {
            if (stack != null && stack.count > 0 &&
                SectorItems.TryGet(stack.id, out _))
            {
                contents.Add(new SectorItemStack(stack.id, stack.count));
            }
        }

        public bool TakeFirst(SectorInventory inventory, out string itemLabel)
        {
            itemLabel = string.Empty;

            if (inventory == null || Empty)
                return false;

            SectorItemStack first = contents[0];

            // Take what fits: a too-heavy whole stack should not block
            // picking up a single item.
            int taken = 0;
            for (int n = first.count; n >= 1; n--)
            {
                if (!inventory.Add(first.id, n))
                    continue;

                taken = n;
                break;
            }

            if (taken == 0)
                return false;

            itemLabel = SectorItems.Get(first.id).Label + " x" + taken;
            first.count -= taken;

            if (first.count == 0)
                contents.RemoveAt(0);

            return true;
        }

        public List<SectorItemStack> Export()
        {
            var copy = new List<SectorItemStack>();

            foreach (SectorItemStack stack in contents)
                copy.Add(new SectorItemStack(stack.id, stack.count));

            return copy;
        }
    }

    [Serializable]
    public sealed class SectorContainerSnapshot
    {
        public string id;
        public List<SectorItemStack> items = new List<SectorItemStack>();
    }
}
