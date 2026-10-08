using UnityEngine;

namespace DeadSector
{
    public enum SectorZombieKind
    {
        Shambler,
        Runner,
        Brute
    }

    /// <summary>
    /// Stat profiles for regular zombies and late-night horde composition.
    /// Distinct silhouettes are assembled by SectorZombieAppearance.
    /// </summary>
    public readonly struct SectorZombieProfile
    {
        public readonly SectorZombieKind Kind;
        public readonly float Health;
        public readonly float WalkSpeed;
        public readonly float ChaseSpeed;
        public readonly float AttackDamage;
        public readonly float AttackCooldown;
        public readonly float SightRange;
        public readonly float HearingRange;
        public readonly Color Tint;
        public readonly float ModelScale;

        public SectorZombieProfile(
            SectorZombieKind kind, float health, float walk,
            float chase, float attackDamage, float attackCooldown,
            float sight, float hearing, Color tint, float scale)
        {
            Kind = kind;
            Health = health;
            WalkSpeed = walk;
            ChaseSpeed = chase;
            AttackDamage = attackDamage;
            AttackCooldown = attackCooldown;
            SightRange = sight;
            HearingRange = hearing;
            Tint = tint;
            ModelScale = scale;
        }
    }

    public static class SectorZombieProfiles
    {
        public static SectorZombieProfile For(SectorZombieKind kind)
        {
            switch (kind)
            {
                case SectorZombieKind.Runner:
                    return new SectorZombieProfile(
                        kind, 70f, 1.5f, 4.7f, 8f, .84f,
                        45f, 42f, new Color(.41f, .24f, .23f), .94f);

                case SectorZombieKind.Brute:
                    return new SectorZombieProfile(
                        kind, 210f, .62f, 2.2f, 22f, 1.75f,
                        35f, 28f, new Color(.24f, .32f, .25f), 1.16f);

                default:
                    return new SectorZombieProfile(
                        SectorZombieKind.Shambler,
                        100f, .9f, 2.8f, 10f, 1.2f,
                        38f, 24f, new Color(.36f, .34f, .27f), 1f);
            }
        }

        public static SectorZombieKind ForHordeIndex(int day, int index)
        {
            // Early waves teach normal infected first; runners appear from day
            // 2 and armored brutes from day 4. Deterministic and testable.
            if (day >= 4 && index > 0 && index % 9 == 0)
                return SectorZombieKind.Brute;

            if (day >= 2 && index > 0 && index % 4 == 0)
                return SectorZombieKind.Runner;

            return SectorZombieKind.Shambler;
        }

        public static SectorZombieKind ForAmbientIndex(int index)
        {
            return index % 8 == 6 ? SectorZombieKind.Runner :
                SectorZombieKind.Shambler;
        }
    }
}
