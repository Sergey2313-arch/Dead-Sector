using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// Survival base construction: B mode, 1..5 plans, Q/E rotation,
    /// left click to pay/place, right click to cancel. All parts use
    /// physics collisions and serialize per save profile.
    /// </summary>
    public sealed class SectorBaseBuilding : MonoBehaviour
    {
        public SectorPlayer player;
        public SectorGameplay gameplay;
        public SectorInventory backpack;

        readonly List<SectorBuildPiece> pieces =
            new List<SectorBuildPiece>();
        GameObject preview;
        Renderer previewRenderer;
        Material woodMaterial;
        Material accentMaterial;
        Material previewMaterial;
        int selected;
        float orientation;
        bool buildMode;
        bool canPlace;
        Vector3 plannedPosition;

        public bool BuildMode => buildMode;
        public bool CanPlace => canPlace;
        public int PieceCount
        {
            get
            {
                RemoveDestroyed();
                return pieces.Count;
            }
        }
        public SectorBuildSpecification SelectedPlan =>
            SectorBuildCatalog.At(selected);
        public string ControlHint => buildMode
            ? "B ВЫХОД | 1–5 ВЫБОР | Q/E ПОВОРОТ | ЛКМ ПОСТРОИТЬ | ПКМ ОТМЕНА"
            : "[B] СТРОИТЬ";

        public void Configure(SectorPlayer target,
            SectorGameplay owner, SectorInventory items)
        {
            player = target;
            gameplay = owner;
            backpack = items;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            woodMaterial = new Material(shader);
            woodMaterial.color = new Color(.43f, .33f, .23f);

            accentMaterial = new Material(shader);
            accentMaterial.color = new Color(.26f, .34f, .32f);

            previewMaterial = new Material(shader);
            previewMaterial.color = new Color(.42f, .62f, .38f);

            preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
            preview.name = "Build_Placement_Ghost";
            preview.transform.SetParent(transform, true);
            // Layer 2 is excluded by placement checks and player raycasts.
            preview.layer = 2;
            Collider col = preview.GetComponent<Collider>();
            if (col != null) col.enabled = false;
            previewRenderer = preview.GetComponent<Renderer>();
            previewRenderer.sharedMaterial = previewMaterial;
            preview.SetActive(false);
        }

        void Update()
        {
            if (player == null || gameplay == null || !player.Ready)
                return;

            if (player.InputBlockedByUI || player.Health <= 0f)
            {
                if (buildMode)
                    ExitBuildMode();
                return;
            }

            if (SectorInput.Pressed(KeyCode.B))
            {
                buildMode = !buildMode;
                if (!buildMode)
                    preview.SetActive(false);
            }

            if (!buildMode)
                return;

            for (int i = 0; i < SectorBuildCatalog.Count; i++)
                if (SectorInput.Pressed(KeyCode.Alpha1 + i))
                    selected = i;

            if (SectorInput.Pressed(KeyCode.Q))
                orientation -= 90f;
            if (SectorInput.Pressed(KeyCode.E))
                orientation += 90f;

            if (SectorInput.RightClick)
            {
                ExitBuildMode();
                return;
            }

            PositionPreview();

            if (SectorInput.Click)
                TryPlace();
        }

        public void ExitBuildMode()
        {
            buildMode = false;
            if (preview != null)
                preview.SetActive(false);
        }

        void PositionPreview()
        {
            SectorBuildSpecification spec = SelectedPlan;
            Vector3 forward = player.transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 target = player.transform.position + forward * 4.5f;
            target.x = Mathf.Round(target.x / 3f) * 3f;
            target.z = Mathf.Round(target.z / 3f) * 3f;
            target.y = SurfaceHeight(target.x, target.z);
            plannedPosition = target;

            if (preview == null)
                return;

            preview.SetActive(true);
            preview.transform.SetPositionAndRotation(
                target + Vector3.up * spec.Dimensions.y * .5f,
                Quaternion.Euler(0f, orientation, 0f));
            preview.transform.localScale = spec.Dimensions;
            canPlace = ValidPlacement(spec, target, orientation) &&
                SectorBuildCatalog.CanAfford(backpack, spec);

            if (previewMaterial != null)
                previewMaterial.color = canPlace
                    ? new Color(.31f, .57f, .32f)
                    : new Color(.62f, .25f, .22f);
        }

        public bool TryPlace()
        {
            if (!buildMode || !canPlace ||
                !ValidPlacement(SelectedPlan, plannedPosition, orientation) ||
                !SectorBuildCatalog.CanAfford(backpack, SelectedPlan))
                return false;

            SectorBuildSpecification plan = SelectedPlan;
            SectorBuildSnapshot snapshot = new SectorBuildSnapshot
            {
                id = Guid.NewGuid().ToString("N"),
                kind = plan.Kind,
                position = plannedPosition,
                angle = orientation,
                health = plan.Health
            };

            SectorBuildPiece created = Spawn(snapshot);
            if (created == null)
                return false;

            if (!SectorBuildCatalog.Consume(backpack, plan))
            {
                pieces.Remove(created);
                Destroy(created.gameObject);
                return false;
            }

            gameplay.NotifyConstruction(plan.Label);
            canPlace = false;
            return true;
        }

        bool ValidPlacement(SectorBuildSpecification plan,
            Vector3 position, float rotation)
        {
            RemoveDestroyed();

            if (pieces.Count >= SectorBuildCatalog.MaxPieces ||
                Mathf.Abs(position.x) > 3990f ||
                Mathf.Abs(position.z) > 3990f ||
                SectorGeography.TryGetWaterLevel(
                    position.x, position.z, out _))
                return false;

            Vector3 center = position +
                Vector3.up * (plan.Dimensions.y * .5f + .12f);

            if ((player.transform.position - center).sqrMagnitude < 5f)
                return false;

            Quaternion rot = Quaternion.Euler(0f, rotation, 0f);

            // Forbid structures that would hang off a steep slope or clip
            // through a hill. Snap placement to the actual streamed ground.
            float centerGround = SurfaceHeight(position.x, position.z);
            float tolerance = plan.Kind == SectorBuildKind.Foundation
                ? .55f : 1.25f;
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    Vector3 offset = rot * new Vector3(
                        sx * plan.Dimensions.x * .4f, 0f,
                        sz * plan.Dimensions.z * .4f);
                    float corner = SurfaceHeight(
                        position.x + offset.x, position.z + offset.z);
                    if (Mathf.Abs(corner - centerGround) > tolerance)
                        return false;
                }

            Collider[] blockers = Physics.OverlapBox(
                center, plan.Dimensions * .5f * .92f,
                rot, ~(1 << 2), QueryTriggerInteraction.Ignore);

            foreach (Collider collider in blockers)
            {
                if (collider == null ||
                    collider is TerrainCollider ||
                    collider.gameObject.layer == 2)
                    continue;

                return false;
            }
            return true;
        }

        static float SurfaceHeight(float x, float z)
        {
            foreach (Terrain t in Terrain.activeTerrains)
            {
                if (t == null || t.terrainData == null)
                    continue;

                Vector3 p = t.transform.position;
                Vector3 sz = t.terrainData.size;
                if (x >= p.x && x <= p.x + sz.x &&
                    z >= p.z && z <= p.z + sz.z)
                    return p.y + t.SampleHeight(new Vector3(x, 0f, z));
            }
            return SectorLayout.Height(x, z);
        }

        SectorBuildPiece Spawn(SectorBuildSnapshot snapshot)
        {
            if (snapshot == null ||
                !SectorBuildCatalog.IsValid(snapshot.kind))
                return null;

            GameObject go = new GameObject("Player_Build_" + snapshot.kind);
            go.transform.SetParent(transform, true);
            var part = go.AddComponent<SectorBuildPiece>();
            try
            {
                part.Configure(snapshot, woodMaterial, accentMaterial);
            }
            catch (Exception error)
            {
                Debug.LogError("[Dead Sector] Building failed: " +
                    error.Message);
                Destroy(go);
                return null;
            }

            pieces.Add(part);
            return part;
        }

        public SectorBuildPiece NearbyStorage()
        {
            if (player == null)
                return null;
            Vector3 pos = player.transform.position;

            SectorBuildPiece closest = null;
            float distance = 3f * 3f;

            foreach (SectorBuildPiece piece in pieces)
            {
                if (piece == null || piece.Destroyed ||
                    !piece.IsStorage)
                    continue;

                float d = (piece.transform.position - pos).sqrMagnitude;
                if (d < distance)
                {
                    distance = d;
                    closest = piece;
                }
            }

            return closest;
        }

        public List<SectorBuildSnapshot> Export()
        {
            RemoveDestroyed();
            var result = new List<SectorBuildSnapshot>(pieces.Count);
            foreach (SectorBuildPiece piece in pieces)
                if (piece != null && !piece.Destroyed)
                    result.Add(piece.Export());
            return result;
        }

        public void Import(IEnumerable<SectorBuildSnapshot> saved)
        {
            ExitBuildMode();
            foreach (SectorBuildPiece p in pieces)
            {
                if (p == null) continue;

                // Destroy() is deferred until end-of-frame. Deactivate first
                // so previous save-slot colliders and NavMesh obstacles do not
                // interfere with the newly imported structures in this frame.
                p.gameObject.SetActive(false);
                Destroy(p.gameObject);
            }
            pieces.Clear();

            if (saved == null)
                return;

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (SectorBuildSnapshot state in saved)
            {
                if (state == null || string.IsNullOrEmpty(state.id) ||
                    !ids.Add(state.id) ||
                    !SectorBuildCatalog.IsValid(state.kind) ||
                    state.health <= 0f ||
                    float.IsNaN(state.health) ||
                    float.IsInfinity(state.health) ||
                    float.IsNaN(state.angle) ||
                    float.IsInfinity(state.angle) ||
                    float.IsNaN(state.position.x) ||
                    float.IsNaN(state.position.y) ||
                    float.IsNaN(state.position.z) ||
                    float.IsInfinity(state.position.x) ||
                    float.IsInfinity(state.position.y) ||
                    float.IsInfinity(state.position.z) ||
                    Mathf.Abs(state.position.x) > 3990f ||
                    Mathf.Abs(state.position.z) > 3990f ||
                    pieces.Count >= SectorBuildCatalog.MaxPieces)
                    continue;
                Spawn(state);
            }
        }

        void RemoveDestroyed()
        {
            pieces.RemoveAll(p => p == null || p.Destroyed);
        }

        void OnDestroy()
        {
            if (woodMaterial != null) Destroy(woodMaterial);
            if (accentMaterial != null) Destroy(accentMaterial);
            if (previewMaterial != null) Destroy(previewMaterial);
        }
    }
}
