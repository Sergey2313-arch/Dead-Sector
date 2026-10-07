using UnityEngine;
using UnityEngine.AI;

namespace DeadSector
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class SectorZombie : MonoBehaviour
    {
        public SectorPlayer target;
        public SectorMannequin rig;
        public string State { get; private set; } = "Idle";
        NavMeshAgent agent;
        Vector3 home, lastSeen;
        float nextThink, nextAttack, seenAt = -100f;
        void Awake() { agent = GetComponent<NavMeshAgent>(); home = transform.position; }
        void Update()
        {
            if (target == null || !agent.isOnNavMesh) return;
            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance > 550 || target.Health <= 0)
            {
                agent.isStopped = true; State = "Idle"; rig.SetMotion(0, true, 0); return;
            }
            agent.isStopped = false;
            if (Time.time >= nextThink)
            {
                nextThink = Time.time + .3f;
                Vector3 eye = transform.position + Vector3.up * 1.4f;
                Vector3 toPlayer = target.transform.position + Vector3.up * 1.2f - eye;
                bool sees = distance < 45 && !Physics.Raycast(eye, toPlayer.normalized, toPlayer.magnitude, ~(1 << 2), QueryTriggerInteraction.Ignore);
                if (sees) { seenAt = Time.time; lastSeen = target.transform.position; }
                if (Time.time - seenAt < 6 && NavMesh.SamplePosition(lastSeen, out var chase, 3, NavMesh.AllAreas))
                {
                    State = distance < 1.8f ? "Attack" : "Chase"; agent.speed = 3.2f;
                    agent.SetDestination(chase.position);
                }
                else if (!agent.pathPending && (!agent.hasPath || agent.remainingDistance < 1))
                {
                    State = "Wander"; agent.speed = 1.1f;
                    Vector2 offset = Random.insideUnitCircle * 18;
                    if (NavMesh.SamplePosition(home + new Vector3(offset.x, 0, offset.y), out var wander, 6, NavMesh.AllAreas)) agent.SetDestination(wander.position);
                }
                if (sees && distance < 1.8f && Time.time >= nextAttack)
                {
                    nextAttack = Time.time + 1.2f; target.Damage(10);
                }
            }
            rig.SetMotion(agent.velocity.magnitude, true, 0);
        }
    }
}
