using System.Collections.Generic;

namespace Popcorn.Rigger
{
	public class Graph<TVertex>
	{
		IDictionary<TVertex, ICollection<TVertex>> graph;

		public Graph(int capacity = 8)
		{
			graph = new Dictionary<TVertex, ICollection<TVertex>>(capacity);
		}

		public void AddVertex(TVertex vertex)
		{
			if (vertex == null) throw new System.ArgumentNullException("vertex");
			if (graph.ContainsKey(vertex))
				throw new System.ArgumentException("The vertex already exists in the Graph<TVertex>");
			graph.Add(vertex, new HashSet<TVertex>());
		}

		public bool TryAddVertex(TVertex vertex)
		{
			if (vertex == null) throw new System.ArgumentNullException("vertex");
			if (!graph.ContainsKey(vertex))
			{
				graph.Add(vertex, new HashSet<TVertex>());
				return true;
			}
			return false;
		}

		public bool ContainsVertex(TVertex vertex)
		{
			if (vertex == null) throw new System.ArgumentNullException("vertex");
			return graph.ContainsKey(vertex);
		}

		public bool RemoveVertex(TVertex vertex)
		{
			if (vertex == null) throw new System.ArgumentNullException("vertex");
			if (graph.ContainsKey(vertex))
			{
				foreach (ICollection<TVertex> vertexEdges in graph.Values)
				{
					vertexEdges.Remove(vertex);
				}
			}
			return graph.Remove(vertex);
		}

		public void AddEdge(TVertex vertex0, TVertex vertex1)
		{
			if (vertex0 == null) throw new System.ArgumentNullException("vertex0");
			if (vertex1 == null) throw new System.ArgumentNullException("vertex1");
			if (!graph.ContainsKey(vertex0))
				throw new System.ArgumentException("The first vertex does not exists in the Graph<TVertex>");
			if (!graph.ContainsKey(vertex1))
				throw new System.ArgumentException("The second vertex does not exists in the Graph<TVertex>");
			if (graph[vertex0].Contains(vertex1))
				throw new System.ArgumentException("The edge already exists in the Graph<TVertex>");

			graph[vertex0].Add(vertex1);
		}

		public bool TryAddEdge(TVertex vertex0, TVertex vertex1)
		{
			if (vertex0 == null) throw new System.ArgumentNullException("vertex0");
			if (vertex1 == null) throw new System.ArgumentNullException("vertex1");
			if (!graph.ContainsKey(vertex0))
				throw new System.ArgumentException("The first vertex does not exists in the Graph<TVertex>");
			if (!graph.ContainsKey(vertex1))
				throw new System.ArgumentException("The second vertex does not exists in the Graph<TVertex>");
			
			if (!graph[vertex0].Contains(vertex1))
			{
				graph[vertex0].Add(vertex1);
				return true;
			}
			return false;
		}

		public bool ContainsEdge(TVertex vertex0, TVertex vertex1)
		{
			if (vertex0 == null) throw new System.ArgumentNullException("vertex0");
			if (vertex1 == null) throw new System.ArgumentNullException("vertex1");
			if (!graph.ContainsKey(vertex0))
				throw new System.ArgumentException("The first vertex does not exists in the Graph<TVertex>");
			if (!graph.ContainsKey(vertex1))
				throw new System.ArgumentException("The second vertex does not exists in the Graph<TVertex>");
			
			return graph[vertex0].Contains(vertex1);
		}

		public ICollection<TVertex> GetNeighbors(TVertex vertex)
		{
			if (vertex == null) throw new System.ArgumentNullException("vertex");
			if (!graph.ContainsKey(vertex))
				throw new System.ArgumentException("The vertex already exists in the Graph<TVertex>");
			
			return graph[vertex];
		}

		public bool TryGetEdges(TVertex vertex, out ICollection<TVertex> edges)
		{
			if (vertex == null) throw new System.ArgumentNullException("vertex");
			if (graph.TryGetValue(vertex, out edges))
			{
				return true;
			}
			edges = null;
			return false;
		}

		public bool RemoveEdge(TVertex vertex0, TVertex vertex1)
		{
			if (vertex0 == null) throw new System.ArgumentNullException("vertex0");
			if (vertex1 == null) throw new System.ArgumentNullException("vertex1");
			if (!graph.ContainsKey(vertex0))
				throw new System.ArgumentException("The first vertex does not exists in the Graph<TVertex>");
			if (!graph.ContainsKey(vertex1))
				throw new System.ArgumentException("The second vertex does not exists in the Graph<TVertex>");

			return graph[vertex0].Remove(vertex1);
		}

		public void Clear()
		{
			graph.Clear();
		}

		public int VertexCount { get => graph.Count; }

		public ICollection<TVertex> Vertices { get => graph.Keys; }
	}
}
