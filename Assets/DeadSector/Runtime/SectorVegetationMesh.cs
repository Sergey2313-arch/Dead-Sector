using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// Small reusable low-poly pine canopy mesh, not a heavy per-tree FBX.
    /// Mesh is built once per SectorWorld and shared between foliage tiers.
    /// </summary>
    public static class SectorVegetationMesh
    {
        public static Mesh CreateConiferCone(int sides = 10)
        {
            sides = Mathf.Clamp(sides, 5, 24);
            var vertices = new Vector3[sides + 2];
            var triangles = new int[sides * 6];

            vertices[0] = new Vector3(0f, 1f, 0f);
            for (int i = 0; i < sides; i++)
            {
                float angle = i * Mathf.PI * 2f / sides;
                vertices[i + 1] = new Vector3(
                    Mathf.Cos(angle) * .5f, 0f,
                    Mathf.Sin(angle) * .5f);
            }
            vertices[sides + 1] = Vector3.zero;

            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                int idx = i * 6;
                triangles[idx] = 0;
                triangles[idx + 1] = next + 1;
                triangles[idx + 2] = i + 1;
                triangles[idx + 3] = sides + 1;
                triangles[idx + 4] = i + 1;
                triangles[idx + 5] = next + 1;
            }

            var mesh = new Mesh { name = "Sector_Shared_Conifer_Cone" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
