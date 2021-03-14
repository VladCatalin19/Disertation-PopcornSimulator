using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Popcorn.Slicer
{
	public class MeshData
	{
		private IList<Vector3> vertices;
		private IList<Vector3> normals;
		private IList<Vector2> uvs;
		private IList<Vector4> tangents;
		private IList<Triangle> triangles;

		public MeshData(Vector3[] vertices, Vector3[] normals, Vector2[] uvs,
			Vector4[] tangents, int[] triangles)
		{
			this.vertices = new List<Vector3>(vertices);
			this.normals = new List<Vector3>(normals);
			this.uvs = new List<Vector2>(uvs);
			this.tangents = new List<Vector4>(tangents);

			this.triangles = new List<Triangle>(triangles.Length / 3);
			for (int i = 0; i < triangles.Length; i += 3)
			{
				Triangle t = new Triangle(triangles[i], triangles[i + 1], triangles[i + 2]);
				this.triangles.Add(t);
			}
		}

		public MeshData(Mesh mesh)
			: this(mesh.vertices, mesh.normals, mesh.uv, mesh.tangents, mesh.triangles) {}

		public IList<Vector3> Vertices { get => vertices; }
		public IList<Vector3> Normals { get => normals; }
		public IList<Vector2> UVs { get => uvs; }
		public IList<Vector4> Tangents { get => tangents; }
		public IList<Triangle> Triangles { get => triangles; }

		public void AddVertex(Vector3 vertex)
		{
			vertices.Add(vertex);
		}

		public void AddVertex(Vector3 vertex, Vector3 normal, Vector2 uv, Vector4 tangent)
		{
			vertices.Add(vertex);
			normals.Add(normal);
			uvs.Add(uv);
			tangents.Add(tangent);
		}

		public void AddVertex(Vertex vertex)
		{
			vertices.Add(vertex.Pos);
			normals.Add(vertex.Norm);
			uvs.Add(vertex.UV);
			tangents.Add(vertex.Tan);
		}

		public Vertex GetVertexAt(int index)
		{
			Vector3 pos = vertices[index];
			Vector3 norm = normals[index];
			Vector2 uv = uvs[index];
			Vector4 tan = tangents[index];
		
			return new Vertex(pos, norm, uv, tan);
		}

		public void AddTriangle(Triangle triangle)
		{
			triangles.Add(triangle);
		}

		public void RemoveTriangle(Triangle triangle)
		{
			triangles.Remove(triangle);
		}

		public void RemoveTriangleAt(int index)
		{
			triangles.RemoveAt(index);
		}

		public void ToMesh(Mesh mesh)
		{
			mesh.Clear();

			mesh.vertices = vertices.ToArray();
			mesh.normals = normals.ToArray();
			mesh.uv = uvs.ToArray();
			mesh.tangents = tangents.ToArray();
			mesh.triangles = TrianglesToIntArray();
		}

		private int[] TrianglesToIntArray()
		{
			int[] trianglesIndices = new int[triangles.Count * 3];

			for (int i = 0; i < triangles.Count; ++i)
			{
				Triangle t = triangles[i];
				trianglesIndices[3 * i] = t.I0;
				trianglesIndices[3 * i + 1] = t.I1;
				trianglesIndices[3 * i + 2] = t.I2;
			}

			return trianglesIndices;
		}
	}
}
