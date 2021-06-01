using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace PopcornGenerator
{
	internal static class Rigger
	{
		public static IList<RiggedSlice> RigSlices(IList<Slice> slices, Plane[] riggingPlanes)
		{
			IList<RiggedSlice> riggedSlices = new List<RiggedSlice>(slices.Count);
			for (int sliceIndex = 0; sliceIndex < slices.Count; ++sliceIndex)
			{
				riggedSlices.Add(RigSlice(slices[sliceIndex], riggingPlanes));
			}
			return riggedSlices;
		}

		private static RiggedSlice RigSlice(Slice slice, Plane[] riggingPlanes)
		{
			int riggingZones = riggingPlanes.Length + 1;
			int approxIndicesPerZone = slice.Mesh.Vertices.Count / riggingZones;
			RiggedSlice riggedSlice = new RiggedSlice(slice, riggingZones, approxIndicesPerZone);

			CalculateVerticesRiggingZone(riggedSlice, riggingPlanes);
			CalculateRiggingZonesBonePositions(riggedSlice);
			CalculateRiggingZonesBoneWeights(riggedSlice);

			return riggedSlice;
		}

		private static void CalculateVerticesRiggingZone(RiggedSlice riggedSlice, Plane[] riggingPlanes)
		{
			for (int vertexIndex = 0; vertexIndex < riggedSlice.Slice.Mesh.Vertices.Count; ++vertexIndex)
			{
				int vertexZone = GetVertexPositionBetweenPlanes(
					riggedSlice.Slice.Mesh.Vertices[vertexIndex].Position, riggingPlanes
				);
				riggedSlice.RiggingZonesIndices[vertexZone].Add(vertexIndex);
			}
		}

		// TODO: Array of planes is sorted by y coordinate, use binary search
		private static int GetVertexPositionBetweenPlanes(Vector3 position, Plane[] riggingPlanes)
		{
			if (!IsAbovePlane(position, riggingPlanes[0]))
			{
				return 0;
			}

			for (int riggingPlaneIndex = 1; riggingPlaneIndex < riggingPlanes.Length; ++riggingPlaneIndex)
			{
				if (IsAbovePlane(position, riggingPlanes[riggingPlaneIndex - 1])
					&& !IsAbovePlane(position, riggingPlanes[riggingPlaneIndex]))
				{
					return riggingPlaneIndex;
				}
			}

			// Point is above all planes
			return riggingPlanes.Length;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool IsAbovePlane(Vector3 pos, Plane plane)
		{
			return plane.GetSide(pos) == Plane.Side.up;
		}

		private static void CalculateRiggingZonesBonePositions(RiggedSlice riggedSlice)
		{
			for (int riggingZoneIndex = 0; riggingZoneIndex < riggedSlice.RiggingZonesIndices.Count; ++riggingZoneIndex)
			{
				riggedSlice.BonePositions[riggingZoneIndex] = CalculateRiggingZoneBonePosition(riggedSlice, riggingZoneIndex);
			}
		}

		private static Vector3 CalculateRiggingZoneBonePosition(RiggedSlice riggedSlice, int zoneIndex)
		{
			IList<Vertex> meshVertices = riggedSlice.Slice.Mesh.Vertices;
			IList<Triangle> meshTriangles = riggedSlice.Slice.Mesh.Triangles;

			// Create copy of vertices
			IList<int> zoneIndices = riggedSlice.RiggingZonesIndices[zoneIndex];
			Vector3[] vertices = new Vector3[zoneIndices.Count];
			for (int vertexIndex = 0; vertexIndex < vertices.Length; ++vertexIndex)
			{
				vertices[vertexIndex] = meshVertices[zoneIndices[vertexIndex]].Position;
			}

			// Select triangle in zone
			var indicesHashSet = new HashSet<int>(zoneIndices);
			int approxNumOfTriangles = meshTriangles.Count / riggedSlice.RiggingZonesIndices.Count;
			IList<Triangle> triangles = new List<Triangle>(approxNumOfTriangles);
			for (int triangleIndex = 0; triangleIndex < meshTriangles.Count; ++triangleIndex)
			{
				Triangle t = meshTriangles[triangleIndex];
				if (indicesHashSet.Contains(t.I0) && indicesHashSet.Contains(t.I1) && indicesHashSet.Contains(t.I2))
				{
					triangles.Add(t);
				}
			}

			// Calculate center of gravity
			Vector3 center = Vector3.zero;
			for (int vertexIndex = 0; vertexIndex < vertices.Length; ++vertexIndex)
			{
				center += vertices[vertexIndex];
			}
			center /= vertices.Length;

			// Calculate mean normal
			Vector3 meanNormal = Vector3.zero;
			for (int triangleIndex = 0; triangleIndex < triangles.Count; ++triangleIndex)
			{
				Vector3 v0 = meshVertices[triangles[triangleIndex].I0].Position;
				Vector3 v1 = meshVertices[triangles[triangleIndex].I1].Position;
				Vector3 v2 = meshVertices[triangles[triangleIndex].I2].Position;

				meanNormal += Vector3.Cross(v1 - v0, v2 - v0);
			}
			meanNormal /= triangles.Count;

			// Translate vertices to origin
			for (int vertexIndex = 0; vertexIndex < vertices.Length; ++vertexIndex)
			{
				vertices[vertexIndex] -= center;
			}

			// Project vertices on plane
			for (int vertexIndex = 0; vertexIndex < vertices.Length; ++vertexIndex)
			{
				vertices[vertexIndex] = Vector3.ProjectOnPlane(vertices[vertexIndex], meanNormal);
			}

			// Rotate vertices
			Vector3 dir = Vector3.Angle(meanNormal, Vector3.forward) < Vector3.Angle(meanNormal, Vector3.back)
				? Vector3.forward : Vector3.back;
			Quaternion q = Quaternion.FromToRotation(meanNormal, dir);
			for (int vertexIndex = 0; vertexIndex < vertices.Length; ++vertexIndex)
			{
				vertices[vertexIndex] = q * vertices[vertexIndex];
			}

			// Calculate bounds
			Bounds bounds = new Bounds();
			for (int vertexIndex = 0; vertexIndex < vertices.Length; ++vertexIndex)
			{
				bounds.Encapsulate(vertices[vertexIndex]);
			}

			Vector3 reference = new Vector3(
				bounds.center.x,
				zoneIndex == 0 ? bounds.center.y : bounds.min.y,
				bounds.center.z
			);

			// Find closest vertex to reference
			float sqrMinDinst = float.MaxValue;
			int closestVertexIndex = -1;
			for (int vertexIndex = 0; vertexIndex < vertices.Length; ++vertexIndex)
			{
				float sqrDist = (vertices[vertexIndex] - reference).sqrMagnitude;
				if (sqrDist < sqrMinDinst)
				{
					sqrMinDinst = sqrDist;
					closestVertexIndex = zoneIndices[vertexIndex];
				}
			}
			return meshVertices[closestVertexIndex].Position;

			#if false
			// TODO use a mesh parametrisation instead of uv coordinates
			if (!riggedSlice.Slice.Mesh.HasUVs)
			{
				throw new System.NotImplementedException("Rigger does not support meshes without UV coordinates");
			}

			// Calculate uv coordinates bounds
			Vector2 minCoords = float.MaxValue * Vector2.one;
			Vector2 maxCoords = float.MinValue * Vector2.one;

			foreach (int vertexIndex in riggedSlice.RiggingZonesIndices[zoneIndex])
			{
				Vector2 uv = riggedSlice.Slice.Mesh.Vertices[vertexIndex].UV.Value;
				minCoords = Vector2.Min(minCoords, uv);
				maxCoords = Vector2.Max(maxCoords, uv);
			}

			// Get closest vertex to bottom center of bounds
			Vector2 closestUVCoords = new Vector2(0.5f * (minCoords.x + maxCoords.x), minCoords.y);
			float minSqrDistance = float.MaxValue;
			int closestIndex = -1;
			foreach (int vertexIndex in riggedSlice.RiggingZonesIndices[zoneIndex])
			{
				float sqrDistance = (riggedSlice.Slice.Mesh.Vertices[vertexIndex].UV.Value - closestUVCoords).sqrMagnitude;
				if (sqrDistance < minSqrDistance)
				{
					minSqrDistance = sqrDistance;
					closestIndex = vertexIndex;
				}
			}

			return riggedSlice.Slice.Mesh.Vertices[closestIndex].Position;
			#endif
		}

		// TODO use smoothing
		private static void CalculateRiggingZonesBoneWeights(RiggedSlice riggedSlice)
		{
			for (int riggingZoneIndex = 0; riggingZoneIndex < riggedSlice.RiggingZonesIndices.Count; ++riggingZoneIndex)
			{
				foreach (int vertexIndex in riggedSlice.RiggingZonesIndices[riggingZoneIndex])
				{
					riggedSlice.BoneWeights[vertexIndex] = new BoneWeight()
					{
						boneIndex0 = riggingZoneIndex,
						weight0 = 1.0f
					};
				}
			}
		}
	}
}
