using UnityEngine;

public class Playground : MonoBehaviour
{
    [SerializeField] private GameObject prefab = null;
    [SerializeField] private Transform spawnsParent = null;

    private Mesh[] meshes = new Mesh[125];
    private MeshCollider[] meshColliders = new MeshCollider[125];
    private int updateEveryXFrames = 5;
    private int currentUpdateIndex = 0;

    private void Start()
    {
        Time.timeScale = 0.25f;
        for (int x = 0; x < 5; ++x)
        {
            for (int y = 0; y < 5; ++y)
            {
                for (int z = 0; z < 5; ++z)
                {
                    Vector3 position = new Vector3(x - 2, y + 1, z);
                    GameObject go = Instantiate(prefab, position, Quaternion.identity, spawnsParent);

                    Mesh m = go.GetComponent<MeshFilter>().mesh;
                    m.MarkDynamic();
                    meshes[x * 25 + y * 5 + z] = m;
                    meshColliders[x * 25 + y * 5 + z] = go.GetComponent<MeshCollider>();
                }
            }
        }
    }

    private void Update()
    {
        int updateIndexStart = currentUpdateIndex * (125 / updateEveryXFrames);
        int updateIndexEnd = (currentUpdateIndex + 1) * (125 / updateEveryXFrames);
    
        for (int i = updateIndexStart; i < updateIndexEnd; ++i)
        {
            Mesh mesh = meshes[i];
            MeshCollider meshCollider = meshColliders[i];

            Vector3[] vertices = mesh.vertices;
            Vector2[] uvs = mesh.uv;
            int[] triangles = mesh.triangles;

            for (int j = 0; j < vertices.Length; ++j)
            {
                vertices[j] += Random.insideUnitSphere * 0.001f;
            }
            for (int j = 0 ; j < uvs.Length; ++j)
            {
                uvs[j] += Random.insideUnitCircle;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
        }

        currentUpdateIndex = (currentUpdateIndex + 1) % updateEveryXFrames;
    }
}
