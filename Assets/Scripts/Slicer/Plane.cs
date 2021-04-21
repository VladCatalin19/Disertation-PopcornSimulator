using UnityEngine;

namespace Popcorn.Slicer
{
	internal struct Plane
	{
		public enum Side {up, down, on, none};

		private Vector3 normal;
		private float distance;

		public Plane(UnityEngine.Plane plane)
		{
			normal = plane.normal;
			distance = -plane.distance;
		}

		public Plane(Vector3 normal, float distance)
		{
			this.normal = normal;
			this.distance = distance;
		}

		public Plane(Vector3 normal, Vector3 position)
		{
			this.normal = normal;
			this.distance = Vector3.Dot(normal, position);
		}

		public Side GetSide(Vector3 point)
		{
			float result = Vector3.Dot(normal, point) - distance;

			if (result > Constants.Epsilon)
			{
				return Side.up;
			}

			if (result < -Constants.Epsilon)
			{
				return Side.down;
			}

			return Side.on;
		}

		public bool Raycast(Vector3 p0, Vector3 p1, out float t)
		{
			Vector3 p0p1 = p1 - p0;
			t = (distance - Vector3.Dot(normal, p0)) / Vector3.Dot(normal, p0p1);

			if (Constants.Epsilon <= t && t <= 1.0f - Constants.Epsilon)
			{
				return true;
            }

			t = 0.0f;
			return false;
		}

		public override string ToString()
		{
			return $"normal: {normal}, distance: {distance}";
		}
	}
}
