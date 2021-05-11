using System.Collections.Generic;
using UnityEngine;

using DuplicatedIndicesCollection = System.Collections.Generic.Dictionary<PopcornGenerator.Slicer.IndicesPair, int>;
using SliceAllIndicesCollection = System.Collections.Generic.List<int>;
using SliceBorderIndicesCollection = System.Collections.Generic.List<int>;

namespace PopcornGenerator.Slicer
{
	public static class Slicer
	{
		public static IList<Slice> Slice(GameObject gameObject, UnityEngine.Plane[] cuttingPlanes)
		{
			if (!gameObject) throw new System.ArgumentNullException("gameObject");
			if (cuttingPlanes == null) throw new System.ArgumentNullException("cuttingPlanes");

			MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
			if (!meshFilter)
				throw new System.ArgumentException("Provided GameObject does not have a MeshFilter component");

			return SliceMesh(meshFilter.mesh, cuttingPlanes, gameObject.transform);
		}

		private static IList<Slice> SliceMesh(Mesh mesh, UnityEngine.Plane[] cuttingPlanes, Transform spaceTransform)
		{
			MeshData meshData = new MeshData(mesh);
			Plane[] planes = UnityPlanesToSlicerPlanes(cuttingPlanes);

			TransformMeshDataVerticesToWorldSpace(meshData, spaceTransform);
			DuplicateIndices[] duplicateIndices = SliceMeshTriangles(meshData, planes);
			GetSlicesAndAddThemToMeshData(meshData, planes, duplicateIndices);
			meshData.RemoveUnusedVertices();

			/*
			GameObject[] sidesObjects = new GameObject[meshData.Slices.Count];
			for (int i = 0; i < sidesObjects.Length; ++i)
			{
				sidesObjects[i] = new GameObject($"Side {i}");
			}

			Color[] sideColors = new Color[] { Color.red, Color.green, Color.blue, Color.magenta, Color.cyan };
			Color[] borderColors = new Color[] { Color.white, Color.black, Color.yellow, new Color(1.0f, 165.0f / 255.0f, 0.0f) };
			int sliceIndex = 0;
			foreach (Slice slice in meshData.Slices)
			{
				foreach (int vertexIndex in slice.AllIndices)
				{
					GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
					obj.transform.name = $"Vertex {vertexIndex} Side {sliceIndex}";
					obj.transform.parent = sidesObjects[sliceIndex].transform;
					obj.transform.position = meshData.Vertices[vertexIndex];
					obj.transform.localScale = new Vector3(0.008f, 0.008f, 0.008f);
					obj.GetComponent<Renderer>().material.color = sideColors[sliceIndex];
				}

				foreach (int vertexIndex in slice.BorderIndices)
				{
					GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
					obj.transform.name = $"Vertex {vertexIndex} Side {sliceIndex}";
					obj.transform.parent = sidesObjects[sliceIndex].transform;
					obj.transform.position = meshData.Vertices[vertexIndex];
					obj.transform.localScale = new Vector3(0.009f, 0.009f, 0.009f);
					obj.GetComponent<Renderer>().material.color = borderColors[sliceIndex];
				}
				++sliceIndex;
			}
			*/

			// TODO Remove this and fix slicer
			RemoveTrianglesWithIndicesInDifferentSlices(meshData);

			TransformMeshDataVerticesToLocalSpace(meshData, spaceTransform);
			meshData.ToMesh(mesh);

			return meshData.Slices;
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

		private static DuplicateIndices[] SliceMeshTriangles(MeshData meshData, Plane[] planes)
		{
			DuplicateIndices[] duplicateIndices = new DuplicateIndices[planes.Length];

			for (int planeIndex = 0; planeIndex < planes.Length; ++planeIndex)
			{
				//Debug.Log($"Plane {planeIndex}");

				int maxTriangleIndex = meshData.Triangles.Count;
				duplicateIndices[planeIndex] = new DuplicateIndices(new DuplicatedIndicesCollection(), new DuplicatedIndicesCollection());

				for (int triangleIndex = 0; triangleIndex < maxTriangleIndex; ++triangleIndex)
				{
					bool shouldDeleteTriangle = SliceTriangleAndAddToMeshData(
						meshData, triangleIndex, planes[planeIndex], duplicateIndices[planeIndex]
					);
					if (shouldDeleteTriangle)
					{
						meshData.RemoveTriangleAt(triangleIndex);
						--maxTriangleIndex;
						--triangleIndex;
					}
				}
			}

			return duplicateIndices;
		}

		private static Plane[] UnityPlanesToSlicerPlanes(UnityEngine.Plane[] unityPlanes)
		{
			Plane[] planes = new Plane[unityPlanes.Length];

			for (int planeIndex = 0; planeIndex < unityPlanes.Length; ++planeIndex)
			{
				planes[planeIndex] = new Plane(unityPlanes[planeIndex]);
			}

			return planes;
		}

		private static bool IsOneOfThemTriangles(Triangle t)
		{
			return (t.I0 == 4050 && t.I1 == 4215 && t.I2 == 3691)
				|| (t.I0 == 2558 && t.I1 == 4217 && t.I2 == 3687)
				|| (t.I0 == 2558 && t.I1 == 4053 && t.I2 == 4217)
				|| (t.I0 == 3724 && t.I1 == 4221 && t.I2 == 4219)
				|| (t.I0 == 4222 && t.I1 == 3729 && t.I2 == 4224);
		}

		private static void DisplayProblemTriangle(Triangle t, MeshData meshData, string function)
		{
			Debug.LogWarning($"Found triangle {t} in {function}");

			GameObject triangle = new GameObject("Triangle");
			triangle.transform.position = (meshData.Vertices[t.I0] + meshData.Vertices[t.I1] + meshData.Vertices[t.I2]) / 3;
			Color c = Random.ColorHSV();

			for (int i = 0; i < 3; ++i)
			{
				GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				obj.transform.name = $"Vertex {t[i]}";
				obj.transform.parent = triangle.transform;
				obj.transform.position = meshData.Vertices[t[i]];
				obj.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
				obj.GetComponent<Renderer>().material.color = c;
			}
		}

		private static bool SliceTriangleAndAddToMeshData(MeshData meshData, int triangleIndex, Plane plane,
			DuplicateIndices duplicateIndices)
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
					meshData, v0, v1, v2, duplicateIndices
				);
			}
			else if (IsOneEdgeOnToPlane(v0.PlaneSide, v1.PlaneSide, v2.PlaneSide))
			{
				DuplicateLineVerticesOnPlaneAndAddTriangleToMeshData(
					meshData, v0, v1, v2, duplicateIndices
				);
			}
			else if (IsOnePointOnPlaneAndOthersOnDifferentSides(v0.PlaneSide, v1.PlaneSide, v2.PlaneSide))
			{
				CutTriangleInTwoTrianglesWherePlaneIntersectsAndAddThemToMeshData(
					meshData, v0, v1, v2, duplicateIndices, plane
				);
			}
			else // All points ore not on the plane
			{
				CutTriangleInThreeTrianglesWherePlaneIntersetsAndAddThemToMeshData(
					meshData, v0, v1, v2, duplicateIndices, plane
				);
			}
			return true;
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

