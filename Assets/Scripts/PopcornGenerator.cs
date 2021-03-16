using UnityEngine;
using PopcornSlicer = Popcorn.Slicer;

public class PopcornGenerator : MonoBehaviour
{
	[SerializeField] private GameObject kernel = null;
	[SerializeField] private Transform[] cuttingPlanes = null;

	private void Start()
	{
		Plane[] planes = new Plane[cuttingPlanes.Length];

		for (int i = 0; i < planes.Length; ++i)
		{
			planes[i] = TransformToPlane(cuttingPlanes[i]);
		}

		PopcornSlicer.Slicer.Slice(kernel, planes);
	}

	private Plane TransformToPlane(Transform t)
	{
		Vector3 planeNormal = t.up;
		Vector3 planeInPoint = t.position;
		return new Plane(planeNormal, planeInPoint);
	}
}
