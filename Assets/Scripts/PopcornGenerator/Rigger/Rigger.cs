using System.Collections.Generic;
using BoneTool.Script.Runtime;
using UnityEngine;

using MeshGraph = PopcornGenerator.Rigger.Graph<int>;
using SliceCollection = System.Collections.Generic.List<PopcornGenerator.Rigger.Slice>;
using SliceIndices = System.Collections.Generic.HashSet<int>;
using RiggedPartCollection = System.Collections.Generic.List<PopcornGenerator.Rigger.RiggedPart>;
using RiggedPartIdices = System.Collections.Generic.HashSet<int>;
using SlicesRiggedPartsDictionary = System.Collections.Generic.Dictionary
	<PopcornGenerator.Rigger.Slice, System.Collections.Generic.IList<PopcornGenerator.Rigger.RiggedPart>>;

using Frontier = System.Collections.Generic.List<int>;
using SlicesFrontiersDictionary = System.Collections.Generic.Dictionary
	<PopcornGenerator.Rigger.Slice, System.Collections.Generic.IList<int>>;


namespace PopcornGenerator.Rigger
{
	public static class Rigger
	{
		public static void Rig(GameObject gameObject, Plane[] riggingPlanes, Transform riggingRootBonePosition)
		{
			if (!gameObject) throw new System.ArgumentNullException("gameObject");
			if (riggingPlanes == null) throw new System.ArgumentNullException("riggingPlanes");
			if (!riggingRootBonePosition) throw new System.ArgumentNullException("riggingRootBonePosition");

			MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
			if (!meshFilter)
				throw new System.ArgumentException("Provided GameObject does not have a MeshFilter component");
			
			if (riggingPlanes.Length == 0)
				throw new System.ArgumentException("Provided array of planes is empty");

			RigSlicedMesh(gameObject, meshFilter, riggingPlanes, riggingRootBonePosition);
		}

		private static void RigSlicedMesh(GameObject gameObject, MeshFilter meshFilter,
			Plane[] riggingPlanes, Transform riggingRootBonePosition
		)
		{
			Mesh mesh = meshFilter.mesh;
			RiggerData riggerData = new RiggerData(gameObject, mesh.vertices, mesh.uv, mesh.triangles);

			TransformRiggerDataVerticesToWorldSpace(riggerData);

			MeshGraph graph = CreateMeshGraph(riggerData);
			SetRiggerDataSlices(riggerData, graph);
			SliceSlicesByRiggingPlanes(riggerData, riggingPlanes);

			RiggerResult riggingResult = CreateKernelRig(graph, riggerData, riggingRootBonePosition);
			RigKernel(riggingResult, gameObject, meshFilter);


			riggerData.SlicesFrontiers = new SlicesFrontiersDictionary();
			foreach (Slice slice in riggerData.Slices)
			{
				Frontier frontier = GetSliceFrontier(riggerData, slice, graph);
				riggerData.SlicesFrontiers[slice] = frontier;
				break;
			}

			#if DEBUG
			TransformRiggerDataVerticesToLocalSpace(riggerData);
			AddGraphVisualizerComponent(graph, riggerData);
			AddBoneVisualizerComponent(gameObject, riggingResult);
			#endif
		}

		private static void TransformRiggerDataVerticesToWorldSpace(RiggerData riggerData)
		{
			Transform t = riggerData.GameObject.transform;
			for (int i = 0; i < riggerData.Vertices.Length; ++i)
			{
				riggerData.Vertices[i] = t.TransformPoint(riggerData.Vertices[i]);
			}
		}

		private static void TransformRiggerDataVerticesToLocalSpace(RiggerData riggerData)
		{
			Transform t = riggerData.GameObject.transform;
			for (int i = 0; i < riggerData.Vertices.Length; ++i)
			{
				riggerData.Vertices[i] = t.InverseTransformPoint(riggerData.Vertices[i]);
			}
		}

