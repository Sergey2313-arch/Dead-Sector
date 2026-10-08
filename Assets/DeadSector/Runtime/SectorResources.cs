using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadSector
{
    public sealed class SectorResourceNode : MonoBehaviour
    {
        public string Id { get; private set; }
        public string ItemId { get; private set; }
        public int Count { get; private set; }

        public void Configure(string id, string itemId, int count)
        {
            Id = id;
            ItemId = itemId;
            Count = Mathf.Clamp(count, 1, 6);
        }
    }

    /// <summary>
    /// Deterministic nearby harvest nodes: timber, metal scrap and cloth.
    /// Node IDs persist independently of streamed scenery and their harvested
    /// state is exported to JSON saves.
    /// </summary>
    public sealed class SectorResources : MonoBehaviour
    {
        const int CellSize = 180;
        const float SpawnRange = 190f;
        const float InteractRange = 3.5f;

        public SectorPlayer player;

        readonly Dictionary<string, SectorResourceNode> active =
            new Dictionary<string, SectorResourceNode>(StringComparer.Ordinal);

        readonly HashSet<string> harvested =
            new HashSet<string>(StringComparer.Ordinal);

        Material[] materials;
        float nextRefresh;

        public void Configure(SectorPlayer target)
        {
            player = target;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            materials = new Material[3];

            Color[] colors =
            {
                new Color(.30f, .18f, .09f),
                new Color(.31f, .34f, .34f),
                new Color(.28f, .35f, .18f)
            };

            for (int i = 0; i < materials.Length; i++)
                materials[i] = new Material(shader) { color = colors[i] };
        }

        void Update()
        {
            if (player == null || !player.Ready || Time.time < nextRefresh)
                return;

            nextRefresh = Time.time + .85f;
            Refresh();
        }

        static uint Hash(int x, int z)
        {
            unchecked
            {
                uint h = 2166136261;
                h = (h ^ (uint)x) * 16777619;
                h = (h ^ (uint)z) * 16777619;
                return h;
            }
        }

        void Refresh()
        {
            Vector3 playerPos = player.transform.position;
            int cellX = Mathf.FloorToInt((playerPos.x + 4000f) / CellSize);
            int cellZ = Mathf.FloorToInt((playerPos.z + 4000f) / CellSize);

            HashSet<string> expected = new HashSet<string>(StringComparer.Ordinal);

            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dz = -2; dz <= 2; dz++)
                {
                    int x = cellX + dx;
                    int z = cellZ + dz;
                    uint hash = Hash(x, z);
                    string id = "resource_" + x + "_" + z;

                    if (harvested.Contains(id))
                        continue;

                    float px = -4000f + (x + .5f) * CellSize +
                        ((hash >> 8) % 70) - 35f;
                    float pz = -4000f + (z + .5f) * CellSize +
                        ((hash >> 16) % 70) - 35f;

                    if (px < -3950f || px > 3950f ||
                        pz < -3950f || pz > 3950f)
                        continue;

                    Vector2 position = new Vector2(px, pz);

                    if (Vector2.Distance(position,
                        new Vector2(playerPos.x, playerPos.z)) > SpawnRange ||
                        SectorGeography.TryGetWaterLevel(px, pz, out _) ||
                        SectorGeography.DistanceToRoad(px, pz) < 17f)
                        continue;

                    expected.Add(id);

                    if (!active.ContainsKey(id))
                        SpawnNode(id, position, hash);
                }
            }

            var retire = new List<string>();
            foreach (var pair in active)
                if (!expected.Contains(pair.Key))
                    retire.Add(pair.Key);

            foreach (string id in retire)
            {
                if (active[id] != null)
                    Destroy(active[id].gameObject);

                active.Remove(id);
            }
        }

        void SpawnNode(string id, Vector2 pos, uint hash)
        {
            int index = (int)(hash % 3);
            string resource = index == 0
                ? "wood" : index == 1 ? "scrap" : "cloth";
            int amount = 1 + (int)((hash >> 4) % 3);

            GameObject node = GameObject.CreatePrimitive(index == 2
                ? PrimitiveType.Capsule : PrimitiveType.Cube);

            node.name = "Harvest_" + id;
            node.transform.SetParent(transform, true);
            node.transform.position = new Vector3(
                pos.x, SectorLayout.Height(pos.x, pos.y) + .55f, pos.y);

            node.transform.localScale = index == 0
                ? new Vector3(.55f, 1.1f, .60f)
                : index == 1
                    ? new Vector3(1.4f, .65f, .95f)
                    : new Vector3(.8f, 1.3f, .8f);

            if (materials != null && materials.Length > index)
                node.GetComponent<Renderer>().sharedMaterial = materials[index];

            SectorResourceNode component = node.AddComponent<SectorResourceNode>();
            component.Configure(id, resource, amount);
            active.Add(id, component);
        }

        public SectorResourceNode Nearby()
        {
            if (player == null)
                return null;

            SectorResourceNode nearest = null;
            float closest = InteractRange * InteractRange;

            foreach (SectorResourceNode node in active.Values)
            {
                if (node == null)
                    continue;

                float sqr = (player.transform.position -
                    node.transform.position).sqrMagnitude;

                if (sqr >= closest)
                    continue;

                nearest = node;
                closest = sqr;
            }

            return nearest;
        }

        public bool Harvest(
            SectorResourceNode node, SectorInventory inventory,
            out string label)
        {
            label = string.Empty;

            if (node == null || inventory == null ||
                !active.ContainsKey(node.Id) ||
                harvested.Contains(node.Id))
                return false;

            if (!inventory.Add(node.ItemId, node.Count))
                return false;

            label = SectorItems.Get(node.ItemId).Label +
                " ×" + node.Count;
            harvested.Add(node.Id);
            active.Remove(node.Id);
            Destroy(node.gameObject);
            return true;
        }

        public List<string> Export()
        {
            return new List<string>(harvested);
        }

        public void Import(IEnumerable<string> ids)
        {
            harvested.Clear();
            if (ids != null)
            {
                foreach (string id in ids)
                    if (!string.IsNullOrEmpty(id))
                        harvested.Add(id);
            }

            foreach (SectorResourceNode node in active.Values)
                if (node != null)
                    Destroy(node.gameObject);

            active.Clear();
            nextRefresh = 0f;
        }

        void OnDestroy()
        {
            if (materials == null) return;
            foreach (Material material in materials)
                if (material != null) Destroy(material);
        }
    }
}
