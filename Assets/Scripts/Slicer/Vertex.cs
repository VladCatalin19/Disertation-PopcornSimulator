using UnityEngine;

namespace Popcorn.Slicer
{
	public struct Vertex
	{
		private readonly Vector3 pos;
		private Vector3 norm;
		private Vector2 uv;
		private Vector4 tan;

		public Vertex(Vector3 pos, Vector3 norm, Vector2 uv, Vector4 tan)
		{
			this.pos = pos;
			this.norm = norm;
			this.uv = uv;
			this.tan = tan;
		}

		public Vector3 Pos { get => pos; }
		public Vector3 Norm { get => norm; }
		public Vector2 UV { get => uv; }
		public Vector4 Tan { get => tan; }
	}
}
