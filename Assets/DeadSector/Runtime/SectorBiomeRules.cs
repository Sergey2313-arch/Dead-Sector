using UnityEngine;

namespace DeadSector
{
    // Deterministic biome zoning: independent of streaming order and saves.
    // No additional terrain GameObjects or per-frame biome queries.
    public enum SectorBiome
    {
        Meadow,
        DrySteppe,
        ConiferForest,
        MixedForest,
        RockyHighland
    }

    public static class SectorBiomeRules
    {
        public static SectorBiome At(float x, float z)
        {
            float elevation = SectorLayout.Height(x, z);
            if (elevation >= 170f) return SectorBiome.RockyHighland;

            float moisture = Mathf.PerlinNoise(
                (x + 4000f) * .00054f + 22.7f,
                (z + 4000f) * .00054f + 11.9f);
            float woods = Mathf.PerlinNoise(
                (x + 4000f) * .00071f + 17.4f,
                (z + 4000f) * .00071f + 33.3f);

            if (moisture < .38f)
                return SectorBiome.DrySteppe;
            if (woods > .59f)
                return SectorBiome.ConiferForest;
            if (woods > .43f)
                return SectorBiome.MixedForest;
            return SectorBiome.Meadow;
        }

        public static bool AllowsTallTree(SectorBiome biome) =>
            biome == SectorBiome.ConiferForest ||
            biome == SectorBiome.MixedForest;

        public static float TreeDensity(SectorBiome biome)
        {
            switch (biome)
            {
                case SectorBiome.ConiferForest: return 1f;
                case SectorBiome.MixedForest: return .78f;
                case SectorBiome.Meadow: return .35f;
                case SectorBiome.DrySteppe: return .15f;
                default: return .06f;
            }
        }

        public static Color GrassColor(SectorBiome biome)
        {
            switch (biome)
            {
                case SectorBiome.DrySteppe: return new Color(.53f, .45f, .26f);
                case SectorBiome.ConiferForest: return new Color(.27f, .41f, .24f);
                case SectorBiome.MixedForest: return new Color(.35f, .53f, .29f);
                case SectorBiome.RockyHighland: return new Color(.37f, .38f, .29f);
                default: return new Color(.48f, .62f, .30f);
            }
        }
    }
}
