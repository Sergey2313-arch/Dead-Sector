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
        TimberPile,
        Tree,
        Ore
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
        // Refresh runs every 1.1s. Reuse scratch containers instead of
        // producing a new HashSet/List on each scan.
        readonly HashSet<string> expected =
            new HashSet<string>(StringComparer.Ordinal);
        readonly List<string> unload = new List<string>();

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
                new Color(.36f, .21f, .11f), // timber
                new Color(.32f, .22f, .12f), // standing tree trunk
                new Color(.40f, .44f, .45f)  // ore outcrop
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
            expected.Clear();
            unload.Clear();

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
            int roll = (int)(hash % 18);
            SectorResourceType type = roll < 3 ? SectorResourceType.GroundStone :
                roll < 6 ? SectorResourceType.DryBush :
                roll < 8 ? SectorResourceType.FiberBush :
                roll < 10 ? SectorResourceType.CottonPlant :
                roll == 10 ? SectorResourceType.ScrapPile :
                roll == 11 ? SectorResourceType.TimberPile :
                roll < 15 ? SectorResourceType.Tree :
                SectorResourceType.Ore;

            string item = type == SectorResourceType.GroundStone ? "stone" :
                type == SectorResourceType.DryBush ? "stick" :
                type == SectorResourceType.FiberBush ? "plant_fiber" :
                type == SectorResourceType.CottonPlant ? "cotton" :
                type == SectorResourceType.ScrapPile ||
                type == SectorResourceType.Ore ? "scrap" : "wood";

            string extra = type == SectorResourceType.DryBush
                ? "plant_fiber" : string.Empty;

            int count = type == SectorResourceType.Tree
                ? 4 + (int)((hash >> 4) % 3) :
                type == SectorResourceType.Ore
                    ? 2 + (int)((hash >> 4) % 3)
                    : 1 + (int)((hash >> 4) % 3);
            float height = SectorLayout.Height(pos.x, pos.y);
            GameObject node = GameObject.CreatePrimitive(
                type == SectorResourceType.GroundStone ||
                type == SectorResourceType.ScrapPile ||
                type == SectorResourceType.Ore
                    ? PrimitiveType.Cube :
                type == SectorResourceType.Tree
                    ? PrimitiveType.Cylinder : PrimitiveType.Capsule);

            node.name = "Gather_" + type + "_" + id;
            node.transform.SetParent(transform, true);
            node.transform.position = new Vector3(
                pos.x, height + (type == SectorResourceType.Tree ? 1.8f :
                    type == SectorResourceType.Ore ? .55f : .36f), pos.y);
            node.transform.localScale =
                type == SectorResourceType.Tree
                    ? new Vector3(.42f, 1.75f, .42f)
                    : type == SectorResourceType.Ore
                        ? new Vector3(1.4f, 1.1f, 1.25f)
                    : type == SectorResourceType.GroundStone
                        ? new Vector3(.65f, .45f, .55f)
                    : type == SectorResourceType.CottonPlant
                        ? new Vector3(.65f, 1.15f, .65f)
                    : type == SectorResourceType.DryBush
                        ? new Vector3(1f, .72f, 1f)
                        : new Vector3(.8f, .75f, .8f);

            node.GetComponent<Renderer>().sharedMaterial =
                materials[(int)type];

            // Distinct canopy makes harvestable trees obvious at a glance.
            if (type == SectorResourceType.Tree)
            {
                var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crown.name = "Harvestable_Tree_Canopy";
                crown.transform.SetParent(node.transform, true);
                crown.transform.position = node.transform.position +
                    Vector3.up * 1.8f;
                crown.transform.localScale = new Vector3(2.4f, 2.1f, 2.4f);
                crown.GetComponent<Renderer>().sharedMaterial =
                    materials[(int)SectorResourceType.FiberBush];
                // Only the trunk is targetable; leaves must not block players.
                Collider canopyCollider = crown.GetComponent<Collider>();
                if (canopyCollider != null) canopyCollider.enabled = false;
            }

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

        public static bool RequiresTool(SectorResourceType type) =>
            type == SectorResourceType.Tree || type == SectorResourceType.Ore;

        public static string RequiredToolName(SectorResourceType type) =>
            type == SectorResourceType.Tree ? "ТОПОР" :
            type == SectorResourceType.Ore ? "КИРКА" : "";

        public static bool CorrectTool(SectorResourceType type, string itemId)
        {
            if (type == SectorResourceType.Tree)
                return itemId == "stone_axe" || itemId == "axe";
            if (type == SectorResourceType.Ore)
                return itemId == "stone_pickaxe" || itemId == "pickaxe";
            return true;
        }

        public SectorResourceNode NearbyToolNode(Vector3 forward)
        {
            if (player == null) return null;
            forward.y = 0f;
            if (forward.sqrMagnitude < .01f) return null;
            forward.Normalize();
            SectorResourceNode nearest = null;
            float best = InteractRange * InteractRange;
            foreach (SectorResourceNode node in active.Values)
            {
                if (node == null || !RequiresTool(node.Type))
                    continue;
                Vector3 delta = node.transform.position - player.transform.position;
                delta.y = 0f;
                float sqr = delta.sqrMagnitude;
                if (sqr < .01f || sqr >= best) continue;
                if (Vector3.Dot(forward, delta / Mathf.Sqrt(sqr)) < .45f)
                    continue;
                best = sqr;
                nearest = node;
            }
            return nearest;
        }

        public bool StrikeNearest(
            string toolId, SectorInventory inventory, Vector3 forward,
            out string label, out string warning)
        {
            label = string.Empty;
            warning = string.Empty;
            SectorResourceNode node = NearbyToolNode(forward);
            if (node == null) return false;

            if (!CorrectTool(node.Type, toolId))
            {
                warning = "Нужен инструмент: " + RequiredToolName(node.Type) +
                    ". Создайте его в меню C.";
                return true;
            }
            if (!HarvestInternal(node, inventory, out label))
                warning = "Не хватает свободного места в рюкзаке";
            return true;
        }

        public bool Harvest(
            SectorResourceNode node, SectorInventory inventory, out string label)
        {
            label = string.Empty;
            if (node == null || RequiresTool(node.Type))
                return false;
            return HarvestInternal(node, inventory, out label);
        }

        bool HarvestInternal(
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
