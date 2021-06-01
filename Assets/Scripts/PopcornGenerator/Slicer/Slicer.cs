using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace PopcornGenerator
{
	// TODO Bug: some triangles are flipped
	internal static class Slicer
	{
		public static IList<Slice> SliceMesh(Mesh mesh, IList<Plane> cuttingPlanes)
		{
			IList<Slice> resultingSlices = SliceMeshIntoSlices(mesh, cuttingPlanes);
			SetMeshesBorderIndices(resultingSlices, cuttingPlanes);
			return resultingSlices;
		}

		private static IList<Slice> SliceMeshIntoSlices(Mesh mesh, IList<Plane> cuttingPlanes)
		{
			int approxNumberOfSlices = (int)System.Math.Pow(2, cuttingPlanes.Count);
			var toCutSliceWrappers = new List<SliceBuildingWrapper>(approxNumberOfSlices);
			var resultingSliceWrappers = new List<SliceBuildingWrapper>(approxNumberOfSlices);

			toCutSliceWrappers.Add(new SliceBuildingWrapper(new Slice(mesh)));

			for (int planeIndex = 0; planeIndex < cuttingPlanes.Count; ++planeIndex)
			{
				for (int toCutIndex = 0; toCutIndex < toCutSliceWrappers.Count; ++toCutIndex)
				{
					TwoSliceBuildingsWrapper twoSlices = SliceSlice(
						toCutSliceWrappers[toCutIndex].Slice, cuttingPlanes[planeIndex]
					);
					resultingSliceWrappers.Add(twoSlices.LowerSliceWrapper);
					resultingSliceWrappers.Add(twoSlices.UpperSliceWrapper);
				}

				if (planeIndex != cuttingPlanes.Count - 1)
				{
					// Instead of copying the elements from one list to the other,
					// we just interchange the references
					// It is faster this way
					var auxList = toCutSliceWrappers;
					toCutSliceWrappers = resultingSliceWrappers;
					resultingSliceWrappers = auxList;
					resultingSliceWrappers.Clear();
				}
			}
			return ConvertSliceBuildingWrappersToSlices(resultingSliceWrappers);
		}

		private static IList<Slice> ConvertSliceBuildingWrappersToSlices(
			IList<SliceBuildingWrapper> sliceBuildingWrappers
		)
		{
			IList<Slice> resultingSlices = new List<Slice>(sliceBuildingWrappers.Count);
			for (int sliceIndex = 0; sliceIndex < sliceBuildingWrappers.Count; ++sliceIndex)
			{
				resultingSlices.Add(sliceBuildingWrappers[sliceIndex].Slice);
			}
			return resultingSlices;
		}

		private static void SetMeshesBorderIndices(IList<Slice> resultingSlices, IList<Plane> cuttingPlanes)
		{
			// TODO Find a way to get 'intersecting' indices when we get only one cutting plane
			if (cuttingPlanes.Count == 1)
			{
				Debug.LogWarning("Slicer -> SetMeshesBorderIndices: " +
					"Number of cutting planes is 1. Will not set intersecting planes indices");
			}

			for (int sliceIndex = 0; sliceIndex < resultingSlices.Count; ++sliceIndex)
			{
				Slice slice = resultingSlices[sliceIndex];
				IList<Vertex> vertices = slice.Mesh.Vertices;
				for (int vertexIndex = 0; vertexIndex < vertices.Count; ++vertexIndex)
				{
					Vertex vertex = vertices[vertexIndex];
					int numOfPlanesTheVertexIsOn = 0;
					for (int planeIndex = 0; planeIndex < cuttingPlanes.Count; ++planeIndex)
					{
						if (cuttingPlanes[planeIndex].GetSide(vertex.Position) == Plane.Side.on)
						{
							if (++numOfPlanesTheVertexIsOn == 1)
							{
								slice.Border.AddIndex(vertexIndex);
							}
						}
					}

					if (cuttingPlanes.Count > 1 && numOfPlanesTheVertexIsOn == cuttingPlanes.Count)
					{
						slice.Border.AddIntersectingPlanesIndex(vertexIndex);
					}
				}
			}
		}

		private static TwoSliceBuildingsWrapper SliceSlice(Slice slice, Plane cuttingPlane)
		{
			TwoSliceBuildingsWrapper twoSliceWrappers = new TwoSliceBuildingsWrapper
			(
				new SliceBuildingWrapper(new Slice(new Mesh(slice.Mesh.HasNormals, slice.Mesh.HasUVs))),
				new SliceBuildingWrapper(new Slice(new Mesh(slice.Mesh.HasNormals, slice.Mesh.HasUVs)))
			);

			foreach (Triangle triangle in slice.Mesh.Triangles)
			{
				SlicerTriangleWrapper triangleWrapper = new SlicerTriangleWrapper(
					triangle,
					slice.Mesh.Vertices[triangle.I0],
					slice.Mesh.Vertices[triangle.I1],
					slice.Mesh.Vertices[triangle.I2],
					cuttingPlane.GetSide(slice.Mesh.Vertices[triangle.I0].Position),
					cuttingPlane.GetSide(slice.Mesh.Vertices[triangle.I1].Position),
					cuttingPlane.GetSide(slice.Mesh.Vertices[triangle.I2].Position)
				);

				if (!IsTriangleIntersectingPlane(triangleWrapper.S0, triangleWrapper.S1, triangleWrapper.S2))
				{
					AddTriangleToSliceWhenAllVerticesAreOnSameSide(twoSliceWrappers, triangleWrapper);
				}
				else if (IsOnePointOnPlaneAndOthersOnSameSide(triangleWrapper.S0, triangleWrapper.S1, triangleWrapper.S2))
				{
					AddTriangleToSliceWhenOneVertexIsOnPlane(twoSliceWrappers, triangleWrapper);
				}
				else if (IsOneEdgeOnToPlane(triangleWrapper.S0, triangleWrapper.S1, triangleWrapper.S2))
				{
					AddTriangleToSliceWhenTwoVerticesAreOnPlane(twoSliceWrappers, triangleWrapper);
				}
				else if (IsOnePointOnPlaneAndOthersOnDifferentSides(triangleWrapper.S0, triangleWrapper.S1, triangleWrapper.S2))
				{
					SliceTriangleWithOneVertexOnPlane(twoSliceWrappers, triangleWrapper, cuttingPlane);
				}
				else // Plane intersects triangle and none of the points are on the plane
				{
					SliceTriangleWithNoVertexOnPlane(twoSliceWrappers, triangleWrapper, cuttingPlane);
				}
			}

			return twoSliceWrappers;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool IsTriangleIntersectingPlane(Plane.Side s0, Plane.Side s1, Plane.Side s2)
		{
			return !(s0 == s1 && s0 == s2);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool IsOneEdgeOnToPlane(Plane.Side s0, Plane.Side s1, Plane.Side s2)
		{
			return (s0 == Plane.Side.on && s1 == Plane.Side.on)
				|| (s0 == Plane.Side.on && s2 == Plane.Side.on)
				|| (s1 == Plane.Side.on && s2 == Plane.Side.on);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool IsOnePointOnPlaneAndOthersOnSameSide(Plane.Side s0, Plane.Side s1, Plane.Side s2)
		{
			return (s0 == Plane.Side.on && s1 != Plane.Side.on && s1 == s2)
				|| (s1 == Plane.Side.on && s0 != Plane.Side.on && s0 == s2)
				|| (s2 == Plane.Side.on && s0 != Plane.Side.on && s0 == s1);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool IsOnePointOnPlaneAndOthersOnDifferentSides(Plane.Side s0, Plane.Side s1, Plane.Side s2)
		{
			return (s0 == Plane.Side.on && s1 != Plane.Side.on && s2 != Plane.Side.on && s1 != s2)
				|| (s0 != Plane.Side.on && s1 == Plane.Side.on && s2 != Plane.Side.on && s0 != s2)
				|| (s0 != Plane.Side.on && s1 != Plane.Side.on && s2 == Plane.Side.on && s0 != s1);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void AddTriangleToSliceWhenNoCutsAreMade(
			TwoSliceBuildingsWrapper twoSliceWrappers, SlicerTriangleWrapper triangleWrapper,
			Plane.Side sideNotOnPlane
		)
		{
			SliceBuildingWrapper wrapper = twoSliceWrappers.GetWrapperSameSide(sideNotOnPlane);
			int i0 = wrapper.GetVertexIndex(triangleWrapper.I0, triangleWrapper.V0);
			int i1 = wrapper.GetVertexIndex(triangleWrapper.I1, triangleWrapper.V1);
			int i2 = wrapper.GetVertexIndex(triangleWrapper.I2, triangleWrapper.V2);

			wrapper.Slice.Mesh.Triangles.Add(new Triangle(i0, i1, i2));
		}

		private static void AddTriangleToSliceWhenAllVerticesAreOnSameSide(
			TwoSliceBuildingsWrapper twoSliceWrappers, SlicerTriangleWrapper triangleWrapper
		)
		{
			AddTriangleToSliceWhenNoCutsAreMade(twoSliceWrappers, triangleWrapper, triangleWrapper.S0);
		}

		private static void AddTriangleToSliceWhenOneVertexIsOnPlane(
			TwoSliceBuildingsWrapper twoSliceWrappers, SlicerTriangleWrapper triangleWrapper
		)
		{
			if (triangleWrapper.S0 == Plane.Side.on)
			{
				//  v2 x-----x v1
				//      \   /
				//       \ /
				// -------x------- Plane
				//        v0
				AddTriangleToSliceWhenNoCutsAreMade(twoSliceWrappers, triangleWrapper, triangleWrapper.S1);
			}
			else if (triangleWrapper.S1 == Plane.Side.on)
			{
				//  v0 x-----x v2
				//      \   /
				//       \ /
				// -------x------- Plane
				//        v1
				AddTriangleToSliceWhenNoCutsAreMade(twoSliceWrappers, triangleWrapper, triangleWrapper.S0);
			}
			else if (triangleWrapper.S2 == Plane.Side.on)
			{
				//  v1 x-----x v0
				//      \   /
				//       \ /
				// -------x------- Plane
				//        v2
				AddTriangleToSliceWhenNoCutsAreMade(twoSliceWrappers, triangleWrapper, triangleWrapper.S0);
			}
		}

		private static void AddTriangleToSliceWhenTwoVerticesAreOnPlane(
			TwoSliceBuildingsWrapper twoSliceWrappers, SlicerTriangleWrapper triangleWrapper
		)
		{
			if (triangleWrapper.S0 == Plane.Side.on && triangleWrapper.S1 == Plane.Side.on)
			{
				//        x v2
				//       / \
				//      /   \
				// ----x-----x---- Plane
				//     v0    v1
				AddTriangleToSliceWhenNoCutsAreMade(twoSliceWrappers, triangleWrapper, triangleWrapper.S2);
			}
			else if (triangleWrapper.S0 == Plane.Side.on && triangleWrapper.S2 == Plane.Side.on)
			{
				//        x v1
				//       / \
				//      /   \
				// ----x-----x---- Plane
				//     v2    v0
				AddTriangleToSliceWhenNoCutsAreMade(twoSliceWrappers, triangleWrapper, triangleWrapper.S1);
			}
			else if (triangleWrapper.S1 == Plane.Side.on && triangleWrapper.S2 == Plane.Side.on)
			{
				//        x v0
				//       / \
				//      /   \
				// ----x-----x---- Plane
				//     v1    v2
				AddTriangleToSliceWhenNoCutsAreMade(twoSliceWrappers, triangleWrapper, triangleWrapper.S0);
			}
		}

		// I am not going to make this part any pretty. I have no idea why those indices have to
		// be in those specific orders. I found them empirically. Some go clockwise, some don't.
		// It's all a mess, I don't know why and I don't want to know why. I will leave them as
		// they are. If you have a working fix, hit me with the DMs.
		private static void SliceTriangleWithOneVertexOnPlane(
			TwoSliceBuildingsWrapper twoSliceWrappers, SlicerTriangleWrapper triangleWrapper, Plane cuttingPlane
		)
		{
			float t;
			if (triangleWrapper.S0 == Plane.Side.on
				&& cuttingPlane.Raycast(triangleWrapper.V1.Position, triangleWrapper.V2.Position, out t))
			{
				//        x v2
				//       /|
				//   v0 / | v12
				// ----x--+------- Plane
				//      \ |
				//       \|
				//        x v1
				Vertex v12 = Vertex.Lerp(triangleWrapper.V1, triangleWrapper.V2, t);
				SliceBuildingWrapper wrapperSideOfV1 = twoSliceWrappers.GetWrapperSameSide(triangleWrapper.S1);
				SliceBuildingWrapper wrapperSideOfV2 = twoSliceWrappers.GetWrapperSameSide(triangleWrapper.S2);

				int i0SideOfV1 = wrapperSideOfV1.GetVertexIndex(triangleWrapper.I0, triangleWrapper.V0);
				int i12SideOfV1 = wrapperSideOfV1.GetInterpolatedVertexIndex(triangleWrapper.I1, triangleWrapper.I2, v12);
				int i1SideOfV1 = wrapperSideOfV1.GetVertexIndex(triangleWrapper.I1, triangleWrapper.V1);

				int i0SideOfV2 = wrapperSideOfV2.GetVertexIndex(triangleWrapper.I0, triangleWrapper.V0);
				int i12SideOfV2 = wrapperSideOfV2.GetInterpolatedVertexIndex(triangleWrapper.I1, triangleWrapper.I2, v12);
				int i2SideOfV2 = wrapperSideOfV2.GetVertexIndex(triangleWrapper.I2, triangleWrapper.V2);

				wrapperSideOfV2.Slice.Mesh.Triangles.Add(new Triangle(i0SideOfV2, i12SideOfV2, i2SideOfV2));
				wrapperSideOfV1.Slice.Mesh.Triangles.Add(new Triangle(i1SideOfV1, i12SideOfV1, i0SideOfV1));
			}
			else if (triangleWrapper.S1 == Plane.Side.on
				&& cuttingPlane.Raycast(triangleWrapper.V0.Position, triangleWrapper.V2.Position, out t))
			{
				//        x v0
				//       /|
				//   v1 / | v02
				// ----x--+------- Plane
				//      \ |
				//       \|
				//        x v2
				Vertex v02 = Vertex.Lerp(triangleWrapper.V0, triangleWrapper.V2, t);
				SliceBuildingWrapper wrapperSideOfV0 = twoSliceWrappers.GetWrapperSameSide(triangleWrapper.S0);
				SliceBuildingWrapper wrapperSideOfV2 = twoSliceWrappers.GetWrapperSameSide(triangleWrapper.S2);

				int i1SideOfV0 = wrapperSideOfV0.GetVertexIndex(triangleWrapper.I1, triangleWrapper.V1);
				int i02SideOfV0 = wrapperSideOfV0.GetInterpolatedVertexIndex(triangleWrapper.I0, triangleWrapper.I2, v02);
				int i0SideOfV0 = wrapperSideOfV0.GetVertexIndex(triangleWrapper.I0, triangleWrapper.V0);

				int i1SideOfV2 = wrapperSideOfV2.GetVertexIndex(triangleWrapper.I1, triangleWrapper.V1);
				int i02SideOfV2 = wrapperSideOfV2.GetInterpolatedVertexIndex(triangleWrapper.I0, triangleWrapper.I2, v02);
				int i2SideOfV2 = wrapperSideOfV2.GetVertexIndex(triangleWrapper.I2, triangleWrapper.V2);

				wrapperSideOfV0.Slice.Mesh.Triangles.Add(new Triangle(i1SideOfV0, i02SideOfV0, i0SideOfV0));
				wrapperSideOfV2.Slice.Mesh.Triangles.Add(new Triangle(i1SideOfV2, i02SideOfV2, i2SideOfV2));
			}
			else if (triangleWrapper.S2 == Plane.Side.on
				&& cuttingPlane.Raycast(triangleWrapper.V0.Position, triangleWrapper.V1.Position, out t))
			{
				//        x v1
				//       /|
				//   v2 / | v01
				// ----x--+------- Plane
				//      \ |
				//       \|
				//        x v0
				Vertex v01 = Vertex.Lerp(triangleWrapper.V0, triangleWrapper.V1, t);
				SliceBuildingWrapper wrapperSideOfV0 = twoSliceWrappers.GetWrapperSameSide(triangleWrapper.S0);
				SliceBuildingWrapper wrapperSideOfV1 = twoSliceWrappers.GetWrapperSameSide(triangleWrapper.S1);

				int i2SideOfV0 = wrapperSideOfV0.GetVertexIndex(triangleWrapper.I2, triangleWrapper.V2);
				int i01SideOfV0 = wrapperSideOfV0.GetInterpolatedVertexIndex(triangleWrapper.I0, triangleWrapper.I1, v01);
				int i0SideOfV0 = wrapperSideOfV0.GetVertexIndex(triangleWrapper.I0, triangleWrapper.V0);

				int i2SideOfV1 = wrapperSideOfV1.GetVertexIndex(triangleWrapper.I2, triangleWrapper.V2);
				int i01SideOfV1 = wrapperSideOfV1.GetInterpolatedVertexIndex(triangleWrapper.I0, triangleWrapper.I1, v01);
				int i1SideOfV1 = wrapperSideOfV1.GetVertexIndex(triangleWrapper.I1, triangleWrapper.V1);

				wrapperSideOfV1.Slice.Mesh.Triangles.Add(new Triangle(i1SideOfV1, i01SideOfV1, i2SideOfV1));
				wrapperSideOfV0.Slice.Mesh.Triangles.Add(new Triangle(i0SideOfV0, i01SideOfV0, i2SideOfV0));
			}
		}

		private static void SliceTriangleWithNoVertexOnPlane(
			TwoSliceBuildingsWrapper twoSliceWrappers, SlicerTriangleWrapper triangleWrapper, Plane cuttingPlane
		)
		{
			float t1, t2;
			if (triangleWrapper.S0 != triangleWrapper.S1
				&& cuttingPlane.Raycast(triangleWrapper.V0.Position, triangleWrapper.V1.Position, out t1))
			{
				Vertex v01 = Vertex.Lerp(triangleWrapper.V0, triangleWrapper.V1, t1);
				SliceBuildingWrapper wrapperSideOfV1 = twoSliceWrappers.GetWrapperSameSide(triangleWrapper.S1);

				int i01SideOfV1 = wrapperSideOfV1.GetInterpolatedVertexIndex(triangleWrapper.I0, triangleWrapper.I1, v01);
				int i1SideOfV1 = wrapperSideOfV1.GetVertexIndex(triangleWrapper.I1, triangleWrapper.V1);

				if (triangleWrapper.S0 == triangleWrapper.S2
					&& cuttingPlane.Raycast(triangleWrapper.V1.Position, triangleWrapper.V2.Position, out t2))
				{
					//        x v1
					//   v12 / \ v01
					// -----*---*----- Plane
					//     /     \
					//    x-------x
					//    v2      v0
					Vertex v12 = Vertex.Lerp(triangleWrapper.V1, triangleWrapper.V2, t2);
					int i12SideOfV1 = wrapperSideOfV1.GetInterpolatedVertexIndex(triangleWrapper.I1, triangleWrapper.I2, v12);

					SliceBuildingWrapper wrapperSideOfV2 = twoSliceWrappers.GetWrapperSameSide(triangleWrapper.S2);

					int i0SideOfV2 = wrapperSideOfV2.GetVertexIndex(triangleWrapper.I0, triangleWrapper.V0);
					int i01SideOfV2 = wrapperSideOfV2.GetInterpolatedVertexIndex(triangleWrapper.I0, triangleWrapper.I1, v01);
					int i12SideOfV2 = wrapperSideOfV2.GetInterpolatedVertexIndex(triangleWrapper.I1, triangleWrapper.I2, v12);
					int i2SideOfV2 = wrapperSideOfV2.GetVertexIndex(triangleWrapper.I2, triangleWrapper.V2);

					wrapperSideOfV1.Slice.Mesh.Triangles.Add(new Triangle(i12SideOfV1, i01SideOfV1, i1SideOfV1));
					wrapperSideOfV2.Slice.Mesh.Triangles.Add(new Triangle(i0SideOfV2, i01SideOfV2, i12SideOfV2));
					wrapperSideOfV2.Slice.Mesh.Triangles.Add(new Triangle(i0SideOfV2, i12SideOfV2, i2SideOfV2));
				}
				else if (cuttingPlane.Raycast(triangleWrapper.V0.Position, triangleWrapper.V2.Position, out t2))
				{
					//        x v0
					//   v01 / \ v02
					// -----*---*----- Plane
					//     /     \
					//    x-------x
					//    v1      v2
					Vertex v02 = Vertex.Lerp(triangleWrapper.V0, triangleWrapper.V2, t2);
					int i02SideOfV1 = wrapperSideOfV1.GetInterpolatedVertexIndex(triangleWrapper.I0, triangleWrapper.I2, v02);
					int i2SideOfV1 = wrapperSideOfV1.GetVertexIndex(triangleWrapper.I2, triangleWrapper.V2);

					SliceBuildingWrapper wrapperSideOfV0 = twoSliceWrappers.GetWrapperSameSide(triangleWrapper.S0);

					int i01SideOfV0 = wrapperSideOfV0.GetInterpolatedVertexIndex(triangleWrapper.I0, triangleWrapper.I1, v01);
					int i02SideOfV0 = wrapperSideOfV0.GetInterpolatedVertexIndex(triangleWrapper.I0, triangleWrapper.I2, v02);
					int i0SideOfV0 = wrapperSideOfV0.GetVertexIndex(triangleWrapper.I0, triangleWrapper.V0);

					wrapperSideOfV0.Slice.Mesh.Triangles.Add(new Triangle(i0SideOfV0, i01SideOfV0, i02SideOfV0));
					wrapperSideOfV1.Slice.Mesh.Triangles.Add(new Triangle(i02SideOfV1, i01SideOfV1, i2SideOfV1));
					wrapperSideOfV1.Slice.Mesh.Triangles.Add(new Triangle(i01SideOfV1, i1SideOfV1, i2SideOfV1));
				}
			}
			else if (cuttingPlane.Raycast(triangleWrapper.V2.Position, triangleWrapper.V0.Position, out t1)
				&& cuttingPlane.Raycast(triangleWrapper.V2.Position, triangleWrapper.V1.Position, out t2))
			{
				//        x v2
				//   v20 / \ v21
				// -----*---*----- Plane
				//     /     \
				//    x-------x
				//    v0      v1
				Vertex v20 = Vertex.Lerp(triangleWrapper.V2, triangleWrapper.V0, t1);
				Vertex v21 = Vertex.Lerp(triangleWrapper.V2, triangleWrapper.V1, t2);

				SliceBuildingWrapper wrapperSideOfV2 = twoSliceWrappers.GetWrapperSameSide(triangleWrapper.S2);
				SliceBuildingWrapper wrapperSideOfV0 = twoSliceWrappers.GetWrapperSameSide(triangleWrapper.S0);

				int i2SideOfV2 = wrapperSideOfV2.GetVertexIndex(triangleWrapper.I2, triangleWrapper.V2);
				int i20SideOfV2 = wrapperSideOfV2.GetInterpolatedVertexIndex(triangleWrapper.I2, triangleWrapper.I0, v20);
				int i21SideOfV2 = wrapperSideOfV2.GetInterpolatedVertexIndex(triangleWrapper.I2, triangleWrapper.I1, v21);

				int i0SideOfV0 = wrapperSideOfV0.GetVertexIndex(triangleWrapper.I0, triangleWrapper.V0);
				int i1SideOfV0 = wrapperSideOfV0.GetVertexIndex(triangleWrapper.I1, triangleWrapper.V1);
				int i20SideOfV0 = wrapperSideOfV0.GetInterpolatedVertexIndex(triangleWrapper.I2, triangleWrapper.I0, v20);
				int i21SideOfV0 = wrapperSideOfV0.GetInterpolatedVertexIndex(triangleWrapper.I2, triangleWrapper.I1, v21);

				wrapperSideOfV2.Slice.Mesh.Triangles.Add(new Triangle(i20SideOfV2, i21SideOfV2, i2SideOfV2));
				wrapperSideOfV0.Slice.Mesh.Triangles.Add(new Triangle(i0SideOfV0, i21SideOfV0, i20SideOfV0));
				wrapperSideOfV0.Slice.Mesh.Triangles.Add(new Triangle(i0SideOfV0, i1SideOfV0, i21SideOfV0));
			}
		}
	}
}
