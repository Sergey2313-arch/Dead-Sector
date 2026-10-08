using System;
using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// Procedural prototype weapon silhouettes attached to X Bot bones:
    /// receiver/barrel/stock for guns, bindings and heads for stone tools,
    /// cloth-wrapped torches and visible carried weapons and backpack.
    /// This is not a substitute for final authored weapon meshes/animation.
    /// </summary>
    public sealed class SectorEquipmentVisuals : MonoBehaviour
    {
        public SectorPlayer player;

        readonly string[] equipped = { "", "", "" };
        readonly GameObject[] stowed = new GameObject[3];

        GameObject activeWeapon;
        GameObject backpack;
        GameObject muzzleFlash;

        Material steel;
        Material wornSteel;
        Material wood;
        Material rock;
        Material cloth;
        Material dark;
        Material pack;

        int activeSlot = -1;
        float flashUntil;
        Transform rightHand;
        Transform back;
        Transform hip;
        Transform leftLeg;

        public void Configure(SectorPlayer target)
        {
            player = target;
            Animator animator = player != null && player.visual != null
                ? player.visual.GetComponentInChildren<Animator>(true)
                : null;

            if (animator != null && animator.isHuman)
            {
                rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                back = animator.GetBoneTransform(HumanBodyBones.UpperChest);
                if (back == null) back = animator.GetBoneTransform(HumanBodyBones.Chest);
                if (back == null) back = animator.GetBoneTransform(HumanBodyBones.Spine);

                hip = animator.GetBoneTransform(HumanBodyBones.Hips);
                leftLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            }

            if (rightHand == null) rightHand = transform;
            if (back == null) back = transform;
            if (hip == null) hip = transform;
            if (leftLeg == null) leftLeg = transform;

            steel = NewMaterial(new Color(.15f, .18f, .19f));
            wornSteel = NewMaterial(new Color(.36f, .37f, .34f));
            wood = NewMaterial(new Color(.33f, .22f, .12f));
            rock = NewMaterial(new Color(.43f, .43f, .39f));
            cloth = NewMaterial(new Color(.52f, .45f, .34f));
            dark = NewMaterial(new Color(.08f, .095f, .10f));
            pack = NewMaterial(new Color(.26f, .32f, .22f));

            BuildBackpack();
            SetBackpackVisible(false);
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

        void BuildBackpack()
        {
            backpack = new GameObject("Equipped_Backpack");
            backpack.transform.SetParent(back, false);
            backpack.transform.localPosition = new Vector3(0f, .05f, -.29f);

            Part(backpack.transform, "Canvas_Pack", pack,
                new Vector3(0f, 0f, -.03f),
                new Vector3(.45f, .55f, .22f));

            Part(backpack.transform, "Backpack_Flap", cloth,
                new Vector3(0f, .25f, -.15f),
                new Vector3(.46f, .12f, .13f));

            Part(backpack.transform, "Back_Pocket", pack,
                new Vector3(0f, -.15f, -.18f),
                new Vector3(.30f, .23f, .11f));

            Part(backpack.transform, "Left_Strap", dark,
                new Vector3(-.21f, .01f, .04f),
                new Vector3(.055f, .47f, .06f));

            Part(backpack.transform, "Right_Strap", dark,
                new Vector3(.21f, .01f, .04f),
                new Vector3(.055f, .47f, .06f));

            Part(backpack.transform, "Metal_Buckle", wornSteel,
                new Vector3(0f, .12f, -.23f),
                new Vector3(.09f, .06f, .03f));
        }

        public void SetBackpackVisible(bool visible)
        {
            if (backpack != null && backpack.activeSelf != visible)
                backpack.SetActive(visible);
        }

        public void UpdateLoadout(
            string primary, string sidearm, string melee, int slot)
        {
            string[] values = { primary ?? "", sidearm ?? "", melee ?? "" };

            bool dirty = activeSlot != slot;

            for (int i = 0; i < equipped.Length; i++)
                dirty |= !string.Equals(
                    equipped[i], values[i], StringComparison.Ordinal);

            if (!dirty)
                return;

            for (int i = 0; i < 3; i++)
            {
                equipped[i] = values[i];
                if (stowed[i] != null) Destroy(stowed[i]);
                stowed[i] = null;

                if (i == slot || string.IsNullOrEmpty(equipped[i]))
                    continue;

                Transform parent = i == 0 ? back : i == 1 ? hip : leftLeg;
                stowed[i] = CreateWeapon(parent, equipped[i], false);
            }

            if (activeWeapon != null)
                Destroy(activeWeapon);

            activeWeapon = null;
            muzzleFlash = null;
            activeSlot = slot;

            if (slot >= 0 && slot < 3 && !string.IsNullOrEmpty(equipped[slot]))
                activeWeapon = CreateWeapon(rightHand, equipped[slot], true);
        }

        public void PlayMuzzleFlash()
        {
            if (muzzleFlash == null)
                return;

            muzzleFlash.SetActive(true);
            flashUntil = Time.time + .065f;
        }

        void Update()
        {
            if (muzzleFlash != null && muzzleFlash.activeSelf &&
                Time.time >= flashUntil)
                muzzleFlash.SetActive(false);
        }

        GameObject CreateWeapon(Transform bone, string id, bool held)
        {
            GameObject root = new GameObject(
                (held ? "Held_" : "Stowed_") + id);
            root.transform.SetParent(bone, false);

            root.transform.localPosition = held
                ? new Vector3(.02f, -.08f, .08f)
                : bone == back
                    ? new Vector3(.24f, .12f, -.31f)
                    : new Vector3(.14f, -.18f, -.10f);

            root.transform.localRotation = held
                ? Quaternion.Euler(90f, 0f, 0f)
                : Quaternion.Euler(0f, 0f, 28f);

            switch (id)
            {
                case "rifle":
                    BuildGun(root.transform, true, held);
                    break;
                case "pistol":
                    BuildGun(root.transform, false, held);
                    break;
                case "stone_knife":
                case "knife":
                    BuildKnife(root.transform, id == "stone_knife");
                    break;
                case "stone_axe":
                case "axe":
                    BuildAxe(root.transform, id == "stone_axe");
                    break;
                case "wood_club":
                    BuildClub(root.transform);
                    break;
                case "spear":
                    BuildSpear(root.transform);
                    break;
                case "torch":
                    BuildTorch(root.transform, held);
                    break;
                default:
                    Part(root.transform, "Improvised_Tool", wood,
                        new Vector3(0, 0, .20f),
                        new Vector3(.08f, .10f, .45f));
                    break;
            }

            return root;
        }

        void BuildGun(Transform root, bool rifle, bool held)
        {
            float length = rifle ? .75f : .24f;
            Part(root, "Receiver", steel,
                new Vector3(0f, 0f, length * .46f),
                new Vector3(rifle ? .11f : .09f, .12f, length));

            Part(root, "Barrel", wornSteel,
                new Vector3(0f, .02f, length + (rifle ? .12f : .035f)),
                new Vector3(.045f, .045f, rifle ? .32f : .09f),
                PrimitiveType.Cylinder,
                Quaternion.Euler(90f, 0f, 0f));

            Part(root, "Grip", dark,
                new Vector3(0f, -.14f, .13f),
                new Vector3(.08f, .24f, .11f),
                PrimitiveType.Cube, Quaternion.Euler(-17f, 0f, 0f));

            Part(root, "Magazine", wornSteel,
                new Vector3(0f, -.15f, length * .65f),
                new Vector3(.07f, .19f, .10f),
                PrimitiveType.Cube, Quaternion.Euler(9f, 0f, 0f));

            Part(root, "Front_Sight", wornSteel,
                new Vector3(0f, .10f, length + (rifle ? .12f : .01f)),
                new Vector3(.035f, .075f, .04f));

            Part(root, "Rear_Sight", wornSteel,
                new Vector3(0f, .11f, .12f),
                new Vector3(.05f, .08f, .05f));

            if (rifle)
            {
                Part(root, "Stock", wood,
                    new Vector3(0f, -.02f, -.18f),
                    new Vector3(.11f, .16f, .38f));

                Part(root, "Handguard", wood,
                    new Vector3(0f, -.015f, .64f),
                    new Vector3(.125f, .135f, .35f));
            }

            if (!held)
                return;

            muzzleFlash = Part(root, "Muzzle_Flash", cloth,
                new Vector3(0, .02f, length + (rifle ? .37f : .085f)),
                new Vector3(.14f, .14f, .18f));

            Renderer renderer = muzzleFlash.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Golden tint for flash, without changing all cloth materials.
                MaterialPropertyBlock flashBlock = new MaterialPropertyBlock();
                flashBlock.SetColor("_BaseColor", new Color(1f, .72f, .13f));
                flashBlock.SetColor("_Color", new Color(1f, .72f, .13f));
                renderer.SetPropertyBlock(flashBlock);
            }

            muzzleFlash.SetActive(false);
        }

        void BuildKnife(Transform root, bool stone)
        {
            Part(root, "Wood_Handle", wood,
                new Vector3(0f, 0f, .115f),
                new Vector3(.075f, .082f, .23f));

            Part(root, "Binding", cloth,
                new Vector3(0f, 0f, .19f),
                new Vector3(.09f, .09f, .04f));

            Part(root, stone ? "Chipped_Flint_Edge" : "Steel_Blade",
                stone ? rock : wornSteel,
                new Vector3(0f, 0f, .34f),
                new Vector3(.095f, .023f, .25f));

            Part(root, "Blade_Tip", stone ? rock : wornSteel,
                new Vector3(0f, 0f, .465f),
                new Vector3(.055f, .021f, .10f),
                PrimitiveType.Cube,
                Quaternion.Euler(0f, 38f, 0f));
        }

        void BuildAxe(Transform root, bool stone)
        {
            Part(root, "Wooden_Haft", wood,
                new Vector3(0, 0, .25f),
                new Vector3(.075f, .08f, .50f));

            Part(root, stone ? "Stone_Head" : "Metal_Head",
                stone ? rock : wornSteel,
                new Vector3(.11f, 0, .47f),
                new Vector3(.33f, .095f, .13f),
                PrimitiveType.Cube,
                Quaternion.Euler(0f, -15f, 0f));

            Part(root, "Grip_Binding", cloth,
                new Vector3(0f, 0, .42f),
                new Vector3(.09f, .095f, .065f));
        }

        void BuildClub(Transform root)
        {
            Part(root, "Club_Shaft", wood,
                new Vector3(0f, 0, .31f),
                new Vector3(.095f, .11f, .61f));

            Part(root, "Heavy_Knot", wood,
                new Vector3(0, 0, .59f),
                new Vector3(.19f, .17f, .25f),
                PrimitiveType.Capsule,
                Quaternion.Euler(90f, 0f, 0f));

            Part(root, "Cloth_Grip", cloth,
                new Vector3(0f, 0f, .12f),
                new Vector3(.10f, .12f, .17f));
        }

        void BuildSpear(Transform root)
        {
            Part(root, "Long_Shaft", wood,
                new Vector3(0f, 0f, .55f),
                new Vector3(.055f, .056f, 1.10f));

            Part(root, "Stone_Spearhead", rock,
                new Vector3(0f, 0f, 1.17f),
                new Vector3(.12f, .055f, .25f));

            Part(root, "Rope_Wrap", cloth,
                new Vector3(0f, 0f, 1.01f),
                new Vector3(.088f, .09f, .11f));
        }

        void BuildTorch(Transform root, bool held)
        {
            Part(root, "Dry_Stick", wood,
                new Vector3(0f, 0f, .25f),
                new Vector3(.07f, .075f, .48f));

            Part(root, "Tightly_Wrapped_Rags", cloth,
                new Vector3(0f, 0f, .50f),
                new Vector3(.18f, .17f, .23f));

            if (!held) return;

            GameObject lightObject = new GameObject("Torch_Light");
            lightObject.transform.SetParent(root, false);
            lightObject.transform.localPosition =
                new Vector3(0f, .08f, .55f);

            Light glow = lightObject.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.range = 9f;
            glow.intensity = 1.4f;
            glow.color = new Color(1f, .59f, .25f);
            glow.shadows = LightShadows.None;
        }

        static GameObject Part(
            Transform parent, string name, Material material,
            Vector3 position, Vector3 scale,
            PrimitiveType primitive = PrimitiveType.Cube,
            Quaternion rotation = default)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localRotation =
                rotation == default ? Quaternion.identity : rotation;
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
            return part;
        }

        void OnDestroy()
        {
            if (steel != null) Destroy(steel);
            if (wornSteel != null) Destroy(wornSteel);
            if (wood != null) Destroy(wood);
            if (rock != null) Destroy(rock);
            if (cloth != null) Destroy(cloth);
            if (dark != null) Destroy(dark);
            if (pack != null) Destroy(pack);
        }
    }
}
