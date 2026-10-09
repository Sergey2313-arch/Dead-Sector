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
        public int ToolHitsRemaining { get; private set; }

        public int Strike()
        {
            ToolHitsRemaining = Mathf.Max(0, ToolHitsRemaining - 1);
            return ToolHitsRemaining;
        }

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
            ToolHitsRemaining = type == SectorResourceType.Tree ? 3 :
                type == SectorResourceType.Ore ? 4 : 0;
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

        // Called before a harvested node is destroyed. The gameplay layer
        // persists uncarried material in an on-ground loot cache. Returning
        // false leaves the node and inventory untouched.
        public Func<SectorResourceNode, List<SectorItemStack>, bool> StoreOverflow;

        readonly Dictionary<string, SectorResourceNode> active =
            new Dictionary<string, SectorResourceNode>(StringComparer.Ordinal);
        readonly HashSet<string> harvested =
            new HashSet<string>(StringComparer.Ordinal);

        // Original large trees from SectorWorld are also resource nodes.
        // They are owned by streamed terrain tiles (NOT by the spawned
        // small resource collection), and share the same saved harvest IDs.
        readonly Dictionary<string, SectorResourceNode> worldTrees =
            new Dictionary<string, SectorResourceNode>(StringComparer.Ordinal);
        readonly List<string> forgottenWorldTrees = new List<string>();
        // Refresh runs every 1.1s. Reuse scratch containers instead of
        // producing a new HashSet/List on each scan.
        readonly HashSet<string> expected =
            new HashSet<string>(StringComparer.Ordinal);
        readonly List<string> unload = new List<string>();

        Material[] materials;
        float nextRefresh;

        public bool WasHarvested(string id) =>
            !string.IsNullOrEmpty(id) && harvested.Contains(id);

        public void RegisterWorldTree(SectorResourceNode node)
        {
            if (node == null || string.IsNullOrEmpty(node.Id))
                return;
            if (harvested.Contains(node.Id))
            {
                Destroy(node.gameObject);
                return;
            }
            worldTrees[node.Id] = node;
        }

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
            forgottenWorldTrees.Clear();
            foreach (var pair in worldTrees)
                if (pair.Value == null)
                    forgottenWorldTrees.Add(pair.Key);
            foreach (string id in forgottenWorldTrees)
                worldTrees.Remove(id);

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

        public static SectorResourceType ResourceForBiome(
            SectorBiome biome, uint hash)
        {
            int roll = (int)(hash % 18);
            // Biome accents keep all core survival resources available.
            if (biome == SectorBiome.RockyHighland && roll >= 10)
                return SectorResourceType.Ore;
            if (biome == SectorBiome.Meadow && roll >= 14)
                return SectorResourceType.CottonPlant;
            if (biome == SectorBiome.DrySteppe && roll >= 12 && roll < 16)
                return SectorResourceType.DryBush;

            return roll < 3 ? SectorResourceType.GroundStone :
                roll < 6 ? SectorResourceType.DryBush :
                roll < 8 ? SectorResourceType.FiberBush :
                roll < 10 ? SectorResourceType.CottonPlant :
                roll == 10 ? SectorResourceType.ScrapPile :
                roll == 11 ? SectorResourceType.TimberPile :
                roll < 15 ? SectorResourceType.Tree :
                SectorResourceType.Ore;
        }

        void SpawnNode(string id, Vector2 pos, uint hash)
        {
            // More stones and dry bushes than rare scrap: the first tools
            // should be craftable by exploring the starting 160m area.
            SectorResourceType type = ResourceForBiome(
                SectorBiomeRules.At(pos.x, pos.y), hash);

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
                Vector3 parentScale = node.transform.lossyScale;
                crown.transform.localScale = new Vector3(
                    2.4f / parentScale.x, 2.1f / parentScale.y,
                    2.4f / parentScale.z);
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

            foreach (SectorResourceNode node in worldTrees.Values)
            {
                if (node == null || harvested.Contains(node.Id)) continue;
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
            FindToolTarget(active.Values, forward, ref nearest, ref best);
            FindToolTarget(worldTrees.Values, forward, ref nearest, ref best);
            return nearest;
        }

        void FindToolTarget(
            IEnumerable<SectorResourceNode> candidates,
            Vector3 forward, ref SectorResourceNode nearest, ref float best)
        {
            Vector3 playerPosition = player.transform.position;
            foreach (SectorResourceNode node in candidates)
            {
                if (node == null || harvested.Contains(node.Id) ||
                    !RequiresTool(node.Type))
                    continue;
                Vector3 delta = node.transform.position - playerPosition;
                delta.y = 0f;
                float sqr = delta.sqrMagnitude;
                if (sqr >= best) continue;
                if (sqr > 1.4f * 1.4f &&
                    Vector3.Dot(forward, delta / Mathf.Sqrt(sqr)) < .25f)
                    continue;
                best = sqr;
                nearest = node;
            }
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
            if (node.ToolHitsRemaining > 1)
            {
                int remaining = node.Strike();
                warning = (node.Type == SectorResourceType.Tree
                    ? "Рубим дерево. " : "Добываем руду. ") +
                    "Осталось ударов: " + remaining;
                return true;
            }
            if (!HarvestToolNode(node, inventory, out label))
                warning = "Не удалось оставить добычу на земле";
            return true;
        }

        // Tree/ore harvest is not blocked when the bag cannot hold the
        // entire yield. Store extra in a persistent ground cache and collect
        // only what fits. All-or-nothing E pickup remains unchanged.
        public bool HarvestToolNode(
            SectorResourceNode node, SectorInventory inventory,
            out string label)
        {
            label = string.Empty;
            if (node == null || inventory == null ||
                !RequiresTool(node.Type) ||
                (!active.ContainsKey(node.Id) && !worldTrees.ContainsKey(node.Id)) ||
                harvested.Contains(node.Id))
                return false;

            var trial = new SectorInventory(inventory.SlotLimit, inventory.MaxWeight);
            trial.Import(inventory.Export());
            var leftovers = new List<SectorItemStack>(2);
            int primaryFit = AddAsMuchAsFits(trial, node.ItemId, node.Count);
            if (primaryFit < node.Count)
                leftovers.Add(new SectorItemStack(
                    node.ItemId, node.Count - primaryFit));
            int secondaryFit = 0;
            if (!string.IsNullOrEmpty(node.SecondaryId) && node.SecondaryCount > 0)
            {
                secondaryFit = AddAsMuchAsFits(
                    trial, node.SecondaryId, node.SecondaryCount);
                if (secondaryFit < node.SecondaryCount)
                    leftovers.Add(new SectorItemStack(
                        node.SecondaryId, node.SecondaryCount - secondaryFit));
            }

            // Commit ground cache before mutating player's inventory.
            if (leftovers.Count > 0 &&
                (StoreOverflow == null || !StoreOverflow(node, leftovers)))
                return false;

            if (primaryFit > 0)
                inventory.Add(node.ItemId, primaryFit);
            if (secondaryFit > 0)
                inventory.Add(node.SecondaryId, secondaryFit);

            label = primaryFit + secondaryFit > 0
                ? SectorItems.Get(node.ItemId).Label + " ×" + primaryFit
                : "Добыча оставлена на земле";
            if (leftovers.Count > 0)
                label += " | На земле: " +
                    SectorItems.Get(leftovers[0].id).Label +
                    " ×" + leftovers[0].count;

            harvested.Add(node.Id);
            active.Remove(node.Id);
            worldTrees.Remove(node.Id);
            // EditMode tests invoke this without a running player loop.
            // Destroy is deferred/unsupported there; play mode must keep
            // deferred destruction for physics and streaming safety.
            if (Application.isPlaying)
                Destroy(node.gameObject);
            else
                DestroyImmediate(node.gameObject);
            return true;
        }

        static int AddAsMuchAsFits(
            SectorInventory inventory, string itemId, int requested)
        {
            int count = 0;
            while (count < requested && inventory.CanAdd(itemId, 1))
            {
                if (!inventory.Add(itemId, 1))
                    break;
                count++;
            }
            return count;
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
                (!active.ContainsKey(node.Id) &&
                 !worldTrees.ContainsKey(node.Id)) ||
                harvested.Contains(node.Id))
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
            worldTrees.Remove(node.Id);
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

            // Keep unharvested tile-owned pines. Trees already chopped in
            // a newly loaded profile must disappear immediately.
            forgottenWorldTrees.Clear();
            foreach (var pair in worldTrees)
            {
                if (pair.Value == null || harvested.Contains(pair.Key))
                {
                    if (pair.Value != null)
                        Destroy(pair.Value.gameObject);
                    forgottenWorldTrees.Add(pair.Key);
                }
            }
            foreach (string id in forgottenWorldTrees)
                worldTrees.Remove(id);

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
