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
			// TODO use a mesh parametrisation instead of uv coordinates
			if (!riggedSlice.Slice.Mesh.HasUVs)
			{
				throw new System.NotImplementedException("Rigger does not support meshes without UV coordinates");
			}

			// Calculate uv coordinates bounds
			Bounds uvBounds = new Bounds();
			foreach (int vertexIndex in riggedSlice.RiggingZonesIndices[zoneIndex])
			{
				Vector2 uv = riggedSlice.Slice.Mesh.Vertices[vertexIndex].UV.Value;
				uvBounds.Encapsulate(new Vector3(uv.x, uv.y, 0.0f));
			}

			// Get closest vertex to bottom center of bounds
			Vector2 closestUVCoords = new Vector2(uvBounds.center.x, uvBounds.min.y);
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
