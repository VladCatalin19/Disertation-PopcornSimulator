using UnityEngine;

namespace Popcorn.Rigger
{
	public static class Rigger
	{
		public static void Rig(GameObject gameObject)
		{
			if (!gameObject) throw new System.ArgumentNullException("gameObject");

			MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
			if (!meshFilter) throw new System.ArgumentException("Provided GameObject does not have a MeshFilter component");
		}
	}

}
