using System.Collections.Generic;
using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// Runtime Humanoid ragdoll for killed infected (Mixamo X Bot).
    /// Physics components are constructed ONLY when a zombie dies:
    /// living AI keeps its lightweight Animator/NavMeshAgent movement.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SectorZombieRagdoll : MonoBehaviour
    {
        [Header("Death physics")]
        [Min(1f)] public float impulse = 16f;
        [Min(.1f)] public float inheritedVelocity = .65f;
        [Min(1f)] public float bodyMass = 55f;
        [Min(1f)] public float freezeAfterSeconds = 8f;

        Animator animator;
        Transform visual;
        SectorMannequin mannequin;
        float activatedAt;
        bool frozen;

        readonly List<Rigidbody> bodies = new List<Rigidbody>(12);
        readonly List<Collider> colliders = new List<Collider>(12);

        Rigidbody chestBody;
        Rigidbody hipsBody;
        bool activated;

        public bool IsActive => activated;
        public int ActiveBodyCount => bodies.Count;

        struct BonePart
        {
            public HumanBodyBones Bone;
            public HumanBodyBones End;
            public HumanBodyBones Parent;
            public float Weight;
            public float Radius;
            public bool IsHead;

            public BonePart(
                HumanBodyBones bone, HumanBodyBones end,
                HumanBodyBones parent, float weight, float radius,
                bool isHead = false)
            {
                Bone = bone;
                End = end;
                Parent = parent;
                Weight = weight;
                Radius = radius;
                IsHead = isHead;
            }
        }

        // The parent in each row is an attached Rigidbody bone, not
        // necessarily the direct Transform parent of the humanoid bone.
        static readonly BonePart[] Parts =
        {
            new BonePart(HumanBodyBones.Hips, HumanBodyBones.Spine,
                HumanBodyBones.LastBone, .19f, .32f),
            new BonePart(HumanBodyBones.Chest, HumanBodyBones.Neck,
                HumanBodyBones.Hips, .30f, .31f),
            new BonePart(HumanBodyBones.Head, HumanBodyBones.Neck,
                HumanBodyBones.Chest, .07f, .23f, true),
            new BonePart(HumanBodyBones.LeftUpperArm,
                HumanBodyBones.LeftLowerArm,
                HumanBodyBones.Chest, .045f, .21f),
            new BonePart(HumanBodyBones.LeftLowerArm,
                HumanBodyBones.LeftHand,
                HumanBodyBones.LeftUpperArm, .027f, .20f),
            new BonePart(HumanBodyBones.RightUpperArm,
                HumanBodyBones.RightLowerArm,
                HumanBodyBones.Chest, .045f, .21f),
            new BonePart(HumanBodyBones.RightLowerArm,
                HumanBodyBones.RightHand,
                HumanBodyBones.RightUpperArm, .027f, .20f),
            new BonePart(HumanBodyBones.LeftUpperLeg,
                HumanBodyBones.LeftLowerLeg,
                HumanBodyBones.Hips, .10f, .23f),
            new BonePart(HumanBodyBones.LeftLowerLeg,
                HumanBodyBones.LeftFoot,
                HumanBodyBones.LeftUpperLeg, .055f, .19f),
            new BonePart(HumanBodyBones.RightUpperLeg,
                HumanBodyBones.RightLowerLeg,
                HumanBodyBones.Hips, .10f, .23f),
            new BonePart(HumanBodyBones.RightLowerLeg,
                HumanBodyBones.RightFoot,
                HumanBodyBones.RightUpperLeg, .055f, .19f)
        };

        public void Configure(
            Animator humanoid, Transform visualRoot,
            SectorMannequin fallback = null)
        {
            animator = humanoid;
            visual = visualRoot;
            mannequin = fallback;
        }

        public bool CanActivate
        {
            get
            {
                if (activated || visual == null)
                    return false;

                if (animator == null || !animator.isHuman ||
                    animator.avatar == null || !animator.avatar.isValid ||
                    !animator.avatar.isHuman)
                {
                    return mannequin != null &&
                        mannequin.torso != null &&
                        mannequin.leftArm != null &&
                        mannequin.rightArm != null &&
                        mannequin.leftLeg != null &&
                        mannequin.rightLeg != null;
                }

                // Require the complete minimum Humanoid rig before creating
                // ANY physics objects, so broken imports use fallback.
                for (int i = 0; i < Parts.Length; i++)
                {
                    BonePart part = Parts[i];
                    if (animator.GetBoneTransform(part.Bone) == null ||
                        animator.GetBoneTransform(part.End) == null)
                        return false;
                }

                return true;
            }
        }

        public bool TryActivate(
            Vector3 hitDirection, Vector3 hitPosition,
            Vector3 startingVelocity)
        {
            if (!CanActivate)
                return false;

            if (animator == null || !animator.isHuman ||
                animator.avatar == null || !animator.avatar.isValid)
            {
                return TryActivateMannequin(
                    hitDirection, hitPosition, startingVelocity);
            }

            Dictionary<HumanBodyBones, Rigidbody> map =
                new Dictionary<HumanBodyBones, Rigidbody>();

            // Animator and visual recoil have already been disabled by
            // SectorZombie. Capture the final pose, then make bone bodies.
            for (int i = 0; i < Parts.Length; i++)
            {
                BonePart part = Parts[i];
                Transform bone = animator.GetBoneTransform(part.Bone);
                Transform end = animator.GetBoneTransform(part.End);

                // A bad imported skeleton must never produce NaN forces.
                Vector3 deltaWorld = end.position - bone.position;
                if (part.IsHead)
                    deltaWorld = bone.up * .22f;

                if (deltaWorld.sqrMagnitude < .0004f)
                    deltaWorld = bone.up * .15f;

                Rigidbody body = bone.GetComponent<Rigidbody>();
                if (body == null)
                    body = bone.gameObject.AddComponent<Rigidbody>();

                body.mass = Mathf.Max(.2f, bodyMass * part.Weight);
                body.linearDamping = .12f;
                body.angularDamping = .55f;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.Discrete;
                body.useGravity = true;
                body.isKinematic = true;

                GameObject shape = new GameObject(
                    "Ragdoll_Physics_" + part.Bone);
                shape.transform.SetParent(bone, false);

                if (part.IsHead)
                {
                    shape.transform.position =
                        bone.position + deltaWorld * .18f;
                    SphereCollider head = shape.AddComponent<SphereCollider>();
                    float scale = Mathf.Max(.0001f,
                        Mathf.Max(Mathf.Abs(shape.transform.lossyScale.x),
                            Mathf.Abs(shape.transform.lossyScale.y)));
                    head.radius = .105f / scale;
                    colliders.Add(head);
                }
                else
                {
                    Vector3 localEnd = bone.InverseTransformPoint(
                        bone.position + deltaWorld);
                    float length = Mathf.Max(localEnd.magnitude, .001f);

                    shape.transform.localPosition = localEnd * .5f;
                    shape.transform.localRotation = Quaternion.FromToRotation(
                        Vector3.up, localEnd.normalized);

                    CapsuleCollider capsule =
                        shape.AddComponent<CapsuleCollider>();
                    capsule.direction = 1;
                    capsule.radius = Mathf.Max(
                        length * part.Radius, .015f);
                    capsule.height = Mathf.Max(
                        length * .91f, capsule.radius * 2.15f);
                    colliders.Add(capsule);
                }

                SectorArt.SetActorLayer(shape.transform, SectorArt.EnemyVisualLayer);

                bodies.Add(body);
                map.Add(part.Bone, body);

                if (part.Bone == HumanBodyBones.Chest)
                    chestBody = body;
                if (part.Bone == HumanBodyBones.Hips)
                    hipsBody = body;
            }

            // Connect physical bones only after all the Rigidbody objects
            // exist. Assign anchors from the posed world-space joints.
            for (int i = 0; i < Parts.Length; i++)
            {
                BonePart part = Parts[i];
                if (part.Parent == HumanBodyBones.LastBone)
                    continue;

                Transform bone = animator.GetBoneTransform(part.Bone);
                Rigidbody current = map[part.Bone];
                Rigidbody parent = map[part.Parent];

                CharacterJoint joint = bone.gameObject.AddComponent<CharacterJoint>();
                joint.connectedBody = parent;
                joint.autoConfigureConnectedAnchor = false;

                Vector3 pivot = part.IsHead
                    ? animator.GetBoneTransform(HumanBodyBones.Neck).position
                    : bone.position;

                joint.anchor = bone.InverseTransformPoint(pivot);
                joint.connectedAnchor =
                    parent.transform.InverseTransformPoint(pivot);
                joint.enableCollision = false;
                joint.enablePreprocessing = false;
                joint.breakForce = Mathf.Infinity;
                joint.breakTorque = Mathf.Infinity;

                joint.lowTwistLimit =
                    new SoftJointLimit { limit = -28f };
                joint.highTwistLimit =
                    new SoftJointLimit { limit = 28f };
                joint.swing1Limit =
                    new SoftJointLimit { limit = 40f };
                joint.swing2Limit =
                    new SoftJointLimit { limit = 40f };
            }

            // Self-collisions during unfreezing can throw the whole corpse
            // across the map; keep world/terrain collisions instead.
            for (int i = 0; i < colliders.Count; i++)
                for (int j = i + 1; j < colliders.Count; j++)
                    Physics.IgnoreCollision(colliders[i], colliders[j], true);

            WakePhysics(hitDirection, hitPosition, startingVelocity);
            return true;
        }

        bool TryActivateMannequin(
            Vector3 direction, Vector3 hitPoint, Vector3 velocity)
        {
            if (mannequin == null)
                return false;

            // The procedural mannequin has simple hinged limbs rather than
            // a Mecanim Avatar. Physics bodies on those visible Transform
            // joints allow natural collapse without an imported FBX.
            Rigidbody torso = CreateFallbackBody(
                mannequin.torso, 22f,
                new Vector3(0f, .22f, 0f),
                .88f, .26f);
            chestBody = torso;
            hipsBody = torso;

            Transform headBone = mannequin.torso.Find("Head");
            if (headBone != null)
            {
                Rigidbody head = headBone.gameObject.AddComponent<Rigidbody>();
                PrepareBody(head, 4f);

                SphereCollider sphere = headBone.GetComponent<SphereCollider>();
                if (sphere == null)
                    sphere = headBone.gameObject.AddComponent<SphereCollider>();
                sphere.radius = .5f;
                colliders.Add(sphere);
                bodies.Add(head);
                Connect(head, torso, headBone.position, 35f);
            }

            Rigidbody leftArm = CreateFallbackBody(
                mannequin.leftArm, 4f,
                new Vector3(0f, -.28f, 0f), .65f, .14f);
            Rigidbody rightArm = CreateFallbackBody(
                mannequin.rightArm, 4f,
                new Vector3(0f, -.28f, 0f), .65f, .14f);
            Rigidbody leftLeg = CreateFallbackBody(
                mannequin.leftLeg, 11f,
                new Vector3(0f, -.43f, 0f), .87f, .18f);
            Rigidbody rightLeg = CreateFallbackBody(
                mannequin.rightLeg, 11f,
                new Vector3(0f, -.43f, 0f), .87f, .18f);

            Connect(leftArm, torso, mannequin.leftArm.position, 50f);
            Connect(rightArm, torso, mannequin.rightArm.position, 50f);
            Connect(leftLeg, torso, mannequin.leftLeg.position, 34f);
            Connect(rightLeg, torso, mannequin.rightLeg.position, 34f);

            IgnoreSelfCollisions();
            WakePhysics(direction, hitPoint, velocity);
            return true;
        }

        Rigidbody CreateFallbackBody(
            Transform joint, float mass,
            Vector3 center, float height, float radius)
        {
            Rigidbody body = joint.GetComponent<Rigidbody>();
            if (body == null)
                body = joint.gameObject.AddComponent<Rigidbody>();

            PrepareBody(body, mass);
            CapsuleCollider capsule =
                joint.gameObject.AddComponent<CapsuleCollider>();
            capsule.center = center;
            capsule.direction = 1;
            capsule.height = height;
            capsule.radius = radius;

            bodies.Add(body);
            colliders.Add(capsule);
            SectorArt.SetActorLayer(joint, SectorArt.EnemyVisualLayer);
            return body;
        }

        void PrepareBody(Rigidbody body, float mass)
        {
            body.mass = Mathf.Max(.2f, mass);
            body.isKinematic = true;
            body.useGravity = true;
            body.linearDamping = .12f;
            body.angularDamping = .55f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode =
                CollisionDetectionMode.Discrete;
        }

        static void Connect(
            Rigidbody child, Rigidbody parent,
            Vector3 worldAnchor, float swingLimit)
        {
            CharacterJoint joint =
                child.gameObject.AddComponent<CharacterJoint>();
            joint.connectedBody = parent;
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor =
                child.transform.InverseTransformPoint(worldAnchor);
            joint.connectedAnchor =
                parent.transform.InverseTransformPoint(worldAnchor);
            joint.enableCollision = false;
            joint.enablePreprocessing = false;

            joint.lowTwistLimit =
                new SoftJointLimit { limit = -28f };
            joint.highTwistLimit =
                new SoftJointLimit { limit = 28f };
            joint.swing1Limit =
                new SoftJointLimit { limit = swingLimit };
            joint.swing2Limit =
                new SoftJointLimit { limit = swingLimit };
        }

        void IgnoreSelfCollisions()
        {
            for (int i = 0; i < colliders.Count; i++)
                for (int j = i + 1; j < colliders.Count; j++)
                    Physics.IgnoreCollision(
                        colliders[i], colliders[j], true);
        }

        void WakePhysics(
            Vector3 hitDirection,
            Vector3 hitPosition,
            Vector3 startingVelocity)
        {
            activated = true;
            activatedAt = Time.time;

            Vector3 velocity =
                Vector3.ClampMagnitude(startingVelocity, 7f) *
                Mathf.Max(0f, inheritedVelocity);

            foreach (Rigidbody body in bodies)
            {
                body.isKinematic = false;
                body.linearVelocity = velocity;
            }

            Vector3 direction = hitDirection.sqrMagnitude > .001f
                ? hitDirection.normalized
                : transform.forward;

            Rigidbody receiver = chestBody != null
                ? chestBody : hipsBody;

            if (receiver == null)
                return;

            Vector3 point =
                (hitPosition - receiver.worldCenterOfMass)
                    .sqrMagnitude < 1.25f
                    ? hitPosition : receiver.worldCenterOfMass;

            Vector3 force =
                (direction + Vector3.up * .20f).normalized *
                Mathf.Max(0f, impulse);

            receiver.AddForceAtPosition(
                force, point, ForceMode.Impulse);
        }

        void Update()
        {
            // Corpses are deleted by SectorZombie after 25 s. Stop their
            // simulation after settling to keep big hordes performant.
            if (!activated || frozen ||
                Time.time - activatedAt < freezeAfterSeconds)
                return;

            frozen = true;
            foreach (Rigidbody body in bodies)
            {
                if (body != null)
                    body.isKinematic = true;
            }
        }
    }
}
