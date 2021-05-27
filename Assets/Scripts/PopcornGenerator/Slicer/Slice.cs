using System.Collections.Generic;
using UnityEngine;

namespace PopcornGenerator
{
	internal struct Slice
	{
		private readonly Mesh mesh;
		private readonly IList<int> borderIndicesList;
		private readonly ISet<int> borderIndicesSet;

		public Slice(Mesh mesh) : this(mesh, new List<int>(), new HashSet<int>()) { }

		public Slice(Mesh mesh, IList<int> borderIndicesList, ISet<int> borderIndicesSet)
		{
			this.mesh = mesh;
			this.borderIndicesList = borderIndicesList;
			this.borderIndicesSet = borderIndicesSet;
		}

		public Mesh Mesh { get => mesh; }
		public IList<int> BorderIndicesList { get => borderIndicesList; }
		public ISet<int> BorderIndicesSet { get => borderIndicesSet; }

		public void AddBorderIndex(int index)
		{
			borderIndicesList.Add(index);
			borderIndicesSet.Add(index);
		}
	}
}