		private static void DuplicateVertexOnPlaneAndAddTriangleToMeshData(
			MeshData meshData, Vertex v0, Vertex v1, Vertex v2, DuplicateIndices duplicateIndices
		)
		{
			//Debug.Log($"Slicing triangle {v0.TriangleIndex}, {v1.TriangleIndex}, {v2.TriangleIndex} in DuplicateVertexOnPlaneAndAddTriangleToMeshData");
			Triangle triangle0 = new Triangle();
			if (v0.PlaneSide == Plane.Side.on)
			{
				//  v2 x-----x v1
				//      \   /
				//       \ /
				// -------x------- Plane
				//        v0
				var indices = duplicateIndices.GetHullIndices(v1.PlaneSide).SameSide;
				int v0CopyIndex = GetCopyOfVertexOrCreateDuplicate(meshData, indices, v0.TriangleIndex);
				triangle0 = new Triangle(v0CopyIndex, v1.TriangleIndex, v2.TriangleIndex);
				//Debug.Log($"Duplicated vertex {v0.TriangleIndex} -> {v0CopyIndex}");
			}
			else if (v1.PlaneSide == Plane.Side.on)
			{
				//  v0 x-----x v2
				//      \   /
				//       \ /
				// -------x------- Plane
				//        v1
				var indices = duplicateIndices.GetHullIndices(v0.PlaneSide).SameSide;
				int v1CopyIndex = GetCopyOfVertexOrCreateDuplicate(meshData, indices, v1.TriangleIndex);
				triangle0 = new Triangle(v0.TriangleIndex, v1CopyIndex, v2.TriangleIndex);
				//Debug.Log($"Duplicated vertex {v1.TriangleIndex} -> {v1CopyIndex}");
			}
			else if (v2.PlaneSide == Plane.Side.on)
			{
				//  v1 x-----x v0
				//      \   /
				//       \ /
				// -------x------- Plane
				//        v2
				var indices = duplicateIndices.GetHullIndices(v0.PlaneSide).SameSide;
				int v2CopyIndex = GetCopyOfVertexOrCreateDuplicate(meshData, indices, v2.TriangleIndex);
				triangle0 = new Triangle(v0.TriangleIndex, v1.TriangleIndex, v2CopyIndex);
				//Debug.Log($"Duplicated vertex {v2.TriangleIndex} -> {v2CopyIndex}");
			}
			meshData.AddTriangle(triangle0);
			//if (IsOneOfThemTriangles(triangle0)) DisplayProblemTriangle(triangle0, meshData, "DuplicateVertexOnPlaneAndAddTriangleToMeshData");
		}

