using System.Collections.Generic;
using UnityEngine;

namespace Popcorn.Rigger
{
	public struct RiggedPart
	{
		private ICollection<int> indices;

		public RiggedPart(ICollection<int> indices)
		{
			this.indices = indices;
		}

		public ICollection<int> Indices { get => indices; }
	}
}