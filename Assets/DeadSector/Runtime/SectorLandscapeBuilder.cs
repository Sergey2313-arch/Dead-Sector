using System.Collections.Generic;
using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// Tile-local roads and water mesh surfaces generated from world coordinates.
    /// A tile owns its meshes so unloading a streamed tile also unloads features.
    /// </summary>
    public static class SectorLandscapeBuilder
    {
        const float RoadHalfWidth = 5.3f;
        const int WaterGrid = 64;

        public static void Build(
            Transform tileRoot,
            Vector3 tileOrigin,
            Material roadMaterial,
            Material waterMaterial)
        {
            BuildRoads(tileRoot, tileOrigin, roadMaterial);
            BuildWater(tileRoot, tileOrigin, waterMaterial);
        }

        static void BuildRoads(
            Transform tileRoot,
            Vector3 origin,
            Material material)
        {
            int routeIndex = 0;

            foreach (Vector2[] route in SectorMapPlan.RoadRoutes)
            {
                for (int i = 1; i < route.Length; i++)
                {
                    Vector2 start = route[i - 1];
                    Vector2 end = route[i];

                    if (!ClipSegment(
                        start, end,
                        origin.x, origin.z, SectorLayout.TileSize,
                        out float begin, out float finish))
                        continue;

                    Vector2 a = Vector2.Lerp(start, end, begin);
                    Vector2 b = Vector2.Lerp(start, end, finish);

                    BuildRoadStrip(
                        tileRoot,
                        origin,
                        a, b,
                        material,
                        "Road_" + routeIndex + "_" + i);
                }

                routeIndex++;
            }
        }

        static void BuildRoadStrip(
            Transform tileRoot,
            Vector3 origin,
            Vector2 a, Vector2 b,
            Material material,
            string name)
        {
            Vector2 line = b - a;
            float length = line.magnitude;
            if (length < .5f)
                return;

            Vector2 sideways = new Vector2(-line.y, line.x) / length;
            int steps = Mathf.Max(2, Mathf.CeilToInt(length / 11f));

            Vector3[] vertices = new Vector3[(steps + 1) * 2];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[steps * 6];

            for (int i = 0; i <= steps; i++)
            {
                float fraction = (float)i / steps;
                Vector2 center = Vector2.Lerp(a, b, fraction);

                for (int edge = 0; edge < 2; edge++)
                {
                    Vector2 p = center + sideways * (edge == 0 ? -RoadHalfWidth : RoadHalfWidth);
                    float y = SectorLayout.Height(p.x, p.y) + .18f;

                    if (SectorGeography.TryGetWaterLevel(p.x, p.y, out float water))
                        y = Mathf.Max(y, water + 1.95f);

                    int index = i * 2 + edge;
                    vertices[index] = new Vector3(
                        p.x - origin.x, y, p.y - origin.z);

                    uv[index] = new Vector2(edge, fraction * length / 7f);
                }

                if (i == steps)
                    continue;

                int t = i * 6;
                int first = i * 2;

                triangles[t + 0] = first;
                triangles[t + 1] = first + 2;
                triangles[t + 2] = first + 1;
                triangles[t + 3] = first + 1;
                triangles[t + 4] = first + 2;
                triangles[t + 5] = first + 3;
            }

            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject road = new GameObject(name);
            road.transform.SetParent(tileRoot, false);

            MeshFilter filter = road.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = road.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;

            MeshCollider collider = road.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
        }

        static void BuildWater(
            Transform tileRoot,
            Vector3 origin,
            Material material)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uv = new List<Vector2>();

            float step = SectorLayout.TileSize / WaterGrid;

            for (int z = 0; z < WaterGrid; z++)
            {
                for (int x = 0; x < WaterGrid; x++)
                {
                    float worldX = origin.x + (x + .5f) * step;
                    float worldZ = origin.z + (z + .5f) * step;

                    if (!SectorGeography.TryGetWaterLevel(
                        worldX, worldZ, out float level))
                        continue;

                    int start = vertices.Count;
                    float lx = x * step;
                    float lz = z * step;

                    vertices.Add(new Vector3(lx, level, lz));
                    vertices.Add(new Vector3(lx + step, level, lz));
                    vertices.Add(new Vector3(lx, level, lz + step));
                    vertices.Add(new Vector3(lx + step, level, lz + step));

                    uv.Add(new Vector2(worldX / 30f, worldZ / 30f));
                    uv.Add(new Vector2((worldX + step) / 30f, worldZ / 30f));
                    uv.Add(new Vector2(worldX / 30f, (worldZ + step) / 30f));
                    uv.Add(new Vector2((worldX + step) / 30f, (worldZ + step) / 30f));

                    // Faces point upwards.
                    triangles.Add(start);
                    triangles.Add(start + 2);
                    triangles.Add(start + 1);
                    triangles.Add(start + 1);
                    triangles.Add(start + 2);
                    triangles.Add(start + 3);
                }
            }

            if (triangles.Count == 0)
                return;

            Mesh mesh = new Mesh();
            mesh.name = "Water_Surface";
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject water = new GameObject("Water_Surface");
            water.transform.SetParent(tileRoot, false);
            MeshFilter filter = water.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = water.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;

            // No water collider yet; swimming and buoyancy are future gameplay.
        }

        static bool ClipSegment(
            Vector2 a, Vector2 b,
            float minX, float minZ, float size,
            out float start, out float end)
        {
            start = 0f;
            end = 1f;
            Vector2 d = b - a;

            if (!Clip(-d.x, a.x - minX, ref start, ref end) ||
                !Clip(d.x, minX + size - a.x, ref start, ref end) ||
                !Clip(-d.y, a.y - minZ, ref start, ref end) ||
                !Clip(d.y, minZ + size - a.y, ref start, ref end))
                return false;

            return start < end;
        }

        static bool Clip(float p, float q, ref float a, ref float b)
        {
            if (Mathf.Abs(p) < .00001f)
                return q >= 0f;

            float r = q / p;

            if (p < 0f)
            {
                if (r > b) return false;
                if (r > a) a = r;
            }
            else
            {
                if (r < a) return false;
                if (r < b) b = r;
            }

            return true;
        }
    }
}