		private static void DuplicateLineVerticesOnPlaneAndAddTriangleToMeshData(
			MeshData meshData, Vertex v0, Vertex v1, Vertex v2, DuplicateIndices duplicateIndices
		)
		{
			//Debug.Log($"Slicing triangle {v0.TriangleIndex}, {v1.TriangleIndex}, {v2.TriangleIndex} in DuplicateLineVerticesOnPlaneAndAddTriangleToMeshData");
			Triangle triangle0 = new Triangle();
			if (v0.PlaneSide == Plane.Side.on && v1.PlaneSide == Plane.Side.on)
			{
				//        x v2
				//       / \
				//      /   \
				// ----x-----x---- Plane
				//     v0    v1
				var indices = duplicateIndices.GetHullIndices(v2.PlaneSide).SameSide;
				int v0CopyIndex = GetCopyOfVertexOrCreateDuplicate(meshData, indices, v0.TriangleIndex);
				int v1CopyIndex = GetCopyOfVertexOrCreateDuplicate(meshData, indices, v1.TriangleIndex);

				triangle0 = new Triangle(v0CopyIndex, v1CopyIndex, v2.TriangleIndex);
				//Debug.Log($"Duplicated vertex {v0.TriangleIndex} -> {v0CopyIndex}");
				//Debug.Log($"Duplicated vertex {v1.TriangleIndex} -> {v1CopyIndex}");
			}
			else if (v0.PlaneSide == Plane.Side.on && v2.PlaneSide == Plane.Side.on)
			{
				//        x v1
				//       / \
				//      /   \
				// ----x-----x---- Plane
				//     v2    v0
				var indices = duplicateIndices.GetHullIndices(v1.PlaneSide).SameSide;
				int v0CopyIndex = GetCopyOfVertexOrCreateDuplicate(meshData, indices, v0.TriangleIndex);
				int v2CopyIndex = GetCopyOfVertexOrCreateDuplicate(meshData, indices, v2.TriangleIndex);

				triangle0 = new Triangle(v0CopyIndex, v1.TriangleIndex, v2CopyIndex);
				//Debug.Log($"Duplicated vertex {v0.TriangleIndex} -> {v0CopyIndex}");
				//Debug.Log($"Duplicated vertex {v2.TriangleIndex} -> {v2CopyIndex}");
			}
			else if (v1.PlaneSide == Plane.Side.on && v2.PlaneSide == Plane.Side.on)
			{
				//        x v0
				//       / \
				//      /   \
				// ----x-----x---- Plane
				//     v1    v2
				var indices = duplicateIndices.GetHullIndices(v0.PlaneSide).SameSide;
				int v1CopyIndex = GetCopyOfVertexOrCreateDuplicate(meshData, indices, v1.TriangleIndex);
				int v2CopyIndex = GetCopyOfVertexOrCreateDuplicate(meshData, indices, v2.TriangleIndex);

				triangle0 = new Triangle(v0.TriangleIndex, v1CopyIndex, v2CopyIndex);
				//Debug.Log($"Duplicated vertex {v1.TriangleIndex} -> {v1CopyIndex}");
				//Debug.Log($"Duplicated vertex {v2.TriangleIndex} -> {v2CopyIndex}");
			}
			meshData.AddTriangle(triangle0);
			//if (IsOneOfThemTriangles(triangle0)) DisplayProblemTriangle(triangle0, meshData, "DuplicateLineVerticesOnPlaneAndAddTriangleToMeshData");
		}

