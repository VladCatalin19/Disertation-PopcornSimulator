using System.Collections.Generic;
using UnityEngine;

namespace PopcornGenerator
{
    internal class Mesh
    {
        public const int kernelSubmeshIndex = 0;
        public const int puffSubmeshIndex = 1;

        private readonly List<Vertex> vertices;
        private readonly List<List<Triangle>> subMeshTriangles;
        private readonly bool hasNormals;
        private readonly bool hasUVs;

        public Mesh(int vertexCapacity = 0, int triangleCapacity = 0, bool hasNormals = false, bool hasUVs = false)
        {
            vertices = new List<Vertex>(vertexCapacity);
            subMeshTriangles = new List<List<Triangle>>(1);
            subMeshTriangles.Add(new List<Triangle>(triangleCapacity));
            this.hasNormals = hasNormals;
            this.hasUVs = hasUVs;
        }

        public Mesh(List<Vertex> vertices, List<List<Triangle>> subMeshTriangles, bool hasNormals = false, bool hasUVs = false)
        {
            this.vertices = vertices;
            this.subMeshTriangles = subMeshTriangles;
            this.hasNormals = hasNormals;
            this.hasUVs = hasUVs;
        }

        public Mesh(UnityEngine.Mesh mesh)
            : this(mesh.vertices, mesh.normals, mesh.uv, mesh.triangles) { }

        public Mesh(Vector3[] vertices, Vector3[] normals, Vector2[] uvs, int[] triangles)
        {
            this.vertices = new List<Vertex>(vertices.Length);
            for (int vertexIndex = 0; vertexIndex < vertices.Length; ++vertexIndex)
            {
                Vector3 position = vertices[vertexIndex];
                Vector3 normal = ((normals != null) && (vertexIndex < normals.Length)) ? normals[vertexIndex] : Vector3.zero;
                Vector2 uv = ((uvs != null) && (vertexIndex < uvs.Length)) ? uvs[vertexIndex] : Vector2.zero;

                this.vertices.Add(new Vertex(position, normal, uv));
            }

            var trianglesList = new List<Triangle>(triangles.Length / 3);
            for (int triangleIndex = 0; triangleIndex < triangles.Length; triangleIndex += 3)
            {
                int i0 = triangles[triangleIndex];
                int i1 = triangles[triangleIndex + 1];
                int i2 = triangles[triangleIndex + 2];

                trianglesList.Add(new Triangle(i0, i1, i2));
            }

            subMeshTriangles = new List<List<Triangle>>
            {
                trianglesList
            };

            hasNormals = ((normals != null) && (normals.Length == vertices.Length));
            hasUVs = ((uvs != null) && (uvs.Length == vertices.Length));
        }

        public List<Vertex> Vertices { get => vertices; }
        public List<List<Triangle>> SubMeshTriangles { get => subMeshTriangles; }
        public bool HasNormals { get => hasNormals; }
        public bool HasUVs { get => hasUVs; }

        public UnityEngine.Mesh ToUnityMesh()
        {
            Vector3[] unityVertices = new Vector3[vertices.Count];
            Vector3[] unityNormals = hasNormals ? new Vector3[vertices.Count] : null;
            Vector2[] unityUVs = hasUVs ? new Vector2[vertices.Count] : null;

            for (int vertexIndex = 0; vertexIndex < vertices.Count; ++vertexIndex)
            {
                unityVertices[vertexIndex] = vertices[vertexIndex].Position;
                if (hasNormals)
                {
                    unityNormals[vertexIndex] = vertices[vertexIndex].Normal;
                }
                if (hasUVs)
                {
                    unityUVs[vertexIndex] = vertices[vertexIndex].UV;
                }
            }

            UnityEngine.Mesh unityMesh = new UnityEngine.Mesh
            {
                vertices = unityVertices,
                normals = unityNormals,
                uv = unityUVs,
                subMeshCount = subMeshTriangles.Count,
            };

            for (int submeshIndex = 0; submeshIndex < subMeshTriangles.Count; ++submeshIndex)
            {
                var triangles = subMeshTriangles[submeshIndex];
                int[] unityTriangles = new int[triangles.Count * 3];
                for (int triangleIndex = 0; triangleIndex < triangles.Count; ++triangleIndex)
                {
                    unityTriangles[3 * triangleIndex] = triangles[triangleIndex].I0;
                    unityTriangles[3 * triangleIndex + 1] = triangles[triangleIndex].I1;
                    unityTriangles[3 * triangleIndex + 2] = triangles[triangleIndex].I2;
                }
                unityMesh.SetTriangles(unityTriangles, submeshIndex);
            }

            return unityMesh;
        }
    }
}
