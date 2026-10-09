using NUnit.Framework;
using UnityEngine;

namespace DeadSector.Tests
{
    public sealed class SectorZombieRagdollTests
    {
        [Test]
        public void MissingHumanoidAndFallbackDoesNotCreatePhysics()
        {
            GameObject root = new GameObject("InvalidRagdoll");
            try
            {
                var ragdoll = root.AddComponent<SectorZombieRagdoll>();
                ragdoll.Configure(null, root.transform);

                Assert.IsFalse(ragdoll.CanActivate);
                Assert.IsFalse(ragdoll.TryActivate(
                    Vector3.forward, root.transform.position, Vector3.zero));
                Assert.IsFalse(ragdoll.IsActive);
                Assert.AreEqual(0, ragdoll.ActiveBodyCount);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ProceduralZombieNeverSpawnsDetachedPhysicsLimbs()
        {
            GameObject root = new GameObject("ProceduralCorpse");

            try
            {
                GameObject model = new GameObject("Visual");
                model.transform.SetParent(root.transform, false);
                var mannequin = model.AddComponent<SectorMannequin>();

                mannequin.torso = Bone(model.transform, "Torso",
                    new Vector3(0f, 1f, 0f));
                mannequin.leftArm = Bone(mannequin.torso, "LeftArm",
                    new Vector3(-.31f, .44f, 0f));
                mannequin.rightArm = Bone(mannequin.torso, "RightArm",
                    new Vector3(.31f, .44f, 0f));
                mannequin.leftLeg = Bone(model.transform, "LeftLeg",
                    new Vector3(-.16f, .92f, 0f));
                mannequin.rightLeg = Bone(model.transform, "RightLeg",
                    new Vector3(.16f, .92f, 0f));

                Transform head = Bone(mannequin.torso, "Head",
                    new Vector3(0f, .63f, 0f));

                var ragdoll = root.AddComponent<SectorZombieRagdoll>();
                ragdoll.Configure(null, model.transform, mannequin);

                // The old six-body fallback exploded visible limbs when
                // gameplay killed procedural (non-Mixamo) infected.
                Assert.IsFalse(ragdoll.CanActivate);
                Assert.IsFalse(ragdoll.TryActivate(
                    Vector3.forward * 2f,
                    root.transform.position + Vector3.up,
                    Vector3.forward));
                Assert.IsFalse(ragdoll.IsActive);
                Assert.AreEqual(0, ragdoll.ActiveBodyCount);
                Assert.IsNull(mannequin.torso.GetComponent<Rigidbody>());
                Assert.IsNull(mannequin.leftArm.GetComponent<Rigidbody>());
                Assert.IsNull(mannequin.leftArm.GetComponent<CharacterJoint>());
                Assert.IsNull(head.GetComponent<Rigidbody>());

                // The visual fallback animates the intact model instead.
                var reaction = root.AddComponent<SectorZombieReaction>();
                reaction.Configure(model.transform);
                reaction.Die();
                Assert.IsTrue(reaction.HasDied);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static Transform Bone(Transform parent, string name, Vector3 position)
        {
            Transform bone = new GameObject(name).transform;
            bone.SetParent(parent, false);
            bone.localPosition = position;
            return bone;
        }
    }
}
