using UnityEngine;

namespace DeadSector
{
    // Zombies are NavMesh driven, but their visible FBX (or fallback rig)
    // can have an animated/imported Y offset independent of the agent.
    // Correct only the living VISUAL, not the NavMesh agent or death physics.
    [DefaultExecutionOrder(700)]
    public sealed class SectorZombieVisualGrounding : MonoBehaviour
    {
        SectorZombie zombie;
        Transform visual;
        float originalLocalY;
        Renderer[] feet;
        SkinnedMeshRenderer[] skins;
        Terrain cachedTerrain;
        float nextCheck;

        public void Configure(SectorZombie infected, Transform model,
            SectorMannequin mannequin)
        {
            zombie = infected;
            visual = model;
            if (visual == null) return;
            originalLocalY = visual.localPosition.y;

            // Procedural actors have explicit boot renderers. Their lowest
            // visual point is a more accurate ground anchor than torso bounds.
            if (mannequin != null &&
                mannequin.leftLeg != null && mannequin.rightLeg != null)
            {
                Transform left = mannequin.leftLeg.Find("Boot");
                Transform right = mannequin.rightLeg.Find("Boot");
                if (left != null && right != null)
                {
                    feet = new[]
                    {
                        left.GetComponent<Renderer>(),
                        right.GetComponent<Renderer>()
                    };
                }
            }

            if (feet == null)
                skins = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (feet == null && (skins == null || skins.Length == 0))
                feet = visual.GetComponentsInChildren<Renderer>(true);
            nextCheck = Time.time;
        }

        public static float RequiredLift(float footWorldY, float groundWorldY)
        {
            // A small positive margin prevents Z-fighting. Never pull the
            // character down through a platform, or move the root NavMesh.
            return Mathf.Clamp(groundWorldY + .025f - footWorldY, 0f, 3f);
        }

        void LateUpdate()
        {
            if (visual == null || zombie == null || zombie.Dead ||
                Time.time < nextCheck)
                return;

            nextCheck = Time.time + .10f;
            Vector3 position = visual.position;

            // Prefer the ACTUAL streamed Terrain; the analytic world height
            // is only a fallback while tiles stream in/out.
            Terrain terrain = cachedTerrain;
            if (terrain == null || !terrain.isActiveAndEnabled ||
                terrain.terrainData == null ||
                !SectorPlayer.IsInsideTerrainXZ(
                    terrain.transform.position, terrain.terrainData.size,
                    position))
            {
                cachedTerrain = null;
                foreach (Terrain candidate in Terrain.activeTerrains)
                {
                    if (candidate == null || !candidate.isActiveAndEnabled ||
                        candidate.terrainData == null ||
                        !SectorPlayer.IsInsideTerrainXZ(
                            candidate.transform.position,
                            candidate.terrainData.size, position))
                        continue;
                    cachedTerrain = candidate;
                    break;
                }
                terrain = cachedTerrain;
            }

            float ground = terrain != null
                ? terrain.SampleHeight(position) + terrain.transform.position.y
                : SectorLayout.Height(position.x, position.z);

            // Sample the unadjusted visual every check to avoid accumulating
            // a permanent lift while moving downhill between streamed tiles.
            Vector3 local = visual.localPosition;
            visual.localPosition = new Vector3(
                local.x, originalLocalY, local.z);

            float lowest = float.PositiveInfinity;
            if (feet != null)
            {
                foreach (Renderer renderer in feet)
                    if (renderer != null && renderer.enabled)
                        lowest = Mathf.Min(lowest, renderer.bounds.min.y);
            }
            else if (skins != null)
            {
                foreach (SkinnedMeshRenderer skin in skins)
                    if (skin != null && skin.enabled)
                        lowest = Mathf.Min(lowest, skin.bounds.min.y);
            }

            if (float.IsPositiveInfinity(lowest))
                return;

            float lift = RequiredLift(lowest, ground);
            if (lift > .005f)
            {
                Vector3 adjusted = visual.localPosition;
                adjusted.y += lift;
                visual.localPosition = adjusted;
            }
        }
    }
}
