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
        IEnumerator Start()
        {
            while (!world.Ready) yield return null;
            Status = "Building paths...";
            var sources = new List<NavMeshBuildSource>();
            var bounds = new Bounds(new Vector3(0, 100, 0), new Vector3(900, 250, 900));
            NavMeshBuilder.CollectSources(bounds, ~(1 << 2), NavMeshCollectGeometry.PhysicsColliders, 0,
                new List<NavMeshBuildMarkup>(), sources);
            if (NavMesh.GetSettingsCount() == 0) { Status = "No NavMesh agent settings"; yield break; }
            var settings = NavMesh.GetSettingsByIndex(0);
            settings.overrideVoxelSize = true; settings.voxelSize = .3f;
            settings.overrideTileSize = true; settings.tileSize = 128;
            data = new NavMeshData(settings.agentTypeID);
            yield return NavMeshBuilder.UpdateNavMeshDataAsync(data, settings, sources, bounds);
            instance = NavMesh.AddNavMeshData(data);
            for (int i = 0; i < 12; i++)
            {
                var position = new Vector3(-230 + (i % 6) * 70, 60, i < 6 ? 40 : -40);
                if (!NavMesh.SamplePosition(position, out var hit, 8, NavMesh.AllAreas)) continue;
                var go = new GameObject("Zombie_" + i); go.transform.SetParent(transform); go.transform.position = hit.position;
                SectorArt.SetActorLayer(go.transform);
                var collider = go.AddComponent<CapsuleCollider>(); collider.height = 1.8f; collider.radius = .3f; collider.center = Vector3.up * .9f;
                var agent = go.AddComponent<NavMeshAgent>(); agent.agentTypeID = settings.agentTypeID;
                agent.height = 1.8f; agent.radius = .3f; agent.stoppingDistance = 1.4f; agent.acceleration = 8;
                var rig = world.Art.Person(go.transform, new Color(.32f, .24f, .2f));
                var ai = go.AddComponent<SectorZombie>(); ai.target = player; ai.rig = rig;
                ZombieCount++;
            }
            Ready = true; Status = "Ready";
        }
        void OnDestroy()
        {
            if (instance.valid) instance.Remove();
            if (data != null) { NavMeshBuilder.Cancel(data); Destroy(data); }
        }
    }
}
