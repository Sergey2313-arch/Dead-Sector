using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// Runtime tactical minimap. It renders the real streamed world from above,
    /// follows the player, draws a dedicated player arrow and POI markers, and
    /// can expand into a larger tactical map with M.
    /// </summary>
    public sealed class SectorMinimap : MonoBehaviour
    {
        const int MarkerLayer = 31;

        public SectorPlayer player;
        public SectorWorld world;

        [Header("Minimap")]
        public float cameraHeight = 260f;
        public float minimapSize = 125f;
        public float tacticalMapSize = 700f;
        public bool northUp = true;

        Camera mapCamera;
        Transform playerMarker;
        readonly List<GameObject> markers = new List<GameObject>();
        readonly List<Material> materials = new List<Material>();
        Mesh arrowMesh;
        bool tacticalOpen;
        public bool TacticalOpen => tacticalOpen;
        bool poiBuilt;

        Rect MinimapRect => new Rect(.76f, .72f, .225f, .255f);
        Rect TacticalRect => new Rect(.08f, .08f, .84f, .84f);

        public void Configure(SectorPlayer target, SectorWorld sectorWorld)
        {
            player = target;
            world = sectorWorld;
            BuildCamera();
            BuildPlayerMarker();

            if (isActiveAndEnabled)
                StartCoroutine(BuildPoiMarkersWhenReady());
        }

        void Start()
        {
            if (player != null && mapCamera == null)
                BuildCamera();

            if (player != null && playerMarker == null)
                BuildPlayerMarker();

            if (!poiBuilt)
                StartCoroutine(BuildPoiMarkersWhenReady());
        }

        IEnumerator BuildPoiMarkersWhenReady()
        {
            while (world != null && !world.Ready)
                yield return null;

            if (poiBuilt || world == null)
                yield break;

            poiBuilt = true;

            foreach (SectorPointOfInterest poi in world.PointsOfInterest)
            {
                CreatePoiMarker(
                    poi.Name,
                    poi.Position,
                    poi.MapColor,
                    poi.MapScale);
            }
        }

        void Update()
        {
            if (SectorInput.Pressed(KeyCode.M))
            {
                tacticalOpen = !tacticalOpen;
                ApplyCameraMode();
            }
        }

        void LateUpdate()
        {
            if (mapCamera == null || player == null)
                return;

            Vector3 p = player.transform.position;
            float height = tacticalOpen
                ? Mathf.Max(cameraHeight, tacticalMapSize * .85f)
                : cameraHeight;

            mapCamera.transform.position =
                new Vector3(p.x, p.y + height, p.z);

            float yaw = northUp
                ? 0f
                : player.transform.eulerAngles.y;

            mapCamera.transform.rotation =
                Quaternion.Euler(90f, yaw, 0f);

            if (playerMarker != null)
            {
                playerMarker.position =
                    player.transform.position + Vector3.up * 18f;

                playerMarker.rotation =
                    Quaternion.Euler(
                        0f,
                        player.transform.eulerAngles.y,
                        0f);
            }
        }

        void BuildCamera()
        {
            if (mapCamera != null)
                return;

            GameObject cameraObject =
                new GameObject("MinimapCamera");

            cameraObject.transform.SetParent(transform, false);

            mapCamera = cameraObject.AddComponent<Camera>();
            mapCamera.orthographic = true;
            mapCamera.orthographicSize = minimapSize;
            mapCamera.nearClipPlane = 1f;
            mapCamera.farClipPlane = 1800f;
            mapCamera.clearFlags = CameraClearFlags.SolidColor;
            mapCamera.backgroundColor = new Color(.055f, .065f, .06f, 1f);
            mapCamera.depth = 20f;
            mapCamera.allowHDR = false;
            mapCamera.allowMSAA = false;
            mapCamera.cullingMask = ~0;

            // Dedicated marker layer must never leak into the gameplay camera.
            if (player.view != null)
                player.view.cullingMask &= ~(1 << MarkerLayer);

            ApplyCameraMode();
        }

        void ApplyCameraMode()
        {
            if (mapCamera == null)
                return;

            // Tactical M view is the canonical full-world blueprint atlas.
            // The top-down live camera remains exclusive to the local minimap.
            mapCamera.enabled = !tacticalOpen;
            mapCamera.rect = MinimapRect;
            mapCamera.orthographicSize = minimapSize;
        }

        void BuildPlayerMarker()
        {
            if (player == null || playerMarker != null)
                return;

            arrowMesh = BuildArrowMesh();

            GameObject marker =
                new GameObject("Minimap_Player");

            marker.layer = MarkerLayer;

            MeshFilter filter =
                marker.AddComponent<MeshFilter>();

            filter.sharedMesh = arrowMesh;

            MeshRenderer renderer =
                marker.AddComponent<MeshRenderer>();

            renderer.sharedMaterial =
                CreateMaterial(new Color(.20f, .88f, 1f));

            marker.transform.localScale =
                Vector3.one * 3.2f;

            playerMarker = marker.transform;
            markers.Add(marker);
        }

        void CreatePoiMarker(
            string label,
            Vector3 worldPosition,
            Color color,
            float scale)
        {
            GameObject marker =
                GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            marker.name = "Minimap_POI_" + label;
            marker.layer = MarkerLayer;

            Collider collider = marker.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            marker.transform.position =
                worldPosition + Vector3.up * 22f;

            marker.transform.localScale =
                new Vector3(scale, .08f, scale);

            Renderer renderer = marker.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateMaterial(color);

            markers.Add(marker);
        }

        Material CreateMaterial(Color color)
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Unlit");

            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            Material material = new Material(shader);
            material.color = color;
            materials.Add(material);
            return material;
        }

        static Mesh BuildArrowMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "DeadSector_MinimapArrow";

            mesh.vertices = new[]
            {
                new Vector3(-.72f, 0f, -.75f),
                new Vector3(.72f, 0f, -.75f),
                new Vector3(0f, 0f, 1.25f)
            };

            mesh.triangles = new[]
            {
                0, 2, 1,
                0, 1, 2
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        void OnGUI()
        {
            if (mapCamera == null)
                return;

            if (!tacticalOpen)
            {
                float w = Screen.width;
                float h = Screen.height;

                Rect r = new Rect(
                    w * .76f - 4f,
                    h * (1f - .975f) - 4f,
                    w * .225f + 8f,
                    h * .255f + 8f);

                GUI.Box(r, string.Empty);

                GUI.Label(
                    new Rect(
                        w * .765f,
                        h * .035f,
                        180,
                        22),
                    "MINIMAP   N ↑   [M]");
            }
        }

        void OnDestroy()
        {
            if (arrowMesh != null)
                Destroy(arrowMesh);

            foreach (Material material in materials)
            {
                if (material != null)
                    Destroy(material);
            }
        }
    }
}
