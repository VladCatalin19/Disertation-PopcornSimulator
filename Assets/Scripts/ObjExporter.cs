using UnityEngine;
using System.IO;
using System.Text;

// https://wiki.unity3d.com/index.php?title=ExportOBJ
public class ObjExporter
{
	public static void WriteMesh(GameObject gameObject, string path)
	{
		if (!gameObject) throw new System.ArgumentNullException("gameObject");

		MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
		if (!meshFilter)
			throw new System.ArgumentException("Provided GameObject does not have a MeshFilter Component");

		Renderer renderer = gameObject.GetComponent<Renderer>();
		if (!renderer)
			throw new System.ArgumentException("Provided GameObject does not have a Renderer Component");
		
		Material[] materials = renderer.sharedMaterials;

		using (StreamWriter sw = new StreamWriter(path)) 
		{
			sw.Write(MeshToString(meshFilter.mesh, materials));
		}
	}

	private static string MeshToString(Mesh mesh, Material[] materials)
	{
		StringBuilder stringBuilder = new StringBuilder();

		stringBuilder.Append("g ").Append(mesh.name).Append("\n");

		foreach (Vector3 vertex in mesh.vertices)
		{
			stringBuilder.Append($"v {vertex.x} {vertex.y} {vertex.z}\n");
		}
		stringBuilder.Append("\n");

		foreach (Vector3 normal in mesh.normals)
		{
			stringBuilder.Append($"vn {normal.x} {normal.y} {normal.z}\n");
		}
		stringBuilder.Append("\n");

		foreach (Vector3 uv in mesh.uv)
		{
			stringBuilder.Append($"vt {uv.x} {uv.y}\n");
		}

		for (int material = 0; material < mesh.subMeshCount; ++material)
		{
			stringBuilder.Append("\n");
			stringBuilder.Append($"usemtl {materials[material].name}\n");
			stringBuilder.Append($"usemap {materials[material].name}\n");
 
			int[] triangles = mesh.GetTriangles(material);
			for (int i = 0; i < triangles.Length; i += 3)
			{
				int v0 = triangles[i] + 1;
				int v1 = triangles[i + 1] + 1;
				int v2 = triangles[i + 2] + 1;
				stringBuilder.Append($"f {v0}/{v0}/{v0} {v1}/{v1}/{v1} {v2}/{v2}/{v2}\n");
			}
		}
		return stringBuilder.ToString();
	}
}
