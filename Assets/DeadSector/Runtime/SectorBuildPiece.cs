using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace DeadSector
{
    /// <summary>
    /// Runtime placeholder mesh with real colliders, durability and optional
    /// storage. Later FBX/prefabs can replace visuals independently.
    /// </summary>
    public sealed class SectorBuildPiece : MonoBehaviour
    {
        public string Id { get; private set; }
        public SectorBuildKind Kind { get; private set; }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public bool Destroyed => Health <= 0f;
        public bool IsStorage => Kind == SectorBuildKind.Storage;

        SectorInventory storage;
        SectorDoor hingedDoor;
        NavMeshObstacle doorObstacle;

        public void Configure(SectorBuildSnapshot snapshot,
            Material woodMaterial, Material metalMaterial)
        {
            if (snapshot == null ||
                !SectorBuildCatalog.IsValid(snapshot.kind))
                throw new ArgumentException("Invalid build snapshot");

            SectorBuildSpecification plan =
                SectorBuildCatalog.At((int)snapshot.kind);

            Id = snapshot.id;
            Kind = snapshot.kind;
            MaxHealth = plan.Health;
            Health = Mathf.Clamp(snapshot.health, 0f, MaxHealth);
            transform.SetPositionAndRotation(snapshot.position,
                Quaternion.Euler(0f, snapshot.angle, 0f));

            switch (Kind)
            {
                case SectorBuildKind.Foundation:
                    Cube("Deck", new Vector3(0f, .13f, 0f),
                        new Vector3(3f, .25f, 3f), woodMaterial);
                    break;
                case SectorBuildKind.Wall:
                    Cube("WallPanel", new Vector3(0f, 1.25f, 0f),
                        plan.Dimensions, woodMaterial);
                    Cube("Brace", new Vector3(0f, 1.25f, -.19f),
                        new Vector3(.17f, 2.5f, .14f), metalMaterial);
                    break;
                case SectorBuildKind.Barricade:
                    Cube("BarricadeBody", new Vector3(0f, .63f, 0f),
                        plan.Dimensions, woodMaterial);
                    Cube("TopBeam", new Vector3(0f, 1.15f, 0f),
                        new Vector3(3f, .15f, .65f), metalMaterial);
                    break;
                case SectorBuildKind.Storage:
                    Cube("StorageBody", new Vector3(0f, .55f, 0f),
                        plan.Dimensions, woodMaterial);
                    Cube("StorageLid", new Vector3(0f, 1.1f, 0f),
                        new Vector3(1.4f, .16f, 1.2f), metalMaterial);
                    storage = new SectorInventory(12, 60f);
                    storage.Import(snapshot.storage);
                    break;
                case SectorBuildKind.Roof:
                    // Two 3m long sloped wood panels; centre ridge rises
                    // ~0.78m over the supported 2.5m wall top.
                    Cube("Roof_Slope_Left",
                        new Vector3(-.73f, .39f, 0f),
                        new Vector3(1.85f, .18f, 3.20f),
                        woodMaterial).transform.localRotation =
                            Quaternion.Euler(0f, 0f, -25f);
                    Cube("Roof_Slope_Right",
                        new Vector3(.73f, .39f, 0f),
                        new Vector3(1.85f, .18f, 3.20f),
                        woodMaterial).transform.localRotation =
                            Quaternion.Euler(0f, 0f, 25f);
                    Cube("Ridge_Beam", new Vector3(0f, .77f, 0f),
                        new Vector3(.22f, .22f, 3.25f), metalMaterial);
                    break;
                case SectorBuildKind.Campfire:
                    for (int i = 0; i < 8; i++)
                    {
                        float theta = i * Mathf.PI / 4f;
                        Cube("Fire_Stone_" + i,
                            new Vector3(Mathf.Cos(theta) * .51f,
                                .15f, Mathf.Sin(theta) * .51f),
                            new Vector3(.28f, .28f, .30f), metalMaterial);
                    }
                    for (int i = -1; i <= 1; i += 2)
                        Cube("Firewood_" + i,
                            new Vector3(0f, .19f, 0f),
                            new Vector3(.12f, .16f, 1f), woodMaterial)
                            .transform.localRotation =
                                Quaternion.Euler(0f, i * 36f, 0f);
                    GameObject flame = GameObject.CreatePrimitive(
                        PrimitiveType.Sphere);
                    flame.name = "Fire_Core";
                    flame.transform.SetParent(transform, false);
                    flame.transform.localPosition =
                        new Vector3(0f, .44f, 0f);
                    flame.transform.localScale =
                        new Vector3(.30f, .47f, .30f);
                    flame.GetComponent<Renderer>().sharedMaterial =
                        metalMaterial;
                    Collider fireCollider = flame.GetComponent<Collider>();
                    if (fireCollider != null) fireCollider.enabled = false;
                    Light glow = flame.AddComponent<Light>();
                    glow.type = LightType.Point;
                    glow.color = new Color(1f, .48f, .18f);
                    glow.range = 8f;
                    glow.intensity = 1.7f;
                    glow.shadows = LightShadows.None;
                    break;
                case SectorBuildKind.Door:
                    Cube("LeftPost", new Vector3(-1.38f, 1.3f, 0f),
                        new Vector3(.24f, 2.6f, .4f), woodMaterial);
                    Cube("RightPost", new Vector3(1.38f, 1.3f, 0f),
                        new Vector3(.24f, 2.6f, .4f), woodMaterial);
                    Cube("CrossBeam", new Vector3(0f, 2.48f, 0f),
                        new Vector3(3f, .24f, .4f), woodMaterial);

                    var pivot = new GameObject("HingedDoor_Pivot");
                    pivot.transform.SetParent(transform, false);
                    pivot.transform.localPosition =
                        new Vector3(-1.22f, 0f, 0f);
                    hingedDoor = pivot.AddComponent<SectorDoor>();

                    GameObject slab = GameObject.CreatePrimitive(
                        PrimitiveType.Cube);
                    slab.name = "Door_Slab";
                    slab.transform.SetParent(pivot.transform, false);
                    slab.transform.localPosition =
                        new Vector3(1.13f, 1.16f, 0f);
                    slab.transform.localScale =
                        new Vector3(2.25f, 2.3f, .19f);
                    slab.GetComponent<Renderer>().sharedMaterial =
                        woodMaterial;

                    doorObstacle = pivot.AddComponent<NavMeshObstacle>();
                    doorObstacle.shape = NavMeshObstacleShape.Box;
                    doorObstacle.size =
                        new Vector3(2.25f, 2.3f, .19f);
                    doorObstacle.center =
                        new Vector3(1.13f, 1.16f, 0f);
                    doorObstacle.carving = true;
                    doorObstacle.carveOnlyStationary = true;
                    if (snapshot.doorOpen)
                        hingedDoor.Toggle();
                    break;
            }

            if (Kind != SectorBuildKind.Foundation &&
                Kind != SectorBuildKind.Door)
            {
                NavMeshObstacle obstacle =
                    gameObject.AddComponent<NavMeshObstacle>();
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.size = plan.Dimensions;
                obstacle.center = Vector3.up * plan.Dimensions.y * .5f;
                obstacle.carving = true;
                obstacle.carveOnlyStationary = true;
            }
        }

        void Update()
        {
            if (doorObstacle != null && hingedDoor != null)
                doorObstacle.enabled = !hingedDoor.IsOpen;
        }

        public bool CanRepair =>
            !Destroyed && Health < MaxHealth - .01f;

        // One plank (wood) restores 35 durability, never beyond max.
        public float Repair(float amount)
        {
            if (Destroyed || amount <= 0f)
                return 0f;
            float restored = Mathf.Min(amount, MaxHealth - Health);
            Health += restored;
            return restored;
        }

        GameObject Cube(string label, Vector3 localPosition,
            Vector3 size, Material surface)
        {
            GameObject child = GameObject.CreatePrimitive(
                PrimitiveType.Cube);
            child.name = label;
            child.transform.SetParent(transform, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = size;
            child.GetComponent<Renderer>().sharedMaterial = surface;
            return child;
        }

        public void Damage(float amount)
        {
            if (Destroyed || amount <= 0f)
                return;

            Health = Mathf.Max(0f, Health - amount);
            if (Destroyed)
            {
                // Prevent one more zombie strike against a deferred corpse,
                // remove it from exported save and persist the destroyed base.
                SectorBaseBuilding owner = GetComponentInParent<SectorBaseBuilding>();
                gameObject.SetActive(false);
                if (owner != null)
                    owner.RecordDestroyedPiece(this);
                Destroy(gameObject);
            }
        }

        public SectorBuildSnapshot Export()
        {
            return new SectorBuildSnapshot
            {
                id = Id,
                kind = Kind,
                position = transform.position,
                angle = transform.eulerAngles.y,
                health = Health,
                doorOpen = hingedDoor != null && hingedDoor.IsOpen,
                storage = storage != null
                    ? storage.Export()
                    : new List<SectorItemStack>()
            };
        }

        public bool TryDeposit(SectorInventory backpack, string itemId)
        {
            if (!IsStorage || Destroyed ||
                backpack == null || storage == null ||
                backpack.Count(itemId) < 1 ||
                !storage.CanAdd(itemId, 1))
                return false;

            if (!backpack.Remove(itemId, 1))
                return false;

            if (storage.Add(itemId, 1))
                return true;

            backpack.Add(itemId, 1);
            return false;
        }

        public bool TryWithdraw(SectorInventory backpack,
            out string takenName)
        {
            takenName = "";
            if (!IsStorage || Destroyed || backpack == null ||
                storage == null || storage.Stacks.Count == 0)
                return false;

            SectorItemStack stack = storage.Stacks[0];
            if (!backpack.Add(stack.id, 1))
                return false;

            storage.Remove(stack.id, 1);
            takenName = SectorItems.Get(stack.id).Label;
            return true;
        }

        public int StorageItemCount
        {
            get
            {
                if (storage == null)
                    return 0;

                int total = 0;
                foreach (SectorItemStack stack in storage.Stacks)
                    total += stack.count;
                return total;
            }
        }
    }
}
