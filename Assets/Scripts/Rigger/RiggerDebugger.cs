#if DEBUG
using System.Collections.Generic;
using UnityEngine;

namespace Popcorn.Rigger
{
	public class RiggerDebugger : MonoBehaviour
	{
		private Vector3[] vertices = null;
		private int[] verticesZones = null;
		private static Color[] zoneColor = new Color[]
		{
			Color.red, Color.green, Color.blue, Color.magenta, Color.yellow, Color.cyan,
			new Color(1, .576f, 0), new Color(.536f, 0, 1), Color.white,
			Color.red, Color.green, Color.blue, Color.magenta, Color.yellow, Color.cyan,
			new Color(1, .576f, 0), new Color(.536f, 0, 1), Color.white, Color.black,
			Color.red, Color.green, Color.blue, Color.magenta, Color.yellow, Color.cyan,
			new Color(1, .576f, 0), new Color(.536f, 0, 1), Color.white,
		};
		private Graph<int> graph = null;

		public void Init(Vector3[] vertices, ICollection<RiggedPart> riggedParts, Graph<int> graph)
		{
			this.vertices = vertices;
			this.graph = graph;
			InitVerticesZones(riggedParts);
		}

		private void InitVerticesZones(ICollection<RiggedPart> riggedParts)
		{
			verticesZones = new int[vertices.Length];

			int zoneIndex = 0;
			foreach (RiggedPart riggedPart in riggedParts)
			{
				foreach (int vertexIndex in riggedPart.Indices)
				{
					verticesZones[vertexIndex] = zoneIndex;
				}
				++zoneIndex;
			}
		}

		private void OnDrawGizmos()
		{
			foreach (int vertex in graph.Vertices)
			{
				Color vertexColor = zoneColor[verticesZones[vertex]];
				foreach (int neighbor in graph.GetNeighbors(vertex))
				{
					Color neighborColor = zoneColor[verticesZones[neighbor]];

					Vector3 start = transform.TransformPoint(vertices[vertex]);
					Vector3 end = transform.TransformPoint(vertices[neighbor]);
					Debug.DrawLine(start, end, Color.Lerp(vertexColor, neighborColor, 0.5f));
				}
			}
		}
	}
}
#endif
