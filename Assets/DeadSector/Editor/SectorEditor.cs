using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadSector.Editor
{
    public static class SectorEditor
    {
        [MenuItem("Dead Sector/Open playable prototype")]
        public static void OpenPrototype()
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/DeadSector/Scenes/DeadSectorPrototype.unity");
        }


        [MenuItem("Dead Sector/Setup/00 - PREPARE PLAYABLE PROTOTYPE")]
        public static void PreparePlayablePrototype()
        {
            if (EditorApplication.isPlaying)
                return;

            DeadSector.Editor.Player.MixamoPlayerSetup.BuildEverything();
            ZombieMixamoSetup.BuildEverything();
            InstallFullBodyPlayer();

            Debug.Log(
                "[Dead Sector] Prototype preparation finished. " +
                "Open DeadSectorPrototype and press Play.");
        }

        [MenuItem("Dead Sector/Setup/01 - Install Full-Body Player In Prototype")]
        public static void InstallFullBodyPlayer()
        {
            if (EditorApplication.isPlaying ||
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            const string scenePath =
                "Assets/DeadSector/Scenes/DeadSectorPrototype.unity";

            const string prefabPath =
                "Assets/DeadSector/Characters/Player/Prefabs/Player_Mixamo_FullBody.prefab";

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null)
            {
                Debug.LogError(
                    "[Dead Sector] Player prefab not found. Run " +
                    "Dead Sector > Character > Mixamo > 00 - BUILD EVERYTHING first.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath);

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "Player_Mixamo_FullBody" ||
                    root.name == "Player")
                {
                    Object.DestroyImmediate(root);
                }
            }

            GameObject player =
                PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;

            if (player == null)
            {
                Debug.LogError(
                    "[Dead Sector] Could not instantiate full-body player prefab.");
                return;
            }

            player.name = "Player_Mixamo_FullBody";
            player.transform.position = SectorLayout.Spawn;
            player.transform.rotation = Quaternion.identity;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeGameObject = player;
            SceneView.lastActiveSceneView?.FrameSelected();

            Debug.Log(
                "[Dead Sector] Full-body X Bot installed into prototype scene at " +
                SectorLayout.Spawn + ".");
        }

        [MenuItem("Dead Sector/Bake editable 8 x 8 km map")]
        public static void BakeMap()
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string folder = "Assets/DeadSector/Generated";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/DeadSector", "Generated");
            string output = AssetDatabase.GenerateUniqueAssetPath(folder + "/Map");
            AssetDatabase.CreateFolder(folder, System.IO.Path.GetFileName(output));
            try
            {
                EditorUtility.DisplayProgressBar("Dead Sector", "Building 64 editable terrain tiles...", .1f);
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                var go = new GameObject("World_8x8km_Editable");
                var world = go.AddComponent<SectorWorld>(); world.BuildEditorPreview();
                var saved = new HashSet<Object>(); int index = 0;
                // Persist all generated textures, layers, materials and TerrainData before saving.
                foreach (var terrain in go.GetComponentsInChildren<Terrain>())
                {
                    foreach (var layer in terrain.terrainData.terrainLayers)
                    {
                        Save(layer.diffuseTexture, "Texture", ".asset", output, saved, ref index);
                        Save(layer, "Layer", ".terrainlayer", output, saved, ref index);
                    }
                    Save(terrain.terrainData, "Terrain", ".asset", output, saved, ref index);
                    Save(terrain.materialTemplate, "TerrainMaterial", ".mat", output, saved, ref index);
                }
                foreach (var renderer in go.GetComponentsInChildren<Renderer>())
                    foreach (var material in renderer.sharedMaterials) Save(material, "Material", ".mat", output, saved, ref index);
                Object.DestroyImmediate(world); // Editable scene has no runtime generation or cleanup.
                Camera.main.transform.SetPositionAndRotation(new Vector3(0, 420, -400), Quaternion.Euler(40, 0, 0));
                AssetDatabase.SaveAssets();
                EditorSceneManager.SaveScene(scene, output + "/DeadSectorMap.unity");
                Debug.Log("Editable map saved: " + output + "/DeadSectorMap.unity. Playable scene: DeadSectorPrototype.unity.");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }
        static void Save(Object asset, string name, string extension, string folder, HashSet<Object> saved, ref int index)
        {
            if (asset == null || AssetDatabase.Contains(asset) || !saved.Add(asset)) return;
            asset.name = name + "_" + index;
            AssetDatabase.CreateAsset(asset, folder + "/" + name + "_" + index++ + extension);
        }
    }
}
