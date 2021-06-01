using System.Collections.Generic;
using UnityEngine;

#if DEBUG
using BoneTool.Script.Runtime;
#endif

namespace PopcornGenerator
{
	public class PopcornGenerator : MonoBehaviour
	{
		[SerializeField] private GameObject kernel = null;
		[SerializeField] private Transform[] cuttingPlanes = null;
		[SerializeField] private Transform[] riggingPlanes = null;
		[SerializeField] private Transform riggingRootBonePosition = null;

		private System.Diagnostics.Stopwatch stopWatch;
		private bool didPopcorn = false;

		#if DEBUG
		private static readonly Color[] colors = new Color[]
		{
			Color.white, Color.black, Color.red, Color.cyan,
			Color.magenta, Color.blue, Color.green, Color.grey
		};
		#endif

		private void Start()
		{
			CheckIfFieldsAreGood();
			stopWatch = new System.Diagnostics.Stopwatch();
		}

		private void Update()
		{
			if (Input.GetButtonDown("Fire1") && !didPopcorn)
			{
				stopWatch.Start();
				MakePopcorn();
				stopWatch.Stop();

				print($"Elapsed miliseconds: {stopWatch.ElapsedMilliseconds}");
			}
		}

		private void MakePopcorn()
		{
			Transform kernelTransform = kernel.transform;
			MeshFilter kernelMeshFilter = kernel.GetComponent<MeshFilter>();
			MeshRenderer kernelMeshRenderer = kernel.GetComponent<MeshRenderer>();
			Plane[] cuttingPlanesSlicer = TransformsToPlanes(cuttingPlanes);
			Plane[] riggingPlanesRigger = TransformsToPlanes(riggingPlanes);
			Mesh mesh = new Mesh(kernelMeshFilter.mesh);

			Utils.TransformVerticesToWorldSpace(mesh.Vertices, kernelTransform);
			var slices = Slicer.SliceMesh(mesh, cuttingPlanesSlicer);
			var riggedSlices = Rigger.RigSlices(slices, riggingPlanesRigger);
			var skinnedMeshRenderers = ConvertRiggedSlicesToMeshRenderers(	// TODO do this in a separate static class
				riggedSlices, kernelTransform, kernelMeshRenderer
			);

			AnimateRiggedSlices(skinnedMeshRenderers);

			Destroy(kernelMeshFilter);
			Destroy(kernelMeshRenderer);

			didPopcorn = true;
			HidePlanes();
		}

		private void CheckIfFieldsAreGood()
		{
			if (kernel == null)
			{
				throw new System.ArgumentNullException("kernel");
			}
			if (!kernel.GetComponent<MeshFilter>())
			{
				throw new System.ArgumentException("Provided GameObject does not have a MeshFilter component");
			}
			if (cuttingPlanes == null)
			{
				throw new System.ArgumentNullException("cuttingPlanes");
			}
			if (cuttingPlanes.Length == 0)
			{
				throw new System.ArgumentException("Provided CuttingPlanes array is empty");
			}
			for (int cuttingPlaneIndex = 0; cuttingPlaneIndex < cuttingPlanes.Length; ++cuttingPlaneIndex)
			{
				if (!cuttingPlanes[cuttingPlaneIndex])
				{
					throw new System.ArgumentNullException($"cuttingPlanes[{cuttingPlaneIndex}]");
				}
			}
			if (riggingPlanes == null)
			{
				throw new System.ArgumentNullException("riggingPlanes");
			}
			if (riggingPlanes.Length == 0)
			{
				throw new System.ArgumentException("Provided RiggingPlanes array is empty");
			}
			for (int riggingPlaneIndex = 0; riggingPlaneIndex < riggingPlanes.Length; ++riggingPlaneIndex)
			{
				if (!riggingPlanes[riggingPlaneIndex])
				{
					throw new System.ArgumentNullException($"riggingPlanes[{riggingPlaneIndex}]");
				}
			}
		}

		private Plane TransformToPlane(Transform t)
		{
			Vector3 planeNormal = t.up;
			Vector3 planeInPoint = t.position;
			return new Plane(planeNormal, planeInPoint);
		}

		private Plane[] TransformsToPlanes(Transform[] t)
		{
			Plane[] planes = new Plane[t.Length];

			for (int i = 0; i < planes.Length; ++i)
			{
				planes[i] = TransformToPlane(t[i]);
			}

			return planes;
		}

		private void HidePlanes()
		{
			foreach (Transform t in cuttingPlanes)
			{
				t.gameObject.SetActive(false);
			}
			foreach (Transform t in riggingPlanes)
			{
				t.gameObject.SetActive(false);
			}
		}



