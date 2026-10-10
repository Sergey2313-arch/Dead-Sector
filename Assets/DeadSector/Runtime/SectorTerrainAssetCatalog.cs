using UnityEngine;

namespace DeadSector
{
    // Small Resources-visible reference catalog. Real texture files stay
    // under ExternalAssets; Unity automatically includes referenced dependencies
    // when building. No duplicate 2K textures and no Editor-only AssetDatabase.
    [CreateAssetMenu(fileName = "TerrainCatalog",
        menuName = "Dead Sector/Terrain Asset Catalog")]
    public sealed class SectorTerrainAssetCatalog : ScriptableObject
    {
        public TerrainLayer forestGround;

        public static TerrainLayer LoadForestGround()
        {
            SectorTerrainAssetCatalog catalog =
                Resources.Load<SectorTerrainAssetCatalog>(
                    "DeadSector/TerrainCatalog");
            if (catalog == null || catalog.forestGround == null ||
                catalog.forestGround.diffuseTexture == null)
                return null;

            return catalog.forestGround;
        }
    }
}
