using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// Explicit ammunition-in-magazine state for animation-ready firearms.
    /// Loading moves rounds from bag into a gun; firing never pulls directly
    /// from the bag. Snapshot uses plain integers per saved weapon slot.
    /// </summary>
    public static class SectorMagazineRules
    {
        public static int Capacity(string gunId) =>
            gunId == "rifle" ? 30 :
            gunId == "pistol" ? 12 : 0;

        public static string AmmoId(string gunId) =>
            gunId == "rifle" ? "556" :
            gunId == "pistol" ? "9mm" : "";

        public static int ClampLoaded(string gunId, int count) =>
            Mathf.Clamp(count, 0, Capacity(gunId));

        public static bool Fire(ref int loaded)
        {
            if (loaded <= 0)
                return false;

            loaded--;
            return true;
        }

        public static int Reload(
            string gunId, SectorInventory inventory, ref int loaded)
        {
            int capacity = Capacity(gunId);
            string ammo = AmmoId(gunId);
            if (capacity <= 0 || inventory == null ||
                string.IsNullOrEmpty(ammo))
                return 0;

            loaded = Mathf.Clamp(loaded, 0, capacity);
            int toLoad = Mathf.Min(
                capacity - loaded, inventory.Count(ammo));

            if (toLoad <= 0 || !inventory.Remove(ammo, toLoad))
                return 0;

            loaded += toLoad;
            return toLoad;
        }
    }
}
