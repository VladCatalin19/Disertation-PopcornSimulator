using System.Collections.Generic;
using UnityEngine;

namespace PopcornGenerator.Rigger
{
	internal struct RiggedPartWithVertices
	{
		private readonly IList<Vector3> vertices;
		private readonly IList<int> indices;
		// TODO maybe add here a hashset with all vertices

		public RiggedPartWithVertices(IList<Vector3> vertices, ICollection<int> indices)
		{
			this.vertices = vertices;
			this.indices = new List<int>(indices);
		}

		public IList<Vector3> Vertices { get => vertices; }
		public IList<int> Indices { get => indices; }

		public Bounds GetBounds()
		{
			Bounds bounds = new Bounds();
			foreach (Vector3 vertex in vertices)
			{
				bounds.Encapsulate(vertex);
			}
			return bounds;
		}

		public Vector3 GetCenterOfMass()
		{
			Vector3 center = Vector3.zero;
			for (int i = 0; i < vertices.Count; ++i)
			{
				center += vertices[i];
			}
			return center / vertices.Count;
		}

		public int GetClosestIndex(Vector3 reference)
		{
			int index = 0;
			float minDistance = float.MaxValue;

			for (int i = 0; i < vertices.Count; ++i)
			{
				float distance = Vector3.Distance(reference, vertices[i]);
				if (distance < minDistance)
				{
					minDistance = distance;
					index = i;
				}
			}
			return index;
		}

		public void RotateVertices(Quaternion q)
		{
			for (int i = 0; i < vertices.Count; ++i)
			{
				vertices[i] = q * vertices[i];
			}
		}

		public void TranslateVertices(Vector3 offset)
		{
			for (int i = 0; i < vertices.Count; ++i)
			{
				vertices[i] += offset;
			}
		}

		public void TransformVerticesToWorldSpace(Transform transform)
		{
			for (int i = 0; i < vertices.Count; ++i)
			{
				vertices[i] = transform.TransformPoint(vertices[i]);
			}
		}

		public void TransformVerticesToLocalSpace(Transform transform)
		{
			for (int i = 0; i < vertices.Count; ++i)
			{
				vertices[i] = transform.InverseTransformPoint(vertices[i]);
			}
		}

		public void ProjectOnPlane(Vector3 planeNormal)
		{
			for (int i = 0; i < vertices.Count; ++i)
			{
				vertices[i] = Vector3.ProjectOnPlane(vertices[i], planeNormal);
			}
		}
	}
}
