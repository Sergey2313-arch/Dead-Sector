using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DeadSector.Editor
{
    /// <summary>
    /// Opt-in visual asset setup for the test copy.
    /// Creates independent materials/prefabs without modifying gameplay, scenes, or source FBXs.
    /// Keep originals and licenses under Assets/ExternalAssets.
    /// </summary>
    public static class SectorImportedAssetPrefabs
    {
        private const string OutputFolder = "Assets/DeadSector/Prefabs/ImportedVisuals";
        private const string ShaderName = "Universal Render Pipeline/Lit";

        private struct Entry
        {
            public string Id;
            public string Folder;
            public string PrefabName;

            public Entry(string id, string folder, string prefabName)
            {
                Id = id;
                Folder = folder;
                PrefabName = prefabName;
            }
        }

        private static readonly Entry[] Targets =
        {
            new Entry(
                "dry_branches_medium_01",
                "Assets/ExternalAssets/Vegetation/Models/Deadwood/dry_branches_medium_01",
                "DS_Stick_Visual"),
            new Entry(
                "wooden_hammer_01",
                "Assets/ExternalAssets/Props/Models/Crafting/Tools/wooden_hammer_01",
                "DS_WoodenHammer_Visual"),
            new Entry(
                "medical_box_polyhaven",
                "Assets/ExternalAssets/Props/Models/Loot/Medical/medical_box_polyhaven",
                "DS_MedicalBox_Visual")
        };

        [MenuItem("Dead Sector/Assets/01 - Check 3 imported models")]
        public static void Inspect()
        {
            int ready = 0;
            foreach (Entry entry in Targets)
            {
                string modelPath = entry.Folder + "/Models/Converted/" + entry.Id + ".fbx";
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (model == null)
                {
                    Debug.LogWarning("[Dead Sector] Missing converted FBX: " + modelPath);
                    continue;
                }
                ready++;
                Texture2D albedo = FindTexture(entry.Folder, "basecolor", "diff", "albedo", "color");
                Texture2D normal = FindTexture(entry.Folder, "nor_gl", "normal", "nor");
                Debug.LogFormat("[Dead Sector] {0}: FBX OK, BaseColor={1}, Normal={2}, Renderers={3}.",
                    entry.Id, albedo != null ? albedo.name : "NOT FOUND",
                    normal != null ? normal.name : "NOT FOUND",
                    model.GetComponentsInChildren<Renderer>(true).Length);
            }

            EditorUtility.DisplayDialog("Dead Sector - Asset audit",
                ready + "/3 models found. See Unity Console for texture and renderer details.\n" +
                "This check made no changes.", "OK");
        }

        [MenuItem("Dead Sector/Assets/02 - Build 3 visual prefabs")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Dead Sector",
                    "Stop Play Mode before importing assets.", "OK");
                return;
            }

            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError("[Dead Sector] URP/Lit not found. Verify URP project settings.");
                return;
            }

            EnsureFolder(OutputFolder);
            int created = 0;
            int skipped = 0;
            foreach (Entry entry in Targets)
            {
                string prefabPath = OutputFolder + "/" + entry.PrefabName + ".prefab";
                string modelPath = entry.Folder + "/Models/Converted/" + entry.Id + ".fbx";
                string materialPath = entry.Folder + "/Materials/DS_" + entry.Id + "_URP.mat";

                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                {
                    skipped++;
                    Debug.LogWarning("[Dead Sector] Prefab already exists; will not overwrite: " + prefabPath);
                    continue;
                }

                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (model == null)
                {
                    skipped++;
                    Debug.LogWarning("[Dead Sector] FBX not found: " + modelPath);
                    continue;
                }

                EnsureFolder(entry.Folder + "/Materials");
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(shader) { name = "DS_" + entry.Id + "_URP" };
                    Texture2D albedo = FindTexture(entry.Folder, "basecolor", "diff", "albedo", "color");
                    Texture2D normal = FindTexture(entry.Folder, "nor_gl", "normal", "nor");
                    if (albedo != null)
                        material.SetTexture("_BaseMap", albedo);
                    else
                        Debug.LogWarning("[Dead Sector] No BaseColor texture for " + entry.Id);
                    if (normal != null)
                    {
                        string normalPath = AssetDatabase.GetAssetPath(normal);
                        TextureImporter importer = AssetImporter.GetAtPath(normalPath) as TextureImporter;
                        if (importer != null && importer.textureType != TextureImporterType.NormalMap)
                        {
                            importer.textureType = TextureImporterType.NormalMap;
                            importer.SaveAndReimport();
                            normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                        }
                        material.SetTexture("_BumpMap", normal);
                        material.EnableKeyword("_NORMALMAP");
                    }
                    // Roughness/metalness must be packed for URP Lit separately.
                    material.SetFloat("_Metallic", 0f);
                    material.SetFloat("_Smoothness", 0.25f);
                    AssetDatabase.CreateAsset(material, materialPath);
                }

                GameObject root = new GameObject(entry.PrefabName);
                try
                {
                    GameObject visual = PrefabUtility.InstantiatePrefab(model) as GameObject;
                    if (visual == null)
                    {
                        skipped++;
                        Debug.LogError("[Dead Sector] Could not instantiate FBX: " + modelPath);
                        continue;
                    }
                    visual.transform.SetParent(root.transform, false);
                    visual.name = "Visual";

                    Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
                    int complexSlots = 0;
                    foreach (Renderer renderer in renderers)
                    {
                        Material[] slots = renderer.sharedMaterials;
                        if (slots.Length == 1)
                            renderer.sharedMaterial = material;
                        else
                            complexSlots++;
                    }
                    if (complexSlots > 0)
                    {
                        Debug.LogWarning("[Dead Sector] " + entry.Id + ": " + complexSlots +
                            " renderers with multiple material slots preserved. Review manually.");
                    }
                    // Visual-only: collision size, item pickup, inventory binding and
                    // scene placement are intentionally deferred until Unity inspection.
                    bool success;
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out success);
                    if (!success)
                    {
                        skipped++;
                        Debug.LogError("[Dead Sector] Prefab save failed: " + prefabPath);
                        continue;
                    }
                    created++;
                    Debug.Log("[Dead Sector] Created visual prefab: " + prefabPath);
                }
                catch (Exception exception)
                {
                    skipped++;
                    Debug.LogError("[Dead Sector] " + entry.Id + ": " + exception);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Dead Sector - Visual prefabs",
                "Created: " + created + "\nSkipped: " + skipped +
                "\nCheck materials/scale in Unity before gameplay integration.", "OK");
        }

        private static Texture2D FindTexture(string assetFolder, params string[] tokens)
        {
            if (!AssetDatabase.IsValidFolder(assetFolder))
                return null;
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { assetFolder });
            // Deterministic selection; prefer filenames that contain explicit PBR map tokens.
            Array.Sort(guids, StringComparer.Ordinal);
            foreach (string token in tokens)
            {
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    string file = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                    if (file.Contains(token))
                        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }
            }
            return null;
        }

        private static void EnsureFolder(string path)
        {
            string[] components = path.Split('/');
            string current = components[0];
            for (int i = 1; i < components.Length; i++)
            {
                string next = current + "/" + components[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, components[i]);
                current = next;
            }
        }
    }
}
