using System;
using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// Distinct modular blockout clothing, attached to humanoid bones.
    /// Wooden armor has rigid panels/straps while cotton uses soft-colored
    /// lightweight pieces. Not a replacement for fitted skinned clothing.
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
        Material straps;
        Material padding;

        public void Configure(SectorPlayer target, SectorEquipment equipment)
        {
            player = target;
            gear = equipment;

            Animator animator = player != null && player.visual != null
                ? player.visual.GetComponentInChildren<Animator>(true)
                : null;

            bones = new Transform[4];

            if (animator != null && animator.isHuman)
            {
                bones[0] = animator.GetBoneTransform(HumanBodyBones.Head);
                bones[1] = animator.GetBoneTransform(HumanBodyBones.Chest);
                bones[2] = animator.GetBoneTransform(HumanBodyBones.Hips);
                bones[3] = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            }

            for (int i = 0; i < bones.Length; i++)
                if (bones[i] == null)
                    bones[i] = player != null ? player.transform : transform;

            wood = NewMaterial(new Color(.37f, .26f, .16f));
            cotton = NewMaterial(new Color(.67f, .64f, .55f));
            straps = NewMaterial(new Color(.25f, .21f, .16f));
            padding = NewMaterial(new Color(.43f, .43f, .36f));
        }

        static Material NewMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader);
            material.color = color;
            material.enableInstancing = true;
            return material;
        }

        public void Refresh()
        {
            if (gear == null || bones == null)
                return;

            for (int i = 0; i < shown.Length; i++)
            {
                string current = gear.Equipped(
                    (SectorEquipment.GearSlot)i);

                if (shown[i] == current)
                    continue;

                shown[i] = current;

                if (objects[i] != null)
                    Destroy(objects[i]);

                objects[i] = null;

                if (string.IsNullOrEmpty(current))
                    continue;

                Transform parent = bones[i];
                bool isWood = current.StartsWith(
                    "wood_", StringComparison.Ordinal);

                GameObject root = new GameObject("Worn_" + current);
                root.transform.SetParent(parent, false);
                SectorArt.SetActorLayer(root.transform);

                switch (i)
                {
                    case 0:
                        BuildHead(root.transform, isWood);
                        break;
                    case 1:
                        BuildChest(root.transform, isWood);
                        break;
                    case 2:
                        BuildLegs(root.transform, isWood);
                        break;
                    case 3:
                        BuildFeet(root.transform, isWood);
                        break;
                }

                objects[i] = root;
            }
        }

        void BuildHead(Transform root, bool rigid)
        {
            Material fabric = rigid ? wood : cotton;

            Part(root, "Crown", fabric,
                new Vector3(0f, .095f, 0f),
                new Vector3(.30f, .13f, .31f));

            Part(root, "Front_Brow", fabric,
                new Vector3(0f, .015f, .145f),
                new Vector3(.34f, .075f, .075f));

            Part(root, "Back_Strap", straps,
                new Vector3(0f, -.04f, -.14f),
                new Vector3(.30f, .043f, .06f));

            if (rigid)
            {
                Part(root, "Left_Temple", wood,
                    new Vector3(-.15f, -.03f, 0f),
                    new Vector3(.07f, .14f, .23f));
                Part(root, "Right_Temple", wood,
                    new Vector3(.15f, -.03f, 0f),
                    new Vector3(.07f, .14f, .23f));
            }
        }

        void BuildChest(Transform root, bool rigid)
        {
            Material body = rigid ? wood : cotton;

            Part(root, "Chest_Center", body,
                new Vector3(0, -.06f, .12f),
                new Vector3(.30f, .31f, .095f));

            Part(root, "Left_Panel", body,
                new Vector3(-.18f, -.055f, .10f),
                new Vector3(.12f, .30f, .09f));

            Part(root, "Right_Panel", body,
                new Vector3(.18f, -.055f, .10f),
                new Vector3(.12f, .30f, .09f));

            Part(root, "Back_Panel", rigid ? wood : padding,
                new Vector3(0, -.07f, -.13f),
                new Vector3(.36f, .32f, .07f));

            Part(root, "Waist_Binding", straps,
                new Vector3(0f, -.25f, 0f),
                new Vector3(.42f, .065f, .26f));

            Part(root, "Left_Shoulder", rigid ? wood : cotton,
                new Vector3(-.245f, .15f, 0f),
                new Vector3(.15f, rigid ? .11f : .055f, .24f));

            Part(root, "Right_Shoulder", rigid ? wood : cotton,
                new Vector3(.245f, .15f, 0f),
                new Vector3(.15f, rigid ? .11f : .055f, .24f));

            if (!rigid)
            {
                Part(root, "Shirt_Collar", straps,
                    new Vector3(0, .16f, .10f),
                    new Vector3(.22f, .05f, .13f));
            }
        }

        void BuildLegs(Transform root, bool rigid)
        {
            Material material = rigid ? wood : cotton;

            Part(root, "Belt", straps,
                new Vector3(0f, -.03f, 0f),
                new Vector3(.38f, .075f, .26f));

            Part(root, "Left_Thigh_Panel", material,
                new Vector3(-.13f, -.22f, .09f),
                new Vector3(.16f, .29f, rigid ? .11f : .07f));

            Part(root, "Right_Thigh_Panel", material,
                new Vector3(.13f, -.22f, .09f),
                new Vector3(.16f, .29f, rigid ? .11f : .07f));

            Part(root, "Rear_Cloth", rigid ? padding : cotton,
                new Vector3(0f, -.16f, -.11f),
                new Vector3(.32f, .24f, .07f));

            if (rigid)
            {
                Part(root, "Left_Leather_Tie", straps,
                    new Vector3(-.13f, -.33f, .13f),
                    new Vector3(.17f, .05f, .13f));
                Part(root, "Right_Leather_Tie", straps,
                    new Vector3(.13f, -.33f, .13f),
                    new Vector3(.17f, .05f, .13f));
            }
        }

        void BuildFeet(Transform root, bool rigid)
        {
            Part(root, "Foot_Wrap", rigid ? wood : cotton,
                new Vector3(0f, .07f, .05f),
                new Vector3(.16f, .13f, .27f));

            Part(root, "Sole", straps,
                new Vector3(0f, 0f, .055f),
                new Vector3(.17f, .045f, .30f));
        }

        static void Part(
            Transform parent, string label, Material material,
            Vector3 offset, Vector3 scale)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = label;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = offset;
            part.transform.localScale = scale;

            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                Destroy(collider);
            }

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;

            SectorArt.SetActorLayer(part.transform);
        }

        void OnDestroy()
        {
            if (wood != null) Destroy(wood);
            if (cotton != null) Destroy(cotton);
            if (straps != null) Destroy(straps);
            if (padding != null) Destroy(padding);
        }
    }
}
