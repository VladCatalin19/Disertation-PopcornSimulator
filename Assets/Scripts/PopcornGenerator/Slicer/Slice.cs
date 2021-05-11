using System.Collections.Generic;
using UnityEngine;

namespace PopcornGenerator.Slicer
{
	public class Slice
	{
		private IList<int> allIndices;
		private IList<int> borderIndices;

		public Slice(IList<int> allIndices, IList<int> borderIndices)
		{
			this.allIndices = allIndices;
			this.borderIndices = borderIndices;
		}

		public IList<int> AllIndices { get => allIndices; }
		public IList<int> BorderIndices { get => borderIndices; }
	}
}
