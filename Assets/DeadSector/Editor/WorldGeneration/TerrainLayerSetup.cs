using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DeadSector.Editor.WorldGeneration
{
    public static class TerrainLayerSetup
    {
        private const string Root = "Assets/DeadSector/World/Terrain";
        private const string TextureRoot = Root + "/Textures";
        private const string LayerRoot = Root + "/TerrainLayers";

        private readonly struct SurfacePreset
        {
            public readonly string Name;
            public readonly string Folder;
            public readonly string LayerFile;
            public readonly float TileSize;

            public SurfacePreset(string name, string folder, string layerFile, float tileSize)
            {
                Name = name;
                Folder = folder;
                LayerFile = layerFile;
                TileSize = tileSize;
            }
        }

        private static readonly SurfacePreset[] Presets =
        {
            new("Grass", "Grass", "TL_Grass.asset", 6f),
            new("Dirt",  "Dirt",  "TL_Dirt.asset",  5f),
            new("Mud",   "Mud",   "TL_Mud.asset",   4f),
            new("Rock",  "Rock",  "TL_Rock.asset",  4f)
        };

        [MenuItem("Dead Sector/World/01 - Create Terrain Texture Folders")]
        public static void CreateFolders()
        {
            EnsureFolder("Assets", "DeadSector");
            EnsureFolder("Assets/DeadSector", "World");
            EnsureFolder("Assets/DeadSector/World", "Terrain");
            EnsureFolder(Root, "Textures");
            EnsureFolder(Root, "TerrainLayers");
            EnsureFolder(Root, "Heightmaps");

            foreach (var preset in Presets)
                EnsureFolder(TextureRoot, preset.Folder);

            AssetDatabase.Refresh();
            Debug.Log("[Dead Sector] Terrain folders are ready.");
        }

        [MenuItem("Dead Sector/World/02 - Build Terrain Layers")]
        public static void BuildTerrainLayers()
        {
            CreateFolders();

            foreach (var preset in Presets)
            {
                string folder = TextureRoot + "/" + preset.Folder;
                Texture2D diffuse = FindTexture(folder, "diff", "basecolor", "albedo");
                Texture2D normal = FindTexture(folder, "nor_gl", "normal");

                if (diffuse == null)
                {
                    Debug.LogWarning($"[Dead Sector] {preset.Name}: no diffuse/base color texture found in {folder}");
                    continue;
                }

                if (normal != null)
                    ConfigureNormalMap(normal);

                string layerPath = LayerRoot + "/" + preset.LayerFile;
                TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);

                if (layer == null)
                {
                    layer = new TerrainLayer();
                    AssetDatabase.CreateAsset(layer, layerPath);
                }

                layer.diffuseTexture = diffuse;
                layer.normalMapTexture = normal;
                layer.tileSize = new Vector2(preset.TileSize, preset.TileSize);
                layer.tileOffset = Vector2.zero;
                layer.metallic = 0f;
                layer.smoothness = preset.Name == "Mud" ? 0.22f : 0.08f;

                EditorUtility.SetDirty(layer);
                Debug.Log($"[Dead Sector] Built {preset.Name}: {layerPath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Dead Sector] Terrain Layers build finished.");
        }

        private static Texture2D FindTexture(string folder, params string[] hints)
        {
            if (!AssetDatabase.IsValidFolder(folder))
                return null;

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });

            foreach (string hint in hints)
            {
                string match = guids
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .FirstOrDefault(path =>
                        Path.GetFileNameWithoutExtension(path)
                            .Contains(hint, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(match))
                    return AssetDatabase.LoadAssetAtPath<Texture2D>(match);
            }

            return null;
        }

        private static void ConfigureNormalMap(Texture2D texture)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                return;

            bool changed = false;

            if (importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                changed = true;
            }

            if (importer.sRGBTexture)
            {
                importer.sRGBTexture = false;
                changed = true;
            }

            if (changed)
                importer.SaveAndReimport();
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
