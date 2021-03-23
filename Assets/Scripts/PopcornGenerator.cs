using UnityEngine;

public class PopcornGenerator : MonoBehaviour
{
	[SerializeField] private GameObject kernel = null;
	[SerializeField] private Transform[] cuttingPlanes = null;

	private System.Diagnostics.Stopwatch sw;

	private bool drawGraphToggle = false;

	private void Start()
	{
		sw = new System.Diagnostics.Stopwatch();
	}

	private void Update()
	{
		#if DEBUG
		if (Input.GetKeyDown(KeyCode.Space))
		{
			drawGraphToggle = !drawGraphToggle;
		}
		if (drawGraphToggle)
		{
			Popcorn.Rigger.Rigger.DrawGraph();
		}
		#endif

		if (Input.GetButtonDown("Fire1"))
		{
			sw.Start();
			MakePopcorn();
			sw.Stop();

			print($"Elapsed miliseconds: {sw.ElapsedMilliseconds}");
		}
	}

	private void MakePopcorn()
	{
		Plane[] planes = new Plane[cuttingPlanes.Length];

		for (int i = 0; i < planes.Length; ++i)
		{
			planes[i] = TransformToPlane(cuttingPlanes[i]);
			//Debug.Log($"plane: {planes[i].ToString("F5")}");
		}

		Popcorn.Slicer.Slicer.Slice(kernel, planes);
		ObjExporter.WriteMesh(kernel, @"/home/vlad/Unity/Projects/Popcorn Test/Kernel.obj");
		Popcorn.Rigger.Rigger.Rig(kernel);
	}

	private Plane TransformToPlane(Transform t)
	{
		Vector3 planeNormal = t.up;
		Vector3 planeInPoint = t.position;
		return new Plane(planeNormal, planeInPoint);
	}
}
