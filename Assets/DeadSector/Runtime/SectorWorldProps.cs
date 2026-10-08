using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// Deterministic visual roadside dressing for the approved 8km atlas.
    /// Generates a small number of collision-enabled wrecks, barriers and
    /// signs on streamed tiles (placeholder models, not production vehicles).
    /// </summary>
    public static class SectorWorldProps
    {
        static readonly Color CarRed = new Color(.28f, .11f, .095f);
        static readonly Color CarBlue = new Color(.11f, .16f, .22f);
        static readonly Color CarGrey = new Color(.23f, .25f, .24f);
        static readonly Color RoadSign = new Color(.34f, .38f, .31f);
        static readonly Color Glass = new Color(.12f, .19f, .23f);
        static readonly Color Rubber = new Color(.075f, .085f, .085f);

        public static void Build(Transform tileRoot, Vector3 origin, SectorArt art)
        {
            int routeIndex = 0;

            foreach (Vector2[] route in SectorMapPlan.RoadRoutes)
            {
                for (int segment = 1; segment < route.Length; segment++)
                {
                    Vector2 a = route[segment - 1];
                    Vector2 b = route[segment];
                    Vector2 direction = b - a;
                    float length = direction.magnitude;

                    if (length < 30f)
                        continue;

                    direction /= length;
                    Vector2 tangent = new Vector2(-direction.y, direction.x);

                    // Whole-line sample spacing gives consistent props at tile borders.
                    int count = Mathf.FloorToInt(length / 175f);

                    for (int i = 1; i < count; i++)
                    {
                        int key = routeIndex * 487 + segment * 131 + i * 71;
                        int parity = (key & 1) == 0 ? 1 : -1;
                        float side = parity * (10f + (key % 6));
                        Vector2 along = Vector2.Lerp(a, b, (float)i / count);
                        Vector2 point = along + tangent * side;

                        if (point.x < origin.x + 10f ||
                            point.x >= origin.x + SectorLayout.TileSize - 10f ||
                            point.y < origin.z + 10f ||
                            point.y >= origin.z + SectorLayout.TileSize - 10f)
                            continue;

                        if (SectorGeography.TryGetWaterLevel(
                            point.x, point.y, out float _))
                            continue;

                        float y = SectorLayout.Height(point.x, point.y);
                        Vector3 local = new Vector3(
                            point.x - origin.x,
                            y,
                            point.y - origin.z);

                        GameObject root = new GameObject(
                            "Roadside_" + routeIndex + "_" + segment + "_" + i);

                        root.transform.SetParent(tileRoot, false);
                        root.transform.localPosition = local;
                        root.transform.localRotation = Quaternion.LookRotation(
                            new Vector3(direction.x, 0f, direction.y));

                        if (key % 3 == 0)
                            BuildWreck(root.transform, art, key);
                        else if (key % 3 == 1)
                            BuildBarrier(root.transform, art);
                        else
                            BuildSign(root.transform, art);
                    }
                }

                routeIndex++;
            }
        }

        static void BuildWreck(Transform root, SectorArt art, int seed)
        {
            Color shell = seed % 2 == 0 ? CarRed : CarBlue;

            art.Box(root, "Car_Armored_Chassis",
                new Vector3(0, .80f, 0),
                new Vector3(2.1f, .70f, 4.4f),
                shell);

            art.Box(root, "Car_Cabin",
                new Vector3(0, 1.38f, -.30f),
                new Vector3(1.85f, .56f, 2.3f),
                CarGrey);

            art.Box(root, "Broken_Windshield",
                new Vector3(0, 1.52f, .95f),
                new Vector3(1.6f, .35f, .10f),
                Glass);

            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    GameObject wheel = art.Shape(
                        root,
                        "Flat_Wheel",
                        PrimitiveType.Cylinder,
                        new Vector3(x * 1.04f, .46f, z * 1.45f),
                        new Vector3(.36f, .18f, .36f),
                        Rubber);

                    wheel.transform.localRotation =
                        Quaternion.Euler(0f, 0f, 90f);
                }
            }

            art.Box(root, "Rusty_Bumper",
                new Vector3(0, .65f, 2.18f),
                new Vector3(2.1f, .22f, .27f),
                CarGrey);
        }

        static void BuildBarrier(Transform root, SectorArt art)
        {
            for (int i = -1; i <= 1; i++)
            {
                art.Box(root, "Barrier_Block_" + i,
                    new Vector3(i * 1.35f, .65f, 0f),
                    new Vector3(1.2f, 1.3f, 1f),
                    CarGrey);
            }

            art.Box(root, "Barrier_Warning",
                new Vector3(0f, 1.45f, -.52f),
                new Vector3(2.8f, .20f, .12f),
                CarRed);
        }

        static void BuildSign(Transform root, SectorArt art)
        {
            art.Shape(root, "Road_Sign_Post", PrimitiveType.Cylinder,
                new Vector3(0, 1.65f, 0),
                new Vector3(.09f, 1.65f, .09f),
                CarGrey);

            art.Box(root, "Road_Sign_Plate",
                new Vector3(0, 3.1f, 0f),
                new Vector3(1.45f, .8f, .14f),
                RoadSign);
        }
    }
}
