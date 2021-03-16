using UnityEngine;

namespace Popcorn.Slicer
{
	public struct Vertex
	{
		private readonly Vector3 pos;
		private Vector3? norm;
		private Vector2? uv;
		private Vector4? tan;

		public Vertex(Vector3 pos, Vector3? norm, Vector2? uv, Vector4? tan)
		{
			this.pos = pos;
			this.norm = norm;
			this.uv = uv;
			this.tan = tan;
		}

		public Vector3 Pos { get => pos; }
		public Vector3? Norm { get => norm; }
		public Vector2? UV { get => uv; }
		public Vector4? Tan { get => tan; }

		public static Vertex Lerp(Vertex v0, Vertex v1, float t)
		{
			Vector3 vertex = Vector3.Lerp(v0.Pos, v1.Pos, t);
			Vector3? normal = (v0.Norm.HasValue && v1.Norm.HasValue)
				? (Vector3?)Vector3.Lerp(v0.Norm.Value, v1.Norm.Value, t) : null;
			Vector2? uv = (v0.uv.HasValue && v1.uv.HasValue)
				? (Vector2?)Vector2.Lerp(v0.UV.Value, v1.UV.Value, t) : null;
			Vector4? tangent = (v0.Tan.HasValue && v1.Tan.HasValue)
				? (Vector4?)Vector4.Lerp(v0.Tan.Value, v1.Tan.Value, t) : null;

			return new Vertex(vertex, normal, uv, tangent);
		}
	}
}
