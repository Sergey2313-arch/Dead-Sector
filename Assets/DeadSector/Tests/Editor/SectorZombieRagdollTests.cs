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
        public void ProceduralZombieFallsBackToSixBodyJointedRagdoll()
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
                head.gameObject.AddComponent<SphereCollider>();

                var ragdoll = root.AddComponent<SectorZombieRagdoll>();
                ragdoll.Configure(null, model.transform, mannequin);

                Assert.IsTrue(ragdoll.CanActivate);
                Assert.AreEqual(0, ragdoll.ActiveBodyCount,
                    "Rigidbody objects must not be allocated while AI is alive");

                mannequin.enabled = false;
                Assert.IsTrue(ragdoll.TryActivate(
                    Vector3.forward * 2f,
                    root.transform.position + Vector3.up,
                    Vector3.forward));

                Assert.IsTrue(ragdoll.IsActive);
                Assert.AreEqual(6, ragdoll.ActiveBodyCount);
                Assert.IsNotNull(
                    mannequin.torso.GetComponent<Rigidbody>());
                Assert.IsNotNull(
                    mannequin.leftArm.GetComponent<CharacterJoint>());
                Assert.IsFalse(
                    mannequin.leftLeg.GetComponent<Rigidbody>().isKinematic);
                Assert.IsFalse(ragdoll.TryActivate(
                    Vector3.forward, Vector3.zero, Vector3.zero),
                    "Repeated fatal damage must not recreate physics components");
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
