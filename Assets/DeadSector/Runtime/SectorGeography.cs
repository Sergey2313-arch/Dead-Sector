using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// Deterministic world-space height, road and water queries.
    /// Pure functions keep neighbouring 1 km tiles seamless and the map,
    /// terrain and generated features aligned.
    /// </summary>
    public static class SectorGeography
    {
        public const float LowWaterLevel = 44f;
        public const float MountainWaterLevel = 112f;

        public static float Height(float x, float z)
        {
            float ground = BaseHeight(x, z);

            // Keep the existing central playable prototype undisturbed.
            // The temporary village/factory were hand-positioned at Y=60.
            float existingZone = Mathf.SmoothStep(
                1f, 0f, Mathf.InverseLerp(470f, 610f, new Vector2(x, z).magnitude));
            ground = Mathf.Lerp(ground, 60f, existingZone);

            // Flatten discrete future settlement / landmark pads without
            // changing heights outside their construction zones.
            foreach (SectorMapPlan.Location poi in SectorMapPlan.Locations)
            {
                if (poi.Kind == SectorMapPlan.LocationKind.Wilderness ||
                    poi.Id == "dam" || poi.Id == "factory")
                    continue;

                float radius = SiteRadius(poi.Id);
                if (radius <= 0f)
                    continue;

                float distance = Vector2.Distance(
                    new Vector2(x, z), poi.MapPosition);

                if (distance >= radius * 1.4f)
                    continue;

                float centerHeight = BaseHeight(
                    poi.MapPosition.x, poi.MapPosition.y);

                float flatten = 1f - Mathf.SmoothStep(
                    0f, 1f, Mathf.InverseLerp(radius * .70f, radius * 1.4f, distance));

                ground = Mathf.Lerp(ground, centerHeight, flatten);
            }

            // Rough quarry excavation, with terraces reserved for later.
            Vector2 quarryCenter = SectorMapPlan.FindById("quarry").MapPosition;
            float quarryDistance = Vector2.Distance(
                new Vector2(x, z), quarryCenter);

            if (quarryDistance < 215f)
            {
                float excavation = 1f - Mathf.SmoothStep(
                    0f, 1f, Mathf.InverseLerp(85f, 215f, quarryDistance));

                ground -= excavation * 28f;
            }

            // Carve shorelines as actual terrain depressions, not painted
            // blue splats. Wet surfaces are generated separately per tile.
            ground = CarveLake(
                ground, x, z,
                -2650f, 2850f,
                480f, 365f, MountainWaterLevel);

            ground = CarveLake(
                ground, x, z,
                -250f, -2900f,
                940f, 565f, LowWaterLevel);

            // Outflow channel joins the dam near B7 to the southern lake.
            float spill = DistanceToSegment(
                new Vector2(x, z),
                new Vector2(-2250f, -2450f),
                new Vector2(-780f, -2780f));

            float spillBank = 1f - Mathf.SmoothStep(
                0f, 1f, Mathf.InverseLerp(70f, 175f, spill));
            ground = Mathf.Lerp(ground, LowWaterLevel - 5f, spillBank);

            if (z >= -3400f && z <= 3570f)
            {
                float dx = Mathf.Abs(x - RiverX(z));
                float bank = 1f - Mathf.SmoothStep(
                    0f, 1f, Mathf.InverseLerp(45f, 160f, dx));

                float terminal = Mathf.Min(
                    Mathf.InverseLerp(-3400f, -3160f, z),
                    1f - Mathf.InverseLerp(3350f, 3570f, z));

                bank *= Mathf.Clamp01(terminal);
                ground = Mathf.Lerp(ground, LowWaterLevel - 6f, bank);
            }

            // The only roads intentionally forced to exactly Y=60 are
            // short historic prototype roads near the original spawn.
            float oldRoad = Mathf.Min(Mathf.Abs(z), Mathf.Abs(x - 300f));
            float legacyArea = 1f - Mathf.SmoothStep(
                0f, 1f, Mathf.InverseLerp(400f, 560f, Mathf.Max(Mathf.Abs(x), Mathf.Abs(z))));

            float oldRoadWeight = (1f - Mathf.SmoothStep(
                0f, 1f, Mathf.InverseLerp(10f, 32f, oldRoad))) * legacyArea;

            ground = Mathf.Lerp(ground, 60f, oldRoadWeight);
            return Mathf.Clamp(ground, 2f, 480f);
        }

        public static float BaseHeight(float x, float z)
        {
            float hills = 25f +
                Mathf.PerlinNoise((x + 18000f) / 1800f, (z + 13000f) / 1800f) * 130f;

            hills += Mathf.PerlinNoise(
                (x + 17000f) / 420f, (z + 11000f) / 420f) * 16f;

            float edge = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
            hills += Mathf.SmoothStep(
                0f, 150f, Mathf.InverseLerp(3000f, 3950f, edge));

            float valley = Mathf.SmoothStep(
                0f, 1f, Mathf.InverseLerp(
                    500f, 950f, new Vector2(x, z).magnitude));

            return Mathf.Lerp(60f, hills, valley);
        }

        static float CarveLake(
            float height, float x, float z,
            float cx, float cz, float rx, float rz, float surface)
        {
            float radial = Mathf.Sqrt(
                Mathf.Pow((x - cx) / rx, 2f) +
                Mathf.Pow((z - cz) / rz, 2f));

            if (radial >= 1.42f)
                return height;

            float weight = 1f - Mathf.SmoothStep(
                0f, 1f, Mathf.InverseLerp(.82f, 1.42f, radial));

            float basin = surface - 8f + radial * 2f;
            return Mathf.Lerp(height, basin, weight);
        }

        public static float RiverX(float z)
        {
            float transition = Mathf.SmoothStep(
                0f, 1f, Mathf.InverseLerp(-3100f, -1750f, z));

            float nearLake = -250f + 85f * Mathf.Sin(z / 290f);
            float mainland = 1120f + 210f * Mathf.Sin(z / 640f);
            return Mathf.Lerp(nearLake, mainland, transition);
        }

        public static bool TryGetWaterLevel(float x, float z, out float level)
        {
            float mountainLake =
                Mathf.Pow((x + 2650f) / (480f * .82f), 2f) +
                Mathf.Pow((z - 2850f) / (365f * .82f), 2f);

            if (mountainLake < 1f)
            {
                level = MountainWaterLevel;
                return true;
            }

            float southernLake =
                Mathf.Pow((x + 250f) / (940f * .82f), 2f) +
                Mathf.Pow((z + 2900f) / (565f * .82f), 2f);

            if (southernLake < 1f)
            {
                level = LowWaterLevel;
                return true;
            }

            if (DistanceToSegment(
                new Vector2(x, z),
                new Vector2(-2250f, -2450f),
                new Vector2(-780f, -2780f)) <= 58f)
            {
                level = LowWaterLevel;
                return true;
            }

            if (z >= -3160f && z <= 3350f &&
                Mathf.Abs(x - RiverX(z)) <= 36f)
            {
                level = LowWaterLevel;
                return true;
            }

            level = 0f;
            return false;
        }

        public static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 segment = b - a;
            float lengthSquared = segment.sqrMagnitude;

            if (lengthSquared <= .0001f)
                return Vector2.Distance(p, a);

            float t = Mathf.Clamp01(Vector2.Dot(p - a, segment) / lengthSquared);
            return Vector2.Distance(p, a + segment * t);
        }

        public static float DistanceToRoad(float x, float z)
        {
            Vector2 p = new Vector2(x, z);
            float distance = float.PositiveInfinity;

            foreach (Vector2[] road in SectorMapPlan.RoadRoutes)
            {
                for (int i = 1; i < road.Length; i++)
                {
                    Vector2 a = road[i - 1];
                    Vector2 delta = road[i] - a;
                    float projection = Mathf.Clamp01(
                        Vector2.Dot(p - a, delta) / delta.sqrMagnitude);

                    float current = Vector2.Distance(p, a + projection * delta);
                    if (current < distance)
                        distance = current;
                }
            }

            return distance;
        }

        static float SiteRadius(string id)
        {
            switch (id)
            {
                case "airfield": return 430f;
                case "abandoned_city": return 280f;
                case "town": return 210f;
                case "village": return 190f;
                case "quarry": return 175f;
                case "factory": return 190f;
                case "warehouse": return 150f;
                case "forest_camp": return 90f;
                default: return 90f;
            }
        }
    }
}
