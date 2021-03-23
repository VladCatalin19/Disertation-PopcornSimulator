using System.Collections.Generic;
using UnityEngine;

namespace Popcorn.Rigger
{
	public static class Rigger
	{
		public static void Rig(GameObject gameObject)
		{
			if (!gameObject) throw new System.ArgumentNullException("gameObject");

			MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
			if (!meshFilter)
				throw new System.ArgumentException("Provided GameObject does not have a MeshFilter component");

			Graph<int> graph = InitGraph(meshFilter.mesh);

			#if DEBUG
			vertices = meshFilter.mesh.vertices;
			transform = gameObject.transform;
			Rigger.graph = graph;

			verticesZones = new int[vertices.Length];
			#endif

			IList<IList<int>> zones = GetGraphConnectedComponents(graph);

			Debug.Log($"Zones: {zones.Count}");
			foreach (IList<int> zone in zones)
			{
				Debug.Log($"Zone count: {zone.Count}");
				
			}
		}

		private static Graph<int> InitGraph(Mesh mesh)
		{
			int[] triangles = mesh.triangles;
			Graph<int> graph = new Graph<int>(mesh.vertexCount);

			for (int i = 0; i < triangles.Length; i += 3)
			{
				int i0 = triangles[i];
				int i1 = triangles[i + 1];
				int i2 = triangles[i + 2];

				graph.TryAddVertex(i0);
				graph.TryAddVertex(i1);
				graph.TryAddVertex(i2);

				graph.TryAddEdge(i0, i1);
				graph.TryAddEdge(i1, i0);
				graph.TryAddEdge(i1, i2);
				graph.TryAddEdge(i2, i1);
				graph.TryAddEdge(i2, i0);
				graph.TryAddEdge(i0, i2);
			}

			return graph;
		}

		private static IList<IList<int>> GetGraphConnectedComponents(Graph<int> graph)
		{
			// No need to initialize the matrix with false values since C# does that
			// automatically
			bool[] visited = new bool[graph.VertexCount];
			IList<IList<int>> zones = new List<IList<int>>(4);

			for (int i = 0; i < graph.VertexCount; ++i)
			{
				if (!visited[i])
				{
					zones.Add(BFS(graph, i, visited));

					#if DEBUG
					foreach (int index in zones[zoneCount])
					{
						verticesZones[index] = zoneCount;
					}
					zoneCount++;
					#endif
				}
			}

			#if DEBUG
			zoneColor = new Color[]
			{
				Color.red, Color.green, Color.blue, Color.magenta, Color.yellow, Color.cyan,
				new Color(1, .576f, 0), new Color(.536f, 0, 1), Color.white, Color.white, Color.white, Color.white,
				Color.white, Color.white, Color.white, Color.white, Color.white, Color.white,
			};
			
			#endif

			return zones;
		}

		// MAYBE DO: Optimise flood fill:
		// https://www.codeproject.com/Articles/16405/Queue-Linear-Flood-Fill-A-Fast-Flood-Fill-Algorith
		private static IList<int> BFS(Graph<int> graph, int start, bool[] visited)
		{
			/*
			string path = @"/home/vlad/Unity/Projects/Popcorn Test/Rigger.log";
			System.IO.StreamWriter sw = new System.IO.StreamWriter(path);
			sw.Write($"Starting BFS from: {start}");
			*/
			IList<int> zoneIndices = new List<int>(graph.VertexCount / 4);
			Queue<int> q = new Queue<int>(graph.VertexCount / 4);
			q.Enqueue(start);

			while (q.Count > 0)
			{
				int index = q.Dequeue();
				if (visited[index])
				{
					continue;
				}

				visited[index] = true;
				zoneIndices.Add(index);
				

				/*
				sw.Write($"Visited: {index}\n ");
				sw.Flush();
				*/

				foreach (int neighbor in graph.GetNeighbors(index))
				{
					if (!visited[neighbor])
					{
						q.Enqueue(neighbor);
					}
				}
			}

			//sw.Close();

			return zoneIndices;
		}

		#if DEBUG
		private static Vector3[] vertices;
		private static int[] verticesZones;
		private static int zoneCount;
		private static Color[] zoneColor;
		private static Transform transform; 
		private static Graph<int> graph;
		public static void DrawGraph()
		{
			foreach(int vertex in graph.Vertices)
			{
				Color vertexColor = zoneColor[verticesZones[vertex]];
				foreach(int neighbor in graph.GetNeighbors(vertex))
				{
					Color neighborColor = zoneColor[verticesZones[neighbor]];

					Vector3 start = transform.TransformPoint(vertices[vertex]);
					Vector3 end = transform.TransformPoint(vertices[neighbor]);
					Debug.DrawLine(start, end, Color.Lerp(vertexColor, neighborColor, 0.5f));
				}
			}
		}
		#endif
	}
}
