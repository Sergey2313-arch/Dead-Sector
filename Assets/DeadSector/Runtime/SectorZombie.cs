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
        public float Health { get; private set; } = 100f;
        public bool Dead { get; private set; } = false;
        public bool IsHorde { get; private set; } = false;
        public SectorZombieKind Kind { get; private set; } = SectorZombieKind.Shambler;
        public float AttackDamage { get; private set; } = 10f;
        public float AttackCooldown { get; private set; } = 1.2f;
        SectorZombieAppearance appearance;
        SectorZombieReaction reaction;
        SectorZombieRagdoll ragdoll;

        NavMeshAgent agent;
        Vector3 home;
        Vector3 lastKnownPosition;
        float lastContactAt = -100f;
        float nextThink;
        float nextWander;
        float nextAttack;
        float pendingDamageAt = -1f;
        float nextStructureHit;
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
        }

        void Start()
        {
            CacheAnimatorParameters();
        }

        void Update()
        {
            if (Dead) return;

            if (target == null || !agent.isOnNavMesh)
            {
                SetMotion(0f, false);
                return;
            }

            if (pendingDamageAt > 0f && Time.time >= pendingDamageAt)
            {
                pendingDamageAt = -1f;

                if (target.Health > 0 &&
                    Vector3.Distance(transform.position, target.transform.position) <= attackRange + .35f &&
                    !SectorCombatVisibility.HasBlockingGeometry(
                        transform.position + Vector3.up,
                        target.transform.position + Vector3.up,
                        transform, target.transform))
                {
                    // Doors and walls prevent melee damage even when their
                    // positions happen to be within the attack radius.
                    target.Damage(AttackDamage);
                }
            }

            if (target.Health > 0 && Time.time >= nextStructureHit)
            {
                nextStructureHit = Time.time + 1.3f;
                AttackBlockingStructure();
            }

            if (target.Health <= 0)
            {
                agent.isStopped = true;
                State = "Idle";
                SetMotion(0f, false);
                return;
            }

            // Most infected only need a squared distance to check dormancy.
            // Avoid sqrt every rendered frame; resolve actual distance only
            // for their staggered AI tick or a due melee attack.
            Vector3 offset = target.transform.position - transform.position;
            float distanceSquared = offset.sqrMagnitude;

            if (distanceSquared > 600f * 600f)
            {
                agent.isStopped = true;
                State = "Dormant";
                SetMotion(0f, false);
                return;
            }

            bool thinkDue = Time.time >= nextThink;
            bool attackDue = State == "Attack" && Time.time >= nextAttack;
            float distance = 0f;
            if (thinkDue || attackDue)
                distance = Mathf.Sqrt(distanceSquared);

            if (thinkDue)
            {
                nextThink = Time.time + .18f + Random.value * .10f;
                Think(distance);
            }

            if (State == "Attack")
            {
                FaceTarget();

                if (Time.time >= nextAttack)
                {
                    // Think can enter Attack this frame, so calculate the
                    // distance here if it was not otherwise required.
                    if (!thinkDue && !attackDue)
                        distance = Mathf.Sqrt(distanceSquared);
                    BeginAttack(distance);
                }
            }

            SetMotion(agent.velocity.magnitude, alerted);
        }

        /// <summary>
        /// Infected break player-built barriers that physically block the
        /// line towards their target. No damage to arbitrary scenery.
        /// </summary>
        void AttackBlockingStructure()
        {
            if (target == null || target.Health <= 0f ||
                Vector3.Distance(transform.position,
                    target.transform.position) > 22f)
                return;

            Vector3 origin = transform.position + Vector3.up * 1.1f;
            Vector3 destination =
                target.transform.position + Vector3.up * 1.1f;
            Vector3 delta = destination - origin;
            float distance = delta.magnitude;
            if (distance < .05f)
                return;

            if (!Physics.Raycast(origin, delta / distance,
                out RaycastHit hit, distance, ~(1 << 2),
                QueryTriggerInteraction.Ignore) ||
                hit.distance > 2.5f)
                return;

            SectorBuildPiece piece =
                hit.collider.GetComponentInParent<SectorBuildPiece>();
            if (piece != null && !piece.Destroyed)
                piece.Damage(AttackDamage);
        }

        public void ConfigureArchetype(SectorZombieKind kind, Transform model)
        {
            SectorZombieProfile profile = SectorZombieProfiles.For(kind);
            Kind = profile.Kind;
            Health = profile.Health;
            wanderSpeed = profile.WalkSpeed;
            chaseSpeed = profile.ChaseSpeed;
            AttackDamage = profile.AttackDamage;
            AttackCooldown = profile.AttackCooldown;
            sightRange = profile.SightRange;
            hearingRange = profile.HearingRange;

            if (model != null)
            {
                appearance = gameObject.GetComponent<SectorZombieAppearance>();
                if (appearance == null)
                    appearance = gameObject.AddComponent<SectorZombieAppearance>();

                appearance.Configure(kind, model, profile);

                reaction = gameObject.GetComponent<SectorZombieReaction>();
                if (reaction == null)
                    reaction = gameObject.AddComponent<SectorZombieReaction>();
                reaction.Configure(model);

                // Build joints only when killed: no expensive simulated
                // bodies during ordinary wandering or night hordes.
                ragdoll = gameObject.GetComponent<SectorZombieRagdoll>();
                if (ragdoll == null)
                    ragdoll = gameObject.AddComponent<SectorZombieRagdoll>();
                ragdoll.Configure(animator, model, rig);
            }

            CapsuleCollider bodyCollider = GetComponent<CapsuleCollider>();
            if (bodyCollider != null)
            {
                bodyCollider.height = 1.8f * profile.ModelScale;
                bodyCollider.radius = .30f * profile.ModelScale;
                bodyCollider.center =
                    Vector3.up * bodyCollider.height * .5f;
            }

            if (agent != null && agent.enabled)
            {
                agent.height = 1.8f * profile.ModelScale;
                agent.radius = .30f * profile.ModelScale;
                agent.speed = wanderSpeed;
                if (agent.isOnNavMesh)
                    agent.stoppingDistance = attackRange * .82f;
            }
        }

        public void AssignHorde()
        {
            IsHorde = true;
            AlertToPlayer();
        }

        public void AlertToPlayer()
        {
            if (target == null || Dead) return;
            lastKnownPosition = target.transform.position;
            lastContactAt = Time.time;
            alerted = true;
        }

        public void HearNoise(Vector3 source, float radius)
        {
            if (Dead || radius <= 0f)
                return;

            Vector3 direction = source - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > radius * radius)
                return;

            lastKnownPosition = source;
            lastContactAt = Time.time;

            if (!alerted)
                TriggerIfExists(ScreamHash);

            alerted = true;
        }

        void Think(float distance)
        {
            // Night hordes track the player; ordinary zombies keep their
            // standard line-of-sight/hearing and memory-based behaviour.
            if (IsHorde)
                AlertToPlayer();

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

            nextAttack = Time.time + AttackCooldown + Random.Range(-.08f, .08f);
            pendingDamageAt = Time.time + .32f;

            int variant = Random.Range(0, 3);
            SetIntIfExists(AttackVariantHash, variant);
            TriggerIfExists(AttackHash);
        }

        public void TakeDamage(float damage)
        {
            TakeDamage(damage, Vector3.zero, transform.position + Vector3.up);
        }

        public void TakeDamage(
            float damage, Vector3 incomingDirection, Vector3 impactPoint)
        {
            if (Dead || damage <= 0f)
                return;

            Health = Mathf.Max(0f, Health - damage);
            if (appearance != null)
                appearance.FlashOnHit();
            if (reaction != null)
                reaction.Hit();

            if (target != null)
            {
                lastKnownPosition = target.transform.position;
                lastContactAt = Time.time;
                alerted = true;
            }

            if (Health > 0f)
                return;

            Dead = true;
            State = "Dead";
            pendingDamageAt = -1f;

            // Preserve movement from the last AI frame so the corpse
            // continues forward naturally when physics takes ownership.
            Vector3 deathVelocity =
                agent != null && agent.enabled && agent.isOnNavMesh
                    ? agent.velocity : Vector3.zero;

            if (agent != null && agent.enabled)
            {
                if (agent.isOnNavMesh)
                    agent.ResetPath();
                agent.enabled = false;
            }

            Collider collider = GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;

            if (animator != null)
                animator.enabled = false;

            // Procedural mannequin's LateUpdate must stop posing the
            // limbs before the fallback ragdoll releases its joints.
            if (rig != null)
                rig.enabled = false;

            // Dynamic ragdoll owns the model if the FBX is a valid Humanoid.
            // Fallback keeps the existing simple fall on primitive zombies.
            bool physicsDeath = ragdoll != null &&
                ragdoll.TryActivate(
                    incomingDirection, impactPoint, deathVelocity);

            if (physicsDeath)
            {
                if (reaction != null)
                    reaction.enabled = false;
            }
            else if (reaction != null)
            {
                reaction.Die();
            }

            // Corpses are retained briefly, then reclaimed for horde FPS.
            Destroy(gameObject, 25f);
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
