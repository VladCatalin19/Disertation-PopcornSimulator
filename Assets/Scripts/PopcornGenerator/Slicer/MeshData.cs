using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PopcornGenerator.Slicer
{
	internal class MeshData
	{
		private IList<Vector3> vertices;
		private IList<Vector3> normals;
		private IList<Vector2> uvs;
		private IList<Vector4> tangents;
		private IList<Triangle> triangles;
		private IList<Slice> slices;

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

			slices = new List<Slice>();
		}

		public MeshData(Mesh mesh)
			: this(mesh.vertices, mesh.normals, mesh.uv, mesh.tangents, mesh.triangles) {}

		public IList<Vector3> Vertices { get => vertices; }
		public IList<Vector3> Normals { get => normals; }
		public IList<Vector2> UVs { get => uvs; }
		public IList<Vector4> Tangents { get => tangents; }
		public IList<Triangle> Triangles { get => triangles; }
		public IList<Slice> Slices { get => slices; }

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
			vertices.Add(vertex.Position);

			if(vertex.Normal.HasValue)
			{
				normals.Add(vertex.Normal.Value);
			}
			if (vertex.UV.HasValue)
			{
				uvs.Add(vertex.UV.Value);
			}
			if (vertex.Tangent.HasValue)
			{
				tangents.Add(vertex.Tangent.Value);
			}
		}

		public Vertex GetVertexAt(int index)
		{
			Vector3 pos = vertices[index];
			Vector3? norm = index < normals.Count ? (Vector3?)normals[index] : null;
			Vector2? uv = index < uvs.Count ? (Vector2?)uvs[index] : null;
			Vector4? tan = index < tangents.Count ? (Vector4?)tangents[index] : null;

			return new Vertex(pos, norm, uv, tan, index);
		}

		public void RemoveVertexAt(int index)
		{
			if (0 <= index && index < vertices.Count)
			{
				vertices.RemoveAt(index);

				if (index < normals.Count)
				{
					normals.RemoveAt(index);
				}
				if (index < uvs.Count)
				{
					uvs.RemoveAt(index);
				}
				if (index < tangents.Count)
				{
					tangents.RemoveAt(index);
				}
			}
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

		public void RemoveUnusedVertices()
		{
			short[] verticesFrequencies = CalculateVerticesFrequencies();
			short[] indicesSubstraction = CalculateIndicesSubstractions(verticesFrequencies);
			RemoveVerticesWithZeroFrequency(verticesFrequencies);
			FixTrianglesAfterRemovingVertices(indicesSubstraction);
			FixSlicesAfterRemovingVertices(indicesSubstraction, verticesFrequencies);
		}

		private short[] CalculateVerticesFrequencies()
		{
			short[] verticesFrequencies = new short[vertices.Count];
			foreach (Triangle triangle in triangles)
			{
				++verticesFrequencies[triangle.I0];
				++verticesFrequencies[triangle.I1];
				++verticesFrequencies[triangle.I2];
			}
			return verticesFrequencies;
		}

		private short[] CalculateIndicesSubstractions(short[] verticesFrequencies)
		{
			short[] indicesSubstraction = new short[vertices.Count];
			short currentSubstraction = 0;
			for (int i = 0; i < verticesFrequencies.Length; ++i)
			{
				if (verticesFrequencies[i] == 0)
				{
					++currentSubstraction;
				}
				indicesSubstraction[i] = currentSubstraction;
			}
			return indicesSubstraction;
		}

		private void RemoveVerticesWithZeroFrequency(short[] verticesFrequencies)
		{
			for (int i = vertices.Count - 1; i >= 0; --i)
			{
				if (verticesFrequencies[i] == 0)
				{
					RemoveVertexAt(i);
				}
			}
		}

		private void FixTrianglesAfterRemovingVertices(short[] indicesSubstraction)
		{
			for (int i = 0; i < triangles.Count; ++i)
			{
				int i0 = triangles[i].I0;
				int i1 = triangles[i].I1;
				int i2 = triangles[i].I2;

				i0 -= indicesSubstraction[i0];
				i1 -= indicesSubstraction[i1];
				i2 -= indicesSubstraction[i2];

				triangles[i] = new Triangle(i0, i1, i2);
			}
		}

		private void FixSlicesAfterRemovingVertices(short[] indicesSubstraction, short[] verticesFrequencies)
		{
			foreach (Slice slice in slices)
			{
				// WTH why should the indices be removed from here as well? There should not be any
				// duplicates here!

				for (int i = slice.AllIndices.Count - 1; i >= 0; --i)
				{
					if (verticesFrequencies[slice.AllIndices[i]] == 0)
					{
						slice.AllIndices.RemoveAt(i);
					}
				}
				for (int i = slice.BorderIndices.Count - 1; i >= 0; --i)
				{
					if (verticesFrequencies[slice.BorderIndices[i]] == 0)
					{
						slice.BorderIndices.RemoveAt(i);
					}
				}



				for (int index = 0; index < slice.AllIndices.Count; ++index)
				{
					slice.AllIndices[index] -= indicesSubstraction[slice.AllIndices[index]];
				}
				for (int index = 0; index < slice.BorderIndices.Count; ++index)
				{
					slice.BorderIndices[index] -= indicesSubstraction[slice.BorderIndices[index]];
				}
			}
		}
	}
}
