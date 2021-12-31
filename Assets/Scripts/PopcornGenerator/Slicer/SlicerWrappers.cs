using System.Collections.Generic;
using UnityEngine;

namespace PopcornGenerator
{
    internal struct SliceBuildingWrapper
    {
        private readonly Slice slice;
        private readonly Dictionary<int, int> originalToCurrentIndices;
        private readonly Dictionary<(int, int), int> originalToInterpolatedIndices;

        public SliceBuildingWrapper(Slice slice)
        {
            this.slice = slice;
            originalToCurrentIndices = new Dictionary<int, int>();
            originalToInterpolatedIndices = new Dictionary<(int, int), int>();
        }

        public Slice Slice { get => slice; }

        public int GetVertexIndex(int index, Vertex v)
        {
            if (!originalToCurrentIndices.TryGetValue(index, out int newIndex))
            {
                newIndex = slice.Mesh.Vertices.Count;
                slice.Mesh.Vertices.Add(v);
                originalToCurrentIndices.Add(index, newIndex);
            }
            return newIndex;
        }

        public int GetInterpolatedVertexIndex(int firstIndex, int secondIndex, Vertex v)
        {
            int i0 = Mathf.Min(firstIndex, secondIndex);
            int i1 = Mathf.Max(firstIndex, secondIndex);

            if (!originalToInterpolatedIndices.TryGetValue((i0, i1), out int newIndex))
            {
                newIndex = slice.Mesh.Vertices.Count;
                slice.Mesh.Vertices.Add(v);
                originalToInterpolatedIndices.Add((i0, i1), newIndex);
            }

            return newIndex;
        }
    }

    internal struct TwoSliceBuildingsWrapper
    {
        private readonly SliceBuildingWrapper upperSliceWrapper;
        private readonly SliceBuildingWrapper lowerSliceWrapper;

        public TwoSliceBuildingsWrapper(
            SliceBuildingWrapper upperSliceWrapper, SliceBuildingWrapper lowerSliceWrapper
        )
        {
            this.upperSliceWrapper = upperSliceWrapper;
            this.lowerSliceWrapper = lowerSliceWrapper;
        }

        public SliceBuildingWrapper UpperSliceWrapper { get => upperSliceWrapper; }
        public SliceBuildingWrapper LowerSliceWrapper { get => lowerSliceWrapper; }

        public SliceBuildingWrapper GetWrapperSameSide(Plane.Side planeSide)
        {
            return planeSide switch
            {
                Plane.Side.up => upperSliceWrapper,
                Plane.Side.down => lowerSliceWrapper,
                _ => throw new System.ArgumentException("Invalid Plane Side")
            };
        }
    }


    internal struct SlicerVertexWrapper
    {
        private readonly int i;
        private readonly Vertex v;
        private readonly Plane.Side s;

        public SlicerVertexWrapper(int i, Vertex v, Plane.Side s)
        {
            this.i = i;
            this.v = v;
            this.s = s;
        }

        public int I { get => i; }
        public Vertex V { get => v; }
        public Plane.Side S { get => s; }
    }


    internal struct SlicerTriangleWrapper
    {
        private readonly SlicerVertexWrapper v0;
        private readonly SlicerVertexWrapper v1;
        private readonly SlicerVertexWrapper v2;

        public SlicerTriangleWrapper(Triangle triangle,
                                     Vertex v0, Vertex v1, Vertex v2,
                                     Plane.Side s0, Plane.Side s1, Plane.Side s2
        )
        {
            this.v0 = new SlicerVertexWrapper(triangle.I0, v0, s0);
            this.v1 = new SlicerVertexWrapper(triangle.I1, v1, s1);
            this.v2 = new SlicerVertexWrapper(triangle.I2, v2, s2);
        }

        public SlicerVertexWrapper V0 { get => v0; }
        public SlicerVertexWrapper V1 { get => v1; }
        public SlicerVertexWrapper V2 { get => v2; }
    }


    internal struct SlicerTriangleOneVertexOnPlaneWrapper
    {
        private readonly SlicerVertexWrapper vOnPlane;
        private readonly SlicerVertexWrapper vAbove;
        private readonly SlicerVertexWrapper vBelow;

        public SlicerTriangleOneVertexOnPlaneWrapper(SlicerVertexWrapper vOnPlane,
                                                     SlicerVertexWrapper vAbove,
                                                     SlicerVertexWrapper vBelow)
        {
            this.vOnPlane = vOnPlane;
            this.vAbove = vAbove;
            this.vBelow = vBelow;
        }

        public SlicerVertexWrapper VOnPlane { get => vOnPlane; }
        public SlicerVertexWrapper VAbove { get => vAbove; }
        public SlicerVertexWrapper VBelow { get => vBelow; }
    }


    internal struct SlicerTriangleNoVertexOnPlaneWrapper
    {
        private readonly SlicerVertexWrapper vAbove;
        private readonly SlicerVertexWrapper vBelowRight;
        private readonly SlicerVertexWrapper vBelowLeft;

        public SlicerTriangleNoVertexOnPlaneWrapper(SlicerVertexWrapper vAbove,
                                                    SlicerVertexWrapper vBelowRight,
                                                    SlicerVertexWrapper vBelowLeft)
        {
            this.vAbove = vAbove;
            this.vBelowRight = vBelowRight;
            this.vBelowLeft = vBelowLeft;
        }

        public SlicerVertexWrapper VAbove { get => vAbove; }
        public SlicerVertexWrapper VBelowRight { get => vBelowRight; }
        public SlicerVertexWrapper VBelowLeft { get => vBelowLeft; }
    }
}
