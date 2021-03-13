using UnityEngine;

namespace Popcorn.Slicer
{
	public static class Slicer
	{
		public static void Slice(GameObject gameObject, UnityEngine.Plane[] cuttingPlanes)
		{
			if (!gameObject) throw new System.ArgumentNullException("gameObject");
			if (cuttingPlanes == null) throw new System.ArgumentNullException("cuttingPlanes");

			MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
			if (!meshFilter) throw new System.ArgumentException("Provided GameObject does not have a MeshFilter component");

			//MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
			//if (!meshRenderer) throw new System.ArgumentException("Provided GameObject does not have a MeshRenderer component");

			SliceMesh(meshFilter.mesh, cuttingPlanes, gameObject.transform);
		}

		private static void SliceMesh(Mesh mesh, UnityEngine.Plane[] cuttingPlanes, Transform spaceTransform)
		{
			MeshData meshData = new MeshData(mesh);

			TransformMeshVerticesToWorldSpace(meshData, spaceTransform);

			foreach (UnityEngine.Plane planeUnity in cuttingPlanes)
			{
				Plane plane = new Plane(planeUnity);
				int initialTriangleCount = meshData.Triangles.Count;

				for (int i = 0; i < initialTriangleCount; ++i)
				{
					SliceTriangleAndAddToMeshData(meshData, i, plane);
				}
			}

			TransformMeshVerticesToLocalSpace(meshData, spaceTransform);
			meshData.ToMesh(mesh);
		}

		private static void TransformMeshVerticesToWorldSpace(MeshData meshData, Transform transform)
		{
			for (int i = 0; i < meshData.Vertices.Count; ++i)
			{
				meshData.Vertices[i] = transform.TransformPoint(meshData.Vertices[i]);
			}
		}

		private static void TransformMeshVerticesToLocalSpace(MeshData meshData, Transform transform)
		{
			for (int i = 0; i < meshData.Vertices.Count; ++i)
			{
				meshData.Vertices[i] = transform.InverseTransformPoint(meshData.Vertices[i]);
			}
		}

		private static void SliceTriangleAndAddToMeshData(MeshData meshData, int triangleIndex, Plane plane)
		{
			Triangle triangle = meshData.Triangles[triangleIndex];
			Vertex v0 = meshData.GetVertexAt(triangle.I0);
			Vertex v1 = meshData.GetVertexAt(triangle.I1);
			Vertex v2 = meshData.GetVertexAt(triangle.I2);

			Plane.Side sidep0 = plane.GetSide(v0.Pos);
			Plane.Side sidep1 = plane.GetSide(v1.Pos);
			Plane.Side sidep2 = plane.GetSide(v2.Pos);

			if (!IsTriangleIntersectingPlane(sidep0, sidep1, sidep2)
				|| IsOneEdgeParallelToPlane(sidep0, sidep1, sidep2)
				|| IsOnePointOnPlaneAndOthersOnSameSide(sidep0, sidep1, sidep2))
			{
				return;
			}

			meshData.RemoveTriangleAt(triangleIndex);

			float t, t1, t2;
			if (IsOnePointOnPlaneAndOthersOnDifferentSides(sidep0, sidep1, sidep2))
			{
				if (sidep0 == Plane.Side.on && plane.Raycast(v1.Pos, v2.Pos, out t))
				{
					int i12 = AddInterpolatedVertedToMeshDataAndGetIndex(meshData, v1, v2, t);
					meshData.AddTriangle(new Triangle(triangle.I0, triangle.I1, i12));
					meshData.AddTriangle(new Triangle(triangle.I0, i12, triangle.I2));
				}
				else if (sidep1 == Plane.Side.on && plane.Raycast(v0.Pos, v2.Pos, out t))
				{
					int i02 = AddInterpolatedVertedToMeshDataAndGetIndex(meshData, v0, v2, t);
					meshData.AddTriangle(new Triangle(triangle.I0, triangle.I1, i02));
					meshData.AddTriangle(new Triangle(i02, triangle.I1, triangle.I2));
				}
				else if (sidep2 == Plane.Side.on && plane.Raycast(v0.Pos, v1.Pos, out t))
				{
					int i01 = AddInterpolatedVertedToMeshDataAndGetIndex(meshData, v0, v1, t);
					meshData.AddTriangle(new Triangle(triangle.I0, i01, triangle.I2));
					meshData.AddTriangle(new Triangle(i01, triangle.I1, triangle.I2));
				}
			}
			else
			{
				if (sidep0 != sidep1 && plane.Raycast(v0.Pos, v1.Pos, out t))
				{
					int i01 = AddInterpolatedVertedToMeshDataAndGetIndex(meshData, v0, v1, t);

					if (sidep0 == sidep2 && plane.Raycast(v1.Pos, v2.Pos, out t1))
					{
						int i12 = AddInterpolatedVertedToMeshDataAndGetIndex(meshData, v1, v2, t);
						meshData.AddTriangle(new Triangle(i01, triangle.I1, i12));
						meshData.AddTriangle(new Triangle(triangle.I0, i01, i12));
						meshData.AddTriangle(new Triangle(triangle.I0, i12, triangle.I2));
					}
					else if (plane.Raycast(v0.Pos, v2.Pos, out t1))
					{
						int i02 = AddInterpolatedVertedToMeshDataAndGetIndex(meshData, v0, v2, t);
						meshData.AddTriangle(new Triangle(triangle.I0, i01, i02));
						meshData.AddTriangle(new Triangle(i01, triangle.I1, triangle.I2));
						meshData.AddTriangle(new Triangle(i02, i01, triangle.I2));
					}
				}
				else if (plane.Raycast(v2.Pos, v0.Pos, out t1) && plane.Raycast(v2.Pos, v1.Pos, out t2))
				{
					int i20 = AddInterpolatedVertedToMeshDataAndGetIndex(meshData, v2, v0, t1);
					int i21 = AddInterpolatedVertedToMeshDataAndGetIndex(meshData, v2, v1, t2);
					meshData.AddTriangle(new Triangle(i20, i21, triangle.I2));
					meshData.AddTriangle(new Triangle(triangle.I0, i21, i20));
					meshData.AddTriangle(new Triangle(triangle.I0, triangle.I1, i21));
				}
			}
		}

