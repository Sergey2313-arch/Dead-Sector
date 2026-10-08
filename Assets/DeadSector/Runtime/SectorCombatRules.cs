using UnityEngine;

namespace DeadSector
{
    public enum SectorAttackKind
    {
        WeakPunch,
        StrongPunch,
        KnifeSlash,
        HeavyMelee,
        Firearm
    }

    /// <summary>
    /// Click-to-hit tuning. No charge-up, held-button requirement or
    /// delayed damage event: attacks resolve in the same input frame.
    /// </summary>
    public readonly struct SectorAttackProfile
    {
        public readonly SectorAttackKind Kind;
        public readonly float Damage;
        public readonly float Range;
        public readonly float Cooldown;
        public readonly float StaminaCost;
        public readonly float FacingThreshold;

        public SectorAttackProfile(
            SectorAttackKind kind,
            float damage,
            float range,
            float cooldown,
            float staminaCost,
            float facingThreshold)
        {
            Kind = kind;
            Damage = damage;
            Range = range;
            Cooldown = cooldown;
            StaminaCost = staminaCost;
            FacingThreshold = facingThreshold;
        }
    }

    public static class SectorCombatRules
    {
        public static SectorAttackProfile ForAttack(string itemId, bool strong)
        {
            if (string.IsNullOrEmpty(itemId))
                return strong
                    ? new SectorAttackProfile(
                        SectorAttackKind.StrongPunch, 27f, 2.15f, .74f, 18f, .30f)
                    : new SectorAttackProfile(
                        SectorAttackKind.WeakPunch, 12f, 2.05f, .27f, 5f, .20f);

            if (SectorItems.TryGet(itemId, out SectorItemDefinition item))
            {
                if (item.Kind == SectorItemKind.Firearm)
                    return new SectorAttackProfile(
                        SectorAttackKind.Firearm,
                        item.Damage,
                        itemId == "rifle" ? 95f : 55f,
                        itemId == "rifle" ? .14f : .25f,
                        0f,
                        .982f);

                if (item.Kind == SectorItemKind.Melee)
                    return new SectorAttackProfile(
                        strong ? SectorAttackKind.HeavyMelee :
                            SectorAttackKind.KnifeSlash,
                        item.Damage * (strong ? 1.50f : 1f),
                        itemId == "spear" ? 2.85f :
                            itemId == "axe" ? 2.6f : 2.35f,
                        strong ? .95f : .54f,
                        strong ? 20f : 7f,
                        .22f);
            }

            return ForAttack(string.Empty, strong);
        }

        public static float FinalDamage(
            SectorAttackProfile attack, float staminaMultiplier)
        {
            return attack.Damage *
                Mathf.Clamp(staminaMultiplier, .45f, 1f);
        }
    }
}
