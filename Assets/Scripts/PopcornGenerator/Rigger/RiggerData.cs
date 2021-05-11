using System.Collections.Generic;
using UnityEngine;

namespace PopcornGenerator.Rigger
{
	internal class RiggerData
	{
		private GameObject gameObject;
		private Vector3[] vertices;
		private Vector2[] uv;
		private int[] triangles;
		private IList<Slicer.Slice> slices;
		private IDictionary<Slicer.Slice, IList<RiggedPart>> slicesRiggedParts;
		private IDictionary<Slicer.Slice, IList<int>> slicesFrontiers;

		public RiggerData(GameObject gameObject, Vector3[] vertices, Vector2[] uv, int[] triangles, IList<Slicer.Slice> slices)
		{
			this.gameObject = gameObject;
			this.vertices = vertices;
			this.uv = uv;
			this.triangles = triangles;
			this.slices = slices;
		}

		public GameObject GameObject { get => gameObject; }
		public Vector3[] Vertices { get => vertices; }
		public Vector2[] UV { get => uv; }
		public int[] Triangles { get => triangles; }
		public IList<Slicer.Slice> Slices { get => slices; set => slices = value; }
		public IDictionary<Slicer.Slice, IList<RiggedPart>> SlicesRiggedParts
		{
			get => slicesRiggedParts;
			set => slicesRiggedParts = value;
		}
		public IDictionary<Slicer.Slice, IList<int>> SlicesFrontiers
		{
			get => slicesFrontiers;
			set => slicesFrontiers = value;
		}
	}
}
