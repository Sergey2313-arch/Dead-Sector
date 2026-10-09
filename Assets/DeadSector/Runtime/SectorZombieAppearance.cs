using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// Distinguishing blockout for three infected classes.
    /// Tints the already imported Mixamo body without duplicating FBX meshes
    /// and adds minimal torn clothes / exposed patches / massive shoulders.
    /// </summary>
    public sealed class SectorZombieAppearance : MonoBehaviour
    {
        public SectorZombieKind kind;

        Renderer[] characterRenderers;
        MaterialPropertyBlock block;
        Transform model;
        Material dirt;
        Material accent;
        bool initialized;
        float flashUntil;
        bool flashing;

        public void Configure(
            SectorZombieKind type, Transform modelTransform,
            SectorZombieProfile profile)
        {
            kind = type;
            model = modelTransform;
            if (model == null)
                return;

            characterRenderers = model.GetComponentsInChildren<Renderer>(true);
            block = new MaterialPropertyBlock();

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            dirt = new Material(shader);
            dirt.color = profile.Tint;

            accent = new Material(shader);
            accent.color = type == SectorZombieKind.Runner
                ? new Color(.44f, .15f, .14f)
                : type == SectorZombieKind.Brute
                    ? new Color(.15f, .21f, .16f)
                    : new Color(.22f, .25f, .19f);

            // Preserve Mixamo FBX import-scale (frequently 0.01).
            // Never overwrite the imported model's authored transform.
            model.localScale *= profile.ModelScale;

            Transform chest = model;
            Transform head = null;
            Animator animator = model.GetComponentInChildren<Animator>(true);
            if (animator != null && animator.isHuman)
            {
                chest = animator.GetBoneTransform(HumanBodyBones.Chest);
                if (chest == null)
                    chest = animator.GetBoneTransform(HumanBodyBones.Spine);
                head = animator.GetBoneTransform(HumanBodyBones.Head);
            }

            if (chest == null) chest = model;

            switch (type)
            {
                case SectorZombieKind.Runner:
                    AddPiece(chest, "Torn_Runner_Chest",
                        new Vector3(0f, 0f, .16f),
                        new Vector3(.30f, .24f, .08f), accent);
                    AddPiece(chest, "Runner_Shoulders",
                        new Vector3(0f, .19f, .02f),
                        new Vector3(.49f, .09f, .19f), dirt);
                    break;

                case SectorZombieKind.Brute:
                    AddPiece(chest, "Brute_Chest_Armor",
                        new Vector3(0f, .05f, .18f),
                        new Vector3(.53f, .37f, .16f), accent);
                    AddPiece(chest, "Brute_Shoulder_Bar",
                        new Vector3(0f, .27f, .03f),
                        new Vector3(.76f, .22f, .30f), dirt);
                    break;

                default:
                    AddPiece(chest, "Infected_Torn_Vest",
                        new Vector3(.06f, -.12f, .15f),
                        new Vector3(.21f, .23f, .075f), dirt);
                    break;
            }

            if (head != null)
            {
                AddPiece(head, "Infection_Scar",
                    new Vector3(.08f, .07f, .17f),
                    new Vector3(.10f, .04f, .04f), accent);
            }

            initialized = true;
            ApplyTint(profile.Tint);
        }

        static void AddPiece(
            Transform parent, string name, Vector3 pos,
            Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = pos;
            part.transform.localScale = scale;

            Collider collision = part.GetComponent<Collider>();
            if (collision != null)
            {
                collision.enabled = false;
                Destroy(collision);
            }

            part.GetComponent<Renderer>().sharedMaterial = material;
            SectorArt.SetActorLayer(part.transform, SectorArt.EnemyVisualLayer);
        }

        void ApplyTint(Color color)
        {
            if (characterRenderers == null)
                return;

            foreach (Renderer renderer in characterRenderers)
            {
                if (renderer == null)
                    continue;

                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", color);
                block.SetColor("_Color", color);
                renderer.SetPropertyBlock(block);
            }
        }

        public void FlashOnHit(float duration = .12f)
        {
            if (!initialized)
                return;

            flashUntil = Mathf.Max(flashUntil, Time.time + duration);

            if (!flashing)
            {
                flashing = true;
                ApplyTint(new Color(.98f, .38f, .25f));
            }
        }

        void Update()
        {
            if (flashing && Time.time >= flashUntil)
            {
                flashing = false;
                ApplyTint(SectorZombieProfiles.For(kind).Tint);
            }
        }

        void OnDestroy()
        {
            if (dirt != null) Destroy(dirt);
            if (accent != null) Destroy(accent);
        }
    }
}
