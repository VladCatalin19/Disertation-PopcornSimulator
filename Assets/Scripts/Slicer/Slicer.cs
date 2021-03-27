using System.Collections.Generic;
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
			if (!meshFilter)
				throw new System.ArgumentException("Provided GameObject does not have a MeshFilter component");

			SliceMesh(meshFilter.mesh, cuttingPlanes, gameObject.transform);
		}

		private static void SliceMesh(Mesh mesh, UnityEngine.Plane[] cuttingPlanes, Transform spaceTransform)
		{
			MeshData meshData = new MeshData(mesh);

			/*
			int[] freq = new int[meshData.Vertices.Count];

			foreach (Triangle t in meshData.Triangles)
			{
				++freq[t.I0];
				++freq[t.I1];
				++freq[t.I2];
			}

			for (int i = 0; i < freq.Length; ++i)
			{
				Debug.Log($"V: {i} Freq: {freq[i]}");
			}
			*/

			TransformMeshDataVerticesToWorldSpace(meshData, spaceTransform);
			SliceMeshTriangles(meshData, cuttingPlanes);
			DeleteUnusedVertices(meshData);
			TransformMeshDataVerticesToLocalSpace(meshData, spaceTransform);

			meshData.ToMesh(mesh);
		}

		private static void TransformMeshDataVerticesToWorldSpace(MeshData meshData, Transform transform)
		{
			for (int i = 0; i < meshData.Vertices.Count; ++i)
			{
				meshData.Vertices[i] = transform.TransformPoint(meshData.Vertices[i]);
			}
		}

		private static void TransformMeshDataVerticesToLocalSpace(MeshData meshData, Transform transform)
		{
			for (int i = 0; i < meshData.Vertices.Count; ++i)
			{
				meshData.Vertices[i] = transform.InverseTransformPoint(meshData.Vertices[i]);
			}
		}

		private static void SliceMeshTriangles(MeshData meshData, UnityEngine.Plane[] cuttingPlanes)
		{
			var upperZoneDuplicatedIndices = new Dictionary<IndicesPair, int>();
			var lowerZoneDuplicatedIndices = new Dictionary<IndicesPair, int>();

			foreach (UnityEngine.Plane planeUnity in cuttingPlanes)
			{
				Plane plane = new Plane(planeUnity);
				int maxTriangleIndex = meshData.Triangles.Count;

				upperZoneDuplicatedIndices.Clear();
				lowerZoneDuplicatedIndices.Clear();

				for (int i = 0; i < maxTriangleIndex; ++i)
				{
					bool shouldDeleteTriangle = SliceTriangleAndAddToMeshData(
						meshData, i, plane, upperZoneDuplicatedIndices, lowerZoneDuplicatedIndices
					);
					if (shouldDeleteTriangle)
					{
						meshData.RemoveTriangleAt(i);
						--maxTriangleIndex;
						--i;
					}
				}
			}
		}

		private static void DeleteUnusedVertices(MeshData meshData)
		{
			short[] verticesFrequencies = new short[meshData.Vertices.Count];
			foreach (Triangle triangle in meshData.Triangles)
			{
				++verticesFrequencies[triangle.I0];
				++verticesFrequencies[triangle.I1];
				++verticesFrequencies[triangle.I2];
			}

			short[] indicesDecrement = new short[meshData.Vertices.Count];
			short currentDecrement = 0;
			for (int i = 0; i < verticesFrequencies.Length; ++i)
			{
				if (verticesFrequencies[i] == 0)
				{
					++currentDecrement;
				}
				indicesDecrement[i] = currentDecrement;
			}

			for (int i = meshData.Vertices.Count - 1; i >= 0; --i)
			{
				if (verticesFrequencies[i] == 0)
				{
					meshData.RemoveVertexAt(i);
				}
			}

			for (int i = 0; i < meshData.Triangles.Count; ++i)
			{
				int i0 = meshData.Triangles[i].I0;
				int i1 = meshData.Triangles[i].I1;
				int i2 = meshData.Triangles[i].I2;

				i0 -= indicesDecrement[i0];
				i1 -= indicesDecrement[i1];
				i2 -= indicesDecrement[i2];

				meshData.Triangles[i] = new Triangle(i0, i1, i2);
			}
		}

		private static bool SliceTriangleAndAddToMeshData(MeshData meshData, int triangleIndex, Plane plane,
			IDictionary<IndicesPair, int> upSideDuplicatedIndices, IDictionary<IndicesPair, int> downSideDuplicatedIndices)
		{
			Triangle triangle = meshData.Triangles[triangleIndex];
			Vertex v0 = meshData.GetVertexAt(triangle.I0);
			Vertex v1 = meshData.GetVertexAt(triangle.I1);
			Vertex v2 = meshData.GetVertexAt(triangle.I2);

			v0.PlaneSide = plane.GetSide(v0.Position);
			v1.PlaneSide = plane.GetSide(v1.Position);
			v2.PlaneSide = plane.GetSide(v2.Position);

			if (!IsTriangleIntersectingPlane(v0.PlaneSide, v1.PlaneSide, v2.PlaneSide))
			{
				return false;
			}

			if (IsOnePointOnPlaneAndOthersOnSameSide(v0.PlaneSide, v1.PlaneSide, v2.PlaneSide))
			{
				DuplicateVertexOnPlaneAndAddTriangleToMeshData(
					meshData, v0, v1, v2, downSideDuplicatedIndices, upSideDuplicatedIndices
				);
			}
			else if (IsOneEdgeOnToPlane(v0.PlaneSide, v1.PlaneSide, v2.PlaneSide))
			{
				DuplicateLineVerticesOnPlaneAndAddTriangleToMeshData(
					meshData, v0, v1, v2, downSideDuplicatedIndices, upSideDuplicatedIndices
				);
			}
			else if (IsOnePointOnPlaneAndOthersOnDifferentSides(v0.PlaneSide, v1.PlaneSide, v2.PlaneSide))
			{
				CutTriangleInTwoTrianglesWherePlaneIntersectsAndAddThemToMeshData(
					meshData, v0, v1, v2, downSideDuplicatedIndices, upSideDuplicatedIndices, plane
				);
			}
			else // All points ore not on the plane
			{
				CutTriangleInThreeTrianglesWherePlaneIntersetsAndAddThemToMeshData(
					meshData, v0, v1, v2, downSideDuplicatedIndices, upSideDuplicatedIndices, plane
				);
			}
			return true;
		}

		private static void DuplicateVertexOnPlaneAndAddTriangleToMeshData(
			MeshData meshData, Vertex v0, Vertex v1, Vertex v2,
			IDictionary<IndicesPair, int> downSideDuplicatedIndices, IDictionary<IndicesPair, int> upSideDuplicatedIndices
		)
		{
			Triangle newTriangle = new Triangle();
			if (v0.PlaneSide  == Plane.Side.on)
			{
				//  v2 x-----x v1
				//      \   /
				//       \ /
				// -------x------- Plane
				//        v0
				var indices = v1.PlaneSide  == Plane.Side.down ? downSideDuplicatedIndices : upSideDuplicatedIndices;
				int v0CopyIndex = GetCopyOfVertexOrCreateDuplicate(meshData, indices, v0.TriangleIndex);
				newTriangle = new Triangle(v0CopyIndex, v1.TriangleIndex, v2.TriangleIndex);
			}
			else if (v1.PlaneSide  == Plane.Side.on)
			{
				//  v0 x-----x v2
				//      \   /
				//       \ /
				// -------x------- Plane
				//        v1
				var indices = v0.PlaneSide  == Plane.Side.down ? downSideDuplicatedIndices : upSideDuplicatedIndices;
				int v1CopyIndex = GetCopyOfVertexOrCreateDuplicate(meshData, indices, v1.TriangleIndex);
				newTriangle = new Triangle(v0.TriangleIndex, v1CopyIndex, v2.TriangleIndex);
			}
			else if (v2.PlaneSide  == Plane.Side.on)
			{
				//  v1 x-----x v0
				//      \   /
				//       \ /
				// -------x------- Plane
				//        v2
				var indices = v0.PlaneSide  == Plane.Side.down ? downSideDuplicatedIndices : upSideDuplicatedIndices;
				int v2CopyIndex = GetCopyOfVertexOrCreateDuplicate(meshData, indices, v2.TriangleIndex);
				newTriangle = new Triangle(v0.TriangleIndex, v1.TriangleIndex, v2CopyIndex);
			}
			meshData.AddTriangle(newTriangle);
		}

		private static void DuplicateLineVerticesOnPlaneAndAddTriangleToMeshData(
			MeshData meshData, Vertex v0, Vertex v1, Vertex v2,
			IDictionary<IndicesPair, int> downSideDuplicatedIndices, IDictionary<IndicesPair, int> upSideDuplicatedIndices
		)
		{
			Triangle newTriangle = new Triangle();
			if (v0.PlaneSide == Plane.Side.on && v1.PlaneSide == Plane.Side.on)
			{
				//        x v2
				//       / \
				//      /   \
				// ----x-----x---- Plane
				//     v0    v1
				var indices = v2.PlaneSide == Plane.Side.down ? downSideDuplicatedIndices : upSideDuplicatedIndices;
				int v0CopyIndex = GetCopyOfVertexOrCreateDuplicate(meshData, indices, v0.TriangleIndex);
				int v1CopyIndex = GetCopyOfVertexOrCreateDuplicate(meshData, indices, v1.TriangleIndex);

				newTriangle = new Triangle(v0CopyIndex, v1CopyIndex, v2.TriangleIndex);
			}
			else if (v0.PlaneSide == Plane.Side.on && v2.PlaneSide == Plane.Side.on)
			{
				//        x v1
				//       / \
				//      /   \
				// ----x-----x---- Plane
				//     v2    v0
				var indices = v1.PlaneSide == Plane.Side.down ? downSideDuplicatedIndices : upSideDuplicatedIndices;
				int v0CopyIndex = GetCopyOfVertexOrCreateDuplicate(meshData, indices, v0.TriangleIndex);
				int v2CopyIndex = GetCopyOfVertexOrCreateDuplicate(meshData, indices, v2.TriangleIndex);

				newTriangle = new Triangle(v0CopyIndex, v1.TriangleIndex, v2CopyIndex);
			}
			else if (v1.PlaneSide == Plane.Side.on && v2.PlaneSide == Plane.Side.on)
			{
				//        x v0
				//       / \
				//      /   \
				// ----x-----x---- Plane
				//     v1    v2
				var indices = v0.PlaneSide == Plane.Side.down ? downSideDuplicatedIndices : upSideDuplicatedIndices;
				int v1CopyIndex = GetCopyOfVertexOrCreateDuplicate(meshData, indices, v1.TriangleIndex);
				int v2CopyIndex = GetCopyOfVertexOrCreateDuplicate(meshData, indices, v2.TriangleIndex);

				newTriangle = new Triangle(v0.TriangleIndex, v1CopyIndex, v2CopyIndex);
			}
			meshData.AddTriangle(newTriangle);
		}

		private static void CutTriangleInTwoTrianglesWherePlaneIntersectsAndAddThemToMeshData(
			MeshData meshData, Vertex v0, Vertex v1, Vertex v2,
			IDictionary<IndicesPair, int> downSideDuplicatedIndices, IDictionary<IndicesPair, int> upSideDuplicatedIndices,
			Plane plane
		)
		{
			Triangle triangle0 = new Triangle();
			Triangle triangle1 = new Triangle();
			float t;
			if (v0.PlaneSide == Plane.Side.on && plane.Raycast(v1.Position, v2.Position, out t))
			{
				//        x v2
				//       /|
				//   v0 / | v12
				// ----x--+------- Plane
				//      \ |
				//       \|
				//        x v1
				var sameSideIndices = v1.PlaneSide == Plane.Side.down ? downSideDuplicatedIndices : upSideDuplicatedIndices;
				var otherSideIndices = v1.PlaneSide == Plane.Side.down ? upSideDuplicatedIndices : downSideDuplicatedIndices;

				int v0CopyIndexSameSideAsV1 = GetCopyOfVertexOrCreateDuplicate(meshData, sameSideIndices, v0.TriangleIndex);
				int v0CopyIndexOtherSideAsV1 = GetCopyOfVertexOrCreateDuplicate(meshData, otherSideIndices, v0.TriangleIndex);

				int v12IndexSameSideOfV1 = GetCopyOfInterpolatedVertexOrCreateDuplicate(meshData, sameSideIndices, v1.TriangleIndex, v2.TriangleIndex, t);
				int v12OIndextherSideOfV1 = GetCopyOfInterpolatedVertexOrCreateDuplicate(meshData, otherSideIndices, v1.TriangleIndex, v2.TriangleIndex, t);

				triangle0 = new Triangle(v0CopyIndexSameSideAsV1, v1.TriangleIndex, v12IndexSameSideOfV1);
				triangle1 = new Triangle(v0CopyIndexOtherSideAsV1, v12OIndextherSideOfV1, v2.TriangleIndex);
			}
			else if (v1.PlaneSide == Plane.Side.on && plane.Raycast(v0.Position, v2.Position, out t))
			{
				//        x v0
				//       /|
				//   v1 / | v02
				// ----x--+------- Plane
				//      \ |
				//       \|
				//        x v2
				var sameSideIndices = v0.PlaneSide == Plane.Side.down ? downSideDuplicatedIndices : upSideDuplicatedIndices;
				var otherSideIndices = v0.PlaneSide == Plane.Side.down ? upSideDuplicatedIndices : downSideDuplicatedIndices;

				int i1CopySameSideP0 = GetCopyOfVertexOrCreateDuplicate(meshData, sameSideIndices, v1.TriangleIndex);
				int i1CopyOtherSideP0 = GetCopyOfVertexOrCreateDuplicate(meshData, otherSideIndices, v1.TriangleIndex);

				int i02SameSideP0 = GetCopyOfInterpolatedVertexOrCreateDuplicate(meshData, sameSideIndices, v0.TriangleIndex, v2.TriangleIndex, t);
				int i02OtherSideP0 = GetCopyOfInterpolatedVertexOrCreateDuplicate(meshData, otherSideIndices, v0.TriangleIndex, v2.TriangleIndex, t);

				triangle0 = new Triangle(v0.TriangleIndex, i1CopySameSideP0, i02SameSideP0);
				triangle1 = new Triangle(i1CopyOtherSideP0, i02OtherSideP0, v2.TriangleIndex);
			}
			else if (v2.PlaneSide  == Plane.Side.on && plane.Raycast(v0.Position, v1.Position, out t))
			{
				//        x v1
				//       /|
				//   v2 / | v01
				// ----x--+------- Plane
				//      \ |
				//       \|
				//        x v0
				var sameSideIndices = v0.PlaneSide == Plane.Side.down ? downSideDuplicatedIndices : upSideDuplicatedIndices;
				var otherSideIndices = v0.PlaneSide == Plane.Side.down ? upSideDuplicatedIndices : downSideDuplicatedIndices;

				int i2CopySameSideP0 = GetCopyOfVertexOrCreateDuplicate(meshData, sameSideIndices, v2.TriangleIndex);
				int i2CopyOtherSideP0 = GetCopyOfVertexOrCreateDuplicate(meshData, otherSideIndices, v2.TriangleIndex);
				
				int i01SameSideP0 = GetCopyOfInterpolatedVertexOrCreateDuplicate(meshData, sameSideIndices, v0.TriangleIndex, v1.TriangleIndex, t);
				int i01OtherSideP0 = GetCopyOfInterpolatedVertexOrCreateDuplicate(meshData, otherSideIndices, v0.TriangleIndex, v1.TriangleIndex, t);

				triangle0 = new Triangle(v0.TriangleIndex, i01SameSideP0, i2CopySameSideP0);
				triangle1 = new Triangle(i01OtherSideP0, v1.TriangleIndex, i2CopyOtherSideP0);
			}
			meshData.AddTriangle(triangle0);
			meshData.AddTriangle(triangle1);
		}

		private static void CutTriangleInThreeTrianglesWherePlaneIntersetsAndAddThemToMeshData(
			MeshData meshData, Vertex v0, Vertex v1, Vertex v2,
			IDictionary<IndicesPair, int> downSideDuplicatedIndices, IDictionary<IndicesPair, int> upSideDuplicatedIndices,
			Plane plane
		)
		{
			float t, t1, t2;
			Triangle triangle0 = new Triangle();
			Triangle triangle1 = new Triangle();
			Triangle triangle2 = new Triangle();

			if (v0.PlaneSide != v1.PlaneSide && plane.Raycast(v0.Position, v1.Position, out t))
			{
				var sameSideIndices = v1.PlaneSide == Plane.Side.down ? downSideDuplicatedIndices : upSideDuplicatedIndices;
				var otherSideIndices = v1.PlaneSide == Plane.Side.down ? upSideDuplicatedIndices : downSideDuplicatedIndices;

				int i01SameSideP1 = GetCopyOfInterpolatedVertexOrCreateDuplicate(meshData, sameSideIndices, v0.TriangleIndex, v1.TriangleIndex, t);
				int i01OtherSideP1 = GetCopyOfInterpolatedVertexOrCreateDuplicate(meshData, otherSideIndices, v0.TriangleIndex, v1.TriangleIndex, t);

				if (v0.PlaneSide == v2.PlaneSide && plane.Raycast(v1.Position, v2.Position, out t1))
				{
					//        x v1
					//   v12 / \ v01
					// -----*---*----- Plane
					//     /     \
					//    x-------x
					//    v2      v0
					int i12SameSideP1 = GetCopyOfInterpolatedVertexOrCreateDuplicate(meshData, sameSideIndices, v1.TriangleIndex, v2.TriangleIndex, t1);
					int i12OtherSideP1 = GetCopyOfInterpolatedVertexOrCreateDuplicate(meshData, otherSideIndices, v1.TriangleIndex, v2.TriangleIndex, t1);
					triangle0 = new Triangle(i01SameSideP1, v1.TriangleIndex, i12SameSideP1);
					triangle1 = new Triangle(v0.TriangleIndex, i01OtherSideP1, i12OtherSideP1);
					triangle2 = new Triangle(v0.TriangleIndex, i12OtherSideP1, v2.TriangleIndex);
				}
				else if (plane.Raycast(v0.Position, v2.Position, out t1))
				{
					//        x v0
					//   v01 / \ v02
					// -----*---*----- Plane
					//     /     \
					//    x-------x
					//    v1      v2
					int i02SameSideP1 = GetCopyOfInterpolatedVertexOrCreateDuplicate(meshData, sameSideIndices, v0.TriangleIndex, v2.TriangleIndex, t1);
					int i02OtherSideP1 = GetCopyOfInterpolatedVertexOrCreateDuplicate(meshData, otherSideIndices, v0.TriangleIndex, v2.TriangleIndex, t1);
					triangle0 = new Triangle(v0.TriangleIndex, i01OtherSideP1, i02OtherSideP1);
					triangle1 = new Triangle(i02SameSideP1, i01SameSideP1, v2.TriangleIndex);
					triangle2 = new Triangle(i01SameSideP1, v1.TriangleIndex, v2.TriangleIndex);
				}
			}
			else if (plane.Raycast(v2.Position, v0.Position, out t1) && plane.Raycast(v2.Position, v1.Position, out t2))
			{
				//        x v2
				//   v20 / \ v21
				// -----*---*----- Plane
				//     /     \
				//    x-------x
				//    v0      v1
				var sameSideIndices = v2.PlaneSide == Plane.Side.down ? downSideDuplicatedIndices : upSideDuplicatedIndices;
				var otherSideIndices = v2.PlaneSide == Plane.Side.down ? upSideDuplicatedIndices : downSideDuplicatedIndices;

				int i20SameSideP2 = GetCopyOfInterpolatedVertexOrCreateDuplicate(meshData, sameSideIndices, v2.TriangleIndex, v0.TriangleIndex, t1);
				int i20OtherSideP2 = GetCopyOfInterpolatedVertexOrCreateDuplicate(meshData, otherSideIndices, v2.TriangleIndex, v0.TriangleIndex, t1);

				int i21SameSideP2 = GetCopyOfInterpolatedVertexOrCreateDuplicate(meshData, sameSideIndices, v2.TriangleIndex, v1.TriangleIndex, t2);
				int i21OtherSideP2 = GetCopyOfInterpolatedVertexOrCreateDuplicate(meshData, otherSideIndices, v2.TriangleIndex, v1.TriangleIndex, t2);
				triangle0 = new Triangle(i20SameSideP2, i21SameSideP2, v2.TriangleIndex);
				triangle1 = new Triangle(v0.TriangleIndex, i21OtherSideP2, i20OtherSideP2);
				triangle2 = new Triangle(v0.TriangleIndex, v1.TriangleIndex, i21OtherSideP2);
			}
			meshData.AddTriangle(triangle0);
			meshData.AddTriangle(triangle1);
			meshData.AddTriangle(triangle2);
		}

		private static int AddCopyOfVertexToMeshDataAndGetIndex(MeshData meshData, Vertex v)
		{
			int index = meshData.Vertices.Count;
			// Since Vertex is a struct and structs are passed by value, we do
			// not need to make an explicit copy of the vertex since it is
			// already a copy of the original
			meshData.AddVertex(v);
			return index;
		}

		private struct IndicesPair
		{
			private int i0, i1;
			public IndicesPair(int i0, int i1)
			{
				this.i0 = i0;
				this.i1 = i1;
			}

			public int I0 { get => i0; }
			public int I1 { get => i1; }

            public override string ToString()
            {
                return $"({i0}, {i1})";
            }
        
		}

		private static int GetCopyOfVertexOrCreateDuplicate(MeshData meshData,
			IDictionary<IndicesPair, int> zoneDuplicateIndices, int index
		)
		{
			int indexCopy;
			IndicesPair pair = new IndicesPair(index, index);

			if (!zoneDuplicateIndices.TryGetValue(pair, out indexCopy))
			{
				indexCopy = AddCopyOfVertexToMeshDataAndGetIndex(meshData, meshData.GetVertexAt(index));
				zoneDuplicateIndices.Add(pair, indexCopy);
			}
			return indexCopy;
		}

		private static int GetCopyOfInterpolatedVertexOrCreateDuplicate(MeshData meshData,
			IDictionary<IndicesPair, int> zoneDuplicateIndices, int index0, int index1, float t)
		{
			int indexCopy;
			IndicesPair pair = new IndicesPair(index0, index1);
			IndicesPair reversedPair = new IndicesPair(index1, index0);

			if (!(zoneDuplicateIndices.TryGetValue(pair, out indexCopy) || zoneDuplicateIndices.TryGetValue(reversedPair, out indexCopy)))
			{
				Vertex v0 = meshData.GetVertexAt(index0);
				Vertex v1 = meshData.GetVertexAt(index1);

				indexCopy = AddCopyOfVertexToMeshDataAndGetIndex(meshData, Vertex.Lerp(v0, v1, t));
				zoneDuplicateIndices.Add(pair, indexCopy);
			}
			return indexCopy;
		}

		private static bool IsTriangleIntersectingPlane(Plane.Side s0, Plane.Side s1, Plane.Side s2)
		{
			return !(s0 == s1 && s0 == s2);
		}

		private static bool IsOneEdgeOnToPlane(Plane.Side s0, Plane.Side s1, Plane.Side s2)
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
