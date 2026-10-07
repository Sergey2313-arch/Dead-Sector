using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeadSector
{
    public sealed class SectorWorld : MonoBehaviour
    {
        public SectorPlayer player;
        public bool Ready { get; private set; }
        public int LoadedTiles => tiles.Count;
        public string Status { get; private set; } = "Generating terrain...";
        public SectorArt Art { get; private set; }
        readonly Dictionary<Vector2Int, Terrain> tiles = new Dictionary<Vector2Int, Terrain>();
        TerrainLayer[] layers;
        Material terrainMaterial;
        Vector2Int last = new Vector2Int(-99, -99);
        bool streaming;
        void Awake() => InitializeArt();
        void InitializeArt()
        {
            if (Art != null) return;
            Art = new SectorArt();
            layers = new[] { Layer(new Color(.23f, .28f, .16f)), Layer(new Color(.3f, .26f, .2f)), Layer(new Color(.35f, .37f, .37f)) };
            terrainMaterial = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));
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
                var terrain = tiles[key]; Destroy(terrain.terrainData); Destroy(terrain.transform.parent.gameObject); tiles.Remove(key);
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
                int layer = road < 9 ? 1 : SectorLayout.Height(wx, wz) > 185 ? 2 : 0;
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
                if (new Vector2(wx, wz).magnitude < 470 || Mathf.Abs(wz) < 28 || Mathf.Abs(wx - 300) < 28) continue;
                float h = data.GetInterpolatedHeight(x / 1000, z / 1000);
                var tree = new GameObject("Pine"); tree.transform.SetParent(root.transform, false); tree.transform.localPosition = new Vector3(x, h, z);
                float size = 6 + (float)random.NextDouble() * 5;
                Art.Shape(tree.transform, "Trunk", PrimitiveType.Cylinder, Vector3.up * size * .25f, new Vector3(.5f, size * .25f, .5f), new Color(.2f, .14f, .09f));
                Art.Shape(tree.transform, "Crown", PrimitiveType.Sphere, Vector3.up * size * .7f, new Vector3(size * .5f, size * .8f, size * .5f), new Color(.1f, .19f, .12f), false);
            }
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
            Settlement = new GameObject("Settlement_Prototype").transform; Settlement.SetParent(transform, false);
            Color concrete = new Color(.4f, .41f, .38f), rust = new Color(.32f, .17f, .1f);
            for (int i = 0; i < 6; i++)
            {
                House(new Vector3(-210 + i * 65, 60, 75), 12, 16, 5, concrete, "Village house");
                House(new Vector3(-210 + i * 65, 60, -75), 12, 16, 5, concrete, "Village house");
            }
            House(new Vector3(220, 60, 150), 32, 42, 9, concrete, "Factory");
            House(new Vector3(220, 60, 230), 26, 35, 7, concrete, "Warehouse");
            for (int i = 0; i < 8; i++) Art.Box(Settlement, "Cargo container", new Vector3(180 + i % 4 * 13, 61.5f, 310 + i / 4 * 9), new Vector3(10, 3, 5), rust);
            House(new Vector3(335, 60, -240), 8, 10, 4, concrete, "Checkpoint guardhouse");
            for (int i = 0; i < 8; i++) Art.Box(Settlement, "Checkpoint barricade", new Vector3(280 + i * 6, 60.65f, -205), new Vector3(4, 1.3f, 1.4f), concrete);
            for (int i = 0; i < 15; i++)
            {
                Art.Shape(Settlement, "Streetlight pole", PrimitiveType.Cylinder, new Vector3(-350 + i * 50, 63, 16), new Vector3(.2f, 3, .2f), concrete);
                Art.Box(Settlement, "Streetlight", new Vector3(-350 + i * 50, 66, 15), new Vector3(1.5f, .2f, .5f), concrete);
            }
            // Small collision / jump course near the spawn.
            for (int i = 0; i < 5; i++) Art.Box(Settlement, "Jump test " + i, new Vector3(20 + i * 3, 60 + (.2f + i * .12f), 38), new Vector3(2, .4f + i * .24f, 2), rust);
        }
        void House(Vector3 position, float width, float depth, float height, Color color, string name)
        {
            var root = new GameObject(name); root.transform.SetParent(Settlement, false); root.transform.localPosition = position;
            Art.Box(root.transform, "Floor", new Vector3(0, .05f, 0), new Vector3(width, .1f, depth), color);
            Art.Box(root.transform, "Roof", new Vector3(0, height, 0), new Vector3(width + 1, .3f, depth + 1), new Color(.21f, .22f, .23f));
            Art.Box(root.transform, "Rear wall", new Vector3(0, height / 2, depth / 2), new Vector3(width, height, .3f), color);
            foreach (int sign in new[] { -1, 1 })
            {
                Art.Box(root.transform, "Side wall", new Vector3(sign * width / 2, height / 2, 0), new Vector3(.3f, height, depth), color);
                // 2 m wide / 2.6 m tall open doorway facing the road.
                Art.Box(root.transform, "Front wall", new Vector3(sign * (width / 4 + .5f), height / 2, -depth / 2), new Vector3(width / 2 - 1, height, .3f), color);
            }
            Art.Box(root.transform, "Door lintel", new Vector3(0, (height + 2.6f) / 2, -depth / 2), new Vector3(2, height - 2.6f, .3f), color);
            Art.Box(root.transform, "Supply crate", new Vector3(width / 3, .5f, depth / 3), Vector3.one, new Color(.35f, .27f, .14f));
        }
        void OnDestroy()
        {
            // Baked editor assets belong to AssetDatabase, not the runtime generator.
            if (!Application.isPlaying) return;
            foreach (var t in tiles.Values) if (t != null) Destroy(t.terrainData);
            if (layers != null) foreach (var layer in layers) { Destroy(layer.diffuseTexture); Destroy(layer); }
            if (terrainMaterial != null) Destroy(terrainMaterial);
            Art?.Dispose();
        }
    }
}
