using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace DeadSector
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class SectorZombie : MonoBehaviour
    {
        public SectorPlayer target;
        public SectorMannequin rig;
        public Animator animator;

        [Header("Senses")]
        public float sightRange = 38f;
        public float hearingRange = 22f;
        public float memorySeconds = 7f;
        public float attackRange = 1.65f;

        [Header("Movement")]
        public float wanderSpeed = 1.0f;
        public float chaseSpeed = 3.0f;
        public float homeRadius = 24f;

        public string State { get; private set; } = "Idle";

        NavMeshAgent agent;
        Vector3 home;
        Vector3 lastKnownPosition;
        float lastContactAt = -100f;
        float nextThink;
        float nextWander;
        float nextAttack;
        float pendingDamageAt = -1f;
        bool alerted;
        readonly HashSet<int> parameters = new HashSet<int>();

        static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
        static readonly int AggroHash = Animator.StringToHash("Aggro");
        static readonly int AttackHash = Animator.StringToHash("Attack");
        static readonly int AttackVariantHash = Animator.StringToHash("AttackVariant");
        static readonly int ScreamHash = Animator.StringToHash("Scream");
        static readonly int CrawlHash = Animator.StringToHash("Crawl");

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            home = transform.position;
            CacheAnimatorParameters();
        }

        void Update()
        {
            if (target == null || !agent.isOnNavMesh)
            {
                SetMotion(0f, false);
                return;
            }

            if (pendingDamageAt > 0f && Time.time >= pendingDamageAt)
            {
                pendingDamageAt = -1f;

                if (target.Health > 0 &&
                    Vector3.Distance(transform.position, target.transform.position) <= attackRange + .35f)
                {
                    target.Damage(10f);
                }
            }

            if (target.Health <= 0)
            {
                agent.isStopped = true;
                State = "Idle";
                SetMotion(0f, false);
                return;
            }

            float distance =
                Vector3.Distance(transform.position, target.transform.position);

            if (distance > 600f)
            {
                agent.isStopped = true;
                State = "Dormant";
                SetMotion(0f, false);
                return;
            }

            if (Time.time >= nextThink)
            {
                nextThink = Time.time + .18f + Random.value * .10f;
                Think(distance);
            }

            if (State == "Attack")
            {
                FaceTarget();

                if (Time.time >= nextAttack)
                    BeginAttack(distance);
            }

            SetMotion(agent.velocity.magnitude, alerted);
        }

        void Think(float distance)
        {
            bool sees = CanSeePlayer(distance);
            bool hears = CanHearPlayer(distance);

            if (sees || hears)
            {
                lastContactAt = Time.time;
                lastKnownPosition = target.transform.position;

                if (!alerted)
                {
                    alerted = true;
                    TriggerIfExists(ScreamHash);
                }
            }

            bool remembers =
                Time.time - lastContactAt <= memorySeconds;

            if (sees && distance <= attackRange)
            {
                State = "Attack";
                agent.isStopped = true;
                agent.ResetPath();
                return;
            }

            if (remembers)
            {
                State = sees ? "Chase" : "Investigate";
                agent.isStopped = false;
                agent.speed = chaseSpeed;

                if (NavMesh.SamplePosition(
                    lastKnownPosition,
                    out NavMeshHit chase,
                    4f,
                    NavMesh.AllAreas))
                {
                    agent.SetDestination(chase.position);
                }

                return;
            }

            alerted = false;

            if (Time.time < nextWander &&
                agent.hasPath &&
                agent.remainingDistance > .8f)
            {
                State = "Wander";
                agent.isStopped = false;
                agent.speed = wanderSpeed;
                return;
            }

            if (Time.time >= nextWander)
            {
                nextWander = Time.time + Random.Range(2.5f, 5.5f);

                Vector2 circle =
                    Random.insideUnitCircle * homeRadius;

                Vector3 desired =
                    home + new Vector3(circle.x, 0f, circle.y);

                if (NavMesh.SamplePosition(
                    desired,
                    out NavMeshHit wander,
                    7f,
                    NavMesh.AllAreas))
                {
                    agent.isStopped = false;
                    agent.speed = wanderSpeed;
                    agent.SetDestination(wander.position);
                    State = "Wander";
                    return;
                }
            }

            agent.isStopped = true;
            State = "Idle";
        }

        bool CanSeePlayer(float distance)
        {
            if (distance > sightRange)
                return false;

            Vector3 eye = transform.position + Vector3.up * 1.45f;
            Vector3 targetPoint =
                target.transform.position + Vector3.up * 1.2f;

            Vector3 toTarget = targetPoint - eye;
            Vector3 planar = toTarget;
            planar.y = 0f;

            if (planar.sqrMagnitude > .01f)
            {
                float facing =
                    Vector3.Dot(
                        transform.forward,
                        planar.normalized);

                if (facing < -.15f && distance > 7f)
                    return false;
            }

            // Player is on layer 2, so ignore actor layer and only test whether
            // walls/terrain block the line of sight.
            return !Physics.Raycast(
                eye,
                toTarget.normalized,
                toTarget.magnitude,
                ~(1 << 2),
                QueryTriggerInteraction.Ignore);
        }

        bool CanHearPlayer(float distance)
        {
            if (distance > hearingRange)
                return false;

            float noise = target.Speed;

            if (noise < .25f)
                return false;

            float effectiveRange =
                hearingRange * Mathf.Lerp(.45f, 1.2f, Mathf.Clamp01(noise / 7f));

            return distance <= effectiveRange;
        }

        void BeginAttack(float distance)
        {
            if (distance > attackRange + .25f)
            {
                State = "Chase";
                agent.isStopped = false;
                return;
            }

            nextAttack = Time.time + Random.Range(1.05f, 1.35f);
            pendingDamageAt = Time.time + .32f;

            int variant = Random.Range(0, 3);
            SetIntIfExists(AttackVariantHash, variant);
            TriggerIfExists(AttackHash);
        }

        void FaceTarget()
        {
            Vector3 direction =
                target.transform.position - transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude < .01f)
                return;

            Quaternion wanted =
                Quaternion.LookRotation(direction.normalized);

            transform.rotation =
                Quaternion.RotateTowards(
                    transform.rotation,
                    wanted,
                    520f * Time.deltaTime);
        }

        void SetMotion(float velocity, bool aggro)
        {
            if (rig != null)
                rig.SetMotion(velocity, true, 0f);

            if (animator == null)
                return;

            SetFloatIfExists(MoveSpeedHash, velocity);
            SetBoolIfExists(AggroHash, aggro);
            SetBoolIfExists(CrawlHash, false);
        }

        void CacheAnimatorParameters()
        {
            parameters.Clear();

            if (animator == null)
                return;

            foreach (AnimatorControllerParameter p in animator.parameters)
                parameters.Add(p.nameHash);
        }

        bool Has(int hash)
        {
            return animator != null && parameters.Contains(hash);
        }

        void SetFloatIfExists(int hash, float value)
        {
            if (Has(hash))
                animator.SetFloat(hash, value, .12f, Time.deltaTime);
        }

        void SetBoolIfExists(int hash, bool value)
        {
            if (Has(hash))
                animator.SetBool(hash, value);
        }

        void SetIntIfExists(int hash, int value)
        {
            if (Has(hash))
                animator.SetInteger(hash, value);
        }

        void TriggerIfExists(int hash)
        {
            if (Has(hash))
                animator.SetTrigger(hash);
        }
    }
}
