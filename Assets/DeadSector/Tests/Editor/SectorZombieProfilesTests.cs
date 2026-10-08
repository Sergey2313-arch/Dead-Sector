using NUnit.Framework;

namespace DeadSector.Tests
{
    public sealed class SectorZombieProfilesTests
    {
        [Test]
        public void ThreeZombieTypesHaveDifferentPhysicalThreats()
        {
            SectorZombieProfile shambler =
                SectorZombieProfiles.For(SectorZombieKind.Shambler);
            SectorZombieProfile runner =
                SectorZombieProfiles.For(SectorZombieKind.Runner);
            SectorZombieProfile brute =
                SectorZombieProfiles.For(SectorZombieKind.Brute);

            Assert.Greater(runner.ChaseSpeed, shambler.ChaseSpeed);
            Assert.Less(runner.Health, shambler.Health);
            Assert.Greater(brute.Health, shambler.Health);
            Assert.Greater(brute.AttackDamage, shambler.AttackDamage);
            Assert.Less(brute.ChaseSpeed, runner.ChaseSpeed);
            Assert.Greater(brute.ModelScale, shambler.ModelScale);
        }

        [Test]
        public void OpeningNightContainsOnlyOrdinaryZombies()
        {
            for (int i = 0; i < 12; i++)
                Assert.AreEqual(
                    SectorZombieKind.Shambler,
                    SectorZombieProfiles.ForHordeIndex(1, i));
        }

        [Test]
        public void LaterNightsAddRunnersAndThenBrutes()
        {
            Assert.AreEqual(
                SectorZombieKind.Runner,
                SectorZombieProfiles.ForHordeIndex(2, 4));

            Assert.AreEqual(
                SectorZombieKind.Brute,
                SectorZombieProfiles.ForHordeIndex(4, 9));

            Assert.AreEqual(
                SectorZombieKind.Runner,
                SectorZombieProfiles.ForHordeIndex(4, 8));
        }

        [Test]
        public void AllProfilesUsePositiveHealthDamageAndDelays()
        {
            foreach (SectorZombieKind kind in new[]
                {
                    SectorZombieKind.Shambler,
                    SectorZombieKind.Runner,
                    SectorZombieKind.Brute
                })
            {
                var profile = SectorZombieProfiles.For(kind);

                Assert.Greater(profile.Health, 0f);
                Assert.Greater(profile.AttackDamage, 0f);
                Assert.Greater(profile.AttackCooldown, 0f);
                Assert.Greater(profile.WalkSpeed, 0f);
                Assert.Greater(profile.ChaseSpeed, 0f);
                Assert.Greater(profile.ModelScale, 0f);
            }
        }
    }
}
