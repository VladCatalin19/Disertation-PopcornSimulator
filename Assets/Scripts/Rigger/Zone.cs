using System.Collections.Generic;
using UnityEngine;

namespace Popcorn.Rigger
{
	public struct Zone
	{
		private ICollection<int> indices;
		public Zone(ICollection<int> indices)
		{
			this.indices = indices;
		}

		public ICollection<int> Indices { get => indices; }
	}
}
