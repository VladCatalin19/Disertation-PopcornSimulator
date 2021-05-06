using UnityEngine;

namespace PopcornGenerator
{
	public class PopcornGenerator : MonoBehaviour
	{
		[SerializeField] private GameObject kernel = null;
		[SerializeField] private Transform[] cuttingPlanes = null;
		[SerializeField] private Transform[] riggingPlanes = null;
		[SerializeField] private Transform riggingRootBonePosition = null;
		[SerializeField] private AnimationCurve test = null;

		private System.Diagnostics.Stopwatch sw;
		private bool didPopcorn = false;

		private void Start()
		{
			sw = new System.Diagnostics.Stopwatch();
			//PrintAnimationCurveKeys();

			/*
			System.Collections.Generic.ICollection<int> col = new int[] {1, 2, 3};
			System.Collections.Generic.IEnumerator<int> en = col.GetEnumerator();
			en.MoveNext();
			Debug.Log($"First element: {en.Current}");
			*/
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
			Plane[] cuttingPlanesSlicer = TransformsToPlanes(cuttingPlanes);
			Slicer.Slicer.Slice(kernel, cuttingPlanesSlicer);

			//ObjExporter.WriteMesh(kernel, @"/home/vlad/Unity/Projects/Popcorn Test/Kernel.obj");

			Plane[] riggingPlanesRigger = TransformsToPlanes(riggingPlanes);
			Rigger.Rigger.Rig(kernel, riggingPlanesRigger, riggingRootBonePosition);

			//Popcorn.Animator.Animator.Animate(kernel);

			didPopcorn = true;

			HidePlanes();
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

		private void PrintAnimationCurveKeys()
		{
			foreach (Keyframe keyFrame in test.keys)
			{
				Debug.Log(
					// float time, float value, float inTangent, float outTangent, float inWeight, float outWeight
					$"new Keyframe(time:{keyFrame.time}f, value:{keyFrame.value}f, inTangent:{keyFrame.inTangent}f, " +
					$"outTangent:{keyFrame.outTangent}f, inWeight:{keyFrame.inWeight}f, outWeight:{keyFrame.outWeight}f);"
					/*
					$"Time: {keyFrame.time}\n" +
					$"Value: {keyFrame.value}\n" +
					$"InTangent: {keyFrame.inTangent}\n" +
					$"OutTangent: {keyFrame.outTangent}\n" +
					$"InWeight: {keyFrame.inWeight}\n" +
					$"OutWeight: {keyFrame.outWeight}"
					*/
				);
			}
		}
	}

}