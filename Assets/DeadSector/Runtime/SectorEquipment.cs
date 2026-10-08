using System;
using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// Real equipment IDs for head/chest/legs/feet and backpack.
    /// Gear stays in the backpack (same rule as weapons in the prototype),
    /// and bonuses work only if the inventory still contains the item.
    /// </summary>
    public sealed class SectorEquipment : MonoBehaviour
    {
        public enum GearSlot { Head, Chest, Legs, Feet, Backpack }

        readonly string[] gear = new string[5];

        public string Equipped(GearSlot slot) => gear[(int)slot] ?? "";

        public static GearSlot? SlotFor(string id)
        {
            switch (id)
            {
                case "wood_helmet":
                case "cotton_hood": return GearSlot.Head;
                case "wood_vest":
                case "cotton_shirt": return GearSlot.Chest;
                case "wood_leggings":
                case "cotton_pants": return GearSlot.Legs;
                case "cotton_boots": return GearSlot.Feet;
                case "cotton_bag": return GearSlot.Backpack;
                default: return null;
            }
        }

        public bool Equip(string id, SectorInventory inventory)
        {
            GearSlot? slot = SlotFor(id);
            if (slot == null || inventory == null || inventory.Count(id) <= 0)
                return false;

            if (slot == GearSlot.Backpack)
                inventory.SetSlotLimit(30);

            gear[(int)slot.Value] = id;
            return true;
        }

        public void Unequip(GearSlot slot, SectorInventory inventory)
        {
            gear[(int)slot] = "";
            Refresh(inventory);
        }

        public void Refresh(SectorInventory inventory)
        {
            if (inventory == null)
                return;

            foreach (GearSlot slot in Enum.GetValues(typeof(GearSlot)))
            {
                int i = (int)slot;
                if (!string.IsNullOrEmpty(gear[i]) &&
                    inventory.Count(gear[i]) <= 0)
                    gear[i] = "";
            }

            inventory.SetSlotLimit(Equipped(GearSlot.Backpack) == "cotton_bag"
                ? 30 : SectorInventory.DefaultSlots);
        }

        public float DamageMultiplier
        {
            get
            {
                float protection = 0f;
                for (int i = 0; i < 4; i++)
                {
                    string id = gear[i] ?? "";
                    if (id.StartsWith("wood_", StringComparison.Ordinal))
                        protection += i == 1 ? .13f : .09f;
                    else if (id.StartsWith("cotton_", StringComparison.Ordinal))
                        protection += i == 1 ? .045f : .025f;
                }

                return 1f - Mathf.Clamp(protection, 0f, .45f);
            }
        }

        public string[] Export()
        {
            string[] copy = new string[gear.Length];
            Array.Copy(gear, copy, gear.Length);
            return copy;
        }

        public void Import(string[] ids, SectorInventory inventory)
        {
            for (int i = 0; i < gear.Length; i++)
                gear[i] = "";

            if (ids != null)
            {
                for (int i = 0; i < Mathf.Min(ids.Length, gear.Length); i++)
                {
                    string id = ids[i];
                    GearSlot? slot = SlotFor(id);
                    if (slot != null && (int)slot.Value == i &&
                        inventory != null && inventory.Count(id) > 0)
                        gear[i] = id;
                }
            }

            Refresh(inventory);
        }
    }
}
