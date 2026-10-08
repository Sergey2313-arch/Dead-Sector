using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// Approved 8 x 8 km map concept (8 October 2026).
    /// These are DESIGN coordinates based on the approved illustrated map,
    /// not measurements of existing Unity meshes.
    ///
    /// Unity +X = east, +Z = north. Each grid cell is 1 km.
    /// The current playable settlement is a prototype around (0, 0)
    /// and will be progressively aligned with this plan.
    /// </summary>
    public static class SectorMapPlan
    {
        public const string Version = "world-map-v1";
        public const int MapSizeMetres = 8000;

        public enum BuildStatus
        {
            Planned,
            Prototype
        }

        public enum LocationKind
        {
            Settlement,
            Industry,
            Medical,
            Transport,
            Military,
            Wilderness,
            Hazard,
            Landmark
        }

        public readonly struct Location
        {
            public readonly string Id;
            public readonly string Name;
            public readonly Vector2 MapPosition;
            public readonly LocationKind Kind;
            public readonly BuildStatus Status;

            public Location(
                string id,
                string name,
                Vector2 mapPosition,
                LocationKind kind,
                BuildStatus status = BuildStatus.Planned)
            {
                Id = id;
                Name = name;
                MapPosition = mapPosition;
                Kind = kind;
                Status = status;
            }

            public Vector3 AtHeight(float y)
            {
                return new Vector3(MapPosition.x, y, MapPosition.y);
            }

            public string GridCell => SectorMapPlan.GridCell(MapPosition);
        }

        // Visual placement follows the approved illustration.
        // Prototype status means some gameplay geometry of this CATEGORY
        // exists near the central spawn; NOT that this exact map position
        // is already constructed.
        static readonly Location[] ApprovedLocations =
        {
            new Location("mountain_lake", "Горное озеро", new Vector2(-2650, 2850), LocationKind.Wilderness),
            new Location("forest_camp", "Лесной лагерь", new Vector2(-750, 3100), LocationKind.Wilderness),
            new Location("radio_station", "Радиовышка", new Vector2(720, 3050), LocationKind.Landmark, BuildStatus.Prototype),
            new Location("quarry", "Карьер", new Vector2(3050, 3150), LocationKind.Hazard),
            new Location("abandoned_city", "Заброшенный город", new Vector2(3500, 2100), LocationKind.Settlement),
            new Location("village", "Деревня", new Vector2(-2700, 1450), LocationKind.Settlement, BuildStatus.Prototype),
            new Location("town", "Городок", new Vector2(-700, 1500), LocationKind.Settlement),
            new Location("clinic", "Клиника", new Vector2(550, 1500), LocationKind.Medical, BuildStatus.Prototype),
            new Location("gas_station", "АЗС", new Vector2(1900, 850), LocationKind.Transport, BuildStatus.Prototype),
            new Location("military_checkpoint", "Военный КПП", new Vector2(3700, 750), LocationKind.Military, BuildStatus.Prototype),
            new Location("garages", "Гаражи", new Vector2(-2100, 300), LocationKind.Industry, BuildStatus.Prototype),
            new Location("factory", "Завод (промзона)", new Vector2(-350, 100), LocationKind.Industry, BuildStatus.Prototype),
            new Location("warehouse", "Складской комплекс", new Vector2(2350, -250), LocationKind.Industry, BuildStatus.Prototype),
            new Location("construction", "Стройка", new Vector2(-250, -1350), LocationKind.Industry),
            new Location("dam", "Дамба", new Vector2(-2250, -2450), LocationKind.Landmark),
            new Location("reservoir", "Большое озеро", new Vector2(-250, -2900), LocationKind.Wilderness),
            new Location("airfield", "Аэродром", new Vector2(3050, -2850), LocationKind.Transport)
        };

        public static IReadOnlyList<Location> Locations => ApprovedLocations;

        // These are conceptual road corridors. The existing prototype's
        // crossroads at x=300 and z=0 remain active until replacement roads
        // and the navigation mesh are ready.
        static readonly Vector2[][] PlannedRoadRoutes =
        {
            new[]
            {
                new Vector2(-3650, 1500), new Vector2(-2650, 1400),
                new Vector2(-1800, 1200), new Vector2(-700, 1400),
                new Vector2(550, 1250), new Vector2(1900, 800),
                new Vector2(3650, 700)
            },
            new[]
            {
                new Vector2(-2100, 300), new Vector2(-1200, 200),
                new Vector2(-350, 100), new Vector2(750, -200),
                new Vector2(2350, -250)
            },
            new[]
            {
                new Vector2(-250, -1350), new Vector2(-300, -600),
                new Vector2(-350, 100), new Vector2(-700, 1400),
                new Vector2(-750, 3100)
            },
            new[]
            {
                new Vector2(2350, -250), new Vector2(2550, -1250),
                new Vector2(3050, -2850)
            },
            new[]
            {
                new Vector2(-2250, -2450), new Vector2(-2000, -1400),
                new Vector2(-2100, 300), new Vector2(-2700, 1450)
            }
        };

        public static IReadOnlyList<Vector2[]> RoadRoutes => PlannedRoadRoutes;

        public static string GridCell(Vector2 position)
        {
            int x = Mathf.Clamp(
                Mathf.FloorToInt((position.x + 4000f) / 1000f), 0, 7);
            int z = Mathf.Clamp(
                Mathf.FloorToInt((4000f - position.y) / 1000f), 0, 7);

            return string.Format("{0}{1}", (char)('A' + x), z + 1);
        }

        public static bool IsWithinWorld(Vector2 position)
        {
            return position.x >= -4000f &&
                   position.x <= 4000f &&
                   position.y >= -4000f &&
                   position.y <= 4000f;
        }

        public static Location FindById(string id)
        {
            foreach (Location location in ApprovedLocations)
            {
                if (string.Equals(location.Id, id, StringComparison.Ordinal))
                    return location;
            }

            throw new ArgumentException("Unknown map location: " + id, nameof(id));
        }
    }
}
