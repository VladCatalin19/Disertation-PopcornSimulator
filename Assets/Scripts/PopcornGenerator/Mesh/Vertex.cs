using UnityEngine;

namespace PopcornGenerator
{
    internal struct Vertex
    {
        // TODO maybe add tangent, color, color32, uv2 - 8?
        private Vector3 position;
        private Vector3 normal;
        private Vector2 uv;

        public Vertex(Vector3 position, Vector3 normal, Vector2 uv)
        {
            this.position = position;
            this.normal = normal;
            this.uv = uv;
        }
        
        public Vector3 Position { get => position; set => position = value; }
        public Vector3 Normal { get => normal; set => normal = value; }
        public Vector2 UV { get => uv; set => uv = value; }

        public static Vertex Lerp(Vertex v0, Vertex v1, float t)
        {
            Vector3 position = Vector3.Lerp(v0.Position, v1.Position, t);
            Vector3 normal = Vector3.Lerp(v0.Normal, v1.Normal, t);
            Vector2 uv = Vector2.Lerp(v0.UV, v1.UV, t);

            return new Vertex(position, normal, uv);
        }

        public static Vertex LerpUnclamped(Vertex v0, Vertex v1, float t)
        {
            Vector3 position = Vector3.LerpUnclamped(v0.Position, v1.Position, t);
            Vector3 normal = Vector3.LerpUnclamped(v0.Normal, v1.Normal, t);
            Vector2 uv = Vector2.LerpUnclamped(v0.UV, v1.UV, t);

            return new Vertex(position, normal, uv);
        }
    }
}
