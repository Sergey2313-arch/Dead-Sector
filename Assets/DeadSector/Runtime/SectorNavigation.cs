using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace DeadSector
{
    public sealed class SectorNavigation : MonoBehaviour
    {
        public SectorWorld world;
        public SectorPlayer player;

        public bool Ready { get; private set; }
        public int ZombieCount { get; private set; }
        public string Status { get; private set; } = "Waiting for terrain";

        NavMeshData data;
        NavMeshDataInstance instance;
        GameObject zombieVisualPrefab;
        NavMeshBuildSettings buildSettings;
        Vector3 navigationCenter;
        bool rebuilding;
        float nextRebuild;

        IEnumerator Start()
        {
            while (!world.Ready)
                yield return null;

            Status = "Building paths...";

            var sources = new List<NavMeshBuildSource>();
            var bounds = new Bounds(
                new Vector3(0, 100, 0),
                new Vector3(900, 250, 900));

            NavMeshBuilder.CollectSources(
                bounds,
                ~(1 << 2),
                NavMeshCollectGeometry.PhysicsColliders,
                0,
                new List<NavMeshBuildMarkup>(),
                sources);

            if (NavMesh.GetSettingsCount() == 0)
            {
                Status = "No NavMesh agent settings";
                yield break;
            }

            var settings = NavMesh.GetSettingsByIndex(0);
            settings.overrideVoxelSize = true;
            settings.voxelSize = .3f;
            settings.overrideTileSize = true;
            settings.tileSize = 128;

            buildSettings = settings;
            navigationCenter = Vector3.zero;
            data = new NavMeshData(settings.agentTypeID);

            yield return NavMeshBuilder.UpdateNavMeshDataAsync(
                data,
                settings,
                sources,
                bounds);

            instance = NavMesh.AddNavMeshData(data);

            zombieVisualPrefab =
                Resources.Load<GameObject>("DeadSector/Zombie_XBot");

            if (zombieVisualPrefab == null)
            {
                Debug.LogWarning(
                    "[Dead Sector] Full-body zombie prefab is missing. " +
                    "Run Dead Sector > Character > Zombies > 00 - BUILD ZOMBIES. " +
                    "Procedural mannequin fallback will be used.");
            }

            SpawnGroup(settings.agentTypeID);

            Ready = true;
            Status = zombieVisualPrefab != null
                ? "Ready / Full-body zombies"
                : "Ready / Prototype zombies";
        }

        void Update()
        {
            if (!Ready || rebuilding || world == null || player == null ||
                !world.Ready || world.Status != "Ready" ||
                Time.time < nextRebuild)
                return;

            Vector2 moved = new Vector2(
                player.transform.position.x - navigationCenter.x,
                player.transform.position.z - navigationCenter.z);

            if (moved.sqrMagnitude > 330f * 330f)
                StartCoroutine(RebuildNearPlayer());
        }

        IEnumerator RebuildNearPlayer()
        {
            rebuilding = true;
            nextRebuild = Time.time + 8f;
            Status = "Refreshing local zombie navigation";

            Vector3 center = player.transform.position;
            var bounds = new Bounds(
                new Vector3(center.x, 245f, center.z),
                new Vector3(850f, 600f, 850f));
            var sources = new List<NavMeshBuildSource>();

            NavMeshBuilder.CollectSources(
                bounds, ~(1 << 2),
                NavMeshCollectGeometry.PhysicsColliders,
                0, new List<NavMeshBuildMarkup>(), sources);

            yield return NavMeshBuilder.UpdateNavMeshDataAsync(
                data, buildSettings, sources, bounds);

            navigationCenter = center;
            rebuilding = false;
            Status = "Ready / streamed local NavMesh";

            // Old region AI must not fill the global entity cap after
            // travelling kilometres through streamed terrain.
            foreach (SectorZombie zombie in
                FindObjectsByType<SectorZombie>(FindObjectsSortMode.None))
            {
                if (zombie == null)
                    continue;

                Vector3 diff = zombie.transform.position - center;
                diff.y = 0f;

                if (diff.sqrMagnitude > 540f * 540f)
                {
                    Destroy(zombie.gameObject);
                    ZombieCount = Mathf.Max(0, ZombieCount - 1);
                }
            }
        }

        /// <summary>
        /// Create a capped wave in a ring outside close melee distance.
        /// Returns actual spawned count; callers must not record success
        /// if missing NavMesh or missing loaded terrain prevented spawns.
        /// </summary>
        public int SpawnHordeZombies(int requested, float minimumDistance = 48f, int day = 1)
        {
            if (!Ready || rebuilding || player == null ||
                player.Health <= 0f || requested <= 0)
                return 0;

            int living = 0;
            foreach (SectorZombie zombie in
                FindObjectsByType<SectorZombie>(FindObjectsSortMode.None))
                if (zombie != null && !zombie.Dead)
                    living++;

            int allowance = Mathf.Min(requested, Mathf.Max(0, 45 - living));
            int made = 0;

            for (int i = 0; i < allowance; i++)
            {
                bool found = false;
                NavMeshHit hit = default;

                for (int attempt = 0; attempt < 16; attempt++)
                {
                    float angle = Random.Range(0f, Mathf.PI * 2f);
                    float radius = Random.Range(minimumDistance, 105f);
                    Vector3 location = player.transform.position +
                        new Vector3(Mathf.Cos(angle) * radius, 0f,
                            Mathf.Sin(angle) * radius);

                    if (NavMesh.SamplePosition(location, out hit, 13f,
                        NavMesh.AllAreas) &&
                        Vector3.Distance(hit.position, player.transform.position) > 35f)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    continue;

                GameObject root = new GameObject(
                    "Horde_Zombie_" + i);
                root.transform.SetParent(transform, true);
                root.transform.position = hit.position;
                SectorArt.SetActorLayer(root.transform);

                CapsuleCollider collider =
                    root.AddComponent<CapsuleCollider>();
                collider.height = 1.8f;
                collider.radius = .30f;
                collider.center = Vector3.up * .9f;

                NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
                agent.agentTypeID = buildSettings.agentTypeID;
                agent.height = 1.8f;
                agent.radius = .30f;
                agent.speed = 3.5f;
                agent.acceleration = 12f;
                agent.angularSpeed = 520f;
                agent.stoppingDistance = 1.3f;

                SectorMannequin rig = null;
                Animator animator = null;

                if (zombieVisualPrefab != null)
                {
                    GameObject visual =
                        Instantiate(zombieVisualPrefab, root.transform);
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = Quaternion.identity;
                    SectorArt.SetActorLayer(visual.transform);
                    animator = visual.GetComponentInChildren<Animator>(true);
                }
                else
                {
                    rig = world.Art.Person(root.transform,
                        new Color(.29f, .25f, .23f));
                }

                SectorZombie ai = root.AddComponent<SectorZombie>();
                ai.target = player;
                ai.rig = rig;
                ai.animator = animator;
                ai.homeRadius = 4f;
                ai.chaseSpeed = 3.3f + Random.Range(0f, .6f);
                ai.sightRange = 70f;
                ai.hearingRange = 70f;
                ai.ConfigureArchetype(
                    SectorZombieProfiles.ForHordeIndex(day, i),
                    animator != null ? animator.transform :
                    rig != null ? rig.transform : null);
                ai.AssignHorde();

                made++;
                ZombieCount++;
            }

            return made;
        }

        void SpawnGroup(int agentTypeId)
        {
            Vector3[] spawnPoints =
            {
                new Vector3(-245, 60, 38),
                new Vector3(-205, 60, -42),
                new Vector3(-165, 60, 30),
                new Vector3(-125, 60, -38),
                new Vector3(-80, 60, 42),
                new Vector3(-35, 60, -36),
                new Vector3(35, 60, 44),
                new Vector3(82, 60, -42),
                new Vector3(150, 60, 42),
                new Vector3(190, 60, -36),
                new Vector3(235, 60, 65),
                new Vector3(255, 60, 210),
                new Vector3(205, 60, 285),
                new Vector3(310, 60, -225),
                new Vector3(-285, 60, 95),
                new Vector3(-300, 60, -95)
            };

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (!NavMesh.SamplePosition(
                    spawnPoints[i],
                    out NavMeshHit hit,
                    12,
                    NavMesh.AllAreas))
                {
                    continue;
                }

                GameObject root = new GameObject("Zombie_" + i);
                root.transform.SetParent(transform);
                root.transform.position = hit.position;

                SectorArt.SetActorLayer(root.transform);

                CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
                collider.height = 1.8f;
                collider.radius = .30f;
                collider.center = Vector3.up * .9f;

                NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
                agent.agentTypeID = agentTypeId;
                agent.height = 1.8f;
                agent.radius = .30f;
                agent.stoppingDistance = 1.35f;
                agent.acceleration = 10f;
                agent.angularSpeed = 480f;
                agent.autoBraking = true;
                agent.obstacleAvoidanceType =
                    ObstacleAvoidanceType.HighQualityObstacleAvoidance;

                SectorMannequin rig = null;
                Animator animator = null;

                if (zombieVisualPrefab != null)
                {
                    GameObject visual = Instantiate(
                        zombieVisualPrefab,
                        root.transform);

                    visual.name = "FullBody";
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = Quaternion.identity;

                    animator = visual.GetComponentInChildren<Animator>(true);
                    SectorArt.SetActorLayer(visual.transform);
                }
                else
                {
                    rig = world.Art.Person(
                        root.transform,
                        new Color(.30f, .25f, .20f));
                }

                SectorZombie ai = root.AddComponent<SectorZombie>();
                ai.target = player;
                ai.rig = rig;
                ai.animator = animator;
                ai.homeRadius = 20f + (i % 4) * 4f;
                ai.chaseSpeed = 2.7f + (i % 3) * .25f;
                ai.wanderSpeed = .85f + (i % 2) * .18f;
                ai.hearingRange = 18f + (i % 4) * 2f;
                ai.sightRange = 34f + (i % 3) * 3f;
                ai.ConfigureArchetype(
                    SectorZombieProfiles.ForAmbientIndex(i),
                    animator != null ? animator.transform :
                    rig != null ? rig.transform : null);

                ZombieCount++;
            }
        }

        void OnDestroy()
        {
            if (instance.valid)
                instance.Remove();

            if (data != null)
            {
                NavMeshBuilder.Cancel(data);
                Destroy(data);
            }
        }
    }
}
