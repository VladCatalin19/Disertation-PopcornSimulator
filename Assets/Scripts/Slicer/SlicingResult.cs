using System.Collections.Generic;
using UnityEngine;

namespace Popcorn.Slicer
{
	public class SlicingResult
	{
		private IList<Triangle> triangles;
		private bool hasNormals;
		private bool hasUVs;
		private bool hasTangents;

		public SlicingResult(bool hasNormals, bool hasUVs, bool hasTangents, int listInitiCapacity = 1024)
		{
			triangles = new List<Triangle>(listInitiCapacity);
			this.hasNormals = hasNormals;
			this.hasUVs = hasUVs;
			this.hasTangents = hasTangents;
		}

		public void AddTriangle(Triangle triangle)
		{
			triangles.Add(triangle);
		}

		public void AddTriangles(ICollection<Triangle> triangles)
		{
			foreach (Triangle triangle in triangles)
			{
				triangles.Add(triangle);
			}
		}

		public void Clear()
		{
			triangles.Clear();
		}
	}
}
