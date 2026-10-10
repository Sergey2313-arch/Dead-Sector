using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DeadSector.Editor
{
    /// <summary>
    /// Play Mode only render-isolation tool. Does not change saved scenes,
    /// materials, TerrainData or URP quality/project settings.
    /// </summary>
    public sealed class SectorSurfaceDiagnosisWindow : EditorWindow
    {
        readonly Dictionary<Terrain, bool> terrainOriginal =
            new Dictionary<Terrain, bool>();
        readonly Dictionary<MeshRenderer, bool> waterOriginal =
            new Dictionary<MeshRenderer, bool>();
        readonly Dictionary<MeshRenderer, bool> roadOriginal =
            new Dictionary<MeshRenderer, bool>();

        bool hideTerrain;
        bool hideWater;
        bool hideRoads;
        bool hideFog;
        bool hideReflections;
        bool fogStored;
        bool initialFog;
        bool reflectionsStored;
        float initialReflectionIntensity;

        [MenuItem("Dead Sector/Debug/Diagnose Bright Ground")]
        static void Open()
        {
            GetWindow<SectorSurfaceDiagnosisWindow>(
                "Dead Sector - Ground QA");
        }

        void OnInspectorUpdate()
        {
            if (!EditorApplication.isPlaying)
            {
                RestoreAll();
                Repaint();
                return;
            }

            ApplyChanges();
            Repaint();
        }

        void OnDisable()
        {
            RestoreAll();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField(
                "DEAD SECTOR / GROUND RENDER DIAGNOSTICS",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Temporary visual isolation, only during Play Mode. " +
                "Do not save scenes during the experiment. " +
                "The Reset button restores the captured render state.",
                MessageType.Info);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Press Play in DeadSectorPrototype to use the switches.",
                    MessageType.Warning);
                return;
            }

            bool terrainValue = EditorGUILayout.ToggleLeft(
                "1 - Hide all Terrain geometry", hideTerrain);
            bool waterValue = EditorGUILayout.ToggleLeft(
                "2 - Hide all Water_Surface meshes", hideWater);
            bool roadValue = EditorGUILayout.ToggleLeft(
                "3 - Hide all Road_ meshes", hideRoads);
            bool fogValue = EditorGUILayout.ToggleLeft(
                "4 - Disable fog", hideFog);
            bool reflectionValue = EditorGUILayout.ToggleLeft(
                "5 - Disable environment reflections", hideReflections);

            if (terrainValue != hideTerrain || waterValue != hideWater ||
                roadValue != hideRoads || fogValue != hideFog ||
                reflectionValue != hideReflections)
            {
                hideTerrain = terrainValue;
                hideWater = waterValue;
                hideRoads = roadValue;
                hideFog = fogValue;
                hideReflections = reflectionValue;
                ApplyChanges();
            }

            EditorGUILayout.Space();
            if (GUILayout.Button(
                "RESET ALL / Restore game rendering", GUILayout.Height(32)))
                RestoreAll();

            if (GUILayout.Button("Log scene diagnosis to Console"))
                LogReport();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Captured Terrain pieces: " + terrainOriginal.Count);
            EditorGUILayout.LabelField(
                "Captured water renderers: " + waterOriginal.Count);
            EditorGUILayout.LabelField(
                "Captured road renderers: " + roadOriginal.Count);
        }

        void ApplyChanges()
        {
            if (!EditorApplication.isPlaying)
                return;

            if (hideTerrain)
            {
                foreach (Terrain t in Terrain.activeTerrains)
                {
                    if (t == null) continue;
                    if (!terrainOriginal.ContainsKey(t))
                        terrainOriginal.Add(t, t.drawHeightmap);
                    t.drawHeightmap = false;
                }
            }
            else
            {
                RestoreTerrain();
            }

            if (hideWater || hideRoads)
            {
                MeshRenderer[] renderers =
                    Object.FindObjectsByType<MeshRenderer>(
                        FindObjectsSortMode.None);
                foreach (MeshRenderer renderer in renderers)
                {
                    if (renderer == null) continue;
                    string name = renderer.gameObject.name;
                    if (hideWater && name == "Water_Surface")
                    {
                        if (!waterOriginal.ContainsKey(renderer))
                            waterOriginal.Add(renderer, renderer.enabled);
                        renderer.enabled = false;
                    }

                    if (hideRoads && name.StartsWith("Road_"))
                    {
                        if (!roadOriginal.ContainsKey(renderer))
                            roadOriginal.Add(renderer, renderer.enabled);
                        renderer.enabled = false;
                    }
                }
            }

            if (!hideWater) RestoreRenderers(waterOriginal);
            if (!hideRoads) RestoreRenderers(roadOriginal);

            if (hideFog)
            {
                if (!fogStored)
                {
                    initialFog = RenderSettings.fog;
                    fogStored = true;
                }

                RenderSettings.fog = false;
            }
            else if (fogStored)
            {
                RenderSettings.fog = initialFog;
                fogStored = false;
            }

            if (hideReflections)
            {
                if (!reflectionsStored)
                {
                    initialReflectionIntensity = RenderSettings.reflectionIntensity;
                    reflectionsStored = true;
                }

                RenderSettings.reflectionIntensity = 0f;
            }
            else if (reflectionsStored)
            {
                RenderSettings.reflectionIntensity = initialReflectionIntensity;
                reflectionsStored = false;
            }
        }

        void RestoreTerrain()
        {
            foreach (var entry in terrainOriginal)
                if (entry.Key != null)
                    entry.Key.drawHeightmap = entry.Value;
            terrainOriginal.Clear();
        }

        static void RestoreRenderers(Dictionary<MeshRenderer, bool> original)
        {
            foreach (var entry in original)
                if (entry.Key != null)
                    entry.Key.enabled = entry.Value;
            original.Clear();
        }

        void RestoreAll()
        {
            hideTerrain = false;
            hideWater = false;
            hideRoads = false;
            hideFog = false;
            hideReflections = false;
            RestoreTerrain();
            RestoreRenderers(waterOriginal);
            RestoreRenderers(roadOriginal);

            if (fogStored)
            {
                RenderSettings.fog = initialFog;
                fogStored = false;
            }

            if (reflectionsStored)
            {
                RenderSettings.reflectionIntensity = initialReflectionIntensity;
                reflectionsStored = false;
            }
        }

        static void LogReport()
        {
            Camera camera = Camera.main;
            Terrain[] loaded = Terrain.activeTerrains;
            var water = new List<string>();

            if (camera != null)
            {
                Vector3 forward = camera.transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude > .0001f)
                {
                    forward.Normalize();
                    foreach (int meters in new[] { 100, 300, 600, 1000, 1500 })
                    {
                        Vector3 pos = camera.transform.position + forward * meters;
                        if (SectorGeography.TryGetWaterLevel(
                            pos.x, pos.z, out float waterLevel))
                            water.Add(meters + "m (level " +
                                waterLevel.ToString("F1") + ")");
                    }
                }
            }

            Debug.Log(
                "[Dead Sector][Surface QA] Camera=" +
                (camera != null ? camera.transform.position.ToString("F1") : "missing") +
                "; Terrains=" + loaded.Length +
                "; Water along view=" +
                (water.Count > 0 ? string.Join(", ", water) : "none") +
                "; Fog=" + RenderSettings.fog +
                "; FogColor=" + RenderSettings.fogColor +
                "; FogDensity=" + RenderSettings.fogDensity.ToString("F6") +
                "; ReflectionIntensity=" +
                RenderSettings.reflectionIntensity.ToString("F2"));
        }
    }
}
