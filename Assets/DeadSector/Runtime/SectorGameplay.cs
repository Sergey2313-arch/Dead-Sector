using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DeadSector
{
    [Serializable]
    public sealed class SectorGameSave
    {
        public int version = 1;
        public Vector3 position;
        public float health;
        public float hunger;
        public float thirst;
        public float stamina;
        public string primary = "";
        public string secondary = "";
        public string melee = "";
        public int selectedSlot = 2;
        public List<SectorItemStack> inventory = new List<SectorItemStack>();
        public List<SectorContainerSnapshot> containers =
            new List<SectorContainerSnapshot>();
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

        readonly Dictionary<string, SectorLootContainer> active =
            new Dictionary<string, SectorLootContainer>(StringComparer.Ordinal);

        readonly Dictionary<string, List<SectorItemStack>> persistent =
            new Dictionary<string, List<SectorItemStack>>(StringComparer.Ordinal);

        string[] equipment = { "", "", "knife" };
        int selectedSlot = 2;
        bool inventoryOpen;
        bool loadedOnce;
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

            Needs = target.GetComponent<SectorSurvival>();
            if (Needs == null)
                Needs = target.gameObject.AddComponent<SectorSurvival>();

            Inventory.Add("knife", 1);
            Inventory.Add("water", 1);
            Inventory.Add("bandage", 2);
            equipment[2] = "knife";
            selectedSlot = 2;
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

            if (SectorInput.Pressed(KeyCode.F5))
                SaveGame();

            if (SectorInput.Pressed(KeyCode.F9))
                LoadGame(false);

            if (SectorInput.Pressed(KeyCode.I))
                ToggleInventory();

            if (SectorInput.Pressed(KeyCode.Alpha1)) selectedSlot = 0;
            if (SectorInput.Pressed(KeyCode.Alpha2)) selectedSlot = 1;
            if (SectorInput.Pressed(KeyCode.Alpha3)) selectedSlot = 2;

            if (Time.time >= refreshAt)
            {
                refreshAt = Time.time + .75f;
                RefreshContainers();
            }

            if (!inventoryOpen && player.Health > 0f &&
                Cursor.lockState == CursorLockMode.Locked)
            {
                if (SectorInput.Pressed(KeyCode.E))
                    Interact();

                if (SectorInput.Click)
                    Attack();
            }

            if (Time.time >= autoSaveAt)
            {
                autoSaveAt = Time.time + 90f;
                SaveGame(true);
            }
        }

        void ToggleInventory()
        {
            inventoryOpen = !inventoryOpen;
            player.InputBlockedByUI = inventoryOpen;
            Cursor.lockState = inventoryOpen
                ? CursorLockMode.None
                : CursorLockMode.Locked;
            Cursor.visible = inventoryOpen;
        }

        void RefreshContainers()
        {
            Vector3 origin = player.transform.position;
            var expected = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < StarterSupplies.Length; i++)
            {
                Vector2 pos = StarterSupplies[i];
                EnsureNearby(
                    "starter_" + i, "Supply Box",
                    pos, origin, expected);
            }

            foreach (SectorMapPlan.Location location in SectorMapPlan.Locations)
            {
                if (location.Kind == SectorMapPlan.LocationKind.Wilderness &&
                    location.Id != "forest_camp")
                    continue;

                EnsureNearby(
                    "poi_" + location.Id,
                    location.Name + " Supplies",
                    location.MapPosition + new Vector2(18f, 14f),
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
            crate.transform.position = new Vector3(pos.x, y + .7f, pos.y);
            crate.transform.localScale = new Vector3(1.3f, 1.4f, 1.0f);

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
            SectorLootContainer container = NearbyContainer();
            SectorDoor door = NearbyDoor();

            if (door != null && (container == null ||
                Vector3.Distance(door.transform.position, player.transform.position) <
                Vector3.Distance(container.transform.position, player.transform.position)))
            {
                door.Toggle();
                Notify(door.IsOpen ? "Door opened" : "Door closed");
                return;
            }

            if (container == null)
            {
                Notify("No supply container in reach");
                return;
            }

            if (container.TakeFirst(Inventory, out string description))
            {
                persistent[container.containerId] = container.Export();
                Notify("Picked up: " + description);
            }
            else
            {
                Notify(container.Empty
                    ? "Container is empty"
                    : "Not enough free inventory space / weight");
            }
        }

        string EquippedId()
        {
            string id = equipment[Mathf.Clamp(selectedSlot, 0, 2)];
            return Inventory.Count(id) > 0 ? id : string.Empty;
        }

        void Attack()
        {
            if (Time.time < nextAttack)
                return;

            string id = EquippedId();
            bool firearm = SectorItems.TryGet(id, out SectorItemDefinition weapon) &&
                           weapon.Kind == SectorItemKind.Firearm;

            float range = firearm
                ? id == "rifle" ? 95f : 55f
                : 2.45f;

            float damage = firearm ? weapon.Damage :
                weapon.Damage > 0f ? weapon.Damage : 12f;

            if (firearm)
            {
                string ammo = id == "rifle" ? "556" : "9mm";

                if (!Inventory.Remove(ammo, 1))
                {
                    Notify("No " + SectorItems.Get(ammo).Label);
                    return;
                }
            }

            nextAttack = Time.time + (firearm ? .28f : .65f);

            Vector3 origin = firearm && player.view != null
                ? player.view.transform.position
                : player.transform.position + Vector3.up * 1.25f;

            Vector3 forward = firearm && player.view != null
                ? player.view.transform.forward
                : player.transform.forward;

            SectorZombie target = null;
            float closest = range;

            foreach (SectorZombie zombie in
                FindObjectsByType<SectorZombie>(FindObjectsSortMode.None))
            {
                if (zombie == null || zombie.Dead)
                    continue;

                Vector3 toEnemy = zombie.transform.position +
                    Vector3.up * 1.1f - origin;
                float distance = toEnemy.magnitude;

                if (distance > closest || distance < .05f)
                    continue;

                float facing = Vector3.Dot(forward, toEnemy / distance);
                if (facing < (firearm ? .982f : .25f))
                    continue;

                // Static environment blocks hits; actor colliders use layer 2.
                if (Physics.Raycast(
                    origin, toEnemy / distance,
                    distance - .15f, ~(1 << 2),
                    QueryTriggerInteraction.Ignore))
                    continue;

                target = zombie;
                closest = distance;
            }

            if (target != null)
            {
                target.TakeDamage(damage);
                Notify("Hit: " + Mathf.RoundToInt(damage) + " damage");
            }
        }

        void UseItem(string id)
        {
            if (!SectorItems.TryGet(id, out SectorItemDefinition def) ||
                !def.IsConsumable || Inventory.Count(id) == 0)
                return;

            if (def.HealthRestore > 0f && player.Health >= 100f &&
                def.HungerRestore <= 0f && def.ThirstRestore <= 0f)
            {
                Notify("Health is already full");
                return;
            }

            if (!Inventory.Remove(id, 1))
                return;

            Needs?.Restore(def.HungerRestore, def.ThirstRestore);
            player.Heal(def.HealthRestore);
            Notify("Used " + def.Label);
        }

        void EquipItem(string id)
        {
            if (Inventory.Count(id) == 0)
                return;

            SectorItemDefinition item = SectorItems.Get(id);
            int slot;

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
            Notify("Equipped " + item.Label);
        }

        void Notify(string messageText)
        {
            message = messageText;
            messageUntil = Time.time + 3f;
        }

        string SavePath =>
            Path.Combine(Application.persistentDataPath, "DeadSector_Save_v1.json");

        public void SaveGame(bool silent = false)
        {
            if (player == null || !player.Ready)
                return;

            var data = new SectorGameSave
            {
                position = player.transform.position,
                health = player.Health,
                hunger = Needs != null ? Needs.hunger : 100f,
                thirst = Needs != null ? Needs.thirst : 100f,
                stamina = Needs != null ? Needs.stamina : 100f,
                primary = equipment[0],
                secondary = equipment[1],
                melee = equipment[2],
                selectedSlot = selectedSlot,
                inventory = Inventory.Export()
            };

            foreach (var pair in active)
                persistent[pair.Key] = pair.Value.Export();

            foreach (var pair in persistent)
            {
                data.containers.Add(new SectorContainerSnapshot
                {
                    id = pair.Key,
                    items = pair.Value
                });
            }

            try
            {
                string temporary = SavePath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(data, true));

                if (File.Exists(SavePath))
                    File.Copy(SavePath, SavePath + ".bak", true);

                if (File.Exists(SavePath))
                    File.Delete(SavePath);

                File.Move(temporary, SavePath);

                if (!silent)
                    Notify("Game saved");
            }
            catch (Exception ex)
            {
                Debug.LogError("[Dead Sector] Save failed: " + ex.Message);
                Notify("Save failed: check Console");
            }
        }

        public void LoadGame(bool silent)
        {
            if (!File.Exists(SavePath))
            {
                if (!silent) Notify("No save found");
                return;
            }

            try
            {
                var data = JsonUtility.FromJson<SectorGameSave>(
                    File.ReadAllText(SavePath));

                if (data == null || data.version != 1)
                {
                    Notify("Incompatible save version");
                    return;
                }

                Inventory.Import(data.inventory);
                equipment[0] = ValidEquipped(data.primary);
                equipment[1] = ValidEquipped(data.secondary);
                equipment[2] = ValidEquipped(data.melee);
                selectedSlot = Mathf.Clamp(data.selectedSlot, 0, 2);

                Needs?.ApplySaved(data.hunger, data.thirst, data.stamina);
                player.RestoreHealth(data.health);

                foreach (SectorLootContainer container in active.Values)
                    Destroy(container.gameObject);

                active.Clear();
                persistent.Clear();

                if (data.containers != null)
                {
                    foreach (SectorContainerSnapshot state in data.containers)
                    {
                        if (state != null && !string.IsNullOrEmpty(state.id))
                            persistent[state.id] = state.items ??
                                new List<SectorItemStack>();
                    }
                }

                Vector3 p = data.position;
                p.x = Mathf.Clamp(p.x, -3990f, 3990f);
                p.z = Mathf.Clamp(p.z, -3990f, 3990f);

                // Allow time for the streaming world to load the new tile.
                p.y = SectorLayout.Height(p.x, p.z) + 12f;
                player.TeleportTo(p);
                refreshAt = 0f;
                Notify("Save loaded");
            }
            catch (Exception ex)
            {
                Debug.LogError("[Dead Sector] Load failed: " + ex.Message);
                Notify("Load failed: check Console");
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
            if (player == null || !player.Ready)
                return;

            GUI.depth = -110;

            float x = 14f;
            DrawBar(new Rect(x, 104f, 180f, 18f),
                "HUNGER", Needs != null ? Needs.hunger : 100f,
                new Color(.68f, .59f, .25f));

            DrawBar(new Rect(x, 127f, 180f, 18f),
                "THIRST", Needs != null ? Needs.thirst : 100f,
                new Color(.28f, .62f, .89f));

            DrawBar(new Rect(x, 150f, 180f, 18f),
                "STAMINA", Needs != null ? Needs.stamina : 100f,
                new Color(.38f, .76f, .47f));

            string weapon = EquippedId();
            GUI.Box(new Rect(Screen.width * .5f - 140f, Screen.height - 47f,
                280f, 37f),
                "1 PRIMARY  |  2 SIDEARM  |  3 MELEE   " +
                (string.IsNullOrEmpty(weapon)
                    ? "FISTS"
                    : SectorItems.Get(weapon).Label));

            if (Time.time < messageUntil)
            {
                GUI.Box(new Rect(Screen.width * .5f - 160f, Screen.height - 92f,
                    320f, 35f), message);
            }

            if (!inventoryOpen &&
                Cursor.lockState == CursorLockMode.Locked)
            {
                SectorLootContainer nearby = NearbyContainer();
                SectorDoor door = NearbyDoor();

                if (door != null && (nearby == null ||
                    Vector3.Distance(door.transform.position, player.transform.position) <
                    Vector3.Distance(nearby.transform.position, player.transform.position)))
                {
                    GUI.Box(new Rect(Screen.width * .5f - 142f,
                        Screen.height * .61f, 284f, 50f),
                        "[E] " + (door.IsOpen ? "CLOSE DOOR" : "OPEN DOOR"));
                }
                else if (nearby != null)
                {
                    GUI.Box(new Rect(Screen.width * .5f - 142f,
                        Screen.height * .61f, 284f, 50f),
                        nearby.title + "  [E] TAKE" +
                        (nearby.Empty ? "  (EMPTY)" : ""));
                }
            }

            if (inventoryOpen)
            {
                float width = Mathf.Min(540f, Screen.width - 18f);
                float height = Mathf.Min(600f, Screen.height - 22f);

                GUI.Window(
                    1736,
                    new Rect(
                        (Screen.width - width) * .5f,
                        (Screen.height - height) * .5f,
                        width, height),
                    DrawInventoryWindow, "DEAD SECTOR  /  INVENTORY [I]");
            }
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
            GUI.Label(new Rect(14, 29, 400, 22),
                "Capacity: " + Inventory.UsedSlots + " / " + Inventory.SlotLimit +
                " slots   |   Weight: " + Inventory.Weight.ToString("0.0") +
                " / " + Inventory.MaxWeight.ToString("0.0") + " kg");

            GUI.Label(new Rect(14, 53, 440, 22),
                "1: " + SlotName(0) + "   2: " + SlotName(1) +
                "   3: " + SlotName(2));

            float y = 82f;
            int count = Mathf.Min(Inventory.Stacks.Count, 16);

            for (int i = 0; i < count; i++)
            {
                SectorItemStack stack = Inventory.Stacks[i];

                if (!SectorItems.TryGet(stack.id, out SectorItemDefinition item))
                    continue;

                GUI.Label(new Rect(14f, y, 280f, 23f),
                    item.Label + " ×" + stack.count);

                bool consumable = item.IsConsumable;
                bool equip = item.Kind == SectorItemKind.Firearm ||
                             item.Kind == SectorItemKind.Melee;

                if (consumable &&
                    GUI.Button(new Rect(295f, y, 88f, 23f), "Use"))
                {
                    UseItem(stack.id);
                    break;
                }

                if (equip &&
                    GUI.Button(new Rect(295f, y, 88f, 23f), "Equip"))
                {
                    EquipItem(stack.id);
                    break;
                }

                y += 26f;
            }

            GUI.Label(new Rect(14f, Mathf.Min(y + 12f, 515f), 480f, 24f),
                "E pickup  |  LMB attack  |  F5 save  |  F9 load");

            if (GUI.Button(new Rect(14f, 7f, 70f, 20f), "Close"))
                ToggleInventory();
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
