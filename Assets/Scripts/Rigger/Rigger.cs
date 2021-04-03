using System.Collections.Generic;
using BoneTool.Script.Runtime;
using UnityEngine;

using MeshGraph = Popcorn.Rigger.Graph<int>;

namespace Popcorn.Rigger
{
	public static class Rigger
	{
		public static void Rig(GameObject gameObject, Plane[] riggingPlanes, Transform riggingRootBonePosition)
		{
			if (!gameObject) throw new System.ArgumentNullException("gameObject");

			MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
			if (!meshFilter)
				throw new System.ArgumentException("Provided GameObject does not have a MeshFilter component");

			RigSlicedMesh(gameObject, meshFilter, riggingPlanes, riggingRootBonePosition);
		}

		private static void RigSlicedMesh(GameObject gameObject, MeshFilter meshFilter,
			Plane[] riggingPlanes, Transform riggingRootBonePosition
		)
		{
			Mesh mesh = meshFilter.mesh;
			Vector3[] vertices = mesh.vertices;
			int[] triangles = mesh.triangles;

			MeshGraph graph = InitGraph(vertices.Length, triangles);
			ICollection<Slice> slices = GetGraphConnectedComponents(graph);

			TransformVerticesToWorldSpace(vertices, gameObject.transform);
			ICollection<ICollection<Slice>> slicesSlices = SliceSlicesByRiggingPlanes(
				vertices, slices, riggingPlanes
			);
			TransformVerticesToLocalSpace(vertices, gameObject.transform);

			RiggingResult riggingResult = CreateKernelRig(gameObject, slicesSlices, vertices, triangles, riggingRootBonePosition);
			RigKernel(riggingResult, gameObject, meshFilter);

			#if DEBUG
			AddGraphVisualizerComponent(graph, gameObject, vertices, slicesSlices);
			AddBoneVisualizerComponent(gameObject, riggingResult);
			#endif
		}

		private static MeshGraph InitGraph(int meshVertexCount, int[] meshTriangles)
		{
			int[] triangles = meshTriangles;
			MeshGraph graph = new MeshGraph(meshVertexCount);

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

		private static void TransformVerticesToWorldSpace(Vector3[] vertices, Transform transform)
		{
			for (int i = 0; i < vertices.Length; ++i)
			{
				vertices[i] = transform.TransformPoint(vertices[i]);
			}
		}

		private static void TransformVerticesToLocalSpace(Vector3[] vertices, Transform transform)
		{
			for (int i = 0; i < vertices.Length; ++i)
			{
				vertices[i] = transform.InverseTransformPoint(vertices[i]);
			}
		}

		private static ICollection<Slice> GetGraphConnectedComponents(MeshGraph graph)
		{
			bool[] visited = new bool[graph.VertexCount];
			ICollection<Slice> slices = new List<Slice>();

			for (int i = 0; i < graph.VertexCount; ++i)
			{
				if (!visited[i])
				{
					Slice slice = BFS(graph, i, visited);
					slices.Add(slice);
				}
			}
			return slices;
		}

		private static Slice BFS(MeshGraph graph, int start, bool[] visited)
		{
			Slice slice = new Slice(new HashSet<int>());

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
				slice.Indices.Add(index);

				foreach (int neighbor in graph.GetNeighbors(index))
				{
					if (!visited[neighbor])
					{
						q.Enqueue(neighbor);
					}
				}
			}
			return slice;
		}

		private static ICollection<ICollection<Slice>> SliceSlicesByRiggingPlanes(
			Vector3[] vertices, ICollection<Slice> slices, Plane[] riggingPlanes
		)
		{
			var riggedSlices = new ICollection<Slice>[slices.Count];

			int slicedZoneIndex = 0;
			foreach (Slice zone in slices)
			{
				riggedSlices[slicedZoneIndex] = SplitZoneByRiggingPlane(vertices, zone, riggingPlanes);
				++slicedZoneIndex;
			}

			return riggedSlices;
		}

		private static ICollection<Slice> SplitZoneByRiggingPlane(
			Vector3[] vertices, Slice slice, Plane[] riggingPlanes
		)
		{
			var sliceSlices = new Slice[riggingPlanes.Length + 1];

			for (int i = 0; i < riggingPlanes.Length + 1; ++i)
			{
				sliceSlices[i] = new Slice(new List<int>(vertices.Length / (riggingPlanes.Length + 1)));
			}

			foreach (int index in slice.Indices)
			{
				Vector3 position = vertices[index];
				int betweenPlanesPosition = GetVertexPositionBetweenPlanes(position, riggingPlanes);
				sliceSlices[betweenPlanesPosition].Indices.Add(index);
			}

			return sliceSlices;
		}