		private static int AddInterpolatedVertedToMeshDataAndGetIndex(
			MeshData meshData, Vertex v0, Vertex v1, float t)
		{
			Vector3 vertex = Vector3.Lerp(v0.Pos, v1.Pos, t);
			Vector3 normal = Vector3.Lerp(v0.Norm, v1.Norm, t);
			Vector2 uv = Vector2.Lerp(v0.UV, v1.UV, t);
			Vector4 tangent = Vector4.Lerp(v0.Tan, v1.Tan, t);

			int index = meshData.Vertices.Count;
			meshData.AddVertex(vertex, normal, uv, tangent);
			return index;			
		}

		private static bool IsTriangleIntersectingPlane(Plane.Side s0, Plane.Side s1, Plane.Side s2)
		{
			return !(s0 == s1 && s0 == s2);
		}

		private static bool IsOneEdgeParallelToPlane(Plane.Side s0, Plane.Side s1, Plane.Side s2)
		{
			return (s0 == Plane.Side.on && s1 == Plane.Side.on)
				|| (s0 == Plane.Side.on && s2 == Plane.Side.on)
				|| (s1 == Plane.Side.on && s2 == Plane.Side.on);
		}

		private static bool IsOnePointOnPlaneAndOthersOnSameSide(Plane.Side s0, Plane.Side s1, Plane.Side s2)
		{
			return (s0 == Plane.Side.on && s1 != Plane.Side.on && s1 == s2)
				|| (s1 == Plane.Side.on && s0 != Plane.Side.on && s0 == s2)
				|| (s2 == Plane.Side.on && s0 != Plane.Side.on && s0 == s1);
		}

		private static bool IsOnePointOnPlaneAndOthersOnDifferentSides(Plane.Side s0, Plane.Side s1, Plane.Side s2)
		{
			return (s0 == Plane.Side.on && s1 != Plane.Side.on && s2 != Plane.Side.on && s1 != s2)
				|| (s0 != Plane.Side.on && s1 == Plane.Side.on && s2 != Plane.Side.on && s0 != s2)
				|| (s0 != Plane.Side.on && s1 != Plane.Side.on && s2 == Plane.Side.on && s0 != s1);
		}

		
	}
}
