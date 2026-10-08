using NUnit.Framework;
using UnityEngine;

namespace DeadSector.Tests
{
    public sealed class SectorEquipmentAndHordeTests
    {
        [Test]
        public void WoodenAndCottonArmorReduceRealDamage()
        {
            GameObject obj = new GameObject("ArmorUnitTest");

            try
            {
                SectorEquipment armor = obj.AddComponent<SectorEquipment>();
                var inventory = new SectorInventory();
                Assert.IsTrue(inventory.Add("wood_vest", 1));
                Assert.IsTrue(inventory.Add("cotton_hood", 1));

                Assert.IsTrue(armor.Equip("wood_vest", inventory));
                Assert.IsTrue(armor.Equip("cotton_hood", inventory));

                Assert.Less(armor.DamageMultiplier, 1f);
                Assert.AreEqual("wood_vest",
                    armor.Equipped(SectorEquipment.GearSlot.Chest));
                Assert.AreEqual(1, inventory.Count("wood_vest"));
            }
            finally
            {
                Object.DestroyImmediate(obj);
            }
        }

        [Test]
        public void CottonBackpackActuallyAddsSlots()
        {
            GameObject obj = new GameObject("BackpackUnitTest");
            try
            {
                SectorEquipment armor = obj.AddComponent<SectorEquipment>();
                var inventory = new SectorInventory();
                inventory.Add("cotton_bag", 1);

                Assert.AreEqual(22, inventory.SlotLimit);
                Assert.IsTrue(armor.Equip("cotton_bag", inventory));
                Assert.AreEqual(30, inventory.SlotLimit);

                armor.Unequip(SectorEquipment.GearSlot.Backpack, inventory);
                Assert.AreEqual(22, inventory.SlotLimit);
            }
            finally
            {
                Object.DestroyImmediate(obj);
            }
        }

        [Test]
        public void ArmorLoadRestoresOnlyOwnedItems()
        {
            GameObject obj = new GameObject("ArmorSaveUnitTest");
            try
            {
                SectorEquipment armor = obj.AddComponent<SectorEquipment>();
                var inventory = new SectorInventory();
                inventory.Add("cotton_pants", 1);

                armor.Import(new[]
                {
                    "wood_helmet", "", "cotton_pants", "",
                    "cotton_bag"
                }, inventory);

                Assert.AreEqual("", armor.Equipped(
                    SectorEquipment.GearSlot.Head));
                Assert.AreEqual("cotton_pants", armor.Equipped(
                    SectorEquipment.GearSlot.Legs));
                Assert.AreEqual(22, inventory.SlotLimit);
            }
            finally
            {
                Object.DestroyImmediate(obj);
            }
        }

        [Test]
        public void EachNightHasLargerCappedHorde()
        {
            GameObject obj = new GameObject("HordeUnitTest");
            try
            {
                var horde = obj.AddComponent<SectorHordeDirector>();
                Assert.IsTrue(horde.HordeScheduledFor(1));
                Assert.IsTrue(horde.HordeScheduledFor(2));
                Assert.IsTrue(horde.HordeScheduledFor(3));
                Assert.AreEqual(6, horde.ZombiesForDay(1));
                Assert.AreEqual(9, horde.ZombiesForDay(2));
                Assert.AreEqual(12, horde.ZombiesForDay(3));
                Assert.AreEqual(30, horde.ZombiesForDay(100));

                horde.nightsBetweenAttacks = 7;
                Assert.IsTrue(horde.HordeScheduledFor(1));
                Assert.IsFalse(horde.HordeScheduledFor(2));
                Assert.IsTrue(horde.HordeScheduledFor(8));
            }
            finally
            {
                Object.DestroyImmediate(obj);
            }
        }

        [Test]
        public void LoadingTriggeredNightDoesNotDoubleSpawnHorde()
        {
            GameObject obj = new GameObject("HordeLoadUnitTest");
            try
            {
                var horde = obj.AddComponent<SectorHordeDirector>();
                horde.Import(new SectorHordeSnapshot
                {
                    lastTriggeredDay = 4,
                    lastClearedDay = 3
                });

                Assert.AreEqual(4, horde.LastTriggeredDay);
                Assert.AreEqual(4, horde.LastClearedDay);
                Assert.IsFalse(horde.Active);
            }
            finally
            {
                Object.DestroyImmediate(obj);
            }
        }

        [Test]
        public void WorldClockRestoresSurvivedDays()
        {
            GameObject obj = new GameObject("ClockDaysUnitTest");
            try
            {
                var clock = obj.AddComponent<SectorWorldClock>();
                clock.RestoreTime(21f, 4);
                Assert.AreEqual(4, clock.DaysSurvived);
                Assert.AreEqual(5, clock.DayNumber);
                Assert.AreEqual(21f, clock.hourOfDay);
            }
            finally
            {
                Object.DestroyImmediate(obj);
            }
        }
    }
}
