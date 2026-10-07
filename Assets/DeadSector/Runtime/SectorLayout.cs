using UnityEngine;

namespace DeadSector
{
    // Shared world coordinates guarantee identical heights on neighbouring tile edges.
    public static class SectorLayout
    {
        public const int Tiles = 8;
        public const float TileSize = 1000f;
        public const float HalfSize = 4000f;
        public const int Resolution = 257;
        public static readonly Vector3 Spawn = new Vector3(30f, 60.05f, 20f);

        public static float Height(float x, float z)
        {
            float hills = 25f + Mathf.PerlinNoise((x + 18000f) / 1800f, (z + 13000f) / 1800f) * 130f;
            hills += Mathf.PerlinNoise((x + 17000f) / 420f, (z + 11000f) / 420f) * 16f;
            float edge = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
            hills += Mathf.SmoothStep(0f, 150f, Mathf.InverseLerp(3000f, 3950f, edge));
            // Large flat inhabited valley, blended into the surrounding hills.
            float valley = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(500f, 950f, new Vector2(x, z).magnitude));
            hills = Mathf.Lerp(60f, hills, valley);
            float road = Mathf.Min(Mathf.Abs(z), Mathf.Abs(x - 300f));
            float roadBlend = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(10f, 32f, road));
            return Mathf.Lerp(hills, 60f, roadBlend);
        }

        public static Vector2Int TileAt(Vector3 p)
        {
            return new Vector2Int(Mathf.Clamp(Mathf.FloorToInt((p.x + HalfSize) / TileSize), 0, 7),
                Mathf.Clamp(Mathf.FloorToInt((p.z + HalfSize) / TileSize), 0, 7));
        }
        public static Vector3 Origin(Vector2Int tile) => new Vector3(tile.x * TileSize - HalfSize, 0, tile.y * TileSize - HalfSize);
    }
}