		// TODO: Array of planes is sorted by distance, use binary search
		private static int GetVertexPositionBetweenPlanes(Vector3 position, Plane[] riggingPlanes)
		{
			bool IsAbovePlane(Vector3 pos, Plane plane) { return plane.GetSide(pos); }

			if (!IsAbovePlane(position, riggingPlanes[0]))
			{
				return 0;
			}

			for (int i = 1; i < riggingPlanes.Length; ++i)
			{
				if (IsAbovePlane(position, riggingPlanes[i - 1]) && !IsAbovePlane(position, riggingPlanes[i]))
				{
					return i;
				}
			}

			// Point is above all planes
			return riggingPlanes.Length;
		}

		private static RiggingResult CreateKernelRig(GameObject gameObject,
			ICollection<ICollection<Slice>> sliceSlices, Vector3[] vertices,
			int[] triangles, Transform riggingRootBonePosition
		)
		{
			int bonesCount = GetBoneCount(sliceSlices);
			RiggingResult riggingResult = new RiggingResult(new BoneWeight[vertices.Length],
				new Transform[bonesCount], new Matrix4x4[bonesCount]
			);
	
			riggingResult.Bones[0] = CreateRootBone(gameObject, riggingRootBonePosition);

			CreateBonesAndRigVertices(gameObject, sliceSlices, vertices, triangles, riggingResult);

			BindBonePoses(riggingResult, gameObject.transform);
			return riggingResult;
		}

		private static int GetBoneCount(ICollection<ICollection<Slice>> sliceSlices)
		{
			// The first bone is the root bone
			int bonesCount = 1;
			foreach (ICollection<Slice> slice in sliceSlices)
			{
				// Each slice will have 1 bone
				bonesCount += slice.Count;
			}
			return bonesCount;
		}

		private static Transform CreateBone(string name, Transform parent,
			Vector3 localPosition, Quaternion rotation
		)
		{
			Transform bone = new GameObject(name).transform;
			bone.parent = parent;
			bone.localPosition = localPosition;
			bone.localRotation = rotation;
			return bone;
		}

		private static Transform CreateRootBone(GameObject gameObject, Transform riggingRootBonePosition)
		{
			Transform transform = gameObject.transform;
			Vector3 localPosition =	transform.InverseTransformPoint(riggingRootBonePosition.position);
			Quaternion localRotation = Quaternion.LookRotation((transform.position - riggingRootBonePosition.position).normalized);
			return CreateBone("RootBone", transform, localPosition, localRotation);
		}

		private class SliceMeshData
		{
			private readonly IList<Vector3> vertices;
			private readonly IList<int> triangles;

			public SliceMeshData(IList<Vector3> vertices, ICollection<int> triangles)
			{
				this.vertices = vertices;
				this.triangles = new List<int>(triangles);
			}

			public IList<Vector3> Vertices { get => vertices; }
			public IList<int> Triangles { get => triangles; }
		}

		private static Vector3 CalculateSliceCenterOfMass(Slice slice, Vector3[] vertices)
		{
			Vector3 center = Vector3.zero;

			foreach (int index in slice.Indices)
			{
				center += vertices[index];
			}
			center /= slice.Indices.Count;

			return center;
		}

		private static Vector3 CalculateSliceMeanNormal(Slice slice, Vector3[] vertices, int[] triangles)
		{
			Vector3 mean = Vector3.zero;
			HashSet<int> sliceIndices = new HashSet<int>(slice.Indices);

			for (int i = 0; i < triangles.Length; i += 3)
			{
				int i0 = triangles[i];
				int i1 = triangles[i + 1];
				int i2 = triangles[i + 2];

				if (sliceIndices.Contains(i0) && sliceIndices.Contains(i1) && sliceIndices.Contains(i2))
				{
					mean += Vector3.Cross(vertices[i1] - vertices[i0], vertices[i2] - vertices[i0]).normalized;
				}
			}
			return mean.normalized;
		}

		private static void TranslateSliceVertices(Slice slice, Vector3[] vertices, Vector3 offset)
		{
			foreach (int i in slice.Indices)
			{
				vertices[i] += offset;
			}
		}

		private static SliceMeshData ProjectVerticesOnPlane(Slice slice, Vector3[] vertices, Vector3 planeNormal)
		{
			Vector3[] projectedVertices = new Vector3[slice.Indices.Count];

			int i = 0;
			foreach (int index in slice.Indices)
			{
				projectedVertices[i++] = Vector3.ProjectOnPlane(vertices[index], planeNormal);
			}

			return new SliceMeshData(projectedVertices, slice.Indices);
		}

		private static void RotateVertices(IList<Vector3> vertices, Vector3 fromNormal, Vector3 toNormal)
		{
			Quaternion q = Quaternion.FromToRotation(fromNormal, toNormal);
			for (int i = 0; i < vertices.Count; ++i)
			{
				vertices[i] = q * vertices[i];
			}
		}

