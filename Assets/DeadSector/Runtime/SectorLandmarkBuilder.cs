using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// Streamable blockout structures at the approved world-plan coordinates.
    /// Geometry is deliberately modular and low-detail until production assets
    /// are selected. Every location is parented to its owning terrain tile.
    /// </summary>
    public static class SectorLandmarkBuilder
    {
        static readonly Color Concrete = new Color(.39f, .40f, .38f);
        static readonly Color Wall = new Color(.55f, .50f, .43f);
        static readonly Color Dark = new Color(.20f, .23f, .24f);
        static readonly Color Rust = new Color(.35f, .18f, .12f);
        static readonly Color Wood = new Color(.28f, .20f, .12f);
        static readonly Color Glass = new Color(.24f, .34f, .37f);
        static readonly Color White = new Color(.76f, .76f, .68f);

        public static void Build(
            Transform tile,
            Vector3 origin,
            SectorArt art)
        {
            foreach (SectorMapPlan.Location poi in SectorMapPlan.Locations)
            {
                if (poi.Kind == SectorMapPlan.LocationKind.Wilderness &&
                    poi.Id != "forest_camp")
                    continue;

                if (SectorLayout.TileAt(poi.AtHeight(0f)) !=
                    SectorLayout.TileAt(origin))
                    continue;

                var root = new GameObject("POI_" + poi.Id).transform;
                root.SetParent(tile, false);

                float surface = SectorLayout.Height(
                    poi.MapPosition.x, poi.MapPosition.y);

                root.localPosition = new Vector3(
                    poi.MapPosition.x - origin.x,
                    surface + .04f,
                    poi.MapPosition.y - origin.z);

                switch (poi.Id)
                {
                    case "forest_camp":
                        Camp(root, art); break;
                    case "radio_station":
                        Tower(root, art); break;
                    case "quarry":
                        Quarry(root, art); break;
                    case "abandoned_city":
                        Town(root, art, true); break;
                    case "village":
                        Village(root, art); break;
                    case "town":
                        Town(root, art, false); break;
                    case "clinic":
                        Clinic(root, art); break;
                    case "gas_station":
                        FuelStation(root, art); break;
                    case "military_checkpoint":
                        Checkpoint(root, art); break;
                    case "garages":
                        Garages(root, art); break;
                    case "factory":
                        Industry(root, art, true); break;
                    case "warehouse":
                        Industry(root, art, false); break;
                    case "construction":
                        Construction(root, art); break;
                    case "dam":
                        Dam(root, art); break;
                    case "airfield":
                        Airfield(root, art); break;
                }
            }
        }

        static void Village(Transform root, SectorArt art)
        {
            for (int i = 0; i < 8; i++)
            {
                float x = (i % 4 - 1.5f) * 46f;
                float z = (i / 4 == 0 ? -1f : 1f) * 42f;
                BasicHouse(root, art, new Vector3(x, 0, z), 13f, 16f, 4.4f, i);
            }

            art.Box(root, "Bus_Stop", new Vector3(0, 1.7f, -18f),
                new Vector3(5f, 3.4f, 2.6f), Dark);
        }

        static void Town(Transform root, SectorArt art, bool ruined)
        {
            for (int i = 0; i < (ruined ? 12 : 8); i++)
            {
                float x = (i % 4 - 1.5f) * 50f;
                float z = (i / 4 - (ruined ? 1f : .5f)) * 46f;
                float height = ruined ? 10f + (i % 3) * 6f : 6f + (i % 3) * 2.5f;
                Building(root, art, new Vector3(x, 0, z),
                    24f, 20f, height, ruined ? Dark : Wall, "Block_" + i);

                if (ruined && i % 3 == 0)
                {
                    art.Box(root, "Collapsed_Slab",
                        new Vector3(x + 13f, .7f, z + 10f),
                        new Vector3(14f, 1.3f, 8f), Concrete);
                }
            }
        }

        static void Clinic(Transform root, SectorArt art)
        {
            Building(root, art, Vector3.zero, 29f, 23f, 7.5f, White, "Clinic");
            art.Box(root, "Medical_Cross_Vertical", new Vector3(0, 7.9f, -11.8f),
                new Vector3(.7f, 3.2f, .2f), Rust);
            art.Box(root, "Medical_Cross_Horizontal", new Vector3(0, 7.9f, -11.8f),
                new Vector3(2.7f, .7f, .2f), Rust);
            art.Box(root, "Ambulance_Shelter",
                new Vector3(18f, 2.8f, -5f),
                new Vector3(11f, 5.6f, 10f), Concrete);
        }

        static void FuelStation(Transform root, SectorArt art)
        {
            Building(root, art, new Vector3(-15, 0, 6), 19, 13, 4.3f, Concrete, "Fuel_Shop");
            art.Box(root, "Fuel_Canopy", new Vector3(10, 5.1f, 0),
                new Vector3(26, .55f, 15), Rust);

            for (int i = -1; i <= 1; i += 2)
            {
                art.Shape(root, "Pillar", PrimitiveType.Cylinder,
                    new Vector3(10 + i * 11f, 2.6f, 0),
                    new Vector3(.4f, 2.6f, .4f), Concrete);
                art.Box(root, "Pump", new Vector3(10 + i * 6f, 1.15f, 0),
                    new Vector3(1.4f, 2.3f, 1.2f), Dark);
            }

            art.Box(root, "Price_Sign", new Vector3(31, 5f, -6),
                new Vector3(4.5f, 7f, .5f), Rust);
        }

        static void Checkpoint(Transform root, SectorArt art)
        {
            BasicHouse(root, art, new Vector3(-8, 0, 15), 9, 12, 3.7f, 0);

            for (int i = -4; i <= 4; i++)
            {
                if (i == 0 || i == 1)
                    continue;

                art.Box(root, "Concrete_Block",
                    new Vector3(i * 5f, .8f, -8),
                    new Vector3(4.2f, 1.6f, 1.4f), Concrete);
            }

            art.Box(root, "Security_Boom",
                new Vector3(1, 2.4f, -7),
                new Vector3(14f, .23f, .23f), Rust);

            art.Box(root, "Watch_Post", new Vector3(27, 4.2f, 11),
                new Vector3(5f, 8.4f, 5f), Dark);
        }

        static void Garages(Transform root, SectorArt art)
        {
            for (int i = 0; i < 6; i++)
            {
                float x = (i - 2.5f) * 8.5f;
                art.Box(root, "Garage_" + i,
                    new Vector3(x, 2.3f, 0),
                    new Vector3(8f, 4.6f, 13f), Concrete);
                art.Box(root, "Door_" + i,
                    new Vector3(x, 2.1f, -6.6f),
                    new Vector3(6.8f, 4.1f, .12f),
                    i % 2 == 0 ? Rust : Dark);
            }
        }

        static void Industry(Transform root, SectorArt art, bool factory)
        {
            Building(root, art, Vector3.zero,
                factory ? 90f : 70f,
                factory ? 125f : 92f,
                factory ? 14f : 10f,
                Concrete,
                factory ? "Factory_Hall" : "Storage_Hall");

            for (int i = 0; i < 6; i++)
            {
                art.Box(root, "Cargo",
                    new Vector3(-40f + i * 14f, 1.7f, -78f),
                    new Vector3(11f, 3.4f, 6f),
                    i % 2 == 0 ? Rust : Dark);
            }

            if (factory)
            {
                for (int i = 0; i < 3; i++)
                {
                    art.Shape(root, "Smokestack", PrimitiveType.Cylinder,
                        new Vector3(-32f + i * 28f, 18f, 40f),
                        new Vector3(2.2f, 18f, 2.2f),
                        Rust);
                }
            }
        }

        static void Camp(Transform root, SectorArt art)
        {
            for (int i = 0; i < 6; i++)
            {
                float x = Mathf.Cos(i * Mathf.PI / 3f) * 25f;
                float z = Mathf.Sin(i * Mathf.PI / 3f) * 25f;
                var tent = art.Shape(root, "Tent",
                    PrimitiveType.Cube,
                    new Vector3(x, 1.7f, z),
                    new Vector3(5.8f, 3.4f, 7f),
                    i % 2 == 0 ? Dark : Rust);

                tent.transform.localRotation = Quaternion.Euler(0, i * 60f, 0);
            }

            art.Shape(root, "Firepit", PrimitiveType.Cylinder,
                new Vector3(0, .15f, 0),
                new Vector3(1.2f, .15f, 1.2f), Rust);
        }

        static void Tower(Transform root, SectorArt art)
        {
            for (int i = -1; i <= 1; i += 2)
            {
                for (int j = -1; j <= 1; j += 2)
                {
                    art.Shape(root, "Tower_Leg", PrimitiveType.Cylinder,
                        new Vector3(i * 3f, 17f, j * 3f),
                        new Vector3(.3f, 17f, .3f), Concrete);
                }
            }

            for (int i = 1; i < 11; i++)
            {
                art.Box(root, "Cross_Brace",
                    new Vector3(0, i * 3.1f, 0),
                    new Vector3(6.2f, .2f, .2f),
                    i % 2 == 0 ? Rust : Concrete);
            }

            art.Shape(root, "Antenna", PrimitiveType.Cylinder,
                new Vector3(0, 39f, 0),
                new Vector3(.16f, 8f, .16f), Rust, false);

            Building(root, art, new Vector3(12, 0, -7),
                12, 12, 3.2f, Dark, "Control_Hut");
        }

        static void Quarry(Transform root, SectorArt art)
        {
            for (int i = 0; i < 22; i++)
            {
                float angle = i * Mathf.PI * 2f / 22f;
                float distance = 110f + (i % 3) * 11f;

                art.Shape(root, "Broken_Rock", PrimitiveType.Cube,
                    new Vector3(
                        Mathf.Cos(angle) * distance,
                        2.5f,
                        Mathf.Sin(angle) * distance),
                    new Vector3(9f + i % 3 * 2f, 5f, 8f),
                    Concrete);
            }

            art.Box(root, "Ore_Silo", new Vector3(50, 9, 25),
                new Vector3(10, 18, 11), Dark);
        }

        static void Construction(Transform root, SectorArt art)
        {
            Building(root, art, new Vector3(-20f, 0, 0),
                32f, 36f, 6f, Concrete, "Unfinished_Shell");

            art.Shape(root, "Crane_Mast", PrimitiveType.Cylinder,
                new Vector3(38f, 22f, 0),
                new Vector3(.75f, 22f, .75f), Rust);

            art.Box(root, "Crane_Arm", new Vector3(9f, 43f, 0),
                new Vector3(75f, 1f, 1f), Rust);

            for (int i = 0; i < 10; i++)
            {
                art.Box(root, "Concrete_Supplies",
                    new Vector3(-45f + i * 8f, 1f, -35f),
                    new Vector3(6f, 2f, 8f), Concrete);
            }
        }

        static void Dam(Transform root, SectorArt art)
        {
            // Heavy concrete face at the lake's western outflow.
            art.Box(root, "Dam_Wall", new Vector3(0, 9f, 0),
                new Vector3(22f, 18f, 135f), Concrete);

            art.Box(root, "Dam_Crest", new Vector3(0, 18.6f, 0),
                new Vector3(27f, .9f, 138f), Dark);

            for (int i = 0; i < 7; i++)
            {
                art.Box(root, "Spillway",
                    new Vector3(12f, 7f, (i - 3) * 19f),
                    new Vector3(3f, 14f, 10f), Dark);
            }
        }

        static void Airfield(Transform root, SectorArt art)
        {
            art.Box(root, "Runway",
                new Vector3(0, .14f, 0),
                new Vector3(48f, .28f, 630f), Dark);

            for (int i = -9; i <= 9; i++)
            {
                art.Box(root, "Runway_Marker",
                    new Vector3(0, .31f, i * 28f),
                    new Vector3(.9f, .04f, 12f), White);
            }

            Building(root, art, new Vector3(90f, 0, -120f),
                48f, 55f, 11f, Concrete, "Aircraft_Hangar");

            art.Shape(root, "Radar_Antenna", PrimitiveType.Cylinder,
                new Vector3(-85f, 13f, 120f),
                new Vector3(.55f, 13f, .55f), Concrete);
        }

        static void HingedDoor(
            Transform parent, SectorArt art,
            float frontZ, float width, float height, Color color)
        {
            var hinge = new GameObject("Hinged_Door").transform;
            hinge.SetParent(parent, false);
            hinge.localPosition = new Vector3(-width * .5f, 0, frontZ);
            hinge.gameObject.AddComponent<SectorDoor>();

            art.Box(
                hinge, "Door_Panel",
                new Vector3(width * .5f, height * .5f, 0),
                new Vector3(width - .07f, height, .10f),
                color);

            art.Shape(
                hinge, "Handle",
                PrimitiveType.Sphere,
                new Vector3(width - .25f, height * .48f, -.12f),
                Vector3.one * .13f,
                new Color(.75f, .70f, .55f), false);
        }

        static void BasicHouse(
            Transform parent, SectorArt art,
            Vector3 position, float width, float depth,
            float height, int seed)
        {
            var root = new GameObject("House_" + seed).transform;
            root.SetParent(parent, false);
            root.localPosition = position;

            float front = -depth * .5f;
            float door = 1.6f;
            float side = (width - door) * .5f;

            art.Box(root, "Floor", new Vector3(0, .18f, 0),
                new Vector3(width, .36f, depth), Concrete);
            art.Box(root, "Rear_Wall", new Vector3(0, height * .5f, -front),
                new Vector3(width, height, .3f), Wall);
            art.Box(root, "Left_Wall", new Vector3(-width * .5f, height * .5f, 0),
                new Vector3(.3f, height, depth), Wall);
            art.Box(root, "Right_Wall", new Vector3(width * .5f, height * .5f, 0),
                new Vector3(.3f, height, depth), Wall);
            art.Box(root, "Front_Left", new Vector3(-door * .5f - side * .5f, height * .5f, front),
                new Vector3(side, height, .3f), Wall);
            art.Box(root, "Front_Right", new Vector3(door * .5f + side * .5f, height * .5f, front),
                new Vector3(side, height, .3f), Wall);
            art.Box(root, "Door_Header",
                new Vector3(0, 2.3f + (height - 2.3f) * .5f, front),
                new Vector3(door, height - 2.3f, .3f), Wall);

            HingedDoor(root, art, front, door, 2.28f, Wood);

            // Roof consists of two actual sloped panels.
            float pitch = 28f;
            float panelWidth = width * .5f / Mathf.Cos(pitch * Mathf.Deg2Rad);
            float rise = Mathf.Tan(pitch * Mathf.Deg2Rad) * width * .5f;
            for (int sideSign = -1; sideSign <= 1; sideSign += 2)
            {
                var panel = art.Shape(root, "Roof",
                    PrimitiveType.Cube,
                    new Vector3(sideSign * width * .25f, height + rise * .5f, 0),
                    new Vector3(panelWidth + .4f, .26f, depth + .6f), Dark);

                panel.transform.localRotation =
                    Quaternion.Euler(0, 0, sideSign * pitch);
            }

            art.Box(root, "Front_Window_Left",
                new Vector3(-width * .27f, height * .57f, front - .22f),
                new Vector3(1.8f, 1.4f, .1f), Glass);
            art.Box(root, "Front_Window_Right",
                new Vector3(width * .27f, height * .57f, front - .22f),
                new Vector3(1.8f, 1.4f, .1f), Glass);

            art.Box(root, "Porch",
                new Vector3(0, .22f, front - 1f),
                new Vector3(3.2f, .42f, 2f), Wood);
        }

        static void Building(
            Transform parent, SectorArt art, Vector3 offset,
            float width, float depth, float height,
            Color color, string name)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = offset;

            const float thickness = .35f;
            float front = -depth * .5f;
            float entryWidth = Mathf.Min(5.5f, width * .25f);
            float sideWidth = (width - entryWidth) * .5f;
            float doorwayHeight = Mathf.Min(4f, height * .65f);

            art.Box(root, "Floor",
                new Vector3(0, .15f, 0),
                new Vector3(width, .3f, depth), Concrete);

            art.Box(root, "Rear",
                new Vector3(0, height * .5f, -front),
                new Vector3(width, height, thickness), color);

            art.Box(root, "Left",
                new Vector3(-width * .5f, height * .5f, 0),
                new Vector3(thickness, height, depth), color);

            art.Box(root, "Right",
                new Vector3(width * .5f, height * .5f, 0),
                new Vector3(thickness, height, depth), color);

            art.Box(root, "Front_Left",
                new Vector3(-entryWidth * .5f - sideWidth * .5f, height * .5f, front),
                new Vector3(sideWidth, height, thickness), color);

            art.Box(root, "Front_Right",
                new Vector3(entryWidth * .5f + sideWidth * .5f, height * .5f, front),
                new Vector3(sideWidth, height, thickness), color);

            art.Box(root, "Entry_Header",
                new Vector3(0, doorwayHeight + (height - doorwayHeight) * .5f, front),
                new Vector3(entryWidth, height - doorwayHeight, thickness), color);

            // Reserve usable doorway width for vehicles in hangars,
            // but still provide functional interaction for small buildings.
            if (entryWidth <= 5.6f && width < 40f)
                HingedDoor(root, art, front, entryWidth, doorwayHeight, Dark);

            art.Box(root, "Roof",
                new Vector3(0, height + .16f, 0),
                new Vector3(width + .8f, .32f, depth + .8f), Dark);

            int windows = Mathf.Clamp(Mathf.FloorToInt(width / 9f), 1, 8);
            for (int i = 0; i < windows; i++)
            {
                float x = (i + .5f) * width / windows - width * .5f;

                art.Shape(root, "Window_Glass",
                    PrimitiveType.Cube,
                    new Vector3(x, height * .64f, front - .22f),
                    new Vector3(
                        Mathf.Min(2.8f, width / (windows * 2f)),
                        Mathf.Min(1.8f, height * .22f),
                        .08f),
                    Glass, false);
            }
        }
    }
}
