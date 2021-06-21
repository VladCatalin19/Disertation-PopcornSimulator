using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace PopcornGenerator
{
	public class PopcornGenerator : MonoBehaviour
	{
		[SerializeField] private GameObject kernel = null;
		[SerializeField] private Transform cuttingPlanesParent = null;
		[SerializeField] private Transform riggingPlanesParent = null;

		[SerializeField] private AnimationCurve testCurve = null;
		[SerializeField] private UnityEvent onKernelExpansion = null;

		private System.Diagnostics.Stopwatch stopWatch;
		private bool didPopcorn = false;

		private Plane[] cuttingPlanes = null;
		private Plane[] riggingPlanes = null;

		private void Start()
		{
			CheckIfFieldsAreGood();
			stopWatch = new System.Diagnostics.Stopwatch();

			cuttingPlanes = GenerateRandomCuttingPlanes();
				//GenerateCuttingPlanes();
			riggingPlanes = GenerateRiggingPlanes();

			//PrintCurve();
		}

		private void Update()
		{
			if (Input.GetButtonDown("Fire1") && !didPopcorn)
			{
				stopWatch.Start();
				MakePopcorn();
				stopWatch.Stop();

				print($"Elapsed miliseconds: {stopWatch.ElapsedMilliseconds}");

				didPopcorn = true;
				HidePlanes();
				onKernelExpansion.Invoke();
			}
		}

		private void CheckIfFieldsAreGood()
		{
			if (kernel == null)
				throw new System.ArgumentNullException("kernel");
			if (!kernel.GetComponent<MeshFilter>())
				throw new System.ArgumentException("Provided GameObject does not have a MeshFilter component");

			if (cuttingPlanesParent == null)
				throw new System.ArgumentNullException("cuttingPlanes");
			if (cuttingPlanesParent.childCount == 0)
				throw new System.ArgumentException("Provided CuttingPlanesParent has no children");

			if (riggingPlanesParent == null)
				throw new System.ArgumentNullException("riggingPlanes");
			if (riggingPlanesParent.childCount == 0)
				throw new System.ArgumentException("Provided RiggingPlanesParent has no children");
		}

		private void MakePopcorn()
		{
			Transform kernelTransform = kernel.transform;
			MeshFilter kernelMeshFilter = kernel.GetComponent<MeshFilter>();
			MeshRenderer kernelMeshRenderer = kernel.GetComponent<MeshRenderer>();
			//Plane[] cuttingPlanes = GenerateCuttingPlanes();
			//Plane[] riggingPlanes = GenerateRiggingPlanes();
			Mesh mesh = new Mesh(kernelMeshFilter.mesh);

			Utils.TransformVerticesToWorldSpace(mesh.Vertices, kernelTransform);
			var slices = Slicer.SliceMesh(mesh, cuttingPlanes);
			Puffer.Puff(slices);
			var riggedSlices = Rigger.RigSlices(slices, riggingPlanes);
			var skinnedMeshRenderers = MeshProcessing.RiggedSlicesToSkinnedMeshRenderers(
				riggedSlices, kernelTransform, kernelMeshRenderer
			);

			Animator.Animate(skinnedMeshRenderers);

			Destroy(kernelMeshFilter);
			Destroy(kernelMeshRenderer);
		}

		private Plane[] GenerateCuttingPlanes()
		{
			return TransformsToPlanes(cuttingPlanesParent);
		}

		private Plane[] GenerateRiggingPlanes()
		{
			return TransformsToPlanes(riggingPlanesParent);
		}

		private Plane[] TransformsToPlanes(Transform planesParent)
		{
			Plane[] planes = new Plane[planesParent.childCount];
			for (int planeIndex = 0; planeIndex < planes.Length; ++planeIndex)
			{
				planes[planeIndex] = TransformToPlane(planesParent.GetChild(planeIndex));
			}
			return planes;
		}

		private Plane TransformToPlane(Transform t)
		{
			Vector3 planeNormal = t.up;
			Vector3 planeInPoint = t.position;
			return new Plane(planeNormal, planeInPoint);
		}

		private Plane[] GenerateRandomCuttingPlanes()
		{
			int planesNum = Random.Range(2, 3);
			Plane[] planes = new Plane[planesNum];

			float minAngleBetween = 50.0f;
			float maxAngleBetween = 180.0f / planesNum;

			float angleJitter = Random.Range(0.0f, 180.0f);

			Vector2 randomVector = Random.insideUnitCircle.normalized;
			Vector3 planesInPoint = kernel.transform.position;// + 0.1f * new Vector3(randomVector.x, 0.0f, randomVector.y);

			float totalAngle = angleJitter;

			for (int planeIndex = 0; planeIndex < planesNum; ++planeIndex)
			{
				Vector3 planeNormal = new Vector3(Mathf.Cos(totalAngle), 0.0f, Mathf.Sin(totalAngle));
				planes[planeIndex] = new Plane(planeNormal, planesInPoint);

				float currentAngle = Random.Range(minAngleBetween, maxAngleBetween);
				totalAngle += currentAngle;

				#if true
				GameObject go = Instantiate(cuttingPlanesParent.GetChild(0).gameObject);
				go.SetActive(true);
				go.transform.position = kernel.transform.position;
				go.transform.eulerAngles = new Vector3(0.0f, totalAngle, 90.0f);
				go.transform.parent = cuttingPlanesParent;
				print($"Angle between last 2 planes: {currentAngle}");

				planes[planeIndex] = new Plane(go.transform.up, planesInPoint);

				//Debug.Log($"{go2.transform.up} {planeNormal}");
				#endif
			}

			#if false
			float minAngle = 30.0f;
			Vector2 randomVector = Random.insideUnitCircle;
			Vector3 planesInPoint = transform.position + 0.1f * new Vector3(randomVector.x, 0.0f, randomVector.y);

			for (int planeIndex = 0; planeIndex < planesNum; ++planeIndex)
			{
				int maxSteps = 100;
				bool goodAngle = true;
				Vector3 planeNormal;
				do
				{
					randomVector = Random.insideUnitCircle;
					planeNormal = new Vector3(randomVector.x, 0.0f, randomVector.y).normalized;

					for (int planeIndexCheck = 0; planeIndexCheck < planeIndex; ++planeIndexCheck)
					{
						if (Vector3.Angle(planes[planeIndexCheck].Normal, planeNormal) < minAngle)
						{
							Debug.Log($"Angle: {Vector3.Angle(planes[planeIndexCheck].Normal, planeNormal)}");
							goodAngle = false;
							break;
						}
					}
				}
				while (!goodAngle && --maxSteps > 0);

				if (maxSteps == 0)
				{
					Debug.LogError($"Max Steps reached");
				}

				planes[planeIndex] = new Plane(planeNormal, planesInPoint);
			}
			#endif
			return planes;
		}

		private void HidePlanes()
		{
			cuttingPlanesParent.gameObject.SetActive(false);
			riggingPlanesParent.gameObject.SetActive(false);
		}

		private void PrintCurve()
		{
			System.Text.StringBuilder strBuilder = new System.Text.StringBuilder();
			strBuilder.Append("new Keyframe[] {\n");

			System.Globalization.CultureInfo usFormat = new System.Globalization.CultureInfo("en-US");

			foreach (Keyframe keyFrame in testCurve.keys)
			{
				strBuilder.Append("new Keyframe(")
					.Append(keyFrame.time.ToString("F5", usFormat)).Append("f, ")
					.Append(keyFrame.value.ToString("F5", usFormat)).Append("f, ")
					.Append(keyFrame.inTangent.ToString("F5", usFormat)).Append("f, ")
					.Append(keyFrame.outTangent.ToString("F5", usFormat)).Append("f, ")
					.Append(keyFrame.inWeight.ToString("F5", usFormat)).Append("f, ")
					.Append(keyFrame.outWeight.ToString("F5", usFormat))
					.Append("f),\n");
			}

			strBuilder.Append("}");
			print(strBuilder);
		}
	}
}