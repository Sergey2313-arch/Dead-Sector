using UnityEditor;
using UnityEngine;

namespace DeadSector.Editor
{
    /// <summary>
    /// Play Mode QA utility: inspect distant sectors without walking kilometres.
    /// It never edits scene assets or player prefabs.
    /// </summary>
    public sealed class SectorWorldVisitWindow : EditorWindow
    {
        int selected;
        string[] options;

        [MenuItem("Dead Sector/Debug/Visit World Locations")]
        static void Open()
        {
            GetWindow<SectorWorldVisitWindow>("Dead Sector Map QA");
        }

        void OnEnable()
        {
            BuildOptions();
        }

        void BuildOptions()
        {
            var locations = SectorMapPlan.Locations;
            options = new string[locations.Count];

            for (int i = 0; i < locations.Count; i++)
            {
                SectorMapPlan.Location poi = locations[i];
                options[i] = poi.GridCell + "  " + poi.Name + "  [" + poi.Id + "]";
            }
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("DEAD SECTOR — 8 × 8 KM", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Visit the approved map coordinates during Play Mode. " +
                "Streaming terrain will load around the relocated player. " +
                "The camera, minimap and azimuth will follow.",
                MessageType.Info);

            if (options == null || options.Length == 0)
                BuildOptions();

            selected = EditorGUILayout.Popup("Destination", selected, options);
            SectorMapPlan.Location location = SectorMapPlan.Locations[selected];

            EditorGUILayout.LabelField(
                "World X / Z",
                location.MapPosition.x.ToString("0") + " / " +
                location.MapPosition.y.ToString("0"));

            EditorGUILayout.LabelField(
                "Location stage",
                location.Status.ToString());

            GUI.enabled = EditorApplication.isPlaying;

            if (GUILayout.Button("Teleport to selected location", GUILayout.Height(36)))
                Teleport(location);

            GUI.enabled = true;

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Press Play in the DeadSectorPrototype scene first.",
                    MessageType.Warning);
            }
        }

        static void Teleport(SectorMapPlan.Location poi)
        {
            SectorPlayer player = Object.FindFirstObjectByType<SectorPlayer>();

            if (player == null)
            {
                Debug.LogWarning(
                    "[Dead Sector] Can't teleport: no live SectorPlayer found.");
                return;
            }

            CharacterController character = player.GetComponent<CharacterController>();
            if (character == null)
            {
                Debug.LogWarning(
                    "[Dead Sector] Can't teleport: no CharacterController found.");
                return;
            }

            float ground = SectorLayout.Height(
                poi.MapPosition.x,
                poi.MapPosition.y);

            character.enabled = false;
            player.transform.position = new Vector3(
                poi.MapPosition.x,
                ground + 12f,
                poi.MapPosition.y);

            character.enabled = true;
            Physics.SyncTransforms();

            Debug.Log(
                "[Dead Sector] Play Mode teleport to " + poi.Name +
                " / " + poi.GridCell + " at " + player.transform.position +
                " (12m above calculated ground to allow terrain streaming).");
        }
    }
}
