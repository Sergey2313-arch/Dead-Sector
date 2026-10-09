using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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

    public readonly struct SectorInteriorCache
    {
        public readonly string Id;
        public readonly string Label;
        public readonly Vector2 Position;

        public SectorInteriorCache(string id, string label, Vector2 position)
        {
            Id = id;
            Label = label;
            Position = position;
        }
    }

    public sealed class SectorWorld : MonoBehaviour
    {
        // Metres. A standing player is ~1.78m; homes should read as
        // single-storey buildings, not warehouse-height boxes.
        // A normal entrance: 1.15m wide and 2.12m CLEAR ABOVE THE FINISHED FLOOR.
        // The old code measured the opening from terrain zero and lost 42cm
        // to the raised floor, leaving a 1.8m crawl-sized opening.
        public const float ResidentialDoorWidth = 1.15f;
        public const float ResidentialDoorHeight = 2.12f;
        public const float ResidentialFloorTop = .42f;
        public const float ResidentialStepTop = .20f;
        public const float ResidentialPorchTop = .44f;
        public SectorPlayer player;
        public SectorResources resources;
        public bool Ready { get; private set; }
        public IReadOnlyList<SectorPointOfInterest> PointsOfInterest => pointsOfInterest;
        readonly List<SectorInteriorCache> interiorCaches =
            new List<SectorInteriorCache>();
        public IReadOnlyList<SectorInteriorCache> InteriorCaches => interiorCaches;
        public int LoadedTiles => tiles.Count;
        public string Status { get; private set; } = "Generating terrain...";
        public SectorArt Art { get; private set; }
        readonly Dictionary<Vector2Int, Terrain> tiles = new Dictionary<Vector2Int, Terrain>();
        readonly List<SectorPointOfInterest> pointsOfInterest =
            new List<SectorPointOfInterest>();
        TerrainLayer[] layers;
        Texture2D grassTexture;
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
            grassTexture = BuildGrassBladeTexture();
            Shader terrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            if (terrainShader == null)
                terrainShader = Shader.Find("Nature/Terrain/Standard");

            terrainMaterial = new Material(terrainShader);
            // Explicitly override shader surface settings as well as
            // individual TerrainLayers. Keep daylight hills non-metallic.
            if (terrainMaterial.HasProperty("_Metallic"))
                terrainMaterial.SetFloat("_Metallic", 0f);
            if (terrainMaterial.HasProperty("_Smoothness"))
                terrainMaterial.SetFloat("_Smoothness", 0f);

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
            BuildTerrainGrass(data, origin);
            var go = Terrain.CreateTerrainGameObject(data); go.name = "Terrain"; go.transform.SetParent(root.transform, false);
            var terrain = go.GetComponent<Terrain>(); terrain.materialTemplate = terrainMaterial; terrain.heightmapPixelError = 8;
            terrain.basemapDistance = 800; terrain.drawInstanced = true;
            terrain.drawTreesAndFoliage = true;
            terrain.detailObjectDistance = 65f;
            terrain.detailObjectDensity = .8f;
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

                SectorBiome biome = SectorBiomeRules.At(wx, wz);
                if ((float)random.NextDouble() >
                    SectorBiomeRules.TreeDensity(biome))
                    continue;

                // Stable per-tile ID persists chopping through terrain streaming.
                string treeId = "pine_" + key.x + "_" + key.y + "_" + i;
                if (resources != null && resources.WasHarvested(treeId))
                    continue;

                float h = data.GetInterpolatedHeight(x / 1000, z / 1000);
                var tree = new GameObject("Pine"); tree.transform.SetParent(root.transform, false); tree.transform.localPosition = new Vector3(x, h, z);
                float size = 5.5f + (float)random.NextDouble() * 4f;
                Art.Shape(tree.transform, "Trunk", PrimitiveType.Cylinder,
                    Vector3.up * size * .38f,
                    new Vector3(.30f, size * .38f, .30f),
                    new Color(.23f, .16f, .11f));

                if (biome == SectorBiome.ConiferForest)
                {
                    // Overlapping tapered crowns read as conifer rather than
                    // an enormous featureless black sphere at night.
                    for (int crownIndex = 0; crownIndex < 3; crownIndex++)
                    {
                        float tier = crownIndex;
                        Art.Shape(tree.transform,
                            "Pine_Boughs_" + crownIndex, PrimitiveType.Cylinder,
                            Vector3.up * (size * (.52f + tier * .17f)),
                            new Vector3(1.8f - tier * .42f, .77f, 1.8f - tier * .42f),
                            new Color(.11f + tier * .015f, .24f, .15f), false);
                    }
                }
                else
                {
                    // Natural broad crown for mixed woods and sparse fields.
                    Art.Shape(tree.transform, "Broadleaf_Crown",
                        PrimitiveType.Sphere, Vector3.up * size * .83f,
                        new Vector3(size * .30f, size * .31f, size * .30f),
                        biome == SectorBiome.DrySteppe
                            ? new Color(.39f, .36f, .19f)
                            : new Color(.20f, .36f, .18f), false);
                }
                var harvestable = tree.AddComponent<SectorResourceNode>();
                harvestable.Configure(treeId, SectorResourceType.Tree, "wood", 5);
                resources?.RegisterWorldTree(harvestable);
            }
            SectorLandscapeBuilder.Build(
                root.transform, origin, roadMaterial, waterMaterial);

            SectorLandmarkBuilder.Build(root.transform, origin, Art);
            SectorWorldProps.Build(root.transform, origin, Art);

            return terrain;
        }
        // Lightweight native Terrain vegetation. Details stream with each
        // 1km tile and are culled past 65m, unlike thousands of GameObjects.
        // The visible grass blades are a transparent procedural billboard;
        // the existing TerrainLayer still supplies the ground's matte soil.
        const int GrassResolution = 256;

        static Texture2D BuildGrassBladeTexture()
        {
            const int width = 32;
            const int height = 64;
            var texture = new Texture2D(
                width, height, TextureFormat.RGBA32, false);
            texture.name = "DeadSector_Grass_Blade";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[width * height];
            var blade = new Color32(164, 187, 100, 235);

            for (int y = 0; y < height; y++)
            {
                float growth = (float)y / (height - 1);
                float thickness = Mathf.Lerp(4.5f, .4f, growth);
                // Three overlapping leaves create a recognisable tuft.
                for (int x = 0; x < width; x++)
                {
                    bool leaf0 = Mathf.Abs(x - (15f - growth * 9f)) < thickness;
                    bool leaf1 = Mathf.Abs(x - (16f + growth * 8f)) < thickness;
                    bool leaf2 = Mathf.Abs(x - 16f) <
                        Mathf.Lerp(3.2f, 0f, growth);
                    if (leaf0 || leaf1 || leaf2)
                        pixels[y * width + x] = blade;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        public static bool CanGrowGrass(float x, float z)
        {
            // Suppress grass over built-up central roads/compound, highways,
            // water and high barren mountain peaks.
            if (x * x + z * z < 165f * 165f ||
                SectorGeography.DistanceToRoad(x, z) < 7f ||
                SectorGeography.TryGetWaterLevel(x, z, out _))
                return false;
            float ground = SectorLayout.Height(x, z);
            return ground > 35f && ground < 185f;
        }

        void BuildTerrainGrass(TerrainData data, Vector3 origin)
        {
            data.SetDetailResolution(GrassResolution, 16);
            data.detailPrototypes = new[]
            {
                new DetailPrototype
                {
                    prototypeTexture = grassTexture,
                    renderMode = DetailRenderMode.GrassBillboard,
                    healthyColor = new Color(.49f, .70f, .34f),
                    dryColor = new Color(.62f, .61f, .34f),
                    minWidth = .38f,
                    maxWidth = .85f,
                    minHeight = .25f,
                    maxHeight = .55f,
                    noiseSpread = .55f
                },
                new DetailPrototype
                {
                    prototypeTexture = grassTexture,
                    renderMode = DetailRenderMode.GrassBillboard,
                    healthyColor = new Color(.66f, .52f, .28f),
                    dryColor = new Color(.65f, .53f, .28f),
                    minWidth = .35f,
                    maxWidth = .75f,
                    minHeight = .20f,
                    maxHeight = .48f,
                    noiseSpread = .62f
                }
            };

            int[,] density = new int[GrassResolution, GrassResolution];
            int[,] dryDensity = new int[GrassResolution, GrassResolution];
            // Expensive full-world road/water queries are sampled once per
            // 16m coarse cell, then reused by sixteen adjacent detail cells.
            const int coarseResolution = 64;
            bool[,] grow = new bool[coarseResolution, coarseResolution];
            bool[,] dry = new bool[coarseResolution, coarseResolution];
            float coarseStride = SectorLayout.TileSize / (float)coarseResolution;
            for (int z = 0; z < coarseResolution; z++)
                for (int x = 0; x < coarseResolution; x++)
                {
                    float wx = origin.x + (x + .5f) * coarseStride;
                    float wz = origin.z + (z + .5f) * coarseStride;
                    grow[z, x] = CanGrowGrass(wx, wz);
                    SectorBiome biome = SectorBiomeRules.At(wx, wz);
                    dry[z, x] = biome == SectorBiome.DrySteppe ||
                        biome == SectorBiome.RockyHighland;
                }

            for (int z = 0; z < GrassResolution; z++)
            {
                for (int x = 0; x < GrassResolution; x++)
                {
                    // Sparse yet deterministic. Each detail cell spans ~4m
                    // and visible grass is limited by detailObjectDistance.
                    uint hash = unchecked(
                        (uint)(x * 73856093 ^ z * 19349663 ^
                               Mathf.RoundToInt(origin.x) * 83492791 ^
                               Mathf.RoundToInt(origin.z) * 26544357));
                    if ((hash & 3u) == 0u)
                        continue;

                    if (!grow[z / 4, x / 4])
                        continue;

                    if (dry[z / 4, x / 4])
                        dryDensity[z, x] = (hash & 4u) == 0u ? 1 : 2;
                    else
                        density[z, x] = (hash & 4u) == 0u ? 1 : 2;
                }
            }

            data.SetDetailLayer(0, 0, 0, density);
            data.SetDetailLayer(0, 0, 1, dryDensity);
        }

        TerrainLayer Layer(Color color)
        {
            // Diffuse colour variation without shiny plastic gradients:
            // coarse soil mottling + fine grain, reproducible across tiles.
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            texture.name = "DeadSector_MatteTerrain";
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            const float seed = 72.4f;
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float coarse = Mathf.PerlinNoise(
                    seed + x * .083f, seed + y * .083f);
                float fine = Mathf.PerlinNoise(
                    seed + x * .45f, seed + y * .45f);
                float brightness = Mathf.Lerp(.73f, 1.16f, coarse) *
                    Mathf.Lerp(.90f, 1.10f, fine);
                texture.SetPixel(x, y,
                    new Color(
                        Mathf.Clamp01(color.r * brightness),
                        Mathf.Clamp01(color.g * brightness),
                        Mathf.Clamp01(color.b * brightness), 1f));
            }
            texture.Apply(false, false);
            // Ground should be matte. Default TerrainLayer smoothness can
            // cause unrealistically glossy fields under the URP Terrain/Lit shader.
            return new TerrainLayer
            {
                diffuseTexture = texture,
                tileSize = Vector2.one * 8,
                metallic = 0f,
                smoothness = 0f
            };
        }
        public Transform Settlement { get; private set; }

        void RegisterInteriorCache(
            string id, string label, Transform parent, Vector3 localPosition)
        {
            Vector3 worldPos = parent.TransformPoint(localPosition);
            interiorCaches.Add(new SectorInteriorCache(
                "interior_" + id, label,
                new Vector2(worldPos.x, worldPos.z)));
        }

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
                    9.5f + (i % 2) * 1f,
                    11.5f + (i % 3) * .6f,
                    3.15f + (i % 2) * .18f,
                    i % 2 == 0 ? plaster : plasterWarm,
                    roof,
                    wood,
                    glass,
                    "Village_House_N_" + i,
                    0f);
            }

            for (int i = 0; i < southHouses.Length; i++)
            {
                House(
                    southHouses[i],
                    9.5f + ((i + 1) % 2) * 1f,
                    11.5f + (i % 3) * .6f,
                    3.15f + ((i + 1) % 2) * .18f,
                    i % 2 == 0 ? plasterWarm : plaster,
                    roof,
                    wood,
                    glass,
                    "Village_House_S_" + i,
                    180f);
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

            // The legacy test-zone buildings remain for compatibility,
            // but the public minimap and compass now point at the physical
            // sites generated at canonical world coordinates.
            pointsOfInterest.Clear();
            foreach (SectorMapPlan.Location location in SectorMapPlan.Locations)
            {
                if (location.Status == SectorMapPlan.BuildStatus.Planned)
                    continue;

                Color markerColor;
                switch (location.Kind)
                {
                    case SectorMapPlan.LocationKind.Medical:
                        markerColor = new Color(.88f, .24f, .26f);
                        break;
                    case SectorMapPlan.LocationKind.Military:
                        markerColor = new Color(.86f, .70f, .35f);
                        break;
                    case SectorMapPlan.LocationKind.Transport:
                        markerColor = new Color(.21f, .70f, .94f);
                        break;
                    case SectorMapPlan.LocationKind.Wilderness:
                        markerColor = new Color(.27f, .77f, .39f);
                        break;
                    case SectorMapPlan.LocationKind.Hazard:
                        markerColor = new Color(.90f, .37f, .25f);
                        break;
                    default:
                        markerColor = new Color(.94f, .78f, .28f);
                        break;
                }

                RegisterPOI(
                    location.Name,
                    location.AtHeight(SectorLayout.Height(
                        location.MapPosition.x,
                        location.MapPosition.y)),
                    markerColor,
                    2.6f);
            }

            RegisterPOI(
                "Spawn",
                SectorLayout.Spawn,
                new Color(.20f, .88f, 1f),
                1.8f);
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
            const float doorWidth = ResidentialDoorWidth;
            const float doorHeight = ResidentialDoorHeight;
            float doorLintelY = ResidentialFloorTop + doorHeight;
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
                    doorLintelY + (height - doorLintelY) * .5f,
                    frontZ),
                new Vector3(doorWidth, height - doorLintelY, wall),
                wallColor);

            // A real hinged door with a collider; E opens/closes it.
            // The hinge is at the right jamb so +105 degrees swings inward.
            // The door slab starts CLOSED and no longer clips the opening.
            var hinge = new GameObject("FrontDoor_Hinge");
            hinge.transform.SetParent(root.transform, false);
            hinge.transform.localPosition = new Vector3(
                doorWidth * .5f - .06f, 0f, frontZ - .18f);
            SectorDoor entrance = hinge.AddComponent<SectorDoor>();
            entrance.openAngle = 105f;
            entrance.turnSpeed = 8f;

            var leaf = Art.Shape(
                hinge.transform, "FrontDoor_Slab", PrimitiveType.Cube,
                new Vector3(-doorWidth * .5f + .08f,
                    ResidentialPorchTop + doorHeight * .5f, 0f),
                new Vector3(doorWidth - .16f, doorHeight - .08f, .10f),
                woodColor);

            // Dynamic carving lets AI traverse an opened doorway without
            // making a permanently impassable portal in the NavMesh.
            var obstruction = hinge.AddComponent<NavMeshObstacle>();
            obstruction.shape = NavMeshObstacleShape.Box;
            obstruction.center = leaf.transform.localPosition;
            obstruction.size = leaf.transform.localScale;
            obstruction.carving = true;
            obstruction.carveOnlyStationary = true;

            // A .44m porch is higher than the player's .30m stepOffset.
            // Two navigable steps keep the doorway accessible on foot.
            Art.Box(
                root.transform, "Porch",
                new Vector3(0f, ResidentialPorchTop * .5f, frontZ - 1.05f),
                new Vector3(3.0f, ResidentialPorchTop, 1.8f),
                new Color(.34f, .29f, .22f));
            Art.Box(
                root.transform, "Porch_Low_Step",
                new Vector3(0f, ResidentialStepTop * .5f, frontZ - 2.24f),
                new Vector3(2.6f, ResidentialStepTop, .72f),
                new Color(.31f, .27f, .22f));

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

            // Collision-backed household furniture, away from the
            // front centre corridor and the usable door opening.
            Art.Box(root.transform, "House_Bed_Base",
                new Vector3(-width * .26f, .61f, depth * .29f),
                new Vector3(1.65f, .35f, 2.25f),
                new Color(.32f, .24f, .18f));
            Art.Box(root.transform, "House_Bed_Mattress",
                new Vector3(-width * .26f, .88f, depth * .29f),
                new Vector3(1.45f, .20f, 2f),
                new Color(.42f, .42f, .37f));
            Art.Box(root.transform, "House_Table",
                new Vector3(width * .23f, .86f, depth * .02f),
                new Vector3(1.25f, .10f, 1.10f),
                new Color(.38f, .28f, .17f));
            Art.Box(root.transform, "House_Shelf",
                new Vector3(width * .36f, 1.18f, depth * .39f),
                new Vector3(1.15f, 1.5f, .43f),
                new Color(.32f, .25f, .18f));
            Art.Box(root.transform, "Supply_Crate",
                new Vector3(width * .28f, .70f, depth * .24f),
                new Vector3(.75f, .56f, .64f),
                new Color(.34f, .26f, .14f));

            RegisterInteriorCache(name, "Тайник в жилом доме",
                root.transform,
                new Vector3(width * .29f, 0f, depth * .24f));
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
                    // Slopes must rise inward to the ridge; the former sign made a V-shaped roof.
                    Quaternion.Euler(0, 0, SectorRoofGeometry.PanelRotationZ(side, pitch));
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
                YardFence(p, 15, 19, fenceColor, true);

            foreach (Vector3 p in south)
                YardFence(p, 15, 19, fenceColor, false);
        }

        void YardFence(
            Vector3 center, float width, float depth,
            Color color, bool gateAtNegativeZ)
        {
            float y = center.y + .65f;
            float frontZ = center.z + (gateAtNegativeZ ? -1f : 1f) * depth * .5f;
            float rearZ = center.z - (gateAtNegativeZ ? -1f : 1f) * depth * .5f;
            const float gap = 3.4f;

            // Do not place posts/rails across the entrance to the house.
            for (int i = -2; i <= 2; i++)
            {
                float x = center.x + i * width / 4f;
                Art.Box(Settlement, "Fence_Post",
                    new Vector3(x, y, rearZ), new Vector3(.14f, 1.3f, .14f), color);
                if (i != 0)
                    Art.Box(Settlement, "Fence_Post",
                        new Vector3(x, y, frontZ),
                        new Vector3(.14f, 1.3f, .14f), color);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                Art.Box(Settlement, "Fence_Gate_Post",
                    new Vector3(center.x + side * gap * .5f, y, frontZ),
                    new Vector3(.14f, 1.3f, .14f), color);

                float railWidth = (width - gap) * .5f;
                Art.Box(Settlement, "Fence_Rail_Front",
                    new Vector3(center.x + side * (gap + railWidth) * .5f,
                        y + .18f, frontZ),
                    new Vector3(railWidth, .12f, .12f), color);
            }

            Art.Box(Settlement, "Fence_Rail_Rear",
                new Vector3(center.x, y + .18f, rearZ),
                new Vector3(width, .12f, .12f), color);
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

            // Clinics use human-scale doors, factories retain wide bays.
            bool clinic = name == "Clinic";
            float shutterWidth = clinic ? 2.2f : 6f;
            float portalHeight = clinic ? 2.55f : height - 2.4f;
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
                new Vector3(0, portalHeight + (height - portalHeight) * .5f, -depth * .5f),
                new Vector3(shutterWidth, height - portalHeight, .35f),
                wallColor);

            // .40m foundation requires an intermediate step for
            // a player with .30m CharacterController.stepOffset.
            Art.Box(
                root.transform, "Entrance_Low_Step",
                new Vector3(0f, .10f, -depth * .5f - 1.15f),
                new Vector3(shutterWidth + .7f, .20f, 1.6f),
                new Color(.32f, .32f, .30f));

            if (clinic)
            {
                CreateHingedPortal(
                    root.transform, "Clinic_Door",
                    new Vector3(shutterWidth * .5f - .07f, 0f,
                        -depth * .5f - .20f),
                    shutterWidth - .14f, portalHeight,
                    wallColor, true);
            }

            // Basic believable clinic/workshop interior blockout,
            // leaving the entrance and central aisle unobstructed.
            if (clinic)
            {
                for (int bed = -1; bed <= 1; bed += 2)
                {
                    Art.Box(root.transform, "Clinic_Bed_" + bed,
                        new Vector3(bed * 4f, .72f, depth * .19f),
                        new Vector3(1.4f, .40f, 2.5f),
                        new Color(.40f, .45f, .44f));
                    Art.Box(root.transform, "Clinic_Mattress_" + bed,
                        new Vector3(bed * 4f, 1f, depth * .19f),
                        new Vector3(1.25f, .20f, 2.3f),
                        new Color(.72f, .74f, .68f));
                }
                Art.Box(root.transform, "Clinic_Medicine_Cabinet",
                    new Vector3(width * .36f, 1.23f, depth * .36f),
                    new Vector3(1.25f, 1.9f, .58f),
                    new Color(.63f, .66f, .64f));
                RegisterInteriorCache("Clinic", "Аптечный шкаф",
                    root.transform,
                    new Vector3(width * .36f, 0f, depth * .29f));
            }
            else
            {
                Art.Box(root.transform, "Industrial_Workbench",
                    new Vector3(width * .30f, .75f, depth * .28f),
                    new Vector3(2.4f, .32f, 1.0f),
                    new Color(.35f, .32f, .29f));
                RegisterInteriorCache(name, "Инструменты и запчасти",
                    root.transform,
                    new Vector3(width * .29f, 0f, depth * .28f));
            }

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

        // A collision-bearing door on a real hinge, also used for garages.
        void CreateHingedPortal(
            Transform parent, string name, Vector3 hinge,
            float width, float height, Color color, bool hingeRight)
        {
            GameObject pivot = new GameObject(name + "_Hinge");
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = hinge;

            SectorDoor door = pivot.AddComponent<SectorDoor>();
            door.openAngle = hingeRight ? 105f : -105f;
            door.turnSpeed = 8f;

            float leafOffset = (hingeRight ? -1f : 1f) * width * .5f;
            var leaf = Art.Shape(
                pivot.transform, name + "_Slab", PrimitiveType.Cube,
                new Vector3(leafOffset, height * .5f, 0f),
                new Vector3(width, height - .10f, .12f), color);

            NavMeshObstacle nav = pivot.AddComponent<NavMeshObstacle>();
            nav.shape = NavMeshObstacleShape.Box;
            nav.center = leaf.transform.localPosition;
            nav.size = leaf.transform.localScale;
            nav.carving = true;
            nav.carveOnlyStationary = true;
        }

        void BuildCheckpoint(Color concrete, Color rust)
        {
            House(
                new Vector3(335, 60, -240),
                8,
                10,
                3.25f,
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

            // Previously "Shop" was a single solid 12x5.4x10 cube, so
            // the station had no traversable interior at all.
            // Build a modest 3.4m-high shop with a real accessible entrance.
            var shop = new GameObject("Shop");
            shop.transform.SetParent(root.transform, false);
            shop.transform.localPosition = new Vector3(-8f, 0f, 4f);
            const float shopWidth = 12f;
            const float shopDepth = 10f;
            const float shopHeight = 3.4f;
            const float entranceWidth = ResidentialDoorWidth;
            const float entranceHeight = ResidentialDoorHeight;
            const float front = -shopDepth * .5f;
            const float side = (shopWidth - entranceWidth) * .5f;

            Art.Box(shop.transform, "Floor",
                new Vector3(0f, .16f, 0f),
                new Vector3(shopWidth, .32f, shopDepth), concrete);
            Art.Box(shop.transform, "BackWall",
                new Vector3(0f, shopHeight * .5f, -front),
                new Vector3(shopWidth, shopHeight, .24f), concrete);
            for (int dir = -1; dir <= 1; dir += 2)
            {
                Art.Box(shop.transform, dir < 0 ? "LeftWall" : "RightWall",
                    new Vector3(dir * shopWidth * .5f, shopHeight * .5f, 0f),
                    new Vector3(.24f, shopHeight, shopDepth), concrete);
                Art.Box(shop.transform, dir < 0 ? "FrontLeft" : "FrontRight",
                    new Vector3(dir * (entranceWidth + side) * .5f,
                        shopHeight * .5f, front),
                    new Vector3(side, shopHeight, .24f), concrete);
            }
            Art.Box(shop.transform, "EntranceLintel",
                new Vector3(0f,
                    entranceHeight + (shopHeight - entranceHeight) * .5f,
                    front),
                new Vector3(entranceWidth, shopHeight - entranceHeight, .24f),
                concrete);
            Art.Box(shop.transform, "Roof",
                new Vector3(0f, shopHeight + .12f, 0f),
                new Vector3(shopWidth + .4f, .24f, shopDepth + .4f), concrete);
            Art.Box(shop.transform, "EntranceStep",
                new Vector3(0f, .09f, front - .55f),
                new Vector3(2.5f, .18f, 1f), concrete);
            CreateHingedPortal(shop.transform, "Shop_FrontDoor",
                new Vector3(entranceWidth * .5f - .07f, 0f, front - .18f),
                entranceWidth - .14f, entranceHeight, rust, true);
            Art.Box(shop.transform, "Shop_Counter",
                new Vector3(3f, .82f, 1.0f),
                new Vector3(3.2f, 1.1f, .72f),
                new Color(.38f, .30f, .22f));
            for (int shelf = -1; shelf <= 1; shelf += 2)
                Art.Box(shop.transform, "Shop_Shelf_" + shelf,
                    new Vector3(shelf * 3.5f, 1.05f, 2.7f),
                    new Vector3(1.35f, 1.6f, .65f),
                    new Color(.48f, .42f, .31f));
            RegisterInteriorCache("FuelShop", "Продукты магазина",
                shop.transform, new Vector3(3f, 0f, 1.4f));

            Window(shop.transform,
                new Vector3(-3f, 2.25f, front - .14f),
                new Vector3(2.2f, 1f, .08f), glass);
            Window(shop.transform,
                new Vector3(3f, 2.25f, front - .14f),
                new Vector3(2.2f, 1f, .08f), glass);

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
                    // Split a garage gate into two real hinged leaves.
                    // One open leaf already gives >2.5m of walkable space.
                    Color gate = new Color(.25f, .26f, .25f);
                    CreateHingedPortal(
                        root.transform, "Garage_" + i + "_LeftDoor",
                        new Vector3(x - 2.75f, 0f, -4.85f),
                        2.7f, 3.4f, gate, false);
                    CreateHingedPortal(
                        root.transform, "Garage_" + i + "_RightDoor",
                        new Vector3(x + 2.75f, 0f, -4.85f),
                        2.7f, 3.4f, gate, true);
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

            if (grassTexture != null) Destroy(grassTexture);
            if (terrainMaterial != null) Destroy(terrainMaterial);
            if (waterMaterial != null) Destroy(waterMaterial);
            Art?.Dispose();
        }
    }
}