		private IList<SkinnedMeshRenderer> ConvertRiggedSlicesToMeshRenderers(
			IList<RiggedSlice> riggedSlices, Transform kernelTransform, MeshRenderer kernelMeshRenderer
		)
		{
			var skinnedMeshRenderers = new List<SkinnedMeshRenderer>(riggedSlices.Count);

			for (int riggedSliceIndex = 0; riggedSliceIndex < riggedSlices.Count; ++riggedSliceIndex)
			{
				RiggedSlice riggedSlice = riggedSlices[riggedSliceIndex];
				GameObject sliceGO = CreateSliceGameObject(kernelTransform, riggedSliceIndex);

				Utils.TransformVerticesToLocalSpace(riggedSlice.Slice.Mesh.Vertices, kernelTransform);
				// Only vertices are transformed to local space. BonePositions are still
				// in world space. This function uses world space coordinates.
				Transform[] bones = CreateRiggedSliceBonesTransform(riggedSlice, sliceGO.transform);

				UnityEngine.Mesh unityMesh = CreateUnityMesh(riggedSlice, riggedSliceIndex, kernelTransform, bones);
				AddMeshFilterToSliceGO(sliceGO, unityMesh);
				var skm = AddSkinnedMeshRendererToSliceGO(sliceGO, unityMesh, kernelMeshRenderer.material, bones);
				skinnedMeshRenderers.Add(skm);

				#if DEBUG
				AddBoneVisualizerToSliceGO(sliceGO, bones[0]);

				//ShowSliceBorderIndices(sliceGO, riggedSlice.Slice, colors[riggedSliceIndex % colors.Length]);
				ShowSliceBorderIntersectingPlanesIndices(sliceGO, riggedSlice.Slice, colors[riggedSliceIndex % colors.Length]);
				//ShowRiggedSliceBones(sliceGO, riggedSlice, colors[riggedSliceIndex % colors.Length]);
				//ShowRiggedSliceZoneVertices(sliceGO, riggedSlice);

				//ExportSliceAsObj(sliceGO, riggedSliceIndex, $@"{System.IO.Directory.GetCurrentDirectory()}\RuntimeExports\Slices");
				#endif
			}

			return skinnedMeshRenderers;
		}

		private static GameObject CreateSliceGameObject(Transform kernelTransform, int riggedSliceIndex)
		{
			GameObject sliceGO = new GameObject($"Slice {riggedSliceIndex}");
			sliceGO.transform.SetParent(kernelTransform, false);
			return sliceGO;
		}

		private static Transform[] CreateRiggedSliceBonesTransform(RiggedSlice riggedSlice, Transform rigParent)
		{
			Transform[] bones = new Transform[riggedSlice.BonePositions.Length];

			for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
			{
				Transform bone = new GameObject($"Bone {boneIndex}").transform;
				Transform prevBone = boneIndex == 0 ? rigParent : bones[boneIndex - 1];

				bone.SetParent(prevBone, false);
				bone.position = riggedSlice.BonePositions[boneIndex];
				bone.rotation = Quaternion.identity;//Quaternion.LookRotation((prevBone.position - bone.position).normalized);
				bones[boneIndex] = bone;
			}

			return bones;
		}

		private static UnityEngine.Mesh CreateUnityMesh(
			RiggedSlice riggedSlice, int riggedSliceIndex, Transform kernelTransform, Transform[] bones
		)
		{
			UnityEngine.Mesh unityMesh = riggedSlice.Slice.Mesh.ToUnityMesh();
			unityMesh.name = $"Slice {riggedSliceIndex} mesh";
			unityMesh.boneWeights = riggedSlice.BoneWeights;
			unityMesh.bindposes = CreateRiggedSliceBindPoses(bones, kernelTransform);
			return unityMesh;
		}

		private static Matrix4x4[] CreateRiggedSliceBindPoses(Transform[] bones, Transform kernelTransform)
		{
			Matrix4x4[] bindPoses = new Matrix4x4[bones.Length];

			for (int bindPoseIndex = 0; bindPoseIndex < bindPoses.Length; ++bindPoseIndex)
			{
				bindPoses[bindPoseIndex] = bones[bindPoseIndex].worldToLocalMatrix * kernelTransform.localToWorldMatrix;
			}

			return bindPoses;
		}

		private static MeshFilter AddMeshFilterToSliceGO(GameObject sliceGO, UnityEngine.Mesh unityMesh)
		{
			MeshFilter sliceMeshFilter = sliceGO.AddComponent<MeshFilter>();
			sliceMeshFilter.mesh = unityMesh;
			return sliceMeshFilter;
		}