		private static void CutTriangleInTwoTrianglesWherePlaneIntersectsAndAddThemToMeshData(
			MeshData meshData, Vertex v0, Vertex v1, Vertex v2, DuplicateIndices duplicateIndices, Plane plane
		)
		{
			//Debug.Log($"Slicing triangle {v0.TriangleIndex}, {v1.TriangleIndex}, {v2.TriangleIndex} in CutTriangleInTwoTrianglesWherePlaneIntersectsAndAddThemToMeshData");
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
				var sideOfV1Indices = duplicateIndices.GetHullIndices(v1.PlaneSide);

				int v0CopyIndexSameSideAsV1 = GetCopyOfVertexOrCreateDuplicate(
					meshData, sideOfV1Indices.SameSide, v0.TriangleIndex
				);
				int v0CopyIndexOtherSideAsV1 = GetCopyOfVertexOrCreateDuplicate(
					meshData, sideOfV1Indices.OtherSide, v0.TriangleIndex
				);

				int v12IndexSameSideOfV1 = GetCopyOfInterpolatedVertexOrCreateDuplicate(
					meshData, sideOfV1Indices.SameSide, v1.TriangleIndex, v2.TriangleIndex, t
				);
				int v12OIndextherSideOfV1 = GetCopyOfInterpolatedVertexOrCreateDuplicate(
					meshData, sideOfV1Indices.OtherSide, v1.TriangleIndex, v2.TriangleIndex, t
				);

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
				var sideOfV0Indices = duplicateIndices.GetHullIndices(v0.PlaneSide);

				int i1CopySameSideP0 = GetCopyOfVertexOrCreateDuplicate(
					meshData, sideOfV0Indices.SameSide, v1.TriangleIndex
				);
				int i1CopyOtherSideP0 = GetCopyOfVertexOrCreateDuplicate(
					meshData, sideOfV0Indices.OtherSide, v1.TriangleIndex
				);

				int i02SameSideP0 = GetCopyOfInterpolatedVertexOrCreateDuplicate(
					meshData, sideOfV0Indices.SameSide, v0.TriangleIndex, v2.TriangleIndex, t
				);
				int i02OtherSideP0 = GetCopyOfInterpolatedVertexOrCreateDuplicate(
					meshData, sideOfV0Indices.OtherSide, v0.TriangleIndex, v2.TriangleIndex, t
				);

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
				var sideOfV0Indices = duplicateIndices.GetHullIndices(v0.PlaneSide);

				int i2CopySameSideP0 = GetCopyOfVertexOrCreateDuplicate(
					meshData, sideOfV0Indices.SameSide, v2.TriangleIndex
				);
				int i2CopyOtherSideP0 = GetCopyOfVertexOrCreateDuplicate(
					meshData, sideOfV0Indices.OtherSide, v2.TriangleIndex
				);
				
				int i01SameSideP0 = GetCopyOfInterpolatedVertexOrCreateDuplicate(
					meshData, sideOfV0Indices.SameSide, v0.TriangleIndex, v1.TriangleIndex, t
				);
				int i01OtherSideP0 = GetCopyOfInterpolatedVertexOrCreateDuplicate(
					meshData, sideOfV0Indices.OtherSide, v0.TriangleIndex, v1.TriangleIndex, t
				);

				triangle0 = new Triangle(v0.TriangleIndex, i01SameSideP0, i2CopySameSideP0);
				triangle1 = new Triangle(i01OtherSideP0, v1.TriangleIndex, i2CopyOtherSideP0);
			}
			meshData.AddTriangle(triangle0);
			meshData.AddTriangle(triangle1);

