using System.Collections.Generic;
using UnityEngine;

namespace PopcornGenerator.Slicer
{
	internal struct IndicesPair
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

	internal struct SideIndices
	{
		private IDictionary<IndicesPair, int> sameSide;
		private IDictionary<IndicesPair, int> otherSide;

		public SideIndices(IDictionary<IndicesPair, int> sameSide,
			IDictionary<IndicesPair, int> otherSide
		)
		{
			this.sameSide = sameSide;
			this.otherSide = otherSide;
		}

		public IDictionary<IndicesPair, int> SameSide { get => sameSide; }
		public IDictionary<IndicesPair, int> OtherSide { get => otherSide; }
	}

	internal class DuplicateIndices
	{
		private IDictionary<IndicesPair, int> upperHullIndices;
		private IDictionary<IndicesPair, int> lowerHullIndices;

		public DuplicateIndices(
			IDictionary<IndicesPair, int> upperHullIndices,
			IDictionary<IndicesPair, int> lowerHullIndices
		)
		{
			this.upperHullIndices = upperHullIndices;
			this.lowerHullIndices = lowerHullIndices;
		}

		public SideIndices GetHullIndices(Plane.Side planeSide)
		{
			if (planeSide == Plane.Side.up)
			{
				return new SideIndices(upperHullIndices, lowerHullIndices);
			}
			return new SideIndices(lowerHullIndices, upperHullIndices);
		}

		public void Clear()
		{
			upperHullIndices.Clear();
			lowerHullIndices.Clear();
		}

		public ICollection<int> UpperHullIndices { get => upperHullIndices.Values; }
		public ICollection<int> LowerHullIndices { get => lowerHullIndices.Values; }
	}
}
