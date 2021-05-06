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
		private IList<Slice> slices;
		private IDictionary<Slice, IList<RiggedPart>> slicesRiggedParts;
		private IDictionary<Slice, IList<int>> slicesFrontiers;

		public RiggerData(GameObject gameObject, Vector3[] vertices, Vector2[] uv, int[] triangles)
		{
			this.gameObject = gameObject;
			this.vertices = vertices;
			this.uv = uv;
			this.triangles = triangles;
		}

		public GameObject GameObject { get => gameObject; }
		public Vector3[] Vertices { get => vertices; }
		public Vector2[] UV { get => uv; }
		public int[] Triangles { get => triangles; }
		public IList<Slice> Slices { get => slices; set => slices = value; }
		public IDictionary<Slice, IList<RiggedPart>> SlicesRiggedParts
		{
			get => slicesRiggedParts;
			set => slicesRiggedParts = value;
		}
		public IDictionary<Slice, IList<int>> SlicesFrontiers
		{
			get => slicesFrontiers;
			set => slicesFrontiers = value;
		}
	}
}
