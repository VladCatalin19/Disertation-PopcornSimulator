using System.Collections.Generic;
using UnityEngine;

namespace PopcornGenerator.Rigger
{
	internal struct RiggedPart
	{
		private ICollection<int> allIndices;
		private ICollection<int> borderIndices;

		public RiggedPart(ICollection<int> allIndices, ICollection<int> borderIndices)
		{
			this.allIndices = allIndices;
			this.borderIndices = borderIndices;
		}

		public ICollection<int> AllIndices { get => allIndices; }
		public ICollection<int> BorderIndices { get => borderIndices; }
	}
}