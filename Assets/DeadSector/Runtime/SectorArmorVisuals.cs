using System;
using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// Temporary 3D representation of wooden protection and cotton clothes.
    /// The inventory slots/effect are authoritative; replace these blockouts
    /// with character-fitted modular garments after X Bot Play Mode QA.
    /// </summary>
    public sealed class SectorArmorVisuals : MonoBehaviour
    {
        public SectorPlayer player;
        public SectorEquipment gear;

        readonly string[] shown = new string[4];
        readonly GameObject[] objects = new GameObject[4];
        Transform[] bones;
        Material wood;
        Material cotton;

        public void Configure(SectorPlayer target, SectorEquipment equipment)
        {
            player = target;
            gear = equipment;

            Animator anim = target != null && target.visual != null
                ? target.visual.GetComponentInChildren<Animator>(true)
                : null;

            bones = new Transform[4];
            if (anim != null && anim.isHuman)
            {
                bones[0] = anim.GetBoneTransform(HumanBodyBones.Head);
                bones[1] = anim.GetBoneTransform(HumanBodyBones.Chest);
                bones[2] = anim.GetBoneTransform(HumanBodyBones.Hips);
                bones[3] = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
            }

            for (int i = 0; i < bones.Length; i++)
                if (bones[i] == null)
                    bones[i] = target != null ? target.transform : transform;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            wood = new Material(shader) { color = new Color(.35f, .24f, .15f) };
            cotton = new Material(shader) { color = new Color(.68f, .65f, .55f) };
        }

        public void Refresh()
        {
            if (gear == null || bones == null)
                return;

            for (int i = 0; i < shown.Length; i++)
            {
                string current =
                    gear.Equipped((SectorEquipment.GearSlot)i);

                if (shown[i] == current)
                    continue;

                shown[i] = current;

                if (objects[i] != null)
                    Destroy(objects[i]);

                objects[i] = null;
                if (string.IsNullOrEmpty(current))
                    continue;

                GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mesh.name = "Worn_" + current;
                mesh.transform.SetParent(bones[i], false);

                switch (i)
                {
                    case 0:
                        mesh.transform.localPosition =
                            new Vector3(0, .10f, 0);
                        mesh.transform.localScale =
                            new Vector3(.31f, .17f, .32f);
                        break;
                    case 1:
                        mesh.transform.localPosition =
                            new Vector3(0, -.10f, 0);
                        mesh.transform.localScale =
                            new Vector3(.40f, .37f, .24f);
                        break;
                    case 2:
                        mesh.transform.localPosition =
                            new Vector3(0, -.18f, 0);
                        mesh.transform.localScale =
                            new Vector3(.33f, .31f, .23f);
                        break;
                    default:
                        mesh.transform.localPosition =
                            new Vector3(0, .07f, 0);
                        mesh.transform.localScale =
                            new Vector3(.18f, .17f, .28f);
                        break;
                }

                Collider collider = mesh.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = false;
                    Destroy(collider);
                }

                mesh.GetComponent<Renderer>().sharedMaterial =
                    current.StartsWith("wood_", StringComparison.Ordinal)
                        ? wood : cotton;

                SectorArt.SetActorLayer(mesh.transform);
                objects[i] = mesh;
            }
        }

        void OnDestroy()
        {
            if (wood != null) Destroy(wood);
            if (cotton != null) Destroy(cotton);
        }
    }
}
