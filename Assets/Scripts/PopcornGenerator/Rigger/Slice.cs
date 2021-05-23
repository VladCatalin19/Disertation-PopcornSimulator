using System.Collections.Generic;
using UnityEngine;

namespace PopcornGenerator.Rigger
{
	internal struct Slice
	{
		private ICollection<int> indices;
		
		public Slice(ICollection<int> indices)
		{
			this.indices = indices;
		}

		public ICollection<int> Indices { get => indices; }
	}
}
