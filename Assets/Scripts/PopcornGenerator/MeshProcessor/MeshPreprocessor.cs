using UnityEngine;

namespace PopcornGenerator
{
	internal static class MeshPreprocessor
	{
		public static void Process(Mesh mesh, Transform transform)
		{
			Utils.TransformVerticesToWorldSpace(mesh.Vertices, transform);
		}
	}
}
