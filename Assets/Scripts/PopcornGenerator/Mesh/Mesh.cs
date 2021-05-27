using System.Collections.Generic;
using UnityEngine;

namespace PopcornGenerator
{
	internal class Mesh
	{
		private readonly IList<Vertex> vertices;
		private readonly IList<Triangle> triangles;
		private readonly bool hasNormals;
		private readonly bool hasUVs;

		public Mesh(bool hasNormals = false, bool hasUVs = false)
		{
			vertices = new List<Vertex>();
			triangles = new List<Triangle>();
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
				Vector3? normal = normals?[vertexIndex];
				Vector2? uv = uvs?[vertexIndex];

				this.vertices.Add(new Vertex(position, normal, uv));
			}

			this.triangles = new List<Triangle>(triangles.Length / 3);
			for (int triangleIndex = 0; triangleIndex < triangles.Length; triangleIndex += 3)
			{
				int i0 = triangles[triangleIndex];
				int i1 = triangles[triangleIndex + 1];
				int i2 = triangles[triangleIndex + 2];

				this.triangles.Add(new Triangle(i0, i1, i2));
			}

			hasNormals = normals != null && normals.Length == vertices.Length;
			hasUVs = uvs != null && uvs.Length == vertices.Length;
		}

		public IList<Vertex> Vertices { get => vertices; }
		public IList<Triangle> Triangles { get => triangles; }
		public bool HasNormals { get => hasNormals; }
		public bool HasUVs { get => hasUVs; }

		public UnityEngine.Mesh ToUnityMesh()
		{
			UnityEngine.Mesh unityMesh = new UnityEngine.Mesh();
			
			Vector3[] unityVertices = new Vector3[vertices.Count];
			Vector3[] unityNormals = hasNormals ? new Vector3[vertices.Count] : null;
			Vector2[] unityUVs = hasUVs ? new Vector2[vertices.Count] : null;
			int[] unityTriangles = new int[triangles.Count * 3];

			for (int vertexIndex = 0; vertexIndex < vertices.Count; ++vertexIndex)
			{
				unityVertices[vertexIndex] = vertices[vertexIndex].Position;
				if (hasNormals)
				{
					unityNormals[vertexIndex] = vertices[vertexIndex].Normal.Value;
				}
				if (hasUVs)
				{
					unityUVs[vertexIndex] = vertices[vertexIndex].UV.Value;
				}
			}

			for (int triangleIndex = 0; triangleIndex < triangles.Count; ++triangleIndex)
			{
				unityTriangles[3 * triangleIndex] = triangles[triangleIndex].I0;
				unityTriangles[3 * triangleIndex + 1] = triangles[triangleIndex].I1;
				unityTriangles[3 * triangleIndex + 2] = triangles[triangleIndex].I2;
			}

			unityMesh.vertices = unityVertices;
			unityMesh.normals = unityNormals;
			unityMesh.uv = unityUVs;
			unityMesh.triangles = unityTriangles;
			return unityMesh;
		}
	}
}