		private static int GetPointIndexClosestToSliceCenter(SliceMeshData sliceMeshData)
		{
			Bounds bounds = new Bounds();

			foreach (Vector3 vertex in sliceMeshData.Vertices)
			{
				bounds.Encapsulate(vertex);
			}

			int index = 0;
			float minDistance = float.MaxValue;
			Vector3 refPoint = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

			for (int i = 0; i < sliceMeshData.Vertices.Count; ++i)
			{
				float distance = Vector3.Distance(refPoint, sliceMeshData.Vertices[i]);
				if (distance < minDistance)
				{
					minDistance = distance;
					index = i;
				}
			}
			return sliceMeshData.Triangles[index];
		}

		private static Vector3 CalculateSliceBonePosition(Slice slice, Vector3[] vertices, int[] triangles)
		{
			Vector3 center = CalculateSliceCenterOfMass(slice, vertices);
			Vector3 meanNormal = CalculateSliceMeanNormal(slice, vertices, triangles);
			//TranslateSliceVertices(slice, vertices, -center);
			SliceMeshData smd = ProjectVerticesOnPlane(slice, vertices, meanNormal);
			RotateVertices(smd.Vertices, meanNormal, Vector3.up);
			int index = GetPointIndexClosestToSliceCenter(smd);
			//TranslateSliceVertices(slice, vertices, center);
			return vertices[index];
		}

		private static Vector3 TransformPointFromOneTransformToAnother(Vector3 point, Transform from, Transform to)
		{
			return to.InverseTransformPoint(from.TransformPoint(point));
		}

		private static void CreateBonesAndRigVertices(GameObject gameObject,
			ICollection<ICollection<Slice>> sliceSlices, Vector3[] vertices,
			int[] triangles, RiggingResult riggingResult
		)
		{
			Transform rootBone = riggingResult.Bones[0];
			// Ignore root bone's index
			int boneIndex = 1;
			foreach (ICollection<Slice> slices in sliceSlices)
			{
				Transform prevBone = rootBone;
				int sliceIndex = 0;
				foreach (Slice slice in slices)
				{
					Vector3 objectSpacePosition = CalculateSliceBonePosition(slice, vertices, triangles);
					Vector3 localPosition = TransformPointFromOneTransformToAnother(objectSpacePosition, gameObject.transform, prevBone);
					Quaternion localRotation = Quaternion.identity;
					riggingResult.Bones[boneIndex] = CreateBone($"Rig Zone {sliceIndex}", prevBone, localPosition, localRotation);
					prevBone = riggingResult.Bones[boneIndex];

					foreach (int index in slice.Indices)
					{
						riggingResult.BoneWeights[index].boneIndex0 = boneIndex;
						riggingResult.BoneWeights[index].weight0 = 1.0f;
					}

					++boneIndex;
					++sliceIndex;
				}
			}
		}

		private static void BindBonePoses(RiggingResult riggingResult, Transform transform)
		{
			for (int i = 0; i < riggingResult.Bones.Length; ++i)
			{
				riggingResult.BindPoses[i] = riggingResult.Bones[i].worldToLocalMatrix
					* transform.localToWorldMatrix;
			}
		}

		private static void RigKernel(RiggingResult result, GameObject gameObject, MeshFilter meshFilter)
		{
			Mesh mesh = meshFilter.mesh;
			mesh.boneWeights = result.BoneWeights;
			mesh.bindposes = result.BindPoses;

			SkinnedMeshRenderer rend = gameObject.AddComponent<SkinnedMeshRenderer>();
			rend.bones = result.Bones;
			rend.sharedMesh = mesh;

			Object.Destroy(meshFilter);
			Object.Destroy(gameObject.GetComponent<MeshRenderer>());
		}

		#if DEBUG
		private static void AddGraphVisualizerComponent(MeshGraph graph,
			GameObject gameObject, Vector3[] vertices, ICollection<ICollection<Slice>> slicedAndRiggedZones
		)
		{
			RiggerDebugger rg = gameObject.AddComponent<RiggerDebugger>();
			List<Slice> allZones = new List<Slice>();

			foreach (ICollection<Slice> zones in slicedAndRiggedZones)
			{
				allZones.AddRange(zones);
			}

			rg.Init(vertices, allZones, graph);
		}

		private static void AddBoneVisualizerComponent(GameObject gameObject, RiggingResult riggingResult)
		{
			BoneVisualiser bv = gameObject.AddComponent<BoneVisualiser>();
			bv.RootNode = riggingResult.Bones[0];
			bv.BoneColor = Color.magenta;
			bv.PopulateChildren();
		}
		#endif
	}
}
