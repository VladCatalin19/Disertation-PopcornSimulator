using UnityEngine;

namespace PopcornGenerator.Puffer
{
	public static class Puffer
	{
		public static void Puff(GameObject gameObject)
		{
			if (!gameObject) throw new System.ArgumentNullException("gameObject");

			MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
			if (!meshFilter)
				throw new System.ArgumentException("Provided GameObject does not have a MeshFilter component");
		}

		private static void Puff(MeshFilter meshFilter)
		{
			Vector3[] vertices = meshFilter.mesh.vertices;
			int[] triangles = meshFilter.mesh.triangles;

			TransformVerticesToWorldSpace(vertices, meshFilter.transform);

			foreach (Transform slice in meshFilter.transform)
			{
				Transform middleBone = GetMiddleBone(slice);
				int closestIndex = GetClosestPointIndex(vertices, middleBone.transform.position);

				
			}
		}

		private static Transform GetMiddleBone(Transform slice)
		{
			Transform current = slice;
			Transform middle = slice;
			int depth = 0;

			while (slice.childCount > 0)
			{
				if (++depth % 2 == 0)
				{
					middle = middle.GetChild(0);
				}
				slice = slice.GetChild(0);
			}
			return middle;
		}

		private static void TransformVerticesToWorldSpace(Vector3[] vertices, Transform t)
		{
			for (int i = 0; i < vertices.Length; ++i)
			{
				vertices[i] = t.TransformPoint(vertices[i]);
			}
		}

		private static void TransformVerticesToLocalSpace(Vector3[] vertices, Transform t)
		{
			for (int i = 0; i < vertices.Length; ++i)
			{
				vertices[i] = t.InverseTransformPoint(vertices[i]);
			}
		}

		private static int GetClosestPointIndex(Vector3[] vertices, Vector3 position)
		{
			float minDistanceSqr = float.MaxValue;
			int minDistanceIndex = -1;

			for(int i = 0; i < vertices.Length; ++i)
			{
				float sqrMagnitude = (position - vertices[i]).sqrMagnitude;
				if (sqrMagnitude < minDistanceSqr)
				{
					minDistanceSqr = sqrMagnitude;
					minDistanceIndex = i;
				}
			}
			return minDistanceIndex;
		}
	}
}
