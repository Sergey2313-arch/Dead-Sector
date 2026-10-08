using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadSector
{
    public enum SectorResourceType
    {
        GroundStone,
        DryBush,
        FiberBush,
        CottonPlant,
        ScrapPile,
        TimberPile
    }

    public sealed class SectorResourceNode : MonoBehaviour
    {
        public string Id { get; private set; }
        public string ItemId { get; private set; }
        public int Count { get; private set; }
        public string SecondaryId { get; private set; }
        public int SecondaryCount { get; private set; }
        public SectorResourceType Type { get; private set; }

        public void Configure(
            string id, SectorResourceType type, string item, int count,
            string secondary = null, int secondaryCount = 0)
        {
            Id = id;
            Type = type;
            ItemId = item;
            Count = Mathf.Clamp(count, 1, 6);
            SecondaryId = secondary ?? string.Empty;
            SecondaryCount = Mathf.Max(0, secondaryCount);
        }
    }

    /// <summary>
    /// Stones are collected from the ground; dry bushes yield sticks and fiber;
    /// cotton plants yield cotton. Gathering uses stable world cell IDs and
    /// persists across tile unloading and JSON saves.
    /// </summary>
    public sealed class SectorResources : MonoBehaviour
    {
        const int CellSize = 65;
        const float SpawnRange = 160f;
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

            Color[] colors =
            {
                new Color(.44f, .43f, .40f), // ground stone
                new Color(.35f, .25f, .13f), // dry branches
                new Color(.25f, .41f, .16f), // live fiber shrub
                new Color(.92f, .87f, .76f), // cotton
                new Color(.32f, .33f, .34f), // scrap
                new Color(.36f, .21f, .11f)  // timber
            };

            materials = new Material[colors.Length];
            for (int i = 0; i < colors.Length; i++)
                materials[i] = new Material(shader) { color = colors[i] };
        }

        void Update()
        {
            if (player == null || !player.Ready || Time.time < nextRefresh)
                return;

            nextRefresh = Time.time + 1.1f;
            Refresh();
        }

        static uint Hash(int x, int z)
        {
            unchecked
            {
                uint h = 2166136261;
                h = (h ^ (uint)x) * 16777619;
                return (h ^ (uint)z) * 16777619;
            }
        }

        void Refresh()
        {
            Vector3 playerPos = player.transform.position;
            int cellX = Mathf.FloorToInt((playerPos.x + 4000f) / CellSize);
            int cellZ = Mathf.FloorToInt((playerPos.z + 4000f) / CellSize);
            var expected = new HashSet<string>(StringComparer.Ordinal);

            for (int dx = -3; dx <= 3; dx++)
            {
                for (int dz = -3; dz <= 3; dz++)
                {
                    int x = cellX + dx;
                    int z = cellZ + dz;
                    uint hash = Hash(x, z);
                    string id = "resource_" + x + "_" + z;

                    if (harvested.Contains(id))
                        continue;

                    float px = -4000f + (x + .5f) * CellSize +
                        (int)((hash >> 8) % 28) - 14f;
                    float pz = -4000f + (z + .5f) * CellSize +
                        (int)((hash >> 16) % 28) - 14f;

                    if (px < -3950f || px > 3950f ||
                        pz < -3950f || pz > 3950f)
                        continue;

                    float dX = px - playerPos.x;
                    float dZ = pz - playerPos.z;

                    if (dX * dX + dZ * dZ > SpawnRange * SpawnRange ||
                        SectorGeography.TryGetWaterLevel(px, pz, out _) ||
                        SectorGeography.DistanceToRoad(px, pz) < 9f)
                        continue;

                    expected.Add(id);
                    if (!active.ContainsKey(id))
                        SpawnNode(id, new Vector2(px, pz), hash);
                }
            }

            var unload = new List<string>();
            foreach (var pair in active)
                if (!expected.Contains(pair.Key))
                    unload.Add(pair.Key);

            foreach (string id in unload)
            {
                if (active[id] != null)
                    Destroy(active[id].gameObject);
                active.Remove(id);
            }
        }

        void SpawnNode(string id, Vector2 pos, uint hash)
        {
            // More stones and dry bushes than rare scrap: the first tools
            // should be craftable by exploring the starting 160m area.
            int roll = (int)(hash % 12);
            SectorResourceType type = roll < 3 ? SectorResourceType.GroundStone :
                roll < 6 ? SectorResourceType.DryBush :
                roll < 8 ? SectorResourceType.FiberBush :
                roll < 10 ? SectorResourceType.CottonPlant :
                roll == 10 ? SectorResourceType.ScrapPile :
                SectorResourceType.TimberPile;

            string item = type == SectorResourceType.GroundStone ? "stone" :
                type == SectorResourceType.DryBush ? "stick" :
                type == SectorResourceType.FiberBush ? "plant_fiber" :
                type == SectorResourceType.CottonPlant ? "cotton" :
                type == SectorResourceType.ScrapPile ? "scrap" : "wood";

            string extra = type == SectorResourceType.DryBush
                ? "plant_fiber" : string.Empty;

            int count = 1 + (int)((hash >> 4) % 3);
            float height = SectorLayout.Height(pos.x, pos.y);
            GameObject node = GameObject.CreatePrimitive(
                type == SectorResourceType.GroundStone ||
                type == SectorResourceType.ScrapPile
                    ? PrimitiveType.Cube : PrimitiveType.Capsule);

            node.name = "Gather_" + type + "_" + id;
            node.transform.SetParent(transform, true);
            node.transform.position = new Vector3(pos.x, height + .36f, pos.y);
            node.transform.localScale =
                type == SectorResourceType.GroundStone
                    ? new Vector3(.65f, .45f, .55f)
                    : type == SectorResourceType.CottonPlant
                        ? new Vector3(.65f, 1.15f, .65f)
                        : type == SectorResourceType.DryBush
                            ? new Vector3(1f, .72f, 1f)
                            : new Vector3(.8f, .75f, .8f);

            node.GetComponent<Renderer>().sharedMaterial =
                materials[(int)type];

            var resource = node.AddComponent<SectorResourceNode>();
            resource.Configure(id, type, item, count, extra,
                string.IsNullOrEmpty(extra) ? 0 : 1);
            active.Add(id, resource);
        }

        public SectorResourceNode Nearby()
        {
            if (player == null) return null;
            SectorResourceNode nearest = null;
            float closest = InteractRange * InteractRange;

            foreach (SectorResourceNode node in active.Values)
            {
                if (node == null) continue;
                float d = (player.transform.position -
                    node.transform.position).sqrMagnitude;
                if (d >= closest) continue;

                nearest = node;
                closest = d;
            }

            return nearest;
        }

        public bool Harvest(
            SectorResourceNode node, SectorInventory inventory, out string label)
        {
            label = string.Empty;
            if (node == null || inventory == null ||
                !active.ContainsKey(node.Id) || harvested.Contains(node.Id))
                return false;

            // Validate BOTH outputs before changing inventory.
            var trial = new SectorInventory(
                inventory.SlotLimit, inventory.MaxWeight);
            trial.Import(inventory.Export());
            if (!trial.Add(node.ItemId, node.Count) ||
                (!string.IsNullOrEmpty(node.SecondaryId) &&
                 !trial.Add(node.SecondaryId, node.SecondaryCount)))
                return false;

            inventory.Add(node.ItemId, node.Count);
            if (!string.IsNullOrEmpty(node.SecondaryId))
                inventory.Add(node.SecondaryId, node.SecondaryCount);

            label = SectorItems.Get(node.ItemId).Label + " ×" + node.Count;
            if (!string.IsNullOrEmpty(node.SecondaryId))
                label += " + " + SectorItems.Get(node.SecondaryId).Label;

            harvested.Add(node.Id);
            active.Remove(node.Id);
            Destroy(node.gameObject);
            return true;
        }

        public List<string> Export() => new List<string>(harvested);

        public void Import(IEnumerable<string> ids)
        {
            harvested.Clear();
            if (ids != null)
                foreach (string id in ids)
                    if (!string.IsNullOrEmpty(id))
                        harvested.Add(id);

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
