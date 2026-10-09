using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DeadSector
{
    public static class SectorZombieLootTable
    {
        // Prototype baseline: predictable rewards can be balanced later.
        // This function never creates scene objects or edits inventory.
        public static List<SectorItemStack> For(SectorZombieKind kind)
        {
            switch (kind)
            {
                case SectorZombieKind.Brute:
                    return new List<SectorItemStack>
                    {
                        new SectorItemStack("scrap", 2),
                        new SectorItemStack("bandage", 1)
                    };
                case SectorZombieKind.Runner:
                    return new List<SectorItemStack>
                    {
                        new SectorItemStack("9mm", 4),
                        new SectorItemStack("cloth", 1)
                    };
                default:
                    return new List<SectorItemStack>
                    {
                        new SectorItemStack("cloth", 1)
                    };
            }
        }
    }

    [Serializable]
    public sealed class SectorGameSave
    {
        public int version = 1;
        public float hourOfDay = 12f;
        public int daysSurvived;
        public Vector3 position;
        public float health;
        public float hunger;
        public float thirst;
        public float stamina;
        public string primary = "";
        public string secondary = "";
        public string melee = "";
        public int selectedSlot = 2;
        public int pistolRounds;
        public int rifleRounds;
        public List<SectorItemStack> inventory = new List<SectorItemStack>();
        public List<SectorContainerSnapshot> containers =
            new List<SectorContainerSnapshot>();
        public List<string> harvestedResourceIds = new List<string>();
        public SectorJournalSnapshot journal = new SectorJournalSnapshot();
        public SectorHordeSnapshot horde = new SectorHordeSnapshot();
        public string[] armor = new string[5];
        // Optional additive field; old V1 files load with no structures.
        public List<SectorBuildSnapshot> structures =
            new List<SectorBuildSnapshot>();
    }

    /// <summary>
    /// Single-player prototype gameplay: needs, inventory, equipment, loot,
    /// melee/gun attacks and disk persistence. Network authority is purposely
    /// not faked here; multiplayer needs a separate transport and server.
    /// </summary>
    public sealed class SectorGameplay : MonoBehaviour
    {
        public SectorPlayer player;
        public SectorWorld world;

        public SectorInventory Inventory { get; private set; } = new SectorInventory();
        public SectorSurvival Needs { get; private set; }
        public SectorEquipmentVisuals EquipmentVisuals { get; private set; }
        public SectorWorldClock WorldClock { get; private set; }
        public SectorPunchVisual PunchVisual { get; private set; }
        public SectorCombatFeedback CombatFeedback { get; private set; }
        public SectorResources Resources { get; private set; }
        public SectorJournal Journal { get; private set; }
        public SectorEquipment Armor { get; private set; }
        public SectorArmorVisuals ArmorVisuals { get; private set; }
        public SectorHordeDirector Horde { get; private set; }
        public SectorBaseBuilding Building { get; private set; }
        public SectorSoundscape Sounds { get; private set; }
        public SectorFlashlight Flashlight { get; private set; }
        public string StorageDepositItemId { get; private set; } = "";

        readonly Dictionary<string, SectorLootContainer> active =
            new Dictionary<string, SectorLootContainer>(StringComparer.Ordinal);

        readonly Dictionary<string, List<SectorItemStack>> persistent =
            new Dictionary<string, List<SectorItemStack>>(StringComparer.Ordinal);

        const string HarvestCachePrefix = "harvest_";
        readonly Dictionary<string, Vector2> harvestCacheLocations =
            new Dictionary<string, Vector2>(StringComparer.Ordinal);

        // Register the dropped remainder BEFORE finalizing a tree strike.
        // Harvest caches are saved as ordinary loot containers with positions.
        public bool StoreHarvestOverflow(
            SectorResourceNode node, List<SectorItemStack> materials)
        {
            if (node == null || string.IsNullOrEmpty(node.Id) ||
                materials == null || materials.Count == 0)
                return false;

            string id = HarvestCachePrefix + node.Id;
            var copy = new List<SectorItemStack>(materials.Count);
            foreach (SectorItemStack stack in materials)
            {
                if (stack == null || stack.count <= 0 ||
                    !SectorItems.TryGet(stack.id, out _))
                    return false;
                copy.Add(new SectorItemStack(stack.id, stack.count));
            }
            if (copy.Count == 0)
                return false;

            Vector3 position = node.transform.position;
            harvestCacheLocations[id] =
                new Vector2(position.x, position.z);
            persistent[id] = copy;
            refreshAt = 0f;
            return true;
        }

        // Corpse drops use the same persistent, streamable pickup crates as
        // logged trees and ore. Existing V1 containers remain compatible.
        public bool AddZombieLoot(SectorZombieKind kind, Vector3 position)
        {
            string id = "corpse_" + Guid.NewGuid().ToString("N");
            List<SectorItemStack> loot = SectorZombieLootTable.For(kind);
            harvestCacheLocations[id] = new Vector2(position.x, position.z);
            persistent[id] = loot;
            refreshAt = 0f;
            return true;
        }

        string[] equipment = { "", "", "" };
        int selectedSlot = 2;
        int pistolRounds;
        int rifleRounds;
        float reloadingUntil;
        bool inventoryOpen;
        bool craftingOpen;
        bool externalUiBlocking;
        int suppressedPanelHotkeyFrame = -1;
        bool loadedOnce;
        const string LastSelectedProfileKey = "DeadSector.Save.LastActiveSlotV1";
        int activeSaveSlot = SectorSaveProfiles.LegacySlot;
        public int ActiveSaveSlot => activeSaveSlot;
        public string ActiveProfileName => SectorSaveProfiles.Name(activeSaveSlot);
        Vector2 inventoryScroll;
        Vector2 craftingScroll;
        string selectedInventoryItem = "";
        float refreshAt;
        float autoSaveAt;
        float nextAttack;
        float messageUntil;
        string message = "";

        Material crateMaterial;

        static readonly Vector2[] StarterSupplies =
        {
            new Vector2(34f, 30f),
            new Vector2(10f, 39f),
            new Vector2(-28f, -32f),
            new Vector2(84f, -58f)
        };

        public void Configure(SectorPlayer target, SectorWorld streamedWorld)
        {
            player = target;
            world = streamedWorld;

            // Preserve access to the original legacy slot on first boot.
            // Later boots resume the last *valid existing* numbered slot;
            // an unreadable/empty last slot never becomes the autosave target.
            int previousSlot = PlayerPrefs.GetInt(
                LastSelectedProfileKey, SectorSaveProfiles.LegacySlot);
            if (previousSlot > SectorSaveProfiles.LegacySlot &&
                SectorSaveProfiles.TryRead(
                    Application.persistentDataPath, previousSlot,
                    out SectorGameSave _))
                activeSaveSlot = previousSlot;

            Needs = target.GetComponent<SectorSurvival>();
            if (Needs == null)
                Needs = target.gameObject.AddComponent<SectorSurvival>();

            EquipmentVisuals = target.GetComponent<SectorEquipmentVisuals>();
            if (EquipmentVisuals == null)
                EquipmentVisuals = target.gameObject.AddComponent<SectorEquipmentVisuals>();

            EquipmentVisuals.Configure(target);

            Armor = target.GetComponent<SectorEquipment>();
            if (Armor == null)
                Armor = target.gameObject.AddComponent<SectorEquipment>();

            ArmorVisuals = target.GetComponent<SectorArmorVisuals>();
            if (ArmorVisuals == null)
                ArmorVisuals = target.gameObject.AddComponent<SectorArmorVisuals>();
            ArmorVisuals.Configure(target, Armor);

            PunchVisual = target.GetComponent<SectorPunchVisual>();
            if (PunchVisual == null)
                PunchVisual = target.gameObject.AddComponent<SectorPunchVisual>();

            PunchVisual.Configure(target);

            CombatFeedback = GetComponent<SectorCombatFeedback>();
            if (CombatFeedback == null)
                CombatFeedback = gameObject.AddComponent<SectorCombatFeedback>();
            CombatFeedback.Configure(target);

            Resources = GetComponent<SectorResources>();
            if (Resources == null)
                Resources = gameObject.AddComponent<SectorResources>();
            Resources.Configure(target);
            Resources.StoreOverflow = StoreHarvestOverflow;
            // World-generated pines join the same gather/save registry.
            // Bootstrap creates gameplay before World.Start streams its tiles.
            if (world != null)
                world.resources = Resources;

            Journal = GetComponent<SectorJournal>();
            if (Journal == null)
                Journal = gameObject.AddComponent<SectorJournal>();
            Journal.Configure(target);

            Building = GetComponent<SectorBaseBuilding>();
            if (Building == null)
                Building = gameObject.AddComponent<SectorBaseBuilding>();
            Building.Configure(target, this, Inventory);

            Sounds = GetComponent<SectorSoundscape>();
            if (Sounds == null)
                Sounds = gameObject.AddComponent<SectorSoundscape>();
            Sounds.Configure(target);

            Flashlight = target.GetComponent<SectorFlashlight>();
            if (Flashlight == null)
                Flashlight = target.gameObject.AddComponent<SectorFlashlight>();
            Flashlight.Configure(target);

            WorldClock = FindFirstObjectByType<SectorWorldClock>();

            // Primitive-survival start: fists only, no free weapon.
            // Gather stones and dry branches before the first crafted knife.
            Inventory.Add("water", 1);
            Inventory.Add("bandage", 2);
            // Start unarmed so LMB/RMB punches are essential.
            // Stone knife and wooden weapons must be gathered and crafted.
            equipment[2] = "";
            selectedSlot = 2;
            EquipmentVisuals.UpdateLoadout(
                equipment[0], equipment[1], equipment[2], selectedSlot);
            autoSaveAt = Time.time + 90f;
        }

        void Update()
        {
            if (player == null || world == null || !player.Ready)
                return;

            if (!loadedOnce)
            {
                loadedOnce = true;
                LoadGame(true);
            }

            if (!externalUiBlocking && SectorInput.Pressed(KeyCode.F5))
                SaveGame();

            if (!externalUiBlocking && SectorInput.Pressed(KeyCode.F9))
                LoadGame(false);

            // A Canvas tab may consume the same key in its own Update.
            // Avoid double-toggle when script execution order differs.
            if (!externalUiBlocking &&
                suppressedPanelHotkeyFrame != Time.frameCount)
            {
                if (SectorInput.Pressed(KeyCode.I))
                    ToggleInventory();
                if (SectorInput.Pressed(KeyCode.C))
                    ToggleCrafting();
            }

            if (!externalUiBlocking &&
                (Building == null || !Building.BuildMode))
            {
                if (SectorInput.Pressed(KeyCode.Alpha1)) selectedSlot = 0;
                if (SectorInput.Pressed(KeyCode.Alpha2)) selectedSlot = 1;
                if (SectorInput.Pressed(KeyCode.Alpha3)) selectedSlot = 2;
            }

            if (Time.time >= refreshAt)
            {
                refreshAt = Time.time + .75f;
                RefreshContainers();
            }

            if (!externalUiBlocking && !inventoryOpen &&
                !craftingOpen &&
                (Building == null || !Building.BuildMode) &&
                player.Health > 0f &&
                Cursor.lockState == CursorLockMode.Locked)
            {
                if (SectorInput.Pressed(KeyCode.E))
                    Interact();

                if (SectorInput.Pressed(KeyCode.R))
                    ReloadEquipped();

                string selected = EquippedId();
                bool gunEquipped = SectorItems.TryGet(
                    selected, out SectorItemDefinition selectedItem) &&
                    selectedItem.Kind == SectorItemKind.Firearm;

                if (SectorInput.Click)
                    Attack(false);
                else if (SectorInput.RightClick && !gunEquipped)
                    Attack(true);
            }

            if (Time.time >= autoSaveAt)
            {
                autoSaveAt = Time.time + 90f;
                if (player.Health > 0f)
                    SaveGame(true);
            }

            Armor?.Refresh(Inventory);
            ArmorVisuals?.Refresh();
            EquipmentVisuals?.SetBackpackVisible(
                Armor != null &&
                Armor.Equipped(SectorEquipment.GearSlot.Backpack) != "");
            EquipmentVisuals?.UpdateLoadout(
                equipment[0], equipment[1], equipment[2], selectedSlot);
        }

        public void AttachHorde(SectorHordeDirector director)
        {
            Horde = director;
        }

        // Public UI-facing API. The Canvas and the legacy IMGUI can use
        // exactly the same inventory and save rules; no duplicated state.
        public bool ModernUiEnabled { get; set; }
        public bool ExternalUiBlocking => externalUiBlocking;
        public bool HasLoadedInitialSave => loadedOnce;

        public void SetExternalUiBlocking(bool blocked)
        {
            externalUiBlocking = blocked;
            if (blocked)
            {
                inventoryOpen = false;
                craftingOpen = false;
            }
            UpdatePanelInput();
        }

        public bool InventoryOpen => inventoryOpen;
        public bool CraftingOpen => craftingOpen;
        public int ActiveWeaponSlot => selectedSlot;
        public bool IsReloading => Time.time < reloadingUntil;

        public int RoundsForSlot(int slot)
        {
            string id = WeaponInSlot(slot);
            return id == "rifle" ? rifleRounds :
                id == "pistol" ? pistolRounds : 0;
        }

        public int MagazineCapacityForSlot(int slot) =>
            SectorMagazineRules.Capacity(WeaponInSlot(slot));
        public string RecentMessage => Time.time < messageUntil ? message : "";

        public string WeaponInSlot(int slot)
        {
            if (slot < 0 || slot >= equipment.Length)
                return "";
            string id = equipment[slot];
            return Inventory.Count(id) > 0 ? id : "";
        }

        public void SelectWeaponSlot(int slot)
        {
            if (slot >= 0 && slot < equipment.Length)
                selectedSlot = slot;
        }

        public void ShowInventory()
        {
            if (!inventoryOpen)
                ToggleInventory();
        }

        public void ShowCrafting()
        {
            if (!craftingOpen)
                ToggleCrafting();
        }

        public void CloseInventoryPanels()
        {
            inventoryOpen = false;
            craftingOpen = false;
            UpdatePanelInput();
        }

        public void SuppressPanelHotkeysThisFrame()
        {
            suppressedPanelHotkeyFrame = Time.frameCount;
        }

        public void SetJournalOpen(bool isOpen)
        {
            if (Journal == null)
                return;

            Journal.SetVisible(isOpen);
            SetExternalUiBlocking(isOpen);
        }

        public bool SplitInventoryStack(int stackIndex, int count)
        {
            bool result = Inventory.SplitStack(stackIndex, count);
            if (result)
                Notify("Стопка разделена");
            return result;
        }

        public void SortInventory()
        {
            Inventory.SortStacks();
            Notify("Рюкзак отсортирован");
        }

        public void NotifyConstruction(string description)
        {
            Sounds?.PlayBuild();
            Notify("Построено: " + description);
        }

        public void SelectStorageDepositItem(string id)
        {
            if (Inventory.Count(id) > 0)
                StorageDepositItemId = id;
        }

        public void UseInventoryItem(string id) => UseItem(id);
        public void EquipInventoryItem(string id) => EquipItem(id);

        public void RemoveGear(SectorEquipment.GearSlot slot)
        {
            Armor?.Unequip(slot, Inventory);
            Notify("Снаряжение снято");
        }

        public bool CraftRecipe(string id)
        {
            if (!SectorCrafting.Craft(Inventory, id))
            {
                Notify("Не хватает материалов или места");
                return false;
            }

            Journal?.RecordCraft();
            SectorRecipe recipe = SectorCrafting.Find(id);
            Notify("Создано: " + (recipe != null
                ? SectorItems.Get(recipe.OutputId).Label : id));
            return true;
        }

        // Access the same nearest-interaction priority as E and the
        // former IMGUI prompt. The modern HUD only reads this information.
        public string InteractionHint()
        {
            if (player == null || !player.Ready ||
                inventoryOpen || craftingOpen)
                return "";

            SectorLootContainer container = NearbyContainer();
            SectorDoor door = NearbyDoor();
            SectorResourceNode resource =
                Resources != null ? Resources.Nearby() : null;
            SectorBuildPiece storage =
                Building != null ? Building.NearbyStorage() : null;
            if (storage != null)
                return SectorInput.Sprint
                    ? "[SHIFT+E]  ПОЛОЖИТЬ В ХРАНИЛИЩЕ"
                    : "[E]  ХРАНИЛИЩЕ  (" + storage.StorageItemCount + ")";
            if (Building != null && Building.BuildMode)
                return Building.ControlHint;
            Vector3 position = player.transform.position;

            float resourceDistance = resource != null
                ? Vector3.Distance(resource.transform.position, position)
                : float.PositiveInfinity;
            float doorDistance = door != null
                ? Vector3.Distance(door.transform.position, position)
                : float.PositiveInfinity;
            float containerDistance = container != null
                ? Vector3.Distance(container.transform.position, position)
                : float.PositiveInfinity;

            if (resourceDistance < doorDistance &&
                resourceDistance < containerDistance)
            {
                if (SectorResources.RequiresTool(resource.Type))
                    return "[ЛКМ]  " +
                        (resource.Type == SectorResourceType.Tree
                            ? "РУБИТЬ ДЕРЕВО" : "ДОБЫВАТЬ РУДУ") +
                        "   /   НУЖЕН " +
                        SectorResources.RequiredToolName(resource.Type);
                return "[E]  СОБРАТЬ  " + SectorItems.Get(resource.ItemId).Label;
            }

            if (doorDistance < containerDistance)
                return door.IsOpen ? "[E]  ЗАКРЫТЬ ДВЕРЬ" : "[E]  ОТКРЫТЬ ДВЕРЬ";

            if (container != null)
                return "[E]  " + container.title +
                    (container.Empty ? "  (ПУСТО)" : "  ВЗЯТЬ");

            return "";
        }


        void ToggleInventory()
        {
            bool next = !inventoryOpen;
            inventoryOpen = next;
            craftingOpen = false;
            UpdatePanelInput();
        }

        void ToggleCrafting()
        {
            bool next = !craftingOpen;
            craftingOpen = next;
            inventoryOpen = false;
            UpdatePanelInput();
        }

        void UpdatePanelInput()
        {
            bool modalOpen = inventoryOpen || craftingOpen ||
                externalUiBlocking;
            player.InputBlockedByUI = modalOpen;
            Cursor.lockState = modalOpen
                ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = modalOpen;
        }

        void RefreshContainers()
        {
            Vector3 origin = player.transform.position;
            var expected = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < StarterSupplies.Length; i++)
            {
                Vector2 pos = StarterSupplies[i];
                EnsureNearby(
                    "starter_" + i, "Ящик с припасами",
                    pos, origin, expected);
            }

            foreach (SectorMapPlan.Location location in SectorMapPlan.Locations)
            {
                if (location.Kind == SectorMapPlan.LocationKind.Wilderness &&
                    location.Id != "forest_camp")
                    continue;

                EnsureNearby(
                    "poi_" + location.Id,
                    location.Name + " Припасы",
                    location.MapPosition + new Vector2(18f, 14f),
                    origin, expected);
            }

            // Interior caches use stable scene-generated positions and IDs
            // so clearing a cabinet stays cleared after F5/F9.
            if (world != null)
                foreach (SectorInteriorCache cache in world.InteriorCaches)
                    EnsureNearby(cache.Id, cache.Label,
                        cache.Position, origin, expected);

            // Pick up logs/ore left after chopping with a full backpack.
            foreach (var pair in harvestCacheLocations)
            {
                if (!persistent.TryGetValue(
                        pair.Key, out List<SectorItemStack> contents) ||
                    contents == null || contents.Count == 0)
                    continue;
                EnsureNearby(
                    pair.Key, pair.Key.StartsWith("corpse_", StringComparison.Ordinal)
                        ? "Трофеи заражённого" : "Добытые материалы", pair.Value,
                    origin, expected);
            }

            var remove = new List<string>();

            foreach (var pair in active)
                if (!expected.Contains(pair.Key))
                    remove.Add(pair.Key);

            foreach (string id in remove)
            {
                SectorLootContainer container = active[id];
                persistent[id] = container.Export();
                Destroy(container.gameObject);
                active.Remove(id);
            }
        }

        void EnsureNearby(
            string id, string label, Vector2 pos,
            Vector3 playerPosition, HashSet<string> expected)
        {
            float dx = pos.x - playerPosition.x;
            float dz = pos.y - playerPosition.z;

            if (dx * dx + dz * dz > 210f * 210f)
                return;

            expected.Add(id);

            if (active.ContainsKey(id))
                return;

            float y = SectorLayout.Height(pos.x, pos.y);

            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "Loot_" + id;
            bool small = id.StartsWith(
                "interior_", StringComparison.Ordinal) ||
                id.StartsWith("corpse_", StringComparison.Ordinal);
            crate.transform.position = new Vector3(
                pos.x, y + (small ? .73f : .7f), pos.y);
            crate.transform.localScale = small
                ? new Vector3(.60f, .60f, .60f)
                : new Vector3(1.3f, 1.4f, 1.0f);

            if (crateMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    shader = Shader.Find("Standard");

                crateMaterial = new Material(shader);
                crateMaterial.color = new Color(.34f, .24f, .13f);
            }

            crate.GetComponent<Renderer>().sharedMaterial = crateMaterial;

            SectorLootContainer container =
                crate.AddComponent<SectorLootContainer>();

            persistent.TryGetValue(id, out List<SectorItemStack> items);
            container.Initialize(id, label, items);

            active.Add(id, container);
        }

        SectorLootContainer NearbyContainer()
        {
            SectorLootContainer closest = null;
            float distanceSquared = 3.6f * 3.6f;

            foreach (SectorLootContainer container in active.Values)
            {
                Vector3 delta = container.transform.position -
                    player.transform.position;

                float d = delta.sqrMagnitude;
                if (d > distanceSquared)
                    continue;

                closest = container;
                distanceSquared = d;
            }

            return closest;
        }

        SectorDoor NearbyDoor()
        {
            Vector3 center = player.transform.position + Vector3.up;
            Collider[] hits = Physics.OverlapSphere(
                center, 3f, ~(1 << 2),
                QueryTriggerInteraction.Ignore);

            SectorDoor closest = null;
            float best = 3f * 3f;

            foreach (Collider hit in hits)
            {
                SectorDoor door = hit.GetComponentInParent<SectorDoor>();
                if (door == null) continue;

                float distance = (door.transform.position - player.transform.position)
                    .sqrMagnitude;

                if (distance >= best) continue;
                closest = door;
                best = distance;
            }

            return closest;
        }

        void Interact()
        {
            SectorBuildPiece storage =
                Building != null ? Building.NearbyStorage() : null;
            if (storage != null)
            {
                if (SectorInput.Sprint)
                {
                    if (storage.TryDeposit(Inventory, StorageDepositItemId))
                        Notify("Сложено: " +
                            SectorItems.Get(StorageDepositItemId).Label);
                    else
                        Notify("Выберите предмет в I или хранилище заполнено");
                }
                else if (storage.TryWithdraw(Inventory, out string taken))
                    Notify("Из хранилища: " + taken);
                else
                    Notify("Хранилище пусто или рюкзак заполнен");
                return;
            }

            SectorLootContainer container = NearbyContainer();
            SectorDoor door = NearbyDoor();
            SectorResourceNode resource =
                Resources != null ? Resources.Nearby() : null;

            if (resource != null &&
                (door == null || Vector3.Distance(
                    resource.transform.position, player.transform.position) <
                    Vector3.Distance(door.transform.position, player.transform.position)) &&
                (container == null || Vector3.Distance(
                    resource.transform.position, player.transform.position) <
                    Vector3.Distance(container.transform.position, player.transform.position)))
            {
                if (SectorResources.RequiresTool(resource.Type))
                {
                    Notify("Используйте " +
                        SectorResources.RequiredToolName(resource.Type) +
                        " и ЛКМ для добычи");
                    return;
                }

                if (Resources.Harvest(resource, Inventory, out string found))
                {
                    Journal?.RecordHarvest();
                    Notify("Добыто: " + found);
                }
                else
                    Notify("В рюкзаке не хватает места");
                return;
            }

            if (door != null && (container == null ||
                Vector3.Distance(door.transform.position, player.transform.position) <
                Vector3.Distance(container.transform.position, player.transform.position)))
            {
                door.Toggle();
                Notify(door.IsOpen ? "Дверь открыта" : "Дверь закрыта");
                return;
            }

            if (container == null)
            {
                Notify("Рядом нет ящика с припасами");
                return;
            }

            if (container.TakeFirst(Inventory, out string description))
            {
                persistent[container.containerId] = container.Export();
                Journal?.RecordPickup();
                Notify("Подобрано: " + description);
            }
            else
            {
                Notify(container.Empty
                    ? "Ящик пуст"
                    : "Не хватает места или превышен вес");
            }
        }

        string EquippedId()
        {
            string id = equipment[Mathf.Clamp(selectedSlot, 0, 2)];
            return Inventory.Count(id) > 0 ? id : string.Empty;
        }

        void Attack(bool strong)
        {
            // No charging or wind-up. One mouse press resolves a hit in this
            // frame; the short arm animation is independent visual feedback.
            if (Time.time < nextAttack || IsReloading)
                return;

            string id = EquippedId();
            // Tool swings on nearby standing trees/ore override punching.
            // Three axe hits chop a tree; four pickaxe hits break an ore node.
            if (Resources != null && Resources.StrikeNearest(
                id, Inventory, player.transform.forward,
                out string resourcesObtained, out string harvestInfo))
            {
                nextAttack = Time.time + .42f;
                // A valid swing should animate even on the final hit,
                // when the harvested node has already been removed.
                if (!harvestInfo.StartsWith(
                        "Нужен инструмент:", StringComparison.Ordinal))
                {
                    PunchVisual?.Play(false, true);
                    Sounds?.PlayChop();
                }

                if (!string.IsNullOrEmpty(resourcesObtained))
                {
                    Journal?.RecordHarvest();
                    Notify("Добыто: " + resourcesObtained);
                }
                else if (!string.IsNullOrEmpty(harvestInfo))
                    Notify(harvestInfo);
                return;
            }

            SectorAttackProfile attack = SectorCombatRules.ForAttack(id, strong);

            if (attack.Kind == SectorAttackKind.Firearm)
            {
                if (id == "rifle")
                {
                    if (!SectorMagazineRules.Fire(ref rifleRounds))
                    {
                        Notify("Магазин пуст — нажмите R");
                        return;
                    }
                }
                else if (!SectorMagazineRules.Fire(ref pistolRounds))
                {
                    Notify("Магазин пуст — нажмите R");
                    return;
                }
            }

            // Consume stamina but never block a punch while exhausted.
            float staminaFactor = Needs != null
                ? Needs.SpendAttackStamina(attack.StaminaCost)
                : 1f;

            float damage = SectorCombatRules.FinalDamage(attack, staminaFactor);
            nextAttack = Time.time + attack.Cooldown;

            bool firearm = attack.Kind == SectorAttackKind.Firearm;

            if (firearm)
            {
                EquipmentVisuals?.PlayMuzzleFlash();
                player.ApplyCombatRecoil(id == "rifle" ? 1.0f : .75f);
            }
            else
            {
                PunchVisual?.Play(strong, !string.IsNullOrEmpty(id));
                player.ApplyCombatRecoil(strong ? .32f : .12f);
            }

            Vector3 origin = firearm && player.view != null
                ? player.view.transform.position
                : player.transform.position + Vector3.up * 1.25f;

            Vector3 forward = firearm && player.view != null
                ? player.view.transform.forward
                : player.transform.forward;

            SectorZombie target = null;
            float nearestDistance = attack.Range;

            foreach (SectorZombie zombie in
                FindObjectsByType<SectorZombie>(FindObjectsSortMode.None))
            {
                if (zombie == null || zombie.Dead)
                    continue;

                if (firearm)
                    zombie.HearNoise(
                        player.transform.position,
                        id == "rifle" ? 145f : 95f);

                Vector3 delta = zombie.transform.position +
                    Vector3.up * 1.1f - origin;

                float distance = delta.magnitude;
                if (distance > nearestDistance || distance < .05f)
                    continue;

                Vector3 towardsZombie = delta / distance;
                if (Vector3.Dot(forward, towardsZombie) <
                    attack.FacingThreshold)
                    continue;

                // Target's own collider is an expected impact, not a
                // blocker. Buildings, ground and other zombies still stop it.
                if (SectorCombatVisibility.IsObstructed(
                    origin, zombie.transform.position + Vector3.up * 1.1f,
                    zombie))
                    continue;

                nearestDistance = distance;
                target = zombie;
            }

            if (target != null)
            {
                // Apply impulse from the actual attack direction.
                // Ragdoll activation and damage are both synchronous with
                // the user's click (no charge-up delay).
                Vector3 hitDirection = firearm
                    ? forward
                    : (target.transform.position -
                        player.transform.position).normalized;

                Vector3 impactPoint = target.transform.position +
                    Vector3.up * 1.15f;

                target.TakeDamage(damage, hitDirection, impactPoint);

                if (target.Dead)
                    Journal?.RecordKill();
                Notify((strong ? "Strong" : "Quick") +
                    " hit: " + Mathf.RoundToInt(damage));
            }

            CombatFeedback?.ShowAttack(
                target != null, strong, firearm,
                target != null ? damage : 0f);
        }

        public bool ReloadEquipped()
        {
            if (player == null || player.Health <= 0f || IsReloading)
                return false;

            string gun = EquippedId();
            int loaded;
            if (gun == "rifle")
                loaded = SectorMagazineRules.Reload(
                    gun, Inventory, ref rifleRounds);
            else if (gun == "pistol")
                loaded = SectorMagazineRules.Reload(
                    gun, Inventory, ref pistolRounds);
            else
                return false;

            if (loaded <= 0)
            {
                Notify("Нет патронов или магазин уже полон");
                return false;
            }

            // Reserve ammunition transfers immediately; firing resumes
            // only after the animation-ready reload window completes.
            reloadingUntil = Time.time +
                (gun == "rifle" ? 1.8f : 1.35f);
            Notify("Перезарядка: " + gun + " (+" + loaded + ")");
            return true;
        }

        void UseItem(string id)
        {
            if (!SectorItems.TryGet(id, out SectorItemDefinition def) ||
                !def.IsConsumable || Inventory.Count(id) == 0)
                return;

            if (def.HealthRestore > 0f && player.Health >= 100f &&
                def.HungerRestore <= 0f && def.ThirstRestore <= 0f)
            {
                Notify("Здоровье уже полное");
                return;
            }

            if (!Inventory.Remove(id, 1))
                return;

            Needs?.Restore(def.HungerRestore, def.ThirstRestore);
            player.Heal(def.HealthRestore);
            Notify("Использовано: " + def.Label);
        }

        void EquipItem(string id)
        {
            if (Inventory.Count(id) == 0)
                return;

            SectorItemDefinition item = SectorItems.Get(id);
            int slot;

            if (item.Kind == SectorItemKind.Armor)
            {
                if (Armor != null && Armor.Equip(id, Inventory))
                    Notify("Экипировано: " + item.Label);
                return;
            }

            switch (item.Kind)
            {
                case SectorItemKind.Melee: slot = 2; break;
                case SectorItemKind.Firearm:
                    slot = id == "rifle" ? 0 : 1;
                    break;
                default: return;
            }

            equipment[slot] = id;
            selectedSlot = slot;
            Notify("Экипировано: " + item.Label);
        }

        public void NotifyBuildFailure(string reason) => Notify(reason);

        void Notify(string messageText)
        {
            message = messageText;
            messageUntil = Time.time + 3f;
        }

        string SavePath => SectorSaveProfiles.FilePath(
            Application.persistentDataPath, activeSaveSlot);

        public string ProfileStatus(int slot) =>
            SectorSaveProfiles.Description(
                Application.persistentDataPath, slot);

        /// <summary>
        /// Only EMPTY numbered slots can be created. The original legacy
        /// save is never overwritten, renamed or migrated in this action.
        /// New world state comes from a deterministic starter snapshot,
        /// never from the current in-memory character.
        /// </summary>
        public bool CreateNewProfile(int slot)
        {
            if (slot <= 0 || slot > SectorSaveProfiles.MaxProfileSlot)
                return false;

            if (!SectorSaveProfiles.TryCreateNew(
                Application.persistentDataPath, slot,
                SectorSaveProfiles.Starter()))
                return false;

            return LoadProfile(slot, true);
        }

        /// <summary>
        /// Resolve and validate before changing the active save path.
        /// On failure, the current profile and file remain selected.
        /// </summary>
        public bool LoadProfile(int slot, bool freshlyCreated = false)
        {
            if (!SectorSaveProfiles.IsValidSlot(slot) ||
                !SectorSaveProfiles.TryRead(
                    Application.persistentDataPath, slot,
                    out SectorGameSave saved))
            {
                Notify("Не удалось открыть профиль " + slot);
                return false;
            }

            if (!ApplySave(saved))
            {
                Notify("Ошибка загрузки профиля — текущий слот сохранён");
                return false;
            }

            activeSaveSlot = slot;
            loadedOnce = true;
            PlayerPrefs.SetInt(LastSelectedProfileKey, slot);

            // Reset live NPC actors when crossing independent timelines.
            // An already dead NPC or horde from the previous profile is
            // not part of the newly chosen save.
            SectorNavigation navigation =
                FindFirstObjectByType<SectorNavigation>();
            if (navigation != null)
                navigation.ResetPopulationForProfile();

            // A new character begins on the actual loaded starting tile,
            // rather than hovering twelve metres above the terrain.
            if (freshlyCreated)
                player.ActivateAtSpawn();

            autoSaveAt = Time.time + 90f;
            Notify("Загружено: " + ActiveProfileName +
                " | построек: " +
                (Building != null ? Building.PieceCount : 0));
            return true;
        }

        public void SaveGame(bool silent = false)
        {
            TrySaveGame(silent);
        }

        public bool TrySaveGame(bool silent = false)
        {
            // Never overwrite a living checkpoint with a dead player.
            if (player == null || !player.Ready || player.Health <= 0f)
                return false;

            // Never replace corrupted, unreadable or unsupported files.
            // Such files must remain available for manual recovery.
            if (SectorSaveProfiles.Exists(
                    Application.persistentDataPath, activeSaveSlot) &&
                !SectorSaveProfiles.TryRead(
                    Application.persistentDataPath, activeSaveSlot,
                    out SectorGameSave _))
            {
                Notify("Повреждённое сохранение сохранено; выберите пустой слот");
                return false;
            }

            var data = new SectorGameSave
            {
                hourOfDay = WorldClock != null ? WorldClock.hourOfDay : 12f,
                daysSurvived = WorldClock != null ? WorldClock.DaysSurvived : 0,
                horde = Horde != null ? Horde.Export() : new SectorHordeSnapshot(),
                armor = Armor != null ? Armor.Export() : new string[5],
                position = player.transform.position,
                health = player.Health,
                hunger = Needs != null ? Needs.hunger : 100f,
                thirst = Needs != null ? Needs.thirst : 100f,
                stamina = Needs != null ? Needs.stamina : 100f,
                primary = equipment[0],
                secondary = equipment[1],
                melee = equipment[2],
                selectedSlot = selectedSlot,
                pistolRounds = pistolRounds,
                rifleRounds = rifleRounds,
                inventory = Inventory.Export(),
                structures = Building != null
                    ? Building.Export() : new List<SectorBuildSnapshot>(),
                harvestedResourceIds = Resources != null
                    ? Resources.Export() : new List<string>(),
                journal = Journal != null
                    ? Journal.Export() : new SectorJournalSnapshot()
            };

            foreach (var pair in active)
                persistent[pair.Key] = pair.Value.Export();

            foreach (var pair in persistent)
            {
                data.containers.Add(new SectorContainerSnapshot
                {
                    id = pair.Key,
                    items = pair.Value,
                    position = harvestCacheLocations.TryGetValue(
                        pair.Key, out Vector2 storedPosition)
                        ? new Vector3(
                            storedPosition.x, 0f, storedPosition.y)
                        : Vector3.zero
                });
            }

            try
            {
                string temporary = SavePath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(data, true));

                if (File.Exists(SavePath))
                {
                    // Replace is atomic on the local file system: an
                    // interrupted save retains either old or new data.
                    // The previous save becomes .bak automatically.
                    File.Replace(temporary, SavePath, SavePath + ".bak");
                }
                else
                {
                    File.Move(temporary, SavePath);
                }

                if (!silent)
                    Notify("Игра сохранена: " +
                        data.structures.Count + " построек");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[Dead Sector] Save failed: " + ex.Message);
                Notify("Ошибка сохранения — см. консоль");
                return false;
            }
        }

        public bool HasLiveCheckpoint
        {
            get
            {
                return SectorSaveProfiles.TryRead(
                    Application.persistentDataPath, activeSaveSlot,
                    out SectorGameSave state) &&
                    state.health > 0f;
            }
        }

        public bool RestoreCheckpointAfterDeath()
        {
            if (!SectorSaveProfiles.TryRead(
                Application.persistentDataPath, activeSaveSlot,
                out SectorGameSave state) || state.health <= 0f ||
                !ApplySave(state))
                return false;

            SectorNavigation ai = FindFirstObjectByType<SectorNavigation>();
            if (ai != null)
                ai.ResetPopulationForProfile();
            autoSaveAt = Time.time + 90f;
            return player.Health > 0f;
        }

        public void RespawnAtCamp()
        {
            if (player == null || !player.Ready)
                return;

            player.Respawn();
            Needs?.ApplySaved(65f, 65f, 100f);
            SectorNavigation ai = FindFirstObjectByType<SectorNavigation>();
            if (ai != null)
                ai.ResetPopulationForProfile();
            autoSaveAt = Time.time + 90f;
            Notify("Возрождение в лагере (рюкзак сохранён)");
        }

        public void LoadGame(bool silent)
        {
            if (!SectorSaveProfiles.TryRead(
                Application.persistentDataPath, activeSaveSlot,
                out SectorGameSave saved))
            {
                if (!silent)
                    Notify("Нет рабочего сохранения в " + ActiveProfileName);
                return;
            }

            if (ApplySave(saved) && !silent)
                Notify("Сохранение загружено: " +
                    (Building != null ? Building.PieceCount : 0) +
                    " построек");
        }

        bool ApplySave(SectorGameSave data)
        {
            if (data == null || data.version != 1 ||
                player == null || !player.Ready)
                return false;

            try
            {
                // Install capacity before import so equipped backpacks
                // can carry their full 30 slots from previous saves.
                bool savedBag = data.armor != null &&
                    data.armor.Length >= 5 &&
                    data.armor[4] == "cotton_bag";

                Inventory.ResetForLoad(savedBag
                    ? 30 : SectorInventory.DefaultSlots);
                Inventory.Import(data.inventory);
                equipment[0] = ValidEquipped(data.primary);
                equipment[1] = ValidEquipped(data.secondary);
                equipment[2] = ValidEquipped(data.melee);
                selectedSlot = Mathf.Clamp(data.selectedSlot, 0, 2);
                pistolRounds = equipment[1] == "pistol"
                    ? SectorMagazineRules.ClampLoaded(
                        "pistol", data.pistolRounds) : 0;
                rifleRounds = equipment[0] == "rifle"
                    ? SectorMagazineRules.ClampLoaded(
                        "rifle", data.rifleRounds) : 0;
                reloadingUntil = 0f;

                Needs?.ApplySaved(data.hunger, data.thirst, data.stamina);
                player.RestoreHealth(data.health);
                WorldClock?.RestoreTime(data.hourOfDay, data.daysSurvived);
                Armor?.Import(data.armor, Inventory);
                Horde?.Import(data.horde);
                Resources?.Import(data.harvestedResourceIds);
                Journal?.Import(data.journal);
                Building?.Import(data.structures);
                StorageDepositItemId = "";

                foreach (SectorLootContainer container in active.Values)
                    if (container != null)
                        Destroy(container.gameObject);
                active.Clear();
                persistent.Clear();
                harvestCacheLocations.Clear();

                if (data.containers != null)
                {
                    foreach (SectorContainerSnapshot state in data.containers)
                    {
                        if (state == null || string.IsNullOrEmpty(state.id))
                            continue;
                        persistent[state.id] = state.items ??
                            new List<SectorItemStack>();
                        if (state.id.StartsWith(
                                HarvestCachePrefix, StringComparison.Ordinal) ||
                            state.id.StartsWith(
                                "corpse_", StringComparison.Ordinal))
                            harvestCacheLocations[state.id] = new Vector2(
                                state.position.x, state.position.z);
                    }
                }

                Vector3 p = data.position;
                p.x = Mathf.Clamp(p.x, -3990f, 3990f);
                p.z = Mathf.Clamp(p.z, -3990f, 3990f);

                // Streaming will fetch this profile's terrain tile.
                p.y = SectorLayout.Height(p.x, p.z) + 12f;
                player.TeleportTo(p);
                refreshAt = 0f;
                nextAttack = 0f;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[Dead Sector] Load failed: " + ex.Message);
                return false;
            }
        }

        string ValidEquipped(string id)
        {
            if (!SectorItems.TryGet(id, out SectorItemDefinition item) ||
                Inventory.Count(id) == 0)
                return string.Empty;

            return item.Kind == SectorItemKind.Firearm ||
                   item.Kind == SectorItemKind.Melee
                ? id : string.Empty;
        }

        void OnGUI()
        {
            if (ModernUiEnabled)
                return;

            if (player == null || !player.Ready)
                return;

            GUI.depth = -110;

            DrawHotbar();

            if (Time.time < messageUntil)
            {
                GUI.Box(new Rect(Screen.width * .5f - 160f, Screen.height - 123f,
                    320f, 35f), message);
            }

            if (!inventoryOpen && !craftingOpen &&
                Cursor.lockState == CursorLockMode.Locked)
            {
                SectorLootContainer nearby = NearbyContainer();
                SectorDoor door = NearbyDoor();
                SectorResourceNode resource =
                    Resources != null ? Resources.Nearby() : null;

                if (resource != null &&
                    (door == null || Vector3.Distance(
                        resource.transform.position, player.transform.position) <
                        Vector3.Distance(door.transform.position, player.transform.position)) &&
                    (nearby == null || Vector3.Distance(
                        resource.transform.position, player.transform.position) <
                        Vector3.Distance(nearby.transform.position, player.transform.position)))
                {
                    GUI.Box(new Rect(Screen.width * .5f - 142f,
                        Screen.height * .61f, 284f, 50f),
                        "[E] ДОБЫТЬ " + SectorItems.Get(resource.ItemId).Label);
                }
                else if (door != null && (nearby == null ||
                    Vector3.Distance(door.transform.position, player.transform.position) <
                    Vector3.Distance(nearby.transform.position, player.transform.position)))
                {
                    GUI.Box(new Rect(Screen.width * .5f - 142f,
                        Screen.height * .61f, 284f, 50f),
                        "[E] " + (door.IsOpen ? "ЗАКРЫТЬ ДВЕРЬ" : "ОТКРЫТЬ ДВЕРЬ"));
                }
                else if (nearby != null)
                {
                    GUI.Box(new Rect(Screen.width * .5f - 142f,
                        Screen.height * .61f, 284f, 50f),
                        nearby.title + "  [E] ВЗЯТЬ" +
                        (nearby.Empty ? "  (ПУСТО)" : ""));
                }
            }

            if (inventoryOpen)
            {
                float width = Mathf.Min(850f, Screen.width - 18f);
                float height = Mathf.Min(620f, Screen.height - 22f);

                GUI.Window(
                    1736,
                    new Rect(
                        (Screen.width - width) * .5f,
                        (Screen.height - height) * .5f,
                        width, height),
                    DrawInventoryWindow, "DEAD SECTOR  /  РЮКЗАК [I]");
            }

            if (craftingOpen)
            {
                float width = Mathf.Min(685f, Screen.width - 18f);
                float height = Mathf.Min(550f, Screen.height - 22f);

                GUI.Window(
                    1737,
                    new Rect(
                        (Screen.width - width) * .5f,
                        (Screen.height - height) * .5f,
                        width, height),
                    DrawCraftingWindow, "DEAD SECTOR  /  КРАФТ [C]");
            }
        }

        void DrawHotbar()
        {
            float slotWidth = Mathf.Clamp(
                (Screen.width - 24f) / 3f, 95f, 135f);
            float total = slotWidth * 3f + 12f;
            float left = (Screen.width - total) * .5f;
            float top = Screen.height - 73f;

            for (int i = 0; i < 3; i++)
            {
                bool activeSlot = selectedSlot == i;
                float x = left + i * (slotWidth + 6f);
                Rect rect = new Rect(x, top, slotWidth, 58f);

                Color tint = activeSlot
                    ? new Color(.10f, .24f, .22f, .90f)
                    : new Color(.035f, .045f, .05f, .82f);

                Color previous = GUI.color;
                GUI.color = tint;
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = previous;

                if (activeSlot)
                {
                    GUI.color = new Color(.42f, .86f, .64f, 1f);
                    GUI.DrawTexture(new Rect(x, top, slotWidth, 2f),
                        Texture2D.whiteTexture);
                    GUI.color = previous;
                }

                string id = equipment[i];
                string label = SectorItems.TryGet(id, out SectorItemDefinition item) &&
                    Inventory.Count(id) > 0
                    ? item.Label
                    : i == 2 ? "КУЛАКИ" : "ПУСТО";

                GUI.Label(new Rect(x + 7f, top + 6f, slotWidth - 12f, 20f),
                    (i + 1) + "  /  " + (i == 0 ? "ОСНОВНОЕ" :
                    i == 1 ? "ЗАПАСНОЕ" : "БЛИЖНИЙ БОЙ"));

                GUI.Label(new Rect(x + 7f, top + 29f, slotWidth - 12f, 21f), label);
            }

            string active = EquippedId();
            if (active == "rifle" || active == "pistol")
            {
                string ammo = active == "rifle" ? "556" : "9mm";
                GUI.Box(new Rect(left + total + 7f, top + 6f, 100f, 46f),
                    ammo + "  " + Inventory.Count(ammo));
            }
        }

        void DrawCraftingWindow(int windowId)
        {
            float w = Mathf.Min(685f, Screen.width - 18f);
            float h = Mathf.Min(550f, Screen.height - 22f);
            GUI.Label(new Rect(14f, 29f, w - 30f, 25f),
                "КАМЕНЬ → ДЕРЕВЯННОЕ СНАРЯЖЕНИЕ → ТКАНЬ");

            float viewWidth = w - 42f;
            Rect scrollArea = new Rect(12f, 62f, w - 25f, h - 83f);
            craftingScroll = GUI.BeginScrollView(
                scrollArea, craftingScroll,
                new Rect(0f, 0f, viewWidth, 1130f));

            float y = 2f;
            string[] tierNames = {
                "ЭТАП 0 / ПРИРОДА: КАМЕНЬ, ПАЛКИ, ВОЛОКНА",
                "ЭТАП 1 / СНАРЯЖЕНИЕ ИЗ ДЕРЕВА",
                "ЭТАП 2 / ХЛОПКОВАЯ ОДЕЖДА"
            };

            for (int tier = 0; tier <= 2; tier++)
            {
                GUI.Box(new Rect(2f, y, viewWidth - 15f, 28f),
                    tierNames[tier]);
                y += 33f;

                foreach (SectorRecipe recipe in SectorCrafting.Recipes)
                {
                    if (recipe.Tier != tier) continue;

                    string ingredients = "";
                    foreach (SectorIngredient ingredient in recipe.Ingredients)
                    {
                        if (ingredients.Length > 0) ingredients += "  +  ";
                        ingredients += SectorItems.Get(ingredient.ItemId).Label +
                            " " + Inventory.Count(ingredient.ItemId) + "/" +
                            ingredient.Count;
                    }

                    GUI.DrawTexture(
                        new Rect(10f, y + 2f, 36f, 36f),
                        SectorItemIcons.Get(recipe.OutputId).texture,
                        ScaleMode.ScaleToFit, true);
                    GUI.Label(new Rect(52f, y, viewWidth - 188f, 24f),
                        recipe.Name + "  →  " +
                        SectorItems.Get(recipe.OutputId).Label);

                    GUI.Label(new Rect(52f, y + 23f, viewWidth - 188f, 18f),
                        ingredients);

                    bool canCraft =
                        SectorCrafting.CanCraft(Inventory, recipe);
                    bool previous = GUI.enabled;
                    GUI.enabled = canCraft;

                    if (GUI.Button(new Rect(viewWidth - 119f, y + 7f,
                        105f, 32f), "СОЗДАТЬ"))
                    {
                        if (SectorCrafting.Craft(Inventory, recipe.Id))
                        {
                            Journal?.RecordCraft();
                            Notify("Создано: " +
                                SectorItems.Get(recipe.OutputId).Label);
                        }
                    }

                    GUI.enabled = previous;
                    y += 54f;
                }

                y += 8f;
            }

            GUI.EndScrollView();

            if (GUI.Button(new Rect(w - 84f, 5f, 71f, 22f), "ЗАКРЫТЬ"))
                ToggleCrafting();
        }

        static void DrawBar(Rect r, string label, float value, Color fill)
        {
            GUI.Box(r, string.Empty);
            Color old = GUI.color;
            GUI.color = fill;
            GUI.DrawTexture(
                new Rect(r.x + 2f, r.y + 2f,
                    (r.width - 4f) * Mathf.Clamp01(value / 100f),
                    r.height - 4f),
                Texture2D.whiteTexture);
            GUI.color = old;

            GUI.Label(new Rect(r.x + 6f, r.y - 1f, r.width - 7f, r.height + 2f),
                label + " " + Mathf.RoundToInt(value));
        }

        void DrawInventoryWindow(int id)
        {
            float w = Mathf.Min(850f, Screen.width - 18f);
            float h = Mathf.Min(620f, Screen.height - 22f);
            float leftWidth = Mathf.Min(258f, w * .34f);
            float rightX = leftWidth + 13f;
            float rightWidth = w - rightX - 15f;
            float gridBottom = h - 131f;

            GUI.Box(new Rect(10f, 31f, leftWidth - 2f, h - 43f),
                "ПЕРСОНАЖ  /  СНАРЯЖЕНИЕ");

            DrawCharacterSilhouette(
                12f + leftWidth * .5f,
                Mathf.Min(h - 218f, 356f));

            for (int i = 0; i < 5; i++)
            {
                SectorEquipment.GearSlot gearSlot =
                    (SectorEquipment.GearSlot)i;
                string equipped = Armor != null
                    ? Armor.Equipped(gearSlot) : "";
                string display = SectorItems.TryGet(
                    equipped, out SectorItemDefinition item)
                        ? item.Label : "ПУСТО";

                float y = 56f + i * 51f;
                GUI.Label(new Rect(17f, y, leftWidth - 13f, 18f),
                    gearSlot.ToString().ToUpperInvariant());

                if (GUI.Button(new Rect(17f, y + 19f,
                    leftWidth - 25f, 27f), display +
                    (string.IsNullOrEmpty(equipped) ? "" : "   [СНЯТЬ]")))
                    Armor?.Unequip(gearSlot, Inventory);
            }

            float defenses = Armor != null
                ? (1f - Armor.DamageMultiplier) * 100f : 0f;
            GUI.Label(new Rect(17f, h - 116f, leftWidth - 26f, 22f),
                "БРОНЯ  " + defenses.ToString("0") + "% ЗАЩИТЫ");
            GUI.Label(new Rect(17f, h - 93f, leftWidth - 26f, 22f),
                "HP " + player.Health.ToString("0") +
                "   ВЫНОСЛИВОСТЬ " + (Needs != null
                    ? Needs.stamina.ToString("0") : "100"));
            GUI.Label(new Rect(17f, h - 69f, leftWidth - 26f, 22f),
                "СЫТОСТЬ " + (Needs != null ? Needs.hunger.ToString("0") : "100") +
                "   ВОДА " + (Needs != null
                    ? Needs.thirst.ToString("0") : "100"));

            GUI.Label(new Rect(rightX, 33f, rightWidth, 21f),
                "РЮКЗАК  " + Inventory.UsedSlots + "/" +
                Inventory.SlotLimit + " ЯЧЕЕК   " +
                Inventory.Weight.ToString("0.0") + "/" +
                Inventory.MaxWeight.ToString("0.0") + " KG");

            GUI.Box(new Rect(rightX - 4f, 57f,
                rightWidth + 7f, gridBottom - 51f), "");

            int columns = Mathf.Max(2,
                Mathf.FloorToInt((rightWidth - 22f) / 105f));
            float cellWidth = (rightWidth - 24f) / columns - 5f;
            const float cellHeight = 75f;
            int rows = Mathf.CeilToInt(
                Mathf.Max(1, Inventory.Stacks.Count) / (float)columns);
            float contentHeight = Mathf.Max(gridBottom - 72f,
                rows * (cellHeight + 5f) + 8f);

            inventoryScroll = GUI.BeginScrollView(
                new Rect(rightX, 64f, rightWidth, gridBottom - 64f),
                inventoryScroll,
                new Rect(0f, 0f, rightWidth - 22f, contentHeight));

            GUIStyle itemStyle = new GUIStyle(GUI.skin.button);
            itemStyle.wordWrap = true;
            itemStyle.alignment = TextAnchor.LowerCenter;
            itemStyle.fontSize = 11;

            for (int i = 0; i < Inventory.Stacks.Count; i++)
            {
                SectorItemStack stack = Inventory.Stacks[i];
                if (!SectorItems.TryGet(stack.id,
                    out SectorItemDefinition item)) continue;

                int row = i / columns;
                int col = i % columns;
                float x = 4f + col * (cellWidth + 5f);
                float y = 4f + row * (cellHeight + 5f);
                bool isSelected = selectedInventoryItem == stack.id;

                Color old = GUI.color;
                if (isSelected) GUI.color =
                    new Color(.75f, .95f, .84f);

                if (GUI.Button(
                    new Rect(x, y, cellWidth, cellHeight),
                    item.Label + "\n×" + stack.count,
                    itemStyle))
                    selectedInventoryItem = stack.id;
                GUI.color = old;
                GUI.DrawTexture(
                    new Rect(x + cellWidth * .5f - 17f, y + 3f, 34f, 34f),
                    SectorItemIcons.Get(stack.id).texture,
                    ScaleMode.ScaleToFit, true);
            }

            GUI.EndScrollView();

            float footer = h - 118f;
            GUI.Box(new Rect(rightX - 4f, footer, rightWidth + 7f, 95f),
                "ВЫБРАННЫЙ ПРЕДМЕТ");

            if (Inventory.Count(selectedInventoryItem) <= 0)
                selectedInventoryItem = Inventory.Stacks.Count > 0
                    ? Inventory.Stacks[0].id : "";

            if (SectorItems.TryGet(selectedInventoryItem,
                out SectorItemDefinition selected))
            {
                GUI.Label(new Rect(rightX + 8f, footer + 25f,
                    rightWidth - 24f, 20f), selected.Label + "  /  " +
                    SectorRussian.Kind(selected.Kind) + "  /  " +
                    selected.Weight.ToString("0.00") + " KG");

                if (selected.IsConsumable &&
                    GUI.Button(new Rect(rightX + 8f,
                        footer + 53f, 105f, 28f), "ИСПОЛЬЗОВАТЬ"))
                    UseItem(selected.Id);

                if ((selected.Kind == SectorItemKind.Armor ||
                    selected.Kind == SectorItemKind.Melee ||
                    selected.Kind == SectorItemKind.Firearm) &&
                    GUI.Button(new Rect(rightX + 119f,
                        footer + 53f, 115f, 28f), "НАДЕТЬ"))
                    EquipItem(selected.Id);
            }

            if (GUI.Button(new Rect(w - 84f, 5f, 70f, 22f), "ЗАКРЫТЬ"))
                ToggleInventory();
        }

        static void DrawCharacterSilhouette(float cx, float cy)
        {
            Color old = GUI.color;
            GUI.color = new Color(.26f, .37f, .37f, .62f);
            GUI.DrawTexture(new Rect(cx - 17f, cy - 25f, 34f, 34f),
                Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 26f, cy + 12f, 52f, 76f),
                Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 42f, cy + 15f, 14f, 70f),
                Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx + 28f, cy + 15f, 14f, 70f),
                Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 23f, cy + 91f, 18f, 78f),
                Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx + 5f, cy + 91f, 18f, 78f),
                Texture2D.whiteTexture);
            GUI.color = old;
        }

        string SlotName(int index)
        {
            string id = equipment[index];
            return SectorItems.TryGet(id, out SectorItemDefinition item)
                ? item.Label : "-";
        }

        void OnDisable()
        {
            if (player != null)
                player.InputBlockedByUI = false;
        }

        void OnDestroy()
        {
            if (crateMaterial != null)
                Destroy(crateMaterial);
        }
    }
}
