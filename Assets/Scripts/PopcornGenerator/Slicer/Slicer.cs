using System.Collections.Generic;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace PopcornGenerator
{
    // TODO Bug: some triangles are flipped
    internal static class Slicer
    {
        public static IEnumerator SliceMesh(Mesh mesh, IList<Plane> cuttingPlanes, IList<Slice> resultingSlices)
        {
            IEnumerator sliceMeshEnumerator = SliceMeshIntoSlices(mesh, cuttingPlanes, resultingSlices);
            IEnumerator setMeshBorderEnumerator = SetMeshesBorderIndices(resultingSlices, cuttingPlanes);
            yield return null;

            while (sliceMeshEnumerator.MoveNext())
            {
                yield return null;
            }
            while (setMeshBorderEnumerator.MoveNext())
            {
                yield return null;
            }
        }

        private static IEnumerator SliceMeshIntoSlices(Mesh mesh, IList<Plane> cuttingPlanes, IList<Slice> resultingSlices)
        {
            int approxNumberOfSlices = (int)System.Math.Pow(2, cuttingPlanes.Count);
            var toCutSliceWrappers = new List<SliceBuildingWrapper>(approxNumberOfSlices);
            var resultingSliceWrappers = new List<SliceBuildingWrapper>(approxNumberOfSlices);

            toCutSliceWrappers.Add(new SliceBuildingWrapper(new Slice(mesh)));

            yield return null;

            for (int planeIndex = 0; planeIndex < cuttingPlanes.Count; ++planeIndex)
            {
                for (int toCutIndex = 0; toCutIndex < toCutSliceWrappers.Count; ++toCutIndex)
                {
                    Slice slice = toCutSliceWrappers[toCutIndex].Slice;
                    TwoSliceBuildingsWrapper twoSlices = new TwoSliceBuildingsWrapper
                    (
                        new SliceBuildingWrapper(new Slice(new Mesh(slice.Mesh.HasNormals, slice.Mesh.HasUVs))),
                        new SliceBuildingWrapper(new Slice(new Mesh(slice.Mesh.HasNormals, slice.Mesh.HasUVs)))
                    );

                    IEnumerator sliceEnumerator = SliceSlice(slice, cuttingPlanes[planeIndex], twoSlices);
                    while (sliceEnumerator.MoveNext())
                    {
                        yield return null;
                    }

                    void AddIfNotNone(SliceBuildingWrapper sbw) { if (sbw.Slice.Mesh.Vertices.Count > 0) resultingSliceWrappers.Add(sbw); }
                    AddIfNotNone(twoSlices.LowerSliceWrapper);
                    AddIfNotNone(twoSlices.UpperSliceWrapper);
                    yield return null;
                }

                //CreateMeshForEachSlice(resultingSliceWrappers, planeIndex);

                bool swapListsIfLastItem = (planeIndex != cuttingPlanes.Count - 1);
                if (swapListsIfLastItem)
                {
                    // Instead of copying the elements from one list to the other,
                    // we just interchange the references. It is faster this way.
                    var auxList = toCutSliceWrappers;
                    toCutSliceWrappers = resultingSliceWrappers;
                    resultingSliceWrappers = auxList;
                    resultingSliceWrappers.Clear();
                }
            }

            ConvertSliceBuildingWrappersToSlices(resultingSliceWrappers, resultingSlices);
        }

        private static void CreateMeshForEachSlice(List<SliceBuildingWrapper> resultingSliceWrappers, int planeIndex)
        {
            GameObject goP = new GameObject($"Plane{planeIndex}");
            for (int resultIndex = 0; resultIndex < resultingSliceWrappers.Count; ++resultIndex)
            {
                GameObject go = new GameObject($"Slice{resultIndex}");
                go.transform.parent = goP.transform;
                go.AddComponent<MeshFilter>().mesh = resultingSliceWrappers[resultIndex].Slice.Mesh.ToUnityMesh();
                go.AddComponent<MeshRenderer>().material = new Material(Shader.Find("Universal Render Pipeline/Lit"));

                //ObjExporter.WriteMesh(go, $@"{System.IO.Directory.GetCurrentDirectory()}\RuntimeExports\Slices\{name}.obj");
            }
        }

        private static void ConvertSliceBuildingWrappersToSlices(IList<SliceBuildingWrapper> sliceBuildingWrappers,
                                                                 IList<Slice> resultingSlices)
        {
            for (int sliceIndex = 0; sliceIndex < sliceBuildingWrappers.Count; ++sliceIndex)
            {
                resultingSlices.Add(sliceBuildingWrappers[sliceIndex].Slice);
            }
        }

        private static IEnumerator SetMeshesBorderIndices(IList<Slice> resultingSlices, IList<Plane> cuttingPlanes)
        {
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

                    if ((cuttingPlanes.Count > 1) && (numOfPlanesTheVertexIsOn == cuttingPlanes.Count))
                    {
                        slice.Border.AddIntersectingPlanesIndex(vertexIndex);
                    }
                }
                yield return null;
            }
        }

        private static IEnumerator SliceSlice(Slice slice, Plane cuttingPlane, TwoSliceBuildingsWrapper twoSliceWrappers)
        {
            //foreach (Triangle triangle in slice.Mesh.Triangles)
            for (int triangle_index = 0; triangle_index < slice.Mesh.Triangles.Count; ++triangle_index)
            {
                Triangle triangle = slice.Mesh.Triangles[triangle_index];
                SlicerTriangleWrapper triangleWrapper = new SlicerTriangleWrapper(
                    triangle,
                    slice.Mesh.Vertices[triangle.I0],
                    slice.Mesh.Vertices[triangle.I1],
                    slice.Mesh.Vertices[triangle.I2],
                    cuttingPlane.GetSide(slice.Mesh.Vertices[triangle.I0].Position),
                    cuttingPlane.GetSide(slice.Mesh.Vertices[triangle.I1].Position),
                    cuttingPlane.GetSide(slice.Mesh.Vertices[triangle.I2].Position)
                );

                if (IsTriangleOnPlane(triangleWrapper.V0.S, triangleWrapper.V1.S, triangleWrapper.V2.S))
                {
                    throw new System.ArgumentException("All triangle vertices are on the plane");
                }
                else if (!IsTriangleIntersectingPlane(triangleWrapper.V0.S, triangleWrapper.V1.S, triangleWrapper.V2.S))
                {
                    AddTriangleToSliceWhenAllVerticesAreOnSameSide(twoSliceWrappers, triangleWrapper);
                }
                else if (IsOnePointOnPlaneAndOthersOnSameSide(triangleWrapper.V0.S, triangleWrapper.V1.S, triangleWrapper.V2.S))
                {
                    AddTriangleToSliceWhenOneVertexIsOnPlane(twoSliceWrappers, triangleWrapper);
                }
                else if (IsOneEdgeOnToPlane(triangleWrapper.V0.S, triangleWrapper.V1.S, triangleWrapper.V2.S))
                {
                    AddTriangleToSliceWhenTwoVerticesAreOnPlane(twoSliceWrappers, triangleWrapper);
                }
                else if (IsOnePointOnPlaneAndOthersOnDifferentSides(triangleWrapper.V0.S, triangleWrapper.V1.S, triangleWrapper.V2.S))
                {
                    SliceTriangleWithOneVertexOnPlane(twoSliceWrappers, triangleWrapper, cuttingPlane);
                }
                else // Plane intersects triangle and none of the points are on the plane.
                {
                    SliceTriangleWithNoVertexOnPlane(twoSliceWrappers, triangleWrapper, cuttingPlane);
                }

                if (triangle_index % 300 == 0)
                {
                    yield return null;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsTriangleOnPlane(Plane.Side s0, Plane.Side s1, Plane.Side s2)
        {
            return (s0 == Plane.Side.on) && (s1 == Plane.Side.on) && (s2 == Plane.Side.on);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsTriangleIntersectingPlane(Plane.Side s0, Plane.Side s1, Plane.Side s2)
        {
            return !((s0 == s1) && (s0 == s2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsOneEdgeOnToPlane(Plane.Side s0, Plane.Side s1, Plane.Side s2)
        {
            return ((s0 == Plane.Side.on) && (s1 == Plane.Side.on))
                || ((s0 == Plane.Side.on) && (s2 == Plane.Side.on))
                || ((s1 == Plane.Side.on) && (s2 == Plane.Side.on));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsOnePointOnPlaneAndOthersOnSameSide(Plane.Side s0, Plane.Side s1, Plane.Side s2)
        {
            return ((s0 == Plane.Side.on) && (s1 != Plane.Side.on) && (s1 == s2))
                || ((s1 == Plane.Side.on) && (s0 != Plane.Side.on) && (s0 == s2))
                || ((s2 == Plane.Side.on) && (s0 != Plane.Side.on) && (s0 == s1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsOnePointOnPlaneAndOthersOnDifferentSides(Plane.Side s0, Plane.Side s1, Plane.Side s2)
        {
            return ((s0 == Plane.Side.on) && (s1 != Plane.Side.on) && (s2 != Plane.Side.on) && (s1 != s2))
                || ((s0 != Plane.Side.on) && (s1 == Plane.Side.on) && (s2 != Plane.Side.on) && (s0 != s2))
                || ((s0 != Plane.Side.on) && (s1 != Plane.Side.on) && (s2 == Plane.Side.on) && (s0 != s1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddTriangleToSliceWhenNoCutsAreMade(TwoSliceBuildingsWrapper twoSliceWrappers,
                                                                SlicerTriangleWrapper triangleWrapper,
                                                                Plane.Side sideNotOnPlane)
        {
            SliceBuildingWrapper wrapper = twoSliceWrappers.GetWrapperSameSide(sideNotOnPlane);
            int i0 = wrapper.GetVertexIndex(triangleWrapper.V0.I, triangleWrapper.V0.V);
            int i1 = wrapper.GetVertexIndex(triangleWrapper.V1.I, triangleWrapper.V1.V);
            int i2 = wrapper.GetVertexIndex(triangleWrapper.V2.I, triangleWrapper.V2.V);

            wrapper.Slice.Mesh.Triangles.Add(new Triangle(i0, i1, i2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddTriangleToSliceWhenAllVerticesAreOnSameSide(TwoSliceBuildingsWrapper twoSliceWrappers,
                                                                           SlicerTriangleWrapper triangleWrapper)
        {
            AddTriangleToSliceWhenNoCutsAreMade(twoSliceWrappers, triangleWrapper, triangleWrapper.V0.S);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddTrianglesToSliceWhenPlanCutsTriangleInOneVertex(TwoSliceBuildingsWrapper twoSliceWrappers,
                                                                               SlicerTriangleOneVertexOnPlaneWrapper triangleWrapper,
                                                                               float t)
        {
            //            x vAbove
            //           /|
            // vOnPlane / | v12
            // --------x--+--- Plane
            //          \ |
            //           \|
            //            x vBelow
            Vertex vInterp = Vertex.Lerp(triangleWrapper.VAbove.V, triangleWrapper.VBelow.V, t);
            SliceBuildingWrapper wrapperSideOfVAbove = twoSliceWrappers.GetWrapperSameSide(triangleWrapper.VAbove.S);
            SliceBuildingWrapper wrapperSideOfVBelow = twoSliceWrappers.GetWrapperSameSide(triangleWrapper.VBelow.S);

            int iAboveSideOfVAbove = wrapperSideOfVAbove.GetVertexIndex(triangleWrapper.VAbove.I, triangleWrapper.VAbove.V);
            int iInterpSideOfVAbove = wrapperSideOfVAbove.GetInterpolatedVertexIndex(triangleWrapper.VAbove.I,
                                                                                     triangleWrapper.VBelow.I, vInterp);
            int iOnPlaneSideOfVAbove = wrapperSideOfVAbove.GetVertexIndex(triangleWrapper.VOnPlane.I,
                                                                          triangleWrapper.VOnPlane.V);

            int iOnPlaneSideOfVBelow = wrapperSideOfVBelow.GetVertexIndex(triangleWrapper.VOnPlane.I,
                                                                          triangleWrapper.VOnPlane.V);
            int iInterpSideOfVBelow = wrapperSideOfVBelow.GetInterpolatedVertexIndex(triangleWrapper.VAbove.I,
                                                                                     triangleWrapper.VBelow.I, vInterp);
            int iBelowSideOfVBelow = wrapperSideOfVBelow.GetVertexIndex(triangleWrapper.VBelow.I,
                                                                        triangleWrapper.VBelow.V);

            wrapperSideOfVAbove.Slice.Mesh.Triangles.Add(new Triangle(iAboveSideOfVAbove, iInterpSideOfVAbove,
                                                                      iOnPlaneSideOfVAbove));
            wrapperSideOfVBelow.Slice.Mesh.Triangles.Add(new Triangle(iOnPlaneSideOfVBelow, iInterpSideOfVBelow,
                                                                      iBelowSideOfVBelow));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddTrianglesToSliceWhenPlaneCutsTriangleInTwoPoints(TwoSliceBuildingsWrapper twoSliceWrappers,
                                                                                SlicerTriangleNoVertexOnPlaneWrapper triangleWrapper,
                                                                                float tLeft,
                                                                                float tRight)
        {
            //              x vAbove
            // vInterpLeft / \ vInterpRight
            // -----------*---*----- Plane
            //           / _/  \
            //          x-------x
            // vBelowLeft       vBelowRight
            Vertex vInterpRight = Vertex.Lerp(triangleWrapper.VAbove.V, triangleWrapper.VBelowRight.V, tLeft);
            Vertex vInterpLeft = Vertex.Lerp(triangleWrapper.VAbove.V, triangleWrapper.VBelowLeft.V, tRight);

            SliceBuildingWrapper wrapperSideOfVAbove = twoSliceWrappers.GetWrapperSameSide(triangleWrapper.VAbove.S);
            SliceBuildingWrapper wrapperSideOfVBelow = twoSliceWrappers.GetWrapperSameSide(triangleWrapper.VBelowRight.S);

            int iAboveSideOfVAbove = wrapperSideOfVAbove.GetVertexIndex(triangleWrapper.VAbove.I,
                                                                        triangleWrapper.VAbove.V);
            int iInterpRightSideOfVAbove = wrapperSideOfVAbove.GetInterpolatedVertexIndex(triangleWrapper.VAbove.I,
                                                                                          triangleWrapper.VBelowRight.I,
                                                                                          vInterpRight);
            int iInterpLeftSideOfVAbove = wrapperSideOfVAbove.GetInterpolatedVertexIndex(triangleWrapper.VAbove.I,
                                                                                         triangleWrapper.VBelowLeft.I,
                                                                                         vInterpLeft);

            int iBelowLeftSideOfVBelow = wrapperSideOfVBelow.GetVertexIndex(triangleWrapper.VBelowRight.I,
                                                                            triangleWrapper.VBelowRight.V);
            int iBelowRightSideOfVBelow = wrapperSideOfVBelow.GetVertexIndex(triangleWrapper.VBelowLeft.I,
                                                                             triangleWrapper.VBelowLeft.V);
            int iInterpRightSideOfVBelow = wrapperSideOfVBelow.GetInterpolatedVertexIndex(triangleWrapper.VAbove.I,
                                                                                          triangleWrapper.VBelowRight.I,
                                                                                          vInterpRight);
            int iInterpLeftSideOfVBelow = wrapperSideOfVBelow.GetInterpolatedVertexIndex(triangleWrapper.VAbove.I,
                                                                                         triangleWrapper.VBelowLeft.I,
                                                                                         vInterpLeft);

            wrapperSideOfVAbove.Slice.Mesh.Triangles.Add(new Triangle(iAboveSideOfVAbove,
                                                                      iInterpRightSideOfVAbove,
                                                                      iInterpLeftSideOfVAbove));
            wrapperSideOfVBelow.Slice.Mesh.Triangles.Add(new Triangle(iBelowRightSideOfVBelow,
                                                                      iInterpLeftSideOfVBelow,
                                                                      iInterpRightSideOfVBelow));
            wrapperSideOfVBelow.Slice.Mesh.Triangles.Add(new Triangle(iBelowRightSideOfVBelow,
                                                                      iInterpRightSideOfVBelow,
                                                                      iBelowLeftSideOfVBelow));
        }

        // Important Note: In Unity triangles use clockwise orientation
        private static void AddTriangleToSliceWhenOneVertexIsOnPlane(TwoSliceBuildingsWrapper twoSliceWrappers,
                                                                     SlicerTriangleWrapper triangleWrapper)
        {
            if (triangleWrapper.V0.S == Plane.Side.on)
            {
                //  v1 x-----x v2
                //      \   /
                //       \ /
                // -------x------- Plane
                //        v0
                AddTriangleToSliceWhenNoCutsAreMade(twoSliceWrappers, triangleWrapper, triangleWrapper.V1.S);
            }
            else if (triangleWrapper.V1.S == Plane.Side.on)
            {
                //  v2 x-----x v0
                //      \   /
                //       \ /
                // -------x------- Plane
                //        v1
                AddTriangleToSliceWhenNoCutsAreMade(twoSliceWrappers, triangleWrapper, triangleWrapper.V0.S);
            }
            else if (triangleWrapper.V2.S == Plane.Side.on)
            {
                //  v0 x-----x v1
                //      \   /
                //       \ /
                // -------x------- Plane
                //        v2
                AddTriangleToSliceWhenNoCutsAreMade(twoSliceWrappers, triangleWrapper, triangleWrapper.V0.S);
            }
        }

        private static void AddTriangleToSliceWhenTwoVerticesAreOnPlane(TwoSliceBuildingsWrapper twoSliceWrappers,
                                                                        SlicerTriangleWrapper triangleWrapper)
        {
            if ((triangleWrapper.V0.S == Plane.Side.on) && (triangleWrapper.V1.S == Plane.Side.on))
            {
                //        x v2
                //       / \
                //      /   \
                // ----x-----x---- Plane
                //     v1    v0
                AddTriangleToSliceWhenNoCutsAreMade(twoSliceWrappers, triangleWrapper, triangleWrapper.V2.S);
            }
            else if ((triangleWrapper.V0.S == Plane.Side.on) && (triangleWrapper.V2.S == Plane.Side.on))
            {
                //        x v1
                //       / \
                //      /   \
                // ----x-----x---- Plane
                //     v0    v2
                AddTriangleToSliceWhenNoCutsAreMade(twoSliceWrappers, triangleWrapper, triangleWrapper.V1.S);
            }
            else if ((triangleWrapper.V1.S == Plane.Side.on) && (triangleWrapper.V2.S == Plane.Side.on))
            {
                //        x v0
                //       / \
                //      /   \
                // ----x-----x---- Plane
                //     v2    v1
                AddTriangleToSliceWhenNoCutsAreMade(twoSliceWrappers, triangleWrapper, triangleWrapper.V0.S);
            }
        }

        private static void SliceTriangleWithOneVertexOnPlane(TwoSliceBuildingsWrapper twoSliceWrappers,
                                                              SlicerTriangleWrapper triangleWrapper,
                                                              Plane cuttingPlane)
        {
            float t;
            if ((triangleWrapper.V0.S == Plane.Side.on)
                && cuttingPlane.Raycast(triangleWrapper.V1.V.Position, triangleWrapper.V2.V.Position, out t))
            {
                //        x v1
                //       /|
                //   v0 / | v12
                // ----x--+------- Plane
                //      \ |
                //       \|
                //        x v2
                #if false
                Vertex v12 = Vertex.Lerp(triangleWrapper.V1, triangleWrapper.V2, t);
                SliceBuildingWrapper wrapperSideOfV1 = twoSliceWrappers.GetWrapperSameSide(triangleWrapper.S1);
                SliceBuildingWrapper wrapperSideOfV2 = twoSliceWrappers.GetWrapperSameSide(triangleWrapper.S2);

                int i1SideOfV1 = wrapperSideOfV1.GetVertexIndex(triangleWrapper.I1, triangleWrapper.V1);
                int i12SideOfV1 = wrapperSideOfV1.GetInterpolatedVertexIndex(triangleWrapper.I1, triangleWrapper.I2, v12);
                int i0SideOfV1 = wrapperSideOfV1.GetVertexIndex(triangleWrapper.I0, triangleWrapper.V0);

                int i0SideOfV2 = wrapperSideOfV2.GetVertexIndex(triangleWrapper.I0, triangleWrapper.V0);
                int i12SideOfV2 = wrapperSideOfV2.GetInterpolatedVertexIndex(triangleWrapper.I1, triangleWrapper.I2, v12);
                int i2SideOfV2 = wrapperSideOfV2.GetVertexIndex(triangleWrapper.I2, triangleWrapper.V2);

                wrapperSideOfV1.Slice.Mesh.Triangles.Add(new Triangle(i1SideOfV1, i12SideOfV1, i0SideOfV1));
                wrapperSideOfV2.Slice.Mesh.Triangles.Add(new Triangle(i0SideOfV2, i12SideOfV2, i2SideOfV2));
                #endif
                var triangle = new SlicerTriangleOneVertexOnPlaneWrapper(triangleWrapper.V0, triangleWrapper.V1, triangleWrapper.V2);
                AddTrianglesToSliceWhenPlanCutsTriangleInOneVertex(twoSliceWrappers, triangle, t);
            }
            else if ((triangleWrapper.V1.S == Plane.Side.on)
                && cuttingPlane.Raycast(triangleWrapper.V0.V.Position, triangleWrapper.V2.V.Position, out t))
            {
                //        x v2
                //       /|
                //   v1 / | v02
                // ----x--+------- Plane
                //      \ |
                //       \|
                //        x v0
                #if false
                Vertex v02 = Vertex.Lerp(triangleWrapper.V0, triangleWrapper.V2, t);
                SliceBuildingWrapper wrapperSideOfV2 = twoSliceWrappers.GetWrapperSameSide(triangleWrapper.S2);
                SliceBuildingWrapper wrapperSideOfV0 = twoSliceWrappers.GetWrapperSameSide(triangleWrapper.S0);

                int i2SideOfV2 = wrapperSideOfV2.GetVertexIndex(triangleWrapper.I2, triangleWrapper.V2);
                int i02SideOfV2 = wrapperSideOfV2.GetInterpolatedVertexIndex(triangleWrapper.I0, triangleWrapper.I2, v02);
                int i1SideOfV2 = wrapperSideOfV2.GetVertexIndex(triangleWrapper.I1, triangleWrapper.V1);

                int i1SideOfV0 = wrapperSideOfV0.GetVertexIndex(triangleWrapper.I1, triangleWrapper.V1);
                int i02SideOfV0 = wrapperSideOfV0.GetInterpolatedVertexIndex(triangleWrapper.I0, triangleWrapper.I2, v02);
                int i0SideOfV0 = wrapperSideOfV0.GetVertexIndex(triangleWrapper.I0, triangleWrapper.V0);

                wrapperSideOfV2.Slice.Mesh.Triangles.Add(new Triangle(i2SideOfV2, i02SideOfV2, i1SideOfV2));
                wrapperSideOfV0.Slice.Mesh.Triangles.Add(new Triangle(i1SideOfV0, i02SideOfV0, i0SideOfV0));
                #endif
                var triangle = new SlicerTriangleOneVertexOnPlaneWrapper(triangleWrapper.V1, triangleWrapper.V2, triangleWrapper.V0);
                AddTrianglesToSliceWhenPlanCutsTriangleInOneVertex(twoSliceWrappers, triangle, t);
            }
            else if ((triangleWrapper.V2.S == Plane.Side.on)
                && cuttingPlane.Raycast(triangleWrapper.V0.V.Position, triangleWrapper.V1.V.Position, out t))
            {
                //        x v0
                //       /|
                //   v2 / | v01
                // ----x--+------- Plane
                //      \ |
                //       \|
                //        x v1
                #if false
                Vertex v01 = Vertex.Lerp(triangleWrapper.V0, triangleWrapper.V1, t);
                SliceBuildingWrapper wrapperSideOfV0 = twoSliceWrappers.GetWrapperSameSide(triangleWrapper.S0);
                SliceBuildingWrapper wrapperSideOfV1 = twoSliceWrappers.GetWrapperSameSide(triangleWrapper.S1);

                int i0SideOfV0 = wrapperSideOfV0.GetVertexIndex(triangleWrapper.I0, triangleWrapper.V0);
                int i01SideOfV0 = wrapperSideOfV0.GetInterpolatedVertexIndex(triangleWrapper.I0, triangleWrapper.I1, v01);
                int i2SideOfV0 = wrapperSideOfV0.GetVertexIndex(triangleWrapper.I2, triangleWrapper.V2);

                int i1SideOfV1 = wrapperSideOfV1.GetVertexIndex(triangleWrapper.I1, triangleWrapper.V1);
                int i01SideOfV1 = wrapperSideOfV1.GetInterpolatedVertexIndex(triangleWrapper.I0, triangleWrapper.I1, v01);
                int i2SideOfV1 = wrapperSideOfV1.GetVertexIndex(triangleWrapper.I2, triangleWrapper.V2);

                wrapperSideOfV0.Slice.Mesh.Triangles.Add(new Triangle(i0SideOfV0, i01SideOfV0, i2SideOfV0));
                //wrapperSideOfV1.Slice.Mesh.Triangles.Add(new Triangle(i1SideOfV1, i01SideOfV1, i2SideOfV1));
                wrapperSideOfV1.Slice.Mesh.Triangles.Add(new Triangle(i2SideOfV1, i01SideOfV1, i1SideOfV1));
                #endif
                var triangle = new SlicerTriangleOneVertexOnPlaneWrapper(triangleWrapper.V2, triangleWrapper.V0, triangleWrapper.V1);
                AddTrianglesToSliceWhenPlanCutsTriangleInOneVertex(twoSliceWrappers, triangle, t);
            }
        }

        private static void SliceTriangleWithNoVertexOnPlane(TwoSliceBuildingsWrapper twoSliceWrappers,
                                                             SlicerTriangleWrapper triangleWrapper,
                                                             Plane cuttingPlane)
        {
            float t1, t2;
            if ((triangleWrapper.V0.S != triangleWrapper.V1.S)
                && cuttingPlane.Raycast(triangleWrapper.V0.V.Position, triangleWrapper.V1.V.Position, out t1))
            {
                #if false
                Vertex v01 = Vertex.Lerp(triangleWrapper.V0, triangleWrapper.V1, t1);
                SliceBuildingWrapper wrapperSideOfV1 = twoSliceWrappers.GetWrapperSameSide(triangleWrapper.S1);

                int i01SideOfV1 = wrapperSideOfV1.GetInterpolatedVertexIndex(triangleWrapper.I0, triangleWrapper.I1, v01);
                int i1SideOfV1 = wrapperSideOfV1.GetVertexIndex(triangleWrapper.I1, triangleWrapper.V1);
                #endif

                if ((triangleWrapper.V0.S == triangleWrapper.V2.S)
                    && cuttingPlane.Raycast(triangleWrapper.V1.V.Position, triangleWrapper.V2.V.Position, out t2))
                {
                    //        x v1
                    //   v01 / \ v12
                    // -----*---*----- Plane
                    //     / _/  \
                    //    x-------x
                    //    v0      v2
                    #if true
                    Vertex v01 = Vertex.Lerp(triangleWrapper.V0.V, triangleWrapper.V1.V, t1);
                    Vertex v12 = Vertex.Lerp(triangleWrapper.V1.V, triangleWrapper.V2.V, t2);

                    SliceBuildingWrapper wrapperSideOfV1 = twoSliceWrappers.GetWrapperSameSide(triangleWrapper.V1.S);
                    SliceBuildingWrapper wrapperSideOfV2 = twoSliceWrappers.GetWrapperSameSide(triangleWrapper.V2.S);

                    int i1SideOfV1 = wrapperSideOfV1.GetVertexIndex(triangleWrapper.V1.I, triangleWrapper.V1.V);
                    int i12SideOfV1 = wrapperSideOfV1.GetInterpolatedVertexIndex(triangleWrapper.V1.I, triangleWrapper.V2.I, v12);
                    int i01SideOfV1 = wrapperSideOfV1.GetInterpolatedVertexIndex(triangleWrapper.V0.I, triangleWrapper.V1.I, v01);

                    int i2SideOfV2 = wrapperSideOfV2.GetVertexIndex(triangleWrapper.V2.I, triangleWrapper.V2.V);
                    int i0SideOfV2 = wrapperSideOfV2.GetVertexIndex(triangleWrapper.V0.I, triangleWrapper.V0.V);
                    int i12SideOfV2 = wrapperSideOfV2.GetInterpolatedVertexIndex(triangleWrapper.V1.I, triangleWrapper.V2.I, v12);
                    int i01SideOfV2 = wrapperSideOfV2.GetInterpolatedVertexIndex(triangleWrapper.V0.I, triangleWrapper.V1.I, v01);

                    wrapperSideOfV1.Slice.Mesh.Triangles.Add(new Triangle(i1SideOfV1, i12SideOfV1, i01SideOfV1));
                    wrapperSideOfV2.Slice.Mesh.Triangles.Add(new Triangle(i0SideOfV2, i01SideOfV2, i12SideOfV2));
                    wrapperSideOfV2.Slice.Mesh.Triangles.Add(new Triangle(i0SideOfV2, i12SideOfV2, i2SideOfV2));
                    #endif
                    //var triangle = new SlicerTriangleNoVertexOnPlaneWrapper(triangleWrapper.V1, triangleWrapper.V2, triangleWrapper.V0);
                    // No idea why there has to be 1.0f - t1 here.
                    //AddTrianglesToSliceWhenPlaneCutsTriangleInTwoPoints(twoSliceWrappers, triangle, 1.0f - t1, t2);
                }
                else if (cuttingPlane.Raycast(triangleWrapper.V0.V.Position, triangleWrapper.V2.V.Position, out t2))
                {
                    //        x v0
                    //   v02 / \ v01
                    // -----*---*----- Plane
                    //     / _/  \
                    //    x-------x
                    //    v2      v1
                    #if false
                    Vertex v01 = Vertex.Lerp(triangleWrapper.V0, triangleWrapper.V1, t1);
                    Vertex v12 = Vertex.Lerp(triangleWrapper.V1, triangleWrapper.V2, t2);

                    SliceBuildingWrapper wrapperSideOfV1 = twoSliceWrappers.GetWrapperSameSide(triangleWrapper.S1);
                    SliceBuildingWrapper wrapperSideOfV2 = twoSliceWrappers.GetWrapperSameSide(triangleWrapper.S2);

                    int i1SideOfV1 = wrapperSideOfV1.GetVertexIndex(triangleWrapper.I1, triangleWrapper.V1);
                    int i12SideOfV1 = wrapperSideOfV1.GetInterpolatedVertexIndex(triangleWrapper.I1, triangleWrapper.I2, v12);
                    int i01SideOfV1 = wrapperSideOfV1.GetInterpolatedVertexIndex(triangleWrapper.I0, triangleWrapper.I1, v01);

                    int i2SideOfV2 = wrapperSideOfV2.GetVertexIndex(triangleWrapper.I2, triangleWrapper.V2);
                    int i0SideOfV2 = wrapperSideOfV2.GetVertexIndex(triangleWrapper.I0, triangleWrapper.V0);
                    int i12SideOfV2 = wrapperSideOfV2.GetInterpolatedVertexIndex(triangleWrapper.I1, triangleWrapper.I2, v12);
                    int i01SideOfV2 = wrapperSideOfV2.GetInterpolatedVertexIndex(triangleWrapper.I0, triangleWrapper.I1, v01);

                    wrapperSideOfV1.Slice.Mesh.Triangles.Add(new Triangle(i1SideOfV1, i12SideOfV1, i01SideOfV1));
                    wrapperSideOfV2.Slice.Mesh.Triangles.Add(new Triangle(i0SideOfV2, i01SideOfV2, i12SideOfV2));
                    wrapperSideOfV2.Slice.Mesh.Triangles.Add(new Triangle(i0SideOfV2, i12SideOfV2, i2SideOfV2));
                    #endif
                    var triangle = new SlicerTriangleNoVertexOnPlaneWrapper(triangleWrapper.V0, triangleWrapper.V1, triangleWrapper.V2);
                    AddTrianglesToSliceWhenPlaneCutsTriangleInTwoPoints(twoSliceWrappers, triangle, t1, t2);
                }
            }
            else if (cuttingPlane.Raycast(triangleWrapper.V2.V.Position, triangleWrapper.V0.V.Position, out t1)
                && cuttingPlane.Raycast(triangleWrapper.V2.V.Position, triangleWrapper.V1.V.Position, out t2))
            {
                //        x v2
                //   v21 / \ v20
                // -----*---*----- Plane
                //     / _/  \
                //    x-------x
                //    v1      v0
                #if false
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

                wrapperSideOfV2.Slice.Mesh.Triangles.Add(new Triangle(i2SideOfV2, i20SideOfV2, i21SideOfV2));
                wrapperSideOfV0.Slice.Mesh.Triangles.Add(new Triangle(i1SideOfV0, i21SideOfV0, i20SideOfV0));
                wrapperSideOfV0.Slice.Mesh.Triangles.Add(new Triangle(i1SideOfV0, i20SideOfV0, i0SideOfV0));
                #endif
                var triangle = new SlicerTriangleNoVertexOnPlaneWrapper(triangleWrapper.V2, triangleWrapper.V0, triangleWrapper.V1);
                AddTrianglesToSliceWhenPlaneCutsTriangleInTwoPoints(twoSliceWrappers, triangle, t1, t2);
            }
        }
    }
}
