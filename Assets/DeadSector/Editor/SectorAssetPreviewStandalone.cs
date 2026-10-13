using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadSector.Editor
{
    /// <summary>
    /// Opt-in preview scene builder. Runs ONLY when the user clicks the menu.
    /// Never modifies DeadSectorPrototype, existing prefabs, or existing scenes.
    /// </summary>
    public static class SectorAssetPreviewStandalone
    {
        private const string ScenePath = "Assets/DeadSector/Scenes/AssetPreview.unity";
        private const string FloorMaterialPath =
            "Assets/DeadSector/Art/AssetPreview/DS_AssetPreview_Floor.mat";

        private static readonly string[] VisualPrefabPaths =
        {
            "Assets/DeadSector/Prefabs/ImportedVisuals/DS_Stick_Visual.prefab",
            "Assets/DeadSector/Prefabs/ImportedVisuals/DS_WoodenHammer_Visual.prefab",
            "Assets/DeadSector/Prefabs/ImportedVisuals/DS_MedicalBox_Visual.prefab"
        };

        [MenuItem("Dead Sector/Assets/03 - Create AssetPreview scene")]
        public static void CreateScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Dead Sector",
                    "Stop Play Mode before creating the preview scene.", "OK");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null ||
                System.IO.File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("Dead Sector",
                    "AssetPreview.unity already exists. It has NOT been overwritten.\n" +
                    ScenePath, "OK");
                return;
            }

            var prefabs = new GameObject[VisualPrefabPaths.Length];
            for (int i = 0; i < VisualPrefabPaths.Length; i++)
            {
                prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefabPaths[i]);
                if (prefabs[i] == null)
                {
                    EditorUtility.DisplayDialog("Dead Sector",
                        "Missing prefab (nothing changed):\n" + VisualPrefabPaths[i], "OK");
                    return;
                }
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                EditorUtility.DisplayDialog("Dead Sector",
                    "URP/Lit shader was not found. Scene not created.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EnsureFolder("Assets/DeadSector/Art/AssetPreview");
            Material floorMaterial = AssetDatabase.LoadAssetAtPath<Material>(FloorMaterialPath);
            if (floorMaterial == null)
            {
                floorMaterial = new Material(shader)
                {
                    name = "DS_AssetPreview_Floor"
                };
                floorMaterial.SetColor("_BaseColor", new Color(.29f, .31f, .30f));
                floorMaterial.SetFloat("_Metallic", 0f);
                floorMaterial.SetFloat("_Smoothness", .15f);
                AssetDatabase.CreateAsset(floorMaterial, FloorMaterialPath);
            }

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor_8x5m";
            floor.transform.position = new Vector3(0f, -.05f, 0f);
            floor.transform.localScale = new Vector3(8f, .1f, 5f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;

            var xPositions = new[] { -2.35f, 0f, 2.35f };
            for (int i = 0; i < prefabs.Length; i++)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(prefabs[i], scene)
                    as GameObject;
                if (instance == null)
                {
                    Debug.LogError("[Dead Sector] Cannot instantiate preview prefab: " +
                        VisualPrefabPaths[i]);
                    continue;
                }

                instance.name = prefabs[i].name + "_PreviewOnly";
                instance.transform.position = new Vector3(xPositions[i], 0f, 0f);
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    Debug.LogWarning("[Dead Sector] No renderers found: " + prefabs[i].name);
                    continue;
                }

                Bounds bounds = renderers[0].bounds;
                for (int j = 1; j < renderers.Length; j++)
                    bounds.Encapsulate(renderers[j].bounds);

                // Maintain original 1:1 scale; align bounding box to the ground.
                Vector3 delta = new Vector3(
                    xPositions[i] - bounds.center.x,
                    -bounds.min.y + .015f,
                    -bounds.center.z);
                instance.transform.position += delta;

                Debug.LogFormat(
                    "[Dead Sector] AssetPreview {0} bounds (m): {1:F3} x {2:F3} x {3:F3}",
                    prefabs[i].name, bounds.size.x, bounds.size.y, bounds.size.z);
            }

            GameObject ruler = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ruler.name = "Reference_1_Meter_High";
            ruler.transform.localScale = new Vector3(.10f, 1f, .10f);
            ruler.transform.position = new Vector3(-3.6f, .5f, 0f);
            ruler.GetComponent<Renderer>().sharedMaterial = floorMaterial;

            GameObject referenceCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            referenceCube.name = "Reference_Cube_1x1x1m";
            referenceCube.transform.localScale = Vector3.one;
            referenceCube.transform.position = new Vector3(3.3f, .5f, .4f);
            referenceCube.GetComponent<Renderer>().sharedMaterial = floorMaterial;

            GameObject lightObject = new GameObject("Preview_DirectionalLight");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

            GameObject cameraObject = new GameObject("Preview_Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 52f;
            camera.nearClipPlane = .03f;
            camera.farClipPlane = 100f;
            cameraObject.transform.position = new Vector3(0f, 3.1f, -6.7f);
            cameraObject.transform.LookAt(new Vector3(0f, .45f, 0f));

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError("[Dead Sector] AssetPreview scene could not be saved.");
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Dead Sector] Created preview scene: " + ScenePath +
                ". Existing prefabs and game scene remain unchanged.");
            EditorUtility.DisplayDialog("Dead Sector",
                "AssetPreview scene created. Review real-world scale and PBR materials.\n" +
                "This scene does not add inventory or crafting behaviour.", "OK");
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
