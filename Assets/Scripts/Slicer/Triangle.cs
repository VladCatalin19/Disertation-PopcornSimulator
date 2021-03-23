using UnityEngine;

namespace Popcorn.Slicer
{
	public struct Triangle
	{
		private readonly int i0, i1, i2;

		public Triangle(int i0, int i1, int i2)
		{
			this.i0 = i0;
			this.i1 = i1;
			this.i2 = i2;
		}

		public int I0 { get => i0; }
		public int I1 { get => i1; }
		public int I2 { get => i2; }

		public int this[int key]
		{
			get
			{
				switch(key)
				{
					case 0: return i0;
					case 1: return i1;
					case 2: return i2;
				}
				throw new System.IndexOutOfRangeException("Invalid Triangle index");
			}
		}
	}
}
