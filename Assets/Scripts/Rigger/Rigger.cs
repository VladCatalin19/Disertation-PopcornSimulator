using System.Collections.Generic;
using UnityEngine;

namespace Popcorn.Rigger
{
	public static class Rigger
	{

		public static void Rig(GameObject gameObject, UnityEngine.Plane[] cuttingPlanes)
		{
			if (!gameObject) throw new System.ArgumentNullException("gameObject");

			MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
			if (!meshFilter)
				throw new System.ArgumentException("Provided GameObject does not have a MeshFilter component");

			Mesh mesh = meshFilter.mesh;
			Graph<int> graph = InitGraph(mesh);
			ICollection<Zone> zones = GetGraphConnectedComponents(graph);

			#if DEBUG
			RiggerDebugger rg = gameObject.AddComponent<RiggerDebugger>();
			rg.Init(meshFilter.mesh.vertices, zones, graph);
			#endif


			//RigKernel(GetKernelRig(mesh, graph), gameObject, meshFilter);

			/*
			Debug.Log($"Zones: {zones.Count}");
			foreach (Zone zone in zones)
			{
				Debug.Log($"Zone count: {zone.indices.Count}");
			}
			*/
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

		private static ICollection<Zone> GetGraphConnectedComponents(Graph<int> graph)
		{
			// No need to initialize the matrix with false values since C# does that
			// automatically
			bool[] visited = new bool[graph.VertexCount];
			ICollection<Zone> zones = new List<Zone>();

			for (int i = 0; i < graph.VertexCount; ++i)
			{
				if (!visited[i])
				{
					Zone zone = BFS(graph, i, visited);
					zones.Add(zone);
				}
			}
			return zones;
		}

		// MAYBE DO: Optimise flood fill:
		// https://www.codeproject.com/Articles/16405/Queue-Linear-Flood-Fill-A-Fast-Flood-Fill-Algorith
		private static Zone BFS(Graph<int> graph, int start, bool[] visited)
		{
			Zone zone = new Zone(new HashSet<int>());

			Queue<int> q = new Queue<int>();
			q.Enqueue(start);

			while (q.Count > 0)
			{
				int index = q.Dequeue();
				if (visited[index])
				{
					continue;
				}

				visited[index] = true;
				zone.Indices.Add(index);

				foreach (int neighbor in graph.GetNeighbors(index))
				{
					if (!visited[neighbor])
					{
						q.Enqueue(neighbor);
					}
				}
			}
			return zone;
		}

		private struct RiggingResult
		{
			public BoneWeight[] boneWeights;
			public Transform[] bones;
			public Matrix4x4[] bindPoses;
		}

		private static RiggingResult GetKernelRig(Mesh mesh, Graph<int> graph)
		{
			RiggingResult result = new RiggingResult();
			/*
			BoneWeight[] boneWeights = new BoneWeight[mesh.vertexCount];
			Transform[] bones = new Transform[3];
			Matrix4x4[] bindPoses = new Matrix4x4[bones.Length];
			*/
			return result;
		}

		private static void RigKernel(RiggingResult result, GameObject gameObject, MeshFilter meshFilter)
		{
			Mesh mesh = meshFilter.mesh;
			mesh.boneWeights = result.boneWeights;
			mesh.bindposes = result.bindPoses;

			SkinnedMeshRenderer rend = gameObject.AddComponent<SkinnedMeshRenderer>();
			rend.bones = result.bones;
			rend.sharedMesh = mesh;

			Object.Destroy(meshFilter);
			Object.Destroy(gameObject.GetComponent<MeshRenderer>());
		}
	}
}