			//if (IsOneOfThemTriangles(triangle0)) DisplayProblemTriangle(triangle0, meshData, "CutTriangleInTwoTrianglesWherePlaneIntersectsAndAddThemToMeshData");
			//if (IsOneOfThemTriangles(triangle1)) DisplayProblemTriangle(triangle1, meshData, "CutTriangleInTwoTrianglesWherePlaneIntersectsAndAddThemToMeshData");
		}

		private static void CutTriangleInThreeTrianglesWherePlaneIntersetsAndAddThemToMeshData(
			MeshData meshData, Vertex v0, Vertex v1, Vertex v2, DuplicateIndices duplicateIndices, Plane plane
		)
		{
			//Debug.Log($"Slicing triangle {v0.TriangleIndex}, {v1.TriangleIndex}, {v2.TriangleIndex} in CutTriangleInThreeTrianglesWherePlaneIntersetsAndAddThemToMeshData");
			float t, t1, t2;
			Triangle triangle0 = new Triangle();
			Triangle triangle1 = new Triangle();
			Triangle triangle2 = new Triangle();

			if (v0.PlaneSide != v1.PlaneSide && plane.Raycast(v0.Position, v1.Position, out t))
			{
				var sideOfV1Indices = duplicateIndices.GetHullIndices(v1.PlaneSide);

				int i01SameSideP1 = GetCopyOfInterpolatedVertexOrCreateDuplicate(
					meshData, sideOfV1Indices.SameSide, v0.TriangleIndex, v1.TriangleIndex, t
				);
				int i01OtherSideP1 = GetCopyOfInterpolatedVertexOrCreateDuplicate(
					meshData, sideOfV1Indices.OtherSide, v0.TriangleIndex, v1.TriangleIndex, t
				);

				if (v0.PlaneSide == v2.PlaneSide && plane.Raycast(v1.Position, v2.Position, out t1))
				{
					//        x v1
					//   v12 / \ v01
					// -----*---*----- Plane
					//     /     \
					//    x-------x
					//    v2      v0
					int i12SameSideP1 = GetCopyOfInterpolatedVertexOrCreateDuplicate(
						meshData, sideOfV1Indices.SameSide, v1.TriangleIndex, v2.TriangleIndex, t1
					);
					int i12OtherSideP1 = GetCopyOfInterpolatedVertexOrCreateDuplicate(
						meshData, sideOfV1Indices.OtherSide, v1.TriangleIndex, v2.TriangleIndex, t1
					);
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
					int i02SameSideP1 = GetCopyOfInterpolatedVertexOrCreateDuplicate(
						meshData, sideOfV1Indices.SameSide, v0.TriangleIndex, v2.TriangleIndex, t1
					);
					int i02OtherSideP1 = GetCopyOfInterpolatedVertexOrCreateDuplicate(
						meshData, sideOfV1Indices.OtherSide, v0.TriangleIndex, v2.TriangleIndex, t1
					);
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
				var sideOfV2Indices = duplicateIndices.GetHullIndices(v2.PlaneSide);

				int i20SameSideP2 = GetCopyOfInterpolatedVertexOrCreateDuplicate(
					meshData, sideOfV2Indices.SameSide, v2.TriangleIndex, v0.TriangleIndex, t1
				);
				int i20OtherSideP2 = GetCopyOfInterpolatedVertexOrCreateDuplicate(
					meshData, sideOfV2Indices.OtherSide, v2.TriangleIndex, v0.TriangleIndex, t1
				);

				int i21SameSideP2 = GetCopyOfInterpolatedVertexOrCreateDuplicate(
					meshData, sideOfV2Indices.SameSide, v2.TriangleIndex, v1.TriangleIndex, t2
				);
				int i21OtherSideP2 = GetCopyOfInterpolatedVertexOrCreateDuplicate(
					meshData, sideOfV2Indices.OtherSide, v2.TriangleIndex, v1.TriangleIndex, t2
				);
				triangle0 = new Triangle(i20SameSideP2, i21SameSideP2, v2.TriangleIndex);
				triangle1 = new Triangle(v0.TriangleIndex, i21OtherSideP2, i20OtherSideP2);
				triangle2 = new Triangle(v0.TriangleIndex, v1.TriangleIndex, i21OtherSideP2);
			}
			meshData.AddTriangle(triangle0);
			meshData.AddTriangle(triangle1);
			meshData.AddTriangle(triangle2);

			//if (IsOneOfThemTriangles(triangle0)) DisplayProblemTriangle(triangle0, meshData, "CutTriangleInThreeTrianglesWherePlaneIntersetsAndAddThemToMeshData");
			//if (IsOneOfThemTriangles(triangle1)) DisplayProblemTriangle(triangle1, meshData, "CutTriangleInThreeTrianglesWherePlaneIntersetsAndAddThemToMeshData");
			//if (IsOneOfThemTriangles(triangle2)) DisplayProblemTriangle(triangle2, meshData, "CutTriangleInThreeTrianglesWherePlaneIntersetsAndAddThemToMeshData");
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

			if (!(zoneDuplicateIndices.TryGetValue(pair, out indexCopy)
				|| zoneDuplicateIndices.TryGetValue(reversedPair, out indexCopy)))
			{
				Vertex v0 = meshData.GetVertexAt(index0);
				Vertex v1 = meshData.GetVertexAt(index1);

				indexCopy = AddCopyOfVertexToMeshDataAndGetIndex(meshData, Vertex.Lerp(v0, v1, t));
				zoneDuplicateIndices.Add(pair, indexCopy);
			}
			return indexCopy;
		}

		private static void GetSlicesAndAddThemToMeshData(MeshData meshData, Plane[] planes, ICollection<DuplicateIndices> duplicateIndices)
		{
			// Get the side for each vertex
			Dictionary<int, Slice> slices = new Dictionary<int, Slice>();
			//int[] sides = new int[meshData.Vertices.Count];
			int sidesNum = -1;
			for (int vertexIndex = 0; vertexIndex < meshData.Vertices.Count; ++vertexIndex)
			{
				int vertexSide = 0;
				bool isOnBorder = false;
				for (int planeIndex = 0; planeIndex < planes.Length; ++planeIndex)
				{
					Plane.Side side = planes[planeIndex].GetSide(meshData.Vertices[vertexIndex]);

					switch (side)
					{
						case Plane.Side.up:
							vertexSide |= 1 << planeIndex;
							break;

						case Plane.Side.down:
							vertexSide &= ~(1 << planeIndex);
							break;

						case Plane.Side.on:
							foreach (DuplicateIndices di in duplicateIndices)
							{
								if (di.UpperHullIndices.Contains(vertexIndex))
								{
									vertexSide |= 1 << planeIndex;
									isOnBorder = true;
									break;
								}
								if (di.LowerHullIndices.Contains(vertexIndex))
								{
									vertexSide &= ~(1 << planeIndex);
									isOnBorder = true;
									break;
								}
							}
							break;
					}
				}
				sidesNum = Mathf.Max(sidesNum, vertexSide);
				//sides[vertexIndex] = vertexSide;
				Slice slice = null;
				if (!slices.TryGetValue(vertexSide, out slice))
				{
					slice = new Slice(new SliceAllIndicesCollection(), new SliceBorderIndicesCollection());
					slices.Add(vertexSide, slice);
				}

				slice.AllIndices.Add(vertexIndex);
				if (isOnBorder)
				{
					slice.BorderIndices.Add(vertexIndex);
				}
			}

			foreach (Slice slice in slices.Values)
			{
				meshData.Slices.Add(slice);
			}

			/*
			++sidesNum;
			Debug.Log($"Number of sides: {sidesNum}");

			GameObject[] sidesObjects = new GameObject[sidesNum];
			for (int i = 0; i < sidesNum; ++i)
			{
				sidesObjects[i] = new GameObject($"Side {i}");
			}

			Color[] colors = new Color[] { Color.red, Color.green, Color.blue, Color.magenta, Color.cyan };
			for (int vertexIndex = 0; vertexIndex < meshData.Vertices.Count; ++vertexIndex)
			{
				GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				obj.transform.name = $"Vertex {vertexIndex} Side {sides[vertexIndex]}";
				obj.transform.parent = sidesObjects[sides[vertexIndex]].transform;
				obj.transform.position = meshData.Vertices[vertexIndex];
				obj.transform.localScale = new Vector3(0.008f, 0.008f, 0.008f);
				obj.GetComponent<Renderer>().material.color = colors[sides[vertexIndex]];
			}
			*/
		}

		private static void RemoveTrianglesWithIndicesInDifferentSlices(MeshData meshData)
		{
			int[] verticesSlices = new int[meshData.Vertices.Count];
			for (int vertexIndex = 0; vertexIndex < meshData.Vertices.Count; ++vertexIndex)
			{
				verticesSlices[vertexIndex] = -1;
				for (int sliceIndex = 0; sliceIndex < meshData.Slices.Count; ++sliceIndex)
				{
					if (meshData.Slices[sliceIndex].AllIndices.Contains(vertexIndex))
					{
						verticesSlices[vertexIndex] = sliceIndex;
						break;
					}
				}
			}

			for (int triangleIndex = meshData.Triangles.Count - 1; triangleIndex >= 0; --triangleIndex)
			{
				Triangle t = meshData.Triangles[triangleIndex];
				int ri0 = verticesSlices[t.I0];
				int ri1 = verticesSlices[t.I1];
				int ri2 = verticesSlices[t.I2];

				if (ri0 != ri1 || ri0 != ri2 || ri1 != ri2)
				{
					Debug.Log($"Deleting triangle {t} in zones {ri0}. {ri1}, {ri2}");
					meshData.Triangles.RemoveAt(triangleIndex);
				}
			}
		}
	}
}
