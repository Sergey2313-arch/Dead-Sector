using NUnit.Framework;

namespace DeadSector.Tests
{
    public sealed class SectorMagazineRulesTests
    {
        [Test]
        public void AmmoBelongsToCorrectWeaponAndCapsAtMagazineSize()
        {
            Assert.AreEqual(12, SectorMagazineRules.Capacity("pistol"));
            Assert.AreEqual(30, SectorMagazineRules.Capacity("rifle"));
            Assert.AreEqual(0, SectorMagazineRules.Capacity("stone_knife"));
            Assert.AreEqual("9mm", SectorMagazineRules.AmmoId("pistol"));
            Assert.AreEqual("556", SectorMagazineRules.AmmoId("rifle"));

            Assert.AreEqual(12,
                SectorMagazineRules.ClampLoaded("pistol", 999));
            Assert.AreEqual(0,
                SectorMagazineRules.ClampLoaded("rifle", -30));
        }

        [Test]
        public void ReloadUsesOnlyReserveRoundsAndPreservesTheRemainder()
        {
            var inventory = new SectorInventory(10, 40f);
            Assert.IsTrue(inventory.Add("9mm", 24));
            int loaded = 7;

            Assert.AreEqual(5,
                SectorMagazineRules.Reload("pistol", inventory, ref loaded));
            Assert.AreEqual(12, loaded);
            Assert.AreEqual(19, inventory.Count("9mm"));

            Assert.AreEqual(0,
                SectorMagazineRules.Reload("pistol", inventory, ref loaded));
            Assert.AreEqual(19, inventory.Count("9mm"));
        }

        [Test]
        public void FiringDoesNotConsumeAnyReserveAmmo()
        {
            var inventory = new SectorInventory(10, 40f);
            Assert.IsTrue(inventory.Add("556", 10));
            int magazine = 2;

            Assert.IsTrue(SectorMagazineRules.Fire(ref magazine));
            Assert.IsTrue(SectorMagazineRules.Fire(ref magazine));
            Assert.IsFalse(SectorMagazineRules.Fire(ref magazine));
            Assert.AreEqual(0, magazine);
            Assert.AreEqual(10, inventory.Count("556"));
        }

        [Test]
        public void SeparateWeaponMagazinesRoundtripInOldV1Save()
        {
            var save = new SectorGameSave
            {
                version = 1,
                health = 100f,
                position = SectorLayout.Spawn,
                pistolRounds = 7,
                rifleRounds = 26
            };

            var loaded = UnityEngine.JsonUtility.FromJson<SectorGameSave>(
                UnityEngine.JsonUtility.ToJson(save));

            Assert.AreEqual(7, loaded.pistolRounds);
            Assert.AreEqual(26, loaded.rifleRounds);
            Assert.AreEqual(1, loaded.version);
        }
    }
}
