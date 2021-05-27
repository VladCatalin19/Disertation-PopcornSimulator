using System.Collections.Generic;
using UnityEngine;

namespace PopcornGenerator
{
	public class PopcornGenerator : MonoBehaviour
	{
		[SerializeField] private GameObject kernel = null;
		[SerializeField] private Transform[] cuttingPlanes = null;
		[SerializeField] private Transform[] riggingPlanes = null;
		[SerializeField] private Transform riggingRootBonePosition = null;

		private System.Diagnostics.Stopwatch sw;
		private bool didPopcorn = false;

		private void Start()
		{
			CheckIfFieldsAreGood();
			sw = new System.Diagnostics.Stopwatch();
		}

		private void Update()
		{
			if (Input.GetButtonDown("Fire1") && !didPopcorn)
			{
				sw.Start();
				MakePopcorn();
				sw.Stop();

				print($"Elapsed miliseconds: {sw.ElapsedMilliseconds}");
			}
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

		private void MakePopcorn()
		{
			Transform kernelTransform = kernel.transform;
			MeshFilter kernelMeshFilter = kernel.GetComponent<MeshFilter>();
			MeshRenderer kernelMeshRenderer = kernel.GetComponent<MeshRenderer>();
			Plane[] cuttingPlanesSlicer = TransformsToPlanes(cuttingPlanes);
			Plane[] riggingPlanesRigger = TransformsToPlanes(riggingPlanes);
			Mesh mesh = new Mesh(kernelMeshFilter.mesh);

			MeshPreprocessor.Process(mesh, kernelTransform);
			var slices = Slicer.SliceMesh(mesh, cuttingPlanesSlicer);


			Destroy(kernelMeshFilter);
			Destroy(kernelMeshRenderer);
			Color[] borderColors = new Color[] { Color.white, Color.black, Color.red, Color.cyan, Color.magenta };
			for (int sliceIndex = 0; sliceIndex < slices.Count; ++sliceIndex)
			{
				MeshPostprocessor.Process(slices[sliceIndex].Mesh, kernelTransform);

				GameObject sliceGO = new GameObject($"Slice {sliceIndex}");
				MeshFilter sliceMeshFilter = sliceGO.AddComponent<MeshFilter>();
				SkinnedMeshRenderer sliceSkinnedMeshRenderer = sliceGO.AddComponent<SkinnedMeshRenderer>();
				UnityEngine.Mesh unityMesh = slices[sliceIndex].Mesh.ToUnityMesh();
				GameObject bone = new GameObject($"Root Bone");

				//Debug.Log($"Mesh vertex count: {unityMesh.vertexCount}");

				sliceGO.transform.SetParent(kernelTransform, false);
				bone.transform.SetParent(sliceGO.transform, false);

				unityMesh.name = $"Slice {sliceIndex} mesh";
				sliceSkinnedMeshRenderer.sharedMesh = sliceMeshFilter.mesh = unityMesh;
				sliceSkinnedMeshRenderer.material = kernelMeshRenderer.material;
				sliceSkinnedMeshRenderer.rootBone = bone.transform;

				/*
				GameObject borderParent = new GameObject("Border Indices");
				borderParent.transform.SetParent(sliceGO.transform, false);
				foreach (int borderIndex in slices[sliceIndex].BorderIndicesList)
				{
					GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
					go.name = $"Index {borderIndex}";
					go.transform.SetParent(borderParent.transform, false);
					go.transform.localPosition = slices[sliceIndex].Mesh.Vertices[borderIndex].Position;
					go.transform.localScale = 0.05f * Vector3.one;
					go.GetComponent<Renderer>().material.color = borderColors[sliceIndex];
				}
				*/
			}


			MeshPostprocessor.Process(mesh, kernelTransform);
			/*
			Plane[] cuttingPlanesSlicer = TransformsToPlanes(cuttingPlanes);
			IList<Slicer.Slice> slices = Slicer.Slicer.Slice(kernel, cuttingPlanesSlicer);

			//ObjExporter.WriteMesh(kernel, @"/home/vlad/Unity/Projects/Popcorn Test/Kernel.obj");

			Plane[] riggingPlanesRigger = TransformsToPlanes(riggingPlanes);
			Rigger.Graph<int> graph = Rigger.Rigger.Rig(kernel, slices, riggingPlanesRigger, riggingRootBonePosition);

			Animator.Animator.Animate(kernel);
			*/

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
				planes[i] =  TransformToPlane(t[i]);
			}

			return planes;
		}
	}
}