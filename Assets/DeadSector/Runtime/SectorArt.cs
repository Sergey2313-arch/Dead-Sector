using System.Collections.Generic;
using UnityEngine;

namespace DeadSector
{
    public sealed class SectorArt
    {
        readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        public Material Material(Color color)
        {
            if (materials.TryGetValue(color, out var material)) return material;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader) { color = color, enableInstancing = true };
            material.SetFloat("_Smoothness", .15f);
            materials.Add(color, material); return material;
        }
        public GameObject Shape(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color, bool collision = true)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Material(color);
            if (!collision) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        public void Box(Transform parent, string name, Vector3 p, Vector3 size, Color color) => Shape(parent, name, PrimitiveType.Cube, p, size, color);
        public SectorMannequin Person(Transform parent, Color clothing)
        {
            var root = new GameObject("Visual"); root.transform.SetParent(parent, false);
            var rig = root.AddComponent<SectorMannequin>();
            var dark = new Color(.08f, .09f, .1f); var skin = new Color(.57f, .48f, .38f);
            rig.torso = Joint(root.transform, "Torso", new Vector3(0, 1.05f, 0));
            Shape(rig.torso, "Jacket", PrimitiveType.Cube, new Vector3(0, .25f, 0), new Vector3(.48f, .55f, .28f), clothing, false);
            Shape(rig.torso, "Head", PrimitiveType.Sphere, new Vector3(0, .65f, 0), Vector3.one * .28f, skin, false);
            Shape(rig.torso, "Backpack", PrimitiveType.Cube, new Vector3(0, .25f, -.23f), new Vector3(.35f, .45f, .22f), dark, false);
            rig.leftArm = Joint(rig.torso, "LeftArm", new Vector3(-.31f, .45f, 0));
            rig.rightArm = Joint(rig.torso, "RightArm", new Vector3(.31f, .45f, 0));
            foreach (var arm in new[] { rig.leftArm, rig.rightArm })
            {
                Shape(arm, "Sleeve", PrimitiveType.Capsule, new Vector3(0, -.25f, 0), new Vector3(.15f, .25f, .15f), clothing, false);
                Shape(arm, "Hand", PrimitiveType.Sphere, new Vector3(0, -.53f, 0), Vector3.one * .13f, skin, false);
            }
            rig.leftLeg = Joint(root.transform, "LeftLeg", new Vector3(-.13f, .95f, 0));
            rig.rightLeg = Joint(root.transform, "RightLeg", new Vector3(.13f, .95f, 0));
            foreach (var leg in new[] { rig.leftLeg, rig.rightLeg })
            {
                Shape(leg, "Trousers", PrimitiveType.Capsule, new Vector3(0, -.4f, 0), new Vector3(.2f, .4f, .2f), dark, false);
                Shape(leg, "Boot", PrimitiveType.Cube, new Vector3(0, -.85f, .07f), new Vector3(.21f, .16f, .34f), dark, false);
            }
            SetActorLayer(root.transform); return rig;
        }
        static Transform Joint(Transform parent, string name, Vector3 position)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = position; return go.transform;
        }
        public static void SetActorLayer(Transform root)
        {
            root.gameObject.layer = 2;
            foreach (Transform child in root) SetActorLayer(child);
        }
        public void Dispose() { foreach (var material in materials.Values) Object.Destroy(material); materials.Clear(); }
    }
}
