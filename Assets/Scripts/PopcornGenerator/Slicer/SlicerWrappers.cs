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


	internal struct SlicerTriangleWrapper
	{
		private readonly int i0;
		private readonly int i1;
		private readonly int i2;
		private readonly Vertex v0;
		private readonly Vertex v1;
		private readonly Vertex v2;
		private readonly Plane.Side s0;
		private readonly Plane.Side s1;
		private readonly Plane.Side s2;

		public SlicerTriangleWrapper(
			Triangle triangle,
			Vertex v0, Vertex v1, Vertex v2,
			Plane.Side s0, Plane.Side s1, Plane.Side s2
		)
		{
			i0 = triangle.I0;
			i1 = triangle.I1;
			i2 = triangle.I2;

			this.v0 = v0;
			this.v1 = v1;
			this.v2 = v2;

			this.s0 = s0;
			this.s1 = s1;
			this.s2 = s2;
		}

		public int I0 { get => i0; }
		public int I1 { get => i1; }
		public int I2 { get => i2; }
		public Vertex V0 { get => v0; }
		public Vertex V1 { get => v1; }
		public Vertex V2 { get => v2; }
		public Plane.Side S0 { get => s0; }
		public Plane.Side S1 { get => s1; }
		public Plane.Side S2 { get => s2; }
	}
}
