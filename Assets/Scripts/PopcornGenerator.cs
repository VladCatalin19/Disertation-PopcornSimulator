using UnityEngine;

public class PopcornGenerator : MonoBehaviour
{
	[SerializeField] private GameObject kernel = null;
	[SerializeField] private Transform[] cuttingPlanes = null;
	[SerializeField] private Transform[] riggingPlanes = null;
	[SerializeField] private Transform riggingRootBonePosition = null;

	private System.Diagnostics.Stopwatch sw;

	private void Start()
	{
		sw = new System.Diagnostics.Stopwatch();
	}

	private void Update()
	{
		if (Input.GetButtonDown("Fire1"))
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
		Popcorn.Slicer.Slicer.Slice(kernel, cuttingPlanesSlicer);

		//ObjExporter.WriteMesh(kernel, @"/home/vlad/Unity/Projects/Popcorn Test/Kernel.obj");

		Plane[] riggingPlanesRigger = TransformsToPlanes(riggingPlanes);
		Popcorn.Rigger.Rigger.Rig(kernel, riggingPlanesRigger, riggingRootBonePosition);

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
}
