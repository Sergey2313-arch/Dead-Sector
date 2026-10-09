using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// Readable north-up local terrain schematic, independent of weather,
    /// night lighting or the currently streamed Unity terrain meshes.
    /// Regenerated sparingly when the player moves, not every frame.
    /// </summary>
    public static class SectorTacticalMapRaster
    {
        public const int Resolution = 128;
        public const float DefaultDiameter = 340f;

        static readonly Color32 Land = new Color32(66, 82, 65, 255);
        static readonly Color32 Forest = new Color32(51, 73, 57, 255);
        static readonly Color32 Rock = new Color32(116, 123, 111, 255);
        static readonly Color32 Water = new Color32(46, 110, 133, 255);
        static readonly Color32 Road = new Color32(164, 151, 116, 255);

        public static Vector2 Project(
            Vector2 world, Vector2 center, float diameter)
        {
            // 0..1 UV: north is +Y, east is +X.
            float inv = 1f / Mathf.Max(1f, diameter);
            return new Vector2(
                .5f + (world.x - center.x) * inv,
                .5f + (world.y - center.y) * inv);
        }

        public static Color32 TerrainColor(
            float worldX, float worldZ)
        {
            if (SectorGeography.TryGetWaterLevel(
                worldX, worldZ, out _))
                return Water;

            float height = SectorLayout.Height(worldX, worldZ);
            float roadDistance =
                SectorGeography.DistanceToRoad(worldX, worldZ);

            if (roadDistance < 6.5f ||
                (Mathf.Abs(worldZ) < 8f && Mathf.Abs(worldX) < 540f) ||
                (Mathf.Abs(worldX - 300f) < 8f &&
                 Mathf.Abs(worldZ) < 540f))
                return Road;

            if (height >= 170f)
                return Rock;

            float vegetation = Mathf.PerlinNoise(
                (worldX + 4500f) / 95f, (worldZ + 4500f) / 95f);
            return vegetation > .57f ? Forest : Land;
        }

        public static void Write(
            Color32[] pixels, Vector2 center, float diameter,
            int width)
        {
            if (pixels == null || width <= 0 ||
                pixels.Length < width * width)
                return;

            float step = Mathf.Max(1f, diameter) / width;
            float west = center.x - diameter * .5f;
            float south = center.y - diameter * .5f;

            for (int y = 0; y < width; y++)
            {
                float z = south + (y + .5f) * step;
                int row = y * width;

                for (int x = 0; x < width; x++)
                    pixels[row + x] = TerrainColor(
                        west + (x + .5f) * step, z);
            }
        }
    }
}