		private static MeshGraph CreateMeshGraph(RiggerData riggerData)
		{
			MeshGraph graph = new MeshGraph(riggerData.Vertices.Length);

			for (int i = 0; i < riggerData.Triangles.Length; i += 3)
			{
				int i0 = riggerData.Triangles[i];
				int i1 = riggerData.Triangles[i + 1];
				int i2 = riggerData.Triangles[i + 2];

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

		private static void SetRiggerDataSlices(RiggerData riggerData, MeshGraph graph)
		{
			bool[] visited = new bool[graph.VertexCount];
			SliceCollection slices = new SliceCollection();

			for (int i = 0; i < graph.VertexCount; ++i)
			{
				if (!visited[i])
				{
					Slice slice = BreadthFirstSearch(graph, i, visited);
					slices.Add(slice);
				}
			}
			riggerData.Slices = slices;
		}

		private static Slice BreadthFirstSearch(MeshGraph graph, int start, bool[] visited)
		{
			Slice slice = new Slice(new SliceIndices());

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

		private static void SliceSlicesByRiggingPlanes(RiggerData riggerData, Plane[] riggingPlanes)
		{
			SlicesRiggedPartsDictionary riggedPartDictionary = new SlicesRiggedPartsDictionary();
			for (int i = 0; i < riggerData.Slices.Count; ++i)
			{
				Slice slice = riggerData.Slices[i];
				RiggedPartCollection riggedParts = SplitSliceByRiggingPlane(riggerData, slice, riggingPlanes);
				riggedPartDictionary[slice] = riggedParts;
			}
			riggerData.SlicesRiggedParts = riggedPartDictionary;
		}

		private static RiggedPartCollection SplitSliceByRiggingPlane(RiggerData riggerData, Slice slice, Plane[] riggingPlanes)
		{
			var riggedParts = new RiggedPartCollection(riggingPlanes.Length + 1);

			for (int i = 0; i < riggingPlanes.Length + 1; ++i)
			{
				riggedParts.Add(new RiggedPart(new RiggedPartIdices()));
			}

			foreach (int index in slice.Indices)
			{
				Vector3 position = riggerData.Vertices[index];
				int betweenPlanesPosition = GetVertexPositionBetweenPlanes(position, riggingPlanes);
				riggedParts[betweenPlanesPosition].Indices.Add(index);
			}
			return riggedParts;
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

		private static RiggerResult CreateKernelRig(
			MeshGraph meshGraph, RiggerData riggerData, Transform riggingRootBonePosition
		)
		{
			int bonesCount = GetBoneCount(riggerData);
			RiggerResult riggerResult = new RiggerResult(
				new BoneWeight[riggerData.Vertices.Length],
				new Transform[bonesCount],
				new Matrix4x4[bonesCount]
			);
	
			riggerResult.Bones[0] = CreateRootBone(riggerData.GameObject, riggingRootBonePosition);

			CreateBonesAndRigVertices(meshGraph, riggerData, riggerResult);

			BindBonePoses(riggerResult, riggerData.GameObject.transform);
			return riggerResult;
		}


		private static Transform CreateBone(
			string name, Transform parent, Vector3 localPosition, Quaternion rotation
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

		private static int GetBoneCount(RiggerData riggerData)
		{
			// The first bone is the root bone
			int bonesCount = 1;
			foreach (ICollection<RiggedPart> riggedParts in riggerData.SlicesRiggedParts.Values)
			{
				// Each part will have 1 bone
				bonesCount += riggedParts.Count;
			}
			return bonesCount;
		}

		private struct PositionAndRotation
		{
			public Vector3 position;
			public Vector3 normal;
			public PositionAndRotation(Vector3 position, Vector3 normal)
			{
				this.position = position;
				this.normal = normal;
			}
		}

		private static void CreateBonesAndRigVertices(MeshGraph meshGraph, RiggerData riggerData, RiggerResult riggingResult)
		{
			int[] boneIndices = new int[riggerData.Vertices.Length];
			Transform rootBone = riggingResult.Bones[0];
			// Ignore root bone's index
			int boneIndex = 1;
			foreach (var keyEntry in riggerData.SlicesRiggedParts)
			{
				Slice slice = keyEntry.Key;
				Transform prevBone = rootBone;
				int rigZoneIndex = 0;
				foreach (RiggedPart riggedPart in keyEntry.Value)
				{
					PositionAndRotation posnNorm = CalculateRiggedPartBonePosition(riggedPart, riggerData, rigZoneIndex == 0);
					Vector3 position = posnNorm.position;
					Vector3 normal = posnNorm.normal;
					Vector3 localPosition = prevBone.InverseTransformPoint(position);
					Quaternion localRotation = Quaternion.identity;//rigZoneIndex == 0 
						//? Quaternion.FromToRotation(Vector3.forward, -normal)
						//: Quaternion.identity;
					riggingResult.Bones[boneIndex] = CreateBone($"Rig Zone {rigZoneIndex}", prevBone, localPosition, localRotation);
					if (rigZoneIndex == 0)
					{
						//riggingResult.Bones[boneIndex].rotation = Quaternion.FromToRotation(Vector3.forward, (rootBone.position - position).normalized);
						riggingResult.Bones[boneIndex].rotation = Quaternion.LookRotation((rootBone.position - position).normalized);
						//riggingResult.Bones[boneIndex].localRotation = Quaternion.identity;
					}
					prevBone = riggingResult.Bones[boneIndex];
					//riggedPart.Bone = riggingResult.Bones[boneIndex];

					foreach (int index in riggedPart.Indices)
					{
						boneIndices[index] = boneIndex;
						riggingResult.BoneWeights[index].boneIndex0 = boneIndex;
						riggingResult.BoneWeights[index].weight0 = 1.0f; 
					}

					++boneIndex;
					++rigZoneIndex;
				}
			}

			/*
			var neighborBones = new HashSet<int>();
			for (int vertex = 0; vertex < riggerData.Vertices.Length; ++vertex)
			{
				neighborBones.Clear();
				foreach (int neighbor in meshGraph.GetNeighbors(vertex))
				{
					neighborBones.Add(boneIndices[neighbor]);
				}

				float weight = 1.0f / neighborBones.Count;
				int bone = 0;
				foreach (int neighborBone in neighborBones)
				{
					SetBoneWeight(ref riggingResult.BoneWeights[vertex], bone++, neighborBone, weight);
				}
				//Debug.Log($"Bones: {bone}");
			}
			*/
		}

		private static PositionAndRotation CalculateRiggedPartBonePosition(RiggedPart riggedPart, RiggerData riggerData, bool isFirstPart)
		{
			RiggedPartWithVertices riggedPartVertices = GetRiggedPartWithVertices(riggedPart, riggerData);
			Vector3 center = riggedPartVertices.GetCenterOfMass();
			Vector3 meanNormal = CalculateSliceMeanNormal(riggedPartVertices, riggerData);
			riggedPartVertices.TranslateVertices(-center);
			riggedPartVertices.ProjectOnPlane(meanNormal);

			Vector3 dir = Vector3.Angle(meanNormal, Vector3.forward) < Vector3.Angle(meanNormal, Vector3.back)
				? Vector3.forward : Vector3.back;
			riggedPartVertices.RotateVertices(Quaternion.FromToRotation(meanNormal, dir));

			Bounds bounds = riggedPartVertices.GetBounds();
			Vector3 reference = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
			reference.y = isFirstPart ? bounds.center.y : bounds.min.y ;

			int index = GetIndexClosestToReference(riggedPartVertices, riggerData, reference);
			return new PositionAndRotation(riggerData.Vertices[index], meanNormal);
		}

		private static RiggedPartWithVertices GetRiggedPartWithVertices(RiggedPart riggedPart, RiggerData riggerData)
		{
			Vector3[] vertices = new Vector3[riggedPart.Indices.Count];

			int i = 0;
			foreach (int index in riggedPart.Indices)
			{
				vertices[i++] = riggerData.Vertices[index];
			}

			return new RiggedPartWithVertices(vertices, riggedPart.Indices);
		}

		private static Vector3 CalculateSliceMeanNormal(RiggedPartWithVertices riggedPartVertices, RiggerData riggerData)
		{
			Vector3 meanNormal = Vector3.zero;
			HashSet<int> hashSet = new HashSet<int>(riggedPartVertices.Indices);

			for (int i = 0; i < riggerData.Triangles.Length; i += 3)
			{
				int i0 = riggerData.Triangles[i];
				int i1 = riggerData.Triangles[i + 1];
				int i2 = riggerData.Triangles[i + 2];

				if (hashSet.Contains(i0) && hashSet.Contains(i1) && hashSet.Contains(i2))
				{
					meanNormal += Vector3.Cross(riggerData.Vertices[i1] - riggerData.Vertices[i0], riggerData.Vertices[i2] - riggerData.Vertices[i0]).normalized;
				}
			}
			return meanNormal.normalized;
		}

		private static int GetIndexClosestToReference(RiggedPartWithVertices riggedPartVertices, RiggerData riggerData, Vector3 reference)
		{
			int index = 0;
			float minDistance = float.MaxValue;

			for (int i = 0; i < riggedPartVertices.Vertices.Count; ++i)
			{
				float distance = Vector3.Distance(reference, riggedPartVertices.Vertices[i]);
				if (distance < minDistance)
				{
					minDistance = distance;
					index = i;
				}
			}
			return riggedPartVertices.Indices[index];
		}

		private static void RigKernel(RiggerResult result, GameObject gameObject, MeshFilter meshFilter)
		{
			Mesh mesh = meshFilter.mesh;
			mesh.boneWeights = result.BoneWeights;
			mesh.bindposes = result.BindPoses;

			SkinnedMeshRenderer rend = gameObject.AddComponent<SkinnedMeshRenderer>();
			rend.rootBone = result.Bones[0];
			rend.bones = result.Bones;
			rend.sharedMesh = mesh;

			Object.Destroy(meshFilter);
			Object.Destroy(gameObject.GetComponent<MeshRenderer>());
		}

		private static void SetBoneWeight(ref BoneWeight boneWeight, int index, int boneIndex, float weight)
		{
			switch(index)
			{
				case 0:
					boneWeight.boneIndex0 = boneIndex;
					boneWeight.weight0 = weight;
					break;

				case 1:
					boneWeight.boneIndex1 = boneIndex;
					boneWeight.weight1 = weight;
					break;

				case 2:
					boneWeight.boneIndex2 = boneIndex;
					boneWeight.weight2 = weight;
					break;

				default:
					boneWeight.boneIndex3 = boneIndex;
					boneWeight.weight3 = weight;
					break;
			}
		}

		private static void BindBonePoses(RiggerResult riggingResult, Transform transform)
		{
			for (int i = 0; i < riggingResult.Bones.Length; ++i)
			{
				riggingResult.BindPoses[i] = riggingResult.Bones[i].worldToLocalMatrix
					* transform.localToWorldMatrix;
			}
		}


		/*
		private static void SetBoneWeights(MeshGraph meshGraph, ICollection<Slice> slices, RiggerResult riggingResult)
		{
			var neighborBones = new HashSet<Transform>();
			foreach (int vertex in meshGraph.Vertices)
			{
				neighborBones.Clear();
				foreach (int neighbor in meshGraph.GetNeighbors(vertex))
				{
					foreach (Slice slice in slices)
					{
						if (slice.Indices.Contains(neighbor))
						{
							foreach (RiggedPart riggedPart in slice.RiggedParts)
							{
								if (riggedPart.Indices.Contains(neighbor))
								{
									neighborBones.Add(riggedPart.Bone);
								}
							}
						}
					}
				}
				float weight = 1.0f / neighborBones.Count;
				BoneWeight boneWeight = new BoneWeight();



				riggingResult.BoneWeights[vertex] = boneWeight;
			}
		}
		*/

		private enum Orientation { Colinear, Clockwise, CounterClockwise }

		private const float Epsilon = 1.0e-4f;
		private static Orientation GetPointsOrientation(Vector2 p, Vector2 q, Vector2 r)
		{
			float val = (q.y - p.y) * (r.x - q.x) - (q.x - p.x) * (r.y - q.y);

			if (Mathf.Abs(val) < Epsilon)
			{
				return Orientation.Colinear;
			}
			return val > 0.0f ? Orientation.Clockwise : Orientation.CounterClockwise;
		}

		private static bool OnSegement(Vector2 p, Vector2 q, Vector2 r)
		{
			return (q.x <= Mathf.Max(p.x, r.x) && q.x >= Mathf.Min(p.x, r.x) &&
				q.y <= Mathf.Max(p.y, r.y) && q.y >= Mathf.Min(p.y, r.y));
		}

		// TODO make a more generic method which does not use uv coordinates
		// Matbe a mesh parametrization?
		// https://www.geeksforgeeks.org/convex-hull-set-1-jarviss-algorithm-or-wrapping/
		private static Frontier GetSliceFrontier(RiggerData riggerData, Slice slice, MeshGraph graph)
		{
			Frontier frontier = new Frontier();
			List<int> sliceIndices = new List<int>(slice.Indices);

			int maxSteps = riggerData.Vertices.Length;

			if (riggerData.Vertices.Length < 3)
			{
				return frontier;
			}

			int leftMostPointSliceIndex = 0;
			for (int i = 0; i < sliceIndices.Count; ++i)
			{
				Vector2 point = riggerData.UV[sliceIndices[i]];
				Vector2 leftMostPoint = riggerData.UV[leftMostPointSliceIndex];
				//if (riggerData.UV[sliceIndices[i]].x < riggerData.UV[sliceIndices[leftMostPointSliceIndex]].x)
				if (point.x < leftMostPoint.x || Mathf.Abs(point.x - leftMostPoint.x) < Epsilon && point.y < leftMostPoint.y)
				{
					//leftMostPointSliceIndex = i;
					leftMostPointSliceIndex = sliceIndices[i];
				}
			}

			int previousSliceIndex = leftMostPointSliceIndex, currentSliceIndex = -1;
			do
			{
				//frontier.Add(sliceIndices[firstSliceIndex]);
				frontier.Add(previousSliceIndex);

				ICollection<int> neighbors = graph.GetNeighbors(previousSliceIndex);
				IEnumerator<int> neighborsEnum = neighbors.GetEnumerator();
				neighborsEnum.MoveNext();
				currentSliceIndex = neighborsEnum.Current;//(firstSliceIndex + 1) % sliceIndices.Count;

				//Vector2 firstPoint = riggerData.UV[sliceIndices[firstSliceIndex]];
				//Vector2 secondPoint = riggerData.UV[sliceIndices[secondSliceIndex]];
				Vector2 previousPoint = riggerData.UV[previousSliceIndex];
				Vector2 currentPoint = riggerData.UV[currentSliceIndex];
				//for (int i = 0; i < sliceIndices.Count; ++i)
				foreach (int i in neighbors)
				{
					//if (GetPointsOrientation(firstPoint, riggerData.UV[sliceIndices[i]], secondPoint) == Orientation.CounterClockwise)
					if (GetPointsOrientation(previousPoint, riggerData.UV[i], currentPoint) == Orientation.CounterClockwise
						|| previousSliceIndex != i && GetPointsOrientation(previousPoint, riggerData.UV[i], currentPoint) == Orientation.Colinear
							&& OnSegement(previousPoint, currentPoint, riggerData.UV[i]))
					{
						currentSliceIndex = i;
					}
				}

				previousSliceIndex = currentSliceIndex;
			}
			while (previousSliceIndex != leftMostPointSliceIndex && --maxSteps > 0);

			GameObject root = new GameObject();
			root.transform.position = Vector3.zero;
			root.transform.localScale = Vector3.one;
			root.transform.SetParent(riggerData.GameObject.transform, true);
			Debug.Log($"Left most index: {leftMostPointSliceIndex}");
			System.Text.StringBuilder strb = new System.Text.StringBuilder();
			for (int i = 0; i < frontier.Count; ++i)
			{
				strb.Append(frontier[i]).Append(' ');
				GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				obj.transform.name = $"Frontier index {i}";
				obj.transform.parent = root.transform;
				obj.transform.position = riggerData.Vertices[frontier[i]];
				obj.transform.localScale = new Vector3(0.008f, 0.008f, 0.008f);
				//obj.GetComponent<Renderer>().material.color = Random.ColorHSV();
			}
			Debug.Log($"Indices: {strb.ToString()}");


			return frontier;
		}

		#if DEBUG
		private static void AddGraphVisualizerComponent(MeshGraph graph, RiggerData riggerData)
		{
			RiggerDebugger rg = riggerData.GameObject.AddComponent<RiggerDebugger>();
			/*
			foreach (Slice slice in slices)
			{
				foreach (RiggedPart riggedPart in slice.RiggedParts)
				{
					Vector3 center = CalculateSliceCenterOfMass(riggedPart, vertices);
					Vector3 meanNormal = CalculateSliceMeanNormal(riggedPart, vertices, triangles);
					
					TranslateSliceVertices(riggedPart, vertices, -center);

					foreach (int index in riggedPart.Indices) vertices[index] = Vector3.ProjectOnPlane(vertices[index], meanNormal);
					Vector3 dir = Vector3.Angle(meanNormal, Vector3.forward) < Vector3.Angle(meanNormal, Vector3.back) ? Vector3.forward : Vector3.back;
					Quaternion q = Quaternion.FromToRotation(meanNormal, dir);
					foreach (int i in riggedPart.Indices) vertices[i] = q * vertices[i];
					
					TranslateSliceVertices(riggedPart, vertices, center);
				}

			}
			TransformVerticesToLocalSpace(vertices, gameObject.transform);
			*/
			List<RiggedPart> riggedParts = new List<RiggedPart>();
			foreach (IList<RiggedPart> riggedParts1 in riggerData.SlicesRiggedParts.Values)
			{
				riggedParts.AddRange(riggedParts1);
			}
			rg.Init(riggerData.Vertices, riggedParts, graph);

		}

		private static void AddBoneVisualizerComponent(GameObject gameObject, RiggerResult riggingResult)
		{
			BoneVisualiser bv = gameObject.AddComponent<BoneVisualiser>();
			bv.RootNode = riggingResult.Bones[0];
			bv.BoneColor = Color.magenta;
			bv.PopulateChildren();
		}
		#endif
	}
}
