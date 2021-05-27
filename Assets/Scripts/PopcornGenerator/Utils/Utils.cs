using System.Collections.Generic;
using UnityEngine;

namespace PopcornGenerator
{
	internal static class Utils
	{
		public static void TransformVerticesToWorldSpace(IList<Vertex> vertices, Transform transform)
		{
			for (int vertexIndex = 0; vertexIndex < vertices.Count; ++vertexIndex)
			{
				Vertex vertex = vertices[vertexIndex];
				vertex.Position = transform.TransformPoint(vertices[vertexIndex].Position);
				vertices[vertexIndex] = vertex;
			}
		}
	
		public static void TransformVerticesToLocalSpace(IList<Vertex> vertices, Transform transform)
		{
			for (int vertexIndex = 0; vertexIndex < vertices.Count; ++vertexIndex)
			{
				Vertex vertex = vertices[vertexIndex];
				vertex.Position = transform.InverseTransformPoint(vertices[vertexIndex].Position);
				vertices[vertexIndex] = vertex;
			}
		}
	}
}
