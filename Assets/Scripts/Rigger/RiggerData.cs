using System.Collections.Generic;
using UnityEngine;

namespace Popcorn.Rigger
{
	public class RiggerData
	{
		private GameObject gameObject;
		private Vector3[] vertices;
		private int[] triangles;
		private IList<Slice> slices;
		private IDictionary<Slice, IList<RiggedPart>> slicesRiggedParts;

		public RiggerData(GameObject gameObject, Vector3[] vertices, int[] triangles)
		{
			this.gameObject = gameObject;
			this.vertices = vertices;
			this.triangles = triangles;
		}

		public GameObject GameObject { get => gameObject; }
		public Vector3[] Vertices { get => vertices; }
		public int[] Triangles { get => triangles; }
		public IList<Slice> Slices { get => slices; set => slices = value; }
		public IDictionary<Slice, IList<RiggedPart>> SlicesRiggedParts
		{
			get => slicesRiggedParts;
			set => slicesRiggedParts = value;
		}
	}
}
