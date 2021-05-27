using UnityEngine;

namespace PopcornGenerator
{
	internal static class MeshPostprocessor
	{
		public static void Process(Mesh mesh, Transform transform)
		{
			Utils.TransformVerticesToLocalSpace(mesh.Vertices, transform);
		}
	}
}
