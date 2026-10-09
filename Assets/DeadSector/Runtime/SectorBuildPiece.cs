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
        Material material;

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

            material = woodMaterial;
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
            }

            if (Kind != SectorBuildKind.Foundation)
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

        void Cube(string label, Vector3 localPosition,
            Vector3 size, Material surface)
        {
            GameObject child = GameObject.CreatePrimitive(
                PrimitiveType.Cube);
            child.name = label;
            child.transform.SetParent(transform, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = size;
            child.GetComponent<Renderer>().sharedMaterial = surface;
        }

        public void Damage(float amount)
        {
            if (Destroyed || amount <= 0f)
                return;

            Health = Mathf.Max(0f, Health - amount);
            if (Destroyed)
                Destroy(gameObject);
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
