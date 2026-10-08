using System;
using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// Temporary visible loadout for X Bot. These are simple mesh blockouts:
    /// replace the primitive shapes with actual weapon/backpack prefabs later.
    /// Attached to humanoid bones so TPP has visible equipment.
    /// </summary>
    public sealed class SectorEquipmentVisuals : MonoBehaviour
    {
        public SectorPlayer player;

        readonly string[] equipped = { "", "", "" };
        readonly GameObject[] stowed = new GameObject[3];
        GameObject activeWeapon;
        GameObject backpack;
        Material weaponMaterial;
        Material packMaterial;
        int activeSlot = -1;
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

                if (back == null)
                    back = animator.GetBoneTransform(HumanBodyBones.Chest);

                if (back == null)
                    back = animator.GetBoneTransform(HumanBodyBones.Spine);

                hip = animator.GetBoneTransform(HumanBodyBones.Hips);
                leftLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            }

            if (rightHand == null) rightHand = transform;
            if (back == null) back = transform;
            if (hip == null) hip = transform;
            if (leftLeg == null) leftLeg = transform;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            weaponMaterial = new Material(shader);
            weaponMaterial.color = new Color(.16f, .18f, .19f);

            packMaterial = new Material(shader);
            packMaterial.color = new Color(.21f, .27f, .19f);

            backpack = CreatePart(back, "Backpack", packMaterial,
                new Vector3(0f, .05f, -.28f),
                new Vector3(.45f, .59f, .25f));
        }

        public void UpdateLoadout(string primary, string sidearm, string melee, int slot)
        {
            string[] values = { primary ?? "", sidearm ?? "", melee ?? "" };

            bool dirty = slot != activeSlot;
            for (int i = 0; i < 3; i++)
                dirty |= !string.Equals(values[i], equipped[i], StringComparison.Ordinal);

            if (!dirty)
                return;

            for (int i = 0; i < 3; i++)
            {
                equipped[i] = values[i];

                if (stowed[i] != null)
                    Destroy(stowed[i]);

                stowed[i] = null;

                if (i == slot || string.IsNullOrEmpty(equipped[i]))
                    continue;

                Transform parent = i == 0 ? back : i == 1 ? hip : leftLeg;
                stowed[i] = CreateWeapon(parent, equipped[i], false);
            }

            if (activeWeapon != null)
                Destroy(activeWeapon);

            activeWeapon = null;
            activeSlot = slot;

            if (slot >= 0 && slot < 3 && !string.IsNullOrEmpty(equipped[slot]))
                activeWeapon = CreateWeapon(rightHand, equipped[slot], true);
        }

        GameObject CreateWeapon(Transform parent, string id, bool held)
        {
            bool rifle = id == "rifle";
            bool pistol = id == "pistol";
            bool axe = id == "axe";

            GameObject root = new GameObject(
                (held ? "Held_" : "Stowed_") + id);
            root.transform.SetParent(parent, false);

            float length = rifle ? .85f : pistol ? .25f : axe ? .48f : .27f;

            root.transform.localPosition = held
                ? new Vector3(.02f, -.07f, .08f)
                : parent == back
                    ? new Vector3(.24f, .12f, -.35f)
                    : new Vector3(.18f, -.13f, -.15f);

            root.transform.localRotation = held
                ? Quaternion.Euler(90f, 0f, 0f)
                : Quaternion.Euler(0f, 0f, 28f);

            CreatePart(root.transform, "Main_Body", weaponMaterial,
                new Vector3(0f, 0f, length * .5f),
                new Vector3(.09f, .12f, length));

            if (rifle || pistol)
            {
                CreatePart(root.transform, "Grip", weaponMaterial,
                    new Vector3(0f, -.11f, .11f),
                    new Vector3(.08f, .25f, .11f));
            }
            else if (axe)
            {
                CreatePart(root.transform, "Axe_Head", weaponMaterial,
                    new Vector3(0f, 0f, length),
                    new Vector3(.32f, .15f, .12f));
            }

            return root;
        }

        static GameObject CreatePart(
            Transform parent, string name, Material material,
            Vector3 localPosition, Vector3 localScale)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;

            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            part.GetComponent<Renderer>().sharedMaterial = material;
            SectorArt.SetActorLayer(part.transform);
            return part;
        }

        void OnDestroy()
        {
            if (weaponMaterial != null) Destroy(weaponMaterial);
            if (packMaterial != null) Destroy(packMaterial);
        }
    }
}
