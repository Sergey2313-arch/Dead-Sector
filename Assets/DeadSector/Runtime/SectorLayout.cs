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
            return SectorGeography.Height(x, z);
        }

        public static Vector2Int TileAt(Vector3 p)
        {
            return new Vector2Int(Mathf.Clamp(Mathf.FloorToInt((p.x + HalfSize) / TileSize), 0, 7),
                Mathf.Clamp(Mathf.FloorToInt((p.z + HalfSize) / TileSize), 0, 7));
        }
        public static Vector3 Origin(Vector2Int tile) => new Vector3(tile.x * TileSize - HalfSize, 0, tile.y * TileSize - HalfSize);
    }
}
