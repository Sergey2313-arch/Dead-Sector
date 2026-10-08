using NUnit.Framework;
using UnityEngine;

namespace DeadSector.Tests
{
    public sealed class SectorCombatRulesTests
    {
        [Test]
        public void BareHandsHaveTwoDistinctInstantAttacks()
        {
            SectorAttackProfile weak =
                SectorCombatRules.ForAttack(string.Empty, false);
            SectorAttackProfile strong =
                SectorCombatRules.ForAttack(string.Empty, true);

            Assert.AreEqual(SectorAttackKind.WeakPunch, weak.Kind);
            Assert.AreEqual(SectorAttackKind.StrongPunch, strong.Kind);
            Assert.Greater(strong.Damage, weak.Damage);
            Assert.Greater(strong.StaminaCost, weak.StaminaCost);
            Assert.Greater(strong.Cooldown, weak.Cooldown);
            Assert.AreEqual(12f, weak.Damage);
            Assert.AreEqual(27f, strong.Damage);
        }

        [Test]
        public void ExhaustionWeakensAttackInsteadOfDisablingIt()
        {
            var obj = new GameObject("CombatSurvivalTest");
            try
            {
                SectorSurvival survival = obj.AddComponent<SectorSurvival>();
                survival.stamina = 0f;
                float power = survival.SpendAttackStamina(18f);

                Assert.AreEqual(0f, survival.stamina);
                Assert.Greater(power, 0f);
                Assert.Less(power, 1f);
                Assert.Greater(
                    SectorCombatRules.FinalDamage(
                        SectorCombatRules.ForAttack("", true), power), 0f);
            }
            finally
            {
                Object.DestroyImmediate(obj);
            }
        }

        [Test]
        public void EquippedMeleeKeepsFastAndHeavyClicks()
        {
            SectorAttackProfile quick =
                SectorCombatRules.ForAttack("axe", false);
            SectorAttackProfile heavy =
                SectorCombatRules.ForAttack("axe", true);

            Assert.AreEqual(SectorAttackKind.KnifeSlash, quick.Kind);
            Assert.AreEqual(SectorAttackKind.HeavyMelee, heavy.Kind);
            Assert.Greater(heavy.Damage, quick.Damage);
        }

        [Test]
        public void FirearmsKeepPrimaryFire()
        {
            var gun = SectorCombatRules.ForAttack("pistol", false);
            Assert.AreEqual(SectorAttackKind.Firearm, gun.Kind);
            Assert.AreEqual(0f, gun.StaminaCost);
        }
    }
}
