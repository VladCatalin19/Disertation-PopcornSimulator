using UnityEngine;

namespace Popcorn.Slicer
{
	public struct Vertex
	{
		private readonly Vector3 position;
		private Vector3? normal;
		private Vector2? uv;
		private Vector4? tangent;
		private Plane.Side planeSide;
		private int triangleIndex;

		public Vertex(Vector3 position, Vector3? normal, Vector2? uv, Vector4? tangent,
			int triangleIndex = -1, Plane.Side planeSide = Plane.Side.none)
		{
			this.position = position;
			this.normal = normal;
			this.uv = uv;
			this.tangent = tangent;
			this.triangleIndex = triangleIndex;
			this.planeSide = planeSide;
		}

		public Vector3 Position { get => position; }
		public Vector3? Normal { get => normal; }
		public Vector2? UV { get => uv; }
		public Vector4? Tangent { get => tangent; }
		public int TriangleIndex { get => triangleIndex; }
		public Plane.Side PlaneSide { get => planeSide; set { planeSide = value; } }

		public static Vertex Lerp(Vertex v0, Vertex v1, float t)
		{
			Vector3 vertex = Vector3.Lerp(v0.Position, v1.Position, t);
			Vector3? normal = (v0.Normal.HasValue && v1.Normal.HasValue)
				? (Vector3?)Vector3.Lerp(v0.Normal.Value, v1.Normal.Value, t) : null;
			Vector2? uv = (v0.uv.HasValue && v1.uv.HasValue)
				? (Vector2?)Vector2.Lerp(v0.UV.Value, v1.UV.Value, t) : null;
			Vector4? tangent = (v0.Tangent.HasValue && v1.Tangent.HasValue)
				? (Vector4?)Vector4.Lerp(v0.Tangent.Value, v1.Tangent.Value, t) : null;

			return new Vertex(vertex, normal, uv, tangent);
		}
	}
}