		private static SkinnedMeshRenderer AddSkinnedMeshRendererToSliceGO(
			GameObject sliceGO, UnityEngine.Mesh unityMesh, Material material, Transform[] bones
		)
		{
			SkinnedMeshRenderer sliceSkinnedMeshRenderer = sliceGO.AddComponent<SkinnedMeshRenderer>();
			sliceSkinnedMeshRenderer.sharedMesh = unityMesh;
			sliceSkinnedMeshRenderer.material = material;
			sliceSkinnedMeshRenderer.rootBone = bones[0];
			sliceSkinnedMeshRenderer.bones = bones;
			return sliceSkinnedMeshRenderer;
		}

		private static void AnimateRiggedSlices(IList<SkinnedMeshRenderer> skinnedMeshRenderers)
		{
			for (int rendererIndex = 0; rendererIndex < skinnedMeshRenderers.Count; ++rendererIndex)
			{
				Animator.Animate(skinnedMeshRenderers[rendererIndex]);
			}
		}

		#if DEBUG
		private static void AddBoneVisualizerToSliceGO(GameObject sliceGO, Transform rootBone)
		{
			BoneVisualiser bv = sliceGO.AddComponent<BoneVisualiser>();
			bv.RootNode = rootBone;
			bv.BoneColor = Color.magenta;
			bv.PopulateChildren();
		}

		private static void ShowSliceBorderIndices(GameObject sliceGO, Slice slice, Color indicesColor)
		{
			GameObject borderParent = new GameObject("Border Indices");
			borderParent.transform.SetParent(sliceGO.transform, false);

			foreach (int borderIndex in slice.Border.IndicesList)
			{
				GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
				go.name = $"Index {borderIndex}";
				go.transform.SetParent(borderParent.transform, false);
				go.transform.localPosition = slice.Mesh.Vertices[borderIndex].Position;
				go.transform.localScale = 0.05f * Vector3.one;
				go.GetComponent<Renderer>().material.color = indicesColor;
			}
		}

		private static void ShowSliceBorderIntersectingPlanesIndices(GameObject sliceGO, Slice slice, Color indicesColor)
		{
			GameObject borderIntersectingParent = new GameObject("Border Intersecting Planes Indices");
			borderIntersectingParent.transform.SetParent(sliceGO.transform, false);

			foreach (int borderIntersectingIndex in slice.Border.IntersectingIndices)
			{
				GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
				go.name = $"Index {borderIntersectingIndex}";
				go.transform.SetParent(borderIntersectingParent.transform, false);
				go.transform.localPosition = slice.Mesh.Vertices[borderIntersectingIndex].Position;
				go.transform.localScale = 0.05f * Vector3.one;
				go.GetComponent<Renderer>().material.color = indicesColor;
			}
		}

		private static void ShowRiggedSliceBones(GameObject sliceGO, RiggedSlice riggedSlice, Color bonesColor)
		{
			GameObject bonesParent = new GameObject("Bones");
			bonesParent.transform.SetParent(sliceGO.transform, false);

			for (int bonePositionIndex = 0; bonePositionIndex < riggedSlice.BonePositions.Length; ++bonePositionIndex)
			{
				GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				go.name = $"Bone {bonePositionIndex}";
				go.transform.SetParent(bonesParent.transform, false);
				go.transform.position = riggedSlice.BonePositions[bonePositionIndex];
				go.transform.localScale = 0.3f * Vector3.one;
				go.GetComponent<Renderer>().material.color = bonesColor;
			}
		}

		private static void ShowRiggedSliceZoneVertices(GameObject sliceGO, RiggedSlice riggedSlice)
		{
			int randColorStartIndex = Random.Range(0, colors.Length);
			GameObject verticesParent = new GameObject("Vertices");
			verticesParent.transform.SetParent(sliceGO.transform, false);

			for (int zoneIndex = 0; zoneIndex < riggedSlice.RiggingZonesIndices.Count; ++zoneIndex)
			{
				Color color = colors[(randColorStartIndex + zoneIndex) % colors.Length];
				GameObject zoneParent = new GameObject($"Zone {zoneIndex}");
				zoneParent.transform.SetParent(verticesParent.transform, false);

				foreach (int index in riggedSlice.RiggingZonesIndices[zoneIndex])
				{
					GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
					go.name = $"Index {index}";
					go.transform.SetParent(zoneParent.transform, false);
					go.transform.localPosition = riggedSlice.Slice.Mesh.Vertices[index].Position;
					go.transform.localScale = 0.05f * Vector3.one;
					go.GetComponent<Renderer>().material.color = color;
				}
			}
		}

		private static void ExportSliceAsObj(GameObject sliceGO, int riggedSliceIndex, string directory)
		{
			ObjExporter.WriteMesh(sliceGO, $@"{directory}\slice{riggedSliceIndex}.obj");
		}
		#endif
	}
}