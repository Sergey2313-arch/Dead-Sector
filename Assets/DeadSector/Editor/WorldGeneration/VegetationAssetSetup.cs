using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DeadSector.Editor.WorldGeneration
{
    /// <summary>Prepares imported vegetation model folders for Unity.</summary>
    public static class VegetationAssetSetup
    {
        private const string Root = "Assets/DeadSector/World/Vegetation";
        private static readonly string[] Categories = { "Trees", "Saplings", "Shrubs", "Grass", "Plants", "Rocks", "Stumps" };

        [MenuItem("Dead Sector/World/03 - Create Vegetation Folders")]
        public static void CreateFolders()
        {
            Ensure("Assets/DeadSector/World", "Vegetation");
            foreach (var category in Categories) Ensure(Root, category);
            AssetDatabase.Refresh();
            Debug.Log("[Dead Sector] Vegetation folders created. Copy exported FBX/GLB models and textures into their categories.");
        }

        [MenuItem("Dead Sector/World/04 - Configure Vegetation Models")]
        public static void ConfigureModels()
        {
            CreateFolders();
            var paths = AssetDatabase.FindAssets("t:Model", new[] { Root })
                .Select(AssetDatabase.GUIDToAssetPath).Distinct().ToArray();
            int updated = 0;
            foreach (var path in paths)
            {
                if (AssetImporter.GetAtPath(path) is not ModelImporter importer) continue;
                bool change = !importer.importMaterials || !importer.importCameras || !importer.importLights;
                // Use external materials so textures can be consistently assigned in Unity.
                if (importer.importMaterials) importer.materialImportMode = ModelImporterMaterialImportMode.None;
                if (importer.importCameras) importer.importCameras = false;
                if (importer.importLights) importer.importLights = false;
                if (change) { importer.SaveAndReimport(); updated++; }
            }
            Debug.Log($"[Dead Sector] Scanned {paths.Length} models; updated {updated}. Assign URP/HDRP/Built-in materials according to the project's render pipeline.");
        }

        private static void Ensure(string parent, string folder)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + folder))
                AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
