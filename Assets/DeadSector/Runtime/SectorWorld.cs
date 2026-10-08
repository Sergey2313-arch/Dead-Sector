using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeadSector
{
    public readonly struct SectorPointOfInterest
    {
        public readonly string Name;
        public readonly Vector3 Position;
        public readonly Color MapColor;
        public readonly float MapScale;

        public SectorPointOfInterest(
            string name,
            Vector3 position,
            Color mapColor,
            float mapScale = 2.2f)
        {
            Name = name;
            Position = position;
            MapColor = mapColor;
            MapScale = mapScale;
        }
    }

    public sealed class SectorWorld : MonoBehaviour
    {
        public SectorPlayer player;
        public bool Ready { get; private set; }
        public IReadOnlyList<SectorPointOfInterest> PointsOfInterest => pointsOfInterest;
        public int LoadedTiles => tiles.Count;
        public string Status { get; private set; } = "Generating terrain...";
        public SectorArt Art { get; private set; }
        readonly Dictionary<Vector2Int, Terrain> tiles = new Dictionary<Vector2Int, Terrain>();
        readonly List<SectorPointOfInterest> pointsOfInterest =
            new List<SectorPointOfInterest>();
        TerrainLayer[] layers;
        Material terrainMaterial;
        Material roadMaterial;
        Material waterMaterial;
        Vector2Int last = new Vector2Int(-99, -99);
        bool streaming;
        void Awake() => InitializeArt();
        void InitializeArt()
        {
            if (Art != null) return;
            Art = new SectorArt();
            layers = new[] { Layer(new Color(.23f, .28f, .16f)), Layer(new Color(.3f, .26f, .2f)), Layer(new Color(.35f, .37f, .37f)) };
            Shader terrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            if (terrainShader == null)
                terrainShader = Shader.Find("Nature/Terrain/Standard");

            terrainMaterial = new Material(terrainShader);

            roadMaterial = Art.Material(new Color(.19f, .18f, .165f));

            Shader waterShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (waterShader == null)
                waterShader = Shader.Find("Unlit/Color");

            waterMaterial = new Material(waterShader);
            Color waterColor = new Color(.12f, .32f, .39f, .76f);

            if (waterMaterial.HasProperty("_BaseColor"))
                waterMaterial.SetColor("_BaseColor", waterColor);

            waterMaterial.color = waterColor;
            waterMaterial.SetOverrideTag("RenderType", "Transparent");
            waterMaterial.SetInt("_Surface", 1);
            waterMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            waterMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            waterMaterial.SetInt("_ZWrite", 0);
            waterMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            waterMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        // Editor tooling can bake the same deterministic map into editable scene objects.
        public void BuildEditorPreview()
        {
            InitializeArt();
            for (int x = 0; x < 8; x++) for (int z = 0; z < 8; z++)
            {
                var key = new Vector2Int(x, z); tiles.Add(key, BuildTile(key));
            }
            foreach (var entry in tiles)
            {
                var key = entry.Key;
                entry.Value.SetNeighbors(Get(key + Vector2Int.left), Get(key + Vector2Int.up), Get(key + Vector2Int.right), Get(key + Vector2Int.down));
            }
            BuildSettlement();
        }
        IEnumerator Start()
        {
            yield return Refresh(SectorLayout.TileAt(SectorLayout.Spawn));
            BuildSettlement();
            Physics.SyncTransforms();
            player.ActivateAtSpawn();
            Ready = true;
            Status = "Ready";
        }
        void Update()
        {
            if (!Ready || streaming) return;
            Vector2Int tile = SectorLayout.TileAt(player.transform.position);
            if (tile != last) StartCoroutine(Refresh(tile));
        }
        IEnumerator Refresh(Vector2Int center)
        {
            streaming = true;
            // Build before unloading so the player never loses the terrain underfoot.
            var required = new HashSet<Vector2Int>();
            for (int dx = -1; dx <= 1; dx++) for (int dz = -1; dz <= 1; dz++)
            {
                var key = center + new Vector2Int(dx, dz);
                if (key.x < 0 || key.y < 0 || key.x >= 8 || key.y >= 8) continue;
                required.Add(key);
                if (!tiles.ContainsKey(key))
                {
                    Status = "Terrain " + key.x + ", " + key.y;
                    tiles.Add(key, BuildTile(key));
                    yield return null;
                }
            }
            var old = new List<Vector2Int>();
            foreach (var entry in tiles) if (!required.Contains(entry.Key)) old.Add(entry.Key);
            foreach (var key in old)
            {
                var terrain = tiles[key];
                Transform tileRoot = terrain.transform.parent;
                DisposeTileMeshes(tileRoot);
                Destroy(terrain.terrainData);
                Destroy(tileRoot.gameObject);
                tiles.Remove(key);
            }
            foreach (var entry in tiles)
            {
                var key = entry.Key;
                entry.Value.SetNeighbors(Get(key + Vector2Int.left), Get(key + Vector2Int.up), Get(key + Vector2Int.right), Get(key + Vector2Int.down));
            }
            last = center; streaming = false; Status = "Ready";
        }
        Terrain Get(Vector2Int key) => tiles.TryGetValue(key, out var terrain) ? terrain : null;
        Terrain BuildTile(Vector2Int key)
        {
            var origin = SectorLayout.Origin(key);
            var root = new GameObject("Sector_" + key.x + "_" + key.y); root.transform.SetParent(transform); root.transform.position = origin;
            var data = new TerrainData { heightmapResolution = SectorLayout.Resolution, size = new Vector3(1000, 500, 1000), alphamapResolution = 128 };
            int resolution = SectorLayout.Resolution; var heights = new float[resolution, resolution];
            for (int z = 0; z < resolution; z++) for (int x = 0; x < resolution; x++)
                heights[z, x] = SectorLayout.Height(origin.x + x * 1000f / (resolution - 1), origin.z + z * 1000f / (resolution - 1)) / 500f;
            data.SetHeights(0, 0, heights); data.terrainLayers = layers;
            var splat = new float[128, 128, 3];
            for (int z = 0; z < 128; z++) for (int x = 0; x < 128; x++)
            {
                float wx = origin.x + x * 1000f / 127, wz = origin.z + z * 1000f / 127;
                float road = Mathf.Min(Mathf.Abs(wz), Mathf.Abs(wx - 300));
                float plannedRoad = SectorGeography.DistanceToRoad(wx, wz);
                int layer = (road < 9 && Mathf.Max(Mathf.Abs(wx), Mathf.Abs(wz)) < 540f) ||
                            plannedRoad < 9f
                    ? 1
                    : SectorLayout.Height(wx, wz) > 185f ? 2 : 0;
                splat[z, x, layer] = 1;
            }
            data.SetAlphamaps(0, 0, splat);
            var go = Terrain.CreateTerrainGameObject(data); go.name = "Terrain"; go.transform.SetParent(root.transform, false);
            var terrain = go.GetComponent<Terrain>(); terrain.materialTemplate = terrainMaterial; terrain.heightmapPixelError = 8;
            terrain.basemapDistance = 800; terrain.drawInstanced = true;
            var random = new System.Random(key.x * 7919 + key.y * 104729 + 2026);
            for (int i = 0; i < 80; i++)
            {
                float x = (float)random.NextDouble() * 1000, z = (float)random.NextDouble() * 1000;
                float wx = origin.x + x, wz = origin.z + z;
                if (new Vector2(wx, wz).magnitude < 470 ||
                    (Mathf.Abs(wz) < 28 && Mathf.Abs(wx) < 540) ||
                    (Mathf.Abs(wx - 300) < 28 && Mathf.Abs(wz) < 540) ||
                    SectorGeography.DistanceToRoad(wx, wz) < 20f ||
                    SectorGeography.TryGetWaterLevel(wx, wz, out _))
                    continue;

                bool nearLandmark = false;
                foreach (SectorMapPlan.Location poi in SectorMapPlan.Locations)
                {
                    float radius = poi.Id == "airfield" ? 435f :
                        poi.Id == "abandoned_city" ? 270f :
                        poi.Id == "quarry" ? 240f : 150f;

                    if (Vector2.Distance(
                        new Vector2(wx, wz), poi.MapPosition) < radius)
                    {
                        nearLandmark = true;
                        break;
                    }
                }

                if (nearLandmark)
                    continue;
                float h = data.GetInterpolatedHeight(x / 1000, z / 1000);
                var tree = new GameObject("Pine"); tree.transform.SetParent(root.transform, false); tree.transform.localPosition = new Vector3(x, h, z);
                float size = 6 + (float)random.NextDouble() * 5;
                Art.Shape(tree.transform, "Trunk", PrimitiveType.Cylinder, Vector3.up * size * .25f, new Vector3(.5f, size * .25f, .5f), new Color(.2f, .14f, .09f));
                Art.Shape(tree.transform, "Crown", PrimitiveType.Sphere, Vector3.up * size * .7f, new Vector3(size * .5f, size * .8f, size * .5f), new Color(.1f, .19f, .12f), false);
            }
            SectorLandscapeBuilder.Build(
                root.transform, origin, roadMaterial, waterMaterial);

            SectorLandmarkBuilder.Build(root.transform, origin, Art);

            return terrain;
        }
        TerrainLayer Layer(Color color)
        {
            var texture = new Texture2D(32, 32); texture.wrapMode = TextureWrapMode.Repeat;
            var random = new System.Random(123);
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) texture.SetPixel(x, y, color * (.85f + (float)random.NextDouble() * .3f));
            texture.Apply(); return new TerrainLayer { diffuseTexture = texture, tileSize = Vector2.one * 12 };
        }
        public Transform Settlement { get; private set; }

        void BuildSettlement()
        {
            pointsOfInterest.Clear();

            Settlement = new GameObject("Settlement_Prototype").transform;
            Settlement.SetParent(transform, false);

            Color plaster = new Color(.48f, .47f, .43f);
            Color plasterWarm = new Color(.54f, .49f, .42f);
            Color wood = new Color(.24f, .16f, .10f);
            Color roof = new Color(.19f, .20f, .20f);
            Color rust = new Color(.30f, .16f, .09f);
            Color concrete = new Color(.38f, .39f, .37f);
            Color glass = new Color(.24f, .34f, .38f);

            // Small lived-in village around the main road. These are still
            // procedural prototype buildings, but they have real door openings,
            // windows, roofs, porches, fences and simple interiors.
            Vector3[] northHouses =
            {
                new Vector3(-245, 60, 92),
                new Vector3(-175, 60, 105),
                new Vector3(-103, 60, 88),
                new Vector3(-25, 60, 102),
                new Vector3(58, 60, 90)
            };

            Vector3[] southHouses =
            {
                new Vector3(-232, 60, -96),
                new Vector3(-150, 60, -108),
                new Vector3(-68, 60, -92),
                new Vector3(18, 60, -106),
                new Vector3(100, 60, -90)
            };

            for (int i = 0; i < northHouses.Length; i++)
            {
                House(
                    northHouses[i],
                    12 + (i % 2) * 2,
                    16 + (i % 3) * 2,
                    4.8f + (i % 2) * .4f,
                    i % 2 == 0 ? plaster : plasterWarm,
                    roof,
                    wood,
                    glass,
                    "Village_House_N_" + i,
                    180f);
            }

            for (int i = 0; i < southHouses.Length; i++)
            {
                House(
                    southHouses[i],
                    12 + ((i + 1) % 2) * 2,
                    15 + (i % 3) * 2,
                    4.8f + ((i + 1) % 2) * .4f,
                    i % 2 == 0 ? plasterWarm : plaster,
                    roof,
                    wood,
                    glass,
                    "Village_House_S_" + i,
                    0f);
            }

            BuildYards(northHouses, southHouses, wood);

            RegisterPOI(
                "Village",
                new Vector3(-80, 60, 0),
                new Color(.95f, .86f, .34f),
                2.8f);

            IndustrialBuilding(
                new Vector3(220, 60, 150),
                new Vector3(36, 9, 46),
                concrete,
                roof,
                glass,
                "Factory");

            RegisterPOI(
                "Factory",
                new Vector3(220, 60, 150),
                new Color(.92f, .38f, .20f),
                2.7f);

            IndustrialBuilding(
                new Vector3(222, 60, 235),
                new Vector3(30, 7, 38),
                new Color(.36f, .35f, .31f),
                roof,
                glass,
                "Warehouse");

            RegisterPOI(
                "Warehouse",
                new Vector3(222, 60, 235),
                new Color(.92f, .56f, .20f),
                2.4f);

            BuildClinic(
                new Vector3(-72, 60, 190),
                plaster,
                roof,
                glass);

            BuildGasStation(
                new Vector3(315, 60, 72),
                concrete,
                rust,
                glass);

            BuildGarageRow(
                new Vector3(-302, 60, 205),
                concrete,
                roof);

            BuildRadioTower(
                new Vector3(355, 60, 270),
                concrete,
                rust);

            for (int i = 0; i < 10; i++)
            {
                Art.Box(
                    Settlement,
                    "Cargo_Container_" + i,
                    new Vector3(
                        178 + i % 5 * 12,
                        61.45f,
                        305 + i / 5 * 8),
                    new Vector3(10, 2.9f, 4.6f),
                    i % 2 == 0 ? rust : new Color(.18f, .25f, .24f));
            }

            BuildCheckpoint(concrete, rust);

            RegisterPOI(
                "Checkpoint",
                new Vector3(320, 60, -220),
                new Color(.82f, .82f, .82f),
                2.2f);

            BuildStreetlights(concrete);

            RegisterPOI(
                "Spawn",
                SectorLayout.Spawn,
                new Color(.20f, .88f, 1f),
                1.8f);

            // Small collision / jump course near the spawn.
            for (int i = 0; i < 5; i++)
            {
                Art.Box(
                    Settlement,
                    "Jump_Test_" + i,
                    new Vector3(20 + i * 3, 60 + (.2f + i * .12f), 38),
                    new Vector3(2, .4f + i * .24f, 2),
                    rust);
            }
        }

        void House(
            Vector3 position,
            float width,
            float depth,
            float height,
            Color wallColor,
            Color roofColor,
            Color woodColor,
            Color glassColor,
            string name,
            float yaw)
        {
            var root = new GameObject(name);
            root.transform.SetParent(Settlement, false);
            root.transform.localPosition = position;
            root.transform.localRotation = Quaternion.Euler(0, yaw, 0);

            const float wall = .28f;
            const float doorWidth = 1.25f;
            const float doorHeight = 2.25f;
            float frontZ = -depth * .5f;
            float backZ = depth * .5f;

            Art.Box(
                root.transform,
                "Foundation",
                new Vector3(0, .16f, 0),
                new Vector3(width + .6f, .32f, depth + .6f),
                new Color(.30f, .30f, .28f));

            Art.Box(
                root.transform,
                "Floor",
                new Vector3(0, .34f, 0),
                new Vector3(width, .16f, depth),
                new Color(.27f, .22f, .17f));

            Art.Box(
                root.transform,
                "Rear_Wall",
                new Vector3(0, height * .5f, backZ),
                new Vector3(width, height, wall),
                wallColor);

            Art.Box(
                root.transform,
                "Left_Wall",
                new Vector3(-width * .5f, height * .5f, 0),
                new Vector3(wall, height, depth),
                wallColor);

            Art.Box(
                root.transform,
                "Right_Wall",
                new Vector3(width * .5f, height * .5f, 0),
                new Vector3(wall, height, depth),
                wallColor);

            float frontSideWidth = (width - doorWidth) * .5f;

            Art.Box(
                root.transform,
                "Front_Wall_Left",
                new Vector3(
                    -(doorWidth * .5f + frontSideWidth * .5f),
                    height * .5f,
                    frontZ),
                new Vector3(frontSideWidth, height, wall),
                wallColor);

            Art.Box(
                root.transform,
                "Front_Wall_Right",
                new Vector3(
                    doorWidth * .5f + frontSideWidth * .5f,
                    height * .5f,
                    frontZ),
                new Vector3(frontSideWidth, height, wall),
                wallColor);

            Art.Box(
                root.transform,
                "Door_Lintel",
                new Vector3(
                    0,
                    doorHeight + (height - doorHeight) * .5f,
                    frontZ),
                new Vector3(doorWidth, height - doorHeight, wall),
                wallColor);

            // Open doorway with a visible door swung inward.
            var door = Art.Shape(
                root.transform,
                "Door",
                PrimitiveType.Cube,
                new Vector3(-doorWidth * .48f, doorHeight * .5f, frontZ + .65f),
                new Vector3(.08f, doorHeight, doorWidth),
                woodColor,
                false);

            door.transform.localRotation = Quaternion.Euler(0, -72f, 0);

            Art.Box(
                root.transform,
                "Porch",
                new Vector3(0, .22f, frontZ - 1.1f),
                new Vector3(3.4f, .44f, 1.8f),
                new Color(.34f, .29f, .22f));

            Window(
                root.transform,
                new Vector3(-width * .25f, height * .58f, frontZ - .16f),
                new Vector3(1.7f, 1.35f, .08f),
                glassColor);

            Window(
                root.transform,
                new Vector3(width * .25f, height * .58f, frontZ - .16f),
                new Vector3(1.7f, 1.35f, .08f),
                glassColor);

            Window(
                root.transform,
                new Vector3(-width * .5f - .16f, height * .58f, depth * .16f),
                new Vector3(.08f, 1.25f, 1.7f),
                glassColor);

            // Simple interior divider, deliberately leaving a passage.
            Art.Box(
                root.transform,
                "Interior_Wall",
                new Vector3(-width * .18f, height * .45f, depth * .08f),
                new Vector3(width * .48f, height * .82f, .18f),
                new Color(.56f, .54f, .49f));

            GabledRoof(
                root.transform,
                width,
                depth,
                height,
                roofColor);

            Art.Shape(
                root.transform,
                "Chimney",
                PrimitiveType.Cube,
                new Vector3(width * .28f, height + 1.25f, depth * .12f),
                new Vector3(.65f, 2.3f, .65f),
                new Color(.25f, .23f, .21f));

            Art.Box(
                root.transform,
                "Supply_Crate",
                new Vector3(width * .28f, .75f, depth * .24f),
                new Vector3(1.1f, .8f, .9f),
                new Color(.34f, .26f, .14f));
        }

        void GabledRoof(
            Transform parent,
            float width,
            float depth,
            float wallHeight,
            Color color)
        {
            const float pitch = 28f;
            float radians = pitch * Mathf.Deg2Rad;
            float panelWidth = width * .5f / Mathf.Cos(radians);
            float roofRise = Mathf.Tan(radians) * width * .5f;

            for (int side = -1; side <= 1; side += 2)
            {
                var panel = Art.Shape(
                    parent,
                    side < 0 ? "Roof_Left" : "Roof_Right",
                    PrimitiveType.Cube,
                    new Vector3(
                        side * width * .25f,
                        wallHeight + roofRise * .5f,
                        0),
                    new Vector3(panelWidth + .7f, .25f, depth + 1.1f),
                    color);

                panel.transform.localRotation =
                    Quaternion.Euler(0, 0, side * pitch);
            }
        }

        void Window(
            Transform parent,
            Vector3 position,
            Vector3 size,
            Color glassColor)
        {
            Art.Shape(
                parent,
                "Window_Glass",
                PrimitiveType.Cube,
                position,
                size,
                glassColor,
                false);

            // Thick frame so windows remain readable from a distance.
            Vector3 frame = size;
            frame.x = Mathf.Max(frame.x, .12f);
            frame.y = Mathf.Max(frame.y, .12f);
            frame.z = Mathf.Max(frame.z, .12f);
        }

        void BuildYards(
            Vector3[] north,
            Vector3[] south,
            Color fenceColor)
        {
            foreach (Vector3 p in north)
                YardFence(p + new Vector3(0, 0, 4), 15, 19, fenceColor);

            foreach (Vector3 p in south)
                YardFence(p + new Vector3(0, 0, -4), 15, 19, fenceColor);
        }

        void YardFence(
            Vector3 center,
            float width,
            float depth,
            Color color)
        {
            float y = center.y + .65f;

            for (int i = -2; i <= 2; i++)
            {
                float x = center.x + i * width / 4f;

                Art.Box(
                    Settlement,
                    "Fence_Post",
                    new Vector3(x, y, center.z - depth * .5f),
                    new Vector3(.14f, 1.3f, .14f),
                    color);

                Art.Box(
                    Settlement,
                    "Fence_Post",
                    new Vector3(x, y, center.z + depth * .5f),
                    new Vector3(.14f, 1.3f, .14f),
                    color);
            }

            Art.Box(
                Settlement,
                "Fence_Rail",
                new Vector3(center.x, y + .18f, center.z - depth * .5f),
                new Vector3(width, .12f, .12f),
                color);

            Art.Box(
                Settlement,
                "Fence_Rail",
                new Vector3(center.x, y + .18f, center.z + depth * .5f),
                new Vector3(width, .12f, .12f),
                color);
        }

        void IndustrialBuilding(
            Vector3 position,
            Vector3 size,
            Color wallColor,
            Color roofColor,
            Color glassColor,
            string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(Settlement, false);
            root.transform.localPosition = position;

            float width = size.x;
            float height = size.y;
            float depth = size.z;

            Art.Box(
                root.transform,
                "Foundation",
                new Vector3(0, .2f, 0),
                new Vector3(width + 1, .4f, depth + 1),
                new Color(.26f, .27f, .26f));

            Art.Box(
                root.transform,
                "Rear_Wall",
                new Vector3(0, height * .5f, depth * .5f),
                new Vector3(width, height, .35f),
                wallColor);

            Art.Box(
                root.transform,
                "Left_Wall",
                new Vector3(-width * .5f, height * .5f, 0),
                new Vector3(.35f, height, depth),
                wallColor);

            Art.Box(
                root.transform,
                "Right_Wall",
                new Vector3(width * .5f, height * .5f, 0),
                new Vector3(.35f, height, depth),
                wallColor);

            float shutterWidth = 6f;
            float sideWidth = (width - shutterWidth) * .5f;

            Art.Box(
                root.transform,
                "Front_Left",
                new Vector3(-(shutterWidth + sideWidth) * .5f, height * .5f, -depth * .5f),
                new Vector3(sideWidth, height, .35f),
                wallColor);

            Art.Box(
                root.transform,
                "Front_Right",
                new Vector3((shutterWidth + sideWidth) * .5f, height * .5f, -depth * .5f),
                new Vector3(sideWidth, height, .35f),
                wallColor);

            Art.Box(
                root.transform,
                "Shutter_Lintel",
                new Vector3(0, height - 1.2f, -depth * .5f),
                new Vector3(shutterWidth, 2.4f, .35f),
                wallColor);

            Art.Box(
                root.transform,
                "Roof",
                new Vector3(0, height + .2f, 0),
                new Vector3(width + 1.2f, .4f, depth + 1.2f),
                roofColor);

            for (int i = -2; i <= 2; i++)
            {
                Window(
                    root.transform,
                    new Vector3(
                        i * (width / 6f),
                        height * .68f,
                        -depth * .5f - .20f),
                    new Vector3(2.5f, 1.6f, .08f),
                    glassColor);
            }

            for (int i = 0; i < 4; i++)
            {
                Art.Box(
                    root.transform,
                    "Interior_Crate_" + i,
                    new Vector3(-width * .28f + i * 3.2f, .8f, depth * .12f),
                    new Vector3(2.4f, 1.6f, 2.4f),
                    new Color(.31f, .24f, .13f));
            }
        }

        void BuildCheckpoint(Color concrete, Color rust)
        {
            House(
                new Vector3(335, 60, -240),
                8,
                10,
                4.2f,
                concrete,
                new Color(.16f, .17f, .17f),
                rust,
                new Color(.25f, .34f, .37f),
                "Checkpoint_Guardhouse",
                0f);

            for (int i = 0; i < 8; i++)
            {
                Art.Box(
                    Settlement,
                    "Checkpoint_Barricade_" + i,
                    new Vector3(280 + i * 6, 60.65f, -205),
                    new Vector3(4, 1.3f, 1.4f),
                    i % 2 == 0 ? concrete : rust);
            }

            Art.Box(
                Settlement,
                "Checkpoint_Boom",
                new Vector3(322, 62.2f, -205),
                new Vector3(12, .22f, .22f),
                rust);
        }

        void BuildStreetlights(Color concrete)
        {
            for (int i = 0; i < 15; i++)
            {
                float x = -350 + i * 50;

                Art.Shape(
                    Settlement,
                    "Streetlight_Pole",
                    PrimitiveType.Cylinder,
                    new Vector3(x, 63, 16),
                    new Vector3(.18f, 3f, .18f),
                    concrete);

                Art.Box(
                    Settlement,
                    "Streetlight_Arm",
                    new Vector3(x + .65f, 65.85f, 16),
                    new Vector3(1.5f, .12f, .12f),
                    concrete);

                Art.Box(
                    Settlement,
                    "Streetlight_Lamp",
                    new Vector3(x + 1.35f, 65.65f, 16),
                    new Vector3(.55f, .22f, .45f),
                    new Color(.72f, .69f, .52f));
            }
        }

        void RegisterPOI(
            string name,
            Vector3 position,
            Color mapColor,
            float mapScale)
        {
            pointsOfInterest.Add(
                new SectorPointOfInterest(
                    name,
                    position,
                    mapColor,
                    mapScale));
        }

        void BuildClinic(
            Vector3 position,
            Color wallColor,
            Color roofColor,
            Color glassColor)
        {
            IndustrialBuilding(
                position,
                new Vector3(18, 5.8f, 20),
                wallColor,
                roofColor,
                glassColor,
                "Clinic");

            Art.Box(
                Settlement,
                "Clinic_Sign_Vertical",
                position + new Vector3(0, 6.8f, -10.3f),
                new Vector3(.6f, 3.6f, .25f),
                new Color(.75f, .12f, .12f));

            Art.Box(
                Settlement,
                "Clinic_Sign_Horizontal",
                position + new Vector3(0, 6.8f, -10.3f),
                new Vector3(2.6f, .6f, .25f),
                new Color(.75f, .12f, .12f));

            RegisterPOI(
                "Clinic",
                position,
                new Color(.82f, .18f, .18f),
                2.4f);
        }

        void BuildGasStation(
            Vector3 position,
            Color concrete,
            Color rust,
            Color glass)
        {
            var root = new GameObject("Gas_Station");
            root.transform.SetParent(Settlement, false);
            root.transform.localPosition = position;

            Art.Box(
                root.transform,
                "Shop",
                new Vector3(-8, 2.7f, 4),
                new Vector3(12, 5.4f, 10),
                concrete);

            Window(
                root.transform,
                new Vector3(-8, 3.0f, -1.05f),
                new Vector3(5.0f, 1.8f, .08f),
                glass);

            Art.Box(
                root.transform,
                "Canopy",
                new Vector3(6, 4.8f, -2),
                new Vector3(18, .45f, 10),
                new Color(.30f, .31f, .31f));

            for (int i = -1; i <= 1; i += 2)
            {
                Art.Shape(
                    root.transform,
                    "Canopy_Post",
                    PrimitiveType.Cylinder,
                    new Vector3(6 + i * 6, 2.3f, -2),
                    new Vector3(.22f, 2.3f, .22f),
                    concrete);

                Art.Box(
                    root.transform,
                    "Fuel_Pump",
                    new Vector3(6 + i * 3, 1.0f, -2),
                    new Vector3(1.0f, 2.0f, .9f),
                    rust);
            }

            Art.Shape(
                root.transform,
                "Fuel_Sign_Post",
                PrimitiveType.Cylinder,
                new Vector3(15, 3.5f, 4),
                new Vector3(.18f, 3.5f, .18f),
                concrete);

            Art.Box(
                root.transform,
                "Fuel_Sign",
                new Vector3(15, 7.0f, 4),
                new Vector3(3.2f, 2.2f, .35f),
                rust);

            RegisterPOI(
                "Fuel",
                position,
                new Color(.18f, .72f, .92f),
                2.4f);
        }

        void BuildGarageRow(
            Vector3 position,
            Color wallColor,
            Color roofColor)
        {
            var root = new GameObject("Garage_Row");
            root.transform.SetParent(Settlement, false);
            root.transform.localPosition = position;

            for (int i = 0; i < 5; i++)
            {
                float x = (i - 2) * 7.2f;

                Art.Box(
                    root.transform,
                    "Garage_" + i + "_Rear",
                    new Vector3(x, 2.0f, 5),
                    new Vector3(6.6f, 4.0f, .3f),
                    wallColor);

                Art.Box(
                    root.transform,
                    "Garage_" + i + "_Left",
                    new Vector3(x - 3.3f, 2.0f, 0),
                    new Vector3(.3f, 4.0f, 10),
                    wallColor);

                Art.Box(
                    root.transform,
                    "Garage_" + i + "_Right",
                    new Vector3(x + 3.3f, 2.0f, 0),
                    new Vector3(.3f, 4.0f, 10),
                    wallColor);

                Art.Box(
                    root.transform,
                    "Garage_" + i + "_Roof",
                    new Vector3(x, 4.15f, 0),
                    new Vector3(6.8f, .3f, 10.4f),
                    roofColor);

                if (i % 2 == 0)
                {
                    Art.Box(
                        root.transform,
                        "Garage_" + i + "_Door",
                        new Vector3(x, 1.7f, -4.85f),
                        new Vector3(5.6f, 3.4f, .18f),
                        new Color(.25f, .26f, .25f));
                }
            }

            RegisterPOI(
                "Garages",
                position,
                new Color(.70f, .70f, .66f),
                2.2f);
        }

        void BuildRadioTower(
            Vector3 position,
            Color steel,
            Color rust)
        {
            var root = new GameObject("Radio_Tower");
            root.transform.SetParent(Settlement, false);
            root.transform.localPosition = position;

            for (int i = -1; i <= 1; i += 2)
            {
                Art.Shape(
                    root.transform,
                    "Tower_Leg",
                    PrimitiveType.Cylinder,
                    new Vector3(i * 2.4f, 10f, i * 2.4f),
                    new Vector3(.20f, 10f, .20f),
                    steel);

                Art.Shape(
                    root.transform,
                    "Tower_Leg",
                    PrimitiveType.Cylinder,
                    new Vector3(i * 2.4f, 10f, -i * 2.4f),
                    new Vector3(.20f, 10f, .20f),
                    steel);
            }

            for (int y = 3; y <= 18; y += 3)
            {
                Art.Box(
                    root.transform,
                    "Tower_Brace",
                    new Vector3(0, y, 0),
                    new Vector3(5.2f, .16f, .16f),
                    y % 6 == 0 ? rust : steel);

                Art.Box(
                    root.transform,
                    "Tower_Brace",
                    new Vector3(0, y, 0),
                    new Vector3(.16f, .16f, 5.2f),
                    y % 6 == 0 ? rust : steel);
            }

            Art.Shape(
                root.transform,
                "Antenna",
                PrimitiveType.Cylinder,
                new Vector3(0, 22f, 0),
                new Vector3(.10f, 4f, .10f),
                rust,
                false);

            RegisterPOI(
                "Radio Tower",
                position,
                new Color(.72f, .35f, .90f),
                2.4f);
        }

        static void DisposeTileMeshes(Transform tileRoot)
        {
            foreach (MeshFilter filter in tileRoot.GetComponentsInChildren<MeshFilter>())
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh != null &&
                    (mesh.name.StartsWith("Road_") ||
                     mesh.name == "Water_Surface"))
                {
                    Destroy(mesh);
                }
            }
        }

        void OnDestroy()
        {
            // Baked editor assets belong to AssetDatabase, not the runtime generator.
            if (!Application.isPlaying) return;

            foreach (Terrain t in tiles.Values)
            {
                if (t == null) continue;
                DisposeTileMeshes(t.transform.parent);
                Destroy(t.terrainData);
            }

            if (layers != null)
            {
                foreach (TerrainLayer layer in layers)
                {
                    Destroy(layer.diffuseTexture);
                    Destroy(layer);
                }
            }

            if (terrainMaterial != null) Destroy(terrainMaterial);
            if (waterMaterial != null) Destroy(waterMaterial);
            Art?.Dispose();
        }
    }
}
